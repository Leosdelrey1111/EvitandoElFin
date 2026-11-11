using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Ambientación de la Sierra de Guanajuato: formaciones de cantera (con la "corona" de la Bufa en el horizonte),
/// cruces de camino con veladoras, muros de piedra seca, una bocamina abandonada y ofrendas de cempasúchil
/// con velas en cada claro (puntos cálidos contra la luz fría de la luna).
/// </summary>
public static class SierraProps
{
    static Material mMadera, mVela, mLlama, mCempasuchil, mCempasuchil2, mHojaFlor, mNegro, mLatonFaro;
    static Material[] mRoca;

    public static void Crear(Terrain terreno)
    {
        Preparar();
        GameObject raiz = new GameObject("Ambientacion_Sierra");

        Transform formaciones = Hijo(raiz.transform, "Formaciones");
        Transform camino = Hijo(raiz.transform, "Camino");
        Transform altares = Hijo(raiz.transform, "Ofrendas");

        CrearCoronas(terreno, formaciones);
        CrearPenas(terreno, formaciones);
        CrearCruces(terreno, camino);
        CrearMuros(terreno, camino);
        CrearBocamina(terreno, camino);
        CrearOfrendas(terreno, altares);
    }

    static void Preparar()
    {
        mMadera = SierraFlora.Material_("Madera", new Color(0.26f, 0.17f, 0.10f), 0.1f, false);
        mVela = SierraFlora.Material_("Vela", new Color(0.92f, 0.88f, 0.75f), 0.2f, false);
        mLlama = SierraFlora.MaterialEmisivo_("Llama", new Color(1f, 0.7f, 0.2f), new Color(3.2f, 1.7f, 0.4f));
        mCempasuchil = SierraFlora.MaterialEmisivo_("Cempasuchil", new Color(1f, 0.55f, 0.05f), new Color(0.9f, 0.38f, 0.03f));
        mCempasuchil2 = SierraFlora.MaterialEmisivo_("Cempasuchil2", new Color(1f, 0.75f, 0.1f), new Color(0.9f, 0.55f, 0.04f));
        mHojaFlor = SierraFlora.Material_("HojaFlor", new Color(0.18f, 0.35f, 0.15f), 0.1f, false);
        mNegro = SierraFlora.Material_("VacioMina", new Color(0.01f, 0.01f, 0.012f), 0f, false);
        mLatonFaro = SierraFlora.MaterialEmisivo_("Farol", new Color(1f, 0.8f, 0.4f), new Color(3.5f, 2.2f, 0.7f));
        mRoca = new[]
        {
            SierraFlora.Material_("Cantera0", SierraFlora.Cantera[0], 0.12f, false),
            SierraFlora.Material_("Cantera1", SierraFlora.Cantera[1], 0.12f, false),
            SierraFlora.Material_("Cantera2", SierraFlora.Cantera[2], 0.12f, false),
        };
    }

    // ---------------------------------------------------------------- utilidades

    static Transform Hijo(Transform padre, string nombre)
    {
        GameObject g = new GameObject(nombre);
        g.transform.SetParent(padre, false);
        return g.transform;
    }

    static Vector3 Mundo(Terrain t, Vector2 local)
    {
        Vector3 p = BosqueTerrenoBuilder.LocalAMundo(local, 0f);
        p.y = t.SampleHeight(p) + t.transform.position.y;
        return p;
    }

    static GameObject Bloque(Transform padre, string nombre, PrimitiveType tipo, Vector3 pos, Vector3 escala, Material mat, bool collider)
    {
        GameObject g = GameObject.CreatePrimitive(tipo);
        g.name = nombre;
        g.transform.SetParent(padre, false);
        g.transform.localPosition = pos;
        g.transform.localScale = escala;
        g.GetComponent<Renderer>().sharedMaterial = mat;
        if (!collider) { Collider c = g.GetComponent<Collider>(); if (c != null) Object.DestroyImmediate(c); }
        return g;
    }

