using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;

/// <summary>
/// Toma las animaciones del Chofer (FBX de Meshy, sueltas o en un solo pack) de las carpetas
/// Assets/Game/Animations/Meshy y Assets/Game/Animations/Chofer/Nuevas, las configura como Humanoid,
/// arma un Animator Controller completo (locomoción, salto, esquive, 3 golpes, onda, golpeado, muerte)
/// y lo asigna al Chofer del Nivel 1.
///
/// Cómo sabe qué clip es cuál:
///   1) Primero usa la asignación que hayas hecho en la ventana "Identificar animaciones" (guardada en AsignacionAnimaciones.json).
///   2) Si no hay asignación, intenta reconocer palabras clave en el nombre (Jump, Attack1, Dodge, Hit, Death...).
/// Lo que falte se rellena con las animaciones que ya existen (caminar, correr, esquivar, idle).
/// </summary>
public static class AplicarAnimacionesChofer
{
    public static readonly string[] Carpetas =
    {
        "Assets/Game/Animations/Meshy",
        "Assets/Game/Animations/Chofer/Nuevas",
    };
    public const string RutaControllerViejo = "Assets/Game/Animations/Chofer/ChoferAnimController.controller";
    public const string RutaControllerNuevo = "Assets/Game/Animations/Chofer/ChoferAnimV2.controller";
    public const string RutaAsignacion = "Assets/Game/Animations/Chofer/AsignacionAnimaciones.json";
    const string RutaEscenaNivel1 = "Assets/Scenes/Nivel1_Bosque.unity";

    // ================================================================== asignación guardada

    [Serializable] class Par { public string clip; public string tipo; }
    [Serializable] class Mapa { public List<Par> pares = new List<Par>(); }

    public static Dictionary<string, string> CargarAsignacion()
    {
        var d = new Dictionary<string, string>();
        if (!System.IO.File.Exists(RutaAsignacion)) return d;
        try
        {
            Mapa m = JsonUtility.FromJson<Mapa>(System.IO.File.ReadAllText(RutaAsignacion));
            if (m != null && m.pares != null) foreach (Par p in m.pares) d[p.clip] = p.tipo;
        }
        catch (Exception e) { Debug.LogWarning("[Animaciones] No se pudo leer la asignación: " + e.Message); }
        return d;
    }

    public static void GuardarAsignacion(Dictionary<string, string> d)
    {
        Mapa m = new Mapa();
        foreach (var kv in d) m.pares.Add(new Par { clip = kv.Key, tipo = kv.Value });
        System.IO.File.WriteAllText(RutaAsignacion, JsonUtility.ToJson(m, true));
        AssetDatabase.ImportAsset(RutaAsignacion);
    }

    /// <summary>Tipo del clip: primero la asignación guardada ("" = sin usar), luego palabras clave en el nombre.</summary>
    public static string TipoDeClip(string archivo, string nombreClip, Dictionary<string, string> asignacion)
    {
        string t;
        if (asignacion != null && asignacion.TryGetValue(nombreClip, out t)) return string.IsNullOrEmpty(t) ? null : t;
        return Clasificar(archivo + " " + nombreClip);
    }

    // ================================================================== recolección de clips

    public struct ClipEncontrado
    {
        public string rutaArchivo; public AnimationClip clip;
        public string NombreArchivo { get { return System.IO.Path.GetFileNameWithoutExtension(rutaArchivo); } }
    }

    public static List<ClipEncontrado> BuscarClips()
    {
        var lista = new List<ClipEncontrado>();
        var carpetas = Carpetas.Where(AssetDatabase.IsValidFolder).ToArray();
        if (carpetas.Length == 0) return lista;

        var rutas = new List<string>();
        rutas.AddRange(AssetDatabase.FindAssets("t:Model", carpetas).Select(AssetDatabase.GUIDToAssetPath));
        rutas.AddRange(AssetDatabase.FindAssets("t:AnimationClip", carpetas).Select(AssetDatabase.GUIDToAssetPath).Where(p => p.EndsWith(".anim")));
        foreach (string ruta in rutas.Distinct())
            foreach (AnimationClip c in AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<AnimationClip>())
            {
                if (c.name.StartsWith("__preview__")) continue;
                lista.Add(new ClipEncontrado { rutaArchivo = ruta, clip = c });
            }
        return lista;
    }

