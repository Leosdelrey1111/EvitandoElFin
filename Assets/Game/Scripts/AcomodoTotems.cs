using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Acomoda los tótems coleccionables de cada nivel en runtime:
///  - Los apoya en el suelo real (terreno / piso), nada enterrado ni flotando.
///  - Les da forma temática según el jefe del nivel:
///      Nivel 1 (La Bufa)         -> Piedra encantada con rostro tallado y corona de rocas; cristal ámbar.
///      Nivel 2 (General Momia)   -> Cirio vendado con medalla y sable; flama de fuego fatuo verde.
///      Nivel 3 (Inquisidor)      -> Atril de piedra con paño rojo; sello dorado con lacre y cruz.
///  - El pedestal queda fijo en el suelo; solo la pieza coleccionable gira y flota encima.
/// </summary>
public static class AcomodoTotems
{
    const string NombreModelo = "ModeloTematico";
    static readonly Dictionary<string, Material> cacheMat = new Dictionary<string, Material>();

    // ------------------------------------------------------------------ utilidades

    static Material Mat(Color c, float brillo = 0f)
    {
        string clave = ColorUtility.ToHtmlStringRGBA(c) + "_" + brillo.ToString("0.00");
        Material m;
        if (cacheMat.TryGetValue(clave, out m) && m != null) return m;
        Shader s = Shader.Find("Universal Render Pipeline/Lit");
        if (s == null) s = Shader.Find("Standard");
        m = new Material(s);
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (brillo > 0f)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * brillo);
        }
        cacheMat[clave] = m;
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

    static void Luz(Transform padre, Vector3 pos, Color c, float rango, float intensidad)
    {
        GameObject go = new GameObject("LuzTotemTematica");
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        Light l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = c;
        l.range = rango;
        l.intensity = intensidad;
        l.shadows = LightShadows.None;
    }

    /// <summary>Contenedor hijo que anula la escala del padre: así todo se mide en metros reales.</summary>
    static Transform Contenedor(Transform padre, string nombre)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        Vector3 ls = padre.lossyScale;
        go.transform.localScale = new Vector3(
            Mathf.Abs(ls.x) > 0.0001f ? 1f / ls.x : 1f,
            Mathf.Abs(ls.y) > 0.0001f ? 1f / ls.y : 1f,
            Mathf.Abs(ls.z) > 0.0001f ? 1f / ls.z : 1f);
        return go.transform;
    }

    /// <summary>Altura del terreno en ese punto (si hay un Terrain debajo).</summary>
    public static bool AlturaTerreno(Vector3 p, out float y)
    {
        foreach (Terrain t in Terrain.activeTerrains)
        {
            if (t == null || t.terrainData == null) continue;
            Vector3 tp = t.transform.position;
            Vector3 s = t.terrainData.size;
            if (p.x < tp.x || p.z < tp.z || p.x > tp.x + s.x || p.z > tp.z + s.z) continue;
            y = t.SampleHeight(p) + tp.y;
            return true;
        }
        y = 0f;
        return false;
    }

    /// <summary>
    /// Altura del suelo pisable bajo 'p': el terreno, o un piso/plataforma que esté a lo más 3 m por encima del terreno.
    /// Ignora triggers, al jugador, enemigos y todo lo que cuelgue de 'ignorar'.
    /// </summary>
    public static bool AlturaSuelo(Vector3 p, Transform ignorar, out float y)
    {
        float terreno;
        bool hayTerreno = AlturaTerreno(p, out terreno);
        float origenY = hayTerreno ? terreno + 3f : p.y + 4f;

        RaycastHit[] hits = Physics.RaycastAll(new Vector3(p.x, origenY, p.z), Vector3.down, 60f, ~0, QueryTriggerInteraction.Ignore);
        float mejor = float.MinValue;
        bool hay = false;
        foreach (RaycastHit h in hits)
        {
            if (h.collider == null) continue;
            Transform ht = h.collider.transform;
            if (ignorar != null && ht.IsChildOf(ignorar)) continue;
            if (h.collider.CompareTag("Player") || h.collider.CompareTag("Enemigo")) continue;
            if (h.collider.GetComponentInParent<CharacterController>() != null && !(h.collider is TerrainCollider)) continue;
            if (h.normal.y < 0.6f) continue; // paredes / laterales de rocas no cuentan como piso
            if (h.point.y > mejor) { mejor = h.point.y; hay = true; }
        }

        if (hayTerreno && (!hay || mejor < terreno - 0.05f)) { mejor = terreno; hay = true; }
        y = mejor;
        return hay;
    }

    static int NivelDeEscena(string escena)
    {
        if (escena.Contains("Nivel2") || escena.Contains("Panteon")) return 2;
        if (escena.Contains("Nivel3") || escena.Contains("Ciudad")) return 3;
        return 1;
    }

    // ------------------------------------------------------------------ API

    public static void Acomodar(string escena)
    {
        int nivel = NivelDeEscena(escena);
        Physics.SyncTransforms();

        foreach (TotemColeccionable tc in Object.FindObjectsByType<TotemColeccionable>(FindObjectsInactive.Exclude))
        {
            if (tc == null) continue;
            Transform t = tc.transform;

            // Raíz del tótem: el ancestro "Totem_..." (escenas horneadas; NO el grupo "Totems_Y_Emboscadas")
            // o un pedestal nuevo junto al coleccionable
            Transform raiz = null;
            Transform busca = t.parent;
            for (int nivelPadre = 0; busca != null && nivelPadre < 3; nivelPadre++, busca = busca.parent)
            {
                if (busca.name.StartsWith("Totem_")) { raiz = busca; break; }
            }
            if (raiz == null)
            {
                string nombrePed = t.name + "_Pedestal";
                Transform existente = t.parent != null ? t.parent.Find(nombrePed) : null;
                if (existente == null)
                {
                    GameObject ped = new GameObject(nombrePed);
                    ped.transform.SetParent(t.parent, false);
                    ped.transform.position = t.position;
                    existente = ped.transform;
                }
                raiz = existente;
            }

            if (raiz.Find(NombreModelo) != null) continue; // ya acomodado

            // 1) Apagar el modelo viejo (visual, luces y colliders sólidos) para que no queden piezas sueltas
            foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            foreach (Renderer r in t.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            foreach (Light l in raiz.GetComponentsInChildren<Light>(true)) l.enabled = false;
            foreach (Light l in t.GetComponentsInChildren<Light>(true)) l.enabled = false;
            foreach (Collider c in raiz.GetComponentsInChildren<Collider>(true)) if (!c.isTrigger) c.enabled = false;
            foreach (Collider c in t.GetComponentsInChildren<Collider>(true)) if (!c.isTrigger && c.transform != t) c.enabled = false;

            // 2) Apoyar el pedestal en el suelo real
            float suelo;
            if (AlturaSuelo(raiz.position, raiz, out suelo))
                raiz.position = new Vector3(raiz.position.x, suelo, raiz.position.z);

            // 3) Construir pedestal temático (fijo) y la pieza coleccionable (gira/flota)
            Transform modelo = Contenedor(raiz, NombreModelo);
            float alturaPieza;
            switch (nivel)
            {
                case 2: alturaPieza = PedestalCirio(modelo); break;
                case 3: alturaPieza = PedestalSello(modelo); break;
                default: alturaPieza = PedestalBufa(modelo); break;
            }

            // Collider sólido pequeño del pedestal (su nombre contiene "Totem": el despeje de senderos no lo apaga)
            GameObject col = new GameObject("TotemBaseCollider");
            col.transform.SetParent(modelo, false);
            CapsuleCollider cap = col.AddComponent<CapsuleCollider>();
            cap.radius = 0.5f;
            cap.height = alturaPieza - 0.3f;
            cap.center = new Vector3(0f, cap.height * 0.5f, 0f);

            t.position = raiz.position + Vector3.up * alturaPieza;
            t.rotation = Quaternion.identity;

            if (nivel == 2)
            {
                // Nivel 2: el cirio empieza APAGADO; al acercarse se enciende la flama verde y se queda prendida
                Transform flama = Contenedor(raiz, "FlamaEncendida");
                flama.position = raiz.position + Vector3.up * alturaPieza;
                PiezaFlamaFatua(flama);
                flama.gameObject.SetActive(false);

                // Guía sutil sobre la mecha para ubicar el cirio apagado en la oscuridad
                Transform guia = Contenedor(t, "IndicadorApagado");
                Material matGuia = Mat(new Color(0.25f, 0.65f, 0.50f), 0.7f);
                Parte(guia, "AroGuia", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.22f, Vector3.zero, matGuia);
                Luz(guia, Vector3.zero, new Color(0.2f, 0.7f, 0.5f), 4.5f, 0.65f);
            }
            else
            {
                Transform pieza = Contenedor(t, "PiezaTematica");
                if (nivel == 3) PiezaSelloDorado(pieza);
                else PiezaCristalBufa(pieza);
            }

            // 4) Trigger cómodo de recoger (2.3 m reales) y flotación suave
            SphereCollider sc = t.GetComponent<SphereCollider>();
            if (sc == null) sc = t.gameObject.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.center = Vector3.zero;
            float escMax = Mathf.Max(Mathf.Abs(t.lossyScale.x), Mathf.Abs(t.lossyScale.y), Mathf.Abs(t.lossyScale.z));
            sc.radius = 2.3f / Mathf.Max(0.0001f, escMax);

            tc.amplitudFlotacion = 0.12f;
            tc.frecuenciaFlotacion = 1.6f;
            tc.velocidadRotacion = 45f;
            tc.Reubicar();
        }
    }

    // ------------------------------------------------------------------ NIVEL 1: LA BUFA (piedra encantada)

    static float PedestalBufa(Transform m)
    {
        Material rocaOscura = Mat(new Color(0.33f, 0.31f, 0.30f));
        Material cantera = Mat(new Color(0.64f, 0.50f, 0.46f));
        Material ranura = Mat(new Color(0.08f, 0.06f, 0.06f));
        Material ambar = Mat(new Color(1f, 0.62f, 0.25f), 2.2f);

        Parte(m, "PiedraBase1", PrimitiveType.Cube, new Vector3(0f, 0.17f, 0f), new Vector3(1.6f, 0.34f, 1.5f), new Vector3(0f, 15f, 0f), rocaOscura);
        Parte(m, "PiedraBase2", PrimitiveType.Cube, new Vector3(0.05f, 0.48f, -0.03f), new Vector3(1.15f, 0.3f, 1.1f), new Vector3(0f, 38f, 0f), rocaOscura);
        Parte(m, "Monolito", PrimitiveType.Cube, new Vector3(0f, 1.25f, 0f), new Vector3(0.72f, 1.25f, 0.56f), Vector3.zero, cantera);

        // Rostro tallado de la joven hecha piedra
        Parte(m, "OjoI", PrimitiveType.Cube, new Vector3(-0.16f, 1.58f, 0.285f), new Vector3(0.16f, 0.05f, 0.03f), Vector3.zero, ambar);
        Parte(m, "OjoD", PrimitiveType.Cube, new Vector3(0.16f, 1.58f, 0.285f), new Vector3(0.16f, 0.05f, 0.03f), Vector3.zero, ambar);
        Parte(m, "Boca", PrimitiveType.Cube, new Vector3(0f, 1.36f, 0.285f), new Vector3(0.22f, 0.04f, 0.03f), Vector3.zero, ranura);
        Parte(m, "Trenza", PrimitiveType.Cube, new Vector3(0f, 1.2f, -0.3f), new Vector3(0.14f, 0.8f, 0.06f), Vector3.zero, rocaOscura);

        // Corona de rocas (la corona del Cerro de La Bufa)
        for (int i = 0; i < 6; i++)
        {
            float a = i * 60f * Mathf.Deg2Rad;
            Parte(m, "Corona" + i, PrimitiveType.Cube,
                new Vector3(Mathf.Sin(a) * 0.26f, 1.98f, Mathf.Cos(a) * 0.2f),
                new Vector3(0.16f, 0.22f + (i % 2) * 0.1f, 0.16f), new Vector3(10f, i * 60f, 8f), cantera);
        }
        return 2.55f;
    }

    static void PiezaCristalBufa(Transform p)
    {
        Material ambar = Mat(new Color(1f, 0.58f, 0.30f), 2.4f);
        Material nucleo = Mat(new Color(1f, 0.9f, 0.7f), 3.2f);
        Parte(p, "CristalAmbar", PrimitiveType.Cube, Vector3.zero, new Vector3(0.36f, 0.36f, 0.36f), new Vector3(45f, 0f, 45f), ambar);
        Parte(p, "PuntaArriba", PrimitiveType.Cube, new Vector3(0f, 0.26f, 0f), new Vector3(0.16f, 0.16f, 0.16f), new Vector3(45f, 0f, 45f), ambar);
        Parte(p, "Nucleo", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.18f, Vector3.zero, nucleo);
        Luz(p, Vector3.zero, new Color(1f, 0.65f, 0.3f), 7f, 1.6f);
    }

    // ------------------------------------------------------------------ NIVEL 2: GENERAL MOMIA (cirio vendado)

    static float PedestalCirio(Transform m)
    {
        Material piedra = Mat(new Color(0.30f, 0.28f, 0.30f));
        Material cantera = Mat(new Color(0.72f, 0.52f, 0.46f));
        Material hierro = Mat(new Color(0.12f, 0.12f, 0.13f));
        Material cera = Mat(new Color(0.90f, 0.86f, 0.72f));
        Material venda = Mat(new Color(0.66f, 0.60f, 0.48f));
        Material oro = Mat(new Color(0.85f, 0.70f, 0.22f), 0.5f);
        Material rojo = Mat(new Color(0.6f, 0.08f, 0.08f));
        Material acero = Mat(new Color(0.7f, 0.72f, 0.75f), 0.2f);

        Parte(m, "Zocalo", PrimitiveType.Cube, new Vector3(0f, 0.15f, 0f), new Vector3(1.3f, 0.3f, 1.3f), Vector3.zero, piedra);
        Parte(m, "Peldano", PrimitiveType.Cube, new Vector3(0f, 0.42f, 0f), new Vector3(1.0f, 0.24f, 1.0f), Vector3.zero, cantera);
        Parte(m, "Candelero", PrimitiveType.Cylinder, new Vector3(0f, 0.57f, 0f), new Vector3(0.8f, 0.03f, 0.8f), Vector3.zero, hierro);
        Parte(m, "Cera", PrimitiveType.Cylinder, new Vector3(0f, 1.25f, 0f), new Vector3(0.42f, 0.65f, 0.42f), Vector3.zero, cera);

        // Vendas de momia enrolladas en el cirio
        Parte(m, "Venda1", PrimitiveType.Cylinder, new Vector3(0f, 0.85f, 0f), new Vector3(0.46f, 0.035f, 0.46f), new Vector3(0f, 0f, 7f), venda);
        Parte(m, "Venda2", PrimitiveType.Cylinder, new Vector3(0f, 1.22f, 0f), new Vector3(0.46f, 0.035f, 0.46f), new Vector3(0f, 0f, -6f), venda);
        Parte(m, "Venda3", PrimitiveType.Cylinder, new Vector3(0f, 1.62f, 0f), new Vector3(0.46f, 0.035f, 0.46f), new Vector3(5f, 0f, 4f), venda);

        // Medalla militar del General
        Parte(m, "Liston", PrimitiveType.Cube, new Vector3(0f, 1.5f, 0.215f), new Vector3(0.09f, 0.14f, 0.02f), Vector3.zero, rojo);
        Parte(m, "Medalla", PrimitiveType.Cylinder, new Vector3(0f, 1.38f, 0.22f), new Vector3(0.15f, 0.015f, 0.15f), new Vector3(90f, 0f, 0f), oro);

        // Sable de mando recargado en el candelero
        Parte(m, "Sable", PrimitiveType.Cube, new Vector3(0.36f, 1.0f, 0.05f), new Vector3(0.05f, 0.95f, 0.02f), new Vector3(0f, 0f, -14f), acero);
        Parte(m, "Guarda", PrimitiveType.Cube, new Vector3(0.47f, 1.48f, 0.05f), new Vector3(0.18f, 0.04f, 0.05f), new Vector3(0f, 0f, -14f), oro);

        Parte(m, "Mecha", PrimitiveType.Cylinder, new Vector3(0f, 1.95f, 0f), new Vector3(0.04f, 0.06f, 0.04f), Vector3.zero, hierro);
        return 2.3f;
    }

    static void PiezaFlamaFatua(Transform p)
    {
        Material verde = Mat(new Color(0.15f, 0.95f, 0.45f), 2.6f);
        Material centro = Mat(new Color(0.75f, 1f, 0.85f), 3.5f);
        Parte(p, "Flama", PrimitiveType.Sphere, Vector3.zero, new Vector3(0.3f, 0.48f, 0.3f), Vector3.zero, verde);
        Parte(p, "Punta", PrimitiveType.Sphere, new Vector3(0f, 0.26f, 0f), new Vector3(0.14f, 0.22f, 0.14f), Vector3.zero, verde);
        Parte(p, "Corazon", PrimitiveType.Sphere, new Vector3(0f, -0.06f, 0f), new Vector3(0.14f, 0.22f, 0.14f), Vector3.zero, centro);
        Luz(p, Vector3.zero, new Color(0.2f, 0.95f, 0.55f), 8f, 1.8f);
    }

    // ------------------------------------------------------------------ NIVEL 3: INQUISIDOR (sello inquisitorial)

    static float PedestalSello(Transform m)
    {
        Material piedra = Mat(new Color(0.26f, 0.24f, 0.25f));
        Material madera = Mat(new Color(0.28f, 0.18f, 0.12f));
        Material pano = Mat(new Color(0.55f, 0.05f, 0.07f));
        Material oro = Mat(new Color(0.85f, 0.70f, 0.22f), 0.4f);

        Parte(m, "Base", PrimitiveType.Cube, new Vector3(0f, 0.15f, 0f), new Vector3(1.3f, 0.3f, 1.3f), Vector3.zero, piedra);
        Parte(m, "Columna", PrimitiveType.Cube, new Vector3(0f, 0.85f, 0f), new Vector3(0.5f, 1.1f, 0.5f), Vector3.zero, madera);
        Parte(m, "Atril", PrimitiveType.Cube, new Vector3(0f, 1.46f, 0f), new Vector3(0.85f, 0.12f, 0.85f), Vector3.zero, piedra);
        Parte(m, "MolduraOro", PrimitiveType.Cube, new Vector3(0f, 1.39f, 0f), new Vector3(0.88f, 0.03f, 0.88f), Vector3.zero, oro);
        Parte(m, "PanoRojo", PrimitiveType.Cube, new Vector3(0f, 1.0f, 0.26f), new Vector3(0.46f, 0.7f, 0.02f), Vector3.zero, pano);
        Parte(m, "CruzPanoV", PrimitiveType.Cube, new Vector3(0f, 1.05f, 0.275f), new Vector3(0.05f, 0.34f, 0.01f), Vector3.zero, oro);
        Parte(m, "CruzPanoH", PrimitiveType.Cube, new Vector3(0f, 1.12f, 0.275f), new Vector3(0.2f, 0.05f, 0.01f), Vector3.zero, oro);
        return 2.15f;
    }

    static void PiezaSelloDorado(Transform p)
    {
        Material oro = Mat(new Color(0.95f, 0.78f, 0.25f), 1.2f);
        Material lacre = Mat(new Color(0.75f, 0.06f, 0.08f), 0.8f);
        Parte(p, "DiscoOro", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.7f, 0.03f, 0.7f), new Vector3(90f, 0f, 0f), oro);
        Parte(p, "Lacre", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.42f, 0.045f, 0.42f), new Vector3(90f, 0f, 0f), lacre);
        Parte(p, "CruzV", PrimitiveType.Cube, new Vector3(0f, 0.02f, 0.06f), new Vector3(0.06f, 0.3f, 0.03f), Vector3.zero, oro);
        Parte(p, "CruzH", PrimitiveType.Cube, new Vector3(0f, 0.07f, 0.06f), new Vector3(0.2f, 0.06f, 0.03f), Vector3.zero, oro);
        Parte(p, "CruzVb", PrimitiveType.Cube, new Vector3(0f, 0.02f, -0.06f), new Vector3(0.06f, 0.3f, 0.03f), Vector3.zero, oro);
        Parte(p, "CruzHb", PrimitiveType.Cube, new Vector3(0f, 0.07f, -0.06f), new Vector3(0.2f, 0.06f, 0.03f), Vector3.zero, oro);
        Luz(p, Vector3.zero, new Color(1f, 0.55f, 0.3f), 7f, 1.6f);
    }
}
