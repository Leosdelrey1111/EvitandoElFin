using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Asienta rocas, peñas y construcciones sobre el terreno para que nada flote en las laderas,
/// y añade detalles ambientales (lápidas, veladoras, faroles coloniales y papel picado) en Nivel 2 y Nivel 3.
/// </summary>
public static class AcomodoEntorno
{
    static readonly Dictionary<string, Material> cacheMat = new Dictionary<string, Material>();

    static Material Mat(Color c, float brillo = 0f)
    {
        string k = ColorUtility.ToHtmlStringRGBA(c) + "_" + brillo.ToString("0.00");
        Material m;
        if (cacheMat.TryGetValue(k, out m) && m != null) return m;
        Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        m = new Material(s);
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (brillo > 0f)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * brillo);
        }
        cacheMat[k] = m;
        return m;
    }

    static GameObject Parte(Transform padre, string nombre, PrimitiveType tipo, Vector3 pos, Vector3 esc, Vector3 rot, Material m)
    {
        GameObject g = GameObject.CreatePrimitive(tipo);
        g.name = nombre;
        g.transform.SetParent(padre, false);
        g.transform.localPosition = pos;
        g.transform.localRotation = Quaternion.Euler(rot);
        g.transform.localScale = esc;
        g.GetComponent<Renderer>().sharedMaterial = m;
        Collider c = g.GetComponent<Collider>();
        if (c != null) Object.Destroy(c);
        return g;
    }

    public static void Acomodar(string escena)
    {
        if (GameObject.Find("_EntornoEnriquecido") != null) return;
        GameObject marca = new GameObject("_EntornoEnriquecido");

        AsentarRocasYProps();

        if (escena.Contains("Nivel2") || escena.Contains("Panteon"))
            EnriquecerPanteon(marca.transform);
        else if (escena.Contains("Nivel3") || escena.Contains("Ciudad"))
            EnriquecerCiudad(marca.transform);
    }

    /// <summary>
    /// Baja cualquier roca, peña, lápida o casa que haya quedado flotando en laderas inclinadas
    /// usando el punto más bajo del terreno bajo su huella, y aparta rocas que invadan el centro del sendero.
    /// </summary>
    static void AsentarRocasYProps()
    {
        Vector3[] ruta = SenderoNivel.Instancia != null && SenderoNivel.Instancia.Valido ? SenderoNivel.Instancia.puntos : null;

        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
        {
            if (t == null || (t.parent != null && t.parent.name.StartsWith("Arena"))) continue;
            string n = t.name;
            bool esRoca = n.StartsWith("Pena_") || n == "PilaPiedras" || n.StartsWith("Roca") || n.StartsWith("Rock");
            bool esFlora = n.StartsWith("Maguey") || n.StartsWith("Nopal") || n.StartsWith("Organo") ||
                           n.StartsWith("Mezquite") || n.StartsWith("Huizache");
            bool esProp = n.StartsWith("Cruz_Camino") || n.StartsWith("Tumba_") || n.StartsWith("Gavetas_") ||
                          n.StartsWith("Casa_Colonial") || n.StartsWith("Ofrenda_") || n.StartsWith("Puesto_DonChuy");
            if (!esRoca && !esFlora && !esProp) continue;

            Renderer[] rends = t.GetComponentsInChildren<Renderer>();
            if (rends == null || rends.Length == 0) continue;

            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);

            // Si una roca quedó muy adentro del sendero, orillarla ligeramente para enmarcar el camino
            if (esRoca && ruta != null)
            {
                Vector3 cXZ = new Vector3(b.center.x, 0f, b.center.z);
                float dist = ArreglosEscenaRuntime.DistanciaASenderoXZ(cXZ, ruta);
                if (dist < 4.5f)
                {
                    Vector3 masCercano = PuntoMasCercanoXZ(cXZ, ruta);
                    Vector3 fuera = cXZ - masCercano;
                    fuera.y = 0f;
                    if (fuera.sqrMagnitude < 0.01f) fuera = Vector3.right;
                    fuera.Normalize();
                    t.position += fuera * (6.2f - dist);
                    Physics.SyncTransforms();
                    b = rends[0].bounds;
                    for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                }
            }

            // Muestrear el terreno en el centro y las 4 esquinas de la huella para que ninguna orilla flote
            float minTerreno = float.MaxValue;
            bool hayTerreno = false;
            Vector2[] muestras = {
                Vector2.zero,
                new Vector2(b.extents.x * 0.75f, b.extents.z * 0.75f),
                new Vector2(-b.extents.x * 0.75f, b.extents.z * 0.75f),
                new Vector2(b.extents.x * 0.75f, -b.extents.z * 0.75f),
                new Vector2(-b.extents.x * 0.75f, -b.extents.z * 0.75f)
            };
            foreach (Vector2 off in muestras)
            {
                float h;
                if (AcomodoTotems.AlturaTerreno(new Vector3(b.center.x + off.x, 0f, b.center.z + off.y), out h))
                {
                    if (h < minTerreno) minTerreno = h;
                    hayTerreno = true;
                }
            }
            if (!hayTerreno) continue;

            float hundimiento = esRoca ? 0.45f : (esFlora ? 0.25f : 0.15f);
            float objetivoMinY = minTerreno - hundimiento;
            float delta = objetivoMinY - b.min.y;
            if (delta < -0.15f || delta > 0.35f)
            {
                t.position += Vector3.up * delta;
            }
        }
    }

    static Vector3 PuntoMasCercanoXZ(Vector3 p, Vector3[] ruta)
    {
        Vector3 mejor = ruta[0];
        float minD = float.MaxValue;
        for (int i = 0; i < ruta.Length - 1; i++)
        {
            Vector3 a = new Vector3(ruta[i].x, 0f, ruta[i].z);
            Vector3 b = new Vector3(ruta[i + 1].x, 0f, ruta[i + 1].z);
            Vector3 ab = b - a;
            float magSq = ab.sqrMagnitude;
            float k = magSq > 0.0001f ? Mathf.Clamp01(Vector3.Dot(p - a, ab) / magSq) : 0f;
            Vector3 q = a + ab * k;
            float d = (p - q).sqrMagnitude;
            if (d < minD) { minD = d; mejor = q; }
        }
        return mejor;
    }

    // ------------------------------------------------------------------ NIVEL 2: PANTEÓN DE SANTA PAULA

    static void EnriquecerPanteon(Transform raiz)
    {
        if (SenderoNivel.Instancia == null || !SenderoNivel.Instancia.Valido) return;

        Material cantera = Mat(new Color(0.55f, 0.43f, 0.40f));
        Material piedraOscura = Mat(new Color(0.28f, 0.27f, 0.30f));
        Material hierro = Mat(new Color(0.14f, 0.14f, 0.16f));
        Material vela = Mat(new Color(0.95f, 0.90f, 0.72f));
        Material flamaAmbar = Mat(new Color(1f, 0.65f, 0.15f), 2.4f);
        Material flamaVerde = Mat(new Color(0.2f, 0.95f, 0.5f), 2.0f);

        Vector3[] pts = SenderoNivel.Instancia.puntos;
        int paso = 0;
        for (int i = 0; i < pts.Length - 1; i++)
        {
            Vector3 a = pts[i];
            Vector3 b = pts[i + 1];
            Vector3 d = new Vector3(b.x - a.x, 0f, b.z - a.z);
            float len = d.magnitude;
            if (len < 1f) continue;
            Vector3 dir = d / len;
            Vector3 perp = new Vector3(dir.z, 0f, -dir.x);

            int marcas = Mathf.Max(1, Mathf.FloorToInt(len / 26f));
            for (int m = 0; m < marcas; m++)
            {
                float t = (m + 0.5f) / marcas;
                float lado = (paso % 2 == 0) ? 1f : -1f;
                Vector3 pos = Vector3.Lerp(a, b, t) + perp * (lado * 5.2f);
                float h;
                if (!AcomodoTotems.AlturaTerreno(pos, out h)) continue;
                pos.y = h - 0.1f;

                if (paso % 3 == 0)
                    CrearFarolPanteon(raiz, pos, dir, hierro, flamaVerde);
                else
                    CrearLapidaConVeladoras(raiz, pos, perp * -lado, cantera, piedraOscura, vela, flamaAmbar);

                paso++;
            }
        }
    }

    static void CrearLapidaConVeladoras(Transform padre, Vector3 pos, Vector3 mirando, Material cantera, Material oscura, Material vela, Material flama)
    {
        GameObject go = new GameObject("LapidaAmbiental");
        go.transform.SetParent(padre, false);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.LookRotation(mirando) * Quaternion.Euler(Random.Range(-4f, 4f), Random.Range(-15f, 15f), Random.Range(-3f, 3f));

        Parte(go.transform, "Base", PrimitiveType.Cube, new Vector3(0f, 0.15f, 0f), new Vector3(1.2f, 0.3f, 0.8f), Vector3.zero, oscura);
        Parte(go.transform, "Estela", PrimitiveType.Cube, new Vector3(0f, 0.85f, -0.15f), new Vector3(0.85f, 1.25f, 0.22f), Vector3.zero, cantera);
        Parte(go.transform, "Remate", PrimitiveType.Sphere, new Vector3(0f, 1.48f, -0.15f), new Vector3(0.84f, 0.4f, 0.22f), Vector3.zero, cantera);

        // Veladoras encendidas al pie de la lápida
        for (int v = -1; v <= 1; v += 2)
        {
            Parte(go.transform, "Vela" + v, PrimitiveType.Cylinder, new Vector3(v * 0.28f, 0.38f, 0.18f), new Vector3(0.12f, 0.1f, 0.12f), Vector3.zero, vela);
            Parte(go.transform, "Llama" + v, PrimitiveType.Sphere, new Vector3(v * 0.28f, 0.52f, 0.18f), new Vector3(0.08f, 0.12f, 0.08f), Vector3.zero, flama);
        }

        GameObject luzGo = new GameObject("LuzVeladora");
        luzGo.transform.SetParent(go.transform, false);
        luzGo.transform.localPosition = new Vector3(0f, 0.8f, 0.3f);
        Light l = luzGo.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.68f, 0.28f);
        l.range = 7f;
        l.intensity = 1.0f;
        l.shadows = LightShadows.None;
    }

    static void CrearFarolPanteon(Transform padre, Vector3 pos, Vector3 dir, Material hierro, Material flama)
    {
        GameObject go = new GameObject("FarolPanteon");
        go.transform.SetParent(padre, false);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.LookRotation(dir);

        Parte(go.transform, "Poste", PrimitiveType.Cylinder, new Vector3(0f, 1.6f, 0f), new Vector3(0.14f, 1.6f, 0.14f), Vector3.zero, hierro);
        Parte(go.transform, "Brazo", PrimitiveType.Cube, new Vector3(0f, 3.1f, 0.35f), new Vector3(0.1f, 0.1f, 0.7f), Vector3.zero, hierro);
        Parte(go.transform, "Linterna", PrimitiveType.Cube, new Vector3(0f, 2.75f, 0.65f), new Vector3(0.32f, 0.42f, 0.32f), Vector3.zero, flama);

        GameObject luzGo = new GameObject("LuzFarol");
        luzGo.transform.SetParent(go.transform, false);
        luzGo.transform.localPosition = new Vector3(0f, 2.7f, 0.65f);
        Light l = luzGo.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(0.25f, 0.95f, 0.55f);
        l.range = 12f;
        l.intensity = 1.6f;
        l.shadows = LightShadows.None;
    }

    // ------------------------------------------------------------------ NIVEL 3: CIUDAD COLONIAL

    static void EnriquecerCiudad(Transform raiz)
    {
        if (SenderoNivel.Instancia == null || !SenderoNivel.Instancia.Valido) return;

        Material hierro = Mat(new Color(0.12f, 0.12f, 0.14f));
        Material ambar = Mat(new Color(1f, 0.72f, 0.25f), 2.4f);
        Material barro = Mat(new Color(0.62f, 0.32f, 0.18f));
        Material cempasuchil = Mat(new Color(1f, 0.55f, 0.05f), 0.7f);
        Material[] coloresPapel = {
            Mat(new Color(0.92f, 0.18f, 0.45f), 0.4f),
            Mat(new Color(0.95f, 0.55f, 0.10f), 0.4f),
            Mat(new Color(0.15f, 0.78f, 0.72f), 0.4f),
            Mat(new Color(0.60f, 0.22f, 0.85f), 0.4f)
        };

        Vector3[] pts = SenderoNivel.Instancia.puntos;
        int paso = 0;
        for (int i = 0; i < pts.Length - 1; i++)
        {
            Vector3 a = pts[i];
            Vector3 b = pts[i + 1];
            Vector3 d = new Vector3(b.x - a.x, 0f, b.z - a.z);
            float len = d.magnitude;
            if (len < 1f) continue;
            Vector3 dir = d / len;
            Vector3 perp = new Vector3(dir.z, 0f, -dir.x);

            int marcas = Mathf.Max(1, Mathf.FloorToInt(len / 24f));
            for (int m = 0; m < marcas; m++)
            {
                float t = (m + 0.5f) / marcas;
                float lado = (paso % 2 == 0) ? 1f : -1f;
                Vector3 centro = Vector3.Lerp(a, b, t);
                Vector3 pos = centro + perp * (lado * 5.0f);
                float h;
                if (!AcomodoTotems.AlturaTerreno(pos, out h)) continue;
                pos.y = h - 0.1f;

                CrearFarolColonial(raiz, pos, perp * -lado, hierro, ambar);

                if (paso % 2 == 0)
                    CrearMacetaCempasuchil(raiz, pos + dir * 1.4f, barro, cempasuchil);

                if (paso % 3 == 1)
                {
                    float hCentro;
                    if (AcomodoTotems.AlturaTerreno(centro, out hCentro))
                        CrearTiraPapelPicado(raiz, new Vector3(centro.x, hCentro + 4.8f, centro.z), dir, hierro, coloresPapel);
                }

                paso++;
            }
        }
    }

    static void CrearFarolColonial(Transform padre, Vector3 pos, Vector3 mirando, Material hierro, Material luzMat)
    {
        GameObject go = new GameObject("FarolColonial");
        go.transform.SetParent(padre, false);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.LookRotation(mirando);

        Parte(go.transform, "Base", PrimitiveType.Cylinder, new Vector3(0f, 0.25f, 0f), new Vector3(0.32f, 0.25f, 0.32f), Vector3.zero, hierro);
        Parte(go.transform, "Poste", PrimitiveType.Cylinder, new Vector3(0f, 1.9f, 0f), new Vector3(0.12f, 1.75f, 0.12f), Vector3.zero, hierro);
        Parte(go.transform, "Faro", PrimitiveType.Cube, new Vector3(0f, 3.7f, 0f), new Vector3(0.36f, 0.45f, 0.36f), Vector3.zero, luzMat);
        Parte(go.transform, "Capitel", PrimitiveType.Cube, new Vector3(0f, 4.0f, 0f), new Vector3(0.46f, 0.12f, 0.46f), Vector3.zero, hierro);

        GameObject luzGo = new GameObject("LuzFarol");
        luzGo.transform.SetParent(go.transform, false);
        luzGo.transform.localPosition = new Vector3(0f, 3.65f, 0f);
        Light l = luzGo.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.72f, 0.32f);
        l.range = 12f;
        l.intensity = 1.6f;
        l.shadows = LightShadows.None;
    }

    static void CrearMacetaCempasuchil(Transform padre, Vector3 pos, Material barro, Material flor)
    {
        GameObject go = new GameObject("MacetaCempasuchil");
        go.transform.SetParent(padre, false);
        go.transform.position = pos;
        Parte(go.transform, "Maceta", PrimitiveType.Cylinder, new Vector3(0f, 0.3f, 0f), new Vector3(0.65f, 0.3f, 0.65f), Vector3.zero, barro);
        Parte(go.transform, "Flores", PrimitiveType.Sphere, new Vector3(0f, 0.7f, 0f), new Vector3(0.8f, 0.55f, 0.8f), Vector3.zero, flor);
    }

    static void CrearTiraPapelPicado(Transform padre, Vector3 pos, Vector3 dir, Material cuerda, Material[] colores)
    {
        GameObject go = new GameObject("PapelPicado");
        go.transform.SetParent(padre, false);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.LookRotation(dir);

        Parte(go.transform, "Cuerda", PrimitiveType.Cube, Vector3.zero, new Vector3(11f, 0.03f, 0.03f), Vector3.zero, cuerda);

        // Postes de soporte en ambos extremos aterrizados al terreno para que la tira no flote en el aire
        for (int s = -1; s <= 1; s += 2)
        {
            Vector3 baseMundo = go.transform.TransformPoint(new Vector3(s * 5.5f, 0f, 0f));
            float hSuelo;
            float baseLocalY = AcomodoTotems.AlturaTerreno(baseMundo, out hSuelo) ? (hSuelo - pos.y - 0.1f) : -5.0f;
            float alto = Mathf.Max(2f, -baseLocalY);
            Parte(go.transform, "Poste" + s, PrimitiveType.Cylinder,
                new Vector3(s * 5.5f, baseLocalY + alto * 0.5f, 0f),
                new Vector3(0.11f, alto * 0.5f, 0.11f), Vector3.zero, cuerda);
        }

        for (int i = -4; i <= 4; i++)
        {
            float x = i * 1.1f;
            float curva = -0.25f * (1f - (x * x) / 25f);
            Parte(go.transform, "Banderin" + i, PrimitiveType.Cube,
                new Vector3(x, curva - 0.28f, 0f), new Vector3(0.68f, 0.48f, 0.02f), Vector3.zero,
                colores[Mathf.Abs(i) % colores.Length]);
        }
    }
}
