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
/// Crea la escena "Nivel1_Bosque" (Nivel 1 del juego):
///  - Terreno de bosque nocturno con sendero, claros, montañas alrededor y vegetación URP.
///  - Chofer listo para combatir (combo, onda espiritual, esquive con i-frames) + Criatura guía.
///  - 3 tótems protegidos por emboscadas, almas errantes, esbirros (serpientes y hombres de piedra) y portal de salida.
///  - HUD (poder espiritual, tótems, almas, avisos, recarga de la onda), pausa, derrota y victoria.
/// </summary>
public static class Nivel1BosqueSetup
{
    const string RutaEscena = "Assets/Scenes/Nivel1_Bosque.unity";
    const string RutaTerrenoData = "Assets/Scenes/Nivel1_Bosque_TerrainData.asset";
    const string CarpetaPrefabs = "Assets/Game/Prefabs/Sierra";
    const string CarpetaMats = "Assets/Game/Materials/Sierra";
    const string CarpetaMeshes = "Assets/Game/Meshes/Sierra";
    const string RutaModeloChofer = "Assets/Game/Models/Chofer/Meshy_AI_chofer_trailer_3d_biped_Character_output.fbx";
    const string RutaAnimController = "Assets/Game/Animations/Chofer/ChoferAnimController.controller";

    static Shader shaderLit;

    [MenuItem("EvitandoElFin/6. Crear Nivel 1 (Sierra de Guanajuato)")]
    public static void Crear()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        try
        {
            AsegurarCarpeta("Assets/Game/Prefabs", "Sierra");
            AsegurarCarpeta("Assets/Game/Materials", "Sierra");
            AsegurarCarpeta("Assets/Game", "Meshes");
            AsegurarCarpeta("Assets/Game/Meshes", "Sierra");
            shaderLit = Shader.Find("Universal Render Pipeline/Lit");
            if (shaderLit == null) shaderLit = Shader.Find("Standard");

            var escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            ConfigurarAmbiente();
            Terrain terreno = BosqueTerrenoBuilder.Construir(RutaTerrenoData, CarpetaPrefabs, CarpetaMats, CarpetaMeshes);
            SierraProps.Crear(terreno);
            CrearLimites();

            EditorUtility.DisplayProgressBar("Nivel 1 (Bosque)", "Colocando personaje, tótems y enemigos...", 0.8f);
            GameObject chofer = CrearChofer(terreno);
            ConfigurarCamara(chofer);
            CrearCriatura(chofer);

            HUDEspiritual hud = null;   // el HUD ahora es HUDMexicano (se construye por código)
            new GameObject("HUD_Mexicano").AddComponent<HUDMexicano>();
            LevelUIBuilder.MontarManagersYPaneles(false);
            AjustarTextosPaneles();

            CrearTotemsYZonas(terreno);
            CrearPortal(terreno);
            CrearDirector(terreno);
            CrearLuciernagas(terreno);
            CrearPostProceso();
            Nivel1HistoriaSetup.Aplicar();   // alebrije narrador, hitos, comadres y jefe La Bufa

            GameObject despertar = new GameObject("Secuencia_Despertar");
            DespertarConfusion dc = despertar.AddComponent<DespertarConfusion>();
            dc.chofer = chofer.GetComponent<ChoferController>();
            dc.camara = Camera.main != null ? Camera.main.GetComponent<CamaraTerceraPersona>() : null;

            new GameObject("AmbienteSonoro").AddComponent<AmbienteSonoroBosque>();

            if (hud != null)
            {
                hud.vidaChofer = chofer.GetComponent<PoderEspiritual>();
                hud.inventarioChofer = chofer.GetComponent<InventarioEspiritual>();
                hud.combateChofer = chofer.GetComponent<SistemaCombateChofer>();
            }

            EditorSceneManager.SaveScene(escena, RutaEscena);
            ActualizarBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        Debug.Log("<color=#00FFAA>[Nivel1_Bosque] Escena creada: " + RutaEscena + "</color>");
        EditorUtility.DisplayDialog("Evitando El Fin",
            "Escena 'Nivel1_Bosque' creada y agregada a Build Settings.\n\n" +
            "• Sierra de Guanajuato de noche: mapa de 640 m con un sendero de ~1300 m, cerros de cantera, arroyo seco, nopales, magueyes, mezquites y encinos.\n" +
            "• Cinemáticas narrativas mientras Tonalli cuenta la historia (ENTER las salta).\n" +
            "• Chofer con combo (click izq. / F), onda espiritual (Q) y salto (Espacio) y esquive (Ctrl o C).\n" +
            "• 3 tótems protegidos por emboscadas + almas errantes + serpientes, hombres de piedra y Comadres.\n" +
            "• Alebrije Tonalli cuenta la leyenda de La Bufa (R = pista) y al final espera el jefe LA BUFA.\n" +
            "• Portal de salida (se abre al vencer a La Bufa), pausa (Esc), derrota y victoria.\n\n" +
            "Dale Play directamente a esta escena para probarla.", "Listo");
    }

