using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public static class LevelUIBuilder
{
    [MenuItem("EvitandoElFin/2. Montar GameManager y Paneles (Pausa, Derrota, Victoria)")]
    public static void MontarManagersYPaneles()
    {
        MontarManagersYPaneles(true);
    }

    public static void MontarManagersYPaneles(bool crearPortalDePrueba)
    {
        // 1. Asegurar GameManager en escena
        GameObject gmGO = GameObject.Find("GameManager");
        if (gmGO == null)
        {
            gmGO = new GameObject("GameManager");
        }
        GameManager gm = gmGO.GetComponent<GameManager>();
        if (gm == null) gm = gmGO.AddComponent<GameManager>();

        gm.nombreMenuPrincipal = "MainMenu";
        gm.totemsRequeridos = 3;

        // 2. Buscar Canvas existente o crear uno
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        GameObject canvasGO;
        if (canvas == null)
        {
            canvasGO = new GameObject("Canvas_Gameplay");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler cs = canvasGO.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();
        }
        else
        {
            canvasGO = canvas.gameObject;
            if (canvasGO.GetComponent<GraphicRaycaster>() == null)
            {
                canvasGO.AddComponent<GraphicRaycaster>();
            }
        }

        // Asegurar EventSystem en la escena (crucial para poder hacer clic en botones)
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("[LevelUIBuilder] EventSystem creado para permitir interacción con la UI.");
        }

        // 3. Crear Panel de Pausa
        Transform pPausa = canvasGO.transform.Find("PanelPausa");
        if (pPausa == null)
        {
            GameObject panelPausaGO = CrearPanelBase("PanelPausa", canvasGO.transform, new Color(0, 0, 0, 0.85f));
            CrearTexto("TituloPausa", panelPausaGO.transform, "JUEGO PAUSADO", 48, new Vector2(0, 150));

            Button btnReanudar = CrearBoton("BtnReanudar", panelPausaGO.transform, "REANUDAR", new Vector2(0, 30), new Color(0.15f, 0.5f, 0.4f));
            btnReanudar.onClick.AddListener(gm.ReanudarJuego);

            Button btnReiniciar = CrearBoton("BtnReiniciar", panelPausaGO.transform, "REINICIAR NIVEL", new Vector2(0, -50), new Color(0.2f, 0.3f, 0.45f));
            btnReiniciar.onClick.AddListener(gm.ReiniciarNivel);

            Button btnMenu = CrearBoton("BtnMenu", panelPausaGO.transform, "MENÚ PRINCIPAL", new Vector2(0, -130), new Color(0.5f, 0.2f, 0.2f));
            btnMenu.onClick.AddListener(gm.IrAlMenuPrincipal);

            panelPausaGO.SetActive(false);
            gm.panelPausa = panelPausaGO;
        }
        else
        {
            gm.panelPausa = pPausa.gameObject;
        }

        // 4. Crear Panel de Game Over (Derrota)
        Transform pDerrota = canvasGO.transform.Find("PanelGameOver");
        if (pDerrota == null)
        {
            GameObject panelGameOverGO = CrearPanelBase("PanelGameOver", canvasGO.transform, new Color(0.15f, 0.02f, 0.02f, 0.92f));
            CrearTexto("TituloGameOver", panelGameOverGO.transform, "HAS CAÍDO EN LAS CURVAS", 52, new Vector2(0, 150), new Color(1f, 0.3f, 0.3f));
            CrearTexto("SubGameOver", panelGameOverGO.transform, "Tu alma no resistió el viaje espiritual.", 22, new Vector2(0, 80));

            Button btnReintentar = CrearBoton("BtnReintentar", panelGameOverGO.transform, "REINTENTAR", new Vector2(0, -20), new Color(0.7f, 0.2f, 0.2f));
            btnReintentar.onClick.AddListener(gm.ReiniciarNivel);

            Button btnMenu = CrearBoton("BtnMenu", panelGameOverGO.transform, "MENÚ PRINCIPAL", new Vector2(0, -100), new Color(0.2f, 0.2f, 0.25f));
            btnMenu.onClick.AddListener(gm.IrAlMenuPrincipal);

            panelGameOverGO.SetActive(false);
            gm.panelGameOver = panelGameOverGO;
        }
        else
        {
            gm.panelGameOver = pDerrota.gameObject;
        }

        // 5. Crear Panel de Victoria
        Transform pVictoria = canvasGO.transform.Find("PanelVictoria");
        if (pVictoria == null)
        {
            GameObject panelVicGO = CrearPanelBase("PanelVictoria", canvasGO.transform, new Color(0.02f, 0.12f, 0.10f, 0.94f));
            CrearTexto("TituloVictoria", panelVicGO.transform, "¡NIVEL 1 COMPLETADO!", 56, new Vector2(0, 160), new Color(0.3f, 1f, 0.7f));
            CrearTexto("SubVictoria", panelVicGO.transform, "Has recolectado los tótems y cruzado el portal con éxito.", 22, new Vector2(0, 90));

            Button btnReiniciar = CrearBoton("BtnRejugar", panelVicGO.transform, "JUGAR DE NUEVO", new Vector2(0, -20), new Color(0.15f, 0.6f, 0.45f));
            btnReiniciar.onClick.AddListener(gm.ReiniciarNivel);

            Button btnMenu = CrearBoton("BtnMenu", panelVicGO.transform, "MENÚ PRINCIPAL", new Vector2(0, -100), new Color(0.2f, 0.25f, 0.35f));
            btnMenu.onClick.AddListener(gm.IrAlMenuPrincipal);

            panelVicGO.SetActive(false);
            gm.panelVictoria = panelVicGO;
        }
        else
        {
            gm.panelVictoria = pVictoria.gameObject;
        }

        // 6. Configurar Portal en escena si no existe
        PortalVictoria portal = Object.FindAnyObjectByType<PortalVictoria>();
        if (portal == null && crearPortalDePrueba)
        {
            GameObject portalGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            portalGO.name = "Portal_Victoria_Nivel1";
            portalGO.transform.position = new Vector3(0, 1.5f, 25f);
            portalGO.transform.localScale = new Vector3(3f, 3f, 3f);

            var col = portalGO.GetComponent<Collider>();
            col.isTrigger = true;

            portal = portalGO.AddComponent<PortalVictoria>();

            // Material propio (NO usar sharedMaterial.color: contamina el material compartido de todos los primitivos)
            var renderer = portalGO.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Material m = new Material(renderer.sharedMaterial);
                m.color = new Color(0.1f, 0.9f, 0.7f, 0.5f);
                renderer.sharedMaterial = m;
            }
            Debug.Log("[LevelUIBuilder] Portal de Victoria creado en posición (0, 1.5, 25). Puedes reubicarlo donde termine tu nivel.");
        }

        EditorUtility.SetDirty(gmGO);
        Debug.Log("[LevelUIBuilder] ¡GameManager, Portal y Paneles de Victoria/Derrota/Pausa integrados exitosamente!");
    }

    private static GameObject CrearPanelBase(string nombre, Transform padre, Color colorFondo)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = go.AddComponent<Image>();
        img.color = colorFondo;

        return go;
    }

    private static TextMeshProUGUI CrearTexto(string nombre, Transform padre, string texto, float tamano, Vector2 pos, Color? color = null)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(800, 100);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = texto;
        tmp.fontSize = tamano;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = color ?? Color.white;

        return tmp;
    }

    private static Button CrearBoton(string nombre, Transform padre, string texto, Vector2 pos, Color colorFondo)
    {
        GameObject btnGO = new GameObject(nombre);
        btnGO.transform.SetParent(padre, false);
        RectTransform rt = btnGO.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(320, 60);

        Image img = btnGO.AddComponent<Image>();
        img.color = colorFondo;

        Button btn = btnGO.AddComponent<Button>();

        GameObject txtGO = new GameObject("Texto");
        txtGO.transform.SetParent(btnGO.transform, false);
        RectTransform rtTxt = txtGO.AddComponent<RectTransform>();
        rtTxt.anchorMin = Vector2.zero;
        rtTxt.anchorMax = Vector2.one;
        rtTxt.offsetMin = Vector2.zero;
        rtTxt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text = texto;
        tmp.fontSize = 22;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.raycastTarget = false; // Permitir que el clic pase directamente al botón

        return btn;
    }
}
