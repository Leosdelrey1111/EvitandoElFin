using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

/// <summary>
/// Herramienta de Editor para importar y configurar automáticamente al Jefe "Clérigo Inquisidor"
/// en EvitandoElFin:
/// 1. Configura el rig Humanoid y avatar del modelo base.
/// 2. Configura las texturas (Normal map en modo Normal) y genera el Material URP Lit.
/// 3. Configura el archivo de animaciones con Humanoid (copiando el Avatar) y ajusta los clips.
/// 4. Genera el AnimatorController con BlendTree y transiciones (Locomoción, Ataque, Salto, Aturdido, Muerte).
/// 5. Crea el Prefab final con CharacterController, PoderEspiritual y JefeClerigoInquisidor.
/// </summary>
public static class ConfiguradorJefeClerigo
{
    public const string CarpetaModelos = "Assets/Game/Models/Clerigo";
    public const string CarpetaAnimaciones = "Assets/Game/Animations/Clerigo";
    public const string CarpetaPrefabs = "Assets/Game/Prefabs";

    public const string RutaModeloFbx = CarpetaModelos + "/Meshy_AI_clerigo_inquisidor_3d_20261118_character_mixamo.fbx";
    public const string RutaAnimFbx = CarpetaAnimaciones + "/Meshy_AI_clerigo_inquisidor_3d_20261118_animated_mixamo_60fps_All_animation.fbx";

    public const string RutaTexAlbedo = CarpetaModelos + "/Meshy_AI_clerigo_inquisidor_3d_20261118_animated_mixamo_60fps_All_animation.png";
    public const string RutaTexNormal = CarpetaModelos + "/Meshy_AI_clerigo_inquisidor_3d_20261118_animated_mixamo_60fps_All_animation_normal.png";
    public const string RutaTexMetallic = CarpetaModelos + "/Meshy_AI_clerigo_inquisidor_3d_20261118_animated_mixamo_60fps_All_animation_metallic.png";
    public const string RutaTexRoughness = CarpetaModelos + "/Meshy_AI_clerigo_inquisidor_3d_20261118_animated_mixamo_60fps_All_animation_roughness.png";

    public const string RutaMaterial = CarpetaModelos + "/M_ClerigoInquisidor.mat";
    public const string RutaController = CarpetaAnimaciones + "/ClerigoAnimController.controller";
    public const string RutaPrefab = CarpetaPrefabs + "/Jefe_ClerigoInquisidor.prefab";

    [MenuItem("EvitandoElFin/8. Configurar Jefe Clérigo Inquisidor (Automático)")]
    public static void EjecutarConfiguracionCompleta()
    {
        try
        {
            EditorUtility.DisplayProgressBar("Configurando Jefe Clérigo", "Verificando archivos...", 0.1f);

            if (!System.IO.File.Exists(RutaModeloFbx))
            {
                EditorUtility.DisplayDialog("Evitando El Fin", "No se encontró el modelo base en: " + RutaModeloFbx, "OK");
                return;
            }

            // 1. Configurar Texturas y Material URP
            EditorUtility.DisplayProgressBar("Configurando Jefe Clérigo", "Creando material URP Lit...", 0.25f);
            Material mat = ConfigurarMaterial();

            // 2. Configurar Rig Humanoid y Material en Modelo Base
            EditorUtility.DisplayProgressBar("Configurando Jefe Clérigo", "Configurando esqueleto Humanoid y material del modelo...", 0.45f);
            Avatar avatar = ConfigurarModeloBase(mat);

            // 3. Configurar Animaciones
            EditorUtility.DisplayProgressBar("Configurando Jefe Clérigo", "Configurando clips de animación...", 0.65f);
            var clips = ConfigurarAnimaciones(avatar);

            // 4. Construir Animator Controller
            EditorUtility.DisplayProgressBar("Configurando Jefe Clérigo", "Construyendo Animator Controller...", 0.85f);
            AnimatorController ctrl = ConstruirAnimatorController(clips);

            // 5. Crear Prefab del Jefe
            EditorUtility.DisplayProgressBar("Configurando Jefe Clérigo", "Generando Prefab del Jefe...", 0.95f);
            GameObject prefab = CrearPrefab(avatar, mat, ctrl);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Evitando El Fin - Éxito",
                "¡El personaje Jefe 'Clérigo Inquisidor' ha sido configurado con éxito!\n\n" +
                "• Rigging: Humanoid asignado.\n" +
                "• Material: " + RutaMaterial + " (URP Lit con Normal, Metallic y Albedo).\n" +
                "• Animator Controller: " + RutaController + "\n" +
                "• Prefab generado: " + RutaPrefab + "\n\n" +
                "Ya puedes arrastrar el Prefab a la escena Nivel1_Bosque o colocarlo en la arena.", "¡Excelente!");
        }
        catch (Exception ex)
        {
            Debug.LogError("[ConfiguradorClerigo] Error al configurar: " + ex);
            EditorUtility.DisplayDialog("Error", "Ocurrió un error al configurar el personaje: " + ex.Message, "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    static Avatar ConfigurarModeloBase(Material mat)
    {
        ModelImporter mi = AssetImporter.GetAtPath(RutaModeloFbx) as ModelImporter;
        if (mi != null)
        {
            bool cambio = false;
            if (mi.animationType != ModelImporterAnimationType.Human)
            {
                mi.animationType = ModelImporterAnimationType.Human;
                mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                cambio = true;
            }

            // Remapear el material incrustado "BakedMaterial" a nuestro material URP con texturas
            if (mat != null)
            {
                var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), "BakedMaterial");
                mi.AddRemap(id, mat);
                cambio = true;
            }

            if (cambio)
            {
                mi.SaveAndReimport();
            }
        }
        return AssetDatabase.LoadAllAssetsAtPath(RutaModeloFbx).OfType<Avatar>().FirstOrDefault();
    }

