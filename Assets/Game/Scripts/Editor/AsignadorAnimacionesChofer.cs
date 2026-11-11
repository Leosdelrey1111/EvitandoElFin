using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Ventana para identificar las animaciones de un pack de Meshy: lista cada clip con su duración, lo reproduce en una
/// vista previa del Chofer y deja elegir qué es (Salto, Golpe 1, Golpe 2, Golpe 3, Onda, Golpeado, Muerte, ...).
/// La asignación se guarda y luego "Aplicar al Chofer" arma el Animator.
/// </summary>
public class AsignadorAnimacionesChofer : EditorWindow
{
    class Fila
    {
        public AplicarAnimacionesChofer.ClipEncontrado ce;
        public int tipo;            // índice en Tipos
        public bool sugerido;       // true = asignado por orden de creación (sin confirmar)
    }

    static readonly string[] Tipos = { "", "Idle", "Walk", "Run", "Saltar", "Esquivar", "Atacar", "Atacar2", "Atacar3", "AtaqueArea", "Golpeado", "Muerte", "Despertar" };
    static readonly string[] Etiquetas =
    {
        "(sin usar)", "Idle · reposo", "Walk · caminar", "Run · correr", "Salto", "Esquive", "Golpe 1 · derecha", "Golpe 2 · revés",
        "Golpe 3 · pesado", "Onda espiritual", "Golpeado · reacción", "Muerte", "Despertar · levantarse"
    };
    // Orden en que se pidieron a Meshy (los IDs de Meshy crecen con el tiempo, así que el orden de creación = orden de ID)
    static readonly string[] OrdenSugerido = { "Saltar", "Atacar", "Atacar2", "Atacar3", "AtaqueArea", "Golpeado", "Muerte", "Despertar" };

    List<Fila> filas = new List<Fila>();
    int seleccion = -1;
    Vector2 scroll;

    // vista previa
    PreviewRenderUtility preview;
    GameObject instancia;
    string rutaInstancia;
    Bounds limites = new Bounds(Vector3.up, Vector3.one * 2f);
    float tiempo;
    bool reproduciendo = true;
    float velocidad = 1f;
    double ultimaMarca;
    float giro = 25f;

    [MenuItem("EvitandoElFin/7a. Identificar animaciones del Chofer (vista previa)")]
    public static void Abrir()
    {
        var w = GetWindow<AsignadorAnimacionesChofer>("Animaciones del Chofer");
        w.minSize = new Vector2(980f, 600f);
        w.Show();
    }

    void OnEnable()
    {
        Escanear();
        ultimaMarca = EditorApplication.timeSinceStartup;
        EditorApplication.update += Tick;
    }

    void OnDisable()
    {
        EditorApplication.update -= Tick;
        LimpiarPreview();
    }

    // ================================================================== datos

    void Escanear()
    {
        filas.Clear();
        var asignacion = AplicarAnimacionesChofer.CargarAsignacion();
        var clips = AplicarAnimacionesChofer.BuscarClips().OrderBy(c => c.clip.name, System.StringComparer.Ordinal).ToList();

        foreach (var ce in clips)
        {
            Fila f = new Fila { ce = ce };
            string tipo = AplicarAnimacionesChofer.TipoDeClip(ce.NombreArchivo, ce.clip.name, asignacion);
            f.tipo = Mathf.Max(0, System.Array.IndexOf(Tipos, tipo ?? ""));
            filas.Add(f);
        }

        // Los clips con nombre de Meshy (hash) que siguen sin asignar se proponen por orden de creación
        if (asignacion.Count == 0)
        {
            var usados = new HashSet<string>(filas.Where(f => f.tipo > 0).Select(f => Tipos[f.tipo]));
            var pendientes = OrdenSugerido.Where(t => !usados.Contains(t)).ToList();
            foreach (Fila f in filas.Where(x => x.tipo == 0 && EsHash(x.ce.clip.name)))
            {
                if (pendientes.Count == 0) break;
                f.tipo = System.Array.IndexOf(Tipos, pendientes[0]);
                f.sugerido = true;
                pendientes.RemoveAt(0);
            }
        }
        if (filas.Count > 0 && seleccion < 0) Seleccionar(0);
    }

    static bool EsHash(string nombre)
    {
        string n = nombre.TrimStart('_');
        return n.Length > 8 && System.Text.RegularExpressions.Regex.IsMatch(n, "^[0-9a-f]{6,}");
    }

