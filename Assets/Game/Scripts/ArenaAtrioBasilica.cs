using UnityEngine;
using System.Collections;

/// <summary>
/// Arena cerrada del Atrio de la Basílica para el combate final con el Clérigo Inquisidor (Nivel 3).
/// Cuando el Chofer pisa el Atrio, un anillo CONTINUO de muros de luz se alza alrededor y bloquea la salida
/// hasta derrotar al jefe. Equivalente a la arena de La Bufa (Nivel 1).
///
/// El centro de la arena es la posición de este objeto. 'radioActivacion' debe ser menor que 'radioArena':
/// así el sello solo se cierra con el Chofer ya DENTRO del anillo (antes podía quedar atrapado fuera).
/// </summary>
public class ArenaAtrioBasilica : MonoBehaviour
{
    [Header("Referencias")]
    public Transform jugador;
    public Transform jefe;

    [Header("Activación")]
    [Tooltip("Distancia al centro del Atrio a la que se sella (menor que radioArena).")]
    public float radioActivacion = 12.5f;

    [Header("Anillo de barreras")]
    public int numeroBarreras = 18;
    public float radioArena = 16f;
    public float alturaBarrera = 6f;

    private GameObject[] barreras;
    private Material matBarrera;
    private Light luzArena;
    private PoderEspiritual vidaJugador;
    private bool arenaActiva = false;
    private bool combateIniciado = false;
    private bool jefeVencido = false;

    void Start()
    {
        if (jugador == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) jugador = p.transform;
        }
        if (jugador != null) vidaJugador = jugador.GetComponent<PoderEspiritual>();

        if (jefe == null)
        {
            JefeInquisidor jefeScript = Object.FindAnyObjectByType<JefeInquisidor>();
            if (jefeScript != null) jefe = jefeScript.transform;
        }

        ConstruirBarreras();
        ConstruirLuz();
    }

    Material CrearMaterialBarrera()
    {
        Material m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        Color c = new Color(0.45f, 0.15f, 0.55f, 0.55f);
        m.color = c;
        // Transparente (URP/Lit)
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = 3000;
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", new Color(0.55f, 0.15f, 0.7f) * 1.6f);
        return m;
    }

    void ConstruirBarreras()
    {
        barreras = new GameObject[numeroBarreras];
        matBarrera = CrearMaterialBarrera();

        // Ancho = circunferencia / n (+10 % de solape): el anillo queda CERRADO, sin huecos por donde escapar
        float ancho = (2f * Mathf.PI * radioArena / numeroBarreras) * 1.1f;

        for (int i = 0; i < numeroBarreras; i++)
        {
            float gradosAng = i * (360f / numeroBarreras);
            float angulo = gradosAng * Mathf.Deg2Rad;
            Vector3 pos = transform.position + new Vector3(
                Mathf.Sin(angulo) * radioArena,
                alturaBarrera * 0.5f,
                Mathf.Cos(angulo) * radioArena);

            GameObject barrera = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrera.name = "BarreraAtrio_" + i;
            barrera.transform.position = pos;
            barrera.transform.localScale = new Vector3(ancho, alturaBarrera, 0.6f);
            barrera.transform.rotation = Quaternion.Euler(0f, gradosAng, 0f);
            barrera.GetComponent<Renderer>().sharedMaterial = matBarrera;
            barrera.SetActive(false);
            barreras[i] = barrera;
        }
    }

    void ConstruirLuz()
    {
        GameObject go = new GameObject("LuzArenaAtrio");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 5f, 0f);
        luzArena = go.AddComponent<Light>();
        luzArena.type = LightType.Point;
        luzArena.color = new Color(0.7f, 0.3f, 0.9f);
        luzArena.range = radioArena * 1.8f;
        luzArena.intensity = 0f;
        luzArena.shadows = LightShadows.None;
        luzArena.enabled = false;
    }

    void Update()
    {
        if (jugador == null) return;

        // Si el Chofer cae dentro del combate, el sello se abre y se puede volver a intentar
        if (arenaActiva)
        {
            if (vidaJugador != null && vidaJugador.EstaMuerto) AbrirPorMuerte();
            return;
        }

        if (combateIniciado || jefeVencido) return;

        Vector3 d = jugador.position - transform.position;
        d.y = 0f;
        if (d.magnitude <= radioActivacion) ActivarArena();
    }

    void ActivarArena()
    {
        arenaActiva = true;
        combateIniciado = true;

        foreach (GameObject barrera in barreras)
        {
            if (barrera == null) continue;
            barrera.SetActive(true);
            StartCoroutine(AnimarSubidaBarrera(barrera));
        }

        if (luzArena != null) { luzArena.enabled = true; StartCoroutine(AnimarLuz(0f, 3.2f, 1.5f)); }

        AvisosJuego.Mostrar("¡El Atrio se ha sellado! Debes derrotar al Clérigo Inquisidor.", 4f);

        if (GuiaAlebrije.Instancia != null)
            GuiaAlebrije.Instancia.Decir(HistoriaNivel1.VozPerfil.Tonalli, "El Clérigo Inquisidor se alza ante ti. ¡Este es el enfrentamiento final!", "atrio_cerrado", false);
    }

    IEnumerator AnimarSubidaBarrera(GameObject barrera)
    {
        Vector3 posFinal = barrera.transform.position;
        Vector3 posInicial = posFinal - Vector3.up * alturaBarrera;
        barrera.transform.position = posInicial;

        float t = 0f, duracion = 1.5f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            barrera.transform.position = Vector3.Lerp(posInicial, posFinal, t / duracion);
            yield return null;
        }
        barrera.transform.position = posFinal;
    }

    IEnumerator AnimarLuz(float desde, float hasta, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            if (luzArena != null) luzArena.intensity = Mathf.Lerp(desde, hasta, t / dur);
            yield return null;
        }
        if (luzArena != null)
        {
            luzArena.intensity = hasta;
            if (hasta <= 0.01f) luzArena.enabled = false;
        }
    }

    void BajarBarreras(float segundos)
    {
        foreach (GameObject barrera in barreras)
        {
            if (barrera != null && barrera.activeSelf) StartCoroutine(AnimarBajadaBarrera(barrera, segundos));
        }
        if (luzArena != null && luzArena.enabled) StartCoroutine(AnimarLuz(luzArena.intensity, 0f, segundos));
    }

    void AbrirPorMuerte()
    {
        arenaActiva = false;
        combateIniciado = false; // al volver a entrar se sella de nuevo
        BajarBarreras(0.8f);
    }

    /// <summary>Se llama al derrotar al jefe: el Atrio queda abierto para siempre.</summary>
    public void DesactivarArena()
    {
        jefeVencido = true;
        if (!arenaActiva) return;

        arenaActiva = false;
        BajarBarreras(2f);
        AvisosJuego.Mostrar("¡El sello del Atrio se ha roto! El camino está libre.", 3f);
    }

    IEnumerator AnimarBajadaBarrera(GameObject barrera, float duracion)
    {
        Vector3 posInicial = barrera.transform.position;
        Vector3 posFinal = posInicial - Vector3.up * alturaBarrera;
        Renderer rend = barrera.GetComponent<Renderer>();
        // Colliders fuera desde el primer instante: nadie se queda atorado en un muro que ya baja
        Collider col = barrera.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            barrera.transform.position = Vector3.Lerp(posInicial, posFinal, t / duracion);
            yield return null;
        }

        barrera.SetActive(false);
        barrera.transform.position = posInicial;
        if (col != null) col.enabled = true;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radioActivacion);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radioArena);
    }
}
