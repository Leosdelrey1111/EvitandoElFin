using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Kit de interfaz con identidad mexicana (noche de Día de Muertos / Guanajuato): paleta, sprites generados por código
/// (rectángulos redondeados, íconos, calavera, cempasúchil, papel picado) y widgets listos (botones, sliders, teclas).
/// Todo se genera en runtime: no depende de imágenes externas.
/// </summary>
public static class UIMexicana
{
    // ================================================================== PALETA
    public static Color H(string hex)
    {
        Color c; ColorUtility.TryParseHtmlString("#" + hex, out c); return c;
    }
    public static readonly Color NocheProfunda = H("080B1E");
    public static readonly Color Noche = H("111736");
    public static readonly Color NocheClara = H("1C2750");
    public static readonly Color Rosa = H("E4007C");
    public static readonly Color Cempasuchil = H("FF9F1C");
    public static readonly Color Dorado = H("F4C95D");
    public static readonly Color Turquesa = H("2EC4B6");
    public static readonly Color Crema = H("F7EBD0");
    public static readonly Color Rojo = H("E63946");
    public static readonly Color Verde = H("4CC38A");
    public static readonly Color Violeta = H("7B4FD6");
    public static readonly Color Azul = H("3B6FE0");

    public static readonly Color[] ColoresPapelPicado = { H("E4007C"), H("FF9F1C"), H("2EC4B6"), H("F4C95D"), H("7B4FD6"), H("E63946"), H("4CC38A") };

    public static Color ConAlfa(Color c, float a) { c.a = a; return c; }

    // ================================================================== WIDGETS BÁSICOS

