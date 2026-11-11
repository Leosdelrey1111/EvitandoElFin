using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Evita y repara que la cinemática del accidente (camión, ambulancia, manager, lluvia) termine montada en una escena equivocada.
/// La cinemática SOLO va en SampleScene (la carretera). Si se monta en MainMenu, al abrir el menú arranca la cinemática encima.
/// </summary>
public static class ReparadorEscenas
{
    public const string RutaEscenaCinematica = "Assets/Scenes/SampleScene.unity";
    const string RutaMenu = "Assets/Scenes/MainMenu.unity";

    /// <summary>
    /// Los menús 5 y 5b llaman esto primero: si la escena abierta no es la de la carretera, ofrece abrirla (en vez de ensuciar otra escena).
    /// Devuelve false si hay que cancelar.
    /// </summary>
    public static bool AsegurarEscenaCinematica()
    {
        var activa = EditorSceneManager.GetActiveScene();
        if (activa.path == RutaEscenaCinematica) return true;

        bool abrir = EditorUtility.DisplayDialog("Evitando El Fin",
            "La cinemática del accidente va SOLO en la escena SampleScene (la carretera).\n\n" +
            "Ahora tienes abierta: " + (string.IsNullOrEmpty(activa.name) ? "(sin guardar)" : activa.name) + "\n" +
            "Si la montas aquí, la cinemática se pone a correr encima de esa escena.\n\n¿Abro SampleScene y la monto ahí?",
            "Abrir SampleScene", "Cancelar");
        if (!abrir) return false;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
        EditorSceneManager.OpenScene(RutaEscenaCinematica, OpenSceneMode.Single);
        return true;
    }

    [MenuItem("EvitandoElFin/0. Reparar MainMenu (quitar la cinemática que se coló)")]
    public static void RepararMenu()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RutaMenu) == null)
        {
            EditorUtility.DisplayDialog("Evitando El Fin", "No encuentro " + RutaMenu + ".", "OK");
            return;
        }
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Evitando El Fin", "Detén el Play antes de reparar la escena.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var escena = EditorSceneManager.OpenScene(RutaMenu, OpenSceneMode.Single);

        var aBorrar = new HashSet<GameObject>();
        var nombres = new HashSet<string> { "ManagerCinematica", "Camion_Cinematica", "Ambulancia_Rescate", "Paramedico", "Paramedica", "Camilla" };

        foreach (CinematicaAccidenteCamion c in Object.FindObjectsByType<CinematicaAccidenteCamion>(FindObjectsInactive.Include))
        {
            aBorrar.Add(c.gameObject);
            if (c.camionTransform != null) aBorrar.Add(c.camionTransform.gameObject);
            if (c.ambulanciaObjeto != null) aBorrar.Add(c.ambulanciaObjeto);
        }
        foreach (AmbienteLluviaCinematica a in Object.FindObjectsByType<AmbienteLluviaCinematica>(FindObjectsInactive.Include)) aBorrar.Add(a.gameObject);
        foreach (ActorCaminante a in Object.FindObjectsByType<ActorCaminante>(FindObjectsInactive.Include)) aBorrar.Add(a.gameObject);

        foreach (GameObject raiz in escena.GetRootGameObjects())
            if (nombres.Contains(raiz.name)) aBorrar.Add(raiz);

        // nunca tocar al menú ni a la cámara
        int n = 0;
        foreach (GameObject g in aBorrar)
        {
            if (g == null) continue;
            if (g.GetComponentInChildren<MenuMexicano>(true) != null || g.GetComponent<Camera>() != null) continue;
            Object.DestroyImmediate(g);
            n++;
        }

        // el menú debe tener EventSystem (MenuMexicano también lo asegura al arrancar)
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        Debug.Log("[Reparador] MainMenu: " + n + " objetos de la cinemática eliminados.");
        EditorUtility.DisplayDialog("Evitando El Fin",
            "Listo. Quité " + n + " objetos de la cinemática de MainMenu y guardé la escena.\n\n" +
            "La cinemática sigue en SampleScene (la carretera), que es donde debe estar.\n" +
            "Prueba: abre MainMenu y dale Play; debe verse solo el menú.", "Listo");
    }
}
