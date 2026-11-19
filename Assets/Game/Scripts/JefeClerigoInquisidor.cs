using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controlador y lógica de IA para el JEFE: CLÉRIGO INQUISIDOR.
/// Un implacable juez eclesiástico que persigue al Chofer con paso firme,
/// ejecuta ataques devastadores telegrafiados y puede quedar aturdido ante contraataques.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PoderEspiritual))]
public class JefeClerigoInquisidor : MonoBehaviour
{
    public enum EstadoJefe
    {
        Dormido,
        Persecucion,
        Telegrafiando,
        Atacando,
        Saltando,
        Aturdido,
        Muriendo,
        Derrotado
    }

    [Header("Estadísticas del Jefe")]
    public string nombreJefe = "El Clérigo Inquisidor";
    public float vidaMaxima = 600f;
    public float danioGolpe = 30f;
    public float danioSalto = 40f;

    [Header("Distancias y Rangos")]
    public float radioDeteccion = 20f;
    public float rangoAtaque = 2.4f;
    public float rangoSalto = 9f;
    public float velocidadCaminar = 2.8f;
    public float velocidadCorrer = 5.2f;
    public float velocidadGiro = 8f;

    [Header("Tiempos")]
    public float tiempoTelegrafiado = 0.9f;
    public float cooldownAtaque = 2.2f;
    public float duracionAturdido = 2.5f;

    [Header("Referencias Opcionales")]
    public Animator animator;
    public Transform modeloVisual;

    // Estado interno
    private EstadoJefe estado = EstadoJefe.Dormido;
    private CharacterController controller;
    private PoderEspiritual vida;
    private Transform jugador;
    private PoderEspiritual vidaJugador;
    private Vector3 velocidadVertical;
    private float tiempoProximoAtaque = 0f;
    private float gravedad = -20f;
    private GameObject marcaTelegrafiado;

    // UI de Barra de Vida del Jefe
    private CanvasGroup grupoUI;
    private Image rellenoVida;
    private TextMeshProUGUI textoNombre;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        vida = GetComponent<PoderEspiritual>();
        vida.maxPoder = vidaMaxima;
        vida.actualPoder = vidaMaxima;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // Tag del enemigo para el combate del juego
        gameObject.tag = "Enemigo";

