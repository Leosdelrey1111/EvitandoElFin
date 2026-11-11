using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;
using Debug = UnityEngine.Debug;

/// <summary>
/// Genera las voces del Nivel 1 (Tonalli, La Bufa y Don Chuy) con las voces de Windows, sin internet,
/// y las guarda en Assets/Game/Resources/Voz/&lt;clave&gt;.wav. Las frases salen de HistoriaNivel1.
/// Si grabas voces reales, solo reemplaza el .wav con el mismo nombre (el menú normal no sobrescribe archivos existentes).
/// </summary>
public static class GenerarVocesNivel1
{
    public const string CarpetaSalida = "Assets/Game/Resources/Voz";

    [System.Serializable] class FraseJson { public string clave, texto, voz; }
    [System.Serializable] class ManifiestoJson { public List<FraseJson> frases = new List<FraseJson>(); }

    [MenuItem("EvitandoElFin/9. Generar voces del Nivel 1 (Tonalli, La Bufa y Don Chuy)")]
    public static void Generar() { Ejecutar(false); }

    [MenuItem("EvitandoElFin/9b. Regenerar TODAS las voces (sobrescribe, incluso las grabadas)")]
    public static void Regenerar()
    {
        if (!EditorUtility.DisplayDialog("Regenerar voces",
            "Esto vuelve a crear todos los audios y SOBRESCRIBE los que hayas reemplazado por grabaciones reales. ¿Continuar?", "Regenerar", "Cancelar")) return;
        Ejecutar(true);
    }

