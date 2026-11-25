using UnityEngine;
using System.Collections;

/// <summary>
/// Director del Nivel 3: Callejones Coloniales de Guanajuato y la Basílica.
///  - Distribuye esbirros (Monjes Penitentes, Sombras y Catrines) en los callejones.
///  - Inicia la narración de Tonalli con la leyenda del Callejón del Beso y la Inquisición.
///  - Configura el combate final del juego contra el Clérigo Inquisidor.
/// </summary>
public class DirectorNivelCiudad : MonoBehaviour
{
    [Header("Configuración de Enemigos")]
    public int monjesIniciales = 6;
    public int catrinesIniciales = 4;
    public int comadresIniciales = 3; // Añadido: Comadres chismosas en la ciudad

    [Header("Objetivos")]
    public int sellosRequeridos = 3;

    void Start()
    {
        EstadoNivel.Cambiar(EstadoDeNivel.Jugando);
        Time.timeScale = 1f;
        SistemaGuardado.SincronizarNivel(3, "Nivel3_Ciudad");

        // Inicializar sistemas compartidos
        gameObject.AddComponent<InicializadorNivel>();
        ArreglosEscenaRuntime.EjecutarAhora("Nivel3_Ciudad");

        ConfigurarAmbienteIluminacion();
        ConfigurarAmbienteSonoro();
        AsegurarJugadorYCamara();
        InventarioEspiritual inv = Object.FindAnyObjectByType<InventarioEspiritual>();
        if (inv != null) { inv.cantidadTotems = 0; }
        SpawnearEnemigosCiudad();
        StartCoroutine(RutinaIntroTonalli());
    }

