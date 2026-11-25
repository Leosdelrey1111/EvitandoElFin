using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Automatizador para importar y configurar al ALEBRIJE (Tonalli, el guía espiritual del Chofer):
/// 1. Configura la textura Normal Map.
/// 2. Genera el Material URP Lit con sus mapas de Albedo, Normal y Metálico.
/// 3. Remapea el material interno del FBX (Material.001) para que no se vea gris.
/// 4. Genera el Prefab "Criatura_Alebrije.prefab" con la escala adecuada, luz espiritual y CriaturaGuia.
/// 5. Actualiza la escena abierta (si tiene el alebrije procedural antiguo de esferas).
/// </summary>
public static class ConfiguradorAlebrijeEditor
{
    public const string CarpetaModelos = "Assets/Game/Models/Alebrije";
    public const string CarpetaPrefabs = "Assets/Game/Prefabs";

    public const string RutaModeloFbx = CarpetaModelos + "/Meshy_AI_Chibi_Hummingbird_3D_20261123_image-to-3d-texture.fbx";
    public const string RutaTexAlbedo = CarpetaModelos + "/Meshy_AI_Chibi_Hummingbird_3D_20261123_image-to-3d-texture.png";
    public const string RutaTexNormal = CarpetaModelos + "/Meshy_AI_Chibi_Hummingbird_3D_20261123_image-to-3d-texture_normal.png";
    public const string RutaTexMetallic = CarpetaModelos + "/Meshy_AI_Chibi_Hummingbird_3D_20261123_image-to-3d-texture_metallic.png";
    public const string RutaTexRoughness = CarpetaModelos + "/Meshy_AI_Chibi_Hummingbird_3D_20261123_image-to-3d-texture_roughness.png";

    public const string RutaMaterial = CarpetaModelos + "/M_Alebrije.mat";
    public const string RutaPrefab = CarpetaPrefabs + "/Criatura_Alebrije.prefab";

