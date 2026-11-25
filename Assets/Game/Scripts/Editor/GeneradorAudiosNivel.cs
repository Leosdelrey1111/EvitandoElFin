#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Herramienta de editor para generar archivos de audio faltantes de los niveles 2 y 3.
/// Genera un reporte detallado de qué audios faltan y qué texto deben contener.
/// 
/// NOTA: Este script genera un reporte. Para generar los audios reales, se debe usar:
/// - ElevenLabs API (https://elevenlabs.io)
/// - Microsoft Azure Speech Services
/// - Google Cloud Text-to-Speech
/// - O grabar voces manualmente
/// 
/// Los archivos deben guardarse como .mp3 en: Assets/Game/Resources/Voz/
/// </summary>
public class GeneradorAudiosNivel : EditorWindow
{
    private Vector2 scrollPos;
    private bool mostrarNivel2 = true;
    private bool mostrarNivel3 = true;
    private bool mostrarDetalles = true;

    [MenuItem("EvitandoElFin/Herramientas/Reporte de Audios Faltantes")]
    static void AbrirVentana()
    {
        GeneradorAudiosNivel window = GetWindow<GeneradorAudiosNivel>("Audios Faltantes");
        window.minSize = new Vector2(600, 400);
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("GENERADOR DE AUDIOS - NIVELES 2 Y 3", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Esta herramienta genera un reporte de los audios faltantes.\n\n" +
            "Para generar los archivos de audio reales:\n" +
            "1. Usa ElevenLabs (https://elevenlabs.io) o similar\n" +
            "2. Copia el texto de cada frase del reporte\n" +
            "3. Genera el audio con la voz apropiada\n" +
            "4. Guarda como .mp3 en: Assets/Game/Resources/Voz/",
            MessageType.Info);

        GUILayout.Space(10);

        mostrarNivel2 = EditorGUILayout.Foldout(mostrarNivel2, "NIVEL 2 - Panteón de Santa Paula", true);
        mostrarNivel3 = EditorGUILayout.Foldout(mostrarNivel3, "NIVEL 3 - Ciudad de Guanajuato", true);
        mostrarDetalles = EditorGUILayout.Toggle("Mostrar Detalles Completos", mostrarDetalles);

        GUILayout.Space(10);

        if (GUILayout.Button("Generar Reporte Completo", GUILayout.Height(30)))
        {
            GenerarReporteCompleto();
        }

        if (GUILayout.Button("Verificar Audios Existentes", GUILayout.Height(30)))
        {
            VerificarAudiosExistentes();
        }

        GUILayout.Space(10);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        if (mostrarNivel2)
        {
            MostrarAudiosNivel2();
        }

        if (mostrarNivel3)
        {
            MostrarAudiosNivel3();
        }

        EditorGUILayout.EndScrollView();
    }

    void MostrarAudiosNivel2()
    {
        GUILayout.Label("═══ NIVEL 2: EL PANTEÓN DE SANTA PAULA ═══", EditorStyles.boldLabel);
        GUILayout.Space(5);

        // Obtener todas las frases del Nivel 2
        List<AudioInfo> audios = new List<AudioInfo>();

        foreach (HistoriaNivel1.Frase frase in HistoriaNivel2.TodasLasFrases())
        {
            string rutaArchivo = "Assets/Game/Resources/Voz/" + frase.clave + ".mp3";
            bool existe = File.Exists(rutaArchivo);

            audios.Add(new AudioInfo
            {
                clave = frase.clave,
                texto = frase.texto,
                perfil = frase.voz.ToString(),
                existe = existe
            });
        }

        MostrarListaAudios(audios, "Nivel2");
    }

    void MostrarAudiosNivel3()
    {
        GUILayout.Label("═══ NIVEL 3: LA CIUDAD COLONIAL ═══", EditorStyles.boldLabel);
        GUILayout.Space(5);

        // Obtener todas las frases del Nivel 3
        List<AudioInfo> audios = new List<AudioInfo>();

        foreach (HistoriaNivel1.Frase frase in HistoriaNivel3.TodasLasFrases())
        {
            string rutaArchivo = "Assets/Game/Resources/Voz/" + frase.clave + ".mp3";
            bool existe = File.Exists(rutaArchivo);

            audios.Add(new AudioInfo
            {
                clave = frase.clave,
                texto = frase.texto,
                perfil = frase.voz.ToString(),
                existe = existe
            });
        }

        MostrarListaAudios(audios, "Nivel3");
    }

