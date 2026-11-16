using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Zona de emboscada: cuando el Chofer se acerca, aparecen esbirros alrededor (una sola vez).
/// Se coloca en los claros donde hay tótems para que el objetivo tenga que "ganarse".
/// </summary>
public class ZonaEmboscada : MonoBehaviour
{
    [Header("Activación")]
    public float radioActivacion = 16f;
    public bool unaSolaVez = true;
    [TextArea] public string mensajeInicio = "¡Los guardianes de La Bufa te cierran el paso! Click izquierdo: combo  ·  Q: onda espiritual.";
    [TextArea] public string mensajeLimpia = "Zona despejada.";

    [Header("Oleada")]
    public int serpientes = 4;
    public int hombresPiedra = 1;
    public int comadres = 0;
    public float radioMinSpawn = 8f;
    public float radioMaxSpawn = 13f;

    private Transform jugador;
    private bool activada;
    private bool limpiada;
    private readonly List<EsbirroBaseIA> vivos = new List<EsbirroBaseIA>();

    void Update()
    {
        if (jugador == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) jugador = p.transform;
            return;
        }

        if (!activada)
        {
            Vector3 d = jugador.position - transform.position; d.y = 0f;
            if (d.magnitude <= radioActivacion) Activar();
            return;
        }

        if (!limpiada)
        {
            vivos.RemoveAll(e => e == null || e.EstaMuerto);
            if (vivos.Count == 0)
            {
                limpiada = true;
                AvisosJuego.Mostrar(mensajeLimpia, 2.5f);
                if (!unaSolaVez) { activada = false; limpiada = false; }
            }
        }
    }

    void Activar()
    {
        activada = true;
        AvisosJuego.Mostrar(mensajeInicio, 4f);

        int total = serpientes + hombresPiedra + comadres;
        for (int i = 0; i < total; i++)
        {
            float ang = (i / (float)Mathf.Max(1, total)) * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
            float r = Random.Range(radioMinSpawn, radioMaxSpawn);
            Vector3 p = transform.position + new Vector3(Mathf.Cos(ang) * r, 0f, Mathf.Sin(ang) * r);
            Vector3 suelo;
            if (!UtilTerreno.SueloEn(p.x, p.z, out suelo)) continue;

            EsbirroBaseIA.TipoEsbirro tipo = i < hombresPiedra ? EsbirroBaseIA.TipoEsbirro.HombrePiedra
                : (i < hombresPiedra + comadres ? EsbirroBaseIA.TipoEsbirro.Comadre : EsbirroBaseIA.TipoEsbirro.Serpiente);
            EsbirroBaseIA e = FabricaEsbirros.Crear(tipo, suelo + Vector3.up * 0.15f);
            e.rangoDeteccion = Mathf.Max(e.rangoDeteccion, radioMaxSpawn + 6f); // la emboscada va directo hacia el Chofer
            vivos.Add(e);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, radioActivacion);
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, radioMaxSpawn);
    }
}