    static Material ConfigurarMaterial()
    {
        // Asegurar que la textura normal sea NormalMap
        TextureImporter tiNormal = AssetImporter.GetAtPath(RutaTexNormal) as TextureImporter;
        if (tiNormal != null && tiNormal.textureType != TextureImporterType.NormalMap)
        {
            tiNormal.textureType = TextureImporterType.NormalMap;
            tiNormal.SaveAndReimport();
        }

        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null) litShader = Shader.Find("Standard");

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterial);
        if (mat == null)
        {
            mat = new Material(litShader);
            mat.name = "M_ClerigoInquisidor";
            AssetDatabase.CreateAsset(mat, RutaMaterial);
        }

        Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaTexAlbedo);
        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaTexNormal);
        Texture2D metallic = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaTexMetallic);

        if (albedo != null)
        {
            mat.SetTexture("_BaseMap", albedo);
            mat.SetTexture("_MainTex", albedo);
        }
        if (normal != null)
        {
            mat.SetTexture("_BumpMap", normal);
            mat.EnableKeyword("_NORMALMAP");
        }
        if (metallic != null)
        {
            mat.SetTexture("_MetallicGlossMap", metallic);
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
        }

        mat.SetFloat("_Smoothness", 0.5f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Dictionary<string, AnimationClip> ConfigurarAnimaciones(Avatar avatarBase)
    {
        ModelImporter mi = AssetImporter.GetAtPath(RutaAnimFbx) as ModelImporter;
        if (mi != null)
        {
            bool cambio = false;
            if (mi.animationType != ModelImporterAnimationType.Human)
            {
                mi.animationType = ModelImporterAnimationType.Human;
                cambio = true;
            }
            if (avatarBase != null && mi.sourceAvatar != avatarBase)
            {
                mi.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                mi.sourceAvatar = avatarBase;
                cambio = true;
            }
            if (!mi.importAnimation)
            {
                mi.importAnimation = true;
                cambio = true;
            }

            ModelImporterClipAnimation[] clips = mi.clipAnimations;
            if (clips == null || clips.Length == 0) clips = mi.defaultClipAnimations;

            for (int i = 0; i < clips.Length; i++)
            {
                string nombre = clips[i].name.ToLowerInvariant();

                bool bucle = nombre.Contains("walk") || nombre.Contains("run") || nombre.Contains("idle");
                clips[i].loopTime = bucle;
                clips[i].loopPose = bucle;
                clips[i].lockRootPositionXZ = true;
                clips[i].lockRootRotation = true;
                clips[i].lockRootHeightY = true;
            }
            mi.clipAnimations = clips;
            mi.SaveAndReimport();
        }

        var dicClips = new Dictionary<string, AnimationClip>();
        var todosClips = AssetDatabase.LoadAllAssetsAtPath(RutaAnimFbx).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToList();

        foreach (var c in todosClips)
        {
            string n = c.name.ToLowerInvariant();
            if (n.Contains("running") || (n.Contains("run") && !dicClips.ContainsKey("Run"))) dicClips["Run"] = c;
            else if (n.Contains("unsteady")) dicClips["Aturdido"] = c;
            else if (n.Contains("walking") || (n.Contains("walk") && !dicClips.ContainsKey("Walk"))) dicClips["Walk"] = c;
            else if (n.Contains("jump")) dicClips["Saltar"] = c;
            else if (n.Contains("01a1841a") || n.Contains("attack") || n.Contains("atacar")) dicClips["Atacar"] = c;
        }

        // Si no hay clip Idle nativo, se usa Walk a velocidad 0 o el clip de Walk
        if (!dicClips.ContainsKey("Idle") && dicClips.ContainsKey("Walk"))
        {
            dicClips["Idle"] = dicClips["Walk"];
        }

        return dicClips;
    }

    static AnimatorController ConstruirAnimatorController(Dictionary<string, AnimationClip> clips)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaController) != null)
            AssetDatabase.DeleteAsset(RutaController);

        AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(RutaController);

        ctrl.AddParameter("Velocidad", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Atacar", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Saltar", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Aturdido", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("Muerte", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;

        // BlendTree Locomocion (Idle / Walk / Run)
        BlendTree bt;
        AnimatorState stLoco = ctrl.CreateBlendTreeInController("Locomocion", out bt, 0);
        bt.blendType = BlendTreeType.Simple1D;
        bt.blendParameter = "Velocidad";
        bt.useAutomaticThresholds = false;

        if (clips.ContainsKey("Idle")) bt.AddChild(clips["Idle"], 0f);
        if (clips.ContainsKey("Walk")) bt.AddChild(clips["Walk"], 1f);
        if (clips.ContainsKey("Run")) bt.AddChild(clips["Run"], 2f);
        sm.defaultState = stLoco;

        // Estado Atacar
        if (clips.ContainsKey("Atacar"))
        {
            AnimatorState stAtacar = sm.AddState("Atacar");
            stAtacar.motion = clips["Atacar"];
            stAtacar.speed = 1.2f;

            var trEntrada = sm.AddAnyStateTransition(stAtacar);
            trEntrada.AddCondition(AnimatorConditionMode.If, 0f, "Atacar");
            trEntrada.hasExitTime = false;
            trEntrada.duration = 0.1f;

            var trSalida = stAtacar.AddTransition(stLoco);
            trSalida.hasExitTime = true;
            trSalida.exitTime = 0.85f;
            trSalida.duration = 0.15f;
        }

        // Estado Saltar
        if (clips.ContainsKey("Saltar"))
        {
            AnimatorState stSaltar = sm.AddState("Saltar");
            stSaltar.motion = clips["Saltar"];
            stSaltar.speed = 1.1f;

            var trEntrada = sm.AddAnyStateTransition(stSaltar);
            trEntrada.AddCondition(AnimatorConditionMode.If, 0f, "Saltar");
            trEntrada.hasExitTime = false;
            trEntrada.duration = 0.1f;

            var trSalida = stSaltar.AddTransition(stLoco);
            trSalida.hasExitTime = true;
            trSalida.exitTime = 0.9f;
            trSalida.duration = 0.15f;
        }

        // Estado Aturdido (Unsteady Walk)
        if (clips.ContainsKey("Aturdido"))
        {
            AnimatorState stAturdido = sm.AddState("Aturdido");
            stAturdido.motion = clips["Aturdido"];
            stAturdido.speed = 0.9f;

            var trEntrada = sm.AddAnyStateTransition(stAturdido);
            trEntrada.AddCondition(AnimatorConditionMode.If, 0f, "Aturdido");
            trEntrada.hasExitTime = false;
            trEntrada.duration = 0.15f;

            var trSalida = stAturdido.AddTransition(stLoco);
            trSalida.AddCondition(AnimatorConditionMode.IfNot, 0f, "Aturdido");
            trSalida.hasExitTime = false;
            trSalida.duration = 0.2f;
        }

        // Estado Muerte
        AnimatorState stMuerte = sm.AddState("Muerte");
        if (clips.ContainsKey("Aturdido")) stMuerte.motion = clips["Aturdido"];
        stMuerte.speed = 0.4f;

        var trMuerte = sm.AddAnyStateTransition(stMuerte);
        trMuerte.AddCondition(AnimatorConditionMode.If, 0f, "Muerte");
        trMuerte.hasExitTime = false;
        trMuerte.duration = 0.1f;

        EditorUtility.SetDirty(ctrl);
        return ctrl;
    }

    static GameObject CrearPrefab(Avatar avatar, Material mat, AnimatorController ctrl)
    {
        GameObject raiz = new GameObject("Jefe_ClerigoInquisidor");
        raiz.tag = "Enemigo";

        CharacterController cc = raiz.AddComponent<CharacterController>();
        cc.height = 2.4f;
        cc.radius = 0.6f;
        cc.center = new Vector3(0f, 1.2f, 0f);
        cc.stepOffset = 0.4f;

        PoderEspiritual vida = raiz.AddComponent<PoderEspiritual>();
        vida.maxPoder = 600f;
        vida.actualPoder = 600f;

        GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(RutaModeloFbx);
        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset);
        visual.name = "Visual";
        visual.transform.SetParent(raiz.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;

        Animator anim = visual.GetComponent<Animator>();
        if (anim == null) anim = visual.AddComponent<Animator>();
        anim.runtimeAnimatorController = ctrl;
        if (avatar != null) anim.avatar = avatar;

        if (mat != null)
        {
            foreach (var smr in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                smr.sharedMaterial = mat;
            }
        }

        JefeClerigoInquisidor jefe = raiz.AddComponent<JefeClerigoInquisidor>();
        jefe.animator = anim;
        jefe.modeloVisual = visual.transform;
        jefe.vidaMaxima = 600f;

        if (!AssetDatabase.IsValidFolder(CarpetaPrefabs))
        {
            AssetDatabase.CreateFolder("Assets/Game", "Prefabs");
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
        UnityEngine.Object.DestroyImmediate(raiz);
        return prefab;
    }
}