    // ================================================================== menú principal

    [MenuItem("EvitandoElFin/7. Aplicar Animaciones del Chofer")]
    public static void Aplicar()
    {
        foreach (string c in Carpetas) if (!AssetDatabase.IsValidFolder(c) && c.EndsWith("Nuevas")) AsegurarCarpeta(c);

        var asignacion = CargarAsignacion();

        // 1) Configurar importadores (Humanoid + bucle donde corresponde)
        var rutasFbx = new List<string>();
        foreach (string c in Carpetas.Where(AssetDatabase.IsValidFolder))
            rutasFbx.AddRange(AssetDatabase.FindAssets("t:Model", new[] { c }).Select(AssetDatabase.GUIDToAssetPath));
        foreach (string ruta in rutasFbx.Distinct()) ConfigurarImportador(ruta, asignacion);

        // 2) Reconocer los clips
        var clips = new Dictionary<string, AnimationClip>();
        var origen = new Dictionary<string, string>();
        var sinClasificar = new List<string>();
        foreach (ClipEncontrado ce in BuscarClips())
        {
            string tipo = TipoDeClip(ce.NombreArchivo, ce.clip.name, asignacion);
            if (tipo == null) { sinClasificar.Add(ce.NombreArchivo + " (" + ce.clip.name + ")"); continue; }
            if (!clips.ContainsKey(tipo)) { clips[tipo] = ce.clip; origen[tipo] = ce.clip.name; }
        }
        if (clips.Count == 0 && asignacion.Count == 0)
        {
            EditorUtility.DisplayDialog("Evitando El Fin",
                "No encontré animaciones reconocibles en:\n" + string.Join("\n", Carpetas) +
                "\n\nUsa el menú \"7a. Identificar animaciones\" para ver cada clip y decir cuál es cuál.", "OK");
            return;
        }

        // 3) Relleno con las animaciones que ya tenía el proyecto
        AnimationClip viejoIdle, viejoWalk, viejoRun, viejoDodge; float velDodgeViejo;
        ExtraerViejos(out viejoIdle, out viejoWalk, out viejoRun, out viejoDodge, out velDodgeViejo);
        var usadasViejas = new List<string>();
        if (!clips.ContainsKey("Idle") && viejoIdle != null) { clips["Idle"] = viejoIdle; usadasViejas.Add("Idle"); }
        if (!clips.ContainsKey("Walk") && viejoWalk != null) { clips["Walk"] = viejoWalk; usadasViejas.Add("Walk"); }
        if (!clips.ContainsKey("Run") && viejoRun != null) { clips["Run"] = viejoRun; usadasViejas.Add("Run"); }
        float velDodge = 1f;
        if (!clips.ContainsKey("Esquivar") && viejoDodge != null) { clips["Esquivar"] = viejoDodge; velDodge = velDodgeViejo; usadasViejas.Add("Esquivar"); }

        // Sin Idle real: se genera uno provisional (pose quieta con respiración) a partir del clip de caminar,
        // para que el personaje no camine en el sitio cuando está parado.
        var generadas = new List<string>();
        if (!clips.ContainsKey("Idle") && clips.ContainsKey("Walk"))
        {
            AnimationClip idleGenerado = CrearIdleDesdeCaminar(clips["Walk"]);
            if (idleGenerado != null) { clips["Idle"] = idleGenerado; generadas.Add("Idle"); }
        }

        if (!clips.ContainsKey("Idle") && !clips.ContainsKey("Walk"))
        {
            EditorUtility.DisplayDialog("Evitando El Fin", "No encontré ninguna animación de Idle/Walk (ni nuevas ni anteriores). No se puede armar el controlador.", "OK");
            return;
        }

        // 4) Construir el controlador
        AnimatorController ctrl = Construir(clips, velDodge);

        // 5) Asignarlo al Chofer del Nivel 1
        string estadoEscena = AsignarAlChofer(ctrl);

        // 6) Informe
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Controlador creado: " + RutaControllerNuevo + "\n");
        string[] todos = { "Idle", "Walk", "Run", "Saltar", "Esquivar", "Atacar", "Atacar2", "Atacar3", "AtaqueArea", "Golpeado", "Muerte", "Despertar" };
        foreach (string t in todos)
        {
            string marca = !clips.ContainsKey(t) ? "FALTA"
                : generadas.Contains(t) ? "PROVISIONAL generado desde Walk (cuando tengas un Idle de Meshy, asígnalo en 7a)"
                : (usadasViejas.Contains(t) ? "anterior" : "nuevo (" + Corto(origen[t]) + ")");
            sb.AppendLine("• " + t + ": " + marca);
        }
        if (sinClasificar.Count > 0)
        {
            sb.AppendLine("\nClips sin asignar (usa el menú 7a para decir cuál es cuál):");
            foreach (string s in sinClasificar) sb.AppendLine("  - " + Corto(s));
        }
        sb.AppendLine("\n" + estadoEscena);
        Debug.Log("[Animaciones] " + sb);
        EditorUtility.DisplayDialog("Evitando El Fin", sb.ToString(), "Listo");
    }

