using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Proyectil sencillo (murmullos de las Comadres, esquirlas del jefe...). El impacto se calcula por distancia al Chofer
/// (sin triggers ni Rigidbody), así que el esquive con i-frames funciona igual que con los demás ataques.
/// </summary>
public class ProyectilEspiritual : MonoBehaviour
{
    Vector3 direccion;
    float velocidad = 10f, danio = 10f, radioImpacto = 0.9f, vidaRestante = 4f, gravedad;
    Vector3 vel;
    Transform jugador;
    PoderEspiritual vidaJugador;
    Color color;

    static readonly Dictionary<Color, Material> cache = new Dictionary<Color, Material>();

    public static ProyectilEspiritual Crear(Vector3 origen, Vector3 direccion, float velocidad, float danio, Color color,
        float tamanio = 0.45f, float vida = 4f, float gravedad = 0f)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Proyectil";
        Collider c = go.GetComponent<Collider>();
        if (c != null) Destroy(c);
        go.transform.position = origen;
        go.transform.localScale = Vector3.one * tamanio;
        go.GetComponent<Renderer>().sharedMaterial = MaterialEmisivo(color);
        go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        GameObject luz = new GameObject("Luz");
        luz.transform.SetParent(go.transform, false);
        Light l = luz.AddComponent<Light>();
        l.type = LightType.Point; l.color = color; l.range = 5f; l.intensity = 1.6f; l.shadows = LightShadows.None;

        ProyectilEspiritual p = go.AddComponent<ProyectilEspiritual>();
        p.direccion = direccion.normalized;
        p.velocidad = velocidad;
        p.vel = p.direccion * velocidad;
        p.danio = danio;
        p.vidaRestante = vida;
        p.gravedad = gravedad;
        p.color = color;
        return p;
    }

    static Material MaterialEmisivo(Color c)
    {
        Material m;
        if (cache.TryGetValue(c, out m) && m != null) return m;
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        m = new Material(sh);
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        m.EnableKeyword("_EMISSION");
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c * 2.5f);
        cache[c] = m;
        return m;
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) { jugador = p.transform; vidaJugador = p.GetComponent<PoderEspiritual>(); }
    }

    void Update()
    {
        vidaRestante -= Time.deltaTime;
        if (vidaRestante <= 0f) { Destroy(gameObject); return; }

        Vector3 previa = transform.position;
        vel.y += gravedad * Time.deltaTime;
        Vector3 nueva = previa + vel * Time.deltaTime;

        if (jugador != null && vidaJugador != null && !vidaJugador.EstaMuerto)
        {
            Vector3 centro = jugador.position + Vector3.up * 0.2f;
            if (Vector3.Distance(nueva, centro) < radioImpacto + 0.4f || Vector3.Distance(previa, centro) < radioImpacto + 0.4f)
            {
                vidaJugador.RecibirDanio(danio);
                Chispas(nueva);
                Destroy(gameObject);
                return;
            }
        }

        RaycastHit hit;
        if (Physics.Linecast(previa, nueva, out hit, ~0, QueryTriggerInteraction.Ignore))
        {
            bool esViviente = hit.collider.GetComponentInParent<CharacterController>() != null
                           || hit.collider.GetComponentInParent<PoderEspiritual>() != null;
            if (!esViviente) { Chispas(hit.point); Destroy(gameObject); return; }
        }
        transform.position = nueva;
    }

    void Chispas(Vector3 pos)
    {
        GameObject go = new GameObject("ImpactoProyectil");
        go.transform.position = pos;
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = 0.4f; main.loop = false;
        main.startLifetime = 0.45f; main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
        main.startSize = 0.18f; main.startColor = color; main.gravityModifier = 0.5f;
        var em = ps.emission; em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)14) });
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.15f;
        Shader spr = Shader.Find("Sprites/Default");
        if (spr != null) go.GetComponent<ParticleSystemRenderer>().sharedMaterial = new Material(spr);
        ps.Play();
        Destroy(go, 1.2f);
    }
}
