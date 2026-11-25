using UnityEngine;

/// <summary>
/// Puntos de aparición sobre el sendero del nivel (SenderoNivel), pegados al terreno.
/// Los niveles 2 y 3 son escenas con Terrain horneado: los spawns con coordenadas fijas (x, 0.2, z)
/// quedaban bajo tierra o lejos del camino. Con esto los enemigos nacen a lo largo del recorrido,
/// a ras de suelo y sin pisar el puesto de Don Chuy.
/// </summary>
public static class SpawnSendero
{
    public static bool Disponible
    {
        get { return SenderoNivel.Instancia != null && SenderoNivel.Instancia.Valido; }
    }

    static float Plano(Vector3 a, Vector3 b)
    {
        return Vector3.Distance(new Vector3(a.x, 0f, a.z), new Vector3(b.x, 0f, b.z));
    }

    /// <summary>Punto del sendero a 'fraccion' (0..1) del recorrido total, desplazado 'lateral' metros (+ = derecha), sobre el suelo.</summary>
    public static Vector3 Punto(float fraccion, float lateral)
    {
        Vector3[] pts = SenderoNivel.Instancia.puntos;
        float total = 0f;
        for (int i = 0; i < pts.Length - 1; i++) total += Plano(pts[i], pts[i + 1]);

        float meta = Mathf.Clamp01(fraccion) * total;
        float acum = 0f;
        Vector3 p = pts[pts.Length - 1];
        Vector3 dir = Vector3.forward;

        for (int i = 0; i < pts.Length - 1; i++)
        {
            float tramo = Plano(pts[i], pts[i + 1]);
            Vector3 d = new Vector3(pts[i + 1].x - pts[i].x, 0f, pts[i + 1].z - pts[i].z);
            if (d.sqrMagnitude > 0.0001f) dir = d.normalized;
            if (acum + tramo >= meta)
            {
                float k = tramo > 0.001f ? (meta - acum) / tramo : 0f;
                p = Vector3.Lerp(pts[i], pts[i + 1], Mathf.Clamp01(k));
                break;
            }
            acum += tramo;
        }

        Vector3 perp = new Vector3(dir.z, 0f, -dir.x);
        p += perp * lateral;

        Vector3 suelo;
        if (UtilTerreno.SueloEn(p.x, p.z, out suelo)) p = suelo;
        return p;
    }

    /// <summary>Igual que Punto pero evita caer encima de Don Chuy (se corre un poco hacia atrás en el camino).</summary>
    public static Vector3 PuntoLejosDeChuy(float fraccion, float lateral, float distanciaMinima = 14f)
    {
        Vector3 p = Punto(fraccion, lateral);
        GameObject chuy = GameObject.Find("Comerciante_DonChuy");
        if (chuy == null) chuy = GameObject.Find("Puesto_DonChuy_Panteon");
        if (chuy == null) return p;

        for (int intento = 0; intento < 4; intento++)
        {
            Vector3 a = new Vector3(p.x, 0f, p.z);
            Vector3 b = new Vector3(chuy.transform.position.x, 0f, chuy.transform.position.z);
            if (Vector3.Distance(a, b) >= distanciaMinima) break;
            fraccion -= 0.04f;
            p = Punto(fraccion, lateral);
        }
        return p;
    }
}