    void Seleccionar(int i)
    {
        seleccion = i;
        tiempo = 0f;
        if (i >= 0 && i < filas.Count) PrepararInstancia(filas[i].ce.rutaArchivo);
    }

    // ================================================================== vista previa

    void PrepararInstancia(string rutaFbx)
    {
        if (preview == null)
        {
            preview = new PreviewRenderUtility();
            preview.cameraFieldOfView = 30f;
            preview.camera.clearFlags = CameraClearFlags.SolidColor;
            preview.camera.backgroundColor = new Color(0.1f, 0.12f, 0.2f);
            preview.camera.nearClipPlane = 0.05f;
            preview.camera.farClipPlane = 100f;
            preview.lights[0].intensity = 1.3f;
            preview.lights[0].transform.rotation = Quaternion.Euler(35f, 35f, 0f);
            preview.lights[1].intensity = 0.6f;
            preview.ambientColor = new Color(0.35f, 0.38f, 0.5f);
        }
        if (rutaInstancia == rutaFbx && instancia != null) return;

        if (instancia != null) DestroyImmediate(instancia);
        rutaInstancia = rutaFbx;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(rutaFbx);
        if (prefab == null) { instancia = null; return; }
        instancia = Instantiate(prefab);
        instancia.hideFlags = HideFlags.HideAndDontSave;
        preview.AddSingleGO(instancia);

        Renderer[] rs = instancia.GetComponentsInChildren<Renderer>();
        if (rs.Length > 0)
        {
            limites = rs[0].bounds;
            foreach (Renderer r in rs) limites.Encapsulate(r.bounds);
        }
    }

    void LimpiarPreview()
    {
        if (instancia != null) DestroyImmediate(instancia);
        instancia = null; rutaInstancia = null;
        if (preview != null) preview.Cleanup();
        preview = null;
    }

    void Tick()
    {
        double ahora = EditorApplication.timeSinceStartup;
        float dt = (float)(ahora - ultimaMarca);
        ultimaMarca = ahora;
        if (reproduciendo && seleccion >= 0 && seleccion < filas.Count)
        {
            AnimationClip c = filas[seleccion].ce.clip;
            tiempo += dt * velocidad;
            if (c.length > 0f) tiempo = Mathf.Repeat(tiempo, c.length);
            Repaint();
        }
    }

    void DibujarPreview(Rect rect)
    {
        if (Event.current.type != EventType.Repaint) return;
        if (preview == null || instancia == null || seleccion < 0) { EditorGUI.DrawRect(rect, new Color(0.1f, 0.12f, 0.2f)); return; }

        AnimationClip clip = filas[seleccion].ce.clip;
        clip.SampleAnimation(instancia, Mathf.Clamp(tiempo, 0f, clip.length));

        Vector3 centro = new Vector3(limites.center.x, limites.center.y, limites.center.z);
        float alto = Mathf.Max(1.2f, limites.size.y);
        float dist = alto * 2.1f;

        preview.BeginPreview(rect, GUIStyle.none);
        Quaternion rot = Quaternion.Euler(8f, giro, 0f);
        preview.camera.transform.position = centro + rot * new Vector3(0f, 0f, -dist);
        preview.camera.transform.LookAt(centro);
        preview.camera.Render();
        Texture tex = preview.EndPreview();
        GUI.DrawTexture(rect, tex, ScaleMode.StretchToFill, false);
    }

    // ================================================================== GUI

