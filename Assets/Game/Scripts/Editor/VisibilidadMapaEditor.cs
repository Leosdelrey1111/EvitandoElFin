using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// 1. Quita la pantalla negra en la ventana Scene (apaga el Skybox nocturno negro, la niebla y el post-procesado oscuro en el editor,
///    y activa la luz de trabajo del editor para poder ver y editar los mapas claramente).
/// 2. Enfoca automáticamente la cámara de Scene sobre el mapa al abrir cualquier escena.
/// 3. Muestra un panel en Scene View y un botón grande en el Inspector al seleccionar cualquier escena (.unity) para abrirla y editarla.
/// </summary>
[InitializeOnLoad]
public static class VisibilidadMapaEditor
{
    static VisibilidadMapaEditor()
    {
        EditorSceneManager.sceneOpened += AlAbrirEscena;
        SceneView.duringSceneGui += DibujarPanelEnSceneView;
        Selection.selectionChanged += AlCambiarSeleccion;
        EditorApplication.delayCall += RepararVistaEscenaActual;
    }

    static void AlAbrirEscena(Scene scene, OpenSceneMode mode)
    {
        EditorApplication.delayCall += RepararVistaEscenaActual;
    }

    static void AlCambiarSeleccion()
    {
        // Si el usuario selecciona un Terrain o el Mapa_CurvasGuanajuato en la jerarquía, asegurarse de que la vista Scene esté iluminada
        if (Selection.activeGameObject != null)
        {
            if (Selection.activeGameObject.GetComponent<Terrain>() != null ||
                Selection.activeGameObject.GetComponent<GeneradorCurvasGuanajuato>() != null)
            {
                AsegurarLuzEditorEnSceneViews();
            }
        }
    }

    public static void AsegurarLuzEditorEnSceneViews()
    {
        Tools.visibleLayers = ~0;
        SceneVisibilityManager.instance.ShowAll();

        foreach (SceneView sv in SceneView.sceneViews)
        {
            if (sv == null) continue;

            // Desactivar efectos que dejan la ventana Scene completamente negra (Skybox nocturno, Fog negro y Vignette)
            var state = sv.sceneViewState;
            state.showFog = false;
            state.showSkybox = false;
            state.showImageEffects = false;
            sv.sceneViewState = state;

            // Usar iluminación de edición clara en la vista Scene (no afecta la vista Game ni el modo Play)
            sv.sceneLighting = false;
            sv.drawGizmos = true;
            sv.showGrid = true;
            sv.Repaint();
        }
    }

    [MenuItem("EvitandoElFin/🔍 Enfocar y Mostrar Mapa en Scene View")]
    public static void RepararVistaEscenaActual()
    {
        AsegurarLuzEditorEnSceneViews();

        foreach (SceneView sv in SceneView.sceneViews)
        {
            if (sv == null) continue;
            EnfocarMapaEnSceneView(sv);
            sv.Repaint();
        }
    }

    public static void EnfocarMapaEnSceneView(SceneView sv)
    {
        if (sv == null) return;

        // Si la cámara del SceneView tiene un farClipPlane muy corto, el terreno grande se recorta
        if (sv.camera != null && sv.camera.farClipPlane < 2500f)
        {
            sv.camera.farClipPlane = 5000f;
        }

        // 1. Si está el generador de la carretera (SampleScene), asegurar que su malla exista y enfocarlo
        GeneradorCurvasGuanajuato curvas = Object.FindAnyObjectByType<GeneradorCurvasGuanajuato>(FindObjectsInactive.Include);
        if (curvas != null)
        {
            MeshFilter mf = curvas.GetComponentInChildren<MeshFilter>();
            if (curvas.transform.childCount == 0 || mf == null || mf.sharedMesh == null)
            {
                curvas.Generar();
            }
            Vector3 pos = curvas.transform.position + new Vector3(0f, 4f, 35f);
            Quaternion rot = Quaternion.Euler(26f, 18f, 0f);
            sv.LookAt(pos, rot, 55f);
            return;
        }

        // 2. Si está el Chofer en la escena (Nivel1_Bosque / Nivel1_Limbo), enfocar el mapa alrededor del Chofer
        ChoferController chofer = Object.FindAnyObjectByType<ChoferController>(FindObjectsInactive.Include);
        if (chofer != null)
        {
            Vector3 pos = chofer.transform.position + Vector3.up * 3f;
            Quaternion rot = Quaternion.Euler(26f, chofer.transform.eulerAngles.y, 0f);
            sv.LookAt(pos, rot, 35f);
            return;
        }

        // 3. Si hay un Terrain, enfocar el centro del terreno a una altura visible
        Terrain t = Terrain.activeTerrain;
        if (t == null) t = Object.FindAnyObjectByType<Terrain>(FindObjectsInactive.Include);
        if (t != null && t.terrainData != null)
        {
            Vector3 centro = t.transform.position + new Vector3(t.terrainData.size.x * 0.5f, 0f, t.terrainData.size.z * 0.5f);
            centro.y = t.SampleHeight(centro) + t.transform.position.y + 5f;
            sv.LookAt(centro, Quaternion.Euler(28f, 35f, 0f), 90f);
            return;
        }
    }

