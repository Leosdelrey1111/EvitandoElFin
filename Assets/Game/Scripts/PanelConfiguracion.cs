using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel de Configuración in-game (accesible desde Pausa o desde el Menú):
/// - Pestaña 1: Sonido, Sensibilidad del Mouse, Invertir Eje Y y Calidad Gráfica (en vivo).
/// - Pestaña 2: Reconfiguración de Teclas (movimiento, habilidades, interactuar, pista y códice).
/// </summary>
public class PanelConfiguracion : MonoBehaviour
{
    public static PanelConfiguracion Instancia { get; private set; }
    public static bool Abierto { get { return Instancia != null && Instancia.raiz != null && Instancia.raiz.gameObject.activeSelf; } }
    public static bool EsperandoTecla { get { return Instancia != null && Instancia.accionEnEspera.HasValue; } }

    RectTransform raiz;
    RectTransform paginaAudio, paginaControles;
    Button tabAudio, tabControles;

    Slider slMaster, slMusica, slEfectos, slVoz, slSens;
    TextMeshProUGUI txtSensVal, txtInvertY;
    Button[] btnsCalidad = new Button[3];

    AccionJuego? accionEnEspera = null;
    readonly Dictionary<AccionJuego, TextMeshProUGUI> textosTecla = new Dictionary<AccionJuego, TextMeshProUGUI>();
    readonly Dictionary<AccionJuego, Image> fondosTecla = new Dictionary<AccionJuego, Image>();

    static readonly KeyCode[] todasLasTeclas = (KeyCode[])Enum.GetValues(typeof(KeyCode));

    static readonly (AccionJuego accion, string etiqueta)[] listaAcciones =
    {
        (AccionJuego.Adelante,    "Mover adelante"),
        (AccionJuego.Atras,       "Mover atrás"),
        (AccionJuego.Izquierda,   "Mover izquierda"),
        (AccionJuego.Derecha,     "Mover derecha"),
        (AccionJuego.Correr,      "Correr"),
        (AccionJuego.Saltar,      "Saltar"),
        (AccionJuego.Esquivar,    "Esquivar"),
        (AccionJuego.Golpe,       "Golpear"),
        (AccionJuego.Onda,        "Onda espiritual"),
        (AccionJuego.Interactuar, "Interactuar (Don Chuy)"),
        (AccionJuego.Pista,       "Pista de Tonalli"),
        (AccionJuego.Codice,      "Abrir Códice")
    };

    public static PanelConfiguracion Crear(RectTransform padre)
    {
        if (Instancia != null) return Instancia;
        GameObject go = new GameObject("PanelConfiguracion");
        go.transform.SetParent(padre, false);
        PanelConfiguracion pc = go.AddComponent<PanelConfiguracion>();
        pc.Construir(go.AddComponent<RectTransform>());
        return pc;
    }

    void Awake() { Instancia = this; }
    void OnDestroy() { if (Instancia == this) Instancia = null; }

    public void Abrir()
    {
        if (raiz == null) return;
        RefrescarValores();
        MostrarPestana(true);
        raiz.gameObject.SetActive(true);
    }

    public void Cerrar()
    {
        accionEnEspera = null;
        if (raiz != null) raiz.gameObject.SetActive(false);
        OpcionesJuego.Guardar();
        RefrescarTeclas();
    }

