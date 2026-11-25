using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Generador automático de las escenas del Nivel 2 (Panteón de Santa Paula) y Nivel 3 (Cañada y Callejones de Guanajuato).
/// Ambos niveles se ambientan auténticamente en la Sierra de Guanajuato como mapas colosales de 640 m con sendero de ~1300 m,
/// idénticos en escala, profundidad y calidad de producción al Nivel 1.
/// </summary>
public static class GeneradorNiveles2y3
{
    const string RutaModeloChofer = "Assets/Game/Models/Chofer/Meshy_AI_chofer_trailer_3d_biped_Character_output.fbx";
    const string RutaAnimController = "Assets/Game/Animations/Chofer/ChoferAnimV2.controller";
    const string RutaPrefabChofer = "Assets/Game/Resources/Chofer.prefab";

    [MenuItem("EvitandoElFin/Generar Niveles 2 y 3 Completos")]
    public static void GenerarAmbosNiveles()
    {
        AsegurarPrefabChofer();
        Nivel2PanteonSetup.Crear();
        Nivel3CiudadSetup.Crear();
    }

    [MenuItem("EvitandoElFin/Nivel 2/Generar Escena Nivel 2 (Panteon)")]
    public static void GenerarNivel2()
    {
        AsegurarPrefabChofer();
        Nivel2PanteonSetup.Crear();
    }

    [MenuItem("EvitandoElFin/Nivel 3/Generar Escena Nivel 3 (Ciudad y Callejones)")]
    public static void GenerarNivel3()
    {
        AsegurarPrefabChofer();
        Nivel3CiudadSetup.Crear();
    }

    [MenuItem("EvitandoElFin/Crear Prefab Chofer en Resources")]
    public static GameObject AsegurarPrefabChofer()
    {
        string rutaPrefab = RutaPrefabChofer;
        GameObject existente = AssetDatabase.LoadAssetAtPath<GameObject>(rutaPrefab);
        if (existente != null) return existente;

        if (!AssetDatabase.IsValidFolder("Assets/Game/Resources"))
            AssetDatabase.CreateFolder("Assets/Game", "Resources");

        GameObject root = new GameObject("Chofer");
        root.tag = "Player";
        CharacterController cc = root.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 1f, 0f);
        cc.height = 2f;
        cc.radius = 0.45f;
        cc.stepOffset = 0.7f;
        cc.slopeLimit = 60f;
        cc.radius = 0.5f;

        GameObject modeloPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaModeloChofer);
        if (modeloPrefab != null)
        {
            GameObject modelo = (GameObject)PrefabUtility.InstantiatePrefab(modeloPrefab);
            modelo.name = "ChoferModelo";
            modelo.transform.SetParent(root.transform, false);
            modelo.transform.localPosition = new Vector3(0f, -1f, 0f);
            modelo.transform.localRotation = Quaternion.identity;

            Animator anim = modelo.GetComponentInChildren<Animator>();
            if (anim == null) anim = modelo.AddComponent<Animator>();
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(RutaAnimController);
            if (controller != null) anim.runtimeAnimatorController = controller;
            Avatar av = AssetDatabase.LoadAllAssetsAtPath(RutaModeloChofer).OfType<Avatar>().FirstOrDefault();
            if (av != null) anim.avatar = av;
            anim.applyRootMotion = false;

            ChoferController cch = root.AddComponent<ChoferController>();
            cch.animator = anim;
        }

        root.AddComponent<PoderEspiritual>();
        root.AddComponent<InventarioEspiritual>();
        root.AddComponent<SistemaCombateChofer>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, rutaPrefab);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        Debug.Log("[GeneradorNiveles] Prefab Chofer creado exitosamente en " + rutaPrefab);
        return prefab;
    }

    public static void RegistrarEnBuildSettings(string rutaEscena)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!scenes.Exists(s => s.path == rutaEscena))
        {
            scenes.Add(new EditorBuildSettingsScene(rutaEscena, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[GeneradorNiveles] Registrada en Build Settings: " + rutaEscena);
        }
    }
}