    /// <summary>Conjunto de bloques de cantera facetados (peña / cerro rocoso). escala = anchura típica en metros.</summary>
    static GameObject Formacion(Transform padre, string nombre, Vector3 pos, float escala, int bloques, int semilla, float altoMin, float altoMax)
    {
        GameObject raiz = new GameObject(nombre);
        raiz.transform.SetParent(padre, false);
        raiz.transform.position = pos;
        System.Random rng = new System.Random(semilla);

        for (int i = 0; i < bloques; i++)
        {
            float ancho = escala * (0.7f + (float)rng.NextDouble() * 0.7f);
            float alto = escala * (altoMin + (float)rng.NextDouble() * (altoMax - altoMin));
            if (i == 0) alto *= 1.15f;
            float fondo = escala * (0.6f + (float)rng.NextDouble() * 0.6f);
            float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
            float rad = i == 0 ? 0f : escala * (0.45f + (float)rng.NextDouble() * 0.7f);

            Mesh m = SierraFlora.MallaRoca(semilla * 7 + i * 13, new Vector3(ancho, alto, fondo), 0.30f, 0.9f);
            GameObject b = new GameObject("Bloque" + i);
            b.transform.SetParent(raiz.transform, false);
            b.transform.localPosition = new Vector3(Mathf.Cos(ang) * rad, -alto * 0.12f, Mathf.Sin(ang) * rad);
            b.transform.localRotation = Quaternion.Euler(((float)rng.NextDouble() - 0.5f) * 10f, (float)rng.NextDouble() * 360f, ((float)rng.NextDouble() - 0.5f) * 10f);
            b.AddComponent<MeshFilter>().sharedMesh = m;
            b.AddComponent<MeshRenderer>().sharedMaterial = mRoca[(semilla + i) % 3];
            b.AddComponent<MeshCollider>().sharedMesh = m;
            b.isStatic = true;
        }
        return raiz;
    }

    // ---------------------------------------------------------------- formaciones

    static void CrearCoronas(Terrain t, Transform padre)
    {
        // "Corona de la Bufa": cerro rocoso en el horizonte, justo detrás del portal, y otros más pequeños en los bordes
        // La corona de la Bufa queda detrás de la arena (480,500); el "mirador" queda al norte del tramo donde Tonalli habla de los cerros.
        Vector2[] centros = { new Vector2(520, 540), new Vector2(45, 600), new Vector2(600, 45), new Vector2(320, 626), new Vector2(30, 320),
                              new Vector2(285, 70), new Vector2(110, 40), new Vector2(585, 330), new Vector2(60, 470), new Vector2(430, 600) };
        float[] escalas = { 14f, 9f, 9f, 8.5f, 8f, 11f, 8f, 8.5f, 8f, 8f };
        for (int i = 0; i < centros.Length; i++)
        {
            string nombre = i == 0 ? "Corona_La_Bufa" : (i == 5 ? "Cerro_Mirador" : "Cerro_Rocoso_" + i);
            Formacion(padre, nombre, Mundo(t, centros[i]), escalas[i], i == 0 ? 10 : (i == 5 ? 9 : 7), 31 + i * 17, 1.5f, 3.3f);
        }
    }

    static void CrearPenas(Terrain t, Transform padre)
    {
        List<Vector2> s = BosqueTerrenoBuilder.MuestrasSendero();
        int n = 0;
        for (int i = 18; i < s.Count - 6; i += 24)
        {
            Vector2 d = (s[Mathf.Min(i + 1, s.Count - 1)] - s[i]).normalized;
            Vector2 perp = new Vector2(-d.y, d.x);
            float lado = (n % 2 == 0 ? 1f : -1f) * (13f + (n * 7 % 11));
            Vector2 p = s[i] + perp * lado;
            Formacion(padre, "Pena_" + n, Mundo(t, p), 2.8f + (n % 3) * 0.6f, 3 + n % 2, 101 + n * 9, 1.0f, 2.4f);
            n++;
        }
    }

