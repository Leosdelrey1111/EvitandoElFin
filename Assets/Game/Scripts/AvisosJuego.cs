using System;
using UnityEngine;

/// <summary>Canal global de avisos en pantalla (el HUD se suscribe y los muestra como mensajes breves).</summary>
public static class AvisosJuego
{
    public static event Action<string, float> OnAviso;

    public static void Mostrar(string texto, float segundos = 3f)
    {
        if (OnAviso != null) OnAviso(texto, segundos);
        else Debug.Log("[Aviso] " + texto);
    }
}

/// <summary>Utilidades para colocar cosas sobre el terreno.</summary>
public static class UtilTerreno
{
    /// <summary>Busca el suelo (preferentemente el Terrain) bajo una posición XZ.</summary>
    public static bool SueloEn(float x, float z, out Vector3 punto)
    {
        Vector3 origen = new Vector3(x, 400f, z);
        RaycastHit[] hits = Physics.RaycastAll(origen, Vector3.down, 800f, ~0, QueryTriggerInteraction.Ignore);
        float mejor = float.MinValue;
        bool encontrado = false;
        punto = new Vector3(x, 0f, z);

        foreach (RaycastHit h in hits)
        {
            // Solo el terreno o mallas de suelo; ignora árboles/props con collider
            if (!(h.collider is TerrainCollider)) continue;
            if (h.point.y > mejor) { mejor = h.point.y; punto = h.point; encontrado = true; }
        }
        if (!encontrado && hits.Length > 0)
        {
            foreach (RaycastHit h in hits)
                if (h.point.y > mejor) { mejor = h.point.y; punto = h.point; encontrado = true; }
        }
        return encontrado;
    }

    public static Vector3 SobreSuelo(Vector3 pos, float altura)
    {
        Vector3 p;
        if (SueloEn(pos.x, pos.z, out p)) return p + Vector3.up * altura;
        return pos;
    }
}
