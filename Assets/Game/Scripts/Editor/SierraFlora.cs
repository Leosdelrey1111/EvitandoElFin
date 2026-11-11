using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Flora y rocas procedurales de la Sierra de Guanajuato (estilo low-poly):
/// nopales con tunas, magueyes (y quiotes en flor), mezquites, huizaches, órganos, biznagas, zacate seco
/// y rocas de cantera facetadas. Todo se genera como mallas guardadas en Assets/Game/Meshes/Sierra
/// y prefabs listos para usarse como árboles del Terrain.
/// </summary>
public static class SierraFlora
{
    // ---------------------------------------------------------------- paleta
    public static readonly Color VerdeNopal = new Color(0.26f, 0.46f, 0.25f);
    public static readonly Color RojoTuna = new Color(0.72f, 0.10f, 0.24f);
    public static readonly Color AzulMaguey = new Color(0.33f, 0.50f, 0.48f);
    public static readonly Color VerdeMezquite = new Color(0.20f, 0.30f, 0.14f);
    public static readonly Color CorteZaMezquite = new Color(0.20f, 0.14f, 0.10f);
    public static readonly Color AmarilloHuizache = new Color(0.95f, 0.78f, 0.12f);
    public static readonly Color VerdeOrgano = new Color(0.19f, 0.40f, 0.27f);
    public static readonly Color Zacate = new Color(0.78f, 0.64f, 0.34f);
    public static readonly Color[] Cantera =
    {
        new Color(0.74f, 0.56f, 0.50f),   // cantera rosa (la típica de Guanajuato)
        new Color(0.55f, 0.50f, 0.47f),   // roca gris
        new Color(0.66f, 0.52f, 0.38f),   // ocre
    };

    static Shader shader;
    static string carpetaMeshes, carpetaMats, carpetaPrefabs;

    public struct Parte
    {
        public Mesh mesh; public Material mat;
        public Parte(Mesh m, Material t) { mesh = m; mat = t; }
    }

    // ================================================================== API

