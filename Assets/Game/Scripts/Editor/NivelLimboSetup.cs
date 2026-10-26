using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;

/// <summary>
/// Crea la escena "Nivel1_Limbo": terreno vacío (cañón al fondo del barranco) + Chofer con todos sus
/// componentes + cámara + iluminación del limbo + secuencia de despertar confundido.
/// El diseño del nivel (props, enemigos, tótems) lo haces tú después sobre este terreno.
/// </summary>
public static class NivelLimboSetup
{
    const string RutaEscena = "Assets/Scenes/Nivel1_Limbo.unity";
    const string RutaTerrenoData = "Assets/Scenes/Nivel1_Limbo_TerrainData.asset";
    const string RutaModeloChofer = "Assets/Game/Models/Chofer/Meshy_AI_chofer_trailer_3d_biped_Character_output.fbx";
    const string RutaAnimController = "Assets/Game/Animations/Chofer/ChoferAnimController.controller";

    [MenuItem("EvitandoElFin/6. Crear Escena Nivel1_Limbo (terreno vacío + Chofer confundido)")]
    public static void CrearEscenaLimbo()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // Si en la escena actual ya hay un Chofer ajustado a tu gusto, copiamos su escala visual.
        Vector3 escalaModelo = Vector3.one;
        var choferActual = Object.FindAnyObjectByType<ChoferController>(FindObjectsInactive.Include);
        if (choferActual != null)
        {
            var anim = choferActual.GetComponentInChildren<Animator>(true);
            if (anim != null && anim.transform != choferActual.transform) escalaModelo = anim.transform.localScale;
        }

        var escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        ConfigurarAmbiente();
        Terrain terreno = CrearTerreno();
        GameObject chofer = CrearChofer(terreno, escalaModelo);
        ConfigurarCamara(chofer);

        // GameManager con pausa/derrota (sin portal de prueba: el nivel lo diseñas tú)
        LevelUIBuilder.MontarManagersYPaneles(false);

        // Secuencia de despertar confundido
        GameObject despertar = new GameObject("Secuencia_Despertar");
        DespertarConfusion dc = despertar.AddComponent<DespertarConfusion>();
        dc.chofer = chofer.GetComponent<ChoferController>();
        dc.camara = Camera.main != null ? Camera.main.GetComponent<CamaraTerceraPersona>() : null;

        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
        EditorSceneManager.SaveScene(escena, RutaEscena);
        AgregarABuildSettings(RutaEscena);
        AssetDatabase.SaveAssets();