    [MenuItem("EvitandoElFin/6b. Replantar árboles y vegetación de Nivel 1 (sin rehacer la escena)")]
    public static void ReplantarVegetacion()
    {
        if (BosqueTerrenoBuilder.Replantar(RutaTerrenoData, CarpetaPrefabs, CarpetaMats))
            EditorUtility.DisplayDialog("Evitando El Fin", "Vegetación replantada en el terreno de Nivel1_Bosque.\nSi la escena está abierta, guárdala (Ctrl+S).", "Listo");
        else
            EditorUtility.DisplayDialog("Evitando El Fin", "No encontré el terreno o los prefabs de la sierra. Ejecuta primero el menú 6.", "OK");
    }

    // ================================================================== AMBIENTE

    static void ConfigurarAmbiente()
    {
        Material cielo = EscenaCarreteraSetup.CargarCieloNoche();
        if (cielo != null) RenderSettings.skybox = cielo;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.07f, 0.09f, 0.16f);
        RenderSettings.reflectionIntensity = 0.3f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0075f;
        RenderSettings.fogColor = new Color(0.04f, 0.07f, 0.11f);

        Light sol = Object.FindObjectsByType<Light>().FirstOrDefault(l => l.type == LightType.Directional);
        if (sol != null)
        {
            sol.name = "Luz_Luna";
            sol.color = new Color(0.50f, 0.62f, 1f);
            sol.intensity = 0.65f;
            sol.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
            sol.shadows = LightShadows.Soft;
        }

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = cielo != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.03f, 0.07f);
            cam.farClipPlane = 600f;
            cam.nearClipPlane = 0.1f;
            var datos = cam.GetUniversalAdditionalCameraData();
            if (datos != null) datos.renderPostProcessing = true;
        }
        DynamicGI.UpdateEnvironment();
    }

    static void CrearPostProceso()
    {
        string ruta = CarpetaMats + "/PerfilBosqueNocturno.asset";
        VolumeProfile perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ruta);
        if (perfil == null)
        {
            perfil = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(perfil, ruta);

            var bloom = perfil.Add<Bloom>(true);
            bloom.threshold.Override(0.8f);
            bloom.intensity.Override(1.3f);
            bloom.scatter.Override(0.7f);

            var vignette = perfil.Add<Vignette>(true);
            vignette.intensity.Override(0.38f);
            vignette.smoothness.Override(0.6f);

            var color = perfil.Add<ColorAdjustments>(true);
            color.postExposure.Override(0.35f);
            color.saturation.Override(-8f);
            color.colorFilter.Override(new Color(0.88f, 0.95f, 1f));

            var tono = perfil.Add<Tonemapping>(true);
            tono.mode.Override(TonemappingMode.ACES);

            EditorUtility.SetDirty(perfil);
        }

        GameObject go = new GameObject("Volumen_Postproceso");
        Volume v = go.AddComponent<Volume>();
        v.isGlobal = true;
        v.sharedProfile = perfil;
    }

    static void CrearLimites()
    {
        GameObject raiz = new GameObject("LimitesNivel");
        float m = BosqueTerrenoBuilder.Tam * 0.5f;
        CrearMuro(raiz.transform, new Vector3(0f, 50f, m), new Vector3(BosqueTerrenoBuilder.Tam, 140f, 2f));
        CrearMuro(raiz.transform, new Vector3(0f, 50f, -m), new Vector3(BosqueTerrenoBuilder.Tam, 140f, 2f));
        CrearMuro(raiz.transform, new Vector3(m, 50f, 0f), new Vector3(2f, 140f, BosqueTerrenoBuilder.Tam));
        CrearMuro(raiz.transform, new Vector3(-m, 50f, 0f), new Vector3(2f, 140f, BosqueTerrenoBuilder.Tam));
    }

    static void CrearMuro(Transform padre, Vector3 pos, Vector3 tam)
    {
        GameObject g = new GameObject("MuroInvisible");
        g.transform.SetParent(padre, false);
        g.transform.position = pos;
        g.AddComponent<BoxCollider>().size = tam;
    }

    // ================================================================== PERSONAJE

    static float AlturaMundo(Terrain t, Vector3 mundo)
    {
        return t.SampleHeight(mundo) + t.transform.position.y;
    }

    static GameObject CrearChofer(Terrain terreno)
    {
        Vector2 inicio = BosqueTerrenoBuilder.Sendero[0];
        Vector2 siguiente = BosqueTerrenoBuilder.Sendero[1];
        Vector3 pos = BosqueTerrenoBuilder.LocalAMundo(inicio, 0f);
        pos.y = AlturaMundo(terreno, pos) + 1.1f;
        Vector2 dir = (siguiente - inicio).normalized;

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
            Debug.LogWarning("[Bosque] No se encontró el modelo del Chofer en " + RutaModeloChofer + ". Se usa una cápsula temporal.");
            modelo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Object.DestroyImmediate(modelo.GetComponent<Collider>());
            modelo.transform.SetParent(root.transform, false);
        }
        modelo.name = "ChoferModelo";
        modelo.transform.localPosition = new Vector3(0f, -1f, 0f);
        modelo.transform.localRotation = Quaternion.identity;

        Animator anim = modelo.GetComponentInChildren<Animator>();
        if (anim == null) anim = modelo.AddComponent<Animator>();
        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AplicarAnimacionesChofer.RutaControllerNuevo);
        if (controller == null) controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(RutaAnimController);
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
        CamaraTerceraPersona c = cam.GetComponent<CamaraTerceraPersona>();
        if (c == null) c = cam.gameObject.AddComponent<CamaraTerceraPersona>();
        c.objetivo = chofer.transform;
    }

    static void CrearCriatura(GameObject chofer)
    {
        GameObject root = new GameObject("Criatura_Alebrije");
        root.transform.position = chofer.transform.position + new Vector3(1.2f, 1.8f, -0.8f);

        Material cuerpo = MatEmisivo(new Color(1f, 0.45f, 0.1f), new Color(1.6f, 0.5f, 0.1f));
        Material cabeza = MatEmisivo(new Color(0.95f, 0.2f, 0.6f), new Color(1.4f, 0.2f, 0.7f));
        Material ala = MatEmisivo(new Color(0.2f, 0.9f, 1f), new Color(0.2f, 1.4f, 1.8f));
        Material negro = Mat(new Color(0.03f, 0.03f, 0.04f));

        Parte(root.transform, "Cuerpo", PrimitiveType.Sphere, new Vector3(0f, 0f, 0f), new Vector3(0.45f, 0.38f, 0.6f), cuerpo);
        GameObject cab = Parte(root.transform, "Cabeza", PrimitiveType.Sphere, new Vector3(0f, 0.12f, 0.38f), Vector3.one * 0.3f, cabeza);
        Parte(cab.transform, "OjoI", PrimitiveType.Sphere, new Vector3(-0.28f, 0.15f, 0.4f), Vector3.one * 0.22f, negro);
        Parte(cab.transform, "OjoD", PrimitiveType.Sphere, new Vector3(0.28f, 0.15f, 0.4f), Vector3.one * 0.22f, negro);
        GameObject alaI = Parte(root.transform, "AlaI", PrimitiveType.Sphere, new Vector3(-0.38f, 0.14f, 0f), new Vector3(0.6f, 0.04f, 0.34f), ala);
        alaI.transform.localRotation = Quaternion.Euler(0f, 0f, 24f);
        GameObject alaD = Parte(root.transform, "AlaD", PrimitiveType.Sphere, new Vector3(0.38f, 0.14f, 0f), new Vector3(0.6f, 0.04f, 0.34f), ala);
        alaD.transform.localRotation = Quaternion.Euler(0f, 0f, -24f);
        Parte(root.transform, "Cola1", PrimitiveType.Sphere, new Vector3(0f, 0f, -0.38f), Vector3.one * 0.2f, cabeza);
        Parte(root.transform, "Cola2", PrimitiveType.Sphere, new Vector3(0f, -0.04f, -0.58f), Vector3.one * 0.13f, cuerpo);

        GameObject luzGO = new GameObject("LuzEspiritual");
        luzGO.transform.SetParent(root.transform, false);
        Light luz = luzGO.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = new Color(0.2f, 0.9f, 1f);
        luz.range = 9f;
        luz.intensity = 2.2f;
        luz.shadows = LightShadows.None;

        root.transform.localScale = Vector3.one * 0.8f;
        CriaturaGuia guia = root.AddComponent<CriaturaGuia>();
        guia.objetivoChofer = chofer.transform;
        guia.luzEspiritual = luz;
    }

    // ================================================================== TÓTEMS, ZONAS, PORTAL

    static void CrearTotemsYZonas(Terrain terreno)
    {
        GameObject padre = new GameObject("Totems_Y_Emboscadas");

        // Tótem 1, 2 y 3 en los claros 1..3 con oleadas crecientes
        int[] serpientes = { 3, 4, 3 };
        int[] piedras = { 0, 1, 2 };
        for (int i = 0; i < 3; i++)
        {
            BosqueTerrenoBuilder.Plaza pl = BosqueTerrenoBuilder.Plazas[i + 1];
            Vector3 pos = BosqueTerrenoBuilder.LocalAMundo(pl.centro, 0f);
            pos.y = AlturaMundo(terreno, pos);

            GameObject totem = CrearTotem("Totem_" + (i + 1), pos);
            totem.transform.SetParent(padre.transform, true);

            GameObject zona = new GameObject("Emboscada_" + (i + 1));
            zona.transform.SetParent(padre.transform, true);
            zona.transform.position = pos;
            ZonaEmboscada z = zona.AddComponent<ZonaEmboscada>();
            z.radioActivacion = 20f;
            z.serpientes = serpientes[i];
            z.hombresPiedra = piedras[i];
            z.radioMinSpawn = 9f;
            z.radioMaxSpawn = pl.radio + 2f;
        }
    }

    static GameObject CrearTotem(string nombre, Vector3 posSuelo)
    {
        GameObject root = new GameObject(nombre);
        root.transform.position = posSuelo;

        Material piedra = Mat(new Color(0.22f, 0.23f, 0.26f));
        Material runa = MatEmisivo(new Color(0.2f, 0.9f, 0.85f), new Color(0.3f, 1.8f, 1.6f));
        Material cristal = MatEmisivo(new Color(0.4f, 1f, 0.95f), new Color(0.6f, 2.8f, 2.6f));

        // Pedestal sólido (con collider) y anillos de runas
        GameObject pedestal = Parte(root.transform, "Pedestal", PrimitiveType.Cylinder, new Vector3(0f, 0.9f, 0f), new Vector3(0.9f, 0.9f, 0.9f), piedra, true);
        Parte(root.transform, "Base", PrimitiveType.Cylinder, new Vector3(0f, 0.12f, 0f), new Vector3(1.8f, 0.12f, 1.8f), piedra, true);
        Parte(root.transform, "Runas1", PrimitiveType.Cylinder, new Vector3(0f, 1.2f, 0f), new Vector3(1.0f, 0.04f, 1.0f), runa, false);
        Parte(root.transform, "Runas2", PrimitiveType.Cylinder, new Vector3(0f, 1.65f, 0f), new Vector3(0.95f, 0.03f, 0.95f), runa, false);

        // Cristal flotante recolectable (trigger)
        GameObject cris = new GameObject("Cristal");
        cris.transform.SetParent(root.transform, false);
        cris.transform.localPosition = new Vector3(0f, 2.5f, 0f);
        GameObject gema = Parte(cris.transform, "Gema", PrimitiveType.Sphere, Vector3.zero, new Vector3(0.55f, 0.85f, 0.55f), cristal, false);
        gema.transform.localRotation = Quaternion.Euler(0f, 45f, 20f);
        SphereCollider sc = cris.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 1.6f;
        TotemColeccionable tc = cris.AddComponent<TotemColeccionable>();
        tc.valorTotem = 1;
        tc.curacionEspiritual = 25f;
        tc.velocidadRotacion = 70f;
        tc.amplitudFlotacion = 0.18f;

        GameObject luzGO = new GameObject("LuzTotem");
        luzGO.transform.SetParent(cris.transform, false);
        Light l = luzGO.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(0.3f, 1f, 0.9f);
        l.range = 12f;
        l.intensity = 3.2f;
        l.shadows = LightShadows.None;

        // Haz de luz visible desde lejos
        Shader spr = Shader.Find("Sprites/Default");
        if (spr != null)
        {
            Material haz = new Material(spr) { color = new Color(0.3f, 1f, 0.9f, 0.10f) };
            // Hijo del cristal: desaparece junto con él al recoger el tótem
            GameObject beacon = Parte(cris.transform, "HazLuz", PrimitiveType.Cylinder, new Vector3(0f, 20f, 0f), new Vector3(0.7f, 20f, 0.7f), haz, false);
            beacon.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }
        return root;
    }

    static void CrearPortal(Terrain terreno)
    {
        BosqueTerrenoBuilder.Plaza pl = BosqueTerrenoBuilder.Plazas[4];
        Vector3 pos = BosqueTerrenoBuilder.LocalAMundo(pl.centro, 0f);
        pos.y = AlturaMundo(terreno, pos);

        Vector2 llegada = (BosqueTerrenoBuilder.Sendero[BosqueTerrenoBuilder.Sendero.Length - 1] - BosqueTerrenoBuilder.Sendero[BosqueTerrenoBuilder.Sendero.Length - 2]).normalized;

        GameObject root = new GameObject("Portal_Victoria_Nivel1");
        root.transform.position = pos;
        root.transform.rotation = Quaternion.LookRotation(new Vector3(llegada.x, 0f, llegada.y));

        Material piedra = Mat(new Color(0.2f, 0.21f, 0.24f));
        Material runa = MatEmisivo(new Color(0.2f, 0.9f, 0.85f), new Color(0.3f, 1.8f, 1.6f));
        Material vortice = MatEmisivo(new Color(0.25f, 0.9f, 0.8f), new Color(0.35f, 2.2f, 2.0f));

        Parte(root.transform, "PilarI", PrimitiveType.Cube, new Vector3(-3.2f, 3f, 0f), new Vector3(1.3f, 6f, 1.3f), piedra, true);
        Parte(root.transform, "PilarD", PrimitiveType.Cube, new Vector3(3.2f, 3f, 0f), new Vector3(1.3f, 6f, 1.3f), piedra, true);
        Parte(root.transform, "Dintel", PrimitiveType.Cube, new Vector3(0f, 6.5f, 0f), new Vector3(8.6f, 1.2f, 1.5f), piedra, true);
        Parte(root.transform, "RunaI", PrimitiveType.Cube, new Vector3(-3.2f, 3f, 0.68f), new Vector3(0.3f, 4f, 0.05f), runa, false);
        Parte(root.transform, "RunaD", PrimitiveType.Cube, new Vector3(3.2f, 3f, 0.68f), new Vector3(0.3f, 4f, 0.05f), runa, false);
        Parte(root.transform, "RunaDintel", PrimitiveType.Cube, new Vector3(0f, 6.5f, 0.78f), new Vector3(5f, 0.3f, 0.05f), runa, false);
        GameObject vor = Parte(root.transform, "VorticeCentral", PrimitiveType.Sphere, new Vector3(0f, 3f, 0f), new Vector3(5.2f, 5.4f, 0.35f), vortice, false);

        GameObject luzGO = new GameObject("LuzPortal");
        luzGO.transform.SetParent(root.transform, false);
        luzGO.transform.localPosition = new Vector3(0f, 3f, 1.5f);
        Light l = luzGO.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(0.2f, 1f, 0.8f);
        l.range = 18f;
        l.intensity = 4f;
        l.shadows = LightShadows.None;

        BoxCollider bc = root.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size = new Vector3(5.4f, 5.6f, 2.6f);
        bc.center = new Vector3(0f, 3f, 0f);

        PortalVictoria pv = root.AddComponent<PortalVictoria>();
        pv.efectosPortalActivo = vor;

        // Emboscada final antes del portal
        GameObject zona = new GameObject("Emboscada_Portal");
        zona.transform.position = pos;
        ZonaEmboscada z = zona.AddComponent<ZonaEmboscada>();
        z.radioActivacion = 24f;
        z.serpientes = 4;
        z.hombresPiedra = 2;
        z.radioMinSpawn = 10f;
        z.radioMaxSpawn = 15f;
        z.mensajeInicio = "¡Los guardianes del portal despiertan!";
    }

    // ================================================================== DIRECTOR (almas y esbirros errantes)

    static void CrearDirector(Terrain terreno)
    {
        GameObject dir = new GameObject("Director_Nivel1");
        dir.AddComponent<DirectorNivelBosque>();

        List<Vector2> sendero = BosqueTerrenoBuilder.MuestrasSendero();
        System.Random rng = new System.Random(77);
        int cuenta = 0;

        // Almas a lo largo del sendero (una cada ~36 m)
        for (int i = 6; i < sendero.Count - 4; i += 18)
        {
            Vector2 d = (sendero[Mathf.Min(i + 1, sendero.Count - 1)] - sendero[i]).normalized;
            Vector2 perp = new Vector2(-d.y, d.x);
            float lado = (rng.NextDouble() < 0.5 ? -1f : 1f) * (2.5f + (float)rng.NextDouble() * 3.5f);
            Vector2 p = sendero[i] + perp * lado;
            Marcador(dir.transform, "SpawnAlma_" + (cuenta++), p);
        }
        // Almas escondidas en el bosque (más lejos del camino)
        for (int k = 0; k < 26; k++)
        {
            int i = rng.Next(4, sendero.Count - 4);
            Vector2 d = (sendero[Mathf.Min(i + 1, sendero.Count - 1)] - sendero[i]).normalized;
            Vector2 perp = new Vector2(-d.y, d.x);
            float lado = (rng.NextDouble() < 0.5 ? -1f : 1f) * (12f + (float)rng.NextDouble() * 14f);
            Marcador(dir.transform, "SpawnAlma_" + (cuenta++), sendero[i] + perp * lado);
        }

        // Serpientes errantes entre claros
        // Los errantes aparecen DESPUÉS de que Tonalli los presenta: serpientes desde ~470 m, hombres de piedra desde ~850 m
        // (nada entre ~1110 y ~1180 m: ahí está Don Chuy y se debe poder comprar tranquilo)
        float[] mSerp = Nivel1HistoriaSetup.MetrosSerpientes;
        for (int k = 0; k < mSerp.Length; k++)
        {
            Vector2 d;
            Vector2 p = BosqueTerrenoBuilder.PuntoEnSendero(mSerp[k], out d);
            Vector2 perp = new Vector2(-d.y, d.x);
            Marcador(dir.transform, "SpawnSerpiente_" + k, p + perp * (k % 2 == 0 ? 6f : -6f));
        }
        float[] mPied = Nivel1HistoriaSetup.MetrosPiedras;
        for (int k = 0; k < mPied.Length; k++)
        {
            Vector2 d;
            Vector2 p = BosqueTerrenoBuilder.PuntoEnSendero(mPied[k], out d);
            Vector2 perp = new Vector2(-d.y, d.x);
            Marcador(dir.transform, "SpawnPiedra_" + k, p + perp * (k % 2 == 0 ? -9f : 9f));
        }
    }

    static void Marcador(Transform padre, string nombre, Vector2 local)
    {
        GameObject m = new GameObject(nombre);
        m.transform.SetParent(padre, false);
        m.transform.position = BosqueTerrenoBuilder.LocalAMundo(local, 0f);
    }

    // ================================================================== LUCIÉRNAGAS

    static void CrearLuciernagas(Terrain terreno)
    {
        Shader spr = Shader.Find("Sprites/Default");
        if (spr == null) return;
        Material mat = new Material(spr);
        mat.mainTexture = CrearTexturaBrillo();
        mat.color = Color.white;

        GameObject padre = new GameObject("Luciernagas");
        var centros = new List<Vector2>();
        foreach (var pl in BosqueTerrenoBuilder.Plazas) centros.Add(pl.centro);
        var sendero = BosqueTerrenoBuilder.MuestrasSendero();
        for (int i = 20; i < sendero.Count; i += 45) centros.Add(sendero[i]);

        foreach (Vector2 c in centros)
        {
            Vector3 pos = BosqueTerrenoBuilder.LocalAMundo(c, 0f);
            pos.y = AlturaMundo(terreno, pos) + 2.5f;

            GameObject go = new GameObject("NubeLuciernagas");
            go.transform.SetParent(padre.transform, false);
            go.transform.position = pos;
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f);
            main.startSpeed = 0.15f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.85f, 1f, 0.45f, 1f), new Color(0.55f, 1f, 0.9f, 1f));
            main.maxParticles = 40;

            var em = ps.emission; em.rateOverTime = 5f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(36f, 5f, 36f);

            var ruido = ps.noise;
            ruido.enabled = true;
            ruido.strength = 1.4f;
            ruido.frequency = 0.35f;
            ruido.scrollSpeed = 0.4f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0.3f, 0.5f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);

            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
            ps.Play();
        }
    }

    static Texture2D CrearTexturaBrillo()
    {
        const int n = 64;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x / (n - 1f) - 0.5f) * 2f, dy = (y / (n - 1f) - 0.5f) * 2f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Pow(1f - Mathf.Clamp01(d), 2.4f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        return tex;
    }

    // ================================================================== HUD

    static HUDEspiritual CrearHUD()
    {
        GameObject cgo = new GameObject("Canvas_Gameplay");
        Canvas canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler cs = cgo.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;
        cgo.AddComponent<GraphicRaycaster>();

        Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        // Destello de daño (pantalla completa)
        Image flash = CrearImagen("DestelloDanio", cgo.transform, new Color(0.9f, 0.05f, 0.05f, 0f), null);
        Estirar(flash.rectTransform);
        flash.raycastTarget = false;

        // Marco superior izquierdo
        TextMeshProUGUI etiqueta = CrearTexto("EtiquetaPoder", cgo.transform, "PODER ESPIRITUAL", 20, TextAlignmentOptions.Left, new Color(0.7f, 1f, 0.95f));
        Anclar(etiqueta.rectTransform, new Vector2(0, 1), new Vector2(30, -22), new Vector2(440, 28));

        Slider slider = CrearSlider("SliderPoder", cgo.transform, uiSprite);
        Anclar((RectTransform)slider.transform, new Vector2(0, 1), new Vector2(30, -58), new Vector2(440, 26));

        TextMeshProUGUI totems = CrearTexto("TextoTotems", cgo.transform, "Tótems: 0", 28, TextAlignmentOptions.Left, new Color(0.5f, 1f, 0.9f));
        Anclar(totems.rectTransform, new Vector2(0, 1), new Vector2(30, -96), new Vector2(440, 36));
        TextMeshProUGUI almas = CrearTexto("TextoAlmas", cgo.transform, "Almas: 0", 28, TextAlignmentOptions.Left, new Color(0.7f, 0.95f, 1f));
        Anclar(almas.rectTransform, new Vector2(0, 1), new Vector2(30, -132), new Vector2(440, 36));
        TextMeshProUGUI recuerdos = CrearTexto("TextoRecuerdos", cgo.transform, "", 18, TextAlignmentOptions.Left, new Color(1f, 0.9f, 0.6f));
        Anclar(recuerdos.rectTransform, new Vector2(0, 1), new Vector2(30, -166), new Vector2(600, 26));

        // Objetivo (arriba al centro)
        TextMeshProUGUI objetivo = CrearTexto("TextoObjetivo", cgo.transform, "Objetivo: Reúne los tótems (0/3)", 28, TextAlignmentOptions.Center, Color.white);
        Anclar(objetivo.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -34), new Vector2(1000, 40));

        // Avisos (abajo al centro)
        TextMeshProUGUI aviso = CrearTexto("TextoAviso", cgo.transform, "", 34, TextAlignmentOptions.Center, new Color(1f, 0.95f, 0.75f));
        Anclar(aviso.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(1500, 90));
        aviso.outlineWidth = 0.2f;
        aviso.outlineColor = new Color32(0, 0, 0, 255);

        // Recarga de la onda espiritual (abajo izquierda)
        Image fondoOnda = CrearImagen("FondoOnda", cgo.transform, new Color(0f, 0f, 0f, 0.55f), uiSprite);
        Anclar(fondoOnda.rectTransform, new Vector2(0, 0), new Vector2(30, 56), new Vector2(280, 16));
        Image rellenoOnda = CrearImagen("RellenoOnda", fondoOnda.transform, new Color(0.3f, 0.95f, 1f, 0.95f), uiSprite);
        Estirar(rellenoOnda.rectTransform);
        rellenoOnda.type = Image.Type.Filled;
        rellenoOnda.fillMethod = Image.FillMethod.Horizontal;
        rellenoOnda.fillAmount = 1f;
        TextMeshProUGUI textoOnda = CrearTexto("TextoOnda", cgo.transform, "Q  ONDA ESPIRITUAL  LISTA", 18, TextAlignmentOptions.Left, new Color(0.8f, 1f, 1f));
        Anclar(textoOnda.rectTransform, new Vector2(0, 0), new Vector2(30, 24), new Vector2(420, 26));

        // Ayuda de controles (abajo derecha)
        TextMeshProUGUI ayuda = CrearTexto("TextoControles", cgo.transform,
            "WASD mover  ·  Shift correr  ·  Espacio saltar  ·  Ctrl / C esquivar  ·  Click / F golpear  ·  Q onda espiritual  ·  Esc pausa",
            17, TextAlignmentOptions.Right, new Color(1f, 1f, 1f, 0.55f));
        Anclar(ayuda.rectTransform, new Vector2(1, 0), new Vector2(-30, 24), new Vector2(1100, 26));

        HUDEspiritual hud = cgo.AddComponent<HUDEspiritual>();
        hud.sliderPoderEspiritual = slider;
        hud.textoTotems = totems;
        hud.textoAlmas = almas;
        hud.textoRecuerdos = recuerdos;
        hud.textoObjetivo = objetivo;
        hud.textoAviso = aviso;
        hud.destelloDanio = flash;
        hud.rellenoOnda = rellenoOnda;
        hud.textoOnda = textoOnda;
        return hud;
    }

    static void AjustarTextosPaneles()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return;
        SetTexto(canvas.transform, "PanelGameOver/TituloGameOver", "TU ALMA SE DESVANECIÓ");
        SetTexto(canvas.transform, "PanelGameOver/SubGameOver", "Los esbirros del bosque te vencieron. Inténtalo de nuevo.");
        SetTexto(canvas.transform, "PanelVictoria/SubVictoria", "Reuniste los tótems y cruzaste el portal espiritual.");
    }

    static void SetTexto(Transform raiz, string ruta, string texto)
    {
        Transform t = raiz.Find(ruta);
        if (t == null) return;
        TextMeshProUGUI tmp = t.GetComponent<TextMeshProUGUI>();
        if (tmp != null) tmp.text = texto;
    }

    // ---------------------------------------------------------------- utilidades de UI

    static Slider CrearSlider(string nombre, Transform padre, Sprite sprite)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        Slider s = go.AddComponent<Slider>();

        Image fondo = CrearImagen("Fondo", go.transform, new Color(0f, 0f, 0f, 0.6f), sprite);
        Estirar(fondo.rectTransform);

        GameObject area = new GameObject("AreaRelleno", typeof(RectTransform));
        area.transform.SetParent(go.transform, false);
        RectTransform arect = (RectTransform)area.transform;
        Estirar(arect);
        arect.offsetMin = new Vector2(3, 3);
        arect.offsetMax = new Vector2(-3, -3);

        Image relleno = CrearImagen("Relleno", area.transform, new Color(0.25f, 0.95f, 0.85f, 1f), sprite);
        relleno.rectTransform.anchorMin = Vector2.zero;
        relleno.rectTransform.anchorMax = Vector2.one;
        relleno.rectTransform.offsetMin = Vector2.zero;
        relleno.rectTransform.offsetMax = Vector2.zero;

        s.fillRect = relleno.rectTransform;
        s.targetGraphic = null;
        s.transition = Selectable.Transition.None;
        s.interactable = false;
        s.direction = Slider.Direction.LeftToRight;
        s.minValue = 0f; s.maxValue = 100f; s.value = 100f;
        s.navigation = new Navigation { mode = Navigation.Mode.None };
        return s;
    }

    static Image CrearImagen(string nombre, Transform padre, Color color, Sprite sprite)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        if (sprite != null) { img.sprite = sprite; img.type = Image.Type.Sliced; }
        img.raycastTarget = false;
        return img;
    }

    static TextMeshProUGUI CrearTexto(string nombre, Transform padre, string texto, float tam, TextAlignmentOptions alin, Color color)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.text = texto;
        t.fontSize = tam;
        t.alignment = alin;
        t.color = color;
        t.raycastTarget = false;
        return t;
    }

    static void Anclar(RectTransform rt, Vector2 ancla, Vector2 pos, Vector2 tam)
    {
        rt.anchorMin = rt.anchorMax = ancla;
        rt.pivot = ancla;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
    }

    static void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // ================================================================== MATERIALES Y PRIMITIVAS

    static Material Mat(Color c)
    {
        Material m = new Material(shaderLit);
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.25f);
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

    // ================================================================== ARCHIVOS / BUILD SETTINGS

    static void AsegurarCarpeta(string padre, string nombre)
    {
        // Crea toda la ruta (por ejemplo Assets/Game/Materials/Sierra aunque Materials no exista todavía)
        string[] partes = (padre + "/" + nombre).Split('/');
        string actual = partes[0];
        for (int i = 1; i < partes.Length; i++)
        {
            string siguiente = actual + "/" + partes[i];
            if (!AssetDatabase.IsValidFolder(siguiente)) AssetDatabase.CreateFolder(actual, partes[i]);
            actual = siguiente;
        }
    }

    static void ActualizarBuildSettings()
    {
        var lista = EditorBuildSettings.scenes.ToList();

        // El Limbo ya no forma parte del flujo del juego
        for (int i = 0; i < lista.Count; i++)
            if (lista[i].path.EndsWith("Nivel1_Limbo.unity")) lista[i] = new EditorBuildSettingsScene(lista[i].path, false);

        if (!lista.Any(s => s.path == RutaEscena)) lista.Add(new EditorBuildSettingsScene(RutaEscena, true));
        else
        {
            int i = lista.FindIndex(s => s.path == RutaEscena);
            lista[i] = new EditorBuildSettingsScene(RutaEscena, true);
        }
        EditorBuildSettings.scenes = lista.ToArray();
    }
}