    static string Corto(string s) { return s != null && s.Length > 34 ? s.Substring(0, 34) + "…" : s; }

    // ================================================================== clasificación por nombre

    static bool Tiene(string t, params string[] claves) { foreach (string c in claves) if (t.Contains(c)) return true; return false; }

    public static string Clasificar(string nombre)
    {
        string t = nombre.ToLowerInvariant();
        if (Tiene(t, "wake", "despert", "levant", "getup", "get_up", "stand_up", "standup")) return "Despertar";
        if (Tiene(t, "area", "onda", "slam", "shockwave", "spell", "aoe", "smash")) return "AtaqueArea";
        if (Tiene(t, "attack3", "attack_3", "ataque3", "golpe3", "combo3", "heavy", "overhead", "pesado", "finisher")) return "Atacar3";
        if (Tiene(t, "attack2", "attack_2", "ataque2", "golpe2", "combo2", "backhand")) return "Atacar2";
        if (Tiene(t, "attack", "ataque", "golpe", "punch", "slash", "swing", "strike", "melee", "combo1", "attack1")) return "Atacar";
        if (Tiene(t, "jump", "salt")) return "Saltar";
        if (Tiene(t, "dodge", "roll", "esquiv", "rodar", "evade")) return "Esquivar";
        if (Tiene(t, "death", "die", "dead", "muerte", "muerto")) return "Muerte";
        if (Tiene(t, "hit", "hurt", "damage", "golpeado", "reaccion", "reaction", "flinch")) return "Golpeado";
        if (Tiene(t, "idle", "reposo", "breath", "stand")) return "Idle";
        if (Tiene(t, "walk", "camin")) return "Walk";
        if (Tiene(t, "run", "corr", "sprint", "jog")) return "Run";
        return null;
    }

