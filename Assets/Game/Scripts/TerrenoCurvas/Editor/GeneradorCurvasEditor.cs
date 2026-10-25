#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GeneradorCurvasGuanajuato))]
public class GeneradorCurvasEditor : Editor
{
    public override void OnInspectorGUI()
    {
        GeneradorCurvasGuanajuato generador = (GeneradorCurvasGuanajuato)target;

        // Banner informativo estilizado
        EditorGUILayout.Space(10);
        GUIStyle estiloTitulo = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 15,
            alignment = TextAnchor.MiddleCenter
        };
        estiloTitulo.normal.textColor = new Color(0.1f, 0.85f, 0.65f);

        EditorGUILayout.LabelField("🏔️ CURVAS DE GUANAJUATO 🏔️", estiloTitulo);
        EditorGUILayout.LabelField("Generador de Terreno y Carretera con Curvas Semi-Animadas", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.Space(10);

        // Botones de acción principales
        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = new Color(0.2f, 0.85f, 0.45f);
        if (GUILayout.Button("▶ GENERAR CARRETERA", GUILayout.Height(38)))
        {
            Undo.RegisterFullObjectHierarchyUndo(generador.gameObject, "Generar Curvas Guanajuato");
            generador.Generar();
            EditorUtility.SetDirty(generador);
        }

        GUI.backgroundColor = new Color(0.95f, 0.35f, 0.35f);
        if (GUILayout.Button("✕ LIMPIAR", GUILayout.Height(38), GUILayout.Width(85)))
        {
            Undo.RegisterFullObjectHierarchyUndo(generador.gameObject, "Limpiar Mapa");
            generador.Limpiar();
            EditorUtility.SetDirty(generador);
        }

        EditorGUILayout.EndHorizontal();

        GUI.backgroundColor = Color.white;
        EditorGUILayout.Space(6);

        if (GUILayout.Button("↺ Restablecer Curvas Clásicas de Guanajuato"))
        {
            generador.InicializarCurvasDeGuanajuato();
            EditorUtility.SetDirty(generador);
        }

        EditorGUILayout.Space(10);

        // Dibujar el inspector normal con sus campos
        DrawDefaultInspector();

        EditorGUILayout.Space(15);
        EditorGUILayout.HelpBox(
            "Consejo para 'Evitando el Fin':\n" +
            "• El ancho de carretera recomendado es 12m para dar espacio a esquivar con Space (Dodge) y combatir enemigos.\n" +
            "• Las barreras metálicas tienen colisión para que el Chofer y los esbirros no caigan al abismo accidentalmente.\n" +
            "• La carretera incluye pulso espiritual nocturno y señalamientos de curva reflectantes animados.",
            MessageType.Info
        );
    }
}
#endif
