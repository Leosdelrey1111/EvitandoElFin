using UnityEngine;

/// <summary>
/// Sarcófago monumental del General Momia (Nivel 2, Panteón de Santa Paula).
/// Sustituye al "cubo liso" que había antes: zócalo escalonado, cuerpo de cantera con moldura,
/// tapa desplazada que deja ver el interior abierto (con resplandor de fuego fatuo), cruz de cantera
/// en la cabecera y veladoras en las esquinas.
///
/// IMPORTANTE: el sarcófago es solo decorado + UN collider. El General NO nace dentro de él:
/// aparece de pie delante de la tumba (centro de la arena), así ya no se ve "la cabeza saliendo de la caja".
/// </summary>
public static class SarcofagoGeneral
{
    public const string Nombre = "SepulcroGeneral";

    static Material Mat(Color c, float brillo)
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit");
        if (s == null) s = Shader.Find("Standard");
        Material m = new Material(s);
        m.color = c;
        if (brillo > 0f)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * brillo);
        }
        return m;
    }

    static void QuitarCollider(GameObject g)
    {
        Collider col = g.GetComponent<Collider>();
        if (col == null) return;
        if (Application.isPlaying) Object.Destroy(col); else Object.DestroyImmediate(col);
    }

    static GameObject Parte(Transform padre, string nombre, PrimitiveType tipo, Vector3 pos, Vector3 esc, Material m)
    {
        GameObject g = GameObject.CreatePrimitive(tipo);
        g.name = nombre;
        g.transform.SetParent(padre, false);
        g.transform.localPosition = pos;
        g.transform.localScale = esc;
        g.GetComponent<Renderer>().sharedMaterial = m;
        QuitarCollider(g);
        return g;
    }

    /// <summary>
    /// Construye el sarcófago. 'posLocal.y' es la altura de la base (la parte alta de la plataforma de la arena).
    /// El eje largo del sarcófago es Z (cabecera hacia +Z).
    /// </summary>
    public static GameObject Construir(Transform arena, Vector3 posLocal, Material cantera)
    {
        Material piedra = cantera != null ? cantera : Mat(new Color(0.76f, 0.54f, 0.48f), 0f);
        Material piedraOscura = Mat(new Color(0.30f, 0.27f, 0.30f), 0f);
        Material interior = Mat(new Color(0.02f, 0.03f, 0.03f), 0f);
        Material fatuo = Mat(new Color(0.15f, 0.95f, 0.45f), 2.6f);
        Material oro = Mat(new Color(0.85f, 0.70f, 0.20f), 0.5f);

        GameObject raiz = new GameObject(Nombre);
        raiz.transform.SetParent(arena, false);
        raiz.transform.localPosition = posLocal;
        raiz.transform.localRotation = Quaternion.identity;
        Transform r = raiz.transform;

        // Zócalo escalonado (dos peldaños)
        Parte(r, "Zocalo1", PrimitiveType.Cube, new Vector3(0f, 0.175f, 0f), new Vector3(5.4f, 0.35f, 7.8f), piedraOscura);
        Parte(r, "Zocalo2", PrimitiveType.Cube, new Vector3(0f, 0.525f, 0f), new Vector3(4.8f, 0.35f, 7.2f), piedra);

        // Cuerpo + moldura
        Parte(r, "Cuerpo", PrimitiveType.Cube, new Vector3(0f, 1.25f, 0f), new Vector3(4.0f, 1.1f, 6.4f), piedra);
        Parte(r, "Moldura", PrimitiveType.Cube, new Vector3(0f, 1.88f, 0f), new Vector3(4.3f, 0.16f, 6.7f), piedraOscura);

        // Interior abierto de la cabecera, con resplandor de fuego fatuo
        Parte(r, "InteriorAbierto", PrimitiveType.Cube, new Vector3(0f, 1.985f, 1.55f), new Vector3(3.2f, 0.03f, 2.9f), interior);
        Parte(r, "ResplandorFatuo", PrimitiveType.Cube, new Vector3(0f, 2.0f, 1.55f), new Vector3(1.9f, 0.02f, 1.5f), fatuo);

        // Tapa desplazada hacia el pie: deja el sepulcro entreabierto
        Parte(r, "Tapa", PrimitiveType.Cube, new Vector3(0f, 2.12f, -1.55f), new Vector3(4.1f, 0.3f, 3.6f), piedra);

        // Cruz de cantera en la cabecera
        Parte(r, "CruzV", PrimitiveType.Cube, new Vector3(0f, 1.85f, 3.45f), new Vector3(0.34f, 2.3f, 0.34f), piedra);
        Parte(r, "CruzH", PrimitiveType.Cube, new Vector3(0f, 2.45f, 3.45f), new Vector3(1.3f, 0.3f, 0.34f), piedra);

        // Cuatro veladoras en las esquinas del zócalo
        for (int i = 0; i < 4; i++)
        {
            float sx = (i % 2 == 0) ? -2.2f : 2.2f;
            float sz = (i < 2) ? -2.6f : 2.6f;
            Parte(r, "Veladora" + i, PrimitiveType.Cylinder, new Vector3(sx, 0.9f, sz), new Vector3(0.22f, 0.2f, 0.22f), oro);
            Parte(r, "Llama" + i, PrimitiveType.Sphere, new Vector3(sx, 1.18f, sz), new Vector3(0.16f, 0.22f, 0.16f), fatuo);
        }

        // Luz de fuego fatuo que sale de la tumba
        GameObject luzGo = new GameObject("LuzFatua");
        luzGo.transform.SetParent(r, false);
        luzGo.transform.localPosition = new Vector3(0f, 2.9f, 1.55f);
        Light l = luzGo.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(0.2f, 0.95f, 0.55f);
        l.range = 11f;
        l.intensity = 2.2f;
        l.shadows = LightShadows.None;

        // UN solo collider (macizo) para todo el sarcófago
        BoxCollider bc = raiz.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, 1.1f, 0f);
        bc.size = new Vector3(5.4f, 2.2f, 7.8f);

        return raiz;
    }
}