    static void ConfigurarImportador(string ruta, Dictionary<string, string> asignacion)
    {
        ModelImporter mi = AssetImporter.GetAtPath(ruta) as ModelImporter;
        if (mi == null) return;

        bool cambio = false;
        if (mi.animationType != ModelImporterAnimationType.Human) { mi.animationType = ModelImporterAnimationType.Human; cambio = true; }
        if (mi.importAnimation == false) { mi.importAnimation = true; cambio = true; }

        string archivo = System.IO.Path.GetFileNameWithoutExtension(ruta);
        ModelImporterClipAnimation[] clips = mi.clipAnimations;
        if (clips == null || clips.Length == 0) clips = mi.defaultClipAnimations;
        for (int i = 0; i < clips.Length; i++)
        {
            string tipo = TipoDeClip(archivo, clips[i].name, asignacion);
            bool bucle = tipo == "Idle" || tipo == "Walk" || tipo == "Run";
            if (clips[i].loopTime != bucle) { clips[i].loopTime = bucle; cambio = true; }
            if (bucle && !clips[i].loopPose) { clips[i].loopPose = true; cambio = true; }
            // Animaciones "en el sitio": el movimiento lo hace el script, no el clip
            if (!clips[i].lockRootPositionXZ) { clips[i].lockRootPositionXZ = true; cambio = true; }
            if (!clips[i].lockRootRotation) { clips[i].lockRootRotation = true; cambio = true; }
        }
        mi.clipAnimations = clips;

        if (cambio) mi.SaveAndReimport();

        Avatar av = AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<Avatar>().FirstOrDefault();
        if (av != null && !av.isHuman)
            Debug.LogWarning("[Animaciones] " + ruta + ": Unity no pudo mapear el esqueleto como Humanoid. Las animaciones podrían no reproducirse en el Chofer.");
    }

    // ================================================================== Idle provisional

    public const string RutaIdleGenerado = "Assets/Game/Animations/Chofer/Idle_Generado.anim";

    static string Norm(string s) { return s.Replace(" ", "").ToLowerInvariant(); }

