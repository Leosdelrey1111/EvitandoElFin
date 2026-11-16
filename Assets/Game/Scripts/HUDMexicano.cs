using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD y paneles de juego con estética mexicana (noche, cempasúchil, rosa mexicano, turquesa).
///  - Arriba izquierda: Poder Espiritual (barra con estela de daño), tótems y almas.
///  - Arriba centro: objetivo. Arriba derecha: pausa.
///  - Abajo centro: barra de habilidades con ícono, tecla y recarga (Golpe, Onda, Salto, Esquive).
///  - Avisos, destello de daño y paneles de Pausa / Derrota / Victoria.
/// Se construye por código; basta con tener este componente en la escena del nivel.
/// </summary>
[DefaultExecutionOrder(-50)]
public class HUDMexicano : MonoBehaviour
{
    // --- referencias del juego
    PoderEspiritual vida;
    InventarioEspiritual inventario;
    SistemaCombateChofer combate;
    ChoferController chofer;
    GameManager gm;

    // --- vida
    RectTransform rellenoVida, rellenoRetraso;
    Image imgRellenoVida;
    TextMeshProUGUI textoVida;
    float pvMostrada = 1f, pvRetraso = 1f;
    Image discoEmblema;

    // --- contadores y textos
    TextMeshProUGUI textoTotems, textoAlmas, textoObjetivo, textoAviso;
    CanvasGroup grupoAviso;
    float avisoHasta;

    // --- habilidades
    class Slot
    {
        public RectTransform raiz; public Image fondo, borde, icono, recarga; public TextMeshProUGUI tiempo;
        public bool listoAntes = true; public float destello;
    }
    Slot sGolpe, sOnda, sSalto, sEsquive;
    Image[] pips;

    // --- daño
    Image vinetaDano;
    float alphaDano;

    Canvas canvasHud;