        vida.OnMuerte += Morir;
        vida.OnRecibioDanio += AlRecibirDanio;
    }

    void Start()
    {
        BuscarJugador();
        CrearBarraVidaUI();
    }

    void OnDestroy()
    {
        if (vida != null)
        {
            vida.OnMuerte -= Morir;
            vida.OnRecibioDanio -= AlRecibirDanio;
        }
        if (marcaTelegrafiado != null) Destroy(marcaTelegrafiado);
    }

    void Update()
    {
        if (estado == EstadoJefe.Muriendo || estado == EstadoJefe.Derrotado) return;

        AplicarGravedad();

        if (jugador == null)
        {
            BuscarJugador();
            return;
        }

        ActualizarUI();

        float distancia = Vector3.Distance(transform.position, jugador.position);

        switch (estado)
        {
            case EstadoJefe.Dormido:
                ActualizarAnimacion(0f);
                if (distancia <= radioDeteccion)
                {
                    Despertar();
                }
                break;

            case EstadoJefe.Persecucion:
                MoverHaciaJugador(distancia);
                break;

            case EstadoJefe.Telegrafiando:
                // Se queda en el sitio cargando el golpe mientras telegrafía
                ActualizarAnimacion(0f);
                RotarHacia(jugador.position, 2f);
                break;

            case EstadoJefe.Aturdido:
                ActualizarAnimacion(0.5f); // Unsteady_walk
                break;
        }
    }

    void BuscarJugador()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            jugador = p.transform;
            vidaJugador = p.GetComponent<PoderEspiritual>();
        }
    }

    void Despertar()
    {
        estado = EstadoJefe.Persecucion;
        if (grupoUI != null) StartCoroutine(AparecerUI());
        CamaraTerceraPersona.Sacudir(0.25f);
        Debug.Log($"[Jefe] ¡{nombreJefe} ha despertado!");
    }

    void MoverHaciaJugador(float distancia)
    {
        RotarHacia(jugador.position, velocidadGiro);

        if (distancia <= rangoAtaque && Time.time >= tiempoProximoAtaque)
        {
            StartCoroutine(SecuenciaAtaque());
            return;
        }

        // Posibilidad de salto ofensivo si el jugador se aleja a media distancia
        if (distancia > 5f && distancia <= rangoSalto && Time.time >= tiempoProximoAtaque + 3f && Random.value < 0.015f)
        {
            StartCoroutine(SecuenciaSalto());
            return;
        }

        // Movimiento de persecución
        bool corriendo = distancia > 6f;
        float velocidad = corriendo ? velocidadCorrer : velocidadCaminar;
        Vector3 dir = (jugador.position - transform.position);
        dir.y = 0f;
        dir.Normalize();

        controller.Move(dir * velocidad * Time.deltaTime);
        ActualizarAnimacion(corriendo ? 2f : 1f);
    }

    void RotarHacia(Vector3 objetivo, float velocidad)
    {
        Vector3 dir = (objetivo - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion rotDeseada = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotDeseada, velocidad * Time.deltaTime);
        }
    }

    void AplicarGravedad()
    {
        if (controller.isGrounded && velocidadVertical.y < 0f)
            velocidadVertical.y = -2f;

        velocidadVertical.y += gravedad * Time.deltaTime;
        controller.Move(velocidadVertical * Time.deltaTime);
    }

    void ActualizarAnimacion(float velocidad)
    {
        if (animator == null) return;
        animator.SetFloat("Velocidad", velocidad, 0.1f, Time.deltaTime);
    }

    IEnumerator SecuenciaAtaque()
    {
        estado = EstadoJefe.Telegrafiando;
        tiempoProximoAtaque = Time.time + cooldownAtaque;

        // Mostrar indicador rojo en el suelo bajo el Chofer
        Vector3 puntoImpacto = jugador.position;
        puntoImpacto.y = transform.position.y + 0.05f;
        MostrarTelegrafiado(puntoImpacto, 2.0f);

        yield return new WaitForSeconds(tiempoTelegrafiado);

        OcultarTelegrafiado();

        // Ejecutar ataque
        estado = EstadoJefe.Atacando;
        if (animator != null) animator.SetTrigger("Atacar");

        // Pausa sincronizada con el impacto visual de la animación
        yield return new WaitForSeconds(0.4f);

        // Comprobar si el Chofer está en rango y no esquivó
        float distActual = Vector3.Distance(transform.position, jugador.position);
        if (distActual <= rangoAtaque + 0.6f && vidaJugador != null && !vidaJugador.invulnerable)
        {
            vidaJugador.RecibirDanio(danioGolpe);
            CamaraTerceraPersona.Sacudir(0.35f);
        }

        yield return new WaitForSeconds(0.5f);
        estado = EstadoJefe.Persecucion;
    }

    IEnumerator SecuenciaSalto()
    {
        estado = EstadoJefe.Saltando;
        tiempoProximoAtaque = Time.time + cooldownAtaque;

        if (animator != null) animator.SetTrigger("Saltar");

        Vector3 destino = jugador.position;
        Vector3 origen = transform.position;
        float duracion = 0.8f;
        float t = 0f;

        MostrarTelegrafiado(destino, 3.0f);

        while (t < duracion)
        {
            t += Time.deltaTime;
            float k = t / duracion;
            Vector3 posPlana = Vector3.Lerp(origen, destino, k);
            posPlana.y += Mathf.Sin(k * Mathf.PI) * 3f;
            transform.position = posPlana;
            yield return null;
        }

        OcultarTelegrafiado();
        CamaraTerceraPersona.Sacudir(0.45f);

        // Impacto en área
        if (Vector3.Distance(transform.position, jugador.position) <= 3.2f && vidaJugador != null && !vidaJugador.invulnerable)
        {
            vidaJugador.RecibirDanio(danioSalto);
        }

        yield return new WaitForSeconds(0.4f);
        estado = EstadoJefe.Persecucion;
    }

    public void Aturdir()
    {
        if (estado == EstadoJefe.Muriendo || estado == EstadoJefe.Derrotado) return;
        StartCoroutine(RutinaAturdimiento());
    }

    IEnumerator RutinaAturdimiento()
    {
        estado = EstadoJefe.Aturdido;
        OcultarTelegrafiado();

        if (animator != null) animator.SetBool("Aturdido", true);

        yield return new WaitForSeconds(duracionAturdido);

        if (animator != null) animator.SetBool("Aturdido", false);
        estado = EstadoJefe.Persecucion;
    }

    void AlRecibirDanio(float danio)
    {
        if (estado == EstadoJefe.Dormido) Despertar();

        // 10% de probabilidad de quedar aturdido tras recibir un golpe fuerte
        if (danio >= 35f && Random.value < 0.25f && estado == EstadoJefe.Persecucion)
        {
            Aturdir();
        }
    }

    void Morir()
    {
        if (estado == EstadoJefe.Muriendo || estado == EstadoJefe.Derrotado) return;
        estado = EstadoJefe.Muriendo;

        OcultarTelegrafiado();
        if (animator != null) animator.SetTrigger("Muerte");

        // Soltar almas de recompensa (energía espiritual)
        for (int i = 0; i < 8; i++)
        {
            Vector3 offset = Random.insideUnitSphere * 1.5f;
            offset.y = Mathf.Abs(offset.y) + 0.5f;
            AlmaRecolectable.Crear(transform.position + offset, true, true);
        }

        StartCoroutine(RutinaFinMuerte());
    }

    IEnumerator RutinaFinMuerte()
    {
        if (grupoUI != null)
        {
            float t = 0f;
            while (t < 2f)
            {
                t += Time.deltaTime;
                grupoUI.alpha = 1f - (t / 2f);
                yield return null;
            }
        }

        yield return new WaitForSeconds(3f);
        estado = EstadoJefe.Derrotado;
        gameObject.SetActive(false);
    }

    // ================================================================== TELEGRAFIADO
    void MostrarTelegrafiado(Vector3 posicion, float radio)
    {
        if (marcaTelegrafiado == null)
        {
            marcaTelegrafiado = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marcaTelegrafiado.name = "MarcaTelegrafiadoJefe";
            Destroy(marcaTelegrafiado.GetComponent<Collider>());
            var r = marcaTelegrafiado.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            mat.color = new Color(1f, 0.15f, 0.1f, 0.45f);
            r.material = mat;
        }

        marcaTelegrafiado.transform.position = posicion;
        marcaTelegrafiado.transform.localScale = new Vector3(radio * 2f, 0.02f, radio * 2f);
        marcaTelegrafiado.SetActive(true);
    }

    void OcultarTelegrafiado()
    {
        if (marcaTelegrafiado != null) marcaTelegrafiado.SetActive(false);
    }

    // ================================================================== UI BARRA DE VIDA
    void CrearBarraVidaUI()
    {
        GameObject canvasGo = new GameObject("UI_JefeClerigo");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        grupoUI = canvasGo.AddComponent<CanvasGroup>();
        grupoUI.alpha = 0f;

        // Contenedor barra superior
        GameObject contenedor = new GameObject("ContenedorBarra");
        contenedor.transform.SetParent(canvasGo.transform, false);
        RectTransform rtCont = contenedor.AddComponent<RectTransform>();
        rtCont.anchorMin = new Vector2(0.5f, 1f);
        rtCont.anchorMax = new Vector2(0.5f, 1f);
        rtCont.pivot = new Vector2(0.5f, 1f);
        rtCont.anchoredPosition = new Vector2(0f, -40f);
        rtCont.sizeDelta = new Vector2(600f, 32f);

        // Fondo barra
        GameObject fondo = new GameObject("Fondo");
        fondo.transform.SetParent(contenedor.transform, false);
        Image imgFondo = fondo.AddComponent<Image>();
        imgFondo.color = new Color(0.12f, 0.12f, 0.12f, 0.85f);
        RectTransform rtFondo = fondo.GetComponent<RectTransform>();
        rtFondo.anchorMin = Vector2.zero;
        rtFondo.anchorMax = Vector2.one;
        rtFondo.sizeDelta = Vector2.zero;

        // Relleno rojo
        GameObject relleno = new GameObject("Relleno");
        relleno.transform.SetParent(contenedor.transform, false);
        rellenoVida = relleno.AddComponent<Image>();
        rellenoVida.color = new Color(0.85f, 0.15f, 0.15f, 1f);
        rellenoVida.type = Image.Type.Filled;
        rellenoVida.fillMethod = Image.FillMethod.Horizontal;
        RectTransform rtRelleno = relleno.GetComponent<RectTransform>();
        rtRelleno.anchorMin = Vector2.zero;
        rtRelleno.anchorMax = Vector2.one;
        rtRelleno.sizeDelta = Vector2.zero;

        // Nombre del jefe
        GameObject txtGo = new GameObject("TextoNombre");
        txtGo.transform.SetParent(contenedor.transform, false);
        textoNombre = txtGo.AddComponent<TextMeshProUGUI>();
        textoNombre.text = nombreJefe.ToUpper();
        textoNombre.fontSize = 20;
        textoNombre.fontStyle = FontStyles.Bold;
        textoNombre.alignment = TextAlignmentOptions.Center;
        textoNombre.color = new Color(1f, 0.85f, 0.35f, 1f);
        RectTransform rtTxt = txtGo.GetComponent<RectTransform>();
        rtTxt.anchorMin = new Vector2(0f, 1f);
        rtTxt.anchorMax = new Vector2(1f, 1f);
        rtTxt.pivot = new Vector2(0.5f, 0f);
        rtTxt.anchoredPosition = new Vector2(0f, 6f);
        rtTxt.sizeDelta = new Vector2(0f, 26f);
    }

    IEnumerator AparecerUI()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 2f;
            if (grupoUI != null) grupoUI.alpha = t;
            yield return null;
        }
    }

    void ActualizarUI()
    {
        if (rellenoVida != null && vida != null)
        {
            rellenoVida.fillAmount = Mathf.Lerp(rellenoVida.fillAmount, vida.Porcentaje, Time.deltaTime * 6f);
        }
    }
}