    /// <summary>
    /// Congela el clip de caminar en el instante en que las piernas están juntas y le añade una respiración suave
    /// (pecho, columna, cuello y hombros). Funciona con cualquier clip Humanoid porque trabaja con las curvas de músculo.
    /// </summary>
    static AnimationClip CrearIdleDesdeCaminar(AnimationClip walk)
    {
        if (walk == null) return null;
        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(walk);
        if (bindings.Length == 0) return null;

        // 1) Instante del ciclo con las piernas más juntas (muslos cerca de 0 en Front-Back)
        float mejorT = 0f, mejorPuntaje = float.MaxValue;
        var muslos = bindings.Where(b => Norm(b.propertyName).Contains("upperleg") && Norm(b.propertyName).Contains("front-back")).ToArray();
        if (muslos.Length > 0)
        {
            const int pasos = 120;
            for (int i = 0; i < pasos; i++)
            {
                float t = walk.length * i / pasos;
                float puntaje = 0f;
                foreach (EditorCurveBinding b in muslos) puntaje += Mathf.Abs(AnimationUtility.GetEditorCurve(walk, b).Evaluate(t));
                if (puntaje < mejorPuntaje) { mejorPuntaje = puntaje; mejorT = t; }
            }
        }

        // 2) Clip nuevo: valor constante de cada curva + respiración
        const float duracion = 4f;
        const int muestras = 16;
        AnimationClip idle = new AnimationClip { name = "Idle_Generado", frameRate = 30f };

        foreach (EditorCurveBinding b in bindings)
        {
            float v = AnimationUtility.GetEditorCurve(walk, b).Evaluate(mejorT);
            string n = Norm(b.propertyName);

            float amplitud = 0f, fase = 0f;
            if (n == "spinefront-back") { amplitud = 0.035f; }
            else if (n == "chestfront-back") { amplitud = 0.05f; fase = 0.35f; }
            else if (n == "upperchestfront-back") { amplitud = 0.05f; fase = 0.5f; }
            else if (n.Contains("neck") && n.Contains("nod")) { amplitud = 0.03f; fase = 0.8f; }
            else if (n.Contains("shoulder") && n.Contains("down-up")) { amplitud = 0.03f; fase = 0.5f; }

            Keyframe[] keys;
            if (amplitud > 0f)
            {
                keys = new Keyframe[muestras + 1];
                for (int i = 0; i <= muestras; i++)
                {
                    float k = (float)i / muestras;
                    keys[i] = new Keyframe(k * duracion, v + amplitud * Mathf.Sin((k - fase) * 2f * Mathf.PI) - amplitud * Mathf.Sin(-fase * 2f * Mathf.PI));
                }
            }
            else keys = new[] { new Keyframe(0f, v), new Keyframe(duracion, v) };

            AnimationCurve curva = new AnimationCurve(keys);
            if (amplitud > 0f) for (int i = 0; i < curva.length; i++) curva.SmoothTangents(i, 0f);
            AnimationUtility.SetEditorCurve(idle, b, curva);
        }

        AnimationClipSettings ajustes = AnimationUtility.GetAnimationClipSettings(idle);
        ajustes.loopTime = true;
        ajustes.startTime = 0f;
        ajustes.stopTime = duracion;
        AnimationUtility.SetAnimationClipSettings(idle, ajustes);

        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(RutaIdleGenerado) != null) AssetDatabase.DeleteAsset(RutaIdleGenerado);
        AssetDatabase.CreateAsset(idle, RutaIdleGenerado);
        AssetDatabase.SaveAssets();
        return AssetDatabase.LoadAssetAtPath<AnimationClip>(RutaIdleGenerado);
    }

    static void ExtraerViejos(out AnimationClip idle, out AnimationClip walk, out AnimationClip run, out AnimationClip dodge, out float velDodge)
    {
        idle = walk = run = dodge = null; velDodge = 1.8f;
        AnimatorController viejo = AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControllerViejo);
        if (viejo == null || viejo.layers.Length == 0) return;
        foreach (ChildAnimatorState cs in viejo.layers[0].stateMachine.states)
        {
            if (cs.state.motion is BlendTree)
            {
                BlendTree bt = (BlendTree)cs.state.motion;
                ChildMotion[] hijos = bt.children;
                if (hijos.Length > 0) idle = hijos[0].motion as AnimationClip;
                if (hijos.Length > 1) walk = hijos[1].motion as AnimationClip;
                if (hijos.Length > 2) run = hijos[2].motion as AnimationClip;
            }
            else if (cs.state.name == "Esquivar")
            {
                dodge = cs.state.motion as AnimationClip;
                velDodge = cs.state.speed;
            }
        }
    }

    // ================================================================== controlador

    static AnimatorController Construir(Dictionary<string, AnimationClip> clips, float velDodgeViejo)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControllerNuevo) != null) AssetDatabase.DeleteAsset(RutaControllerNuevo);
        AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(RutaControllerNuevo);

        ctrl.AddParameter("Velocidad", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("EnSuelo", AnimatorControllerParameterType.Bool);
        foreach (string t in new[] { "Saltar", "Esquivar", "Atacar", "Atacar2", "Atacar3", "AtaqueArea", "Golpeado", "Muerte", "Despertar" })
            ctrl.AddParameter(t, AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;

        // Locomoción: Idle (0) - Walk (1) - Run (2)
        BlendTree bt;
        AnimatorState loco = ctrl.CreateBlendTreeInController("Locomocion", out bt, 0);
        bt.blendType = BlendTreeType.Simple1D;
        bt.blendParameter = "Velocidad";
        bt.useAutomaticThresholds = false;
        if (clips.ContainsKey("Idle")) bt.AddChild(clips["Idle"], 0f);
        if (clips.ContainsKey("Walk")) bt.AddChild(clips["Walk"], 1f);
        if (clips.ContainsKey("Run")) bt.AddChild(clips["Run"], 2f);
        sm.defaultState = loco;

        // Acciones. Meshy no genera clips de menos de 2 s, así que se asume que el movimiento útil ocurre al inicio del clip
        // (segundosAccion) y se acelera / corta para que dure lo mismo que la acción en el código (duracionObjetivo).
        Accion(sm, loco, clips, "Saltar", "Saltar", 0.95f, 1.2f, 1f, true);
        bool dodgeViejo = clips.ContainsKey("Esquivar") && velDodgeViejo > 1f;
        Accion(sm, loco, clips, "Esquivar", "Esquivar", dodgeViejo ? 0f : 0.4f, 1.0f, velDodgeViejo, true);
        Accion(sm, loco, clips, "Atacar", "Atacar1", 0.34f, 1.0f, 1f, true);
        Accion(sm, loco, clips, "Atacar2", "Atacar2", 0.34f, 1.0f, 1f, true);
        Accion(sm, loco, clips, "Atacar3", "Atacar3", 0.52f, 1.3f, 1f, true);
        Accion(sm, loco, clips, "AtaqueArea", "AtaqueArea", 0.5f, 1.4f, 1f, true);
        Accion(sm, loco, clips, "Golpeado", "Golpeado", 0.4f, 0.8f, 1f, true);
        Accion(sm, loco, clips, "Despertar", "Despertar", 0f, 0f, 1f, true);
        Accion(sm, loco, clips, "Muerte", "Muerte", 0f, 0f, 1f, false);

        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();
        return ctrl;
    }

    static void Accion(AnimatorStateMachine sm, AnimatorState loco, Dictionary<string, AnimationClip> clips, string tipo, string nombreEstado, float duracionObjetivo, float segundosAccion, float velocidadBase, bool vuelve)
    {
        AnimationClip clip;
        if (!clips.TryGetValue(tipo, out clip) || clip == null) return;

        AnimatorState st = sm.AddState(nombreEstado);
        st.motion = clip;

        float largo = Mathf.Max(0.1f, clip.length);
        float accion = segundosAccion > 0f ? Mathf.Min(segundosAccion, largo) : largo;
        st.speed = duracionObjetivo > 0f ? Mathf.Clamp(accion / duracionObjetivo, 0.6f, 3f) : velocidadBase;

        AnimatorStateTransition entrada = sm.AddAnyStateTransition(st);
        entrada.AddCondition(AnimatorConditionMode.If, 0f, tipo);
        entrada.hasExitTime = false;
        entrada.duration = 0.08f;                 // fundido un poco más largo = movimientos menos "a tirones"
        entrada.canTransitionToSelf = true;       // se puede repetir el mismo golpe / acción sin esperar a que termine
        entrada.interruptionSource = TransitionInterruptionSource.Destination;

        if (vuelve)
        {
            AnimatorStateTransition salida = st.AddTransition(loco);
            salida.hasExitTime = true;
            salida.interruptionSource = TransitionInterruptionSource.Destination;
            // Salir cuando termina la parte útil del clip (el resto del clip de Meshy es solo relleno hasta los 2 s mínimos)
            float salir = duracionObjetivo > 0f ? Mathf.Clamp(accion / largo, 0.2f, 0.95f) : 0.88f;
            salida.exitTime = salir;
            salida.duration = 0.16f;
        }
    }

    // ================================================================== escena

    static string AsignarAlChofer(AnimatorController ctrl)
    {
        var activa = EditorSceneManager.GetActiveScene();
        bool abrirNivel = activa.path != RutaEscenaNivel1;
        if (abrirNivel)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RutaEscenaNivel1) == null)
                return "El controlador está listo. Cuando crees Nivel1_Bosque (menú 6) se asignará solo.";
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return "El controlador está listo, pero no se asignó a la escena (cancelaste el guardado).";
            EditorSceneManager.OpenScene(RutaEscenaNivel1, OpenSceneMode.Single);
        }

        int asignados = 0;
        foreach (ChoferController ch in UnityEngine.Object.FindObjectsByType<ChoferController>(FindObjectsInactive.Include))
        {
            Animator anim = ch.GetComponentInChildren<Animator>(true);
            if (anim == null) continue;
            anim.runtimeAnimatorController = ctrl;
            anim.applyRootMotion = false;
            ch.animator = anim;
            EditorUtility.SetDirty(anim);
            asignados++;
        }
        if (asignados > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
        return asignados > 0 ? "Asignado al Chofer de Nivel1_Bosque y escena guardada." : "No encontré un Chofer en la escena abierta.";
    }

    static void AsegurarCarpeta(string ruta)
    {
        string[] partes = ruta.Split('/');
        string actual = partes[0];
        for (int i = 1; i < partes.Length; i++)
        {
            string sig = actual + "/" + partes[i];
            if (!AssetDatabase.IsValidFolder(sig)) AssetDatabase.CreateFolder(actual, partes[i]);
            actual = sig;
        }
    }
}