    public static RectTransform Nuevo(string nombre, Transform padre)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        return (RectTransform)go.transform;
    }

    public static void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    public static void Anclar(RectTransform rt, Vector2 ancla, Vector2 pos, Vector2 tam)
    {
        rt.anchorMin = rt.anchorMax = ancla;
        rt.pivot = ancla;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
    }

    public static void AnclarCentro(RectTransform rt, Vector2 ancla, Vector2 pos, Vector2 tam)
    {
        rt.anchorMin = rt.anchorMax = ancla;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
    }

    public static Image Imagen(string nombre, Transform padre, Sprite sprite, Color color, bool sliced = false, bool raycast = false)
    {
        RectTransform rt = Nuevo(nombre, padre);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = raycast;
        if (sprite != null && sliced) img.type = Image.Type.Sliced;
        return img;
    }

    public static TextMeshProUGUI Texto(string nombre, Transform padre, string texto, float tam, Color color,
        TextAlignmentOptions alin = TextAlignmentOptions.Center, FontStyles estilo = FontStyles.Normal, float espaciado = 0f)
    {
        RectTransform rt = Nuevo(nombre, padre);
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = texto;
        t.fontSize = tam;
        t.color = color;
        t.alignment = alin;
        t.fontStyle = estilo;
        t.characterSpacing = espaciado;
        t.raycastTarget = false;
        t.richText = true;
        return t;
    }

    public static Canvas CrearCanvas(string nombre, int orden)
    {
        GameObject go = new GameObject(nombre);
        Canvas c = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = orden;
        CanvasScaler cs = go.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        cs.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return c;
    }

    public static void AsegurarEventSystem()
    {
        if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null) return;
        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    // ================================================================== SPRITES

    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    static Sprite Crear(string clave, int w, int h, Func<float, float, Color> f, Vector4 borde = default(Vector4), FilterMode filtro = FilterMode.Bilinear)
    {
        Sprite s;
        if (cache.TryGetValue(clave, out s) && s != null) return s;

        Color32[] px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[y * w + x] = f((x + 0.5f) / w, (y + 0.5f) / h);

        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = filtro;
        tex.SetPixels32(px);
        tex.Apply();
        s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, borde);
        s.name = clave;
        cache[clave] = s;
        return s;
    }

    static float Suave(float sd, float aa) { return Mathf.Clamp01(0.5f - sd / aa); }

    // ---- SDF
    static float Circ(Vector2 p, Vector2 c, float r) { return (p - c).magnitude - r; }
    static float Seg(Vector2 p, Vector2 a, Vector2 b, float r)
    {
        Vector2 pa = p - a, ba = b - a;
        float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Mathf.Max(0.0001f, Vector2.Dot(ba, ba)));
        return (pa - ba * h).magnitude - r;
    }
    static float Caja(Vector2 p, Vector2 c, Vector2 half, float rad)
    {
        Vector2 d = new Vector2(Mathf.Abs(p.x - c.x), Mathf.Abs(p.y - c.y)) - half + Vector2.one * rad;
        return Vector2.Max(d, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0f) - rad;
    }
    static float Anillo(Vector2 p, Vector2 c, float r, float th) { return Mathf.Abs((p - c).magnitude - r) - th; }
    static float Arco(Vector2 p, Vector2 c, float r, float th, float a0, float a1)
    {
        Vector2 d = p - c;
        float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        float mid = (a0 + a1) * 0.5f, half = Mathf.Abs(a1 - a0) * 0.5f;
        float da = Mathf.Abs(Mathf.DeltaAngle(ang, mid)) - half;
        float sd = Mathf.Max(Anillo(p, c, r, th), da * Mathf.Deg2Rad * r);
        Vector2 e0 = c + new Vector2(Mathf.Cos(a0 * Mathf.Deg2Rad), Mathf.Sin(a0 * Mathf.Deg2Rad)) * r;
        Vector2 e1 = c + new Vector2(Mathf.Cos(a1 * Mathf.Deg2Rad), Mathf.Sin(a1 * Mathf.Deg2Rad)) * r;
        return Mathf.Min(sd, Mathf.Min(Circ(p, e0, th), Circ(p, e1, th)));
    }
    static float Corte(float a, float b) { return Mathf.Max(a, -b); }

    // ---- Formas básicas

    /// <summary>Rectángulo redondeado 9-slice (usar con Image.type = Sliced).</summary>
    public static Sprite Redondeado(int radio = 16)
    {
        int s = radio * 2 + 4;
        return Crear("rr" + radio, s, s, (u, v) =>
        {
            Vector2 p = new Vector2(u * s, v * s) - Vector2.one * (s * 0.5f);
            float sd = Caja(p, Vector2.zero, Vector2.one * (s * 0.5f - 0.5f), radio);
            return new Color(1f, 1f, 1f, Suave(sd, 1.2f));
        }, new Vector4(radio + 1, radio + 1, radio + 1, radio + 1));
    }

    /// <summary>Solo el contorno de un rectángulo redondeado (9-slice).</summary>
    public static Sprite RedondeadoBorde(int radio = 16, float grosor = 2f)
    {
        int s = radio * 2 + 4;
        return Crear("rb" + radio + "_" + grosor, s, s, (u, v) =>
        {
            Vector2 p = new Vector2(u * s, v * s) - Vector2.one * (s * 0.5f);
            float sd = Caja(p, Vector2.zero, Vector2.one * (s * 0.5f - 0.5f), radio);
            float anillo = Mathf.Max(sd, -(sd + grosor));
            return new Color(1f, 1f, 1f, Suave(anillo, 1.2f));
        }, new Vector4(radio + 1, radio + 1, radio + 1, radio + 1));
    }

    public static Sprite Circulo()
    {
        return Crear("circulo", 128, 128, (u, v) =>
        {
            Vector2 p = new Vector2(u - 0.5f, v - 0.5f) * 2f;
            return new Color(1f, 1f, 1f, Suave(p.magnitude - 0.97f, 0.03f));
        });
    }

    public static Sprite Brillo()
    {
        return Crear("brillo", 128, 128, (u, v) =>
        {
            Vector2 p = new Vector2(u - 0.5f, v - 0.5f) * 2f;
            float a = Mathf.Pow(Mathf.Clamp01(1f - p.magnitude), 2.2f);
            return new Color(1f, 1f, 1f, a);
        });
    }

    /// <summary>Viñeta: transparente al centro, oscura en los bordes (para daño y ambientación).</summary>
    public static Sprite Vineta()
    {
        return Crear("vineta", 128, 128, (u, v) =>
        {
            Vector2 p = new Vector2(u - 0.5f, v - 0.5f) * 2f;
            float a = Mathf.SmoothStep(0.35f, 1.25f, p.magnitude);
            return new Color(1f, 1f, 1f, a);
        });
    }

    public static Sprite Blanco()
    {
        return Crear("blanco", 8, 8, (u, v) => Color.white, default(Vector4), FilterMode.Point);
    }

    /// <summary>Degradado vertical (arriba -> abajo).</summary>
    public static Sprite DegradadoVertical(string clave, Color arriba, Color medio, Color abajo)
    {
        return Crear("dv_" + clave, 4, 256, (u, v) =>
        {
            float t = 1f - v;
            Color c = t < 0.55f ? Color.Lerp(arriba, medio, t / 0.55f) : Color.Lerp(medio, abajo, (t - 0.55f) / 0.45f);
            return c;
        });
    }

    public static Sprite DegradadoHorizontal(string clave, Color izq, Color der)
    {
        return Crear("dh_" + clave, 256, 4, (u, v) => Color.Lerp(izq, der, u));
    }

    public static Sprite Petalo()
    {
        return Crear("petalo", 32, 48, (u, v) =>
        {
            float x = (u - 0.5f) * 2f, y = (v - 0.5f) * 2f;
            float ancho = 0.75f * (1f - y * y) * (y < 0f ? 1f : 0.85f);
            float d = Mathf.Abs(x) - ancho;
            return new Color(1f, 1f, 1f, Suave(d, 0.12f) * Mathf.Clamp01((1f - Mathf.Abs(y)) * 8f));
        });
    }

    // ---- Íconos (128x128, blancos; se tiñen con Image.color)

    public enum Icono { Golpe, Onda, Salto, Esquive, Totem, Alma, Calavera, Flor, Flecha, Corazon, Serpiente }

    public static Sprite IconoSprite(Icono tipo)
    {
        return Crear("ico_" + tipo, 128, 128, (u, v) =>
        {
            Vector2 p = new Vector2(u - 0.5f, v - 0.5f) * 2f;
            float sd = SdIcono(tipo, p);
            return new Color(1f, 1f, 1f, Suave(sd, 0.03f));
        });
    }

    static float SdIcono(Icono tipo, Vector2 p)
    {
        switch (tipo)
        {
            case Icono.Golpe:
            {
                // tres garras diagonales
                float d = float.MaxValue;
                for (int i = 0; i < 3; i++)
                {
                    float ox = -0.52f + i * 0.48f;
                    float largo = i == 1 ? 0.95f : 0.78f;
                    d = Mathf.Min(d, Seg(p, new Vector2(ox - 0.18f, 0.68f - (1f - largo) * 0.3f), new Vector2(ox + 0.32f, -0.7f + (1f - largo) * 0.3f), 0.085f));
                }
                d = Mathf.Min(d, Circ(p, new Vector2(0.62f, 0.62f), 0.07f));
                d = Mathf.Min(d, Circ(p, new Vector2(-0.62f, -0.62f), 0.06f));
                return d;
            }
            case Icono.Onda:
            {
                float d = Circ(p, Vector2.zero, 0.17f);
                d = Mathf.Min(d, Mathf.Min(Arco(p, Vector2.zero, 0.46f, 0.07f, -55f, 55f), Arco(p, Vector2.zero, 0.46f, 0.07f, 125f, 235f)));
                d = Mathf.Min(d, Mathf.Min(Arco(p, Vector2.zero, 0.78f, 0.07f, -45f, 45f), Arco(p, Vector2.zero, 0.78f, 0.07f, 135f, 225f)));
                d = Mathf.Min(d, Mathf.Min(Arco(p, Vector2.zero, 0.46f, 0.07f, 70f, 110f), Arco(p, Vector2.zero, 0.46f, 0.07f, 250f, 290f)));
                return d;
            }
            case Icono.Salto:
            {
                float d = Seg(p, new Vector2(-0.6f, 0.02f), new Vector2(0f, 0.6f), 0.1f);
                d = Mathf.Min(d, Seg(p, new Vector2(0.6f, 0.02f), new Vector2(0f, 0.6f), 0.1f));
                d = Mathf.Min(d, Seg(p, new Vector2(-0.6f, -0.42f), new Vector2(0f, 0.16f), 0.1f));
                d = Mathf.Min(d, Seg(p, new Vector2(0.6f, -0.42f), new Vector2(0f, 0.16f), 0.1f));
                d = Mathf.Min(d, Seg(p, new Vector2(-0.55f, -0.82f), new Vector2(0.55f, -0.82f), 0.055f));
                return d;
            }
            case Icono.Esquive:
            {
                float d = Arco(p, Vector2.zero, 0.58f, 0.09f, 20f, 300f);
                Vector2 e = new Vector2(Mathf.Cos(20f * Mathf.Deg2Rad), Mathf.Sin(20f * Mathf.Deg2Rad)) * 0.58f;
                d = Mathf.Min(d, Seg(p, e, e + new Vector2(0.28f, 0.16f), 0.09f));
                d = Mathf.Min(d, Seg(p, e, e + new Vector2(-0.08f, 0.3f), 0.09f));
                d = Mathf.Min(d, Circ(p, Vector2.zero, 0.11f));
                return d;
            }
            case Icono.Totem:
            {
                float cuerpo = Caja(p, new Vector2(0f, -0.1f), new Vector2(0.34f, 0.72f), 0.1f);
                float cabeza = Circ(p, new Vector2(0f, 0.78f), 0.24f);
                float d = Mathf.Min(cuerpo, cabeza);
                d = Corte(d, Seg(p, new Vector2(-0.5f, 0.3f), new Vector2(0.5f, 0.3f), 0.045f));
                d = Corte(d, Seg(p, new Vector2(-0.5f, -0.12f), new Vector2(0.5f, -0.12f), 0.045f));
                d = Corte(d, Seg(p, new Vector2(-0.5f, -0.54f), new Vector2(0.5f, -0.54f), 0.045f));
                d = Corte(d, Circ(p, new Vector2(0f, 0.8f), 0.07f));
                return d;
            }
            case Icono.Alma:
            {
                float d = Circ(p, new Vector2(0.18f, 0.2f), 0.4f);
                for (int i = 1; i <= 9; i++)
                {
                    float t = i / 9f;
                    Vector2 c = Vector2.Lerp(new Vector2(0.05f, 0.05f), new Vector2(-0.75f, -0.78f), t) + new Vector2(Mathf.Sin(t * 5f) * 0.1f, 0f);
                    d = Mathf.Min(d, Circ(p, c, Mathf.Lerp(0.32f, 0.04f, t)));
                }
                d = Corte(d, Circ(p, new Vector2(0.06f, 0.3f), 0.06f));
                d = Corte(d, Circ(p, new Vector2(0.3f, 0.3f), 0.06f));
                return d;
            }
            case Icono.Calavera:
            {
                float cabeza = Circ(p, new Vector2(0f, 0.2f), 0.72f);
                float mandibula = Caja(p, new Vector2(0f, -0.5f), new Vector2(0.4f, 0.26f), 0.14f);
                float d = Mathf.Min(cabeza, mandibula);
                Vector2 oi = new Vector2(-0.32f, 0.12f), od = new Vector2(0.32f, 0.12f);
                d = Corte(d, Mathf.Min(Circ(p, oi, 0.21f), Circ(p, od, 0.21f)));
                d = Mathf.Min(d, Mathf.Min(Circ(p, oi, 0.07f), Circ(p, od, 0.07f)));
                d = Corte(d, Mathf.Min(Anillo(p, oi, 0.3f, 0.03f), Anillo(p, od, 0.3f, 0.03f)));
                d = Corte(d, Mathf.Min(Circ(p, new Vector2(-0.07f, -0.2f), 0.075f), Circ(p, new Vector2(0.07f, -0.2f), 0.075f)));
                d = Corte(d, Seg(p, new Vector2(-0.34f, -0.55f), new Vector2(0.34f, -0.55f), 0.022f));
                for (int i = 0; i < 4; i++)
                {
                    float x = -0.2f + i * 0.133f;
                    d = Corte(d, Seg(p, new Vector2(x, -0.55f), new Vector2(x, -0.76f), 0.022f));
                }
                for (int i = 0; i < 5; i++)
                {
                    float a = i * 72f * Mathf.Deg2Rad;
                    d = Corte(d, Circ(p, new Vector2(0f, 0.6f) + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 0.13f, 0.045f));
                }
                d = Corte(d, Mathf.Min(Circ(p, new Vector2(-0.52f, -0.2f), 0.045f), Circ(p, new Vector2(0.52f, -0.2f), 0.045f)));
                return d;
            }
            case Icono.Flor:
            {
                float r = p.magnitude;
                float ang = Mathf.Atan2(p.y, p.x);
                float R = 0.92f * (0.8f + 0.2f * Mathf.Abs(Mathf.Cos(ang * 8f)));
                float d = r - R;
                d = Corte(d, Anillo(p, Vector2.zero, 0.5f, 0.025f));
                d = Corte(d, Anillo(p, Vector2.zero, 0.28f, 0.025f));
                d = Mathf.Min(d, Circ(p, Vector2.zero, 0.1f));
                return d;
            }
            case Icono.Serpiente:
            {
                // cuerpo ondulado que se afina hacia la cola, cabeza con ojo y lengua bífida
                float d = float.MaxValue;
                const int n = 28;
                Vector2 previo = Vector2.zero, cabeza = Vector2.zero;
                for (int i = 0; i <= n; i++)
                {
                    float t = i / (float)n;
                    Vector2 q = new Vector2(Mathf.Lerp(-0.78f, 0.5f, t), Mathf.Sin(t * Mathf.PI * 2.4f) * 0.36f - 0.12f);
                    if (i > 0) d = Mathf.Min(d, Seg(p, previo, q, Mathf.Lerp(0.05f, 0.16f, t)));
                    previo = q; cabeza = q;
                }
                d = Mathf.Min(d, Circ(p, cabeza + new Vector2(0.06f, 0.02f), 0.2f));
                d = Corte(d, Circ(p, cabeza + new Vector2(0.1f, 0.08f), 0.045f));
                Vector2 boca = cabeza + new Vector2(0.26f, 0f);
                d = Mathf.Min(d, Seg(p, boca, boca + new Vector2(0.2f, 0f), 0.022f));
                d = Mathf.Min(d, Seg(p, boca + new Vector2(0.2f, 0f), boca + new Vector2(0.3f, 0.07f), 0.02f));
                d = Mathf.Min(d, Seg(p, boca + new Vector2(0.2f, 0f), boca + new Vector2(0.3f, -0.07f), 0.02f));
                return d;
            }
            case Icono.Flecha:
            {
                float d = Seg(p, new Vector2(-0.7f, 0f), new Vector2(0.6f, 0f), 0.1f);
                d = Mathf.Min(d, Seg(p, new Vector2(0.6f, 0f), new Vector2(0.15f, 0.45f), 0.1f));
                d = Mathf.Min(d, Seg(p, new Vector2(0.6f, 0f), new Vector2(0.15f, -0.45f), 0.1f));
                return d;
            }
            default: // Corazón
            {
                Vector2 q = new Vector2(p.x, p.y + 0.1f);
                float a = Circ(q, new Vector2(-0.3f, 0.25f), 0.38f);
                float b = Circ(q, new Vector2(0.3f, 0.25f), 0.38f);
                float t = Seg(q, new Vector2(-0.62f, 0.05f), new Vector2(0f, -0.72f), 0.1f);
                float t2 = Seg(q, new Vector2(0.62f, 0.05f), new Vector2(0f, -0.72f), 0.1f);
                float tri = Mathf.Max(Mathf.Abs(q.x) - (0.68f + q.y * 0.85f), -q.y - 0.7f);
                return Mathf.Min(Mathf.Min(a, b), Mathf.Min(Mathf.Min(t, t2), tri));
            }
        }
    }

    // ---- Papel picado

    public static Sprite BanderaPapelPicado(int variante)
    {
        return Crear("papel" + variante, 160, 110, (u, v) =>
        {
            float tri = Mathf.Abs(Mathf.Repeat(u * 5f, 1f) * 2f - 1f);   // 0..1
            float borde = 0.08f + 0.16f * tri;
            float dentro = (v > borde && u > 0.02f && u < 0.98f && v < 0.985f) ? 1f : 0f;
            if (dentro < 0.5f) return new Color(1, 1, 1, 0);

            float corte = 0f;
            Vector2 p = new Vector2(u, v);
            if (variante == 0)
            {
                if (Mathf.Abs(u - 0.5f) + Mathf.Abs(v - 0.55f) < 0.17f) corte = 1f;
                if (Mathf.Abs(u - 0.5f) + Mathf.Abs(v - 0.55f) < 0.07f) corte = 0f;
                for (int i = 0; i < 5; i++) if ((p - new Vector2(0.2f + i * 0.15f, 0.84f)).magnitude < 0.045f) corte = 1f;
            }
            else if (variante == 1)
            {
                float r = (new Vector2(u - 0.5f, (v - 0.55f) * 1.35f)).magnitude;
                if (r < 0.2f && r > 0.14f) corte = 1f;
                if (r < 0.07f) corte = 1f;
                for (int i = 0; i < 2; i++) if (Mathf.Abs(u - (0.17f + i * 0.66f)) + Mathf.Abs(v - 0.55f) < 0.1f) corte = 1f;
            }
            else
            {
                for (int i = 0; i < 5; i++) if (Mathf.Abs(u - (0.2f + i * 0.15f)) < 0.028f && v > 0.35f && v < 0.88f) corte = 1f;
                if (Mathf.Abs(v - 0.2f) < 0.025f && u > 0.1f && u < 0.9f) corte = 1f;
            }
            return new Color(1f, 1f, 1f, corte > 0.5f ? 0f : 1f);
        });
    }

    /// <summary>Hilo con ligera caída para colgar el papel picado.</summary>
    public static Sprite Hilo()
    {
        return Crear("hilo", 64, 4, (u, v) => new Color(1f, 1f, 1f, 1f));
    }

    // ================================================================== SONIDO DE INTERFAZ

    static AudioSource fuenteUI;
    static AudioClip clipTick, clipClick, clipAtras;

    public static void Sonar(string tipo)
    {
        if (fuenteUI == null)
        {
            GameObject go = new GameObject("SonidoUI");
            UnityEngine.Object.DontDestroyOnLoad(go);
            fuenteUI = go.AddComponent<AudioSource>();
            fuenteUI.spatialBlend = 0f;
            fuenteUI.playOnAwake = false;
        }
        AudioClip c;
        if (tipo == "click") { if (clipClick == null) clipClick = Pluck(new[] { 523f, 784f }, 0.26f, 0.12f); c = clipClick; }
        else if (tipo == "atras") { if (clipAtras == null) clipAtras = Pluck(new[] { 523f, 392f }, 0.24f, 0.12f); c = clipAtras; }
        else { if (clipTick == null) clipTick = Pluck(new[] { 988f }, 0.10f, 0f); c = clipTick; }
        fuenteUI.PlayOneShot(c, 0.55f * OpcionesJuego.Efectos);
    }

    /// <summary>Notas tipo marimba (parcial armónico + caída exponencial).</summary>
    public static AudioClip Pluck(float[] notas, float duracion, float separacion)
    {
        int rate = 22050;
        int n = (int)(rate * (duracion + separacion * (notas.Length - 1)));
        float[] d = new float[n];
        for (int k = 0; k < notas.Length; k++)
        {
            int ini = (int)(rate * separacion * k);
            for (int i = 0; ini + i < n; i++)
            {
                float t = i / (float)rate;
                float env = Mathf.Exp(-t * 16f) * (1f - Mathf.Exp(-t * 400f));
                float f = notas[k];
                float s = Mathf.Sin(2f * Mathf.PI * f * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * f * 4f * t) * Mathf.Exp(-t * 30f);
                d[ini + i] += s * env * 0.5f;
            }
        }
        AudioClip c = AudioClip.Create("UI", n, 1, rate, false);
        c.SetData(d, 0);
        return c;
    }

    // ================================================================== WIDGETS COMPUESTOS

    public static Button Boton(string nombre, Transform padre, string texto, Vector2 tam, Color acento, Action onClick, float fuente = 30f)
    {
        RectTransform raiz = Nuevo(nombre, padre);
        raiz.sizeDelta = tam;

        Image fondo = raiz.gameObject.AddComponent<Image>();
        fondo.sprite = Redondeado(14);
        fondo.type = Image.Type.Sliced;
        fondo.color = new Color(0.07f, 0.09f, 0.2f, 0.62f);
        fondo.raycastTarget = true;

        Button b = raiz.gameObject.AddComponent<Button>();
        b.targetGraphic = fondo;
        b.transition = Selectable.Transition.None;

        Image borde = Imagen("Borde", raiz, RedondeadoBorde(14, 1.6f), new Color(1f, 1f, 1f, 0.12f), true);
        Estirar(borde.rectTransform);

        RectTransform contenido = Nuevo("Contenido", raiz);
        Estirar(contenido);

        Image barra = Imagen("Acento", contenido, Redondeado(3), acento, true);
        Anclar(barra.rectTransform, new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(6f, tam.y - 30f));
        barra.rectTransform.pivot = new Vector2(0f, 0.5f);

        TextMeshProUGUI t = Texto("Texto", contenido, texto, fuente, Crema, TextAlignmentOptions.Left, FontStyles.Bold, 3f);
        Estirar(t.rectTransform);
        t.rectTransform.offsetMin = new Vector2(42f, 0f);

        BotonMexicano bm = raiz.gameObject.AddComponent<BotonMexicano>();
        bm.fondo = fondo; bm.borde = borde; bm.acento = barra; bm.texto = t; bm.contenido = contenido; bm.colorAcento = acento;

        if (onClick != null) b.onClick.AddListener(() => onClick());
        return b;
    }

    /// <summary>Tecla (keycap) con texto, para mostrar controles.</summary>
    public static RectTransform Tecla(Transform padre, string texto, float alto = 52f, float fuente = 24f)
    {
        float ancho = Mathf.Max(alto, 26f + texto.Length * fuente * 0.62f);
        RectTransform raiz = Nuevo("Tecla_" + texto, padre);
        raiz.sizeDelta = new Vector2(ancho, alto);

        Image sombra = Imagen("Sombra", raiz, Redondeado(10), new Color(0f, 0f, 0f, 0.45f), true);
        Estirar(sombra.rectTransform);
        sombra.rectTransform.offsetMin = new Vector2(0f, -4f);
        sombra.rectTransform.offsetMax = new Vector2(0f, -4f);

        Image cara = Imagen("Cara", raiz, Redondeado(10), new Color(0.12f, 0.16f, 0.34f, 1f), true);
        Estirar(cara.rectTransform);
        Image borde = Imagen("Borde", raiz, RedondeadoBorde(10, 2f), ConAlfa(Dorado, 0.9f), true);
        Estirar(borde.rectTransform);

        TextMeshProUGUI t = Texto("Texto", raiz, texto, fuente, Crema, TextAlignmentOptions.Center, FontStyles.Bold, 1f);
        Estirar(t.rectTransform);
        return raiz;
    }

    public static Slider SliderMex(Transform padre, string nombre, Vector2 tam, float valorInicial, Color color, Action<float> alCambiar)
    {
        RectTransform raiz = Nuevo(nombre, padre);
        raiz.sizeDelta = tam;
        Slider s = raiz.gameObject.AddComponent<Slider>();

        Image fondo = Imagen("Fondo", raiz, Redondeado(8), new Color(0.03f, 0.05f, 0.14f, 0.9f), true);
        fondo.rectTransform.anchorMin = new Vector2(0f, 0.5f); fondo.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        fondo.rectTransform.sizeDelta = new Vector2(0f, 14f);

        RectTransform area = Nuevo("AreaRelleno", raiz);
        area.anchorMin = new Vector2(0f, 0.5f); area.anchorMax = new Vector2(1f, 0.5f);
        area.sizeDelta = new Vector2(-10f, 14f);
        Image relleno = Imagen("Relleno", area, Redondeado(8), color, true);
        relleno.rectTransform.anchorMin = Vector2.zero; relleno.rectTransform.anchorMax = Vector2.one;
        relleno.rectTransform.offsetMin = Vector2.zero; relleno.rectTransform.offsetMax = Vector2.zero;

        RectTransform zona = Nuevo("ZonaMango", raiz);
        zona.anchorMin = Vector2.zero; zona.anchorMax = Vector2.one;
        zona.offsetMin = new Vector2(10f, 0f); zona.offsetMax = new Vector2(-10f, 0f);
        Image mango = Imagen("Mango", zona, Circulo(), Crema, false, true);
        mango.rectTransform.sizeDelta = new Vector2(30f, 30f);
        Image anillo = Imagen("Anillo", mango.transform, Circulo(), ConAlfa(color, 1f));
        Estirar(anillo.rectTransform);
        anillo.rectTransform.offsetMin = new Vector2(8f, 8f); anillo.rectTransform.offsetMax = new Vector2(-8f, -8f);

        s.fillRect = relleno.rectTransform;
        s.handleRect = mango.rectTransform;
        s.targetGraphic = mango;
        s.direction = Slider.Direction.LeftToRight;
        s.minValue = 0f; s.maxValue = 1f;
        s.value = valorInicial;
        s.transition = Selectable.Transition.None;
        if (alCambiar != null) s.onValueChanged.AddListener(v => alCambiar(v));
        return s;
    }
}

