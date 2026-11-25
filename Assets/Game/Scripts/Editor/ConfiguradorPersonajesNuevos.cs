using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

/// <summary>
/// Automatizador para importar y configurar los 9 personajes nuevos en EvitandoElFin:
/// 1. La Bufa (Jefe doncella de piedra)
/// 2. Esbirro Alma (Espectro ágil)
/// 3. Esbirro Bruja 1 (Comadre hechicera)
/// 4. Esbirro Bruja 2 (Hechicera de vestido morado)
/// 5. Esbirro Calavera (Catrín / Calavera completa)
/// 6. Esbirro Calavera Femenina / Muerta (Catrina)
/// 7. Esbirro Hombre de Piedra
/// 8. Esbirro Monja (Monje inquisitorial)
/// 9. Guerrero Indígena (Comandante momia de Guanajuato)
/// </summary>
public static class ConfiguradorPersonajesNuevos
{
    public class DefinicionPersonaje
    {
        public string id;
        public string nombreLegible;
        public string carpetaModelos;
        public string carpetaAnimaciones;
        public string nombrePrefab;
        public float vida;
        public EsbirroBaseIA.TipoEsbirro tipo;
        public bool esJefe;
    }

    public static readonly DefinicionPersonaje[] Personajes = new[]
    {
        new DefinicionPersonaje
        {
            id = "Bufa",
            nombreLegible = "La Bufa (Doncella)",
            carpetaModelos = "Assets/Game/Models/Bufa",
            carpetaAnimaciones = "Assets/Game/Animations/Bufa",
            nombrePrefab = "Jefe_BufaHumana",
            vida = 700f,
            tipo = EsbirroBaseIA.TipoEsbirro.HombrePiedra,
            esJefe = true
        },
        new DefinicionPersonaje
        {
            id = "EsbirroAlma",
            nombreLegible = "Esbirro Alma Espectral",
            carpetaModelos = "Assets/Game/Models/EsbirroAlma",
            carpetaAnimaciones = "Assets/Game/Animations/EsbirroAlma",
            nombrePrefab = "Esbirro_Alma",
            vida = 35f,
            tipo = EsbirroBaseIA.TipoEsbirro.Cuervo,
            esJefe = false
        },
        new DefinicionPersonaje
        {
            id = "EsbirroBruja",
            nombreLegible = "Esbirro Bruja / Comadre",
            carpetaModelos = "Assets/Game/Models/EsbirroBruja",
            carpetaAnimaciones = "Assets/Game/Animations/EsbirroBruja",
            nombrePrefab = "Esbirro_Bruja",
            vida = 45f,
            tipo = EsbirroBaseIA.TipoEsbirro.Comadre,
            esJefe = false
        },
        new DefinicionPersonaje
        {
            id = "EsbirroBruja2",
            nombreLegible = "Esbirro Bruja Vestido Morado",
            carpetaModelos = "Assets/Game/Models/EsbirroBruja2",
            carpetaAnimaciones = "Assets/Game/Animations/EsbirroBruja2",
            nombrePrefab = "Esbirro_BrujaMorada",
            vida = 50f,
            tipo = EsbirroBaseIA.TipoEsbirro.Comadre,
            esJefe = false
        },
        new DefinicionPersonaje
        {
            id = "EsbirroCalavera",
            nombreLegible = "Esbirro Calavera / Catrín",
            carpetaModelos = "Assets/Game/Models/EsbirroCalavera",
            carpetaAnimaciones = "Assets/Game/Animations/EsbirroCalavera",
            nombrePrefab = "Esbirro_Calavera",
            vida = 40f,
            tipo = EsbirroBaseIA.TipoEsbirro.Catrin,
            esJefe = false
        },
        new DefinicionPersonaje
        {
            id = "EsbirroCalavera2",
            nombreLegible = "Esbirro Calavera Femenina / Catrina",
            carpetaModelos = "Assets/Game/Models/EsbirroCalavera2",
            carpetaAnimaciones = "Assets/Game/Animations/EsbirroCalavera2",
            nombrePrefab = "Esbirro_Catrina",
            vida = 40f,
            tipo = EsbirroBaseIA.TipoEsbirro.Catrin,
            esJefe = false
        },
        new DefinicionPersonaje
        {
            id = "EsbirroHombrePiedra",
            nombreLegible = "Esbirro Hombre de Piedra",
            carpetaModelos = "Assets/Game/Models/EsbirroHombrePiedra",
            carpetaAnimaciones = "Assets/Game/Animations/EsbirroHombrePiedra",
            nombrePrefab = "Esbirro_HombrePiedra",
            vida = 80f,
            tipo = EsbirroBaseIA.TipoEsbirro.HombrePiedra,
            esJefe = false
        },
        new DefinicionPersonaje
        {
            id = "EsbirroMonja",
            nombreLegible = "Esbirro Monja Inquisitorial",
            carpetaModelos = "Assets/Game/Models/EsbirroMonja",
            carpetaAnimaciones = "Assets/Game/Animations/EsbirroMonja",
            nombrePrefab = "Esbirro_Monja",
            vida = 55f,
            tipo = EsbirroBaseIA.TipoEsbirro.Monje,
            esJefe = false
        },
        new DefinicionPersonaje
        {
            id = "GuerreroIndigena",
            nombreLegible = "Guerrero Indígena / Comandante Momia",
            carpetaModelos = "Assets/Game/Models/GuerreroIndigena",
            carpetaAnimaciones = "Assets/Game/Animations/GuerreroIndigena",
            nombrePrefab = "Esbirro_GuerreroIndigena",
            vida = 70f,
            tipo = EsbirroBaseIA.TipoEsbirro.Momia,
            esJefe = false
        }
    };