    public static Dictionary<string, GameObject> CrearTodo(string prefabs, string meshes, string mats)
    {
        carpetaPrefabs = prefabs; carpetaMeshes = meshes; carpetaMats = mats;
        shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        var d = new Dictionary<string, GameObject>();

        Material mNopal = Material_("Nopal", VerdeNopal, 0.25f, false);
        Material mTuna = Material_("Tuna", RojoTuna, 0.35f, false);
        Material mMaguey = Material_("Maguey", AzulMaguey, 0.30f, true);
        Material mQuiote = Material_("Quiote", new Color(0.52f, 0.46f, 0.26f), 0.15f, false);
        Material mFlorQuiote = MaterialEmisivo_("FlorQuiote", new Color(0.95f, 0.85f, 0.3f), new Color(0.6f, 0.5f, 0.1f));
        Material mCorteza = Material_("CortezaMezquite", CorteZaMezquite, 0.1f, false);
        Material mFollaje = Material_("FollajeMezquite", VerdeMezquite, 0.1f, false);
        Material mFollajeHuiz = Material_("FollajeHuizache", new Color(0.30f, 0.38f, 0.17f), 0.1f, false);
        Material mFlorHuiz = MaterialEmisivo_("FlorHuizache", AmarilloHuizache, new Color(0.7f, 0.5f, 0.05f));
        Material mOrgano = Material_("Organo", VerdeOrgano, 0.25f, false);
        Material mBiznaga = Material_("Biznaga", new Color(0.30f, 0.50f, 0.30f), 0.25f, false);
        Material mFlorBiznaga = MaterialEmisivo_("FlorBiznaga", new Color(0.95f, 0.7f, 0.15f), new Color(0.6f, 0.4f, 0.05f));
        Material mZacate = Material_("Zacate", Zacate, 0.05f, true);
        Material mZacateOscuro = Material_("ZacateOscuro", new Color(0.55f, 0.46f, 0.24f), 0.05f, true);

        // Nopales (2 variantes)
        for (int v = 0; v < 2; v++)
        {
            Mesh pencas, tunas;
            MallaNopal(100 + v * 13, out pencas, out tunas);
            d["Nopal" + (v + 1)] = GuardarPrefab("Nopal" + (v + 1), 1.9f, 0.45f, 1.5f, new Parte(Guardar(pencas, "Nopal" + (v + 1) + "_Pencas"), mNopal), new Parte(Guardar(tunas, "Nopal" + (v + 1) + "_Tunas"), mTuna));
        }

        // Magueyes (2 + uno con quiote en flor)
        for (int v = 0; v < 2; v++)
        {
            Mesh m = MallaMaguey(200 + v * 7, 20 + v * 4, 1.5f + v * 0.2f);
            d["Maguey" + (v + 1)] = GuardarPrefab("Maguey" + (v + 1), 1.2f, 0.7f, 1.0f, new Parte(Guardar(m, "Maguey" + (v + 1)), mMaguey));
        }
        {
            Mesh m = MallaMaguey(215, 22, 1.7f);
            Mesh tallo, flores;
            MallaQuiote(out tallo, out flores);
            d["MagueyQuiote"] = GuardarPrefab("MagueyQuiote", 5.5f, 0.7f, 1.0f,
                new Parte(Guardar(m, "MagueyQuiote_Hojas"), mMaguey), new Parte(Guardar(tallo, "MagueyQuiote_Tallo"), mQuiote), new Parte(Guardar(flores, "MagueyQuiote_Flores"), mFlorQuiote));
        }

        // Mezquites (2 variantes)
        for (int v = 0; v < 2; v++)
        {
            Mesh corteza, follaje;
            MallaMezquite(300 + v * 31, 1f + v * 0.15f, out corteza, out follaje);
            d["Mezquite" + (v + 1)] = GuardarPrefab("Mezquite" + (v + 1), 5.5f, 0.3f, 3.5f, new Parte(Guardar(corteza, "Mezquite" + (v + 1) + "_Corteza"), mCorteza), new Parte(Guardar(follaje, "Mezquite" + (v + 1) + "_Follaje"), mFollaje));
        }

        // Huizache
        {
            Mesh corteza, follaje, flores;
            MallaHuizache(401, out corteza, out follaje, out flores);
            d["Huizache"] = GuardarPrefab("Huizache", 3.4f, 0.22f, 2.2f,
                new Parte(Guardar(corteza, "Huizache_Corteza"), mCorteza), new Parte(Guardar(follaje, "Huizache_Follaje"), mFollajeHuiz), new Parte(Guardar(flores, "Huizache_Flores"), mFlorHuiz));
        }

        // Órgano (cactus columnar) y biznaga
        {
            Mesh m = MallaOrgano(501);
            d["Organo"] = GuardarPrefab("Organo", 3.6f, 0.45f, 3.0f, new Parte(Guardar(m, "Organo"), mOrgano));
            Mesh b, f;
            MallaBiznaga(out b, out f);
            d["Biznaga"] = GuardarPrefab("Biznaga", 0.6f, 0f, 0f, new Parte(Guardar(b, "Biznaga"), mBiznaga), new Parte(Guardar(f, "Biznaga_Flor"), mFlorBiznaga));
        }

        // Zacate seco (2 variantes)
        for (int v = 0; v < 2; v++)
        {
            Mesh m = MallaZacate(600 + v * 17, 8 + v * 2);
            d["Zacate" + (v + 1)] = GuardarPrefab("Zacate" + (v + 1), 0.7f, 0f, 0f, new Parte(Guardar(m, "Zacate" + (v + 1)), v == 0 ? mZacate : mZacateOscuro));
        }

        // Rocas de cantera (4 formas x colores)
        for (int v = 0; v < 4; v++)
        {
            float ancho = 1.6f + v * 0.5f;
            Mesh m = MallaRoca(700 + v * 11, new Vector3(ancho, ancho * (0.55f + 0.12f * v), ancho * 0.85f), 0.38f, 0f);
            Material mr = Material_("Cantera" + (v % 3), Cantera[v % 3], 0.12f, false);
            d["Roca" + (v + 1)] = GuardarPrefab("Roca" + (v + 1), ancho * 0.6f, ancho * 0.45f, ancho * 0.7f, new Parte(Guardar(m, "Roca" + (v + 1)), mr));
        }

        AssetDatabase.SaveAssets();
        return d;
    }