        Debug.Log("<color=#00FFAA>[NivelLimbo] Escena creada: " + RutaEscena + "</color>");
        EditorUtility.DisplayDialog("Evitando El Fin",
            "Escena 'Nivel1_Limbo' creada y agregada a Build Settings.\n\n" +
            "• Terreno vacío tipo cañón (borde de colinas alrededor).\n" +
            "• Chofer en el centro, despierta confundido al darle Play.\n" +
            "• Luz fría + niebla de limbo.\n\n" +
            "Puedes darle Play directamente en esta escena para probar el despertar.", "Listo");
    }

    // ------------------------------------------------------------------ ambiente

    static void ConfigurarAmbiente()
    {
        // Luz direccional = luz de luna fría y baja
        Light sol = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional);
        if (sol != null)
        {
            sol.name = "Luz_Luna_Limbo";
            sol.color = new Color(0.62f, 0.72f, 1f);
            sol.intensity = 0.9f;
            sol.transform.rotation = Quaternion.Euler(38f, -35f, 0f);
            sol.shadows = LightShadows.Soft;
        }

        Color niebla = new Color(0.20f, 0.27f, 0.33f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.16f, 0.20f, 0.28f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.012f;
        RenderSettings.fogColor = niebla;

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = niebla;
            cam.farClipPlane = 600f;
        }
    }

    // ------------------------------------------------------------------ terreno

    static Terrain CrearTerreno()
    {
        TerrainData td = new TerrainData();
        td.heightmapResolution = 257;
        td.size = new Vector3(200f, 24f, 200f);

        int res = td.heightmapResolution;
        float[,] h = new float[res, res];
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float nx = x / (res - 1f);
                float ny = y / (res - 1f);
                float dx = (nx - 0.5f) * 2f;
                float dy = (ny - 0.5f) * 2f;
                float d = Mathf.Sqrt(dx * dx + dy * dy); // 0 centro, 1 borde del cuadrado inscrito

                // Cañón: piso plano al centro y paredes de colinas que suben hacia los bordes
                float pared = Mathf.SmoothStep(0.62f, 1.05f, d) * 15f;
                // Ondulación suave y muy leve en el piso (no totalmente plano)
                float ruido = (Mathf.PerlinNoise(nx * 6f, ny * 6f) - 0.5f) * 0.9f * Mathf.Clamp01(d * 2f);
                // Irregularidad extra solo en las paredes
                float rocas = Mathf.PerlinNoise(nx * 14f + 7f, ny * 14f + 3f) * 4f * Mathf.SmoothStep(0.55f, 1f, d);

                h[y, x] = Mathf.Clamp01((pared + ruido + rocas) / td.size.y);
            }
        }
        td.SetHeights(0, 0, h);

        // Capa de pintura: usa la que ya tengas en el proyecto (NewLayer), si existe
        TerrainLayer capa = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/NewLayer.terrainlayer");
        if (capa == null) capa = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/NewLayer 1.terrainlayer");
        if (capa != null) td.terrainLayers = new[] { capa };

        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
        AssetDatabase.CreateAsset(td, RutaTerrenoData);

        GameObject go = Terrain.CreateTerrainGameObject(td);
        go.name = "Terreno_Limbo";
        go.transform.position = new Vector3(-100f, 0f, -100f);
        go.isStatic = true;
        return go.GetComponent<Terrain>();
    }

    // ------------------------------------------------------------------ chofer

    static GameObject CrearChofer(Terrain terreno, Vector3 escalaModelo)
    {
        GameObject root = new GameObject("Chofer");
        root.tag = "Player";

        float yPiso = terreno != null ? terreno.SampleHeight(new Vector3(0f, 0f, 0f)) + terreno.transform.position.y : 0f;
        root.transform.position = new Vector3(0f, yPiso + 1.1f, 0f);
        root.transform.rotation = Quaternion.Euler(0f, 160f, 0f);

        CharacterController cc = root.AddComponent<CharacterController>();
        cc.center = Vector3.zero;
        cc.height = 2f;
        cc.radius = 0.5f;

        // Modelo visual
        GameObject modeloPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaModeloChofer);
        GameObject modelo;
        if (modeloPrefab != null)
        {
            modelo = (GameObject)PrefabUtility.InstantiatePrefab(modeloPrefab);
            modelo.transform.SetParent(root.transform, false);
        }
        else
        {
            Debug.LogWarning("[NivelLimbo] No se encontró el modelo del Chofer en " + RutaModeloChofer + ". Se usa una cápsula temporal.");
            modelo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Object.DestroyImmediate(modelo.GetComponent<Collider>());
            modelo.transform.SetParent(root.transform, false);
        }
        modelo.name = "ChoferModelo";
        modelo.transform.localPosition = new Vector3(0f, -1f, 0f);
        modelo.transform.localRotation = Quaternion.identity;
        modelo.transform.localScale = escalaModelo;

        // Animator con el controller del proyecto
        Animator anim = modelo.GetComponentInChildren<Animator>();
        if (anim == null) anim = modelo.AddComponent<Animator>();
        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(RutaAnimController);
        if (controller != null) anim.runtimeAnimatorController = controller;
        if (anim.avatar == null)
        {
            Avatar av = AssetDatabase.LoadAllAssetsAtPath(RutaModeloChofer).OfType<Avatar>().FirstOrDefault();
            if (av != null) anim.avatar = av;
        }
        anim.applyRootMotion = false;

        // Scripts de gameplay
        ChoferController cch = root.AddComponent<ChoferController>();
        cch.animator = anim;
        root.AddComponent<PoderEspiritual>();
        root.AddComponent<InventarioEspiritual>();

        // Combate: hitbox melé frente al Chofer
        GameObject hit = new GameObject("HitboxMele");
        hit.transform.SetParent(root.transform, false);
        hit.transform.localPosition = new Vector3(0f, 0f, 1.1f);
        BoxCollider bc = hit.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size = new Vector3(1.4f, 1.6f, 1.4f);
        hit.AddComponent<HitboxAtaque>();
        hit.SetActive(false);

        SistemaCombateChofer combate = root.AddComponent<SistemaCombateChofer>();
        combate.hitboxMele = hit;

        return root;
    }

    static void ConfigurarCamara(GameObject chofer)
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        cam.nearClipPlane = 0.1f;
        cam.transform.position = chofer.transform.position + new Vector3(0f, 2.5f, -5f);

        CamaraTerceraPersona c = cam.GetComponent<CamaraTerceraPersona>();
        if (c == null) c = cam.gameObject.AddComponent<CamaraTerceraPersona>();
        c.objetivo = chofer.transform;
    }

    // ------------------------------------------------------------------ build settings

    static void AgregarABuildSettings(string ruta)
    {
        var lista = EditorBuildSettings.scenes.ToList();
        if (lista.Any(s => s.path == ruta)) return;
        lista.Add(new EditorBuildSettingsScene(ruta, true));
        EditorBuildSettings.scenes = lista.ToArray();
    }
}
