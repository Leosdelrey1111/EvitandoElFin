using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Activa en la escena de la carretera las mejoras de la cinemática: noche de tormenta (asfalto mojado, relámpagos, gotas en el lente),
/// el Chofer acostado en la camilla (modelo de Meshy) y las bengalas de emergencia.
/// Se ejecuta sola al montar la cinemática (menú 5) y también suelta desde el menú 5b sin tocar nada más de la escena.
/// </summary>
public static class CinematicaMejoradaSetup
{
    public const string RutaModeloChofer = "Assets/Game/Models/Chofer/Meshy_AI_chofer_trailer_3d_biped_Character_output.fbx";

    [MenuItem("EvitandoElFin/5b. Mejorar cinemática (lluvia, Chofer en la camilla, bengalas)")]
    public static void AplicarEnMenu()
    {
        if (!ReparadorEscenas.AsegurarEscenaCinematica()) return;
        CinematicaAccidenteCamion cin = Object.FindAnyObjectByType<CinematicaAccidenteCamion>(FindObjectsInactive.Include);
        if (cin == null)
        {
            EditorUtility.DisplayDialog("Evitando El Fin",
                "No encuentro la cinemática en la escena abierta.\nAbre la escena de la carretera (SampleScene) y ejecuta primero el menú 5.", "OK");
            return;
        }
        string informe = Configurar(cin);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[Cinemática] " + informe);
        EditorUtility.DisplayDialog("Evitando El Fin", informe + "\nGuarda la escena (Ctrl+S) y dale Play.", "Listo");
    }

    public static string Configurar(CinematicaAccidenteCamion cin)
    {
        var sb = new System.Text.StringBuilder();
        Undo.RecordObject(cin, "Mejorar cinemática");

        cin.lluviaNocturna = true;
        cin.bengalasDeEmergencia = true;

        AmbienteLluviaCinematica amb = cin.GetComponent<AmbienteLluviaCinematica>();
        if (amb == null) amb = Undo.AddComponent<AmbienteLluviaCinematica>(cin.gameObject);
        cin.ambienteLluvia = amb;
        sb.AppendLine("• Noche de tormenta: asfalto mojado con charcos y reflejos, salpicaduras, relámpagos con trueno, gotas en el lente, haz de los faros y look de cine.");

        GameObject chofer = AssetDatabase.LoadAssetAtPath<GameObject>(RutaModeloChofer);
        cin.prefabChoferHerido = chofer;
        sb.AppendLine(chofer != null
            ? "• El Chofer (modelo de Meshy) va acostado en la camilla."
            : "• No encontré el modelo del Chofer en " + RutaModeloChofer + ": la camilla lleva una figura provisional.");

        sb.AppendLine("• Bengalas rojas de emergencia sobre el asfalto mojado.");

        bool hayParamedico = Resources.Load<GameObject>("Cinematica/Paramedico") != null;
        sb.AppendLine(hayParamedico
            ? "• Paramédicos con modelo propio (Resources/Cinematica)."
            : "• Paramédicos provisionales con chaleco reflectante y paso animado (cámbialos por modelos de Meshy: ver Docs/Cinematica_Personajes_y_Animaciones.md).");

        EditorUtility.SetDirty(cin);
        return sb.ToString();
    }
}
