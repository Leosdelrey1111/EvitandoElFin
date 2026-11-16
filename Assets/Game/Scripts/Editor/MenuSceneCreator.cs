using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Crea la escena MainMenu: solo necesita una cámara y el componente MenuMexicano,
/// que construye todo el menú (paisaje de Guanajuato, papel picado, botones, opciones, controles y créditos) al iniciar.
/// </summary>
public static class MenuSceneCreator
{
    [MenuItem("EvitandoElFin/1. Generar Escena MainMenu")]
    public static void CrearEscenaMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Sin luces ni iluminación 3D: el menú es solo interfaz
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.04f, 0.11f);
            cam.cullingMask = 0;
        }

        GameObject menu = new GameObject("MenuMexicano");
        MenuMexicano m = menu.AddComponent<MenuMexicano>();
        m.escenaJuego = "SampleScene";

        string ruta = "Assets/Scenes/MainMenu.unity";
        EditorSceneManager.SaveScene(escena, ruta);
        ActualizarBuildSettings(ruta);
        Debug.Log("<color=#00FFAA>[MenuSceneCreator] Escena '" + ruta + "' generada. Dale Play para ver el menú mexicano.</color>");
        EditorUtility.DisplayDialog("Evitando El Fin", "Menú principal creado en " + ruta + ".\n\nDale Play para verlo: Opciones, Controles y Créditos ya están dentro.", "Listo");
    }

    private static void ActualizarBuildSettings(string rutaMenu)
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.RemoveAll(s => s.path == rutaMenu);
        scenes.Insert(0, new EditorBuildSettingsScene(rutaMenu, true));   // el menú siempre es la escena 0
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
