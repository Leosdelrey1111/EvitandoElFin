using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// Crea la escena "Nivel2_Panteon" (Nivel 2 del juego):
///  - Mapa monumental de 640 m en las altas cumbres de la Sierra de Guanajuato de noche.
///  - Sendero de ~1300 m con riscos de cantera rosa, muros de gavetas, mausoleos familiares,
///    tumbas con veladoras y cempasúchil, nopales, biznagas y cipreses funerarios.
///  - Chofer real (modelo 3D bípedo Meshy con ChoferAnimController) + Alebrije Tonalli guía.
///  - 3 Cirios de Santa Paula protegidos por emboscadas de Momias de 1833, Catrines y Cuervos.
///  - Capilla de Don Chuy, el mercader de ánimas.
///  - Cumbre rocosa con el Gran Mausoleo del General Momia (combate con sable) y Portal de salida.
///  - HUD Mexicano, códice de historia (TAB), cinemáticas narrativas y secuencia de despertar.
/// </summary>
public static class Nivel2PanteonSetup
{
    const string RutaEscena = "Assets/Scenes/Nivel2_Panteon.unity";
    const string RutaTerrenoData = "Assets/Scenes/Nivel2_Panteon_TerrainData.asset";
    const string CarpetaPrefabs = "Assets/Game/Prefabs/Sierra";
    const string CarpetaMats = "Assets/Game/Materials/Sierra";
    const string CarpetaMeshes = "Assets/Game/Meshes/Sierra";
    const string RutaModeloChofer = "Assets/Game/Models/Chofer/Meshy_AI_chofer_trailer_3d_biped_Character_output.fbx";
    const string RutaAnimController = "Assets/Game/Animations/Chofer/ChoferAnimV2.controller";

    public const float Tam = 640f;
    public const float Alto = 110f;
    public const int Res = 1025;

    public static readonly Vector2[] Sendero =
    {
        new Vector2(110, 110), // Plaza 0: Entrada y Despertar (0 m)
        new Vector2(155, 130),
        new Vector2(210, 120),
        new Vector2(260, 145), // Mirador de las Cruces Altas (~165 m)
        new Vector2(305, 130),
        new Vector2(360, 155),
        new Vector2(405, 140),
        new Vector2(455, 165),
        new Vector2(490, 210), // Plaza 1: Primer Cirio y Gavetas (~430 m)
        new Vector2(510, 260),
        new Vector2(485, 305),
        new Vector2(435, 325),
        new Vector2(380, 310), // Desfiladero de las Criptas Antiguas (~650 m)
        new Vector2(325, 330),
        new Vector2(270, 315),
        new Vector2(225, 340), // Plaza 2: Segundo Cirio y Catrines (~815 m)
        new Vector2(180, 370),
        new Vector2(145, 415),
        new Vector2(165, 460),
        new Vector2(220, 475),
        new Vector2(275, 460), // Plaza 3: Tercer Cirio y Capilla Don Chuy (~1090 m)
        new Vector2(325, 480),
        new Vector2(380, 495), // Vista de la Cumbre del Mausoleo (~1200 m)
        new Vector2(435, 475),
        new Vector2(480, 500)  // Plaza 4: Gran Mausoleo del General Momia y Portal (~1310 m)
    };

    public static readonly BosqueTerrenoBuilder.Plaza[] Plazas =
    {
        new BosqueTerrenoBuilder.Plaza("Inicio", new Vector2(110, 110), 14f),
        new BosqueTerrenoBuilder.Plaza("Cirio1", new Vector2(490, 210), 13f),
        new BosqueTerrenoBuilder.Plaza("Cirio2", new Vector2(225, 340), 13f),
        new BosqueTerrenoBuilder.Plaza("Cirio3", new Vector2(275, 460), 14f),
        new BosqueTerrenoBuilder.Plaza("GranMausoleo", new Vector2(480, 500), 18f)
    };

    static Shader shaderLit;