    void Awake()
    {
        UIMexicana.AsegurarEventSystem();
        gm = Object.FindAnyObjectByType<GameManager>();

        OcultarHudAnterior();

        canvasHud = UIMexicana.CrearCanvas("HUD_Mexicano", 50);
        canvasHud.transform.SetParent(transform, false);
        ConstruirHud((RectTransform)canvasHud.transform);

        // Los paneles viven en otro canvas (encima de todo) y reemplazan a los anteriores
        Canvas canvasPaneles = UIMexicana.CrearCanvas("Paneles_Mexicano", 100);
        canvasPaneles.transform.SetParent(transform, false);
        ConstruirPaneles((RectTransform)canvasPaneles.transform);
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            vida = p.GetComponent<PoderEspiritual>();
            inventario = p.GetComponent<InventarioEspiritual>();
            combate = p.GetComponent<SistemaCombateChofer>();
            chofer = p.GetComponent<ChoferController>();
        }
        if (vida != null) vida.OnRecibioDanio += AlRecibirDanio;
        AvisosJuego.OnAviso += MostrarAviso;
    }

    void OnDestroy()
    {
        AvisosJuego.OnAviso -= MostrarAviso;
        if (vida != null) vida.OnRecibioDanio -= AlRecibirDanio;
    }

    /// <summary>Apaga el HUD anterior (HUDEspiritual) y sus elementos, conservando sus paneles ocultos.</summary>
    void OcultarHudAnterior()
    {
        HUDEspiritual viejo = Object.FindAnyObjectByType<HUDEspiritual>(FindObjectsInactive.Include);
        if (viejo == null) return;
        viejo.enabled = false;
        foreach (Transform hijo in viejo.transform)
            if (!hijo.name.StartsWith("Panel")) hijo.gameObject.SetActive(false);
    }

    // ================================================================== HUD

    void ConstruirHud(RectTransform raiz)
    {
        // ---------- viñeta de daño (detrás de todo)
        vinetaDano = UIMexicana.Imagen("VinetaDano", raiz, UIMexicana.Vineta(), new Color(0.9f, 0.05f, 0.1f, 0f));
        UIMexicana.Estirar(vinetaDano.rectTransform);

        ConstruirVida(raiz);
        ConstruirObjetivo(raiz);
        ConstruirHabilidades(raiz);
        ConstruirAviso(raiz);

        // pista de pausa (arriba derecha)
        RectTransform pausa = UIMexicana.Nuevo("PistaPausa", raiz);
        UIMexicana.Anclar(pausa, new Vector2(1f, 1f), new Vector2(-36f, -34f), new Vector2(190f, 46f));
        Image pf = UIMexicana.Imagen("Fondo", pausa, UIMexicana.Redondeado(23), new Color(0.05f, 0.07f, 0.18f, 0.72f), true);
        UIMexicana.Estirar(pf.rectTransform);
        Image pb = UIMexicana.Imagen("Borde", pausa, UIMexicana.RedondeadoBorde(23, 1.4f), UIMexicana.ConAlfa(UIMexicana.Dorado, 0.35f), true);
        UIMexicana.Estirar(pb.rectTransform);
        RectTransform k = UIMexicana.Tecla(pausa, "ESC", 32f, 16f);
        k.anchorMin = k.anchorMax = new Vector2(0f, 0.5f); k.pivot = new Vector2(0f, 0.5f); k.anchoredPosition = new Vector2(12f, 0f);
        TextMeshProUGUI t = UIMexicana.Texto("Texto", pausa, "Pausa", 20, UIMexicana.ConAlfa(UIMexicana.Crema, 0.85f), TextAlignmentOptions.Left, FontStyles.Bold, 2f);
        UIMexicana.Anclar(t.rectTransform, new Vector2(0f, 0.5f), new Vector2(86f, 0f), new Vector2(90f, 36f));
        t.rectTransform.pivot = new Vector2(0f, 0.5f);

        // pista del Códice (libro de la historia)
        RectTransform cod = UIMexicana.Nuevo("PistaCodice", raiz);
        UIMexicana.Anclar(cod, new Vector2(1f, 1f), new Vector2(-36f, -88f), new Vector2(190f, 46f));
        Image cf = UIMexicana.Imagen("Fondo", cod, UIMexicana.Redondeado(23), new Color(0.05f, 0.07f, 0.18f, 0.72f), true);
        UIMexicana.Estirar(cf.rectTransform);
        Image cb = UIMexicana.Imagen("Borde", cod, UIMexicana.RedondeadoBorde(23, 1.4f), UIMexicana.ConAlfa(UIMexicana.Dorado, 0.35f), true);
        UIMexicana.Estirar(cb.rectTransform);
        RectTransform kc = UIMexicana.Tecla(cod, "TAB", 32f, 16f);
        kc.anchorMin = kc.anchorMax = new Vector2(0f, 0.5f); kc.pivot = new Vector2(0f, 0.5f); kc.anchoredPosition = new Vector2(12f, 0f);
        TextMeshProUGUI tc = UIMexicana.Texto("Texto", cod, "Códice", 20, UIMexicana.ConAlfa(UIMexicana.Crema, 0.85f), TextAlignmentOptions.Left, FontStyles.Bold, 2f);
        UIMexicana.Anclar(tc.rectTransform, new Vector2(0f, 0.5f), new Vector2(86f, 0f), new Vector2(90f, 36f));
        tc.rectTransform.pivot = new Vector2(0f, 0.5f);
    }

    void ConstruirVida(RectTransform raiz)
    {
        RectTransform grupo = UIMexicana.Nuevo("Vida", raiz);
        UIMexicana.Anclar(grupo, new Vector2(0f, 1f), new Vector2(36f, -30f), new Vector2(560f, 170f));

        // barra
        RectTransform marco = UIMexicana.Nuevo("Marco", grupo);
        UIMexicana.Anclar(marco, new Vector2(0f, 1f), new Vector2(70f, -32f), new Vector2(430f, 34f));
        Image sombra = UIMexicana.Imagen("Sombra", marco, UIMexicana.Redondeado(17), new Color(0f, 0f, 0f, 0.4f), true);
        UIMexicana.Estirar(sombra.rectTransform); sombra.rectTransform.offsetMin = new Vector2(-2f, -4f); sombra.rectTransform.offsetMax = new Vector2(2f, -2f);
        Image fondo = UIMexicana.Imagen("Fondo", marco, UIMexicana.Redondeado(17), new Color(0.04f, 0.06f, 0.16f, 0.9f), true);
        UIMexicana.Estirar(fondo.rectTransform);

        RectTransform interior = UIMexicana.Nuevo("Interior", marco);
        UIMexicana.Estirar(interior);
        interior.offsetMin = new Vector2(5f, 5f); interior.offsetMax = new Vector2(-5f, -5f);

        Image retraso = UIMexicana.Imagen("Retraso", interior, UIMexicana.Redondeado(12), UIMexicana.H("FFB36B"), true);
        rellenoRetraso = retraso.rectTransform;
        rellenoRetraso.anchorMin = Vector2.zero; rellenoRetraso.anchorMax = Vector2.one; rellenoRetraso.pivot = new Vector2(0f, 0.5f);
        rellenoRetraso.offsetMin = Vector2.zero; rellenoRetraso.offsetMax = Vector2.zero;

        imgRellenoVida = UIMexicana.Imagen("Relleno", interior, UIMexicana.Redondeado(12), UIMexicana.Turquesa, true);
        rellenoVida = imgRellenoVida.rectTransform;
        rellenoVida.anchorMin = Vector2.zero; rellenoVida.anchorMax = Vector2.one; rellenoVida.pivot = new Vector2(0f, 0.5f);
        rellenoVida.offsetMin = Vector2.zero; rellenoVida.offsetMax = Vector2.zero;
        Image brillo = UIMexicana.Imagen("Brillo", rellenoVida, UIMexicana.Redondeado(8), new Color(1f, 1f, 1f, 0.22f), true);
        brillo.rectTransform.anchorMin = new Vector2(0f, 0.5f); brillo.rectTransform.anchorMax = new Vector2(1f, 1f);
        brillo.rectTransform.offsetMin = new Vector2(4f, 0f); brillo.rectTransform.offsetMax = new Vector2(-4f, -2f);

        Image borde = UIMexicana.Imagen("Borde", marco, UIMexicana.RedondeadoBorde(17, 1.8f), UIMexicana.ConAlfa(UIMexicana.Dorado, 0.75f), true);
        UIMexicana.Estirar(borde.rectTransform);

        textoVida = UIMexicana.Texto("TextoVida", marco, "100 / 100", 17, Color.white, TextAlignmentOptions.Right, FontStyles.Bold, 1f);
        UIMexicana.Estirar(textoVida.rectTransform);
        textoVida.rectTransform.offsetMax = new Vector2(-16f, 0f);

        TextMeshProUGUI etiqueta = UIMexicana.Texto("Etiqueta", grupo, "PODER ESPIRITUAL", 15, UIMexicana.Turquesa, TextAlignmentOptions.Left, FontStyles.Bold, 5f);
        UIMexicana.Anclar(etiqueta.rectTransform, new Vector2(0f, 1f), new Vector2(92f, 0f), new Vector2(380f, 26f));

        // emblema (calavera sobre flor)
        RectTransform emb = UIMexicana.Nuevo("Emblema", grupo);
        UIMexicana.Anclar(emb, new Vector2(0f, 1f), new Vector2(0f, -8f), new Vector2(88f, 88f));
        Image flor = UIMexicana.Imagen("Flor", emb, UIMexicana.IconoSprite(UIMexicana.Icono.Flor), UIMexicana.ConAlfa(UIMexicana.Cempasuchil, 0.95f));
        UIMexicana.Estirar(flor.rectTransform);
        discoEmblema = UIMexicana.Imagen("Disco", emb, UIMexicana.Circulo(), UIMexicana.H("12183C"));
        UIMexicana.AnclarCentro(discoEmblema.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60f, 60f));
        Image cal = UIMexicana.Imagen("Calavera", emb, UIMexicana.IconoSprite(UIMexicana.Icono.Calavera), UIMexicana.Crema);
        UIMexicana.AnclarCentro(cal.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46f, 46f));

        // contadores: tótems y almas
        textoTotems = Chip(grupo, "ChipTotems", UIMexicana.Icono.Totem, UIMexicana.Turquesa, new Vector2(70f, -86f), 124f);
        textoAlmas = Chip(grupo, "ChipAlmas", UIMexicana.Icono.Alma, UIMexicana.H("9FE7FF"), new Vector2(206f, -86f), 112f);
    }

    TextMeshProUGUI Chip(RectTransform padre, string nombre, UIMexicana.Icono icono, Color colorIcono, Vector2 pos, float ancho)
    {
        RectTransform chip = UIMexicana.Nuevo(nombre, padre);
        UIMexicana.Anclar(chip, new Vector2(0f, 1f), pos, new Vector2(ancho, 44f));
        Image f = UIMexicana.Imagen("Fondo", chip, UIMexicana.Redondeado(22), new Color(0.05f, 0.07f, 0.18f, 0.8f), true);
        UIMexicana.Estirar(f.rectTransform);
        Image b = UIMexicana.Imagen("Borde", chip, UIMexicana.RedondeadoBorde(22, 1.4f), UIMexicana.ConAlfa(colorIcono, 0.45f), true);
        UIMexicana.Estirar(b.rectTransform);
        Image ic = UIMexicana.Imagen("Icono", chip, UIMexicana.IconoSprite(icono), colorIcono);
        UIMexicana.AnclarCentro(ic.rectTransform, new Vector2(0f, 0.5f), new Vector2(26f, 0f), new Vector2(28f, 28f));
        TextMeshProUGUI t = UIMexicana.Texto("Valor", chip, "0", 26, UIMexicana.Crema, TextAlignmentOptions.Left, FontStyles.Bold, 1f);
        UIMexicana.Anclar(t.rectTransform, new Vector2(0f, 0.5f), new Vector2(50f, 0f), new Vector2(ancho - 56f, 40f));
        t.rectTransform.pivot = new Vector2(0f, 0.5f);
        return t;
    }

    void ConstruirObjetivo(RectTransform raiz)
    {
        RectTransform obj = UIMexicana.Nuevo("Objetivo", raiz);
        UIMexicana.AnclarCentro(obj, new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(740f, 46f));
        Image f = UIMexicana.Imagen("Fondo", obj, UIMexicana.Redondeado(23), new Color(0.05f, 0.07f, 0.18f, 0.7f), true);
        UIMexicana.Estirar(f.rectTransform);
        Image b = UIMexicana.Imagen("Borde", obj, UIMexicana.RedondeadoBorde(23, 1.4f), UIMexicana.ConAlfa(UIMexicana.Cempasuchil, 0.45f), true);
        UIMexicana.Estirar(b.rectTransform);
        Image fl = UIMexicana.Imagen("Flor", obj, UIMexicana.IconoSprite(UIMexicana.Icono.Flor), UIMexicana.Cempasuchil);
        UIMexicana.AnclarCentro(fl.rectTransform, new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(28f, 28f));
        textoObjetivo = UIMexicana.Texto("Texto", obj, "", 22, UIMexicana.Crema, TextAlignmentOptions.Left, FontStyles.Bold, 1.5f);
        UIMexicana.Estirar(textoObjetivo.rectTransform);
        textoObjetivo.rectTransform.offsetMin = new Vector2(56f, 0f);
        textoObjetivo.rectTransform.offsetMax = new Vector2(-18f, 0f);
    }

    void ConstruirAviso(RectTransform raiz)
    {
        RectTransform av = UIMexicana.Nuevo("Aviso", raiz);
        UIMexicana.AnclarCentro(av, new Vector2(0.5f, 0f), new Vector2(0f, 260f), new Vector2(1100f, 60f));
        grupoAviso = av.gameObject.AddComponent<CanvasGroup>();
        grupoAviso.alpha = 0f;
        Image f = UIMexicana.Imagen("Fondo", av, UIMexicana.Redondeado(30), new Color(0.05f, 0.07f, 0.18f, 0.78f), true);
        UIMexicana.Estirar(f.rectTransform);
        Image b = UIMexicana.Imagen("Borde", av, UIMexicana.RedondeadoBorde(30, 1.6f), UIMexicana.ConAlfa(UIMexicana.Dorado, 0.6f), true);
        UIMexicana.Estirar(b.rectTransform);
        textoAviso = UIMexicana.Texto("Texto", av, "", 27, UIMexicana.Crema, TextAlignmentOptions.Center, FontStyles.Bold, 1f);
        UIMexicana.Estirar(textoAviso.rectTransform);
        textoAviso.rectTransform.offsetMin = new Vector2(24f, 0f); textoAviso.rectTransform.offsetMax = new Vector2(-24f, 0f);
    }

    // ---------------------------------------------------------------- habilidades

    void ConstruirHabilidades(RectTransform raiz)
    {
        RectTransform barra = UIMexicana.Nuevo("Habilidades", raiz);
        UIMexicana.AnclarCentro(barra, new Vector2(0.5f, 0f), new Vector2(0f, 96f), new Vector2(520f, 150f));

        sGolpe = CrearSlot(barra, "Golpe", "GOLPE", "CLIC", UIMexicana.Icono.Golpe, UIMexicana.Rosa, -210f);
        sOnda = CrearSlot(barra, "Onda", "ONDA", "Q", UIMexicana.Icono.Onda, UIMexicana.Turquesa, -70f);
        sSalto = CrearSlot(barra, "Salto", "SALTO", "ESPACIO", UIMexicana.Icono.Salto, UIMexicana.Cempasuchil, 70f);
        sEsquive = CrearSlot(barra, "Esquive", "ESQUIVE", "CTRL", UIMexicana.Icono.Esquive, UIMexicana.Violeta, 210f);

        // pips del combo (3) arriba del slot de golpe
        pips = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            Image p = UIMexicana.Imagen("Pip" + i, sGolpe.raiz, UIMexicana.Circulo(), new Color(1f, 1f, 1f, 0.2f));
            UIMexicana.AnclarCentro(p.rectTransform, new Vector2(0.5f, 1f), new Vector2((i - 1) * 16f, -12f), new Vector2(9f, 9f));
            pips[i] = p;
        }
    }

    Slot CrearSlot(RectTransform padre, string id, string nombre, string tecla, UIMexicana.Icono icono, Color color, float x)
    {
        Slot s = new Slot();
        s.raiz = UIMexicana.Nuevo("Slot_" + id, padre);
        UIMexicana.AnclarCentro(s.raiz, new Vector2(0.5f, 0.5f), new Vector2(x, 14f), new Vector2(94f, 94f));

        Image sombra = UIMexicana.Imagen("Sombra", s.raiz, UIMexicana.Redondeado(20), new Color(0f, 0f, 0f, 0.4f), true);
        UIMexicana.Estirar(sombra.rectTransform); sombra.rectTransform.offsetMin = new Vector2(-2f, -5f); sombra.rectTransform.offsetMax = new Vector2(2f, -2f);
        s.fondo = UIMexicana.Imagen("Fondo", s.raiz, UIMexicana.Redondeado(20), new Color(0.05f, 0.07f, 0.19f, 0.9f), true);
        UIMexicana.Estirar(s.fondo.rectTransform);
        s.borde = UIMexicana.Imagen("Borde", s.raiz, UIMexicana.RedondeadoBorde(20, 2.2f), UIMexicana.ConAlfa(color, 0.9f), true);
        UIMexicana.Estirar(s.borde.rectTransform);
        s.icono = UIMexicana.Imagen("Icono", s.raiz, UIMexicana.IconoSprite(icono), color);
        UIMexicana.AnclarCentro(s.icono.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(56f, 56f));

        s.recarga = UIMexicana.Imagen("Recarga", s.raiz, UIMexicana.Blanco(), new Color(0.02f, 0.03f, 0.1f, 0.78f));
        UIMexicana.Estirar(s.recarga.rectTransform);
        s.recarga.rectTransform.offsetMin = new Vector2(5f, 5f); s.recarga.rectTransform.offsetMax = new Vector2(-5f, -5f);
        s.recarga.type = Image.Type.Filled;
        s.recarga.fillMethod = Image.FillMethod.Radial360;
        s.recarga.fillOrigin = (int)Image.Origin360.Top;
        s.recarga.fillClockwise = false;
        s.recarga.fillAmount = 0f;

        s.tiempo = UIMexicana.Texto("Tiempo", s.raiz, "", 28, Color.white, TextAlignmentOptions.Center, FontStyles.Bold, 0f);
        UIMexicana.Estirar(s.tiempo.rectTransform);

        TextMeshProUGUI n = UIMexicana.Texto("Nombre", s.raiz, nombre, 14, UIMexicana.ConAlfa(UIMexicana.Crema, 0.7f), TextAlignmentOptions.Center, FontStyles.Bold, 3f);
        UIMexicana.AnclarCentro(n.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 16f), new Vector2(130f, 22f));

        RectTransform k = UIMexicana.Tecla(s.raiz, tecla, 34f, tecla.Length > 3 ? 14f : 18f);
        k.anchorMin = k.anchorMax = new Vector2(0.5f, 0f);
        k.pivot = new Vector2(0.5f, 1f);
        k.anchoredPosition = new Vector2(0f, -10f);
        return s;
    }

    // ================================================================== PANELES (Pausa, Derrota, Victoria)

    RectTransform CrearPanelBase(RectTransform padre, string nombre, Vector2 tamTarjeta, Color acento, string titulo, string subtitulo, UIMexicana.Icono icono)
    {
        RectTransform raiz = UIMexicana.Nuevo(nombre, padre);
        UIMexicana.Estirar(raiz);
        CanvasGroup cg = raiz.gameObject.AddComponent<CanvasGroup>();
        raiz.gameObject.AddComponent<PanelAparece>();

        Image atenuar = UIMexicana.Imagen("Atenuar", raiz, UIMexicana.Blanco(), UIMexicana.ConAlfa(UIMexicana.NocheProfunda, 0.8f), false, true);
        UIMexicana.Estirar(atenuar.rectTransform);
        Image vin = UIMexicana.Imagen("Vineta", raiz, UIMexicana.Vineta(), UIMexicana.ConAlfa(acento, 0.28f));
        UIMexicana.Estirar(vin.rectTransform);

        RectTransform tarjeta = UIMexicana.Nuevo("Tarjeta", raiz);
        UIMexicana.AnclarCentro(tarjeta, new Vector2(0.5f, 0.5f), Vector2.zero, tamTarjeta);
        Image sombra = UIMexicana.Imagen("Sombra", tarjeta, UIMexicana.Redondeado(30), new Color(0f, 0f, 0f, 0.5f), true);
        UIMexicana.Estirar(sombra.rectTransform); sombra.rectTransform.offsetMin = new Vector2(-14f, -22f); sombra.rectTransform.offsetMax = new Vector2(14f, 6f);
        Image fondo = UIMexicana.Imagen("Fondo", tarjeta, UIMexicana.Redondeado(30), UIMexicana.ConAlfa(UIMexicana.Noche, 0.97f), true);
        UIMexicana.Estirar(fondo.rectTransform);
        Image borde = UIMexicana.Imagen("Borde", tarjeta, UIMexicana.RedondeadoBorde(30, 2.5f), UIMexicana.ConAlfa(acento, 0.9f), true);
        UIMexicana.Estirar(borde.rectTransform);
        Image bordeI = UIMexicana.Imagen("BordeInterior", tarjeta, UIMexicana.RedondeadoBorde(22, 1.2f), UIMexicana.ConAlfa(UIMexicana.Dorado, 0.45f), true);
        UIMexicana.Estirar(bordeI.rectTransform); bordeI.rectTransform.offsetMin = new Vector2(10f, 10f); bordeI.rectTransform.offsetMax = new Vector2(-10f, -10f);

        // papel picado superior
        int n = Mathf.RoundToInt(tamTarjeta.x / 80f);
        for (int i = 0; i < n; i++)
        {
            Color c = UIMexicana.ColoresPapelPicado[i % UIMexicana.ColoresPapelPicado.Length]; c.a = 0.9f;
            Image f = UIMexicana.Imagen("mini" + i, tarjeta, UIMexicana.BanderaPapelPicado(i % 3), c);
            RectTransform rt = f.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f); rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(58f, 40f);
            rt.anchoredPosition = new Vector2((i - (n - 1) * 0.5f) * 72f, -12f);
        }

        Image ic = UIMexicana.Imagen("Icono", tarjeta, UIMexicana.IconoSprite(icono), acento);
        UIMexicana.AnclarCentro(ic.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(84f, 84f));
        TextMeshProUGUI t = UIMexicana.Texto("Titulo", tarjeta, titulo, 50, UIMexicana.Crema, TextAlignmentOptions.Center, FontStyles.Bold, 5f);
        UIMexicana.AnclarCentro(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -196f), new Vector2(tamTarjeta.x - 80f, 64f));
        t.enableVertexGradient = true;
        t.colorGradient = new VertexGradient(UIMexicana.Crema, UIMexicana.Crema, Color.Lerp(UIMexicana.Crema, acento, 0.7f), Color.Lerp(UIMexicana.Crema, acento, 0.7f));
        if (!string.IsNullOrEmpty(subtitulo))
        {
            TextMeshProUGUI s = UIMexicana.Texto("Subtitulo", tarjeta, subtitulo, 22, UIMexicana.ConAlfa(UIMexicana.Crema, 0.8f), TextAlignmentOptions.Center);
            UIMexicana.AnclarCentro(s.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -246f), new Vector2(tamTarjeta.x - 120f, 40f));
        }

        raiz.gameObject.SetActive(false);
        return raiz;
    }

    void ConstruirPaneles(RectTransform padre)
    {
        // ---------- PAUSA
        RectTransform pausa = CrearPanelBase(padre, "PanelPausa", new Vector2(1120f, 640f), UIMexicana.Turquesa, "PAUSA", "El tiempo se detiene… la sierra espera.", UIMexicana.Icono.Calavera);
        Vector2 tb = new Vector2(430f, 64f);
        BotonPanel(pausa, "BtnReanudar", "REANUDAR", new Vector2(-250f, -10f), tb, UIMexicana.Turquesa);
        BotonPanel(pausa, "BtnReiniciar", "REINICIAR NIVEL", new Vector2(-250f, -92f), tb, UIMexicana.Cempasuchil);
        BotonPanel(pausa, "BtnMenu", "MENÚ PRINCIPAL", new Vector2(-250f, -174f), tb, UIMexicana.Rojo);
        ListaControles((RectTransform)pausa.Find("Tarjeta"), new Vector2(210f, -86f));

        // ---------- DERROTA
        RectTransform derrota = CrearPanelBase(padre, "PanelGameOver", new Vector2(760f, 640f), UIMexicana.Rojo, "TU ALMA SE DESVANECIÓ", "Los esbirros del bosque te vencieron.\nLevántate y vuelve a intentarlo.", UIMexicana.Icono.Calavera);
        BotonPanel(derrota, "BtnReintentar", "REINTENTAR", new Vector2(0f, -70f), new Vector2(460f, 66f), UIMexicana.Rosa);
        BotonPanel(derrota, "BtnMenu", "MENÚ PRINCIPAL", new Vector2(0f, -152f), new Vector2(460f, 66f), UIMexicana.Violeta);

        // ---------- VICTORIA
        RectTransform victoria = CrearPanelBase(padre, "PanelVictoria", new Vector2(760f, 640f), UIMexicana.Dorado, "¡NIVEL 1 COMPLETADO!", "Rompiste el hechizo de La Bufa y cruzaste el portal espiritual.", UIMexicana.Icono.Flor);
        BotonPanel(victoria, "BtnRejugar", "JUGAR DE NUEVO", new Vector2(0f, -70f), new Vector2(460f, 66f), UIMexicana.Turquesa);
        BotonPanel(victoria, "BtnMenu", "MENÚ PRINCIPAL", new Vector2(0f, -152f), new Vector2(460f, 66f), UIMexicana.Violeta);
        textoResumen = UIMexicana.Texto("Resumen", (RectTransform)victoria.Find("Tarjeta"), "", 26, UIMexicana.Dorado, TextAlignmentOptions.Center, FontStyles.Bold, 2f);
        UIMexicana.AnclarCentro(textoResumen.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(640f, 44f));
        panelVictoria = victoria.gameObject;

        if (gm != null)
        {
            gm.panelPausa = pausa.gameObject;
            gm.panelGameOver = derrota.gameObject;
            gm.panelVictoria = victoria.gameObject;
        }
    }

    TextMeshProUGUI textoResumen;
    GameObject panelVictoria;

    /// <summary>Botón del panel: es hijo directo del panel (GameManager lo busca por nombre) y se posiciona respecto al centro.</summary>
    void BotonPanel(RectTransform panel, string nombre, string texto, Vector2 pos, Vector2 tam, Color acento)
    {
        Button b = UIMexicana.Boton(nombre, panel, texto, tam, acento, null, 27f);
        UIMexicana.AnclarCentro(b.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), pos, tam);
    }

    void ListaControles(RectTransform tarjeta, Vector2 origen)
    {
        TextMeshProUGUI cab = UIMexicana.Texto("CabControles", tarjeta, "CONTROLES", 20, UIMexicana.Dorado, TextAlignmentOptions.Left, FontStyles.Bold, 6f);
        UIMexicana.AnclarCentro(cab.rectTransform, new Vector2(0.5f, 0.5f), origen + new Vector2(60f, 118f), new Vector2(330f, 30f));

        string[][] filas =
        {
            new[] { "WASD", "Moverte" }, new[] { "SHIFT", "Correr" }, new[] { "ESPACIO", "Saltar" },
            new[] { "CTRL", "Esquivar" }, new[] { "CLIC", "Golpear" }, new[] { "Q", "Onda espiritual" },
        };
        for (int i = 0; i < filas.Length; i++)
        {
            RectTransform fila = UIMexicana.Nuevo("Ctl" + i, tarjeta);
            UIMexicana.AnclarCentro(fila, new Vector2(0.5f, 0.5f), origen + new Vector2(60f, 74f - i * 46f), new Vector2(340f, 40f));
            RectTransform k = UIMexicana.Tecla(fila, filas[i][0], 36f, filas[i][0].Length > 4 ? 14f : 17f);
            k.anchorMin = k.anchorMax = new Vector2(0f, 0.5f); k.pivot = new Vector2(0f, 0.5f); k.anchoredPosition = Vector2.zero;
            TextMeshProUGUI d = UIMexicana.Texto("D", fila, filas[i][1], 20, UIMexicana.Crema, TextAlignmentOptions.Left);
            UIMexicana.Anclar(d.rectTransform, new Vector2(0f, 0.5f), new Vector2(112f, 0f), new Vector2(220f, 34f));
            d.rectTransform.pivot = new Vector2(0f, 0.5f);
        }
    }

    // ================================================================== ACTUALIZACIÓN

    void AlRecibirDanio(float cantidad)
    {
        alphaDano = Mathf.Clamp(0.35f + cantidad * 0.01f, 0.35f, 0.8f);
    }

    void MostrarAviso(string texto, float segundos)
    {
        textoAviso.text = texto;
        avisoHasta = Time.unscaledTime + segundos;
    }

    void Update()
    {
        if (vida == null) { ActualizarObjetivo(); return; }

        // ---- vida
        float objetivo = vida.Porcentaje;
        pvMostrada = Mathf.MoveTowards(pvMostrada, objetivo, 2.2f * Time.unscaledDeltaTime);
        if (pvRetraso > pvMostrada) pvRetraso = Mathf.MoveTowards(pvRetraso, pvMostrada, 0.45f * Time.unscaledDeltaTime);
        else pvRetraso = pvMostrada;
        rellenoVida.anchorMax = new Vector2(Mathf.Clamp01(pvMostrada), 1f);
        rellenoRetraso.anchorMax = new Vector2(Mathf.Clamp01(pvRetraso), 1f);
        Color alta = UIMexicana.Turquesa, media = UIMexicana.Dorado, baja = UIMexicana.Rojo;
        Color c = pvMostrada > 0.5f ? Color.Lerp(media, alta, (pvMostrada - 0.5f) * 2f) : Color.Lerp(baja, media, pvMostrada * 2f);
        imgRellenoVida.color = c;
        textoVida.text = Mathf.CeilToInt(vida.actualPoder) + " / " + Mathf.CeilToInt(vida.maxPoder);
        float latido = pvMostrada < 0.3f ? 1f + 0.06f * Mathf.Sin(Time.unscaledTime * 8f) : 1f;
        discoEmblema.transform.parent.localScale = Vector3.one * latido;

        // ---- contadores
        if (inventario != null && gm != null)
        {
            textoTotems.text = inventario.cantidadTotems + "/" + gm.totemsRequeridos;
            textoAlmas.text = Mathf.RoundToInt(inventario.energiaEspiritual).ToString();
        }
        ActualizarObjetivo();

        // ---- habilidades
        if (combate != null)
        {
            ActualizarRecarga(sOnda, combate.ProgresoAtaqueArea, combate.cooldownArea);
            // golpe: activo mientras ataca; pips = paso del combo
            bool atacando = combate.Atacando;
            sGolpe.fondo.color = Color.Lerp(sGolpe.fondo.color, atacando ? new Color(0.35f, 0.04f, 0.2f, 0.95f) : new Color(0.05f, 0.07f, 0.19f, 0.9f), Time.unscaledDeltaTime * 14f);
            for (int i = 0; i < 3; i++)
                pips[i].color = i < combate.PasoCombo ? UIMexicana.Rosa : new Color(1f, 1f, 1f, 0.2f);
        }
        if (chofer != null)
        {
            ActualizarRecarga(sEsquive, chofer.ProgresoEsquive, chofer.recargaEsquive);
            bool suelo = chofer.EstaEnSuelo;
            Color ci = sSalto.icono.color; ci.a = Mathf.Lerp(ci.a, suelo ? 1f : 0.35f, Time.unscaledDeltaTime * 12f); sSalto.icono.color = ci;
        }

        // ---- aviso
        float objAviso = Time.unscaledTime < avisoHasta ? 1f : 0f;
        grupoAviso.alpha = Mathf.MoveTowards(grupoAviso.alpha, objAviso, 3.5f * Time.unscaledDeltaTime);

        // ---- destello de daño (y alerta de poca vida)
        alphaDano = Mathf.MoveTowards(alphaDano, 0f, 1.5f * Time.unscaledDeltaTime);
        float baseAlerta = pvMostrada < 0.25f && !vida.EstaMuerto ? 0.18f + 0.1f * Mathf.Sin(Time.unscaledTime * 5f) : 0f;
        vinetaDano.color = new Color(0.9f, 0.05f, 0.1f, Mathf.Max(alphaDano, baseAlerta));

        // ---- resumen de victoria
        if (textoResumen != null && panelVictoria != null && panelVictoria.activeInHierarchy && inventario != null)
            textoResumen.text = "TÓTEMS  " + inventario.cantidadTotems + "      ALMAS  " + Mathf.RoundToInt(inventario.energiaEspiritual);
    }

    void ActualizarRecarga(Slot s, float progreso, float cooldownTotal)
    {
        bool listo = progreso >= 0.999f;
        s.recarga.fillAmount = listo ? 0f : 1f - progreso;
        s.tiempo.text = listo ? "" : (cooldownTotal * (1f - progreso)).ToString("0.0");
        Color col = s.icono.color; col.a = listo ? 1f : 0.45f; s.icono.color = col;
        if (listo && !s.listoAntes) s.destello = 1f;     // destello al quedar listo
        s.listoAntes = listo;
        s.destello = Mathf.MoveTowards(s.destello, 0f, 3f * Time.unscaledDeltaTime);
        s.raiz.localScale = Vector3.one * (1f + 0.12f * s.destello);
    }

    void ActualizarObjetivo()
    {
        if (textoObjetivo == null || gm == null) return;
        int req = gm.totemsRequeridos;
        int actual = inventario != null ? inventario.cantidadTotems : 0;
        if (actual < req && gm.estadoJefe != GameManager.EstadoJefe.Combate && gm.estadoJefe != GameManager.EstadoJefe.Derrotado)
            textoObjetivo.text = "REÚNE LOS TÓTEMS   <color=#F4C95D>" + actual + " / " + req + "</color>";
        else if (gm.jefeRequerido && gm.estadoJefe == GameManager.EstadoJefe.Combate)
            textoObjetivo.text = "<color=#E63946>¡VENCE A LA BUFA!</color>  Rompe el hechizo";
        else if (gm.jefeRequerido && gm.estadoJefe != GameManager.EstadoJefe.Derrotado)
            textoObjetivo.text = "ENTRA A LA <color=#F4C95D>CUEVA VIEJA</color>  y enfrenta a La Bufa";
        else
            textoObjetivo.text = "<color=#2EC4B6>¡PORTAL ABIERTO!</color>  Cruza el portal espiritual";
    }
}

/// <summary>Anima la aparición de un panel al activarlo (funciona con el juego en pausa).</summary>
public class PanelAparece : MonoBehaviour
{
    CanvasGroup cg;
    RectTransform tarjeta;
    float t;

    void OnEnable()
    {
        cg = GetComponent<CanvasGroup>();
        tarjeta = transform.Find("Tarjeta") as RectTransform;
        t = 0f;
        if (cg != null) cg.alpha = 0f;
    }

    void Update()
    {
        if (cg == null || t >= 1f) return;
        t = Mathf.Min(1f, t + Time.unscaledDeltaTime / 0.25f);
        float k = Mathf.SmoothStep(0f, 1f, t);
        cg.alpha = k;
        if (tarjeta != null) tarjeta.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, k);
    }
}