    void MostrarListaAudios(List<AudioInfo> audios, string nivel)
    {
        int faltantes = 0;
        int existentes = 0;

        foreach (AudioInfo audio in audios)
        {
            if (audio.existe) existentes++;
            else faltantes++;
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label($"Total: {audios.Count} | Existentes: {existentes} | Faltantes: {faltantes}");
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);

        foreach (AudioInfo audio in audios)
        {
            Color bgColor = audio.existe ? new Color(0.2f, 0.8f, 0.2f, 0.2f) : new Color(0.8f, 0.2f, 0.2f, 0.2f);
            GUI.backgroundColor = bgColor;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = Color.white;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(audio.existe ? "✓" : "✗", GUILayout.Width(20));
            GUILayout.Label(audio.clave, EditorStyles.boldLabel, GUILayout.Width(250));
            GUILayout.Label($"[{audio.perfil}]", GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            if (mostrarDetalles || !audio.existe)
            {
                EditorGUILayout.LabelField("Texto:", EditorStyles.miniLabel);
                EditorGUILayout.SelectableLabel(audio.texto, EditorStyles.textArea, GUILayout.Height(EditorStyles.textArea.CalcHeight(new GUIContent(audio.texto), Screen.width - 50)));
            }

            if (!audio.existe)
            {
                EditorGUILayout.HelpBox($"Generar como: {audio.clave}.mp3", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(3);
        }

        GUILayout.Space(10);
    }

    void GenerarReporteCompleto()
    {
        string reporte = "REPORTE DE AUDIOS FALTANTES - EVITANDO EL FIN\n";
        reporte += "===============================================\n\n";
        reporte += "Generado: " + System.DateTime.Now.ToString() + "\n\n";

        // Nivel 2
        reporte += "═══════════════════════════════════════════════\n";
        reporte += "NIVEL 2: EL PANTEÓN DE SANTA PAULA\n";
        reporte += "═══════════════════════════════════════════════\n\n";

        foreach (HistoriaNivel1.Frase frase in HistoriaNivel2.TodasLasFrases())
        {
            string rutaArchivo = "Assets/Game/Resources/Voz/" + frase.clave + ".mp3";
            bool existe = File.Exists(rutaArchivo);

            reporte += $"[{(existe ? "✓" : "✗")}] {frase.clave}.mp3\n";
            reporte += $"    Voz: {frase.voz}\n";
            reporte += $"    Texto: \"{frase.texto}\"\n";
            if (!existe)
                reporte += "    >>> GENERAR ESTE ARCHIVO <<<\n";
            reporte += "\n";
        }

        // Nivel 3
        reporte += "\n═══════════════════════════════════════════════\n";
        reporte += "NIVEL 3: LA CIUDAD COLONIAL DE GUANAJUATO\n";
        reporte += "═══════════════════════════════════════════════\n\n";

        foreach (HistoriaNivel1.Frase frase in HistoriaNivel3.TodasLasFrases())
        {
            string rutaArchivo = "Assets/Game/Resources/Voz/" + frase.clave + ".mp3";
            bool existe = File.Exists(rutaArchivo);

            reporte += $"[{(existe ? "✓" : "✗")}] {frase.clave}.mp3\n";
            reporte += $"    Voz: {frase.voz}\n";
            reporte += $"    Texto: \"{frase.texto}\"\n";
            if (!existe)
                reporte += "    >>> GENERAR ESTE ARCHIVO <<<\n";
            reporte += "\n";
        }

        // Configuración de voces
        reporte += "\n═══════════════════════════════════════════════\n";
        reporte += "CONFIGURACIÓN DE VOCES RECOMENDADA\n";
        reporte += "═══════════════════════════════════════════════\n\n";

        reporte += "Tonalli (Guía):\n";
        reporte += "  - Voz: Masculina, cálida, sabia\n";
        reporte += "  - Edad aparente: 40-50 años\n";
        reporte += "  - Acento: Mexicano neutro\n";
        reporte += "  - Ritmo: Pausado, narrativo\n\n";

        reporte += "General Momia (Jefe Nivel 2):\n";
        reporte += "  - Voz: Masculina, militar, con eco\n";
        reporte += "  - Edad aparente: 50-60 años\n";
        reporte += "  - Acento: Mexicano formal\n";
        reporte += "  - Efecto: Reverb (eco de piedra)\n";
        reporte += "  - Ritmo: Autoritario, pausado\n\n";

        reporte += "Clérigo Inquisidor (Jefe Nivel 3):\n";
        reporte += "  - Voz: Masculina, solemne, intimidante\n";
        reporte += "  - Edad aparente: 55-65 años\n";
        reporte += "  - Acento: Español colonial\n";
        reporte += "  - Efecto: Ligero reverb (eco de catedral)\n";
        reporte += "  - Ritmo: Sentencioso, pausado\n\n";

        // Guardar reporte
        string rutaReporte = "Assets/Docs/Reporte_Audios_Faltantes.txt";
        string dirReporte = Path.GetDirectoryName(rutaReporte);
        if (!Directory.Exists(dirReporte))
            Directory.CreateDirectory(dirReporte);

        File.WriteAllText(rutaReporte, reporte);
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Reporte Generado",
            $"El reporte completo se ha guardado en:\n{rutaReporte}\n\n" +
            "Ábrelo para ver todos los audios que faltan y sus textos.",
            "Abrir Reporte", "Cerrar");

        EditorUtility.RevealInFinder(rutaReporte);
    }

    void VerificarAudiosExistentes()
    {
        int totalNivel2 = 0;
        int existentesNivel2 = 0;
        int totalNivel3 = 0;
        int existentesNivel3 = 0;

        // Verificar Nivel 2
        foreach (HistoriaNivel1.Frase frase in HistoriaNivel2.TodasLasFrases())
        {
            totalNivel2++;
            string rutaArchivo = "Assets/Game/Resources/Voz/" + frase.clave + ".mp3";
            if (File.Exists(rutaArchivo)) existentesNivel2++;
        }

        // Verificar Nivel 3
        foreach (HistoriaNivel1.Frase frase in HistoriaNivel3.TodasLasFrases())
        {
            totalNivel3++;
            string rutaArchivo = "Assets/Game/Resources/Voz/" + frase.clave + ".mp3";
            if (File.Exists(rutaArchivo)) existentesNivel3++;
        }

        int faltantesNivel2 = totalNivel2 - existentesNivel2;
        int faltantesNivel3 = totalNivel3 - existentesNivel3;
        int totalFaltantes = faltantesNivel2 + faltantesNivel3;

        string mensaje = $"VERIFICACIÓN DE AUDIOS:\n\n";
        mensaje += $"NIVEL 2 (Panteón de Santa Paula):\n";
        mensaje += $"  • Total: {totalNivel2}\n";
        mensaje += $"  • Existentes: {existentesNivel2}\n";
        mensaje += $"  • Faltantes: {faltantesNivel2}\n\n";

        mensaje += $"NIVEL 3 (Ciudad de Guanajuato):\n";
        mensaje += $"  • Total: {totalNivel3}\n";
        mensaje += $"  • Existentes: {existentesNivel3}\n";
        mensaje += $"  • Faltantes: {faltantesNivel3}\n\n";

        mensaje += $"TOTAL FALTANTE: {totalFaltantes} archivos de audio\n\n";

        if (totalFaltantes == 0)
        {
            mensaje += "✓ ¡Todos los audios están completos!";
        }
        else
        {
            mensaje += "⚠ Generar los archivos faltantes usando el reporte.";
        }

        EditorUtility.DisplayDialog("Verificación de Audios", mensaje, "OK");
    }

    class AudioInfo
    {
        public string clave;
        public string texto;
        public string perfil;
        public bool existe;
    }
}
#endif
