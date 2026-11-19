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
/// Crea la escena "Nivel3_Ciudad" (Nivel 3 del juego):
///  - Mapa monumental de 640 m en la cañada profunda de la Sierra de Guanajuato de noche.
///  - Sendero de ~1300 m descendiendo por escalinatas adoquinadas, casas coloniales trepando por los riscos,
///    el legendario Callejón del Beso, túneles mineros de piedra y la monumental Basílica de Guanajuato.
///  - Chofer real (modelo 3D bípedo Meshy con ChoferAnimController) + Alebrije Tonalli guía.
///  - 3 Sellos Inquisitoriales protegidos por emboscadas de Monjes Penitentes, Sombras y Catrines.
///  - Puesto colonial de Don Chuy en una plazuela virreinal.
///  - Combate final en el Atrio de la Basílica contra el Clérigo Inquisidor y Portal final de retorno a la vida.
///  - HUD Mexicano, códice de historia (TAB), cinemáticas narrativas y secuencia de despertar.
/// </summary>
public static class Nivel3CiudadSetup
{
    const string RutaEscena = "Assets/Scenes/Nivel3_Ciudad.unity";
    const string RutaTerrenoData = "Assets/Scenes/Nivel3_Ciudad_TerrainData.asset";
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
        new Vector2(110, 110), // Plaza 0: Mirador Alto de la Sierra (0 m)
        new Vector2(160, 130),
        new Vector2(215, 120),
        new Vector2(265, 145), // Mirador del Pípila y tiro de mina (~165 m)
        new Vector2(310, 130),
        new Vector2(365, 155),
        new Vector2(410, 140),
        new Vector2(460, 165),
        new Vector2(495, 210), // Plaza 1: Primer Sello y Casas Virreinales (~430 m)
        new Vector2(515, 260),
        new Vector2(490, 305),
        new Vector2(440, 325),
        new Vector2(385, 310), // Callejón estrecho de cantera (~650 m)
        new Vector2(330, 330),
        new Vector2(275, 315),
        new Vector2(230, 340), // Plaza 2: El Callejón del Beso y Segundo Sello (~815 m)
        new Vector2(185, 370),
        new Vector2(150, 415),
        new Vector2(170, 460),
        new Vector2(225, 475),
        new Vector2(280, 460), // Plaza 3: Túneles Mineros, Don Chuy y Tercer Sello (~1090 m)
        new Vector2(330, 480),
        new Vector2(385, 495), // Vista de las Torres de la Basílica (~1200 m)
        new Vector2(440, 475),
        new Vector2(480, 500)  // Plaza 4: El Gran Atrio de la Basílica y Clérigo Inquisidor (~1310 m)
    };

    public static readonly BosqueTerrenoBuilder.Plaza[] Plazas =
    {
        new BosqueTerrenoBuilder.Plaza("Inicio", new Vector2(110, 110), 14f),
        new BosqueTerrenoBuilder.Plaza("Sello1", new Vector2(495, 210), 13f),
        new BosqueTerrenoBuilder.Plaza("CallejonBeso", new Vector2(230, 340), 13f),
        new BosqueTerrenoBuilder.Plaza("TunelesMinas", new Vector2(280, 460), 14f),
        new BosqueTerrenoBuilder.Plaza("AtrioBasilica", new Vector2(480, 500), 20f)
    };

    static Shader shaderLit;

    [MenuItem("EvitandoElFin/Nivel 3/8. Crear Nivel 3 (Cañada Colonial y Basílica - Sierra)")]
    public static void Crear()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        try
        {
            shaderLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            var escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            ConfigurarAmbiente();
            Terrain terreno = ConstruirTerrenoCiudad();
            SierraProps.Crear(terreno);
            PoblarGuanajuatoColonial(terreno);
            CrearLimites();

            EditorUtility.DisplayProgressBar("Nivel 3 (Cañada Colonial y Basílica)", "Colocando Chofer, Tonalli y Sellos...", 0.75f);
            GameObject chofer = CrearChofer(terreno);
            ConfigurarCamara(chofer);
            CrearAlebrijeTonalli(chofer);

            new GameObject("HUD_Mexicano").AddComponent<HUDMexicano>();
            LevelUIBuilder.MontarManagersYPaneles(false);
            AjustarTextosPaneles();

            CrearSellosYZonas(terreno);
            CrearPortalVictoriaFinal(terreno);
            CrearDirector(terreno);
            CrearLuciernagasPolvoOro(terreno);
            CrearPostProceso();
            ConfigurarNarrativaYBasílica(terreno);

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

        Debug.Log("<color=#00FFAA>[Nivel3_Ciudad] Escena creada exitosamente: " + RutaEscena + "</color>");
        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog("Evitando El Fin",
                "Escena 'Nivel3_Ciudad' (Cañada Colonial y Basílica) creada con éxito.\n\n" +
                "• Sierra de Guanajuato de noche: mapa colosal de 640 m con sendero de ~1300 m.\n" +
                "• Chofer real con combo, onda espiritual, esquive y animaciones del Nivel 1.\n" +
                "• Casas coloniales sobre los riscos, Callejón del Beso (70 cm de distancia), túneles y arcos.\n" +
                "• 3 Sellos Inquisitoriales protegidos por Monjes Penitentes y Sombras.\n" +
                "• Don Chuy con su puesto colonial en la plazuela.\n" +
                "• Monumental Basílica de Guanajuato (torres gemelas de 24 m) con el Clérigo Inquisidor.\n" +
                "• Portal dorado de regreso a la vida, cinemáticas y Códice (TAB).", "Listo");
        }
    }

    // ================================================================== AMBIENTE
    static void ConfigurarAmbiente()
    {
        Material cielo = EscenaCarreteraSetup.CargarCieloNoche();
        if (cielo != null) RenderSettings.skybox = cielo;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.18f, 0.12f, 0.15f); // Atmósfera colonial cálida
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.009f;
        RenderSettings.fogColor = new Color(0.12f, 0.08f, 0.10f);

        Light sol = Object.FindObjectsByType<Light>().FirstOrDefault(l => l.type == LightType.Directional);
        if (sol != null)
        {
            sol.name = "Luz_Luna_Colonial";
            sol.color = new Color(0.85f, 0.75f, 0.65f);
            sol.intensity = 0.75f;
            sol.transform.rotation = Quaternion.Euler(46f, -55f, 0f);
            sol.shadows = LightShadows.Soft;
        }

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = cielo != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.04f, 0.06f);
            cam.farClipPlane = 600f;
            cam.nearClipPlane = 0.1f;
            var datos = cam.GetUniversalAdditionalCameraData();
            if (datos != null) datos.renderPostProcessing = true;
        }
        DynamicGI.UpdateEnvironment();
    }

    // ================================================================== TERRENO DE SIERRA Y CAÑADA
    static Terrain ConstruirTerrenoCiudad()
    {
        EditorUtility.DisplayProgressBar("Nivel 3 (Cañada de Guanajuato)", "Esculpiendo el cañón de la Sierra...", 0.2f);

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

                // Descenso hacia el fondo del cañón
                float bajada = (1f - Mathf.Clamp01(wz / Tam)) * 32f + 5f;
                float perlin = Mathf.PerlinNoise(wx * 0.008f + 60f, wz * 0.008f + 60f) * 12f;
                float altura = bajada + perlin;

                // Sendero y calles adoquinadas niveladas
                float dSendero = DistanciaAlSendero(wx, wz);
                float wSendero = 1f - Mathf.SmoothStep(0f, 1f, (dSendero - 5.5f) / 16f);
                if (wSendero > 0f)
                {
                    float hCalle = (1f - Mathf.Clamp01(wz / Tam)) * 28f + 4f;
                    altura = Mathf.Lerp(altura, hCalle, wSendero);
                }

                // Riscos imponentes de la Sierra (Cerro de San Miguel y La Bufa)
                float u = Mathf.Max(Mathf.Abs(wx - Tam * 0.5f), Mathf.Abs(wz - Tam * 0.5f)) / (Tam * 0.5f);
                if (u > 0.65f)
                {
                    float factorBorde = Mathf.SmoothStep(0f, 1f, (u - 0.65f) / 0.35f);
                    altura += factorBorde * (50f + Mathf.PerlinNoise(wx * 0.035f, wz * 0.035f) * 28f);
                }

                h[z, x] = Mathf.Clamp01(altura / Alto);
            }
        }
        td.SetHeights(0, 0, h);

        AsignarCapasTerreno(td);

        GameObject go = Terrain.CreateTerrainGameObject(td);
        go.name = "Terreno_Sierra_CanadaGuanajuato";
        go.transform.position = new Vector3(-Tam * 0.5f, 0f, -Tam * 0.5f);
        Terrain t = go.GetComponent<Terrain>();
        t.drawInstanced = true;

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
        string[] nombres = { "Pebbles_A_TerrainLayer", "Rock_TerrainLayer", "Soil_Rocks_TerrainLayer", "Grass_Dry_TerrainLayer" };
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

    // ================================================================== ARQUITECTURA COLONIAL
    static void PoblarGuanajuatoColonial(Terrain terreno)
    {
        GameObject raiz = new GameObject("Arquitectura_Ciudad_Colonial");

        Color[] coloresFachadas =
        {
            new Color(0.85f, 0.35f, 0.22f), // Terracota
            new Color(0.92f, 0.72f, 0.22f), // Ocre mostaza
            new Color(0.18f, 0.42f, 0.68f), // Azul añil
            new Color(0.88f, 0.45f, 0.55f), // Rosa mexicano
            new Color(0.92f, 0.88f, 0.80f)  // Encalado
        };
        Material canteraRosa = Mat(new Color(0.82f, 0.62f, 0.55f));
        Material tejadoRojo = Mat(new Color(0.68f, 0.22f, 0.15f));
        Material herreria = Mat(new Color(0.1f, 0.1f, 0.12f));
        Material farolCalido = MatEmisivo(new Color(1f, 0.8f, 0.35f), new Color(2.5f, 1.8f, 0.5f));

        for (int i = 1; i < Sendero.Length - 2; i += 2)
        {
            Vector3 posCamino = LocalAMundo(terreno, Sendero[i]);
            Vector2 dir2D = (Sendero[i + 1] - Sendero[i]).normalized;
            Vector3 dir = new Vector3(dir2D.x, 0, dir2D.y);
            Vector3 der = Vector3.Cross(Vector3.up, dir).normalized;

            // Casas coloniales flanqueando la calle
            Material matCasaI = Mat(coloresFachadas[i % coloresFachadas.Length]);
            Material matCasaD = Mat(coloresFachadas[(i + 2) % coloresFachadas.Length]);

            Vector3 posCasaI = posCamino - der * 13f; posCasaI.y = AlturaMundo(terreno, posCasaI);
            Vector3 posCasaD = posCamino + der * 13f; posCasaD.y = AlturaMundo(terreno, posCasaD);
            CrearCasaColonial(raiz.transform, posCasaI, dir, matCasaI, tejadoRojo, herreria);
            CrearCasaColonial(raiz.transform, posCasaD, -dir, matCasaD, tejadoRojo, herreria);

            // Farol colonial en la esquina
            Vector3 posFarol = posCamino + der * 4.5f; posFarol.y = AlturaMundo(terreno, posFarol);
            CrearFarolColonial(raiz.transform, posFarol, herreria, farolCalido);

            // Arcos de túnel minero en secciones clave
            if (i == 7 || i == 15)
            {
                CrearArcoCantera(raiz.transform, posCamino, dir, canteraRosa);
            }
        }

        // Construir el Callejón del Beso en la Plaza 2
        CrearCallejonDelBesoIconico(raiz.transform, terreno, Plazas[2].centro);
    }

    static void CrearCasaColonial(Transform padre, Vector3 pos, Vector3 forward, Material fachada, Material teja, Material herreria)
    {
        GameObject casa = new GameObject("Casa_Colonial");
        casa.transform.SetParent(padre, false);
        casa.transform.position = pos;
        casa.transform.rotation = Quaternion.LookRotation(forward);

        GameObject cuerpo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cuerpo.transform.SetParent(casa.transform, false);
        cuerpo.transform.localPosition = new Vector3(0, 3.5f, 0);
        cuerpo.transform.localScale = new Vector3(6.5f, 7f, 6.5f);
        cuerpo.GetComponent<Renderer>().sharedMaterial = fachada;

        GameObject techo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        techo.transform.SetParent(casa.transform, false);
        techo.transform.localPosition = new Vector3(0, 7.3f, 0);
        techo.transform.localScale = new Vector3(7.2f, 0.6f, 7.2f);
        techo.GetComponent<Renderer>().sharedMaterial = teja;

        // Balcón de herrería en segundo piso
        GameObject balcon = GameObject.CreatePrimitive(PrimitiveType.Cube);
        balcon.transform.SetParent(casa.transform, false);
        balcon.transform.localPosition = new Vector3(0, 4.5f, 3.5f);
        balcon.transform.localScale = new Vector3(2.5f, 1.1f, 0.8f);
        balcon.GetComponent<Renderer>().sharedMaterial = herreria;
    }

    static void CrearFarolColonial(Transform padre, Vector3 pos, Material herreria, Material luzMat)
    {
        GameObject farol = new GameObject("Farol_Colonial");
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
        cabina.GetComponent<Renderer>().sharedMaterial = luzMat;
        Object.DestroyImmediate(cabina.GetComponent<Collider>());

        Light l = farol.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.85f, 0.5f);
        l.range = 12f;
        l.intensity = 2.6f;
    }

    static void CrearArcoCantera(Transform padre, Vector3 pos, Vector3 forward, Material cantera)
    {
        GameObject arco = new GameObject("Arco_Cantera_Tunel");
        arco.transform.SetParent(padre, false);
        arco.transform.position = pos;
        arco.transform.rotation = Quaternion.LookRotation(forward);

        Parte(arco.transform, "PilarI", PrimitiveType.Cube, new Vector3(-4f, 3.5f, 0), new Vector3(1.4f, 7f, 1.4f), cantera, true);
        Parte(arco.transform, "PilarD", PrimitiveType.Cube, new Vector3(4f, 3.5f, 0), new Vector3(1.4f, 7f, 1.4f), cantera, true);
        Parte(arco.transform, "DintelArco", PrimitiveType.Cube, new Vector3(0, 7.5f, 0), new Vector3(9.5f, 1.5f, 1.6f), cantera, true);
    }

    static void CrearCallejonDelBesoIconico(Transform padre, Terrain terreno, Vector2 localCentro)
    {
        Vector3 pos = LocalAMundo(terreno, localCentro);
        GameObject besoRoot = new GameObject("Hito_CallejonDelBeso");
        besoRoot.transform.SetParent(padre, false);
        besoRoot.transform.position = pos;

        Material rojoCarmen = Mat(new Color(0.85f, 0.25f, 0.25f));
        Material amarilloLuis = Mat(new Color(0.92f, 0.72f, 0.22f));
        Material teja = Mat(new Color(0.68f, 0.22f, 0.15f));
        Material herreria = Mat(new Color(0.1f, 0.1f, 0.12f));

        // Casa de Doña Carmen (Izquierda)
        GameObject casaCarmen = new GameObject("Balcon_Carmen");
        casaCarmen.transform.SetParent(besoRoot.transform, false);
        casaCarmen.transform.localPosition = new Vector3(-3.2f, 0, 0);
        CrearCasaColonial(casaCarmen.transform, Vector3.zero, Vector3.forward, rojoCarmen, teja, herreria);

        // Casa de Don Luis (Derecha) - los balcones están a apenas 70 cm
        GameObject casaLuis = new GameObject("Balcon_Luis");
        casaLuis.transform.SetParent(besoRoot.transform, false);
        casaLuis.transform.localPosition = new Vector3(3.2f, 0, 0);
        CrearCasaColonial(casaLuis.transform, Vector3.zero, Vector3.forward, amarilloLuis, teja, herreria);
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

    // ================================================================== SELLOS Y EMBOSCADAS
    static void CrearSellosYZonas(Terrain terreno)
    {
        GameObject padre = new GameObject("Sellos_Y_Emboscadas");

        int[] monjes = { 4, 5, 5 };
        int[] sombras = { 1, 2, 2 };

        for (int i = 0; i < 3; i++)
        {
            BosqueTerrenoBuilder.Plaza pl = Plazas[i + 1];
            Vector3 pos = LocalAMundo(terreno, pl.centro);

            GameObject sello = CrearSello("Totem_Sello_" + (i + 1), pos);
            sello.transform.SetParent(padre.transform, true);

            GameObject zona = new GameObject("Emboscada_Sello_" + (i + 1));
            zona.transform.SetParent(padre.transform, true);
            zona.transform.position = pos;
            ZonaEmboscada z = zona.AddComponent<ZonaEmboscada>();
            z.radioActivacion = 22f;
            z.serpientes = monjes[i];
            z.hombresPiedra = sombras[i];
            z.radioMinSpawn = 9f;
            z.radioMaxSpawn = pl.radio + 2f;
        }
    }

    static GameObject CrearSello(string nombre, Vector3 posSuelo)
    {
        GameObject root = new GameObject(nombre);
        root.transform.position = posSuelo;

        Material cantera = Mat(new Color(0.82f, 0.62f, 0.55f));
        Material dorado = MatEmisivo(new Color(1f, 0.8f, 0.2f), new Color(2.4f, 1.8f, 0.4f));

        Parte(root.transform, "PedestalCantera", PrimitiveType.Cylinder, new Vector3(0, 0.8f, 0), new Vector3(1.3f, 0.8f, 1.3f), cantera, true);
        Parte(root.transform, "AroDorado", PrimitiveType.Cylinder, new Vector3(0, 1.6f, 0), new Vector3(1.1f, 0.1f, 1.1f), dorado, false);

        GameObject cris = new GameObject("SelloEspiritual");
        cris.transform.SetParent(root.transform, false);
        cris.transform.localPosition = new Vector3(0, 2.8f, 0);
        Parte(cris.transform, "GemaSello", PrimitiveType.Sphere, Vector3.zero, new Vector3(0.6f, 0.9f, 0.6f), dorado, false);

        SphereCollider sc = cris.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 1.8f;
        TotemColeccionable tc = cris.AddComponent<TotemColeccionable>();
        tc.valorTotem = 1;
        tc.curacionEspiritual = 40f;

        Light l = cris.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.85f, 0.2f);
        l.range = 14f;
        l.intensity = 3.8f;

        return root;
    }

    static void CrearPortalVictoriaFinal(Terrain terreno)
    {
        BosqueTerrenoBuilder.Plaza pl = Plazas[4];
        Vector3 pos = LocalAMundo(terreno, pl.centro);

        GameObject root = new GameObject("Portal_Victoria_Nivel3");
        root.transform.position = pos + Vector3.forward * 16f;

        Material cantera = Mat(new Color(0.82f, 0.62f, 0.55f));
        Material dorado = MatEmisivo(new Color(1f, 0.9f, 0.4f), new Color(3f, 2.5f, 1f));

        Parte(root.transform, "PilarI", PrimitiveType.Cube, new Vector3(-3.5f, 3.5f, 0), new Vector3(1.4f, 7f, 1.4f), cantera, true);
        Parte(root.transform, "PilarD", PrimitiveType.Cube, new Vector3(3.5f, 3.5f, 0), new Vector3(1.4f, 7f, 1.4f), cantera, true);
        Parte(root.transform, "Dintel", PrimitiveType.Cube, new Vector3(0, 7.5f, 0), new Vector3(9f, 1.4f, 1.6f), cantera, true);
        GameObject vor = Parte(root.transform, "VorticeCentral", PrimitiveType.Sphere, new Vector3(0, 3.5f, 0), new Vector3(5.4f, 5.8f, 0.4f), dorado, false);

        BoxCollider bc = root.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size = new Vector3(5.6f, 6f, 2.8f);

        PortalVictoria pv = root.AddComponent<PortalVictoria>();
        pv.efectosPortalActivo = vor;
    }

    // ================================================================== NARRATIVA Y BASÍLICA DE GUANAJUATO
    static void ConfigurarNarrativaYBasílica(Terrain terreno)
    {
        BosqueTerrenoBuilder.Plaza plArena = Plazas[4];
        Vector3 centroArena = LocalAMundo(terreno, plArena.centro);

        // Monumental Basílica Colegiata de Guanajuato (Arena Final)
        GameObject basilica = new GameObject("AtrioBasílica_ArenaFinal");
        basilica.transform.position = centroArena;
        Material amarilloBasílica = Mat(new Color(0.92f, 0.75f, 0.22f));
        Material canteraRosa = Mat(new Color(0.82f, 0.62f, 0.55f));
        Material rojoCupula = Mat(new Color(0.72f, 0.22f, 0.18f));

        // Atrio amplio de cantera
        GameObject atrio = GameObject.CreatePrimitive(PrimitiveType.Cube);
        atrio.transform.SetParent(basilica.transform, false);
        atrio.transform.localPosition = new Vector3(0, 0.1f, 0);
        atrio.transform.localScale = new Vector3(36f, 0.4f, 36f);
        atrio.GetComponent<Renderer>().sharedMaterial = canteraRosa;

        // Fachada principal de la Basílica
        GameObject fachada = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fachada.transform.SetParent(basilica.transform, false);
        fachada.transform.localPosition = new Vector3(0, 8f, 14f);
        fachada.transform.localScale = new Vector3(22f, 16f, 6f);
        fachada.GetComponent<Renderer>().sharedMaterial = amarilloBasílica;

        // Torres gemelas campanario (24 metros de altura)
        GameObject torreI = GameObject.CreatePrimitive(PrimitiveType.Cube);
        torreI.transform.SetParent(basilica.transform, false);
        torreI.transform.localPosition = new Vector3(-8.5f, 12f, 14f);
        torreI.transform.localScale = new Vector3(5f, 24f, 5f);
        torreI.GetComponent<Renderer>().sharedMaterial = amarilloBasílica;

        GameObject torreD = GameObject.CreatePrimitive(PrimitiveType.Cube);
        torreD.transform.SetParent(basilica.transform, false);
        torreD.transform.localPosition = new Vector3(8.5f, 12f, 14f);
        torreD.transform.localScale = new Vector3(5f, 24f, 5f);
        torreD.GetComponent<Renderer>().sharedMaterial = amarilloBasílica;

        // Cúpula central
        GameObject cúpula = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cúpula.transform.SetParent(basilica.transform, false);
        cúpula.transform.localPosition = new Vector3(0, 18f, 14f);
        cúpula.transform.localScale = new Vector3(9f, 9f, 9f);
        cúpula.GetComponent<Renderer>().sharedMaterial = rojoCupula;

        // Jefe Final: El Clérigo Inquisidor
        GameObject bossGo = new GameObject("Jefe_ClerigoInquisidor");
        bossGo.transform.position = centroArena + Vector3.up * 0.2f;
        bossGo.AddComponent<JefeInquisidor>();

        // Puesto Don Chuy en Plaza 3
        Vector3 posChuy = LocalAMundo(terreno, Sendero[20]);
        GameObject chuyGo = new GameObject("Comerciante_DonChuy");
        chuyGo.transform.position = posChuy;
        chuyGo.AddComponent<ComercianteMercader>();

        // Hitos Narrativos
        GameObject hitos = new GameObject("Hitos_Narrativos");
        CrearHito(hitos.transform, terreno, "intro_ciudad", Sendero[1], 16f);
        CrearHito(hitos.transform, terreno, "mirador_pipila", Sendero[3], 18f);
        CrearHito(hitos.transform, terreno, "monjes", Plazas[1].centro, 32f);
        CrearHito(hitos.transform, terreno, "sello1", Plazas[1].centro + Vector2.one * 5f, 20f);
        CrearHito(hitos.transform, terreno, "callejon_beso", Plazas[2].centro, 32f);
        CrearHito(hitos.transform, terreno, "sello2", Plazas[2].centro + Vector2.one * 5f, 20f);
        CrearHito(hitos.transform, terreno, "tuneles_mineros", Sendero[20], 20f);
        CrearHito(hitos.transform, terreno, "sello3", Plazas[3].centro, 20f);
        CrearHito(hitos.transform, terreno, "basilica", Plazas[4].centro, 32f);

        // Sendero guardado
        SenderoNivel sn = new GameObject("Sendero_Nivel3").AddComponent<SenderoNivel>();
        var pts = new List<Vector3>();
        foreach (var s in Sendero) pts.Add(LocalAMundo(terreno, s));
        sn.puntos = pts.ToArray();

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

    // ================================================================== DIRECTOR, POLVO DE ORO Y LÍMITES
    static void CrearDirector(Terrain terreno)
    {
        GameObject dir = new GameObject("Director_Nivel3");
        dir.AddComponent<DirectorNivelCiudad>();

        for (int i = 0; i < Sendero.Length; i += 2)
        {
            Vector3 p = LocalAMundo(terreno, Sendero[i]);
            GameObject alma = new GameObject("SpawnAlma_" + i);
            alma.transform.SetParent(dir.transform, false);
            alma.transform.position = p + Vector3.up * 1f;
        }
    }

    static void CrearLuciernagasPolvoOro(Terrain terreno)
    {
        GameObject padre = new GameObject("Polvo_Oro_Colonial");
        foreach (var pl in Plazas)
        {
            Vector3 pos = LocalAMundo(terreno, pl.centro) + Vector3.up * 2f;
            GameObject go = new GameObject("NubeOro");
            go.transform.SetParent(padre.transform, false);
            go.transform.position = pos;
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startColor = new Color(1f, 0.85f, 0.35f, 0.8f);
            main.startSize = 0.3f;
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
        string ruta = CarpetaMats + "/PerfilCiudadNocturno.asset";
        VolumeProfile perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ruta);
        if (perfil == null)
        {
            perfil = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(perfil, ruta);
            var bloom = perfil.Add<Bloom>(true);
            bloom.threshold.value = 0.8f;
            bloom.intensity.value = 1.6f;
        }

        GameObject go = new GameObject("PostProceso_Ciudad");
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
            if (tmp != null) tmp.text = "La Inquisición y las sombras de la ciudad te condenaron. Inténtalo de nuevo.";
        }
        Transform tVictoria = canvas.transform.Find("PanelVictoria/SubVictoria");
        if (tVictoria != null)
        {
            var tmp = tVictoria.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.text = "¡Rompiste la condena en la Basílica! Tu alma regresa al amanecer con tu familia.";
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
