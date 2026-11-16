using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Terrain del Nivel 1: Sierra de Guanajuato de noche.
/// Cerros rocosos que cierran el valle, un arroyo seco que cruza el camino, sendero de tepetate que une
/// los claros (inicio, 3 tótems y portal) y vegetación típica: encinos en las cañadas, mezquites, huizaches,
/// nopales, magueyes, órganos, biznagas, zacate seco y rocas de cantera.
/// </summary>
public static class BosqueTerrenoBuilder
{
    public const float Tam = 640f;      // metros por lado (el sendero serpentea ~1300 m para que la historia tenga su tiempo)
    public const float Alto = 110f;     // altura máxima del Terrain
    public const int Res = 1025;        // resolución del mapa de alturas (0.62 m por muestra)
    const int ResMapaTexturas = 1024;   // resolución del mapa de texturas del suelo

    // Coordenadas LOCALES del terreno (0..Tam). El mundo = local - Tam/2.
    // El área jugable es |local - Tam/2| < 224 (más allá suben los cerros). Longitud acumulada aproximada entre paréntesis.
    public static readonly Vector2[] Sendero =
    {
        new Vector2(110, 110), // inicio (despertar)                       (0 m)
        new Vector2(160, 128),
        new Vector2(215, 118),
        new Vector2(265, 140), // mirador de los cerros                    (~165 m)
        new Vector2(310, 128),
        new Vector2(365, 150),                                              // (~270 m)
        new Vector2(410, 135),
        new Vector2(460, 160),
        new Vector2(495, 205), // claro del tótem 1                        (~430 m)
        new Vector2(515, 255), // el arroyo seco cruza el camino
        new Vector2(490, 300),
        new Vector2(440, 320),
        new Vector2(385, 305), // bocamina                                 (~650 m)
        new Vector2(330, 325),
        new Vector2(275, 310),
        new Vector2(230, 335), // claro del tótem 2                        (~815 m)
        new Vector2(185, 365),
        new Vector2(150, 410),
        new Vector2(170, 455),
        new Vector2(225, 470),
        new Vector2(280, 455), // claro del tótem 3                        (~1090 m)
        new Vector2(330, 475),
        new Vector2(385, 490), // vista de la corona de La Bufa            (~1200 m)
        new Vector2(440, 470),
        new Vector2(480, 500)  // arena y portal                           (~1310 m)
    };

    public struct Plaza
    {
        public string nombre; public Vector2 centro; public float radio;
        public Plaza(string n, Vector2 c, float r) { nombre = n; centro = c; radio = r; }
    }

    public static readonly Plaza[] Plazas =
    {
        new Plaza("Inicio", new Vector2(110, 110), 13f),
        new Plaza("Totem1", new Vector2(495, 205), 11f),
        new Plaza("Totem2", new Vector2(230, 335), 12f),
        new Plaza("Totem3", new Vector2(280, 455), 12f),
        new Plaza("Portal", new Vector2(480, 500), 14f)
    };

    // Arroyo seco (cauce) que atraviesa el valle entre la primera y la segunda hilera del sendero, y cruza el camino
    static readonly Vector2[] Arroyo =
    {
        new Vector2(0, 250), new Vector2(70, 228), new Vector2(140, 262), new Vector2(210, 226), new Vector2(290, 214),
        new Vector2(360, 250), new Vector2(430, 232), new Vector2(505, 262), new Vector2(570, 236), new Vector2(640, 256)
    };

    public static Vector3 LocalAMundo(Vector2 l, float y) { return new Vector3(l.x - Tam * 0.5f, y, l.y - Tam * 0.5f); }

    /// <summary>Longitud total del sendero en metros.</summary>
    public static float LongitudSendero()
    {
        List<Vector2> s = MuestrearSendero();
        float total = 0f;
        for (int i = 1; i < s.Count; i++) total += Vector2.Distance(s[i - 1], s[i]);
        return total;
    }

    /// <summary>Punto del sendero (coordenadas locales) a 'metros' desde el inicio, con la dirección de avance.</summary>
    public static Vector2 PuntoEnSendero(float metros, out Vector2 direccion)
    {
        List<Vector2> s = MuestrearSendero();
        float acum = 0f;
        for (int i = 1; i < s.Count; i++)
        {
            float tramo = Vector2.Distance(s[i - 1], s[i]);
            if (acum + tramo >= metros || i == s.Count - 1)
            {
                direccion = (s[i] - s[i - 1]).normalized;
                float k = tramo > 0.0001f ? Mathf.Clamp01((metros - acum) / tramo) : 0f;
                return Vector2.Lerp(s[i - 1], s[i], k);
            }
            acum += tramo;
        }
        direccion = Vector2.right;
        return s[0];
    }