    [MenuItem("EvitandoElFin/9c. Instalar voces neuronales (Piper, una sola vez, ~150 MB)")]
    public static void InstalarVocesNeuronales()
    {
        if (Application.platform != RuntimePlatform.WindowsEditor)
        {
            EditorUtility.DisplayDialog("Evitando El Fin", "La instalación automática solo está preparada para Windows.", "OK");
            return;
        }
        string raiz = Directory.GetParent(Application.dataPath).FullName;
        string script = Path.Combine(raiz, "Tools", "InstalarVocesNeuronales.ps1");
        if (!File.Exists(script)) { EditorUtility.DisplayDialog("Evitando El Fin", "No encuentro Tools/InstalarVocesNeuronales.ps1.", "OK"); return; }
        if (!EditorUtility.DisplayDialog("Instalar voces neuronales",
            "Necesita Python 3 e internet. Descarga Piper y dos voces en español de México (~150 MB) dentro de la carpeta Tools del proyecto.\n\nPuede tardar unos minutos. ¿Continuar?", "Instalar", "Cancelar")) return;

        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = "-NoProfile -ExecutionPolicy Bypass -File \"" + script + "\"",
            UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8
        };
        var log = new StringBuilder();
        string paso = "Preparando…";
        try
        {
            using (Process p = Process.Start(psi))
            {
                p.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) { log.AppendLine(e.Data); if (e.Data.StartsWith("PASO ")) paso = e.Data.Substring(5); } };
                p.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) log.AppendLine(e.Data); };
                p.BeginOutputReadLine(); p.BeginErrorReadLine();
                float t = 0f;
                while (!p.HasExited)
                {
                    t = (t + 0.01f) % 1f;
                    EditorUtility.DisplayProgressBar("Instalando voces neuronales", paso, t);
                    System.Threading.Thread.Sleep(200);
                }
                p.WaitForExit();
                bool ok = p.ExitCode == 0;
                Debug.Log("[Voces] " + log);
                EditorUtility.DisplayDialog("Evitando El Fin", ok
                    ? "Voces neuronales instaladas. Ahora ejecuta \"9b. Regenerar TODAS las voces\" para crear los audios con ellas."
                    : "La instalación falló:\n\n" + log, "OK");
            }
        }
        finally { EditorUtility.ClearProgressBar(); }
    }

    static void Ejecutar(bool forzar)
    {
        if (Application.platform != RuntimePlatform.WindowsEditor)
        {
            EditorUtility.DisplayDialog("Evitando El Fin", "La generación de voces usa las voces de Windows y solo funciona en el Editor de Windows.", "OK");
            return;
        }

        string raiz = Directory.GetParent(Application.dataPath).FullName;
        string script = Path.Combine(raiz, "Tools", "GenerarVoces.ps1");
        if (!File.Exists(script))
        {
            EditorUtility.DisplayDialog("Evitando El Fin", "No encuentro Tools/GenerarVoces.ps1 en la carpeta del proyecto.", "OK");
            return;
        }

        // manifiesto con todas las frases del guion
        var m = new ManifiestoJson();
        foreach (HistoriaNivel1.Frase f in HistoriaNivel1.TodasLasFrases())
            m.frases.Add(new FraseJson { clave = f.clave, texto = f.texto, voz = f.voz.ToString() });
        string manifiesto = Path.Combine(Path.GetTempPath(), "voces_evitando_el_fin.json");
        File.WriteAllText(manifiesto, JsonUtility.ToJson(m), new UTF8Encoding(false));

        string salida = Path.Combine(raiz, CarpetaSalida.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(salida);

        // Motor: voces neuronales Piper si están instaladas (mucho más naturales); si no, las voces de Windows
        string pythonPiper = Path.Combine(raiz, "Tools", "voces_piper", "venv", "Scripts", "python.exe");
        string scriptPiper = Path.Combine(raiz, "Tools", "generar_voces_piper.py");
        string modeloPiper = Path.Combine(raiz, "Tools", "voces_piper", "modelos", "es_MX-claude-high.onnx");
        bool usarPiper = File.Exists(pythonPiper) && File.Exists(scriptPiper) && File.Exists(modeloPiper);

        // Voces neuronales de Edge (las más naturales; necesitan internet): se usan si están instaladas
        string scriptEdge = Path.Combine(raiz, "Tools", "generar_voces_edge.py");
        string paqueteEdge = Path.Combine(raiz, "Tools", "voces_piper", "venv", "Lib", "site-packages", "edge_tts");
        bool usarEdge = File.Exists(pythonPiper) && File.Exists(scriptEdge) && Directory.Exists(paqueteEdge);
        bool usarPython = usarEdge || usarPiper;
        string scriptPython = usarEdge ? scriptEdge : scriptPiper;

        var psi = new ProcessStartInfo
        {
            FileName = usarPython ? pythonPiper : "powershell.exe",
            Arguments = usarPython
                ? "\"" + scriptPython + "\" \"" + manifiesto + "\" \"" + salida + "\"" + (forzar ? " --forzar" : "")
                : "-NoProfile -ExecutionPolicy Bypass -File \"" + script + "\" -Manifiesto \"" + manifiesto + "\" -Salida \"" + salida + "\"" + (forzar ? " -Forzar" : ""),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        int hechas = 0, saltadas = 0, total = m.frases.Count;
        string vozUsada = "";
        var errores = new StringBuilder();

        try
        {
            using (Process p = Process.Start(psi))
            {
                p.OutputDataReceived += (s, e) =>
                {
                    if (string.IsNullOrEmpty(e.Data)) return;
                    if (e.Data.StartsWith("OK ")) hechas++;
                    else if (e.Data.StartsWith("SALTADA ")) saltadas++;
                    else if (e.Data.StartsWith("VOZ ")) vozUsada = e.Data.Substring(4);
                    else if (e.Data.StartsWith("ERROR")) errores.AppendLine(e.Data);
                };
                p.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) errores.AppendLine(e.Data); };
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();

                while (!p.HasExited)
                {
                    EditorUtility.DisplayProgressBar("Generando voces", "Sintetizando frases (" + (hechas + saltadas) + " / " + total + ")…", Mathf.Clamp01((hechas + saltadas) / (float)Mathf.Max(1, total)));
                    System.Threading.Thread.Sleep(150);
                }
                p.WaitForExit();
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.Refresh();

        if (errores.Length > 0 && hechas + saltadas == 0)
        {
            Debug.LogError("[Voces] " + errores);
            EditorUtility.DisplayDialog("Evitando El Fin", "No se pudieron generar las voces:\n\n" + errores, "OK");
            return;
        }

        if (!usarPython) vozUsada += "  (para voces más naturales usa el menú 9c)";
        string msg = "Voz usada: " + vozUsada + "\nNuevas: " + hechas + "   ·   Ya existían: " + saltadas + "   ·   Total: " + total +
                     "\n\nLos audios están en " + CarpetaSalida + ". Tonalli ya habla al jugar; el libro (TAB) permite volver a escucharla.";
        Debug.Log("[Voces] " + msg);
        EditorUtility.DisplayDialog("Evitando El Fin", msg, "Listo");
    }
}

/// <summary>Ajustes de importación de las voces: mono y comprimidas (Vorbis) para que no pesen en el build.</summary>
public class ImportadorVoces : AssetPostprocessor
{
    void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith(GenerarVocesNivel1.CarpetaSalida)) return;
        AudioImporter ai = (AudioImporter)assetImporter;
        ai.forceToMono = true;
        ai.loadInBackground = false;
        AudioImporterSampleSettings s = ai.defaultSampleSettings;
        s.loadType = AudioClipLoadType.CompressedInMemory;
        s.compressionFormat = AudioCompressionFormat.Vorbis;
        s.quality = 0.55f;
        ai.defaultSampleSettings = s;
    }
}