    [MenuItem("EvitandoElFin/10. Configurar y Reemplazar Alebrije en Nivel 1")]
    public static void ConfigurarAlebrije()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Evitando El Fin", 
                "Estás dentro del MODO PLAY (juego en ejecución).\n\n" +
                "Por favor, desactiva el botón de Play en la parte superior de Unity antes de ejecutar este comando, para que el nuevo modelo 3D se guarde permanentemente en la escena.", "Entendido");
            return;
        }

        try
        {
            EditorUtility.DisplayProgressBar("Configurando Alebrije", "Verificando archivos del modelo...", 0.1f);

            if (!File.Exists(RutaModeloFbx))
            {
                EditorUtility.DisplayDialog("Evitando El Fin", "No se encontró el modelo del Alebrije en:\n" + RutaModeloFbx, "OK");
                return;
            }

            // 1. Texturas y Material URP
            EditorUtility.DisplayProgressBar("Configurando Alebrije", "Configurando texturas y material URP Lit...", 0.35f);
            Material mat = ConfigurarMaterial();

            // 2. Remapeo en el FBX
            EditorUtility.DisplayProgressBar("Configurando Alebrije", "Remapeando material en el FBX...", 0.6f);
            ConfigurarImportadorFBX(mat);

            // 3. Crear Prefab
            EditorUtility.DisplayProgressBar("Configurando Alebrije", "Generando Prefab Criatura_Alebrije...", 0.85f);
            GameObject prefab = CrearPrefab(mat);

            // 4. Reemplazar en la escena Nivel1_Bosque y guardar
            EditorUtility.DisplayProgressBar("Configurando Alebrije", "Actualizando y guardando escena Nivel1_Bosque...", 0.95f);
            bool escenaGuardada = ReemplazarYGuardarEnEscena(prefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Evitando El Fin",
                "¡El personaje Alebrije 3D (Colibrí Tonalli) ha quedado listo!\n\n" +
                "• Material URP Lit: " + RutaMaterial + "\n" +
                "• Prefab generado: " + RutaPrefab + "\n" +
                (escenaGuardada ? "• ¡La escena 'Nivel1_Bosque' fue actualizada y guardada con el nuevo modelo 3D!\n\nYa puedes darle Play para verlo volar y acompañar al Chofer." : 
                                 "• Puedes arrastrar el Prefab desde Assets/Game/Prefabs/Criatura_Alebrije hacia tu escena."),
                "¡Excelente!");
        }
        catch (Exception ex)
        {
            Debug.LogError("[ConfiguradorAlebrije] Error: " + ex);
            EditorUtility.DisplayDialog("Error", "Ocurrió un error al configurar el Alebrije: " + ex.Message, "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    static Material ConfigurarMaterial()
    {
        if (File.Exists(RutaTexNormal))
        {
            TextureImporter ti = AssetImporter.GetAtPath(RutaTexNormal) as TextureImporter;
            if (ti != null && ti.textureType != TextureImporterType.NormalMap)
            {
                ti.textureType = TextureImporterType.NormalMap;
                ti.SaveAndReimport();
            }
        }

        Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterial);
        if (mat == null)
        {
            mat = new Material(litShader);
            mat.name = "M_Alebrije";
            AssetDatabase.CreateAsset(mat, RutaMaterial);
        }

        Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaTexAlbedo);
        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaTexNormal);
        Texture2D metallic = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaTexMetallic);

        if (albedo != null) { mat.SetTexture("_BaseMap", albedo); mat.SetTexture("_MainTex", albedo); }
        if (normal != null) { mat.SetTexture("_BumpMap", normal); mat.EnableKeyword("_NORMALMAP"); }
        if (metallic != null) { mat.SetTexture("_MetallicGlossMap", metallic); mat.EnableKeyword("_METALLICSPECGLOSSMAP"); }

        mat.SetFloat("_Smoothness", 0.5f);
        // Brillo sutil espiritual en los bordes
        mat.SetColor("_EmissionColor", new Color(0.1f, 0.35f, 0.45f) * 0.4f);
        mat.EnableKeyword("_EMISSION");

        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void ConfigurarImportadorFBX(Material mat)
    {
        ModelImporter mi = AssetImporter.GetAtPath(RutaModeloFbx) as ModelImporter;
        if (mi != null)
        {
            bool cambio = false;
            // Para objetos estáticos o compañeros voladores que flotan por código, Generic o None es óptimo
            if (mi.animationType != ModelImporterAnimationType.None)
            {
                mi.animationType = ModelImporterAnimationType.None;
                cambio = true;
            }

            if (mat != null)
            {
                var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), "Material.001");
                mi.AddRemap(id, mat);
                cambio = true;
            }

            if (cambio) mi.SaveAndReimport();
        }
    }

    static GameObject CrearPrefab(Material mat)
    {
        if (!AssetDatabase.IsValidFolder(CarpetaPrefabs))
        {
            AssetDatabase.CreateFolder("Assets/Game", "Prefabs");
        }

        GameObject raiz = new GameObject("Criatura_Alebrije");

        // Instanciar modelo FBX como hijo visual
        GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(RutaModeloFbx);
        if (fbxAsset != null)
        {
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset);
            visual.name = "Visual";
            visual.transform.SetParent(raiz.transform, false);

            // Ajustar escala y rotación del modelo para que mire hacia adelante con tamaño de colibrí (~0.5m)
            var mf = visual.GetComponentInChildren<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                Bounds b = mf.sharedMesh.bounds;
                float maxDim = Mathf.Max(b.size.x, b.size.y, b.size.z);
                if (maxDim > 0.001f)
                {
                    float factor = 0.55f / maxDim; // altura/envergadura de ~55cm
                    visual.transform.localScale = Vector3.one * factor;
                    visual.transform.localPosition = -b.center * factor;
                }
            }

            if (mat != null)
            {
                foreach (var r in visual.GetComponentsInChildren<Renderer>())
                {
                    r.sharedMaterial = mat;
                }
            }
        }

        // Luz espiritual celestial
        GameObject luzGO = new GameObject("LuzEspiritual");
        luzGO.transform.SetParent(raiz.transform, false);
        Light luz = luzGO.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = new Color(0.2f, 0.9f, 1f);
        luz.range = 8.5f;
        luz.intensity = 2.4f;
        luz.shadows = LightShadows.None;

        // Componente de guía y acompañante
        CriaturaGuia guia = raiz.AddComponent<CriaturaGuia>();
        guia.tipo = CriaturaGuia.TipoAcompaniante.Alebrije;
        guia.desplazamiento = new Vector3(1.1f, 1.7f, -0.7f);
        guia.velocidadFlotacion = 3.6f;
        guia.alturaFlotacion = 0.22f;
        guia.luzEspiritual = luz;
        guia.intensidadLuz = 2.4f;
        guia.colorLuzAlebrije = new Color(0.2f, 0.9f, 1f);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
        UnityEngine.Object.DestroyImmediate(raiz);
        return prefab;
    }

    static bool ReemplazarYGuardarEnEscena(GameObject prefab)
    {
        if (prefab == null) return false;

        const string rutaEscena = "Assets/Scenes/Nivel1_Bosque.unity";
        var escenaActiva = EditorSceneManager.GetActiveScene();

        // Si la escena activa no es Nivel1_Bosque, abrirla
        if (escenaActiva.path != rutaEscena)
        {
            if (File.Exists(rutaEscena))
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
                escenaActiva = EditorSceneManager.OpenScene(rutaEscena, OpenSceneMode.Single);
            }
            else
            {
                Debug.LogWarning("[Alebrije] No se encontró la escena en: " + rutaEscena);
                return false;
            }
        }

        Vector3 pos = new Vector3(-208.8f, 14.5f, -210.8f);
        Quaternion rot = Quaternion.identity;
        Transform parent = null;
        Transform chofer = null;

        var choferObj = GameObject.FindGameObjectWithTag("Player");
        if (choferObj != null) chofer = choferObj.transform;

        // 1. Eliminar instancias previas de CriaturaGuia o Criatura_Alebrije
        var viejosGuias = UnityEngine.Object.FindObjectsByType<CriaturaGuia>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var v in viejosGuias)
        {
            if (chofer == null) chofer = v.objetivoChofer;
            pos = v.transform.position;
            rot = v.transform.rotation;
            parent = v.transform.parent;
            UnityEngine.Object.DestroyImmediate(v.gameObject);
        }

        // Buscar también por nombre por si quedó algún objeto huérfano sin script
        GameObject viejoPorNombre = GameObject.Find("Criatura_Alebrije");
        while (viejoPorNombre != null)
        {
            pos = viejoPorNombre.transform.position;
            rot = viejoPorNombre.transform.rotation;
            parent = viejoPorNombre.transform.parent;
            UnityEngine.Object.DestroyImmediate(viejoPorNombre);
            viejoPorNombre = GameObject.Find("Criatura_Alebrije");
        }

        // 2. Instanciar el nuevo Prefab con el modelo 3D
        GameObject nueva = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        nueva.name = "Criatura_Alebrije";
        nueva.transform.SetPositionAndRotation(pos, rot);
        if (parent != null) nueva.transform.SetParent(parent, true);

        CriaturaGuia nuevaGuia = nueva.GetComponent<CriaturaGuia>();
        if (nuevaGuia != null && chofer != null)
        {
            nuevaGuia.objetivoChofer = chofer;
        }

        // 3. Guardar físicamente la escena en disco
        EditorSceneManager.MarkSceneDirty(escenaActiva);
        bool guardada = EditorSceneManager.SaveScene(escenaActiva);
        Debug.Log("<color=#00FFAA>[Alebrije] ¡Criatura_Alebrije reemplazada por el Colibrí 3D y Nivel1_Bosque guardada con éxito!</color>");
        return guardada;
    }
}