    /// <summary>Distancia recorrida (m) sobre el sendero hasta el punto del sendero más cercano a 'local'.</summary>
    public static float MetrosEnSendero(Vector2 local)
    {
        List<Vector2> s = MuestrearSendero();
        float acum = 0f, mejorD = float.MaxValue, mejorM = 0f;
        for (int i = 0; i < s.Count; i++)
        {
            if (i > 0) acum += Vector2.Distance(s[i - 1], s[i]);
            float d = Vector2.SqrMagnitude(s[i] - local);
            if (d < mejorD) { mejorD = d; mejorM = acum; }
        }
        return mejorM;
    }

    // ---------------------------------------------------------------- estado interno de la construcción
    static List<Vector2> muestras;       // sendero muestreado cada ~2 m
    static List<int>[] rejilla;          // índice espacial de 'muestras' para consultas rápidas
    static int rejN;
    const float CeldaRejilla = 16f;
    static float[] alturaSendero;        // altura suavizada a lo largo del sendero
    static float[] alturaPlaza;

    // ================================================================== ALTURAS

    static float Fbm(float x, float z)
    {
        float a = 1f, f = 1f, s = 0f, n = 0f;
        for (int i = 0; i < 4; i++)
        {
            s += Mathf.PerlinNoise(x * f + 100f, z * f + 100f) * a;
            n += a; a *= 0.5f; f *= 2f;
        }
        return s / n;
    }

    static float Cresta(float x, float z, float frec, float off)
    {
        return 1f - Mathf.Abs(2f * Fbm(x * frec + off, z * frec + off * 0.6f) - 1f);
    }

    static float AlturaBase(float x, float z)
    {
        float amplio = Fbm(x * 0.007f, z * 0.007f);
        float crestas = Mathf.Pow(Cresta(x, z, 0.018f, 7f), 2.2f);
        float h = Mathf.Max(2f, 6f + (amplio - 0.5f) * 40f + crestas * 12f);

        float u = Mathf.Max(Mathf.Abs(x - Tam * 0.5f), Mathf.Abs(z - Tam * 0.5f)) / (Tam * 0.5f);
        float t = Mathf.SmoothStep(0f, 1f, (u - 0.70f) / 0.30f);
        float borde = t * (42f + 34f * Cresta(x, z, 0.03f, 50f));
        return h + borde;
    }

    /// <summary>Sendero muestreado cada ~2 m en coordenadas locales del terreno.</summary>
    public static List<Vector2> MuestrasSendero() { return MuestrearSendero(); }

    static List<Vector2> MuestrearSendero()
    {
        var puntos = new List<Vector2>();
        var p = new List<Vector2>(Sendero);
        p.Insert(0, Sendero[0] - (Sendero[1] - Sendero[0]));
        p.Add(Sendero[Sendero.Length - 1] + (Sendero[Sendero.Length - 1] - Sendero[Sendero.Length - 2]));

        for (int i = 0; i < p.Count - 3; i++)
        {
            float largo = Vector2.Distance(p[i + 1], p[i + 2]);
            int pasos = Mathf.Max(2, Mathf.CeilToInt(largo / 2f));
            for (int s = 0; s < pasos; s++)
            {
                float t = s / (float)pasos;
                Vector2 pos = 0.5f * ((2f * p[i + 1]) + (-p[i] + p[i + 2]) * t +
                    (2f * p[i] - 5f * p[i + 1] + 4f * p[i + 2] - p[i + 3]) * t * t +
                    (-p[i] + 3f * p[i + 1] - 3f * p[i + 2] + p[i + 3]) * t * t * t);
                puntos.Add(pos);
            }
        }
        puntos.Add(Sendero[Sendero.Length - 1]);
        return puntos;
    }

    /// <summary>Muestrea el sendero y arma la rejilla espacial (la consulta de distancia se hace millones de veces al construir el mapa).</summary>
    static void CargarMuestras()
    {
        muestras = MuestrearSendero();
        rejN = Mathf.CeilToInt(Tam / CeldaRejilla) + 1;
        rejilla = new List<int>[rejN * rejN];
        for (int i = 0; i < muestras.Count; i++)
        {
            int cx = Mathf.Clamp(Mathf.FloorToInt(muestras[i].x / CeldaRejilla), 0, rejN - 1);
            int cz = Mathf.Clamp(Mathf.FloorToInt(muestras[i].y / CeldaRejilla), 0, rejN - 1);
            int c = cz * rejN + cx;
            if (rejilla[c] == null) rejilla[c] = new List<int>();
            rejilla[c].Add(i);
        }
    }

