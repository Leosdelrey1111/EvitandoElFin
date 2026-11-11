using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Deja limpia la escena de la carretera (SampleScene) para la cinemática del accidente:
///  - Borra duplicados (incluidos los INACTIVOS) del camión, ambulancia y manager.
///  - Quita elementos extra de gameplay (tótems, gemas, criatura, chofer, UI, plano de prueba, señales, vegetación procedural...).
///  - Hunde/aplana el Terrain bajo la carretera (el terreno tapaba el asfalto) conservando las montañas.
///  - Aplica atmósfera nocturna (cielo, luna, niebla).
/// Se ejecuta desde "EvitandoElFin > 5. Montar Cinemática del Accidente".
/// </summary>
public static class EscenaCarreteraSetup
{
    // Opciones: ponlas en false si quieres conservar alguno de estos elementos
    const bool QuitarSenalesYChevrons = true;
    const bool QuitarVegetacionProcedural = true;
    const bool QuitarColinasProcedurales = true; // las "lomas" de malla del generador (el paisaje es el Terrain)

    const string RutaCieloNoche = "Assets/AllSkyFree/Night MoonBurst/Night Moon Burst.mat";
    const string RutaCieloNocheAlt = "Assets/AllSkyFree/Cartoon Base NightSky/Cartoon Base NightSky.mat";

    static readonly string[] RaicesABorrar =
    {
        "Camion_Cinematica", "Ambulancia_Rescate", "ManagerCinematica", "Arena_Espiritual_Nivel1",
        "Canvas_Gameplay", "TextoNarrativo", "PanelFadeNegro", "HUD_Espiritual", "ManagerUI_Gameplay",
        "ElementosNivel1", "Criatura_Alebrije", "Chofer", "Plane", "GameManager"
    };

    public static Material CargarCieloNoche()
    {
        Material m = AssetDatabase.LoadAssetAtPath<Material>(RutaCieloNoche);
        if (m == null) m = AssetDatabase.LoadAssetAtPath<Material>(RutaCieloNocheAlt);
        return m;
    }

    public static string Preparar()
    {
        System.Text.StringBuilder log = new System.Text.StringBuilder();

        // ---------------------------------------------------------------- 1. borrar duplicados y extras
        int borrados = BorrarRaices(log);

        // ---------------------------------------------------------------- 2. limpiar el mapa generado
        GeneradorCurvasGuanajuato gen = Object.FindAnyObjectByType<GeneradorCurvasGuanajuato>(FindObjectsInactive.Include);
        if (gen != null)
        {
            gen.transform.position = Vector3.zero;
            gen.transform.rotation = Quaternion.identity;
            gen.transform.localScale = Vector3.one;

            // El mapa es una instancia de prefab: hay que desempaquetarla para poder borrar partes
            GameObject raizPrefab = PrefabUtility.IsPartOfPrefabInstance(gen.gameObject)
                ? PrefabUtility.GetOutermostPrefabInstanceRoot(gen.gameObject) : null;
            if (raizPrefab != null)
            {
                PrefabUtility.UnpackPrefabInstance(raizPrefab, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                log.AppendLine("• Mapa_CurvasGuanajuato desempaquetado del prefab (para poder limpiarlo).");
            }

            // Evita que el generador se regenere solo (Start lo hace si no tiene hijos)
            if (QuitarColinasProcedurales) BorrarHijo(gen.transform, "Paisaje_Sierra_Guanajuato", log);
            if (QuitarVegetacionProcedural) BorrarHijo(gen.transform, "Vegetacion_Sierra_Guanajuato", log);
            if (QuitarSenalesYChevrons) BorrarHijo(gen.transform, "Ambiente_SemiAnimado_Sierra", log);
            BorrarHijo(gen.transform, "Tunel_Colonial_Guanajuato", log);
            BorrarHijo(gen.transform, "Pueblito_Guanajuato_Valle", log);
            gen.generarMontaniasYVegetacion = false;
            gen.agregarSenalesChevrons = false;
        }
        else
        {
            log.AppendLine("⚠ No se encontró GeneradorCurvasGuanajuato; no se pudo limpiar ni hundir el terreno bajo la carretera.");
        }

        // ---------------------------------------------------------------- 3. terreno bajo la carretera
        if (gen != null) log.AppendLine(AjustarTerrenoALaCarretera(gen));

        // ---------------------------------------------------------------- 4. noche
        AplicarNoche();
        log.AppendLine("• Atmósfera nocturna aplicada (cielo, luna, niebla, ambiente).");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        log.Insert(0, "Objetos eliminados (duplicados y extras): " + borrados + "\n");
        return log.ToString();
    }

    // ================================================================== borrado

    static int BorrarRaices(System.Text.StringBuilder log)
    {
        int total = 0;
        var nombres = new HashSet<string>(RaicesABorrar);
        var aBorrar = new List<GameObject>();

        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (t == null || t.parent != null) continue;                 // solo raíces
            if (!t.gameObject.scene.IsValid() || t.gameObject.scene != EditorSceneManager.GetActiveScene()) continue;
            if (nombres.Contains(t.name)) aBorrar.Add(t.gameObject);
        }

        foreach (GameObject go in aBorrar)
        {
            if (go == null) continue;
            log.AppendLine("• Eliminado: " + go.name + (go.activeSelf ? "" : " (inactivo)"));
            Undo.DestroyObjectImmediate(go);
            total++;
        }
        return total;
    }

