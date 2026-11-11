using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Capas de paisaje para el menú, dibujadas por código: cerros lejanos, la ciudad de Guanajuato de noche
/// (casitas de colores con ventanas encendidas y la silueta de la Basílica) y un primer plano con agaves y nopales.
/// </summary>
public static class PaisajeGuanajuato
{
    class Lienzo
    {
        public int w, h; public Color32[] px;
        public Lienzo(int w, int h) { this.w = w; this.h = h; px = new Color32[w * h]; }

        public void Poner(int x, int y, Color32 c)
        {
            if (x < 0 || y < 0 || x >= w || y >= h) return;
            px[y * w + x] = c;
        }

        public void Rect(int x0, int y0, int x1, int y1, Color32 c)
        {
            for (int y = Mathf.Max(0, y0); y < Mathf.Min(h, y1); y++)
                for (int x = Mathf.Max(0, x0); x < Mathf.Min(w, x1); x++) px[y * w + x] = c;
        }

        public void Elipse(float cx, float cy, float rx, float ry, Color32 c)
        {
            int x0 = Mathf.FloorToInt(cx - rx), x1 = Mathf.CeilToInt(cx + rx), y0 = Mathf.FloorToInt(cy - ry), y1 = Mathf.CeilToInt(cy + ry);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x - cx) / rx, dy = (y - cy) / ry;
                    if (dx * dx + dy * dy <= 1f) Poner(x, y, c);
                }
        }

        public void Triangulo(Vector2 a, Vector2 b, Vector2 c, Color32 col)
        {
            int x0 = Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))), x1 = Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x)));
            int y0 = Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))), y1 = Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y)));
            float den = (b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y);
            if (Mathf.Abs(den) < 0.0001f) return;
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float l1 = ((b.y - c.y) * (x - c.x) + (c.x - b.x) * (y - c.y)) / den;
                    float l2 = ((c.y - a.y) * (x - c.x) + (a.x - c.x) * (y - c.y)) / den;
                    float l3 = 1f - l1 - l2;
                    if (l1 >= 0 && l2 >= 0 && l3 >= 0) Poner(x, y, col);
                }
        }

        public Sprite ASprite(string nombre)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixels32(px);
            tex.Apply();
            Sprite s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 100f, 0, SpriteMeshType.FullRect);
            s.name = nombre;
            return s;
        }
    }

    static float Fbm(float x, float sem)
    {
        return (Mathf.PerlinNoise(x * 0.004f + sem, sem * 0.7f) * 0.6f + Mathf.PerlinNoise(x * 0.012f + sem * 2f, 3.3f) * 0.3f + Mathf.PerlinNoise(x * 0.04f + sem, 8.1f) * 0.1f);
    }

    // ================================================================== CERROS LEJANOS (se tiñen con Image.color)
    public static Sprite Cerros(int w, int h, float semilla, float baseAlto, float amplitud)
    {
        Lienzo l = new Lienzo(w, h);
        for (int x = 0; x < w; x++)
        {
            int top = Mathf.Clamp(Mathf.RoundToInt(baseAlto + Fbm(x, semilla) * amplitud), 1, h - 1);
            for (int y = 0; y < top; y++)
            {
                float k = y / (float)top;
                byte v = (byte)Mathf.RoundToInt(Mathf.Lerp(190f, 255f, k));    // la cima algo más clara = bruma
                l.px[y * w + x] = new Color32(v, v, v, 255);
            }
        }
        return l.ASprite("cerros" + semilla);
    }

    // ================================================================== CIUDAD DE GUANAJUATO
    static readonly Color[] FachadasBase =
    {
        new Color(0.85f, 0.62f, 0.18f), new Color(0.80f, 0.34f, 0.24f), new Color(0.26f, 0.48f, 0.80f),
        new Color(0.80f, 0.28f, 0.50f), new Color(0.30f, 0.62f, 0.46f), new Color(0.66f, 0.40f, 0.72f), new Color(0.90f, 0.78f, 0.45f)
    };

    static Color32 Noche(Color c, float k) { return new Color32((byte)(c.r * k * 255f), (byte)(c.g * k * 255f), (byte)(c.b * k * 255f), 255); }

    public static Sprite Ciudad(int w, int h, float baseAlto, float xBasilica)
    {
        Lienzo l = new Lienzo(w, h);
        System.Random rng = new System.Random(1810);   // año de inicio de la Independencia

        int[] cresta = new int[w];
        for (int x = 0; x < w; x++) cresta[x] = Mathf.RoundToInt(baseAlto + Fbm(x, 4.7f) * 150f);

        // Ladera oscura
        for (int x = 0; x < w; x++)
            for (int y = 0; y < cresta[x]; y++) l.px[y * w + x] = new Color32(14, 17, 44, 255);

        // Dos filas de casitas (la de atrás más oscura y baja)
        for (int fila = 0; fila < 2; fila++)
        {
            int x = -10;
            while (x < w)
            {
                int ancho = rng.Next(34, 66);
                int alto = rng.Next(26, 64) - fila * 8;
                int baseY = cresta[Mathf.Clamp(x + ancho / 2, 0, w - 1)] - (fila == 0 ? 36 : 4) - rng.Next(0, 18);
                Color col = FachadasBase[rng.Next(FachadasBase.Length)];
                float k = fila == 0 ? 0.30f : 0.43f;
                l.Rect(x, baseY, x + ancho, baseY + alto, Noche(col, k));
                // techo: plano con borde oscuro o a dos aguas
                if (rng.NextDouble() < 0.4)
                    l.Triangulo(new Vector2(x - 3, baseY + alto), new Vector2(x + ancho + 3, baseY + alto), new Vector2(x + ancho * 0.5f, baseY + alto + ancho * 0.28f), Noche(new Color(0.55f, 0.25f, 0.18f), k * 0.9f));
                else
                    l.Rect(x - 2, baseY + alto, x + ancho + 2, baseY + alto + 4, Noche(col, k * 0.6f));
                // ventanas con luz
                int vent = rng.Next(1, 4);
                for (int v = 0; v < vent; v++)
                {
                    if (rng.NextDouble() < 0.28) continue;
                    int vx = x + 7 + v * (ancho - 14) / Mathf.Max(1, vent);
                    int vy = baseY + 8 + rng.Next(0, Mathf.Max(1, alto - 24));
                    l.Rect(vx, vy, vx + 6, vy + 9, new Color32(255, (byte)rng.Next(190, 225), (byte)rng.Next(80, 130), 255));
                }
                x += ancho + rng.Next(-4, 10);
            }
        }

        // Basílica Colegiata de Nuestra Señora de Guanajuato (silueta con luz cálida)
        int xb = Mathf.RoundToInt(xBasilica);
        int yb = cresta[Mathf.Clamp(xb, 0, w - 1)] - 8;
        Color32 ocre = new Color32(122, 98, 50, 255), ocreOsc = new Color32(98, 78, 42, 255), cupula = new Color32(104, 96, 90, 255);
        l.Rect(xb - 110, yb, xb + 110, yb + 118, ocre);                       // nave
        l.Rect(xb - 118, yb + 118, xb + 118, yb + 128, ocreOsc);              // cornisa
        for (int lado = -1; lado <= 1; lado += 2)                             // torres
        {
            int tx = xb + lado * 74;
            l.Rect(tx - 22, yb, tx + 22, yb + 214, ocre);
            l.Rect(tx - 26, yb + 150, tx + 26, yb + 160, ocreOsc);
            l.Rect(tx - 20, yb + 214, tx + 20, yb + 226, ocreOsc);
            l.Triangulo(new Vector2(tx - 22, yb + 226), new Vector2(tx + 22, yb + 226), new Vector2(tx, yb + 292), cupula);
            l.Rect(tx - 2, yb + 290, tx + 2, yb + 312, ocreOsc);
            l.Rect(tx - 9, yb + 300, tx + 9, yb + 304, ocreOsc);
            for (int v = 0; v < 2; v++)
            {
                l.Rect(tx - 7, yb + 168 + v * 24, tx + 7, yb + 188 + v * 24, new Color32(255, 205, 105, 255));
                l.Rect(tx - 4, yb + 172 + v * 24, tx + 4, yb + 184 + v * 24, new Color32(40, 28, 20, 255));
            }
        }
        l.Elipse(xb, yb + 128, 58, 50, cupula);                               // cúpula
        l.Rect(xb - 12, yb + 170, xb + 12, yb + 196, ocreOsc);                // linterna
        l.Triangulo(new Vector2(xb - 14, yb + 196), new Vector2(xb + 14, yb + 196), new Vector2(xb, yb + 226), cupula);
        l.Rect(xb - 1, yb + 224, xb + 2, yb + 246, ocreOsc);
        l.Rect(xb - 7, yb + 236, xb + 8, yb + 239, ocreOsc);
        l.Rect(xb - 24, yb, xb + 24, yb + 62, new Color32(255, 200, 100, 255)); // portada iluminada
        l.Elipse(xb, yb + 62, 24, 20, new Color32(255, 200, 100, 255));
        l.Rect(xb - 14, yb, xb + 14, yb + 46, new Color32(46, 30, 22, 255));
        l.Elipse(xb, yb + 46, 14, 14, new Color32(46, 30, 22, 255));
        for (int i = -3; i <= 3; i++) if (i != 0 && Mathf.Abs(i) != 0) l.Rect(xb + i * 30 - 5, yb + 40, xb + i * 30 + 5, yb + 62, new Color32(255, 190, 90, 255));

        return l.ASprite("ciudad");
    }

    // ================================================================== PRIMER PLANO: agaves y nopales (siluetas muy oscuras)
    public static Sprite PrimerPlano(int w, int h)
    {
        Lienzo l = new Lienzo(w, h);
        Color32 sombra = new Color32(5, 7, 18, 255);
        Color32 tuna = new Color32(78, 10, 44, 255);

        for (int x = 0; x < w; x++)
        {
            int top = Mathf.RoundToInt(34 + Fbm(x, 9.1f) * 70f);
            for (int y = 0; y < top; y++) l.px[y * w + x] = sombra;
        }

        System.Random rng = new System.Random(12);
        // agaves
        float[] xs = { 90f, 360f, 1530f, 1760f, 1890f };
        foreach (float cx in xs)
        {
            float tam = 80f + (float)rng.NextDouble() * 70f;
            float y0 = 48f + Fbm(cx, 9.1f) * 70f - 6f;
            for (int i = 0; i < 15; i++)
            {
                float a = Mathf.Lerp(-80f, 80f, i / 14f) * Mathf.Deg2Rad;
                float largo = tam * (0.6f + 0.4f * Mathf.Cos(a * 1.1f)) * (0.85f + 0.3f * (float)rng.NextDouble());
                Vector2 b = new Vector2(cx, y0);
                Vector2 punta = new Vector2(cx + Mathf.Sin(a) * largo, y0 + Mathf.Cos(a) * largo * 0.95f);
                l.Triangulo(b + new Vector2(-7, 0), b + new Vector2(7, 0), punta, sombra);
            }
        }
        // nopales
        float[] nx = { 220f, 1650f };
        foreach (float cx in nx)
        {
            float y0 = 44f + Fbm(cx, 9.1f) * 70f;
            l.Elipse(cx, y0 + 40, 30, 44, sombra);
            l.Elipse(cx - 44, y0 + 98, 28, 40, sombra);
            l.Elipse(cx + 42, y0 + 112, 28, 42, sombra);
            l.Elipse(cx + 66, y0 + 168, 22, 34, sombra);
            l.Elipse(cx - 56, y0 + 160, 22, 32, sombra);
            for (int i = 0; i < 5; i++) l.Elipse(cx - 56 + i * 28, y0 + 196 + (i % 2) * 10, 7, 9, tuna);
        }
        return l.ASprite("primerplano");
    }
}