    static void DistanciaAlSendero(float x, float z, out float dist, out int idx)
    {
        int cx = Mathf.Clamp(Mathf.FloorToInt(x / CeldaRejilla), 0, rejN - 1);
        int cz = Mathf.Clamp(Mathf.FloorToInt(z / CeldaRejilla), 0, rejN - 1);
        float mejor = float.MaxValue; int mi = 0;

        // anillos de celdas alrededor de la consulta; se corta cuando nada fuera del anillo puede ser más cercano
        for (int r = 0; r <= rejN; r++)
        {
            for (int gz = cz - r; gz <= cz + r; gz++)
            {
                if (gz < 0 || gz >= rejN) continue;
                bool filaCompleta = (gz == cz - r || gz == cz + r);
                int paso = filaCompleta || r == 0 ? 1 : 2 * r;
                for (int gx = cx - r; gx <= cx + r; gx += paso)
                {
                    if (gx < 0 || gx >= rejN) continue;
                    List<int> lista = rejilla[gz * rejN + gx];
                    if (lista == null) continue;
                    for (int k = 0; k < lista.Count; k++)
                    {
                        int i = lista[k];
                        float dx = muestras[i].x - x, dz = muestras[i].y - z;
                        float d2 = dx * dx + dz * dz;
                        if (d2 < mejor) { mejor = d2; mi = i; }
                    }
                }
            }
            if (mejor < float.MaxValue && Mathf.Sqrt(mejor) <= r * CeldaRejilla) break;
        }
        dist = Mathf.Sqrt(mejor); idx = mi;
    }

    /// <summary>Distancia al borde del claro más cercano (negativo = dentro).</summary>
    static float DistanciaAPlaza(float x, float z, out int plaza)
    {
        float mejor = float.MaxValue; plaza = 0;
        for (int i = 0; i < Plazas.Length; i++)
        {
            float d = Vector2.Distance(new Vector2(x, z), Plazas[i].centro) - Plazas[i].radio;
            if (d < mejor) { mejor = d; plaza = i; }
        }
        return mejor;
    }

    static float DistanciaAlArroyo(float x, float z)
    {
        float mejor = float.MaxValue;
        Vector2 p = new Vector2(x, z);
        for (int i = 0; i < Arroyo.Length - 1; i++)
        {
            Vector2 a = Arroyo[i], b = Arroyo[i + 1];
            // el cauce serpentea un poco
            float sinu = Mathf.Sin(i * 1.7f + x * 0.03f) * 4f;
            a.y += sinu; b.y += sinu;
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            float d = Vector2.Distance(p, a + ab * t);
            if (d < mejor) mejor = d;
        }
        return mejor;
    }

    // ================================================================== CONSTRUCCIÓN