    [MenuItem("EvitandoElFin/9. Configurar TODOS los 9 Personajes Nuevos (Automático)")]
    public static void ConfigurarTodos()
    {
        int exitosos = 0;
        int total = Personajes.Length;

        try
        {
            for (int i = 0; i < total; i++)
            {
                var def = Personajes[i];
                float progreso = (float)i / total;
                EditorUtility.DisplayProgressBar("Configurando Personajes", $"Procesando [{i + 1}/{total}]: {def.nombreLegible}...", progreso);

                bool ok = ConfigurarUnPersonaje(def);
                if (ok) exitosos++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Evitando El Fin",
                $"¡Proceso completado con éxito!\n\n" +
                $"• Personajes configurados: {exitosos} de {total}\n" +
                $"• Cada personaje tiene:\n" +
                $"  - Rigs Humanoid con retargeting de avatar\n" +
                $"  - Materiales URP Lit a color remapeados\n" +
                $"  - Animator Controllers listos para combate\n" +
                $"  - Prefabs listos en Assets/Game/Prefabs/", "¡Excelente!");
        }
        catch (Exception ex)
        {
            Debug.LogError("[ConfiguradorPersonajes] Error general: " + ex);
            EditorUtility.DisplayDialog("Error", "Ocurrió un error: " + ex.Message, "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    public static bool ConfigurarUnPersonaje(DefinicionPersonaje def)
    {
        Debug.Log($"<color=#00FFAA>[Personajes] Configurando {def.nombreLegible}...</color>");

        // 1. Encontrar archivos
        string rutaModelo = BuscarArchivo(def.carpetaModelos, "*.fbx", f => !f.ToLower().Contains("animation"));
        string rutaAnim = BuscarArchivo(def.carpetaAnimaciones, "*.fbx", f => f.ToLower().Contains("animation"));

        if (string.IsNullOrEmpty(rutaModelo))
        {
            Debug.LogWarning($"[Personajes] No se encontró modelo FBX en {def.carpetaModelos}");
            return false;
        }

        string rutaTexAlbedo = BuscarArchivo(def.carpetaModelos, "*.png", f => !f.EndsWith("_normal.png") && !f.EndsWith("_metallic.png") && !f.EndsWith("_roughness.png"));
        string rutaTexNormal = BuscarArchivo(def.carpetaModelos, "*_normal.png");
        string rutaTexMetallic = BuscarArchivo(def.carpetaModelos, "*_metallic.png");

        // 2. Texturas y Material
        Material mat = CrearMaterialURP(def, rutaTexAlbedo, rutaTexNormal, rutaTexMetallic);

        // 3. Configurar Rig del Modelo Base y Avatar
        Avatar avatar = ConfigurarModeloBase(rutaModelo, mat);

        // 4. Configurar Rig de Animaciones
        Dictionary<string, AnimationClip> clips = null;
        if (!string.IsNullOrEmpty(rutaAnim))
        {
            clips = ConfigurarAnimaciones(rutaAnim, avatar);
        }

        // 5. Construir Animator Controller
        string rutaController = $"{def.carpetaAnimaciones}/{def.id}AnimController.controller";
        AnimatorController ctrl = ConstruirController(rutaController, clips);

        // 6. Generar Prefab
        string rutaPrefab = $"Assets/Game/Prefabs/{def.nombrePrefab}.prefab";
        CrearPrefab(def, rutaModelo, avatar, mat, ctrl, rutaPrefab);

        return true;
    }

    static string BuscarArchivo(string carpeta, string patron, Func<string, bool> filtro = null)
    {
        if (!Directory.Exists(carpeta)) return null;
        string[] archivos = Directory.GetFiles(carpeta, patron);
        foreach (string a in archivos)
        {
            string norm = a.Replace("\\", "/");
            if (filtro == null || filtro(norm)) return norm;
        }
        return null;
    }

    static Material CrearMaterialURP(DefinicionPersonaje def, string rutaAlbedo, string rutaNormal, string rutaMetallic)
    {
        // Normal Map texture type
        if (!string.IsNullOrEmpty(rutaNormal))
        {
            TextureImporter ti = AssetImporter.GetAtPath(rutaNormal) as TextureImporter;
            if (ti != null && ti.textureType != TextureImporterType.NormalMap)
            {
                ti.textureType = TextureImporterType.NormalMap;
                ti.SaveAndReimport();
            }
        }

        Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        string rutaMat = $"{def.carpetaModelos}/M_{def.id}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(rutaMat);

        if (mat == null)
        {
            mat = new Material(litShader);
            mat.name = $"M_{def.id}";
            AssetDatabase.CreateAsset(mat, rutaMat);
        }

        if (!string.IsNullOrEmpty(rutaAlbedo))
        {
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(rutaAlbedo);
            if (tex != null) { mat.SetTexture("_BaseMap", tex); mat.SetTexture("_MainTex", tex); }
        }
        if (!string.IsNullOrEmpty(rutaNormal))
        {
            Texture2D norm = AssetDatabase.LoadAssetAtPath<Texture2D>(rutaNormal);
            if (norm != null) { mat.SetTexture("_BumpMap", norm); mat.EnableKeyword("_NORMALMAP"); }
        }
        if (!string.IsNullOrEmpty(rutaMetallic))
        {
            Texture2D met = AssetDatabase.LoadAssetAtPath<Texture2D>(rutaMetallic);
            if (met != null) { mat.SetTexture("_MetallicGlossMap", met); mat.EnableKeyword("_METALLICSPECGLOSSMAP"); }
        }

        mat.SetFloat("_Smoothness", 0.45f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Avatar ConfigurarModeloBase(string rutaModelo, Material mat)
    {
        ModelImporter mi = AssetImporter.GetAtPath(rutaModelo) as ModelImporter;
        if (mi != null)
        {
            bool cambio = false;
            if (mi.animationType != ModelImporterAnimationType.Human)
            {
                mi.animationType = ModelImporterAnimationType.Human;
                mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                cambio = true;
            }

            if (mat != null)
            {
                var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), "BakedMaterial");
                mi.AddRemap(id, mat);
                cambio = true;
            }

            if (cambio) mi.SaveAndReimport();
        }

        return AssetDatabase.LoadAllAssetsAtPath(rutaModelo).OfType<Avatar>().FirstOrDefault();
    }

    static Dictionary<string, AnimationClip> ConfigurarAnimaciones(string rutaAnim, Avatar avatarBase)
    {
        ModelImporter mi = AssetImporter.GetAtPath(rutaAnim) as ModelImporter;
        if (mi != null)
        {
            if (mi.animationType != ModelImporterAnimationType.Human)
            {
                mi.animationType = ModelImporterAnimationType.Human;
            }
            if (avatarBase != null && mi.sourceAvatar != avatarBase)
            {
                mi.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                mi.sourceAvatar = avatarBase;
            }
            if (!mi.importAnimation)
            {
                mi.importAnimation = true;
            }

            ModelImporterClipAnimation[] clips = mi.clipAnimations;
            if (clips == null || clips.Length == 0) clips = mi.defaultClipAnimations;

            for (int i = 0; i < clips.Length; i++)
            {
                string n = clips[i].name.ToLowerInvariant();
                bool bucle = n.Contains("walk") || n.Contains("run") || n.Contains("idle") || n.Contains("sneaky") ||
                             n.Contains("monster") || n.Contains("elderly") || n.Contains("limping") || n.Contains("unsteady");

                clips[i].loopTime = bucle;
                clips[i].loopPose = bucle;
                clips[i].lockRootPositionXZ = true;
                clips[i].lockRootRotation = true;
                clips[i].lockRootHeightY = true;
            }
            mi.clipAnimations = clips;
            mi.SaveAndReimport();
        }

        var dic = new Dictionary<string, AnimationClip>();
        var todos = AssetDatabase.LoadAllAssetsAtPath(rutaAnim).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToList();

        foreach (var c in todos)
        {
            string n = c.name.ToLowerInvariant();
            if (n.Contains("running") || (n.Contains("run") && !dic.ContainsKey("Run"))) dic["Run"] = c;
            else if (n.Contains("walking") || (n.Contains("walk") && !dic.ContainsKey("Walk"))) dic["Walk"] = c;
            else if (n.Contains("01a1841a") || n.Contains("attack") || n.Contains("slash") || n.Contains("spin"))
            {
                if (!dic.ContainsKey("Atacar")) dic["Atacar"] = c;
                else if (!dic.ContainsKey("Atacar2")) dic["Atacar2"] = c;
            }
            else if (n.Contains("01a1841c"))
            {
                if (!dic.ContainsKey("Atacar2")) dic["Atacar2"] = c;
                else if (!dic.ContainsKey("Atacar")) dic["Atacar"] = c;
            }
            else if (n.Contains("jump") && !dic.ContainsKey("Saltar")) dic["Saltar"] = c;
            else if ((n.Contains("scream") || n.Contains("shout") || n.Contains("talking") || n.Contains("alert")) && !dic.ContainsKey("Gritar")) dic["Gritar"] = c;
            else if ((n.Contains("injured") || n.Contains("unsteady") || n.Contains("01a18416")) && !dic.ContainsKey("Aturdido")) dic["Aturdido"] = c;
        }

        if (!dic.ContainsKey("Idle") && dic.ContainsKey("Walk")) dic["Idle"] = dic["Walk"];

        return dic;
    }

    static AnimatorController ConstruirController(string ruta, Dictionary<string, AnimationClip> clips)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ruta) != null)
            AssetDatabase.DeleteAsset(ruta);

        AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ruta);