    static void DibujarPanelEnSceneView(SceneView sv)
    {
        Handles.BeginGUI();

        Scene activa = EditorSceneManager.GetActiveScene();
        bool tieneTerreno = Object.FindAnyObjectByType<Terrain>(FindObjectsInactive.Include) != null;
        bool tieneCarretera = Object.FindAnyObjectByType<GeneradorCurvasGuanajuato>(FindObjectsInactive.Include) != null;
        bool esMenu = activa.name == "MainMenu" || (!tieneTerreno && !tieneCarretera);

        GUILayout.BeginArea(new Rect(52f, 10f, 460f, esMenu ? 140f : 100f), GUI.skin.box);

        GUIStyle titulo = new GUIStyle(EditorStyles.boldLabel)
        {
            normal = { textColor = new Color(0.35f, 0.95f, 0.85f) },
            fontSize = 12
        };

        GUILayout.Label("🗺️ Editor de Mapas — Escena abierta: " + (string.IsNullOrEmpty(activa.name) ? "(Sin guardar)" : activa.name), titulo);

        if (esMenu)
        {
            GUIStyle aviso = new GUIStyle(EditorStyles.wordWrappedMiniLabel)
            {
                normal = { textColor = new Color(1f, 0.85f, 0.4f) }
            };
            GUILayout.Label("⚠ MainMenu no tiene mapa 3D editable. Haz clic en uno de estos botones para abrir un mapa:", aviso);
        }

        GUILayout.BeginHorizontal();
        GUI.backgroundColor = activa.name == "SampleScene" ? new Color(0.3f, 0.9f, 0.5f) : Color.white;
        if (GUILayout.Button("🛣️ Editar Mapa Carretera (SampleScene)", GUILayout.Height(26)))
        {
            AbrirEscena("Assets/Scenes/SampleScene.unity");
        }

        GUI.backgroundColor = activa.name == "Nivel1_Bosque" ? new Color(0.3f, 0.9f, 0.5f) : Color.white;
        if (GUILayout.Button("⛰️ Editar Mapa Sierra (Nivel1_Bosque)", GUILayout.Height(26)))
        {
            AbrirEscena("Assets/Scenes/Nivel1_Bosque.unity");
        }
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("🎯 Centrar Cámara en el Mapa", GUILayout.Height(22)))
        {
            RepararVistaEscenaActual();
        }

        bool modoNoche = sv.sceneLighting;
        if (GUILayout.Button(modoNoche ? "☀️ Iluminar Todo (Modo Edición)" : "🌙 Previsualizar Noche en Editor", GUILayout.Height(22)))
        {
            sv.sceneLighting = !modoNoche;
            var st = sv.sceneViewState;
            st.showFog = !modoNoche;
            st.showSkybox = !modoNoche;
            st.showImageEffects = !modoNoche;
            sv.sceneViewState = st;
            sv.Repaint();
        }
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
        Handles.EndGUI();
    }

    public static void AbrirEscena(string ruta)
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Evitando El Fin", "Detén el modo Play antes de cambiar de escena en el editor.", "OK");
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ruta) == null)
        {
            EditorUtility.DisplayDialog("Evitando El Fin", "No se encontró la escena en: " + ruta, "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
        EditorApplication.delayCall += RepararVistaEscenaActual;
    }
}

/// <summary>
/// Cuando seleccionas un archivo de escena (.unity) con 1 solo clic en la ventana Project,
/// muestra un botón grande en el Inspector para abrirla y enfocar su mapa inmediatamente.
/// </summary>
[CustomEditor(typeof(SceneAsset))]
public class SceneAssetMapaEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        string ruta = AssetDatabase.GetAssetPath(target);
        if (string.IsNullOrEmpty(ruta) || !ruta.EndsWith(".unity")) return;

        GUI.enabled = true;
        EditorGUILayout.Space(12);
        EditorGUILayout.HelpBox(
            "En Unity, hacer 1 solo clic en una escena solo selecciona el archivo.\n" +
            "Haz doble clic en el archivo o pulsa el botón de abajo para ABRIRLA y ver el mapa en Scene View.",
            MessageType.Info);

        GUI.backgroundColor = new Color(0.25f, 0.85f, 0.45f);
        if (GUILayout.Button("▶ ABRIR ESTA ESCENA Y ENFOCAR MAPA", GUILayout.Height(36)))
        {
            VisibilidadMapaEditor.AbrirEscena(ruta);
        }
        GUI.backgroundColor = Color.white;
    }
}