    void ConfigurarAmbienteIluminacion()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.18f, 0.12f, 0.15f); // Atmósfera cálida colonial de noche
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.12f, 0.08f, 0.10f);
        RenderSettings.fogDensity = 0.015f;

        GameObject luzDir = new GameObject("LuzLunaColonial");
        Light l = luzDir.AddComponent<Light>();
        l.type = LightType.Directional;
        l.color = new Color(0.85f, 0.75f, 0.65f);
        l.intensity = 0.75f;
        luzDir.transform.rotation = Quaternion.Euler(45f, -60f, 0f);
    }

    void ConfigurarAmbienteSonoro()
    {
        // Si ya existe un ambiente sonoro en la escena, no duplicar
        if (GameObject.Find("AmbienteSonoroCiudad") != null) return;

        GameObject goAmb = new GameObject("AmbienteSonoroCiudad");
        AudioSource amb = goAmb.AddComponent<AudioSource>();
        amb.loop = true;
        amb.spatialBlend = 0f;
        amb.volume = 0.35f * Mathf.Lerp(0.4f, 1.2f, OpcionesJuego.Musica);

        const int rate = 22050;
        const float dur = 8f;
        int n = (int)(rate * dur);
        float[] d = new float[n];
        System.Random rng = new System.Random(789);
        float lp = 0f, lp2 = 0f;

        for (int i = 0; i < n; i++)
        {
            float t = (float)i / rate;
            float ruido = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp += (ruido - lp) * 0.02f;
            lp2 += (lp - lp2) * 0.04f;

            // Viento encañonado en callejones estrechos
            float mod = 0.55f + 0.30f * Mathf.Sin(2f * Mathf.PI * 0.18f * t) + 0.15f * Mathf.Sin(2f * Mathf.PI * 0.4f * t + 1.1f);
            float viento = lp2 * 7.5f * mod;

            // Eco lejano de campana de bronce (doblado solemne)
            float campanaEnv = Mathf.Exp(-3.5f * (t % 4.0f));
            float campana = Mathf.Sin(2f * Mathf.PI * 220f * t) * campanaEnv * 0.05f +
                            Mathf.Sin(2f * Mathf.PI * 440f * t) * campanaEnv * 0.025f;

            d[i] = Mathf.Clamp(viento + campana, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("AmbienteCiudad", n, 1, rate, false);
        clip.SetData(d, 0);
        amb.clip = clip;
        amb.Play();
    }

    void AsegurarJugadorYCamara()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            GameObject prefab = Resources.Load<GameObject>("Chofer") ?? Resources.Load<GameObject>("Player");
            if (prefab != null)
            {
                player = Instantiate(prefab, new Vector3(0, 0.5f, 5f), Quaternion.identity);
            }
            else
            {
                player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                player.name = "Chofer";
                player.tag = "Player";
                player.transform.position = new Vector3(0, 1f, 5f);
                player.AddComponent<CharacterController>();
                player.AddComponent<ChoferController>();
                player.AddComponent<SistemaCombateChofer>();
                player.AddComponent<PoderEspiritual>();
                player.AddComponent<InventarioEspiritual>();
            }
        }

        Camera cam = Camera.main;
        if (cam != null && cam.GetComponent<CamaraTerceraPersona>() == null)
        {
            CamaraTerceraPersona c3 = cam.gameObject.AddComponent<CamaraTerceraPersona>();
            c3.objetivo = player.transform;
        }

        if (GameManager.Instancia == null)
        {
            GameObject gmGo = new GameObject("GameManager");
            GameManager gm = gmGo.AddComponent<GameManager>();
            gm.totemsRequeridos = sellosRequeridos;
            gm.siguienteEscena = "MainMenu"; // Al vencer el Nivel 3 concluye la aventura
        }

        if (Object.FindAnyObjectByType<HUDMexicano>() == null)
        {
            GameObject hudGo = new GameObject("HUDMexicano");
            hudGo.AddComponent<HUDMexicano>();
        }

        if (GuiaAlebrije.Instancia == null)
        {
            GameObject guiaGo = new GameObject("GuiaAlebrije");
            guiaGo.AddComponent<GuiaAlebrije>();
        }
    }

    void SpawnearEnemigosCiudad()
    {
        int totalMonjes = Mathf.Max(monjesIniciales, 14);
        int totalCatrines = Mathf.Max(catrinesIniciales, 8);
        int totalComadres = Mathf.Max(comadresIniciales, 7);

        if (SpawnSendero.Disponible)
        {
            for (int i = 0; i < totalMonjes; i++)
            {
                float frac = Mathf.Lerp(0.08f, 0.86f, (i + 0.5f) / totalMonjes);
                float lat = (i % 2 == 0) ? -2.0f : 2.0f;
                Vector3 p = SpawnSendero.PuntoLejosDeChuy(frac, lat);
                FabricaEsbirros.Crear(EsbirroBaseIA.TipoEsbirro.Monje, p + Vector3.up * 0.1f);
            }

            for (int i = 0; i < totalCatrines; i++)
            {
                float frac = Mathf.Lerp(0.14f, 0.84f, (i + 0.5f) / totalCatrines);
                float lat = (i % 2 == 0) ? 3.0f : -3.0f;
                Vector3 p = SpawnSendero.PuntoLejosDeChuy(frac, lat);
                FabricaEsbirros.Crear(EsbirroBaseIA.TipoEsbirro.Catrin, p + Vector3.up * 0.1f);
            }

            for (int i = 0; i < totalComadres; i++)
            {
                float frac = Mathf.Lerp(0.12f, 0.82f, (i + 0.5f) / totalComadres);
                float lat = (i % 2 == 0) ? -2.8f : 2.8f;
                Vector3 p = SpawnSendero.PuntoLejosDeChuy(frac, lat);
                FabricaEsbirros.Crear(EsbirroBaseIA.TipoEsbirro.Comadre, p + Vector3.up * 0.2f);
            }
            return;
        }

        // Monjes Penitentes en los callejones
        for (int i = 0; i < monjesIniciales; i++)
        {
            float z = 18f + i * 14f;
            float x = (i % 2 == 0) ? -4f : 4f;
            Vector3 pos = new Vector3(x, 0.2f, z);
            Vector3 suelo;
            if (UtilTerreno.SueloEn(x, z, out suelo)) pos = suelo + Vector3.up * 0.1f;
            FabricaEsbirros.Crear(EsbirroBaseIA.TipoEsbirro.Monje, pos);
        }

        // Catrines en las plazuelas
        for (int i = 0; i < catrinesIniciales; i++)
        {
            float z = 32f + i * 20f;
            float x = (i % 2 == 0) ? 5.5f : -5.5f;
            Vector3 pos = new Vector3(x, 0.2f, z);
            Vector3 suelo;
            if (UtilTerreno.SueloEn(x, z, out suelo)) pos = suelo + Vector3.up * 0.1f;
            FabricaEsbirros.Crear(EsbirroBaseIA.TipoEsbirro.Catrin, pos);
        }

        // Comadres chismosas en las esquinas
        for (int i = 0; i < comadresIniciales; i++)
        {
            float z = 25f + i * 25f;
            float x = (i % 2 == 0) ? -6f : 6f;
            Vector3 pos = new Vector3(x, 0.2f, z);
            Vector3 suelo;
            if (UtilTerreno.SueloEn(x, z, out suelo)) pos = suelo + Vector3.up * 0.2f;
            FabricaEsbirros.Crear(EsbirroBaseIA.TipoEsbirro.Comadre, pos);
        }
    }

    IEnumerator RutinaIntroTonalli()
    {
        yield return new WaitForSeconds(1.2f);
        if (GuiaAlebrije.Instancia != null)
        {
            GuiaAlebrije.Instancia.ContarHistoria("intro_ciudad", true);
        }
        else
        {
            string[] lineas = HistoriaNivel3.Lineas("intro_ciudad");
            if (lineas != null)
            {
                foreach (string l in lineas)
                {
                    AvisosJuego.Mostrar("TONALLI:\n«" + l + "»", 4f);
                    yield return new WaitForSeconds(4.2f);
                }
            }
        }
    }
}