    // ================================================================== MATERIALES Y ARCHIVOS

    public static Material Material_(string nombre, Color c, float suavidad, bool doble)
    {
        string ruta = carpetaMats + "/Sierra_" + nombre + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null)
        {
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, ruta);
        }
        m.SetColor("_BaseColor", c);
        m.color = c;
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", suavidad);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        if (m.HasProperty("_Cull")) m.SetFloat("_Cull", doble ? 0f : 2f);
        m.enableInstancing = true;
        EditorUtility.SetDirty(m);
        return m;
    }

    public static Material MaterialEmisivo_(string nombre, Color c, Color emision)
    {
        Material m = Material_(nombre, c, 0.3f, false);
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", emision);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        EditorUtility.SetDirty(m);
        return m;
    }

    static Mesh Guardar(Mesh mesh, string nombre)
    {
        mesh.name = nombre;
        mesh.RecalculateBounds();
        string ruta = carpetaMeshes + "/" + nombre + ".asset";
        Mesh existente = AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
        if (existente != null) AssetDatabase.DeleteAsset(ruta);
        AssetDatabase.CreateAsset(mesh, ruta);
        return mesh;
    }

    /// <summary>Crea un prefab con las partes dadas. radioCol/altoCol &gt; 0 agregan un CapsuleCollider (colisión de árbol del Terrain).</summary>
    static GameObject GuardarPrefab(string nombre, float altoObjetivoRef, float radioCol, float altoCol, params Parte[] partes)
    {
        GameObject raiz = new GameObject(nombre);
        foreach (Parte p in partes)
        {
            GameObject hijo = new GameObject(p.mesh.name);
            hijo.transform.SetParent(raiz.transform, false);
            hijo.AddComponent<MeshFilter>().sharedMesh = p.mesh;
            MeshRenderer mr = hijo.AddComponent<MeshRenderer>();
            mr.sharedMaterial = p.mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }
        if (radioCol > 0f && altoCol > 0f)
        {
            CapsuleCollider cap = raiz.AddComponent<CapsuleCollider>();
            cap.radius = radioCol;
            cap.height = Mathf.Max(altoCol, radioCol * 2f);
            cap.center = new Vector3(0f, cap.height * 0.5f, 0f);
            cap.direction = 1;
        }
        string ruta = carpetaPrefabs + "/" + nombre + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
        Object.DestroyImmediate(raiz);
        return prefab;
    }

    // ================================================================== UTILIDADES DE MALLA

    static Mesh Builtin(string nombre) { return Resources.GetBuiltinResource<Mesh>(nombre); }

    static Mesh Combinar(List<CombineInstance> lista)
    {
        Mesh m = new Mesh();
        m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m.CombineMeshes(lista.ToArray(), true, true);
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    static void Add(List<CombineInstance> l, Mesh m, Matrix4x4 mat)
    {
        l.Add(new CombineInstance { mesh = m, transform = mat });
    }

    /// <summary>Convierte a sombreado plano (cada triángulo con sus propios vértices): aspecto facetado low-poly.</summary>
    public static Mesh Facetado(Mesh src)
    {
        Vector3[] v = src.vertices; int[] t = src.triangles;
        Vector3[] nv = new Vector3[t.Length]; int[] nt = new int[t.Length]; Vector2[] uv = new Vector2[t.Length];
        Vector2[] suv = src.uv;
        for (int i = 0; i < t.Length; i++)
        {
            nv[i] = v[t[i]]; nt[i] = i;
            uv[i] = suv != null && suv.Length == v.Length ? suv[t[i]] : Vector2.zero;
        }
        Mesh m = new Mesh();
        m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m.vertices = nv; m.triangles = nt; m.uv = uv;
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    static float Ruido3(Vector3 p)
    {
        return (Mathf.PerlinNoise(p.x, p.y) + Mathf.PerlinNoise(p.y, p.z) + Mathf.PerlinNoise(p.z, p.x)) / 3f;
    }

    /// <summary>Roca de cantera facetada: esfera deformada con ruido, base plana. escala = tamaño en metros; estratos = relieve de capas horizontales.</summary>
    public static Mesh MallaRoca(int semilla, Vector3 escala, float rugosidad, float estratos)
    {
        Mesh esfera = Builtin("Sphere.fbx");
        Vector3[] v = esfera.vertices;
        Vector3[] nv = new Vector3[v.Length];
        float off = semilla * 0.173f;
        for (int i = 0; i < v.Length; i++)
        {
            Vector3 dir = v[i].normalized;
            float n = Ruido3(dir * 1.8f + new Vector3(off, off * 0.7f, off * 1.3f)) - 0.5f;
            float n2 = Ruido3(dir * 4.2f + new Vector3(off * 1.7f, off, off * 0.4f)) - 0.5f;
            float r = 0.5f * (1f + n * rugosidad * 2f + n2 * rugosidad * 0.8f);
            Vector3 p = dir * r;
            if (estratos > 0f) p += dir * (Mathf.Sin(p.y * 9f / Mathf.Max(0.5f, escala.y)) * estratos * 0.05f);
            p = Vector3.Scale(p, escala);
            p.y = Mathf.Max(p.y, -escala.y * 0.28f);   // base plana (enterrada)
            p.y += escala.y * 0.28f;                   // el suelo queda en y = 0
            nv[i] = p;
        }
        Mesh m = new Mesh();
        m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m.vertices = nv; m.triangles = esfera.triangles; m.uv = esfera.uv;
        return Facetado(m);
    }

    /// <summary>Hoja alargada y curva (penca de maguey, brizna de zacate). Apunta hacia +Z, con base en el origen.</summary>
    static Mesh MallaHoja(float largo, float ancho, int seg, float anguloInicial, float curvatura)
    {
        int rings = seg + 1;
        Vector3[] vs = new Vector3[rings * 3];
        Vector2[] uvs = new Vector2[rings * 3];
        Vector3 pos = Vector3.zero;
        for (int i = 0; i < rings; i++)
        {
            float t = i / (float)seg;
            float a = (anguloInicial + curvatura * t) * Mathf.Deg2Rad;       // ángulo desde la vertical hacia +Z
            Vector3 tang = new Vector3(0f, Mathf.Cos(a), Mathf.Sin(a));
            Vector3 normal = new Vector3(0f, -Mathf.Sin(a), Mathf.Cos(a));
            float w = ancho * Mathf.Pow(1f - t, 0.75f) * (0.55f + 0.45f * Mathf.Min(1f, t * 5f));
            vs[i * 3 + 0] = pos - Vector3.right * w - normal * (w * 0.35f);
            vs[i * 3 + 1] = pos + normal * (w * 0.15f);
            vs[i * 3 + 2] = pos + Vector3.right * w - normal * (w * 0.35f);
            uvs[i * 3 + 0] = new Vector2(0f, t); uvs[i * 3 + 1] = new Vector2(0.5f, t); uvs[i * 3 + 2] = new Vector2(1f, t);
            pos += tang * (largo / seg);
        }
        var tris = new List<int>();
        for (int i = 0; i < seg; i++)
        {
            int a0 = i * 3, b0 = (i + 1) * 3;
            tris.AddRange(new[] { a0, b0, a0 + 1, a0 + 1, b0, b0 + 1 });
            tris.AddRange(new[] { a0 + 1, b0 + 1, a0 + 2, a0 + 2, b0 + 1, b0 + 2 });
        }
        Mesh m = new Mesh();
        m.vertices = vs; m.uv = uvs; m.triangles = tris.ToArray();
        m.RecalculateNormals();
        return m;
    }

    // ================================================================== PLANTAS

    static void MallaNopal(int semilla, out Mesh pencas, out Mesh tunas)
    {
        System.Random rng = new System.Random(semilla);
        var lp = new List<CombineInstance>();
        var lt = new List<CombineInstance>();
        Mesh esfera = Builtin("Sphere.fbx");

        // pala base y ramas recursivas
        System.Action<Matrix4x4, float, int> pala = null;
        pala = (frame, escala, prof) =>
        {
            float w = 0.62f * escala, h = 0.80f * escala, t = 0.11f * escala;
            Add(lp, esfera, frame * Matrix4x4.TRS(new Vector3(0f, h * 0.5f, 0f), Quaternion.identity, new Vector3(w, h, t)));
            // tunas en el borde superior
            int nt = prof >= 1 ? rng.Next(1, 4) : 0;
            for (int k = 0; k < nt; k++)
            {
                float ax = ((float)rng.NextDouble() - 0.5f) * w * 0.8f;
                Add(lt, esfera, frame * Matrix4x4.TRS(new Vector3(ax, h * (0.92f + 0.06f * k), t * 0.6f), Quaternion.identity, new Vector3(0.13f, 0.17f, 0.13f)));
            }
            if (prof >= 3) return;
            int hijos = prof == 0 ? 3 : rng.Next(1, 3);
            for (int c = 0; c < hijos; c++)
            {
                float yaw = (float)rng.NextDouble() * 360f;
                float tilt = (c % 2 == 0 ? -1f : 1f) * (18f + (float)rng.NextDouble() * 28f);
                Matrix4x4 hijo = frame * Matrix4x4.TRS(new Vector3(((float)rng.NextDouble() - 0.5f) * w * 0.5f, h * 0.88f, 0f), Quaternion.Euler(0f, yaw, tilt), Vector3.one);
                pala(hijo, escala * (0.80f + (float)rng.NextDouble() * 0.12f), prof + 1);
            }
        };
        pala(Matrix4x4.identity, 1f, 0);

        pencas = Combinar(lp);
        tunas = Combinar(lt);
    }

    static Mesh MallaMaguey(int semilla, int pencasN, float largo)
    {
        System.Random rng = new System.Random(semilla);
        var l = new List<CombineInstance>();
        for (int k = 0; k < pencasN; k++)
        {
            float t = k / (float)pencasN;
            float yaw = k * 137.5f;
            float a0 = Mathf.Lerp(8f, 64f, t) + (float)rng.NextDouble() * 6f;
            float bend = Mathf.Lerp(18f, 42f, t);
            float largoK = largo * Mathf.Lerp(0.62f, 1.0f, Mathf.Sin(t * Mathf.PI * 0.5f)) * (0.9f + (float)rng.NextDouble() * 0.2f);
            Mesh hoja = MallaHoja(largoK, 0.20f * (largo / 1.5f), 6, a0, bend);
            Add(l, hoja, Matrix4x4.TRS(new Vector3(0f, 0.12f, 0f), Quaternion.Euler(0f, yaw, 0f), Vector3.one));
        }
        return Combinar(l);
    }

    static void MallaQuiote(out Mesh tallo, out Mesh flores)
    {
        var lt = new List<CombineInstance>();
        var lf = new List<CombineInstance>();
        Mesh cil = Builtin("Cylinder.fbx");
        Mesh esf = Builtin("Sphere.fbx");
        Add(lt, cil, Matrix4x4.TRS(new Vector3(0f, 2.3f, 0f), Quaternion.identity, new Vector3(0.14f, 2.3f, 0.14f)));
        for (int k = 0; k < 7; k++)
        {
            float y = 3.0f + k * 0.38f;
            float yaw = k * 120f;
            Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            Add(lt, cil, Matrix4x4.TRS(new Vector3(dir.x * 0.25f, y, dir.z * 0.25f), Quaternion.Euler(0f, yaw, 70f) * Quaternion.Euler(0f, 0f, 0f), new Vector3(0.05f, 0.28f, 0.05f)));
            Add(lf, esf, Matrix4x4.TRS(new Vector3(dir.x * 0.52f, y + 0.08f, dir.z * 0.52f), Quaternion.identity, new Vector3(0.28f, 0.22f, 0.28f)));
        }
        Add(lf, esf, Matrix4x4.TRS(new Vector3(0f, 5.0f, 0f), Quaternion.identity, new Vector3(0.3f, 0.4f, 0.3f)));
        tallo = Combinar(lt);
        flores = Combinar(lf);
    }

    static void MallaMezquite(int semilla, float escala, out Mesh corteza, out Mesh follaje)
    {
        System.Random rng = new System.Random(semilla);
        var lc = new List<CombineInstance>();
        var lf = new List<CombineInstance>();
        Mesh cil = Builtin("Cylinder.fbx");
        Mesh esf = Builtin("Sphere.fbx");

        // Tronco curvo en 3 tramos
        Vector3 p = Vector3.zero;
        float yawTronco = (float)rng.NextDouble() * 360f;
        float[] largos = { 1.3f, 1.1f, 0.9f };
        float[] radios = { 0.20f, 0.15f, 0.11f };
        Quaternion rot = Quaternion.Euler(0f, yawTronco, 0f);
        for (int i = 0; i < 3; i++)
        {
            rot *= Quaternion.Euler(((float)rng.NextDouble() - 0.5f) * 22f, 0f, ((float)rng.NextDouble() - 0.5f) * 22f);
            Vector3 dir = rot * Vector3.up;
            Add(lc, cil, Matrix4x4.TRS(p + dir * (largos[i] * 0.5f), rot, new Vector3(radios[i] * 2f, largos[i] * 0.5f, radios[i] * 2f)));
            p += dir * largos[i];
        }

        // Ramas que abren en sombrilla, con copa aplanada en cada punta
        int ramas = 4 + rng.Next(0, 2);
        for (int i = 0; i < ramas; i++)
        {
            float yaw = i * (360f / ramas) + (float)rng.NextDouble() * 25f;
            float incl = 38f + (float)rng.NextDouble() * 22f;
            Quaternion r = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(incl, 0f, 0f);
            Vector3 dir = r * Vector3.up;
            float largo = 1.5f + (float)rng.NextDouble() * 0.7f;
            Add(lc, cil, Matrix4x4.TRS(p + dir * (largo * 0.5f), r, new Vector3(0.14f, largo * 0.5f, 0.14f)));
            Vector3 punta = p + dir * largo;
            float ancho = 2.4f + (float)rng.NextDouble() * 0.9f;
            Add(lf, esf, Matrix4x4.TRS(punta + Vector3.up * 0.15f, Quaternion.Euler(0f, yaw, 0f), new Vector3(ancho, 1.0f, ancho)));
        }
        Add(lf, esf, Matrix4x4.TRS(p + Vector3.up * 0.55f, Quaternion.identity, new Vector3(3.0f, 1.2f, 3.0f)));

        // escala global
        Matrix4x4 s = Matrix4x4.Scale(Vector3.one * escala);
        corteza = Escalar(Combinar(lc), s);
        follaje = Escalar(Combinar(lf), s);
    }

    static void MallaHuizache(int semilla, out Mesh corteza, out Mesh follaje, out Mesh flores)
    {
        System.Random rng = new System.Random(semilla);
        var lc = new List<CombineInstance>();
        var lf = new List<CombineInstance>();
        var lfl = new List<CombineInstance>();
        Mesh cil = Builtin("Cylinder.fbx");
        Mesh esf = Builtin("Sphere.fbx");

        Add(lc, cil, Matrix4x4.TRS(new Vector3(0f, 0.55f, 0f), Quaternion.identity, new Vector3(0.22f, 0.55f, 0.22f)));
        Vector3 baseRamas = new Vector3(0f, 1.0f, 0f);
        for (int i = 0; i < 5; i++)
        {
            float yaw = i * 72f + (float)rng.NextDouble() * 20f;
            Quaternion r = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(35f + (float)rng.NextDouble() * 25f, 0f, 0f);
            Vector3 dir = r * Vector3.up;
            float largo = 1.0f + (float)rng.NextDouble() * 0.5f;
            Add(lc, cil, Matrix4x4.TRS(baseRamas + dir * (largo * 0.5f), r, new Vector3(0.09f, largo * 0.5f, 0.09f)));
            Vector3 punta = baseRamas + dir * largo;
            float ancho = 1.5f + (float)rng.NextDouble() * 0.5f;
            Add(lf, esf, Matrix4x4.TRS(punta, Quaternion.Euler(0f, yaw, 0f), new Vector3(ancho, 0.85f, ancho)));
            // flores amarillas sobre la copa
            for (int k = 0; k < 4; k++)
            {
                Vector2 c = Random2(rng) * ancho * 0.42f;
                Add(lfl, esf, Matrix4x4.TRS(punta + new Vector3(c.x, 0.38f + (float)rng.NextDouble() * 0.12f, c.y), Quaternion.identity, Vector3.one * 0.15f));
            }
        }
        corteza = Combinar(lc); follaje = Combinar(lf); flores = Combinar(lfl);
    }

    static Vector2 Random2(System.Random rng)
    {
        float a = (float)rng.NextDouble() * Mathf.PI * 2f, r = Mathf.Sqrt((float)rng.NextDouble());
        return new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
    }

    static Mesh MallaOrgano(int semilla)
    {
        var l = new List<CombineInstance>();
        Mesh cap = Builtin("Capsule.fbx");
        // Capsule.fbx: altura 2, radio 0.5, centrada
        Add(l, cap, Matrix4x4.TRS(new Vector3(0f, 1.7f, 0f), Quaternion.identity, new Vector3(0.45f, 1.7f, 0.45f)));
        Add(l, cap, Matrix4x4.TRS(new Vector3(0.55f, 1.45f, 0f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.32f, 0.5f, 0.32f)));
        Add(l, cap, Matrix4x4.TRS(new Vector3(0.95f, 2.05f, 0f), Quaternion.identity, new Vector3(0.32f, 0.95f, 0.32f)));
        Add(l, cap, Matrix4x4.TRS(new Vector3(-0.5f, 2.1f, 0f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.30f, 0.45f, 0.30f)));
        Add(l, cap, Matrix4x4.TRS(new Vector3(-0.88f, 2.6f, 0f), Quaternion.identity, new Vector3(0.30f, 0.8f, 0.30f)));
        return Combinar(l);
    }

    static void MallaBiznaga(out Mesh cuerpo, out Mesh flor)
    {
        var lc = new List<CombineInstance>();
        var lf = new List<CombineInstance>();
        Mesh esf = Builtin("Sphere.fbx");
        Add(lc, esf, Matrix4x4.TRS(new Vector3(0f, 0.28f, 0f), Quaternion.identity, new Vector3(0.7f, 0.56f, 0.7f)));
        for (int k = 0; k < 4; k++)
        {
            float a = k * 90f * Mathf.Deg2Rad;
            Add(lf, esf, Matrix4x4.TRS(new Vector3(Mathf.Cos(a) * 0.12f, 0.56f, Mathf.Sin(a) * 0.12f), Quaternion.identity, Vector3.one * 0.1f));
        }
        cuerpo = Combinar(lc); flor = Combinar(lf);
    }

    static Mesh MallaZacate(int semilla, int briznas)
    {
        System.Random rng = new System.Random(semilla);
        var l = new List<CombineInstance>();
        for (int k = 0; k < briznas; k++)
        {
            float yaw = (float)rng.NextDouble() * 360f;
            float a0 = 4f + (float)rng.NextDouble() * 22f;
            float largo = 0.45f + (float)rng.NextDouble() * 0.4f;
            Mesh h = MallaHoja(largo, 0.032f, 3, a0, 18f + (float)rng.NextDouble() * 22f);
            Vector2 c = Random2(rng) * 0.07f;
            Add(l, h, Matrix4x4.TRS(new Vector3(c.x, 0f, c.y), Quaternion.Euler(0f, yaw, 0f), Vector3.one));
        }
        return Combinar(l);
    }

    static Mesh Escalar(Mesh m, Matrix4x4 s)
    {
        Vector3[] v = m.vertices;
        for (int i = 0; i < v.Length; i++) v[i] = s.MultiplyPoint3x4(v[i]);
        m.vertices = v;
        m.RecalculateBounds();
        return m;
    }
}