/// <summary>Comportamiento visual del botón del menú: resalta al pasar el ratón o al seleccionarlo con teclado.</summary>
public class BotonMexicano : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, IPointerClickHandler, ISubmitHandler
{
    public Image fondo, borde, acento;
    public TextMeshProUGUI texto;
    public RectTransform contenido;
    public Color colorAcento = UIMexicana.Rosa;

    bool hover, seleccionado;
    float t;
    Color fondoBase = new Color(0.07f, 0.09f, 0.2f, 0.62f);

    void Update()
    {
        float objetivo = (hover || seleccionado) ? 1f : 0f;
        t = Mathf.MoveTowards(t, objetivo, Time.unscaledDeltaTime * 7f);
        float k = Mathf.SmoothStep(0f, 1f, t);

        Color resalte = Color.Lerp(colorAcento, UIMexicana.Noche, 0.45f); resalte.a = 0.95f;
        fondo.color = Color.Lerp(fondoBase, resalte, k);
        borde.color = Color.Lerp(new Color(1f, 1f, 1f, 0.12f), UIMexicana.ConAlfa(colorAcento, 0.95f), k);
        contenido.anchoredPosition = new Vector2(Mathf.Lerp(0f, 16f, k), 0f);
        acento.rectTransform.sizeDelta = new Vector2(Mathf.Lerp(6f, 10f, k), acento.rectTransform.sizeDelta.y);
        texto.color = Color.Lerp(UIMexicana.Crema, Color.white, k);
        transform.localScale = Vector3.one * (1f + 0.025f * k);
    }

    public void OnPointerEnter(PointerEventData e) { hover = true; UIMexicana.Sonar("tick"); }
    public void OnPointerExit(PointerEventData e) { hover = false; }
    public void OnSelect(BaseEventData e) { seleccionado = true; }
    public void OnDeselect(BaseEventData e) { seleccionado = false; }
    public void OnPointerClick(PointerEventData e) { UIMexicana.Sonar("click"); }
    public void OnSubmit(BaseEventData e) { UIMexicana.Sonar("click"); }

    void OnDisable() { hover = false; seleccionado = false; t = 0f; }
}