    // ---------------------------------------------------------------- camino

    static void CrearCruces(Terrain t, Transform padre)
    {
        List<Vector2> s = BosqueTerrenoBuilder.MuestrasSendero();
        float[] f = { 0.06f, 0.17f, 0.29f, 0.40f, 0.52f, 0.64f, 0.76f, 0.88f };
        for (int k = 0; k < f.Length; k++)
        {
            int i = Mathf.Clamp(Mathf.RoundToInt(s.Count * f[k]), 2, s.Count - 3);
            Vector2 d = (s[i + 1] - s[i]).normalized;
            Vector2 perp = new Vector2(-d.y, d.x);
            Vector3 pos = Mundo(t, s[i] + perp * (k % 2 == 0 ? 3.4f : -3.4f));

            GameObject cruz = new GameObject("Cruz_Camino_" + k);
            cruz.transform.SetParent(padre, false);
            cruz.transform.position = pos;
            cruz.transform.rotation = Quaternion.LookRotation(new Vector3(-d.x, 0f, -d.y)) * Quaternion.Euler(0f, (k - 2) * 8f, 0f);

            Mesh pila = SierraFlora.MallaRoca(800 + k, new Vector3(1.3f, 0.5f, 1.1f), 0.35f, 0f);
            GameObject b = new GameObject("PilaPiedras");
            b.transform.SetParent(cruz.transform, false);
            b.AddComponent<MeshFilter>().sharedMesh = pila;
            b.AddComponent<MeshRenderer>().sharedMaterial = mRoca[k % 3];
            b.AddComponent<MeshCollider>().sharedMesh = pila;

            Bloque(cruz.transform, "Poste", PrimitiveType.Cube, new Vector3(0f, 1.05f, 0f), new Vector3(0.13f, 1.7f, 0.13f), mMadera, false);
            Bloque(cruz.transform, "Brazo", PrimitiveType.Cube, new Vector3(0f, 1.45f, 0f), new Vector3(0.85f, 0.12f, 0.12f), mMadera, false);
            Bloque(cruz.transform, "Veladora", PrimitiveType.Cylinder, new Vector3(0.45f, 0.35f, 0.5f), new Vector3(0.12f, 0.12f, 0.12f), mVela, false);
            Bloque(cruz.transform, "Llama", PrimitiveType.Sphere, new Vector3(0.45f, 0.5f, 0.5f), new Vector3(0.07f, 0.11f, 0.07f), mLlama, false);
            Bloque(cruz.transform, "Veladora2", PrimitiveType.Cylinder, new Vector3(-0.4f, 0.32f, 0.45f), new Vector3(0.1f, 0.1f, 0.1f), mVela, false);
            Bloque(cruz.transform, "Llama2", PrimitiveType.Sphere, new Vector3(-0.4f, 0.44f, 0.45f), new Vector3(0.06f, 0.1f, 0.06f), mLlama, false);

            if (k % 2 == 0)
            {
                GameObject lz = new GameObject("LuzVeladora");
                lz.transform.SetParent(cruz.transform, false);
                lz.transform.localPosition = new Vector3(0.1f, 0.8f, 0.5f);
                Light l = lz.AddComponent<Light>();
                l.type = LightType.Point; l.color = new Color(1f, 0.62f, 0.25f); l.range = 6f; l.intensity = 1.1f; l.shadows = LightShadows.None;
            }
        }
    }

    static void CrearMuros(Terrain t, Transform padre)
    {
        Muro(t, padre, "Muro_Inicio", new Vector2(90, 120), new Vector2(104, 140), 1);
        Muro(t, padre, "Muro_Totem3", new Vector2(257, 471), new Vector2(273, 487), 2);
        Muro(t, padre, "Muro_Totem1", new Vector2(510, 190), new Vector2(526, 208), 3);
    }