        ctrl.AddParameter("Velocidad", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Atacar", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Muerte", AnimatorControllerParameterType.Trigger);
        if (clips != null && clips.ContainsKey("Atacar2")) ctrl.AddParameter("Atacar2", AnimatorControllerParameterType.Trigger);
        if (clips != null && clips.ContainsKey("Saltar")) ctrl.AddParameter("Saltar", AnimatorControllerParameterType.Trigger);
        if (clips != null && clips.ContainsKey("Gritar")) ctrl.AddParameter("Gritar", AnimatorControllerParameterType.Trigger);
        if (clips != null && clips.ContainsKey("Aturdido")) ctrl.AddParameter("Aturdido", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;

        // Locomocion
        BlendTree bt;
        AnimatorState stLoco = ctrl.CreateBlendTreeInController("Locomocion", out bt, 0);
        bt.blendType = BlendTreeType.Simple1D;
        bt.blendParameter = "Velocidad";
        bt.useAutomaticThresholds = false;

        if (clips != null)
        {
            if (clips.ContainsKey("Idle")) bt.AddChild(clips["Idle"], 0f);
            if (clips.ContainsKey("Walk")) bt.AddChild(clips["Walk"], 1f);
            if (clips.ContainsKey("Run")) bt.AddChild(clips["Run"], 2f);
        }
        sm.defaultState = stLoco;

        if (clips != null)
        {
            CrearEstadoAccion(sm, stLoco, clips, "Atacar", "Atacar", 1.1f);
            if (clips.ContainsKey("Atacar2")) CrearEstadoAccion(sm, stLoco, clips, "Atacar2", "Atacar2", 1.1f);
            if (clips.ContainsKey("Saltar")) CrearEstadoAccion(sm, stLoco, clips, "Saltar", "Saltar", 1.0f);
            if (clips.ContainsKey("Gritar")) CrearEstadoAccion(sm, stLoco, clips, "Gritar", "Gritar", 1.0f);

            if (clips.ContainsKey("Aturdido"))
            {
                AnimatorState stAturd = sm.AddState("Aturdido");
                stAturd.motion = clips["Aturdido"];
                var tr = sm.AddAnyStateTransition(stAturd);
                tr.AddCondition(AnimatorConditionMode.If, 0f, "Aturdido");
                tr.hasExitTime = false;
                tr.duration = 0.15f;

                var salida = stAturd.AddTransition(stLoco);
                salida.AddCondition(AnimatorConditionMode.IfNot, 0f, "Aturdido");
                salida.hasExitTime = false;
                salida.duration = 0.2f;
            }
        }

        // Muerte
        AnimatorState stMuerte = sm.AddState("Muerte");
        if (clips != null && clips.ContainsKey("Aturdido")) stMuerte.motion = clips["Aturdido"];
        stMuerte.speed = 0.5f;
        var trMuerte = sm.AddAnyStateTransition(stMuerte);
        trMuerte.AddCondition(AnimatorConditionMode.If, 0f, "Muerte");
        trMuerte.hasExitTime = false;
        trMuerte.duration = 0.1f;

        EditorUtility.SetDirty(ctrl);
        return ctrl;
    }