    static void BorrarHijo(Transform padre, string nombre, System.Text.StringBuilder log)
    {
        Transform h = padre.Find(nombre);
        if (h == null) return;
        log.AppendLine("• Eliminado del mapa: " + nombre);
        Undo.DestroyObjectImmediate(h.gameObject);
    }

    // ================================================================== terreno

    struct MuestraRuta
    {
        public Vector3 pos;      // centro del asfalto
        public Vector3 right;    // 3D (incluye peralte)
        public float rightH;     // longitud horizontal de right
        public float tanPeralte; // pendiente transversal
    }

    static string AjustarTerrenoALaCarretera(GeneradorCurvasGuanajuato gen)
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null) terrain = Object.FindAnyObjectByType<Terrain>(FindObjectsInactive.Include);
        if (terrain == null || terrain.terrainData == null) return "• No hay Terrain en la escena: nada que hundir.";

        // Muestrear la ruta (cada ~0.4 m)
        int tramos = gen.ObtenerTotalTramos();
        if (tramos <= 0) return "• La carretera no tiene tramos.";
        var ruta = new List<MuestraRuta>();
        for (float s = 0f; s < tramos - 0.001f; s += 0.01f)
        {
            Vector3 p, f, u, r; Quaternion q;
            gen.EvaluarRutaCompleta(s, 0f, out p, out q, out f, out u, out r);
            float rh = new Vector2(r.x, r.z).magnitude;
            ruta.Add(new MuestraRuta { pos = p, right = r, rightH = Mathf.Max(0.5f, rh), tanPeralte = r.y / Mathf.Max(0.5f, rh) });
        }

        TerrainData td = terrain.terrainData;
        Vector3 tp = terrain.transform.position;
        Vector3 size = td.size;
        int res = td.heightmapResolution;
        float cx = size.x / (res - 1);
        float cz = size.z / (res - 1);
        float[,] h = td.GetHeights(0, 0, res, res);

        const float RadioInfluencia = 70f;
        const float Nucleo = 9.4f;          // medio asfalto + acotamiento + valla
        const float MezclaIzq = 30f;        // lado de la montaña: sube suave hasta el terreno original
        const float MezclaDerStart = 4f;    // lado del voladero: ladera suave antes de volver al terreno original
        const float MezclaDerEnd = 42f;
        const float PendienteDer = 0.20f;   // ladera suave hacia el valle (aprox. 11°)

        float[,] mejor = new float[res, res];
        int[,] idx = new int[res, res];
        for (int z = 0; z < res; z++) for (int x = 0; x < res; x++) { mejor[z, x] = float.MaxValue; idx[z, x] = -1; }

        for (int i = 0; i < ruta.Count; i++)
        {
            Vector3 p = ruta[i].pos;
            float lx = p.x - tp.x, lz = p.z - tp.z;
            int x0 = Mathf.Max(0, Mathf.FloorToInt((lx - RadioInfluencia) / cx));
            int x1 = Mathf.Min(res - 1, Mathf.CeilToInt((lx + RadioInfluencia) / cx));
            int z0 = Mathf.Max(0, Mathf.FloorToInt((lz - RadioInfluencia) / cz));
            int z1 = Mathf.Min(res - 1, Mathf.CeilToInt((lz + RadioInfluencia) / cz));
            for (int z = z0; z <= z1; z++)
            {
                float dz = z * cz - lz;
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x * cx - lx;
                    float d2 = dx * dx + dz * dz;
                    if (d2 < mejor[z, x]) { mejor[z, x] = d2; idx[z, x] = i; }
                }
            }
        }

        int modificadas = 0;
        for (int z = 0; z < res; z++)
        {
            for (int x = 0; x < res; x++)
            {
                int i = idx[z, x];
                if (i < 0 || mejor[z, x] > RadioInfluencia * RadioInfluencia) continue;

                MuestraRuta m = ruta[i];
                float wx = tp.x + x * cx, wz = tp.z + z * cz;
                float dx = wx - m.pos.x, dz = wz - m.pos.z;
                Vector2 rXZ = new Vector2(m.right.x, m.right.z).normalized;
                float L = dx * rXZ.x + dz * rXZ.y;      // + = lado del voladero (derecha), - = lado de la montaña
                float a = Mathf.Abs(L);
                float original = tp.y + h[z, x] * size.y;
                float yAsfalto = m.pos.y + L * m.tanPeralte;
                float objetivo;

                if (a <= Nucleo)
                {
                    objetivo = yAsfalto - 0.5f; // justo debajo del asfalto
                }
                else if (L < 0f)
                {
                    if (a > Nucleo + MezclaIzq) continue;
                    float yBorde = m.pos.y - Nucleo * m.tanPeralte - 0.5f;
                    float t = Mathf.SmoothStep(0f, 1f, (a - Nucleo) / MezclaIzq);
                    objetivo = Mathf.Lerp(yBorde, original, t);
                }
                else
                {
                    if (a > Nucleo + MezclaDerEnd) continue;
                    float yBorde = m.pos.y + Nucleo * m.tanPeralte - 0.5f;
                    float ladera = yBorde - (a - Nucleo) * PendienteDer;
                    float t = Mathf.SmoothStep(0f, 1f, (a - Nucleo - MezclaDerStart) / (MezclaDerEnd - MezclaDerStart));
                    objetivo = Mathf.Lerp(ladera, original, t);
                }

                h[z, x] = Mathf.Clamp01((objetivo - tp.y) / size.y);
                modificadas++;
            }
        }

        Undo.RegisterCompleteObjectUndo(td, "Ajustar terreno a la carretera");
        td.SetHeights(0, 0, h);

        // Quitar árboles y pasto del terreno que quedaron sobre el asfalto
        int arbolesQuitados = QuitarArbolesEnCorredor(td, tp, size, ruta, 13f);
        int pastoQuitado = QuitarDetalleEnCorredor(td, tp, size, ruta, 11f);

        EditorUtility.SetDirty(td);
        terrain.Flush();
        TerrainCollider tc = terrain.GetComponent<TerrainCollider>();
        if (tc != null) { tc.enabled = false; tc.enabled = true; tc.terrainData = td; }
        AssetDatabase.SaveAssets();

        return "• Terreno ajustado a la carretera (" + modificadas + " puntos del mapa de alturas; árboles quitados: " + arbolesQuitados + "; pasto: " + pastoQuitado + " celdas). " +
               "Si no te gusta el resultado: git checkout \"Assets/New Terrain.asset\" lo restaura.";
    }

    static int QuitarArbolesEnCorredor(TerrainData td, Vector3 tp, Vector3 size, List<MuestraRuta> ruta, float radio)
    {
        TreeInstance[] arboles = td.treeInstances;
        if (arboles == null || arboles.Length == 0) return 0;
        var conservados = new List<TreeInstance>(arboles.Length);
        float r2 = radio * radio;
        foreach (TreeInstance t in arboles)
        {
            float wx = tp.x + t.position.x * size.x, wz = tp.z + t.position.z * size.z;
            bool cerca = false;
            for (int i = 0; i < ruta.Count; i += 2)
            {
                float dx = ruta[i].pos.x - wx, dz = ruta[i].pos.z - wz;
                if (dx * dx + dz * dz < r2) { cerca = true; break; }
            }
            if (!cerca) conservados.Add(t);
        }
        int quitados = arboles.Length - conservados.Count;
        if (quitados > 0) td.SetTreeInstances(conservados.ToArray(), true);
        return quitados;
    }

    static int QuitarDetalleEnCorredor(TerrainData td, Vector3 tp, Vector3 size, List<MuestraRuta> ruta, float radio)
    {
        int capas = td.detailPrototypes != null ? td.detailPrototypes.Length : 0;
        if (capas == 0) return 0;
        int w = td.detailWidth, hgt = td.detailHeight;
        if (w <= 0 || hgt <= 0) return 0;

        bool[,] mascara = new bool[hgt, w];
        float cdx = size.x / w, cdz = size.z / hgt;
        int total = 0;
        foreach (MuestraRuta m in ruta)
        {
            float lx = m.pos.x - tp.x, lz = m.pos.z - tp.z;
            int x0 = Mathf.Max(0, Mathf.FloorToInt((lx - radio) / cdx)), x1 = Mathf.Min(w - 1, Mathf.CeilToInt((lx + radio) / cdx));
            int z0 = Mathf.Max(0, Mathf.FloorToInt((lz - radio) / cdz)), z1 = Mathf.Min(hgt - 1, Mathf.CeilToInt((lz + radio) / cdz));
            for (int z = z0; z <= z1; z++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x + 0.5f) * cdx - lx, dz = (z + 0.5f) * cdz - lz;
                    if (dx * dx + dz * dz <= radio * radio && !mascara[z, x]) { mascara[z, x] = true; total++; }
                }
        }

        for (int capa = 0; capa < capas; capa++)
        {
            int[,] d = td.GetDetailLayer(0, 0, w, hgt, capa);
            for (int z = 0; z < hgt; z++) for (int x = 0; x < w; x++) if (mascara[z, x]) d[z, x] = 0;
            td.SetDetailLayer(0, 0, capa, d);
        }
        return total;
    }

    // ================================================================== noche

    public static void AplicarNoche()
    {
        Material cielo = CargarCieloNoche();
        if (cielo != null) RenderSettings.skybox = cielo;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.045f, 0.06f, 0.11f);
        RenderSettings.reflectionIntensity = 0.25f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0045f;
        RenderSettings.fogColor = new Color(0.035f, 0.055f, 0.10f);

        foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
        {
            if (l.type != LightType.Directional) continue;
            l.name = "Luz_Luna";
            l.color = new Color(0.45f, 0.58f, 0.95f);
            l.intensity = 0.35f;
            l.transform.rotation = Quaternion.Euler(38f, -32f, 0f);
            l.shadows = LightShadows.Soft;
        }

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = cielo != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.03f, 0.07f);
        }
        DynamicGI.UpdateEnvironment();
    }
}