    public static Terrain Construir(string rutaTerrainData, string carpetaPrefabs, string carpetaMats, string carpetaMeshes)
    {
        CargarMuestras();

        // Altura suavizada a lo largo del sendero (promedio de ±24 m)
        alturaSendero = new float[muestras.Count];
        for (int i = 0; i < muestras.Count; i++)
        {
            float suma = 0f; int n = 0;
            for (int k = -12; k <= 12; k++)
            {
                int j = Mathf.Clamp(i + k, 0, muestras.Count - 1);
                suma += AlturaBase(muestras[j].x, muestras[j].y); n++;
            }
            alturaSendero[i] = suma / n;
        }
        alturaPlaza = new float[Plazas.Length];
        for (int i = 0; i < Plazas.Length; i++)
        {
            float d; int idx;
            DistanciaAlSendero(Plazas[i].centro.x, Plazas[i].centro.y, out d, out idx);
            alturaPlaza[i] = alturaSendero[idx];
        }

        TerrainData td = new TerrainData();
        td.heightmapResolution = Res;
        td.size = new Vector3(Tam, Alto, Tam);

        EditorUtility.DisplayProgressBar("Nivel 1 (Sierra de Guanajuato)", "Levantando los cerros...", 0.1f);
        float[,] h = new float[Res, Res];
        float cell = Tam / (Res - 1);
        for (int z = 0; z < Res; z++)
        {
            for (int x = 0; x < Res; x++)
            {
                float wx = x * cell, wz = z * cell;
                float altura = AlturaBase(wx, wz);

                // Arroyo seco: cauce hundido
                float dA = DistanciaAlArroyo(wx, wz);
                float u = Mathf.Max(Mathf.Abs(wx - Tam * 0.5f), Mathf.Abs(wz - Tam * 0.5f)) / (Tam * 0.5f);
                if (dA < 10f && u < 0.78f) altura -= 3.4f * (1f - Mathf.SmoothStep(0f, 1f, (dA - 1.5f) / 8.5f));

                float d; int idx;
                DistanciaAlSendero(wx, wz, out d, out idx);
                float wSendero = 1f - Mathf.SmoothStep(0f, 1f, (d - 3.5f) / 12.5f);
                if (wSendero > 0f)
                {
                    float ruido = (Mathf.PerlinNoise(wx * 0.4f, wz * 0.4f) - 0.5f) * 0.22f;
                    altura = Mathf.Lerp(altura, alturaSendero[idx] + ruido, wSendero);
                }

                int pl;
                float dp = DistanciaAPlaza(wx, wz, out pl);
                float wPlaza = 1f - Mathf.SmoothStep(0f, 1f, dp / 9f);
                if (wPlaza > 0f) altura = Mathf.Lerp(altura, alturaPlaza[pl], Mathf.Clamp01(wPlaza));

                h[z, x] = Mathf.Clamp01(altura / Alto);
            }
        }
        td.SetHeights(0, 0, h);

        // ------------------------------------------------ texturas del suelo (seco, rocoso, tepetate)
        EditorUtility.DisplayProgressBar("Nivel 1 (Sierra de Guanajuato)", "Pintando tierra, pasto seco y roca...", 0.35f);
        TerrainLayer pasto = BuscarCapa("Grass_Dry_TerrainLayer");
        TerrainLayer tierra = BuscarCapa("Soil_Rocks_TerrainLayer");
        TerrainLayer camino = BuscarCapa("Pebbles_A_TerrainLayer");
        TerrainLayer roca = BuscarCapa("Rock_TerrainLayer");
        if (pasto == null) pasto = BuscarCapa("layer_ground_grass");
        if (tierra == null) tierra = BuscarCapa("layer_ground_soil");
        if (camino == null) camino = BuscarCapa("layer_ground_road");
        if (roca == null) roca = BuscarCapa("layer_ground_rock");

        var capas = new List<TerrainLayer>();
        if (pasto != null) capas.Add(pasto);
        if (tierra != null) capas.Add(tierra);
        if (camino != null) capas.Add(camino);
        if (roca != null) capas.Add(roca);
        if (capas.Count == 0)
        {
            TerrainLayer alt = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/NewLayer.terrainlayer");
            if (alt != null) capas.Add(alt);
        }
        td.terrainLayers = capas.ToArray();

        if (capas.Count > 0)
        {
            const int AR = ResMapaTexturas;
            td.alphamapResolution = AR;
            float[,,] mapa = new float[AR, AR, capas.Count];
            int iPasto = capas.IndexOf(pasto), iTierra = capas.IndexOf(tierra), iCamino = capas.IndexOf(camino), iRoca = capas.IndexOf(roca);
            float celda = Tam / (AR - 1);

            for (int z = 0; z < AR; z++)
            {
                for (int x = 0; x < AR; x++)
                {
                    float wx = x * celda, wz = z * celda;
                    float d; int idx;
                    DistanciaAlSendero(wx, wz, out d, out idx);
                    int pl;
                    float dp = DistanciaAPlaza(wx, wz, out pl);
                    float dA = DistanciaAlArroyo(wx, wz);

                    float wCamino = 1f - Mathf.SmoothStep(0f, 1f, (d - 1.6f) / 2.4f);
                    float wArroyo = (1f - Mathf.SmoothStep(0f, 1f, (dA - 2f) / 4f)) * 0.9f;
                    float wPlazaTierra = (1f - Mathf.SmoothStep(0f, 1f, (dp + Plazas[pl].radio * 0.45f) / (Plazas[pl].radio * 0.55f + 2f))) * 0.7f;
                    float steep = td.GetSteepness(x / (float)(AR - 1), z / (float)(AR - 1));
                    float wTierra = Mathf.Max(wPlazaTierra, Mathf.SmoothStep(0.55f, 0.72f, Mathf.PerlinNoise(wx * 0.045f, wz * 0.045f)) * 0.7f);
                    wTierra = Mathf.Max(wTierra, Mathf.SmoothStep(14f, 26f, steep) * 0.6f);
                    float wRoca = Mathf.SmoothStep(28f, 42f, steep);

                    float[] w = new float[capas.Count];
                    float resto = 1f;
                    if (iRoca >= 0) { w[iRoca] = wRoca; resto -= wRoca; }
                    if (iCamino >= 0) { float v = Mathf.Min(resto, Mathf.Max(wCamino, wArroyo)); w[iCamino] = v; resto -= v; }
                    if (iTierra >= 0) { float v = Mathf.Min(resto, wTierra); w[iTierra] = v; resto -= v; }
                    if (iPasto >= 0) w[iPasto] = Mathf.Max(0f, resto);
                    else w[0] += Mathf.Max(0f, resto);

                    float sum = 0f;
                    for (int c = 0; c < w.Length; c++) sum += w[c];
                    if (sum <= 0.0001f) { w[0] = 1f; sum = 1f; }
                    for (int c = 0; c < w.Length; c++) mapa[z, x, c] = w[c] / sum;
                }
            }
            td.SetAlphamaps(0, 0, mapa);
        }

        AssetDatabase.CreateAsset(td, rutaTerrainData);

        GameObject go = Terrain.CreateTerrainGameObject(td);
        go.name = "Terreno_Sierra";
        go.transform.position = new Vector3(-Tam * 0.5f, 0f, -Tam * 0.5f);
        go.isStatic = true;
        Terrain terrain = go.GetComponent<Terrain>();
        terrain.drawInstanced = true;
        terrain.heightmapPixelError = 5f;
        terrain.treeDistance = 280f;
        terrain.treeBillboardDistance = 280f;
        terrain.treeCrossFadeLength = 20f;

        // ------------------------------------------------ vegetación
        EditorUtility.DisplayProgressBar("Nivel 1 (Sierra de Guanajuato)", "Creando nopales, magueyes, mezquites y rocas...", 0.55f);
        Dictionary<string, GameObject> flora = SierraFlora.CrearTodo(carpetaPrefabs, carpetaMeshes, carpetaMats);

        EditorUtility.DisplayProgressBar("Nivel 1 (Sierra de Guanajuato)", "Plantando la sierra...", 0.7f);
        PlantarVegetacion(td, flora, carpetaPrefabs, carpetaMats);

        EditorUtility.ClearProgressBar();
        return terrain;
    }

