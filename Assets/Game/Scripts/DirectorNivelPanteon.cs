using UnityEngine;
using System.Collections;

/// <summary>
/// Director del Nivel 2: Panteón de Santa Paula.
///  - Distribuye enemigos temáticos (Momias, Catrines, Cuervos) en los pasillos de las criptas.
///  - Gestiona la iluminación fúnebre nocturna con niebla de fuego fatuo.
///  - Inicia la narración de Tonalli con la leyenda de las momias.
///  - Asegura que el Chofer, la Cámara y el HUD estén configurados.
/// </summary>
public class DirectorNivelPanteon : MonoBehaviour
{
    [Header("Configuración de Enemigos")]
    public int momiasIniciales = 6;
    public int catrinesIniciales = 3;
    public int cuervosIniciales = 4;

    [Header("Objetivos")]
    public int ciriosRequeridos = 3;

    void Start()
    {
        EstadoNivel.Cambiar(EstadoDeNivel.Jugando);
        Time.timeScale = 1f;
        SistemaGuardado.SincronizarNivel(2, "Nivel2_Panteon");
        ConfigurarAmbienteIluminacion();
        AsegurarJugadorYCamara();
        InventarioEspiritual inv = Object.FindAnyObjectByType<InventarioEspiritual>();
        if (inv != null) { inv.cantidadTotems = 0; }
        SpawnearEnemigosPanteon();
        StartCoroutine(RutinaIntroTonalli());
    }

    void ConfigurarAmbienteIluminacion()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.08f, 0.16f, 0.14f); // Tono verdoso de ultratumba
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.05f, 0.12f, 0.10f);
        RenderSettings.fogDensity = 0.025f;

        // Luz de luna espectral
        GameObject luzDir = new GameObject("LuzLunaPanteon");
        Light l = luzDir.AddComponent<Light>();
        l.type = LightType.Directional;
        l.color = new Color(0.4f, 0.7f, 0.65f);
        l.intensity = 0.65f;
        luzDir.transform.rotation = Quaternion.Euler(50f, -40f, 0f);
    }

    void AsegurarJugadorYCamara()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            // Cargar prefab o buscar chofer
            GameObject prefab = Resources.Load<GameObject>("Chofer") ?? Resources.Load<GameObject>("Player");
            if (prefab != null)
            {
                player = Instantiate(prefab, new Vector3(0, 0.5f, 4f), Quaternion.identity);
            }
            else
            {
                player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                player.name = "Chofer";
                player.tag = "Player";
                player.transform.position = new Vector3(0, 1f, 4f);
                player.AddComponent<CharacterController>();
                player.AddComponent<ChoferController>();
                player.AddComponent<SistemaCombateChofer>();
                player.AddComponent<PoderEspiritual>();
                player.AddComponent<InventarioEspiritual>();
            }
        }

        // Asegurar cámara
        Camera cam = Camera.main;
        if (cam != null && cam.GetComponent<CamaraTerceraPersona>() == null)
        {
            CamaraTerceraPersona c3 = cam.gameObject.AddComponent<CamaraTerceraPersona>();
            c3.objetivo = player.transform;
        }

        // Asegurar GameManager y HUD
        if (GameManager.Instancia == null)
        {
            GameObject gmGo = new GameObject("GameManager");
            GameManager gm = gmGo.AddComponent<GameManager>();
            gm.totemsRequeridos = ciriosRequeridos;
            gm.siguienteEscena = "Nivel3_Ciudad";
        }

        if (Object.FindAnyObjectByType<HUDMexicano>() == null)
        {
            GameObject hudGo = new GameObject("HUDMexicano");
            hudGo.AddComponent<HUDMexicano>();
        }

        // Asegurar Guía Tonalli
        if (GuiaAlebrije.Instancia == null)
        {
            GameObject guiaGo = new GameObject("GuiaAlebrije");
            guiaGo.AddComponent<GuiaAlebrije>();
        }
    }

    void SpawnearEnemigosPanteon()
    {
        // Spawns de Momias a lo largo del pasillo
        for (int i = 0; i < momiasIniciales; i++)
        {
            float z = 18f + i * 12f;
            float x = (i % 2 == 0) ? -5f : 5f;
            FabricaEsbirros.Crear(EsbirroBaseIA.TipoEsbirro.Momia, new Vector3(x, 0.2f, z));
        }

        // Spawns de Catrines cerca de mausoleos
        for (int i = 0; i < catrinesIniciales; i++)
        {
            float z = 28f + i * 22f;
            float x = (i % 2 == 0) ? 9f : -9f;
            FabricaEsbirros.Crear(EsbirroBaseIA.TipoEsbirro.Catrin, new Vector3(x, 0.2f, z));
        }

        // Spawns de Cuervos
        for (int i = 0; i < cuervosIniciales; i++)
        {
            float z = 22f + i * 18f;
            FabricaEsbirros.Crear(EsbirroBaseIA.TipoEsbirro.Cuervo, new Vector3(Random.Range(-4f, 4f), 0.5f, z));
        }
    }

    IEnumerator RutinaIntroTonalli()
    {
        yield return new WaitForSeconds(1.2f);
        if (GuiaAlebrije.Instancia != null)
        {
            GuiaAlebrije.Instancia.ContarHistoria("intro_panteon", true);
        }
        else
        {
            string[] lineas = HistoriaNivel2.Lineas("intro_panteon");
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