    static void CrearEstadoAccion(AnimatorStateMachine sm, AnimatorState loco, Dictionary<string, AnimationClip> clips, string clave, string trigger, float speed)
    {
        if (!clips.ContainsKey(clave)) return;
        AnimatorState st = sm.AddState(clave);
        st.motion = clips[clave];
        st.speed = speed;

        var tr = sm.AddAnyStateTransition(st);
        tr.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        tr.hasExitTime = false;
        tr.duration = 0.1f;

        var salida = st.AddTransition(loco);
        salida.hasExitTime = true;
        salida.exitTime = 0.85f;
        salida.duration = 0.15f;
    }

    static void CrearPrefab(DefinicionPersonaje def, string rutaModelo, Avatar avatar, Material mat, AnimatorController ctrl, string rutaPrefab)
    {
        GameObject raiz = new GameObject(def.nombrePrefab);
        raiz.tag = "Enemigo";

        CharacterController cc = raiz.AddComponent<CharacterController>();
        cc.height = def.esJefe ? 2.8f : 2.0f;
        cc.radius = def.esJefe ? 0.8f : 0.45f;
        cc.center = new Vector3(0f, cc.height * 0.5f, 0f);
        cc.stepOffset = 0.4f;

        PoderEspiritual vida = raiz.AddComponent<PoderEspiritual>();
        vida.maxPoder = def.vida;
        vida.actualPoder = def.vida;

        if (def.esJefe)
        {
            // Para Bufa Jefe
            var jefeComp = raiz.AddComponent<JefeClerigoInquisidor>();
            jefeComp.nombreJefe = "La Doncella de La Bufa";
            jefeComp.vidaMaxima = def.vida;
            jefeComp.danioGolpe = 28f;
        }
        else
        {
            var ia = raiz.AddComponent<EsbirroBaseIA>();
            ia.tipo = def.tipo;
            ia.velocidadPersecucion = 3.8f;
            ia.velocidadPatrulla = 1.6f;
            ia.rangoDeteccion = 10f;
            ia.rangoAtaque = def.tipo == EsbirroBaseIA.TipoEsbirro.Comadre ? 8f : 1.8f;
            ia.ataqueADistancia = def.tipo == EsbirroBaseIA.TipoEsbirro.Comadre;
            ia.danio = 16f;
        }

        GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(rutaModelo);
        if (fbxAsset != null)
        {
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset);
            visual.name = "Visual";
            visual.transform.SetParent(raiz.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            visual.transform.localRotation = Quaternion.identity;

            Animator anim = visual.GetComponent<Animator>() ?? visual.AddComponent<Animator>();
            anim.runtimeAnimatorController = ctrl;
            if (avatar != null) anim.avatar = avatar;

            if (mat != null)
            {
                foreach (var smr in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    smr.sharedMaterial = mat;
                }
            }

            if (def.esJefe)
            {
                var j = raiz.GetComponent<JefeClerigoInquisidor>();
                if (j != null) { j.animator = anim; j.modeloVisual = visual.transform; }
            }
        }

        if (!AssetDatabase.IsValidFolder("Assets/Game/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets/Game", "Prefabs");
        }

        PrefabUtility.SaveAsPrefabAsset(raiz, rutaPrefab);
        UnityEngine.Object.DestroyImmediate(raiz);
        Debug.Log($"<color=#00FFAA>[Personajes] Prefab creado con éxito: {rutaPrefab}</color>");
    }
}