    static TerrainLayer BuscarCapa(string nombre)
    {
        foreach (string guid in AssetDatabase.FindAssets(nombre + " t:TerrainLayer"))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(ruta) == nombre) return AssetDatabase.LoadAssetAtPath<TerrainLayer>(ruta);
        }
        return null;
    }

    static readonly string[] ClavesFlora = { "Nopal1", "Nopal2", "Maguey1", "Maguey2", "MagueyQuiote", "Mezquite1", "Mezquite2", "Huizache", "Organo", "Biznaga", "Zacate1", "Zacate2", "Roca1", "Roca2", "Roca3", "Roca4" };

    /// <summary>Vuelve a plantar toda la vegetación sobre el TerrainData ya creado (útil para ajustar densidad sin rehacer el nivel).</summary>
    public static bool Replantar(string rutaTerrainData, string carpetaPrefabs, string carpetaMats)
    {
        TerrainData td = AssetDatabase.LoadAssetAtPath<TerrainData>(rutaTerrainData);
        if (td == null) return false;
        CargarMuestras();

        var flora = new Dictionary<string, GameObject>();
        foreach (string clave in ClavesFlora)
        {
            GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>(carpetaPrefabs + "/" + clave + ".prefab");
            if (p != null) flora[clave] = p;
        }
        if (flora.Count == 0) return false;

        PlantarVegetacion(td, flora, carpetaPrefabs, carpetaMats);
        EditorUtility.SetDirty(td);
        AssetDatabase.SaveAssets();
        foreach (Terrain t in Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include)) if (t.terrainData == td) t.Flush();
        return true;
    }

    // ================================================================== VEGETACIÓN

    struct Info { public float d, dp, dA, steep, h, hum; }

    static Info Medir(TerrainData td, float x, float z)
    {
        Info i;
        int idx; float d; DistanciaAlSendero(x, z, out d, out idx);
        int pl; float dp = DistanciaAPlaza(x, z, out pl);
        i.d = d; i.dp = dp;
        i.dA = DistanciaAlArroyo(x, z);
        float nx = Mathf.Clamp01(x / Tam), nz = Mathf.Clamp01(z / Tam);
        i.steep = td.GetSteepness(nx, nz);
        i.h = td.GetInterpolatedHeight(nx, nz);
        // humedad: cañadas y cercanía al arroyo => encinos y mezquites; lomas secas => nopal y maguey
        float cerca = 1f - Mathf.SmoothStep(0f, 1f, (i.dA - 5f) / 38f);
        i.hum = Mathf.Clamp01(Fbm(x * 0.02f + 90f, z * 0.02f + 90f) * 0.75f + cerca * 0.45f - Mathf.SmoothStep(25f, 60f, i.h) * 0.25f);
        return i;
    }

    /// <summary>
    /// El mapa es 4.5 veces más grande: la vegetación se concentra alrededor del sendero (donde el jugador la ve)
    /// y se aclara hacia los cerros lejanos para no plantar cientos de miles de instancias.
    /// </summary>
    static float Densidad(Info i) { return Mathf.Lerp(1f, 0.14f, Mathf.SmoothStep(0f, 1f, (i.d - 42f) / 80f)); }

    static bool Libre(Info i, float dSendero) { return i.d >= dSendero && i.dp >= 2.5f && i.dA >= 1.8f; }

    static void PlantarVegetacion(TerrainData td, Dictionary<string, GameObject> flora, string carpetaPrefabs, string carpetaMats)
    {
        var protos = new List<TreePrototype>();
        var indice = new Dictionary<string, int>();

        System.Action<string, GameObject, float> reg = (clave, prefab, bend) =>
        {
            if (prefab == null) return;
            prefab = AsegurarLOD(prefab);   // el Terrain exige MeshRenderer en la raíz o un LODGroup
            indice[clave] = protos.Count;
            protos.Add(new TreePrototype { prefab = prefab, bendFactor = bend });
        };
        foreach (var kv in flora) reg(kv.Key, kv.Value, 0f);

        // Del pack FantasyEnvironments: encinos, pinos (solo en las alturas), arbustos secos y rocas, con tintes de la sierra
        reg("Encino1", PrepararPrefabPack("Oak_tree1", 8.5f, true, carpetaPrefabs, carpetaMats), 0.3f);
        reg("Encino2", PrepararPrefabPack("Oak_tree2", 7.0f, true, carpetaPrefabs, carpetaMats), 0.3f);
        reg("Pino1", PrepararPrefabPack("Pine_tree1", 13f, true, carpetaPrefabs, carpetaMats), 0.3f);
        reg("Pino2", PrepararPrefabPack("Pine_tree2", 11f, true, carpetaPrefabs, carpetaMats), 0.3f);
        reg("Arbusto", PrepararPrefabPack("Bush1", 1.3f, false, carpetaPrefabs, carpetaMats), 0f);
        reg("RocaPack1", PrepararPrefabPack("Rock1", 1.7f, true, carpetaPrefabs, carpetaMats), 0f);
        reg("RocaPack2", PrepararPrefabPack("Rock2", 1.3f, true, carpetaPrefabs, carpetaMats), 0f);
        reg("RocaPack3", PrepararPrefabPack("Rock3", 2.3f, true, carpetaPrefabs, carpetaMats), 0f);

        if (protos.Count == 0) { Debug.LogWarning("[Sierra] No se pudo crear ninguna especie vegetal."); return; }
        td.treePrototypes = protos.ToArray();

        System.Random rng = new System.Random(2026);
        var instancias = new List<TreeInstance>();

        // Planta en rejilla con jitter. 'prob' devuelve 0..1 para cada punto candidato.
        System.Action<string[], float, float, float, System.Func<Info, float>> plantar = (claves, paso, sMin, sMax, prob) =>
        {
            var validas = new List<int>();
            foreach (string c in claves) { int ix; if (indice.TryGetValue(c, out ix)) validas.Add(ix); }
            if (validas.Count == 0) return;

            for (float x = 2f; x < Tam - 2f; x += paso)
            {
                for (float z = 2f; z < Tam - 2f; z += paso)
                {
                    float px = x + ((float)rng.NextDouble() - 0.5f) * paso;
                    float pz = z + ((float)rng.NextDouble() - 0.5f) * paso;
                    if (px < 1f || pz < 1f || px > Tam - 1f || pz > Tam - 1f) continue;

                    Info info = Medir(td, px, pz);
                    float p = prob(info) * Densidad(info);
                    if (p <= 0f || rng.NextDouble() > p) continue;

                    float s = sMin + (float)rng.NextDouble() * (sMax - sMin);
                    instancias.Add(new TreeInstance
                    {
                        position = new Vector3(px / Tam, 0f, pz / Tam),
                        prototypeIndex = validas[rng.Next(validas.Count)],
                        widthScale = s * (0.9f + (float)rng.NextDouble() * 0.2f),
                        heightScale = s,
                        rotation = (float)rng.NextDouble() * Mathf.PI * 2f,
                        color = new Color32(255, 255, 255, 255),
                        lightmapColor = new Color32(255, 255, 255, 255)
                    });
                }
            }
        };

        // --- Árboles (bosque de encinos y mezquites que flanquea el sendero; más densos en cañadas y a los costados del camino)
        System.Func<Info, float> flanco = i => 0.30f * Mathf.SmoothStep(0f, 1f, (i.d - 6f) / 6f) * (1f - Mathf.SmoothStep(0f, 1f, (i.d - 26f) / 14f));
        plantar(new[] { "Encino1", "Encino2" }, 5.5f, 1.0f, 1.5f, i => Libre(i, 6f) && i.steep < 32f ? Mathf.Clamp01(Mathf.Lerp(0.16f, 0.92f, Mathf.SmoothStep(0.28f, 0.60f, i.hum)) + flanco(i)) : 0f);
        plantar(new[] { "Mezquite1", "Mezquite2" }, 6f, 0.95f, 1.4f, i => Libre(i, 5f) && i.steep < 34f && i.hum > 0.12f && i.hum < 0.9f ? 0.50f + flanco(i) * 0.5f : 0f);
        plantar(new[] { "Huizache" }, 7f, 0.9f, 1.3f, i => Libre(i, 5f) && i.steep < 36f && i.hum < 0.70f ? 0.34f : 0f);
        plantar(new[] { "Pino1", "Pino2" }, 7f, 0.95f, 1.4f, i => Libre(i, 7f) && i.h > 24f && i.steep < 38f ? 0.58f : 0f);
        // --- Cactáceas y agaves (laderas secas y soleadas)
        plantar(new[] { "Nopal1", "Nopal2" }, 4.2f, 0.8f, 1.35f, i => Libre(i, 3.5f) && i.steep < 38f ? 0.30f * Mathf.Clamp01(1.15f - i.hum) : 0f);
        plantar(new[] { "Maguey1", "Maguey2" }, 5f, 0.8f, 1.4f, i => Libre(i, 3.5f) && i.steep < 42f ? 0.17f + 0.20f * Mathf.SmoothStep(18f, 34f, i.steep) : 0f);
        plantar(new[] { "MagueyQuiote" }, 6f, 0.9f, 1.2f, i => Libre(i, 5f) && i.steep < 36f ? 0.05f : 0f);
        plantar(new[] { "Organo" }, 12f, 0.8f, 1.3f, i => Libre(i, 5f) && i.steep < 40f && i.hum < 0.6f ? 0.3f : 0f);
        plantar(new[] { "Biznaga" }, 5.5f, 0.8f, 1.5f, i => Libre(i, 3f) ? 0.16f : 0f);
        plantar(new[] { "Arbusto" }, 6f, 0.8f, 1.4f, i => Libre(i, 3.5f) ? 0.15f : 0f);

        // --- Rocas de cantera (más en laderas empinadas y en el arroyo)
        plantar(new[] { "Roca1", "Roca2", "Roca3", "Roca4", "RocaPack1", "RocaPack2" }, 3.6f, 0.7f, 1.6f,
            i => i.d >= 3f && i.dp >= 2f ? 0.07f + 0.28f * Mathf.SmoothStep(22f, 38f, i.steep) + (i.dA < 10f ? 0.12f : 0f) : 0f);
        plantar(new[] { "Roca3", "Roca4", "RocaPack3" }, 12f, 1.8f, 3.2f, i => Libre(i, 8f) ? 0.5f : 0f);

        // --- Zacate seco (cubre el suelo)
        plantar(new[] { "Zacate1", "Zacate2" }, 2.3f, 0.8f, 1.5f, i => i.d >= 2f && i.dp >= 1.2f && i.dA >= 2.5f && i.steep < 34f ? 0.30f : 0f);

        td.SetTreeInstances(instancias.ToArray(), true);
        Debug.Log("[Sierra] Vegetación plantada: " + instancias.Count + " instancias, " + protos.Count + " especies.");
    }

    /// <summary>
    /// Un árbol del Terrain solo es válido si el prefab tiene MeshRenderer en la raíz o un LODGroup en la raíz.
    /// Nuestros prefabs tienen los renderers en hijos, así que se les agrega un LODGroup raíz (y se conserva solo el LOD0 de modelos del pack con LOD propios).
    /// </summary>
    static GameObject AsegurarLOD(GameObject prefab)
    {
        string ruta = AssetDatabase.GetAssetPath(prefab);
        if (string.IsNullOrEmpty(ruta)) return prefab;

        GameObject contenido = PrefabUtility.LoadPrefabContents(ruta);
        try
        {
            if (contenido.GetComponent<LODGroup>() != null || contenido.GetComponent<MeshRenderer>() != null) return prefab;

            // Si el modelo trae sus propios LODGroup en hijos: quedarse solo con el LOD más detallado
            foreach (LODGroup lg in contenido.GetComponentsInChildren<LODGroup>(true))
            {
                LOD[] niveles = lg.GetLODs();
                var conservar = new HashSet<Renderer>();
                if (niveles.Length > 0) foreach (Renderer r in niveles[0].renderers) if (r != null) conservar.Add(r);
                for (int i = 1; i < niveles.Length; i++)
                    foreach (Renderer r in niveles[i].renderers)
                        if (r != null && !conservar.Contains(r)) Object.DestroyImmediate(r.gameObject);
                Object.DestroyImmediate(lg);
            }

            Renderer[] rs = contenido.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) { Debug.LogWarning("[Sierra] El prefab " + ruta + " no tiene renderers."); return prefab; }

            LODGroup grupo = contenido.AddComponent<LODGroup>();
            grupo.SetLODs(new[] { new LOD(0.02f, rs) });
            grupo.RecalculateBounds();
            PrefabUtility.SaveAsPrefabAsset(contenido, ruta);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contenido);
        }
        return AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
    }

    // ================================================================== PREFABS DEL PACK (convertidos a URP con tintes)

    static GameObject BuscarPrefabFuente(string nombre)
    {
        foreach (string guid in AssetDatabase.FindAssets(nombre + " t:Prefab"))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            if (!ruta.Contains("FantasyEnvironments")) continue;
            if (System.IO.Path.GetFileNameWithoutExtension(ruta) != nombre) continue;
            return AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
        }
        return null;
    }

    static GameObject PrepararPrefabPack(string nombre, float altoObjetivo, bool colision, string carpetaPrefabs, string carpetaMats)
    {
        string destino = carpetaPrefabs + "/" + nombre + "_Sierra.prefab";
        GameObject existente = AssetDatabase.LoadAssetAtPath<GameObject>(destino);
        if (existente != null) return existente;

        GameObject fuente = BuscarPrefabFuente(nombre);
        if (fuente == null) { Debug.LogWarning("[Sierra] No se encontró el prefab '" + nombre + "' (se omite)."); return null; }

        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(fuente);
        PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        inst.transform.position = Vector3.zero;
        inst.transform.rotation = Quaternion.identity;
        inst.transform.localScale = Vector3.one;

        foreach (Renderer r in inst.GetComponentsInChildren<Renderer>())
        {
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = ConvertirMaterial(mats[i], carpetaMats);
            r.sharedMaterials = mats;
        }

        Bounds b = new Bounds(Vector3.zero, Vector3.zero);
        bool primero = true;
        foreach (Renderer r in inst.GetComponentsInChildren<Renderer>())
        {
            if (primero) { b = r.bounds; primero = false; } else b.Encapsulate(r.bounds);
        }
        float factor = altoObjetivo / Mathf.Max(0.01f, b.size.y);

        GameObject raiz = new GameObject(nombre + "_Sierra");
        inst.transform.SetParent(raiz.transform, false);
        inst.transform.localScale = Vector3.one * factor;
        inst.transform.localPosition = new Vector3(0f, -b.min.y * factor, 0f);

        foreach (Collider c in inst.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
        if (colision)
        {
            bool arbol = nombre.Contains("tree");
            CapsuleCollider cap = raiz.AddComponent<CapsuleCollider>();
            float radio = arbol ? Mathf.Max(0.25f, altoObjetivo * 0.025f) : Mathf.Max(0.3f, altoObjetivo * 0.3f);
            cap.radius = radio;
            cap.height = arbol ? altoObjetivo * 0.6f : Mathf.Max(radio * 2f, altoObjetivo);
            cap.center = new Vector3(0f, cap.height * 0.5f, 0f);
            cap.direction = 1;
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(raiz, destino);
        Object.DestroyImmediate(raiz);
        return prefab;
    }

    static Material ConvertirMaterial(Material viejo, string carpetaMats)
    {
        if (viejo == null) return null;
        string ruta = carpetaMats + "/" + viejo.name + "_Sierra.mat";
        Material ya = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (ya != null) return ya;

        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) return viejo;

        Material m = new Material(lit);
        m.name = viejo.name + "_Sierra";

        Texture tex = viejo.HasProperty("_MainTex") ? viejo.GetTexture("_MainTex") : null;
        Color col = viejo.HasProperty("_Color") ? viejo.GetColor("_Color") : Color.white;

        string n = viejo.name.ToLowerInvariant();
        string sh = viejo.shader != null ? viejo.shader.name.ToLowerInvariant() : "";

        // Tintes para que el pack (verde europeo) se vea como la sierra: follaje seco/oliva, pinos oscuros, roca de cantera
        Color tinte = Color.white;
        if (n.Contains("pine") && n.Contains("leaves")) tinte = new Color(0.55f, 0.68f, 0.58f);
        else if (n.Contains("leaves")) tinte = new Color(0.86f, 0.84f, 0.50f);
        else if (n.Contains("plant") || n.Contains("bush")) tinte = new Color(0.85f, 0.75f, 0.45f);
        else if (n.Contains("rock") || n.Contains("stone")) tinte = new Color(0.98f, 0.80f, 0.74f);
        else if (n.Contains("bark")) tinte = new Color(0.85f, 0.78f, 0.72f);

        if (tex != null) m.SetTexture("_BaseMap", tex);
        m.SetColor("_BaseColor", col * tinte);
        m.SetFloat("_Smoothness", 0.08f);
        m.SetFloat("_Metallic", 0f);

        bool follaje = n.Contains("leaves") || n.Contains("plant") || n.Contains("grass") || n.Contains("fern") || n.Contains("flower")
                    || n.Contains("bush") || n.Contains("rye") || n.Contains("sunflower") || sh.Contains("cutout") || sh.Contains("leaves")
                    || viejo.renderQueue >= 2450;
        if (follaje)
        {
            m.SetFloat("_AlphaClip", 1f);
            m.EnableKeyword("_ALPHATEST_ON");
            m.SetFloat("_Cutoff", viejo.HasProperty("_Cutoff") ? Mathf.Clamp(viejo.GetFloat("_Cutoff"), 0.2f, 0.8f) : 0.5f);
            m.SetFloat("_Cull", 0f);
            m.renderQueue = 2450;
        }

        m.enableInstancing = true;
        AssetDatabase.CreateAsset(m, ruta);
        return m;
    }
}