    [MenuItem("EvitandoElFin/Nivel 2/7. Crear Nivel 2 (Panteón de Santa Paula - Sierra)")]
    public static void Crear()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        try
        {
            shaderLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            var escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            ConfigurarAmbiente();
            Terrain terreno = ConstruirTerrenoPanteon();
            SierraProps.Crear(terreno);
            PoblarPanteonArquitectura(terreno);
            CrearLimites();

            EditorUtility.DisplayProgressBar("Nivel 2 (Panteón de Santa Paula)", "Colocando Chofer, Tonalli y Cirios...", 0.75f);
            GameObject chofer = CrearChofer(terreno);
            ConfigurarCamara(chofer);
            CrearAlebrijeTonalli(chofer);

            new GameObject("HUD_Mexicano").AddComponent<HUDMexicano>();
            LevelUIBuilder.MontarManagersYPaneles(false);
            AjustarTextosPaneles();

            CrearCiriosYZonas(terreno);
            CrearPortalMausoleo(terreno);
            CrearDirector(terreno);
            CrearLuciernagasFatuo(terreno);
            CrearPostProceso();
            ConfigurarNarrativaYJefe(terreno);

            new GameObject("AmbienteSonoro").AddComponent<AmbienteSonoroBosque>();

            EditorSceneManager.SaveScene(escena, RutaEscena);
            ActualizarBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        Debug.Log("<color=#00FFAA>[Nivel2_Panteon] Escena creada exitosamente: " + RutaEscena + "</color>");
        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog("Evitando El Fin",
                "Escena 'Nivel2_Panteon' (Panteón de Santa Paula en la Sierra) creada con éxito.\n\n" +
                "• Sierra de Guanajuato de noche: mapa colosal de 640 m con sendero de ~1300 m.\n" +
                "• Chofer real con combo, onda espiritual, esquive y animaciones del Nivel 1.\n" +
                "• Gavetas de cantera rosa en los riscos, mausoleos virreinales, cipreses y veladoras.\n" +
                "• 3 Cirios de Santa Paula con fuego fatuo protegidos por Momias y Catrines.\n" +
                "• Don Chuy con su capilla de ánimas vendiendo mejoras.\n" +
                "• Gran Mausoleo en la cumbre rocosa con el General Momia y su sable.\n" +
                "• Cinemáticas narrativas sincronizadas con cámara y Códice (TAB).", "Listo");
        }
    }

    // ================================================================== AMBIENTE
    static void ConfigurarAmbiente()
    {
        Material cielo = EscenaCarreteraSetup.CargarCieloNoche();
        if (cielo != null) RenderSettings.skybox = cielo;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.08f, 0.16f, 0.14f); // Tono verdoso espectral
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.012f;
        RenderSettings.fogColor = new Color(0.05f, 0.12f, 0.10f);

        Light sol = Object.FindObjectsByType<Light>().FirstOrDefault(l => l.type == LightType.Directional);
        if (sol != null)
        {
            sol.name = "Luz_Luna_Panteon";
            sol.color = new Color(0.42f, 0.75f, 0.65f);
            sol.intensity = 0.65f;
            sol.transform.rotation = Quaternion.Euler(48f, -42f, 0f);
            sol.shadows = LightShadows.Soft;
        }

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = cielo != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.08f, 0.06f);
            cam.farClipPlane = 600f;
            cam.nearClipPlane = 0.1f;
            var datos = cam.GetUniversalAdditionalCameraData();
            if (datos != null) datos.renderPostProcessing = true;
        }
        DynamicGI.UpdateEnvironment();
    }

    // ================================================================== TERRENO DE SIERRA
    static Terrain ConstruirTerrenoPanteon()
    {
        EditorUtility.DisplayProgressBar("Nivel 2 (Panteón de Santa Paula)", "Esculpiendo el collado rocoso de la Sierra...", 0.2f);

        TerrainData td = AssetDatabase.LoadAssetAtPath<TerrainData>(RutaTerrenoData);
        if (td == null)
        {
            td = new TerrainData();
            td.heightmapResolution = Res;
            td.size = new Vector3(Tam, Alto, Tam);
            AssetDatabase.CreateAsset(td, RutaTerrenoData);
        }
        else
        {
            td.heightmapResolution = Res;
            td.size = new Vector3(Tam, Alto, Tam);
        }

        float[,] h = new float[Res, Res];
        float cell = Tam / (Res - 1);

        for (int z = 0; z < Res; z++)
        {
            for (int x = 0; x < Res; x++)
            {
                float wx = x * cell;
                float wz = z * cell;

                // Base montañosa con Perlin
                float perlin = Mathf.PerlinNoise(wx * 0.007f + 20f, wz * 0.007f + 20f);
                float altura = 8f + perlin * 28f;

                // Sendero y plazas alisadas
                float dSendero = DistanciaAlSendero(wx, wz);
                float wSendero = 1f - Mathf.SmoothStep(0f, 1f, (dSendero - 4.5f) / 14f);
                if (wSendero > 0f)
                {
                    float hSendero = 6f + Mathf.Clamp01(wz / Tam) * 22f;
                    altura = Mathf.Lerp(altura, hSendero, wSendero);
                }

                // Bordes montañosos que encierran el valle
                float u = Mathf.Max(Mathf.Abs(wx - Tam * 0.5f), Mathf.Abs(wz - Tam * 0.5f)) / (Tam * 0.5f);
                if (u > 0.68f)
                {
                    float factorBorde = Mathf.SmoothStep(0f, 1f, (u - 0.68f) / 0.32f);
                    altura += factorBorde * (45f + Mathf.PerlinNoise(wx * 0.03f, wz * 0.03f) * 25f);
                }

                h[z, x] = Mathf.Clamp01(altura / Alto);
            }
        }
        td.SetHeights(0, 0, h);

        // Capas URP
        AsignarCapasTerreno(td);

        GameObject go = Terrain.CreateTerrainGameObject(td);
        go.name = "Terreno_Sierra_SantaPaula";
        go.transform.position = new Vector3(-Tam * 0.5f, 0f, -Tam * 0.5f);
        Terrain t = go.GetComponent<Terrain>();
        t.drawInstanced = true;

        // Vegetación
        var flora = SierraFlora.CrearTodo(CarpetaPrefabs, CarpetaMeshes, CarpetaMats);
        BosqueTerrenoBuilder.Replantar(RutaTerrenoData, CarpetaPrefabs, CarpetaMats);

        return t;
    }

    static float DistanciaAlSendero(float x, float z)
    {
        Vector2 p = new Vector2(x, z);
        float minD = float.MaxValue;
        for (int i = 0; i < Sendero.Length - 1; i++)
        {
            Vector2 a = Sendero[i], b = Sendero[i + 1];
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            float d = Vector2.Distance(p, a + ab * t);
            if (d < minD) minD = d;
        }
        return minD;
    }

    static void AsignarCapasTerreno(TerrainData td)
    {
        var capas = new List<TerrainLayer>();
        string[] nombres = { "Rock_TerrainLayer", "Soil_Rocks_TerrainLayer", "Pebbles_A_TerrainLayer", "Grass_Dry_TerrainLayer" };
        foreach (string nom in nombres)
        {
            foreach (string guid in AssetDatabase.FindAssets(nom + " t:TerrainLayer"))
            {
                string r = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(r) == nom)
                {
                    TerrainLayer l = AssetDatabase.LoadAssetAtPath<TerrainLayer>(r);
                    if (l != null && !capas.Contains(l)) capas.Add(l);
                    break;
                }
            }
        }
        if (capas.Count > 0) td.terrainLayers = capas.ToArray();
    }

    // ================================================================== ARQUITECTURA PANTEÓN
    static void PoblarPanteonArquitectura(Terrain terreno)
    {
        GameObject raiz = new GameObject("Arquitectura_Panteon_SantaPaula");
        Material cantera = Mat(new Color(0.76f, 0.54f, 0.48f)); // Cantera rosa
        Material piedraOscura = Mat(new Color(0.28f, 0.28f, 0.32f));
        Material herreria = Mat(new Color(0.12f, 0.12f, 0.14f));
        Material fuegoFatuo = MatEmisivo(new Color(0.15f, 0.95f, 0.45f), new Color(0.2f, 2.2f, 0.8f));
        Material cempasuchil = MatEmisivo(new Color(1f, 0.65f, 0.1f), new Color(1.4f, 0.8f, 0.1f));

        for (int i = 1; i < Sendero.Length - 2; i += 2)
        {
            Vector3 posCamino = LocalAMundo(terreno, Sendero[i]);
            Vector2 dir2D = (Sendero[i + 1] - Sendero[i]).normalized;
            Vector3 dir = new Vector3(dir2D.x, 0, dir2D.y);
            Vector3 der = Vector3.Cross(Vector3.up, dir).normalized;

            // Gavetas en las peñas laterales
            Vector3 posGavI = posCamino - der * 16f; posGavI.y = AlturaMundo(terreno, posGavI);
            Vector3 posGavD = posCamino + der * 16f; posGavD.y = AlturaMundo(terreno, posGavD);
            CrearMuroGavetas(raiz.transform, posGavI, dir, cantera, piedraOscura);
            CrearMuroGavetas(raiz.transform, posGavD, -dir, cantera, piedraOscura);

            // Mausoleos familiares
            if (i % 4 == 1)
            {
                Vector3 posMau = posCamino + der * 10f; posMau.y = AlturaMundo(terreno, posMau);
                CrearMausoleo(raiz.transform, posMau, cantera, piedraOscura);
            }

            // Tumbas con veladoras y cempasúchil a la orilla del sendero
            Vector3 posTumba = posCamino - der * 5f; posTumba.y = AlturaMundo(terreno, posTumba);
            CrearTumba(raiz.transform, posTumba, cantera, piedraOscura, cempasuchil);

            // Farol con fuego fatuo esmeralda
            Vector3 posFarol = posCamino + der * 4.5f; posFarol.y = AlturaMundo(terreno, posFarol);
            CrearFarol(raiz.transform, posFarol, herreria, fuegoFatuo);
        }
    }

    static void CrearMuroGavetas(Transform padre, Vector3 pos, Vector3 forward, Material cantera, Material oscuro)
    {
        GameObject muro = GameObject.CreatePrimitive(PrimitiveType.Cube);
        muro.name = "Gavetas_Pena";
        muro.transform.SetParent(padre, false);
        muro.transform.position = pos + Vector3.up * 3f;
        muro.transform.rotation = Quaternion.LookRotation(forward);
        muro.transform.localScale = new Vector3(3f, 6f, 12f);
        muro.GetComponent<Renderer>().sharedMaterial = cantera;
    }

    static void CrearMausoleo(Transform padre, Vector3 pos, Material cantera, Material oscuro)
    {
        GameObject m = new GameObject("Mausoleo_Familiar");
        m.transform.SetParent(padre, false);
        m.transform.position = pos;

        GameObject cuerpo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cuerpo.transform.SetParent(m.transform, false);
        cuerpo.transform.localPosition = new Vector3(0, 2.2f, 0);
        cuerpo.transform.localScale = new Vector3(5f, 4.4f, 5f);
        cuerpo.GetComponent<Renderer>().sharedMaterial = cantera;

        GameObject cúpula = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cúpula.transform.SetParent(m.transform, false);
        cúpula.transform.localPosition = new Vector3(0, 4.8f, 0);
        cúpula.transform.localScale = new Vector3(5.4f, 0.8f, 5.4f);
        cúpula.GetComponent<Renderer>().sharedMaterial = oscuro;
    }

    static void CrearTumba(Transform padre, Vector3 pos, Material cantera, Material oscuro, Material cempasuchil)
    {
        GameObject t = new GameObject("Tumba_SantaPaula");
        t.transform.SetParent(padre, false);
        t.transform.position = pos;

        GameObject losa = GameObject.CreatePrimitive(PrimitiveType.Cube);
        losa.transform.SetParent(t.transform, false);
        losa.transform.localPosition = new Vector3(0, 0.2f, 0);
        losa.transform.localScale = new Vector3(1.3f, 0.4f, 2.4f);
        losa.GetComponent<Renderer>().sharedMaterial = oscuro;

        GameObject lapida = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lapida.transform.SetParent(t.transform, false);
        lapida.transform.localPosition = new Vector3(0, 0.85f, 1.1f);
        lapida.transform.localScale = new Vector3(1.1f, 1.3f, 0.28f);
        lapida.GetComponent<Renderer>().sharedMaterial = cantera;

        GameObject vela = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        vela.transform.SetParent(t.transform, false);
        vela.transform.localPosition = new Vector3(0.35f, 0.45f, 0.5f);
        vela.transform.localScale = new Vector3(0.14f, 0.18f, 0.14f);
        vela.GetComponent<Renderer>().sharedMaterial = cempasuchil;
        Object.DestroyImmediate(vela.GetComponent<Collider>());
    }

    static void CrearFarol(Transform padre, Vector3 pos, Material herreria, Material fuegoFatuo)
    {
        GameObject farol = new GameObject("Farol_FuegoFatuo");
        farol.transform.SetParent(padre, false);
        farol.transform.position = pos;

        GameObject poste = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        poste.transform.SetParent(farol.transform, false);
        poste.transform.localPosition = new Vector3(0, 2f, 0);
        poste.transform.localScale = new Vector3(0.16f, 2f, 0.16f);
        poste.GetComponent<Renderer>().sharedMaterial = herreria;

        GameObject cabina = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cabina.transform.SetParent(farol.transform, false);
        cabina.transform.localPosition = new Vector3(0, 4.1f, 0);
        cabina.transform.localScale = new Vector3(0.55f, 0.7f, 0.55f);
        cabina.GetComponent<Renderer>().sharedMaterial = fuegoFatuo;
        Object.DestroyImmediate(cabina.GetComponent<Collider>());

        Light l = farol.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(0.2f, 0.95f, 0.5f);
        l.range = 11f;
        l.intensity = 2.4f;
    }

    // ================================================================== CHOFER REAL Y TONALLI
    static GameObject CrearChofer(Terrain terreno)
    {
        Vector3 pos = LocalAMundo(terreno, Sendero[0]);
        pos.y = AlturaMundo(terreno, pos) + 1.1f;
        Vector2 dir = (Sendero[1] - Sendero[0]).normalized;

        GameObject root = new GameObject("Chofer");
        root.tag = "Player";
        root.transform.position = pos;
        root.transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.y));

        CharacterController cc = root.AddComponent<CharacterController>();
        cc.center = Vector3.zero;
        cc.height = 2f;
        cc.radius = 0.5f;

        GameObject modeloPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaModeloChofer);
        GameObject modelo;
        if (modeloPrefab != null)
        {
            modelo = (GameObject)PrefabUtility.InstantiatePrefab(modeloPrefab);
            modelo.transform.SetParent(root.transform, false);
        }
        else
        {
            modelo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Object.DestroyImmediate(modelo.GetComponent<Collider>());
            modelo.transform.SetParent(root.transform, false);
        }
        modelo.name = "ChoferModelo";
        modelo.transform.localPosition = new Vector3(0f, -1f, 0f);
        modelo.transform.localRotation = Quaternion.identity;

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

        ChoferController cch = root.AddComponent<ChoferController>();
        cch.animator = anim;

        root.AddComponent<PoderEspiritual>();
        root.AddComponent<InventarioEspiritual>();
        root.AddComponent<SistemaCombateChofer>();

        return root;
    }

    static void ConfigurarCamara(GameObject chofer)
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        cam.transform.position = chofer.transform.position + new Vector3(0f, 2.5f, -5f);
        cam.transform.rotation = Quaternion.Euler(15f, 0f, 0f);
        CamaraTerceraPersona c = cam.GetComponent<CamaraTerceraPersona>();
        if (c == null) c = cam.gameObject.AddComponent<CamaraTerceraPersona>();
        c.objetivo = chofer.transform;
    }

    static void CrearAlebrijeTonalli(GameObject chofer)
    {
        GameObject root = new GameObject("Criatura_Alebrije");
        root.transform.position = chofer.transform.position + new Vector3(1.2f, 1.8f, -0.8f);

        Material cuerpo = MatEmisivo(new Color(1f, 0.45f, 0.1f), new Color(1.6f, 0.5f, 0.1f));
        Material cabeza = MatEmisivo(new Color(0.95f, 0.2f, 0.6f), new Color(1.4f, 0.2f, 0.7f));
        Material ala = MatEmisivo(new Color(0.2f, 0.9f, 1f), new Color(0.2f, 1.4f, 1.8f));
        Material negro = Mat(new Color(0.04f, 0.04f, 0.05f));

        Parte(root.transform, "Cuerpo", PrimitiveType.Sphere, Vector3.zero, new Vector3(0.45f, 0.38f, 0.6f), cuerpo);
        GameObject cab = Parte(root.transform, "Cabeza", PrimitiveType.Sphere, new Vector3(0f, 0.12f, 0.38f), Vector3.one * 0.3f, cabeza);
        Parte(cab.transform, "OjoI", PrimitiveType.Sphere, new Vector3(-0.28f, 0.15f, 0.4f), Vector3.one * 0.22f, negro);
        Parte(cab.transform, "OjoD", PrimitiveType.Sphere, new Vector3(0.28f, 0.15f, 0.4f), Vector3.one * 0.22f, negro);

        GameObject alaI = Parte(root.transform, "AlaI", PrimitiveType.Sphere, new Vector3(-0.38f, 0.14f, 0f), new Vector3(0.6f, 0.04f, 0.34f), ala);
        alaI.transform.localRotation = Quaternion.Euler(0f, 0f, 24f);
        GameObject alaD = Parte(root.transform, "AlaD", PrimitiveType.Sphere, new Vector3(0.38f, 0.14f, 0f), new Vector3(0.6f, 0.04f, 0.34f), ala);
        alaD.transform.localRotation = Quaternion.Euler(0f, 0f, -24f);

        GameObject luzGO = new GameObject("LuzEspiritual");
        luzGO.transform.SetParent(root.transform, false);
        Light luz = luzGO.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = new Color(0.2f, 0.9f, 1f);
        luz.range = 9f;
        luz.intensity = 2.2f;

        root.transform.localScale = Vector3.one * 0.85f;
        CriaturaGuia guia = root.AddComponent<CriaturaGuia>();
        guia.objetivoChofer = chofer.transform;
        guia.luzEspiritual = luz;
    }

    // ================================================================== CIRIOS Y EMBOSCADAS
    static void CrearCiriosYZonas(Terrain terreno)
    {
        GameObject padre = new GameObject("Cirios_Y_Emboscadas");

        int[] momias = { 4, 5, 4 };
        int[] catrines = { 0, 1, 2 };

        for (int i = 0; i < 3; i++)
        {
            BosqueTerrenoBuilder.Plaza pl = Plazas[i + 1];
            Vector3 pos = LocalAMundo(terreno, pl.centro);

            GameObject cirio = CrearCirio("Totem_Cirio_" + (i + 1), pos);
            cirio.transform.SetParent(padre.transform, true);

            GameObject zona = new GameObject("Emboscada_Cirio_" + (i + 1));
            zona.transform.SetParent(padre.transform, true);
            zona.transform.position = pos;
            ZonaEmboscada z = zona.AddComponent<ZonaEmboscada>();
            z.radioActivacion = 22f;
            z.serpientes = momias[i]; // Las momias usan la ranura melé
            z.hombresPiedra = catrines[i]; // Los catrines usan la ranura pesada/distancia
            z.radioMinSpawn = 9f;
            z.radioMaxSpawn = pl.radio + 2f;
        }
    }

    static GameObject CrearCirio(string nombre, Vector3 posSuelo)
    {
        GameObject root = new GameObject(nombre);
        root.transform.position = posSuelo;

        Material cantera = Mat(new Color(0.76f, 0.54f, 0.48f));
        Material cera = Mat(new Color(0.92f, 0.90f, 0.82f));
        Material llama = MatEmisivo(new Color(0.15f, 0.95f, 0.45f), new Color(0.2f, 2.5f, 0.8f));

        Parte(root.transform, "PedestalCantera", PrimitiveType.Cylinder, new Vector3(0, 0.8f, 0), new Vector3(1.2f, 0.8f, 1.2f), cantera, true);
        Parte(root.transform, "CuerpoCirio", PrimitiveType.Cylinder, new Vector3(0, 2.4f, 0), new Vector3(0.55f, 1.6f, 0.55f), cera, true);

        GameObject cris = new GameObject("FlamaEspiritual");
        cris.transform.SetParent(root.transform, false);
        cris.transform.localPosition = new Vector3(0, 4.2f, 0);
        Parte(cris.transform, "GemaFuego", PrimitiveType.Sphere, Vector3.zero, new Vector3(0.5f, 0.8f, 0.5f), llama, false);

        SphereCollider sc = cris.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 1.8f;
        TotemColeccionable tc = cris.AddComponent<TotemColeccionable>();
        tc.valorTotem = 1;
        tc.curacionEspiritual = 35f;

        Light l = cris.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(0.2f, 1f, 0.5f);
        l.range = 14f;
        l.intensity = 3.5f;

        return root;
    }

    static void CrearPortalMausoleo(Terrain terreno)
    {
        BosqueTerrenoBuilder.Plaza pl = Plazas[4];
        Vector3 pos = LocalAMundo(terreno, pl.centro);

        GameObject root = new GameObject("Portal_Victoria_Nivel2");
        root.transform.position = pos + Vector3.forward * 14f;

        Material cantera = Mat(new Color(0.76f, 0.54f, 0.48f));
        Material vortice = MatEmisivo(new Color(0.15f, 0.95f, 0.45f), new Color(0.3f, 2.4f, 1.2f));

        Parte(root.transform, "PilarI", PrimitiveType.Cube, new Vector3(-3.4f, 3f, 0), new Vector3(1.4f, 6f, 1.4f), cantera, true);
        Parte(root.transform, "PilarD", PrimitiveType.Cube, new Vector3(3.4f, 3f, 0), new Vector3(1.4f, 6f, 1.4f), cantera, true);
        Parte(root.transform, "Dintel", PrimitiveType.Cube, new Vector3(0, 6.6f, 0), new Vector3(8.8f, 1.2f, 1.6f), cantera, true);
        GameObject vor = Parte(root.transform, "VorticeCentral", PrimitiveType.Sphere, new Vector3(0, 3f, 0), new Vector3(5.2f, 5.4f, 0.4f), vortice, false);

        BoxCollider bc = root.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size = new Vector3(5.4f, 5.6f, 2.6f);

        PortalVictoria pv = root.AddComponent<PortalVictoria>();
        pv.efectosPortalActivo = vor;
    }

    // ================================================================== NARRATIVA Y JEFE GENERAL MOMIA
    static void ConfigurarNarrativaYJefe(Terrain terreno)
    {
        BosqueTerrenoBuilder.Plaza plArena = Plazas[4];
        Vector3 centroArena = LocalAMundo(terreno, plArena.centro);

        // Gran Mausoleo en la cumbre rocosa
        GameObject mausoleo = new GameObject("ArenaMausoleoGeneral");
        mausoleo.transform.position = centroArena;
        Material cantera = Mat(new Color(0.76f, 0.54f, 0.48f));

        GameObject plataforma = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        plataforma.transform.SetParent(mausoleo.transform, false);
        plataforma.transform.localPosition = new Vector3(0, -0.2f, 0);
        plataforma.transform.localScale = new Vector3(32f, 0.5f, 32f);
        plataforma.GetComponent<Renderer>().sharedMaterial = cantera;

        for (int i = 0; i < 8; i++)
        {
            float ang = i * 45f * Mathf.Deg2Rad;
            GameObject col = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            col.transform.SetParent(mausoleo.transform, false);
            col.transform.localPosition = new Vector3(Mathf.Sin(ang) * 14.5f, 3.5f, Mathf.Cos(ang) * 14.5f);
            col.transform.localScale = new Vector3(1.4f, 3.5f, 1.4f);
            col.GetComponent<Renderer>().sharedMaterial = cantera;
        }

        // Sepulcro del General
        GameObject cripta = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cripta.name = "SepulcroGeneral";
        cripta.transform.SetParent(mausoleo.transform, false);
        cripta.transform.localPosition = new Vector3(0, 1f, 0);
        cripta.transform.localScale = new Vector3(4.5f, 2f, 6.5f);
        cripta.GetComponent<Renderer>().sharedMaterial = cantera;

        // Jefe General Momia
        GameObject bossGo = new GameObject("Jefe_GeneralMomia");
        bossGo.transform.position = centroArena + Vector3.up * 0.2f;
        bossGo.AddComponent<JefeGeneralMomia>();

        // Capilla Don Chuy
        Vector3 posChuy = LocalAMundo(terreno, Sendero[20]);
        GameObject chuyGo = new GameObject("Comerciante_DonChuy");
        chuyGo.transform.position = posChuy;
        chuyGo.AddComponent<ComercianteMercader>();

        // Hitos Narrativos
        GameObject hitos = new GameObject("Hitos_Narrativos");
        CrearHito(hitos.transform, terreno, "intro_panteon", Sendero[1], 16f);
        CrearHito(hitos.transform, terreno, "collado_sierra", Sendero[3], 18f);
        CrearHito(hitos.transform, terreno, "momias", Plazas[1].centro, 32f);
        CrearHito(hitos.transform, terreno, "criptas_antiguas", Sendero[12], 18f);
        CrearHito(hitos.transform, terreno, "catrines", Plazas[2].centro, 32f);
        CrearHito(hitos.transform, terreno, "cuervos", Sendero[18], 18f);
        CrearHito(hitos.transform, terreno, "cripta", Plazas[4].centro, 28f);

        // Sendero guardado
        SenderoNivel sn = new GameObject("Sendero_Nivel2").AddComponent<SenderoNivel>();
        var pts = new List<Vector3>();
        foreach (var s in Sendero) pts.Add(LocalAMundo(terreno, s));
        sn.puntos = pts.ToArray();

        // Cinemáticas y Guía
        if (Object.FindAnyObjectByType<GuiaAlebrije>() == null) new GameObject("Guia_Alebrije").AddComponent<GuiaAlebrije>();
        if (Object.FindAnyObjectByType<CinematicaNarrativa>() == null) new GameObject("Cinematicas_Narrativas").AddComponent<CinematicaNarrativa>();
        if (Object.FindAnyObjectByType<CodiceHistoria>() == null) new GameObject("Codice_Historia").AddComponent<CodiceHistoria>();
    }

    static void CrearHito(Transform padre, Terrain t, string id, Vector2 local, float radio)
    {
        GameObject h = new GameObject("Hito_" + id);
        h.transform.SetParent(padre, false);
        h.transform.position = LocalAMundo(t, local);
        HitoNarrativo hn = h.AddComponent<HitoNarrativo>();
        hn.idHistoria = id;
        hn.radio = radio;
    }

    // ================================================================== DIRECTOR, LUCIÉRNAGAS Y LÍMITES
    static void CrearDirector(Terrain terreno)
    {
        GameObject dir = new GameObject("Director_Nivel2");
        dir.AddComponent<DirectorNivelPanteon>();

        for (int i = 0; i < Sendero.Length; i += 2)
        {
            Vector3 p = LocalAMundo(terreno, Sendero[i]);
            GameObject alma = new GameObject("SpawnAlma_" + i);
            alma.transform.SetParent(dir.transform, false);
            alma.transform.position = p + Vector3.up * 1f;
        }
    }

    static void CrearLuciernagasFatuo(Terrain terreno)
    {
        GameObject padre = new GameObject("Luciernagas_FuegoFatuo");
        foreach (var pl in Plazas)
        {
            Vector3 pos = LocalAMundo(terreno, pl.centro) + Vector3.up * 2f;
            GameObject go = new GameObject("NubeFatuo");
            go.transform.SetParent(padre.transform, false);
            go.transform.position = pos;
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startColor = new Color(0.2f, 1f, 0.6f, 0.8f);
            main.startSize = 0.35f;
            main.maxParticles = 30;
        }
    }

    static void CrearLimites()
    {
        GameObject limites = new GameObject("Limites_Sierra");
        const float tam = Tam;
        const float alto = 80f;
        CrearMuroColision(limites.transform, new Vector3(0, alto * 0.5f, tam * 0.5f), new Vector3(tam, alto, 10f));
        CrearMuroColision(limites.transform, new Vector3(0, alto * 0.5f, -tam * 0.5f), new Vector3(tam, alto, 10f));
        CrearMuroColision(limites.transform, new Vector3(tam * 0.5f, alto * 0.5f, 0), new Vector3(10f, alto, tam));
        CrearMuroColision(limites.transform, new Vector3(-tam * 0.5f, alto * 0.5f, 0), new Vector3(10f, alto, tam));
    }

    static void CrearMuroColision(Transform padre, Vector3 pos, Vector3 tam)
    {
        GameObject g = new GameObject("MuroLimite");
        g.transform.SetParent(padre, false);
        g.transform.position = pos;
        BoxCollider bc = g.AddComponent<BoxCollider>();
        bc.size = tam;
    }

    static void CrearPostProceso()
    {
        string ruta = CarpetaMats + "/PerfilPanteonNocturno.asset";
        VolumeProfile perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ruta);
        if (perfil == null)
        {
            perfil = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(perfil, ruta);
            var bloom = perfil.Add<Bloom>(true);
            bloom.threshold.value = 0.85f;
            bloom.intensity.value = 1.4f;
        }

        GameObject go = new GameObject("PostProceso_Panteon");
        Volume v = go.AddComponent<Volume>();
        v.isGlobal = true;
        v.profile = perfil;
    }

    static void AjustarTextosPaneles()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return;
        Transform tGameOver = canvas.transform.Find("PanelGameOver/SubGameOver");
        if (tGameOver != null)
        {
            var tmp = tGameOver.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.text = "Las momias y el frío de la sierra te vencieron. Inténtalo de nuevo.";
        }
        Transform tVictoria = canvas.transform.Find("PanelVictoria/SubVictoria");
        if (tVictoria != null)
        {
            var tmp = tVictoria.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.text = "Rendiste al General Momia y recuperaste el Rosario de Plata.";
        }
    }

    // ================================================================== UTILIDADES
    static Vector3 LocalAMundo(Terrain t, Vector2 local)
    {
        Vector3 p = new Vector3(local.x - Tam * 0.5f, 0f, local.y - Tam * 0.5f);
        p.y = AlturaMundo(t, p);
        return p;
    }

    static float AlturaMundo(Terrain t, Vector3 mundo)
    {
        return t != null ? t.SampleHeight(mundo) + t.transform.position.y : 0f;
    }

    static Material Mat(Color c)
    {
        Material m = new Material(shaderLit);
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        return m;
    }

    static Material MatEmisivo(Color c, Color emision)
    {
        Material m = Mat(c);
        m.EnableKeyword("_EMISSION");
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", emision);
        return m;
    }

    static GameObject Parte(Transform padre, string nombre, PrimitiveType tipo, Vector3 pos, Vector3 escala, Material mat, bool conCollider = false)
    {
        GameObject g = GameObject.CreatePrimitive(tipo);
        g.name = nombre;
        g.transform.SetParent(padre, false);
        g.transform.localPosition = pos;
        g.transform.localScale = escala;
        g.GetComponent<Renderer>().sharedMaterial = mat;
        if (!conCollider)
        {
            Collider c = g.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
        }
        return g;
    }

    static void ActualizarBuildSettings()
    {
        var lista = EditorBuildSettings.scenes.ToList();
        if (!lista.Any(s => s.path == RutaEscena)) lista.Add(new EditorBuildSettingsScene(RutaEscena, true));
        EditorBuildSettings.scenes = lista.ToArray();
    }
}
