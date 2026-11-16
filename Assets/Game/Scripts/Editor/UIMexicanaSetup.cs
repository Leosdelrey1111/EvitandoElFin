using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>Aplica el HUD mexicano a una escena de nivel ya creada, sin rehacer el nivel.</summary>
public static class UIMexicanaSetup
{
    const string EscenaNivel1 = "Assets/Scenes/Nivel1_Bosque.unity";

    [MenuItem("EvitandoElFin/8. Aplicar HUD y Paneles Mexicanos a Nivel 1")]
    public static void AplicarANivel1()
    {
        var activa = EditorSceneManager.GetActiveScene();
        if (activa.path != EscenaNivel1)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(EscenaNivel1) == null)
            {
                EditorUtility.DisplayDialog("Evitando El Fin", "No existe Nivel1_Bosque. Ejecuta primero el menú 6.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(EscenaNivel1, OpenSceneMode.Single);
        }

        if (Object.FindAnyObjectByType<HUDMexicano>() == null)
        {
            new GameObject("HUD_Mexicano").AddComponent<HUDMexicano>();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("<color=#00FFAA>[UI] HUD mexicano aplicado a Nivel1_Bosque.</color>");
        EditorUtility.DisplayDialog("Evitando El Fin", "HUD y paneles (pausa, derrota, victoria) mexicanos aplicados a Nivel1_Bosque.\nEl HUD anterior queda oculto automáticamente.", "Listo");
    }
}