    void OnGUI()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Identifica cada animación del pack: selecciónala para verla y elige qué es.", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Los clips con nombre tipo \"_01a18413…\" se ordenan por fecha de creación en Meshy. Propuse la asignación en el orden en que se pidieron " +
            "(Salto, Golpe 1, Golpe 2, Golpe 3, Onda, Golpeado, Muerte, Despertar), pero CONFIRMA viéndolas: las marcadas con “?” son solo suposiciones.",
            MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            // ---------------- lista
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(520f)))
            {
                scroll = EditorGUILayout.BeginScrollView(scroll);
                for (int i = 0; i < filas.Count; i++)
                {
                    Fila f = filas[i];
                    bool sel = i == seleccion;
                    Rect fila = EditorGUILayout.BeginHorizontal(sel ? "SelectionRect" : GUIStyle.none, GUILayout.Height(30f));
                    if (GUILayout.Button(sel ? "▶" : "  ", GUILayout.Width(28f))) Seleccionar(i);
                    string nombre = f.ce.clip.name;
                    if (nombre.Length > 26) nombre = "…" + nombre.Substring(nombre.Length - 25);
                    EditorGUILayout.LabelField(new GUIContent((i + 1) + ". " + nombre, f.ce.clip.name), GUILayout.Width(190f));
                    EditorGUILayout.LabelField(f.ce.clip.length.ToString("0.0") + " s", GUILayout.Width(44f));

                    int nuevo = EditorGUILayout.Popup(f.tipo, Etiquetas, GUILayout.Width(170f));
                    if (nuevo != f.tipo) { f.tipo = nuevo; f.sugerido = false; }
                    if (f.sugerido) GUILayout.Label("?", EditorStyles.boldLabel, GUILayout.Width(14f));
                    EditorGUILayout.EndHorizontal();
                    if (Event.current.type == EventType.MouseDown && fila.Contains(Event.current.mousePosition) && Event.current.button == 0 && !sel)
                    {
                        Seleccionar(i); Event.current.Use();
                    }
                }
                EditorGUILayout.EndScrollView();
            }

            // ---------------- vista previa
            using (new EditorGUILayout.VerticalScope())
            {
                Rect r = GUILayoutUtility.GetRect(100f, 100f, 360f, 4000f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                DibujarPreview(r);

                if (seleccion >= 0 && seleccion < filas.Count)
                {
                    AnimationClip c = filas[seleccion].ce.clip;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button(reproduciendo ? "⏸ Pausa" : "▶ Reproducir", GUILayout.Width(110f))) reproduciendo = !reproduciendo;
                        float t = EditorGUILayout.Slider(tiempo, 0f, Mathf.Max(0.01f, c.length));
                        if (!Mathf.Approximately(t, tiempo)) { tiempo = t; reproduciendo = false; }
                    }
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Velocidad", GUILayout.Width(70f));
                        velocidad = EditorGUILayout.Slider(velocidad, 0.1f, 2f);
                        EditorGUILayout.LabelField("Girar", GUILayout.Width(40f));
                        giro = EditorGUILayout.Slider(giro, -180f, 180f);
                    }
                    EditorGUILayout.LabelField("Clip: " + c.name + "   (" + c.length.ToString("0.00") + " s, " + c.frameRate + " fps)", EditorStyles.miniLabel);
                }
            }
        }

        // ---------------- resumen y acciones
        EditorGUILayout.Space(6);
        var asignados = filas.Where(f => f.tipo > 0).GroupBy(f => Tipos[f.tipo]).ToList();
        foreach (var g in asignados.Where(g => g.Count() > 1))
            EditorGUILayout.HelpBox("\"" + Etiquetas[System.Array.IndexOf(Tipos, g.Key)] + "\" está asignado a " + g.Count() + " clips. Solo se usará el primero.", MessageType.Warning);
        var faltan = Tipos.Skip(1).Where(t => !asignados.Any(g => g.Key == t) && t != "Despertar" && t != "Idle").ToList();
        if (faltan.Count > 0)
            EditorGUILayout.LabelField("Sin asignar: " + string.Join(", ", faltan) + "   (se omiten o se usa la animación anterior si existe)", EditorStyles.miniLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Volver a escanear", GUILayout.Height(30f), GUILayout.Width(150f))) { Escanear(); }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Guardar asignación", GUILayout.Height(30f), GUILayout.Width(170f))) { Guardar(); ShowNotification(new GUIContent("Asignación guardada")); }
            GUI.backgroundColor = new Color(0.4f, 0.9f, 0.7f);
            if (GUILayout.Button("Aplicar al Chofer", GUILayout.Height(30f), GUILayout.Width(200f)))
            {
                Guardar();
                AplicarAnimacionesChofer.Aplicar();
                Escanear();
            }
            GUI.backgroundColor = Color.white;
        }
    }

    void Guardar()
    {
        var d = new Dictionary<string, string>();
        foreach (Fila f in filas) d[f.ce.clip.name] = Tipos[f.tipo];
        AplicarAnimacionesChofer.GuardarAsignacion(d);
        foreach (Fila f in filas) f.sugerido = false;
    }
}
