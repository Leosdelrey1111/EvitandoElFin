using System.IO;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Configura automáticamente todas las canciones en Assets/Game/Resources/Musica para que:
/// 1. Se importen en estéreo (2D), precargadas y descomprimidas en memoria (DecompressOnLoad)
///    para evitar cualquier micro-corte o silencio al reiniciar el bucle (loop).
/// 2. Tengan su Prefab listo para arrastrar y usar en Assets/Game/Prefabs/Musica/ con AudioSource.loop = true.
/// </summary>
public static class ConfiguradorMusicaEditor
{
    public const string CarpetaMusica = "Assets/Game/Resources/Musica";
    public const string CarpetaPrefabsMusica = "Assets/Game/Prefabs/Musica";
    public const string CarpetaOrigenExterna = @"C:\proyects\Barron\Canciones";

    public static readonly PistaMusica[] TodasLasPistas =
    {
        PistaMusica.MenuEvitandoElFin,
        PistaMusica.CaminoEntornoBase,
        PistaMusica.JefeNivel1Pelea,
        PistaMusica.JefeNivel2,
        PistaMusica.JefeNivel3Pelea
    };

    [MenuItem("EvitandoElFin/10. Configurar Canciones y Prefabs de Música (Loop)")]
    public static void ConfigurarCancionesYPrefabs()
    {
        try
        {
            EditorUtility.DisplayProgressBar("Configurando Canciones", "Verificando carpeta de música...", 0.1f);
            AsegurarCarpetas();
            SincronizarDesdeCarpetaExternaSiExiste();

            AssetDatabase.Refresh();

            int clipsConfigurados = 0;
            int prefabsCreados = 0;

            for (int i = 0; i < TodasLasPistas.Length; i++)
            {
                PistaMusica pista = TodasLasPistas[i];
                string nombre = ReproductorMusicaLoop.NombreArchivo(pista);
                string rutaClip = CarpetaMusica + "/" + nombre + ".ogg";

                EditorUtility.DisplayProgressBar(
                    "Configurando Canciones en Loop",
                    "Ajustando importación y prefab: " + nombre,
                    (i + 1f) / TodasLasPistas.Length);

                AudioImporter importer = AssetImporter.GetAtPath(rutaClip) as AudioImporter;
                if (importer != null)
                {
                    if (AplicarAjustesLoopSinCortes(importer))
                    {
                        importer.SaveAndReimport();
                    }
                    clipsConfigurados++;
                }

                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(rutaClip);
                if (clip != null)
                {
                    CrearOActualizarPrefabMusica(pista, clip);
                    prefabsCreados++;
                }
                else
                {
                    Debug.LogWarning("[ConfiguradorMusica] No se encontró el clip en: " + rutaClip);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Evitando El Fin - Canciones Listas",
                "Se guardaron y prepararon las canciones para funcionar en loop:\n\n" +
                "• Audios configurados: " + clipsConfigurados + " en " + CarpetaMusica + "\n" +
                "• Prefabs listos en loop: " + prefabsCreados + " en " + CarpetaPrefabsMusica + "\n\n" +
                "No se modificó ninguna escena. Cuando quieras implementarlas, puedes arrastrar el Prefab o cargar el clip con ReproductorMusicaLoop.ObtenerClip(...).",
                "¡Listo!");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    static void AsegurarCarpetas()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Game/Resources"))
            AssetDatabase.CreateFolder("Assets/Game", "Resources");
        if (!AssetDatabase.IsValidFolder(CarpetaMusica))
            AssetDatabase.CreateFolder("Assets/Game/Resources", "Musica");
        if (!AssetDatabase.IsValidFolder("Assets/Game/Prefabs"))
            AssetDatabase.CreateFolder("Assets/Game", "Prefabs");
        if (!AssetDatabase.IsValidFolder(CarpetaPrefabsMusica))
            AssetDatabase.CreateFolder("Assets/Game/Prefabs", "Musica");
    }

    static void SincronizarDesdeCarpetaExternaSiExiste()
    {
        if (!Directory.Exists(CarpetaOrigenExterna)) return;

        string raizProyecto = Directory.GetParent(Application.dataPath).FullName;
        string destinoFisico = Path.Combine(raizProyecto, CarpetaMusica.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(destinoFisico);

        foreach (string archivo in Directory.GetFiles(CarpetaOrigenExterna, "*.ogg"))
        {
            string nombre = Path.GetFileName(archivo);
            string destino = Path.Combine(destinoFisico, nombre);
            if (!File.Exists(destino))
            {
                File.Copy(archivo, destino, false);
            }
        }
    }

    public static bool AplicarAjustesLoopSinCortes(AudioImporter ai)
    {
        bool cambio = false;

        if (ai.forceToMono) { ai.forceToMono = false; cambio = true; }
        if (ai.loadInBackground) { ai.loadInBackground = false; cambio = true; }
        if (ai.ambisonic) { ai.ambisonic = false; cambio = true; }

        AudioImporterSampleSettings s = ai.defaultSampleSettings;
        // DecompressOnLoad mantiene el loop corto (14-15 s) descomprimido en RAM para que el salto
        // de la última muestra a la muestra 0 sea instantáneo y sin ningún micro-corte de decodificación.
        if (s.loadType != AudioClipLoadType.DecompressOnLoad)
        {
            s.loadType = AudioClipLoadType.DecompressOnLoad;
            cambio = true;
        }
        if (s.compressionFormat != AudioCompressionFormat.Vorbis)
        {
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            cambio = true;
        }
        if (Mathf.Abs(s.quality - 1f) > 0.01f)
        {
            s.quality = 1f;
            cambio = true;
        }
        if (!s.preloadAudioData)
        {
            s.preloadAudioData = true;
            cambio = true;
        }

        if (cambio)
        {
            ai.defaultSampleSettings = s;
        }
        return cambio;
    }

    static void CrearOActualizarPrefabMusica(PistaMusica pista, AudioClip clip)
    {
        string nombre = ReproductorMusicaLoop.NombreArchivo(pista);
        string rutaPrefab = CarpetaPrefabsMusica + "/Musica_" + nombre + ".prefab";

        GameObject go = new GameObject("Musica_" + nombre);

        AudioSource src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.loop = true;
        src.playOnAwake = true;
        src.spatialBlend = 0f;
        src.priority = 0;
        src.volume = 0.65f;

        ReproductorMusicaLoop rep = go.AddComponent<ReproductorMusicaLoop>();
        rep.pista = pista;
        rep.clip = clip;
        rep.enLoop = true;
        rep.reproducirAlIniciar = true;
        rep.volumenBase = 0.65f;
        rep.sincronizarConOpciones = true;
        rep.crossfadeLoopSegundos = 0f;

        PrefabUtility.SaveAsPrefabAsset(go, rutaPrefab);
        Object.DestroyImmediate(go);
    }
}

/// <summary>
/// Asegura automáticamente que cualquier pista agregada a Assets/Game/Resources/Musica/
/// quede importada con configuración óptima para loops continuos sin latencia ni cortes.
/// </summary>
public class ImportadorMusicaLoop : AssetPostprocessor
{
    void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith(ConfiguradorMusicaEditor.CarpetaMusica)) return;
        AudioImporter ai = (AudioImporter)assetImporter;
        ConfiguradorMusicaEditor.AplicarAjustesLoopSinCortes(ai);
    }
}