    void Update()
    {
        if (!Abierto) return;

        if (accionEnEspera.HasValue)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                accionEnEspera = null;
                RefrescarTeclas();
                return;
            }
            for (int i = 0; i < todasLasTeclas.Length; i++)
            {
                KeyCode k = todasLasTeclas[i];
                if (k == KeyCode.None || k == KeyCode.Escape) continue;
                if (Input.GetKeyDown(k))
                {
                    OpcionesJuego.AsignarTecla(accionEnEspera.Value, k);
                    accionEnEspera = null;
                    RefrescarTeclas();
                    return;
                }
            }
        }
    }

    void Construir(RectTransform rt)
    {
        raiz = rt;
        UIMexicana.Estirar(raiz);

        Image velo = UIMexicana.Imagen("Velo", raiz, UIMexicana.Blanco(), new Color(0.02f, 0.03f, 0.09f, 0.90f), false, true);
        UIMexicana.Estirar(velo.rectTransform);

        Vector2 tam = new Vector2(1060f, 760f);
        RectTransform tarjeta = UIMexicana.Nuevo("Tarjeta", raiz);
        UIMexicana.AnclarCentro(tarjeta, new Vector2(0.5f, 0.5f), Vector2.zero, tam);
        Image fondo = UIMexicana.Imagen("Fondo", tarjeta, UIMexicana.Redondeado(26), UIMexicana.H("0D1330"), true);
        UIMexicana.Estirar(fondo.rectTransform);
        Image borde = UIMexicana.Imagen("Borde", tarjeta, UIMexicana.RedondeadoBorde(26, 3f), UIMexicana.Turquesa, true);
        UIMexicana.Estirar(borde.rectTransform);

        TextMeshProUGUI tit = UIMexicana.Texto("Titulo", tarjeta, "CONFIGURACIÓN", 40, UIMexicana.Dorado, TextAlignmentOptions.Center, FontStyles.Bold, 5f);
        UIMexicana.AnclarCentro(tit.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(700f, 52f));

        // Pestañas
        tabAudio = UIMexicana.Boton("TabAudio", tarjeta, "SONIDO Y CÁMARA", new Vector2(310f, 48f), UIMexicana.Turquesa, () => MostrarPestana(true), 20f);
        UIMexicana.AnclarCentro(tabAudio.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(-170f, -106f), new Vector2(310f, 48f));

        tabControles = UIMexicana.Boton("TabControles", tarjeta, "CAMBIAR TECLAS", new Vector2(310f, 48f), UIMexicana.Cempasuchil, () => MostrarPestana(false), 20f);
        UIMexicana.AnclarCentro(tabControles.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(170f, -106f), new Vector2(310f, 48f));

        paginaAudio = UIMexicana.Nuevo("PaginaAudio", tarjeta);
        UIMexicana.Estirar(paginaAudio);
        paginaAudio.offsetMin = new Vector2(60f, 95f);
        paginaAudio.offsetMax = new Vector2(-60f, -145f);
        ConstruirPaginaAudio(paginaAudio);

        paginaControles = UIMexicana.Nuevo("PaginaControles", tarjeta);
        UIMexicana.Estirar(paginaControles);
        paginaControles.offsetMin = new Vector2(45f, 95f);
        paginaControles.offsetMax = new Vector2(-45f, -145f);
        ConstruirPaginaControles(paginaControles);

        Button btnCerrar = UIMexicana.Boton("BtnVolver", tarjeta, "VOLVER", new Vector2(260f, 52f), UIMexicana.Turquesa, Cerrar, 24f);
        UIMexicana.AnclarCentro(btnCerrar.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(260f, 52f));

        raiz.gameObject.SetActive(false);
    }

    void MostrarPestana(bool audio)
    {
        if (paginaAudio != null) paginaAudio.gameObject.SetActive(audio);
        if (paginaControles != null) paginaControles.gameObject.SetActive(!audio);
        accionEnEspera = null;
        RefrescarTeclas();
    }

    void ConstruirPaginaAudio(RectTransform padre)
    {
        float y = 210f;
        slMaster = FilaSlider(padre, "Volumen general", y, 0f, 1f, OpcionesJuego.Master, v => OpcionesJuego.Master = v); y -= 64f;
        slMusica = FilaSlider(padre, "Música", y, 0f, 1f, OpcionesJuego.Musica, v => OpcionesJuego.Musica = v); y -= 64f;
        slEfectos = FilaSlider(padre, "Efectos", y, 0f, 1f, OpcionesJuego.Efectos, v => OpcionesJuego.Efectos = v); y -= 64f;
        slVoz = FilaSlider(padre, "Voz de Tonalli", y, 0f, 1f, OpcionesJuego.Voz, v => OpcionesJuego.Voz = v); y -= 64f;

        // Sensibilidad del mouse con valor numérico
        slSens = FilaSlider(padre, "Sensibilidad mouse", y, 0.3f, 2f, OpcionesJuego.Sensibilidad, v =>
        {
            OpcionesJuego.Sensibilidad = v;
            if (txtSensVal != null) txtSensVal.text = v.ToString("0.00") + "x";
        });
        txtSensVal = UIMexicana.Texto("SensVal", padre, OpcionesJuego.Sensibilidad.ToString("0.00") + "x", 20, UIMexicana.Dorado, TextAlignmentOptions.Right, FontStyles.Bold);
        UIMexicana.AnclarCentro(txtSensVal.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-60f, y), new Vector2(90f, 32f));
        y -= 66f;

        // Invertir Eje Y
        TextMeshProUGUI lblInv = UIMexicana.Texto("LblInv", padre, "Invertir cámara vertical (Y)", 22, UIMexicana.Crema, TextAlignmentOptions.Left, FontStyles.Bold);
        UIMexicana.AnclarCentro(lblInv.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-240f, y), new Vector2(360f, 36f));
        Button btnInv = UIMexicana.Boton("BtnInvertY", padre, OpcionesJuego.InvertirY ? "ACTIVADO" : "DESACTIVADO", new Vector2(220f, 44f), UIMexicana.Turquesa, () =>
        {
            OpcionesJuego.InvertirY = !OpcionesJuego.InvertirY;
            if (txtInvertY != null) txtInvertY.text = OpcionesJuego.InvertirY ? "ACTIVADO" : "DESACTIVADO";
        }, 18f);
        UIMexicana.AnclarCentro(btnInv.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(180f, y), new Vector2(220f, 44f));
        txtInvertY = btnInv.GetComponentInChildren<TextMeshProUGUI>();
        y -= 64f;

        // Calidad gráfica
        TextMeshProUGUI lblCal = UIMexicana.Texto("LblCal", padre, "Calidad gráfica", 22, UIMexicana.Crema, TextAlignmentOptions.Left, FontStyles.Bold);
        UIMexicana.AnclarCentro(lblCal.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-240f, y), new Vector2(360f, 36f));
        string[] nombresCal = { "BAJA", "MEDIA", "ALTA" };
        for (int i = 0; i < 3; i++)
        {
            int idx = i;
            Button b = UIMexicana.Boton("BtnCal" + i, padre, nombresCal[i], new Vector2(124f, 42f), UIMexicana.Cempasuchil, () =>
            {
                OpcionesJuego.Calidad = idx;
                ActualizarBotonesCalidad();
            }, 17f);
            UIMexicana.AnclarCentro(b.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(56f + i * 136f, y), new Vector2(124f, 42f));
            btnsCalidad[i] = b;
        }
        ActualizarBotonesCalidad();
    }

    Slider FilaSlider(RectTransform padre, string etiqueta, float y, float min, float max, float val, Action<float> onChanged)
    {
        TextMeshProUGUI t = UIMexicana.Texto("Lbl_" + etiqueta, padre, etiqueta, 22, UIMexicana.Crema, TextAlignmentOptions.Left, FontStyles.Bold);
        UIMexicana.AnclarCentro(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-240f, y), new Vector2(360f, 36f));
        Slider s = UIMexicana.SliderMex(padre, "Sl_" + etiqueta, new Vector2(380f, 36f), val, UIMexicana.Turquesa, null);
        s.minValue = min; s.maxValue = max; s.value = val;
        s.onValueChanged.AddListener(v => onChanged(v));
        UIMexicana.AnclarCentro(s.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(180f, y), new Vector2(380f, 36f));
        return s;
    }

    void ConstruirPaginaControles(RectTransform padre)
    {
        textosTecla.Clear();
        fondosTecla.Clear();

        TextMeshProUGUI nota = UIMexicana.Texto("Nota", padre, "Haz clic en una tecla y pulsa la nueva tecla o botón de mouse (ESC cancela).", 17, UIMexicana.ConAlfa(UIMexicana.Crema, 0.78f), TextAlignmentOptions.Center);
        UIMexicana.AnclarCentro(nota.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(880f, 28f));

        // 2 columnas x 6 filas = 12 acciones
        for (int i = 0; i < listaAcciones.Length; i++)
        {
            int col = i / 6;
            int fila = i % 6;
            float x = col == 0 ? -245f : 245f;
            float y = 148f - fila * 62f;

            var item = listaAcciones[i];
            RectTransform row = UIMexicana.Nuevo("Row_" + item.accion, padre);
            UIMexicana.AnclarCentro(row, new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(440f, 50f));

            TextMeshProUGUI lbl = UIMexicana.Texto("Lbl", row, item.etiqueta, 19, UIMexicana.Crema, TextAlignmentOptions.Left, FontStyles.Bold);
            UIMexicana.AnclarCentro(lbl.rectTransform, new Vector2(0f, 0.5f), new Vector2(130f, 0f), new Vector2(250f, 36f));

            Button btn = UIMexicana.Boton("Key_" + item.accion, row, OpcionesJuego.NombreCortoTecla(OpcionesJuego.ObtenerTecla(item.accion)), new Vector2(150f, 42f), UIMexicana.Dorado, () =>
            {
                accionEnEspera = item.accion;
                RefrescarTeclas();
            }, 17f);
            UIMexicana.AnclarCentro(btn.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(-82f, 0f), new Vector2(150f, 42f));

            textosTecla[item.accion] = btn.GetComponentInChildren<TextMeshProUGUI>();
            Image img = btn.GetComponent<Image>();
            if (img != null) fondosTecla[item.accion] = img;
        }

        Button btnReset = UIMexicana.Boton("BtnReset", padre, "RESTABLECER POR DEFECTO", new Vector2(360f, 44f), UIMexicana.Rojo, () =>
        {
            accionEnEspera = null;
            OpcionesJuego.RestablecerControles();
            RefrescarTeclas();
        }, 18f);
        UIMexicana.AnclarCentro(btnReset.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(360f, 44f));
    }

    void RefrescarValores()
    {
        if (slMaster != null) slMaster.SetValueWithoutNotify(OpcionesJuego.Master);
        if (slMusica != null) slMusica.SetValueWithoutNotify(OpcionesJuego.Musica);
        if (slEfectos != null) slEfectos.SetValueWithoutNotify(OpcionesJuego.Efectos);
        if (slVoz != null) slVoz.SetValueWithoutNotify(OpcionesJuego.Voz);
        if (slSens != null) slSens.SetValueWithoutNotify(OpcionesJuego.Sensibilidad);
        if (txtSensVal != null) txtSensVal.text = OpcionesJuego.Sensibilidad.ToString("0.00") + "x";
        if (txtInvertY != null) txtInvertY.text = OpcionesJuego.InvertirY ? "ACTIVADO" : "DESACTIVADO";
        ActualizarBotonesCalidad();
        RefrescarTeclas();
    }

    void ActualizarBotonesCalidad()
    {
        int c = OpcionesJuego.Calidad;
        for (int i = 0; i < 3; i++)
        {
            if (btnsCalidad[i] == null) continue;
            Image im = btnsCalidad[i].GetComponent<Image>();
            if (im != null) im.color = (i == c) ? UIMexicana.Turquesa : UIMexicana.NocheClara;
        }
    }

    void RefrescarTeclas()
    {
        foreach (var kv in textosTecla)
        {
            bool esperando = accionEnEspera.HasValue && accionEnEspera.Value == kv.Key;
            kv.Value.text = esperando ? "PULSA..." : OpcionesJuego.NombreCortoTecla(OpcionesJuego.ObtenerTecla(kv.Key));
            Image im;
            if (fondosTecla.TryGetValue(kv.Key, out im) && im != null)
                im.color = esperando ? UIMexicana.Rosa : UIMexicana.NocheClara;
        }
    }
}
