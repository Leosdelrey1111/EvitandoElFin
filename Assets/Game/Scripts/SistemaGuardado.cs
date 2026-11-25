using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Sistema de guardado y persistencia en disco (JSON / PlayerPrefs) para Evitando el Fin (Issue #25).
/// Guarda:
///  - Último nivel alcanzado y checkpoint activo dentro del nivel.
///  - Almas recolectadas y acumuladas.
///  - Tótems y coleccionables recogidos.
///  - Mejoras compradas con Don Chuy.
///  - Fragmentos de recuerdos desbloqueados en el Códice.
/// </summary>
public static class SistemaGuardado
{
    const string PrefijoClave = "EvitandoElFin_Save_v1";

    [Serializable]
    public class DatosGuardado
    {
        public string escenaActual = "Nivel1_Bosque";
        public string idUltimoCheckpoint = "";
        public float posX, posY, posZ;
        public float rotY;
        public int almasTotales = 0;
        public int totemsNivelActual = 0;
        public int nivelAlcanzado = 1;
        public int nivelVidaComprado = 0;
        public int nivelAtaqueComprado = 0;
        public int pocionesAtole = 0;
        public string checkpointsActivados = "";
        public string totemsRecogidosIds = "";
        public string recuerdosDesbloqueados = "";
        public long timestampGuardado = 0;

        // Nuevos campos para sistema de mejoras y presentaciones
        public string mejorasCompradas = ""; // "atole:3,filo:2,rebozo:1,copal:1"
        public string enemigosVistos = ""; // "Serpiente,HombrePiedra,Comadre"
    }

    private static DatosGuardado datosCache;

    public static DatosGuardado Datos
    {
        get
        {
            if (datosCache == null) Cargar();
            return datosCache;
        }
    }

    public static void Cargar()
    {
        try
        {
            string rutaArchivo = ObtenerRutaArchivo();
            if (File.Exists(rutaArchivo))
            {
                string json = File.ReadAllText(rutaArchivo);
                datosCache = JsonUtility.FromJson<DatosGuardado>(json);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[SistemaGuardado] No se pudo leer archivo de guardado: " + e.Message);
        }

        if (datosCache == null)
        {
            // Intentar PlayerPrefs como respaldo
            if (PlayerPrefs.HasKey(PrefijoClave))
            {
                string jsonPrefs = PlayerPrefs.GetString(PrefijoClave);
                datosCache = JsonUtility.FromJson<DatosGuardado>(jsonPrefs);
            }
        }

        if (datosCache == null)
        {
            datosCache = new DatosGuardado();
        }
    }

    public static void Guardar()
    {
        if (datosCache == null) datosCache = new DatosGuardado();

        datosCache.timestampGuardado = DateTime.UtcNow.Ticks;
        string json = JsonUtility.ToJson(datosCache, true);

        try
        {
            string ruta = ObtenerRutaArchivo();
            string dir = Path.GetDirectoryName(ruta);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(ruta, json);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[SistemaGuardado] Error escribiendo en disco: " + e.Message);
        }

        PlayerPrefs.SetString(PrefijoClave, json);
        PlayerPrefs.Save();
        Debug.Log("[SistemaGuardado] Progreso guardado con éxito. Checkpoint: " + datosCache.idUltimoCheckpoint);
    }

    public static void BorrarProgreso()
    {
        datosCache = new DatosGuardado();
        PlayerPrefs.DeleteKey(PrefijoClave);
        PlayerPrefs.Save();
        string ruta = ObtenerRutaArchivo();
        if (File.Exists(ruta))
        {
            try { File.Delete(ruta); } catch { }
        }
    }

    public static void RegistrarCheckpoint(string idCheckpoint, Vector3 posicion, Quaternion rotacion, string nombreEscena)
    {
        Cargar();
        datosCache.idUltimoCheckpoint = idCheckpoint;
        datosCache.escenaActual = nombreEscena;
        datosCache.posX = posicion.x;
        datosCache.posY = posicion.y;
        datosCache.posZ = posicion.z;
        datosCache.rotY = rotacion.eulerAngles.y;

        if (string.IsNullOrEmpty(datosCache.checkpointsActivados))
            datosCache.checkpointsActivados = idCheckpoint;
        else if (!datosCache.checkpointsActivados.Contains(idCheckpoint))
            datosCache.checkpointsActivados += "," + idCheckpoint;

        Guardar();
    }

    public static bool EsCheckpointActivado(string idCheckpoint)
    {
        Cargar();
        return !string.IsNullOrEmpty(datosCache.checkpointsActivados) &&
               datosCache.checkpointsActivados.Contains(idCheckpoint);
    }

    public static void RegistrarTotem(string idTotem, int totalActual)
    {
        Cargar();
        datosCache.totemsNivelActual = totalActual;
        if (string.IsNullOrEmpty(datosCache.totemsRecogidosIds))
            datosCache.totemsRecogidosIds = idTotem;
        else if (!datosCache.totemsRecogidosIds.Contains(idTotem))
            datosCache.totemsRecogidosIds += "," + idTotem;

        Guardar();
    }

    public static bool EsTotemRecogido(string idTotem)
    {
        Cargar();
        return !string.IsNullOrEmpty(datosCache.totemsRecogidosIds) &&
               datosCache.totemsRecogidosIds.Contains(idTotem);
    }

    public static void SincronizarAlmas(int almas)
    {
        Cargar();
        datosCache.almasTotales = almas;
    }

    public static void SincronizarNivel(int nivel, string escena)
    {
        Cargar();
        if (nivel > datosCache.nivelAlcanzado) datosCache.nivelAlcanzado = nivel;
        datosCache.escenaActual = escena;
        datosCache.totemsNivelActual = 0;
        datosCache.totemsRecogidosIds = "";   // nivel nuevo = tótems nuevos
        datosCache.idUltimoCheckpoint = "";
        Guardar();
    }

    public static bool HayPartidaGuardada()
    {
        Cargar();
        return !string.IsNullOrEmpty(datosCache.idUltimoCheckpoint) || datosCache.nivelAlcanzado > 1 || datosCache.almasTotales > 0;
    }

    public static Vector3 ObtenerPosicionReaparicion(Vector3 fallback)
    {
        Cargar();
        if (string.IsNullOrEmpty(datosCache.idUltimoCheckpoint)) return fallback;
        return new Vector3(datosCache.posX, datosCache.posY, datosCache.posZ);
    }

    public static Quaternion ObtenerRotacionReaparicion(Quaternion fallback)
    {
        Cargar();
        if (string.IsNullOrEmpty(datosCache.idUltimoCheckpoint)) return fallback;
        return Quaternion.Euler(0f, datosCache.rotY, 0f);
    }

    private static string ObtenerRutaArchivo()
    {
        return Path.Combine(Application.persistentDataPath, "PartidaEvitandoElFin.json");
    }

    // ================================================================== MEJORAS DE DON CHUY

    /// <summary>
    /// Guarda una mejora comprada y su nivel.
    /// </summary>
    public static void GuardarMejora(string idMejora, int nivel)
    {
        Cargar();
        
        // Parsear mejoras existentes
        System.Collections.Generic.Dictionary<string, int> mejoras = new System.Collections.Generic.Dictionary<string, int>();
        if (!string.IsNullOrEmpty(datosCache.mejorasCompradas))
        {
            string[] partes = datosCache.mejorasCompradas.Split(',');
            foreach (string parte in partes)
            {
                string[] kv = parte.Split(':');
                if (kv.Length == 2)
                {
                    int n;
                    if (int.TryParse(kv[1], out n))
                        mejoras[kv[0]] = n;
                }
            }
        }

        // Actualizar o agregar
        mejoras[idMejora] = nivel;

        // Serializar de vuelta
        System.Collections.Generic.List<string> lista = new System.Collections.Generic.List<string>();
        foreach (System.Collections.Generic.KeyValuePair<string, int> kv in mejoras)
            lista.Add(kv.Key + ":" + kv.Value);

        datosCache.mejorasCompradas = string.Join(",", lista.ToArray());
        Guardar();
    }

    /// <summary>
    /// Obtiene el nivel de una mejora (0 si no se ha comprado).
    /// </summary>
    public static int ObtenerNivelMejora(string idMejora)
    {
        Cargar();
        if (string.IsNullOrEmpty(datosCache.mejorasCompradas)) return 0;

        string[] partes = datosCache.mejorasCompradas.Split(',');
        foreach (string parte in partes)
        {
            string[] kv = parte.Split(':');
            if (kv.Length == 2 && kv[0] == idMejora)
            {
                int n;
                if (int.TryParse(kv[1], out n))
                    return n;
            }
        }
        return 0;
    }

    /// <summary>
    /// Carga todas las mejoras guardadas en un diccionario.
    /// </summary>
    public static System.Collections.Generic.Dictionary<string, int> CargarTodasMejoras()
    {
        Cargar();
        System.Collections.Generic.Dictionary<string, int> resultado = new System.Collections.Generic.Dictionary<string, int>();
        
        if (string.IsNullOrEmpty(datosCache.mejorasCompradas)) return resultado;

        string[] partes = datosCache.mejorasCompradas.Split(',');
        foreach (string parte in partes)
        {
            string[] kv = parte.Split(':');
            if (kv.Length == 2)
            {
                int n;
                if (int.TryParse(kv[1], out n))
                    resultado[kv[0]] = n;
            }
        }
        return resultado;
    }
}
