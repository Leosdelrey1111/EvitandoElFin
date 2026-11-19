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

    [Header("Objetivos")]
    public int sellosRequeridos = 3;

    void Start()
    {
        EstadoNivel.Cambiar(EstadoDeNivel.Jugando);
        Time.timeScale = 1f;
        SistemaGuardado.SincronizarNivel(3, "Nivel3_Ciudad");
        ConfigurarAmbienteIluminacion();
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
        for (int i = 0; i < monjesIniciales; i++)
        {
            float z = 18f + i * 14f;
            float x = (i % 2 == 0) ? -4f : 4f;
            FabricaEsbirros.Crear(EsbirroBaseIA.TipoEsbirro.Monje, new Vector3(x, 0.2f, z));
        }

        for (int i = 0; i < catrinesIniciales; i++)
        {
            float z = 32f + i * 20f;
            float x = (i % 2 == 0) ? 5.5f : -5.5f;
            FabricaEsbirros.Crear(EsbirroBaseIA.TipoEsbirro.Catrin, new Vector3(x, 0.2f, z));
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
