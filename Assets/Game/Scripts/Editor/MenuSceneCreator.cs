using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class MenuSceneCreator
{
    [MenuItem("EvitandoElFin/1. Generar Escena MainMenu")]
    public static void CrearEscenaMenu()
    {
        // 1. Crear nueva escena vacía
        var nuevaEscena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // 2. Crear EventSystem si no existe
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // 3. Crear Canvas
        GameObject canvasGO = new GameObject("Canvas_MenuPrincipal");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // Manager de Menú
        MenuPrincipal menuManager = canvasGO.AddComponent<MenuPrincipal>();
        menuManager.nombreEscenaNivel1 = "SampleScene";

        // Fondo Oscuro / Místico
        GameObject bgGO = CrearUIObject("Fondo", canvasGO.transform);
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.05f, 0.07f, 0.12f, 0.95f);
        AlinearLlenar(bgGO.GetComponent<RectTransform>());

        // Panel Principal
        GameObject panelPrincipal = CrearUIObject("PanelPrincipal", canvasGO.transform);
        AlinearLlenar(panelPrincipal.GetComponent<RectTransform>());
        menuManager.panelPrincipal = panelPrincipal;

        // Título del Juego
        GameObject tituloGO = CrearUIObject("TituloJuego", panelPrincipal.transform);
        RectTransform rtTitulo = tituloGO.GetComponent<RectTransform>();
        rtTitulo.anchoredPosition = new Vector2(0, 240);
        rtTitulo.sizeDelta = new Vector2(900, 150);
        TextMeshProUGUI txtTitulo = tituloGO.AddComponent<TextMeshProUGUI>();
        txtTitulo.text = "EVITANDO EL FIN";
        txtTitulo.fontSize = 72;
        txtTitulo.alignment = TextAlignmentOptions.Center;
        txtTitulo.fontStyle = FontStyles.Bold;
        txtTitulo.color = new Color(0.35f, 0.95f, 0.85f);

        // Subtítulo
        GameObject subTituloGO = CrearUIObject("Subtitulo", panelPrincipal.transform);
        RectTransform rtSub = subTituloGO.GetComponent<RectTransform>();
        rtSub.anchoredPosition = new Vector2(0, 160);
        rtSub.sizeDelta = new Vector2(800, 60);
        TextMeshProUGUI txtSub = subTituloGO.AddComponent<TextMeshProUGUI>();
        txtSub.text = "Las Curvas Espirituales de Guanajuato";
        txtSub.fontSize = 26;
        txtSub.alignment = TextAlignmentOptions.Center;
        txtSub.color = new Color(0.8f, 0.85f, 0.9f, 0.8f);

        // Contenedor de Botones
        GameObject contenedorBotones = CrearUIObject("ContenedorBotones", panelPrincipal.transform);
        RectTransform rtCont = contenedorBotones.GetComponent<RectTransform>();
        rtCont.anchoredPosition = new Vector2(0, -60);
        rtCont.sizeDelta = new Vector2(400, 360);

        VerticalLayoutGroup vlg = contenedorBotones.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 20;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = false;

        // Botones
        Button btnJugar = CrearBoton("BtnJugar", contenedorBotones.transform, "INICIAR JUEGO", new Color(0.12f, 0.55f, 0.45f));
        btnJugar.onClick.AddListener(menuManager.JugarNivel1);

        Button btnControles = CrearBoton("BtnControles", contenedorBotones.transform, "CONTROLES", new Color(0.2f, 0.28f, 0.4f));
        btnControles.onClick.AddListener(menuManager.MostrarPanelControles);

        Button btnSalir = CrearBoton("BtnSalir", contenedorBotones.transform, "SALIR", new Color(0.45f, 0.15f, 0.15f));
        btnSalir.onClick.AddListener(menuManager.SalirDelJuego);

        // Panel de Controles
        GameObject panelControles = CrearUIObject("PanelControles", canvasGO.transform);
        AlinearLlenar(panelControles.GetComponent<RectTransform>());
        menuManager.panelControles = panelControles;
        panelControles.SetActive(false);

        GameObject bgControles = CrearUIObject("FondoControles", panelControles.transform);
        Image imgC = bgControles.AddComponent<Image>();
        imgC.color = new Color(0.02f, 0.04f, 0.08f, 0.98f);
        AlinearLlenar(bgControles.GetComponent<RectTransform>());

        GameObject txtCGO = CrearUIObject("TextoControles", panelControles.transform);
        RectTransform rtTC = txtCGO.GetComponent<RectTransform>();
        rtTC.anchoredPosition = new Vector2(0, 40);
        rtTC.sizeDelta = new Vector2(800, 400);
        TextMeshProUGUI txtC = txtCGO.AddComponent<TextMeshProUGUI>();
        txtC.text = "<b><size=44>GUÍA DE CONTROLES</size></b>\n\n" +
                   "<b>WASD / Flechas:</b> Mover al Chofer\n" +
                   "<b>Shift Izquierdo:</b> Correr\n" +
                   "<b>Espacio:</b> Esquivar (Dodge Roll)\n" +
                   "<b>Click Izquierdo:</b> Ataque Espiritual\n" +
                   "<b>Escape / P:</b> Menú de Pausa\n\n" +
                   "<b>Misión:</b> Reúne los 3 Tótems repartidos en el mapa y cruza el Portal.";
        txtC.fontSize = 24;
        txtC.alignment = TextAlignmentOptions.Center;
        txtC.color = Color.white;

        Button btnVolver = CrearBoton("BtnVolver", panelControles.transform, "VOLVER", new Color(0.2f, 0.28f, 0.4f));
        RectTransform rtVolver = btnVolver.GetComponent<RectTransform>();
        rtVolver.anchoredPosition = new Vector2(0, -260);
        btnVolver.onClick.AddListener(menuManager.MostrarPanelPrincipal);

        // Guardar escena en Assets/Scenes/MainMenu.unity
        string rutaEscena = "Assets/Scenes/MainMenu.unity";
        EditorSceneManager.SaveScene(nuevaEscena, rutaEscena);
        Debug.Log($"[MenuSceneCreator] ¡Escena '{rutaEscena}' generada y configurada exitosamente!");

        // Añadir a Build Settings si no está
        ActualizarBuildSettings(rutaEscena);
    }

    private static void ActualizarBuildSettings(string rutaMenu)
    {
        var scenes = EditorBuildSettings.scenes;
        bool menuExiste = false;

        foreach (var s in scenes)
        {
            if (s.path == rutaMenu) { menuExiste = true; break; }
        }

        if (!menuExiste)
        {
            EditorBuildSettingsScene[] newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
            newScenes[0] = new EditorBuildSettingsScene(rutaMenu, true);
            for (int i = 0; i < scenes.Length; i++)
            {
                newScenes[i + 1] = scenes[i];
            }
            EditorBuildSettings.scenes = newScenes;
            Debug.Log("[MenuSceneCreator] MainMenu añadido en el índice 0 de Build Settings.");
        }
    }

    private static GameObject CrearUIObject(string nombre, Transform padre)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private static void AlinearLlenar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static Button CrearBoton(string nombre, Transform padre, string texto, Color colorFondo)
    {
        GameObject btnGO = CrearUIObject(nombre, padre);
        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300, 65);

        Image img = btnGO.AddComponent<Image>();
        img.color = colorFondo;

        Button btn = btnGO.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = colorFondo * 1.25f;
        cb.pressedColor = colorFondo * 0.75f;
        btn.colors = cb;

        GameObject txtGO = CrearUIObject("Texto", btnGO.transform);
        AlinearLlenar(txtGO.GetComponent<RectTransform>());
        TextMeshProUGUI tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text = texto;
        tmp.fontSize = 24;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;

        return btn;
    }
}