    static void Muro(Terrain t, Transform padre, string nombre, Vector2 a, Vector2 b, int semilla)
    {
        GameObject raiz = new GameObject(nombre);
        raiz.transform.SetParent(padre, false);
        System.Random rng = new System.Random(semilla * 97);
        float largo = Vector2.Distance(a, b);
        int piezas = Mathf.CeilToInt(largo / 0.85f);
        Vector2 dir = (b - a).normalized;
        for (int fila = 0; fila < 3; fila++)
        {
            for (int i = 0; i < piezas - fila; i++)
            {
                // algunos huecos: muro derruido
                if (fila > 0 && rng.NextDouble() < 0.25 + 0.2 * fila) continue;
                Vector2 p = a + dir * (i * 0.85f + fila * 0.4f) + new Vector2(-dir.y, dir.x) * (((float)rng.NextDouble() - 0.5f) * 0.12f);
                Vector3 pos = Mundo(t, p);
                GameObject c = Bloque(raiz.transform, "Piedra", PrimitiveType.Cube, pos + Vector3.up * (0.2f + fila * 0.38f),
                    new Vector3(0.8f + (float)rng.NextDouble() * 0.2f, 0.36f, 0.55f + (float)rng.NextDouble() * 0.15f), mRoca[(i + fila) % 3], true);
                c.transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.y)) * Quaternion.Euler(((float)rng.NextDouble() - 0.5f) * 6f, ((float)rng.NextDouble() - 0.5f) * 10f, ((float)rng.NextDouble() - 0.5f) * 6f);
                c.isStatic = true;
            }
        }
    }

    static void CrearBocamina(Terrain t, Transform padre)
    {
        // A ~30 m del sendero (lado sur), a la altura del hito "tesoro": la entrada mira al camino
        Vector2 dirCamino;
        Vector2 enCamino = BosqueTerrenoBuilder.PuntoEnSendero(BosqueTerrenoBuilder.MetrosEnSendero(BosqueTerrenoBuilder.Sendero[12]), out dirCamino);
        Vector2 perpCamino = new Vector2(-dirCamino.y, dirCamino.x);
        if (perpCamino.y < 0f) perpCamino = -perpCamino;
        Vector2 sitio = enCamino + perpCamino * 30f;
        Vector2 haciaClaro = (enCamino - sitio).normalized;
        Vector3 pos = Mundo(t, sitio);

        GameObject raiz = new GameObject("Bocamina_Abandonada");
        raiz.transform.SetParent(padre, false);
        raiz.transform.position = pos;
        raiz.transform.rotation = Quaternion.LookRotation(new Vector3(haciaClaro.x, 0f, haciaClaro.y));

        // Masa de roca detrás de la entrada
        GameObject roca = Formacion(raiz.transform, "MasaRoca", pos + raiz.transform.forward * -4.5f, 7.5f, 4, 555, 1.1f, 1.9f);

        Bloque(raiz.transform, "Vacio", PrimitiveType.Cube, new Vector3(0f, 1.5f, -0.3f), new Vector3(2.4f, 3.0f, 0.3f), mNegro, false);
        Bloque(raiz.transform, "PosteI", PrimitiveType.Cube, new Vector3(-1.45f, 1.6f, 0.1f), new Vector3(0.38f, 3.3f, 0.38f), mMadera, true);
        Bloque(raiz.transform, "PosteD", PrimitiveType.Cube, new Vector3(1.45f, 1.6f, 0.1f), new Vector3(0.38f, 3.3f, 0.38f), mMadera, true);
        Bloque(raiz.transform, "Dintel", PrimitiveType.Cube, new Vector3(0f, 3.35f, 0.1f), new Vector3(3.6f, 0.42f, 0.5f), mMadera, true);
        // Rieles y carro minero
        for (int i = 0; i < 2; i++) Bloque(raiz.transform, "Riel" + i, PrimitiveType.Cube, new Vector3(-0.55f + i * 1.1f, 0.08f, 3.2f), new Vector3(0.08f, 0.08f, 7.2f), mNegro, false);
        for (int i = 0; i < 6; i++) Bloque(raiz.transform, "Durmiente" + i, PrimitiveType.Cube, new Vector3(0f, 0.04f, 0.2f + i * 1.2f), new Vector3(1.5f, 0.06f, 0.16f), mMadera, false);
        GameObject carro = Bloque(raiz.transform, "CarroMinero", PrimitiveType.Cube, new Vector3(0f, 0.6f, 5.6f), new Vector3(1.2f, 0.8f, 1.6f), mMadera, true);
        carro.transform.localRotation = Quaternion.Euler(0f, 8f, 0f);

        // Farol colgado
        GameObject farol = Bloque(raiz.transform, "Farol", PrimitiveType.Sphere, new Vector3(1.9f, 2.7f, 0.5f), new Vector3(0.22f, 0.3f, 0.22f), mLatonFaro, false);
        GameObject lz = new GameObject("LuzFarol");
        lz.transform.SetParent(farol.transform, false);
        Light l = lz.AddComponent<Light>();
        l.type = LightType.Point; l.color = new Color(1f, 0.68f, 0.3f); l.range = 12f; l.intensity = 2.2f; l.shadows = LightShadows.None;
    }

    // ---------------------------------------------------------------- ofrendas en cada claro

    static void CrearOfrendas(Terrain t, Transform padre)
    {
        for (int i = 0; i < 4; i++)   // Inicio y los tres claros de tótems
        {
            BosqueTerrenoBuilder.Plaza pl = BosqueTerrenoBuilder.Plazas[i];
            Vector3 c = Mundo(t, pl.centro);
            System.Random rng = new System.Random(900 + i);

            GameObject ofrenda = new GameObject("Ofrenda_" + pl.nombre);
            ofrenda.transform.SetParent(padre, false);
            ofrenda.transform.position = c;

            // Veladoras en círculo
            for (int k = 0; k < 8; k++)
            {
                float a = k * Mathf.PI * 2f / 8f + 0.2f;
                float r = 3.3f + (float)rng.NextDouble() * 0.4f;
                Vector3 p = Mundo(t, pl.centro + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
                float alto = 0.14f + (float)rng.NextDouble() * 0.12f;
                Bloque(ofrenda.transform, "Vela", PrimitiveType.Cylinder, p + Vector3.up * (alto * 0.5f), new Vector3(0.09f, alto * 0.5f, 0.09f), mVela, false);
                Bloque(ofrenda.transform, "Llama", PrimitiveType.Sphere, p + Vector3.up * (alto + 0.05f), new Vector3(0.06f, 0.1f, 0.06f), mLlama, false);
            }

            // Cempasúchil
            for (int k = 0; k < 36; k++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float r = 2.3f + (float)rng.NextDouble() * 2.4f;
                Vector3 p = Mundo(t, pl.centro + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
                float s = 0.16f + (float)rng.NextDouble() * 0.12f;
                Bloque(ofrenda.transform, "Flor", PrimitiveType.Sphere, p + Vector3.up * 0.14f, new Vector3(s, s * 0.6f, s), k % 3 == 0 ? mCempasuchil2 : mCempasuchil, false);
                Bloque(ofrenda.transform, "Hojas", PrimitiveType.Sphere, p + Vector3.up * 0.05f, new Vector3(s * 1.1f, s * 0.35f, s * 1.1f), mHojaFlor, false);
            }

            // Un solo punto de luz cálida por ofrenda (contrasta con la luna)
            GameObject lz = new GameObject("LuzOfrenda");
            lz.transform.SetParent(ofrenda.transform, false);
            lz.transform.localPosition = new Vector3(0f, 1.3f, 0f);
            Light l = lz.AddComponent<Light>();
            l.type = LightType.Point; l.color = new Color(1f, 0.6f, 0.25f); l.range = 13f; l.intensity = 1.5f; l.shadows = LightShadows.None;
        }
    }
}
