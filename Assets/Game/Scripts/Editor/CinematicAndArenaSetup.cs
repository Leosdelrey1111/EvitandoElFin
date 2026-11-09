using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Monta en la escena de la carretera (SampleScene) el camión de carga, la ambulancia de rescate y el manager de la cinemática.
/// El Nivel 1 vive en su propia escena Nivel1_Limbo (menú 6).
/// </summary>
public static class CinematicAndArenaSetup
{
    [MenuItem("EvitandoElFin/5. Montar Cinemática del Accidente (escena de la carretera)")]
    public static void MontarCinematica()
    {
        // 1. Limpiar objetos de versiones anteriores o UIs residuales
        BorrarSiExiste("Camion_Cinematica");
        BorrarSiExiste("Ambulancia_Rescate");
        BorrarSiExiste("ManagerCinematica");
        BorrarSiExiste("Arena_Espiritual_Nivel1");
        BorrarSiExiste("Canvas_Gameplay");
        BorrarSiExiste("TextoNarrativo");
        BorrarSiExiste("PanelFadeNegro");
        BorrarSiExiste("HUD_Espiritual");
        BorrarSiExiste("ManagerUI_Gameplay");

        // 2. Normalizar la carretera (GeneradorCurvasGuanajuato) en el origen del mundo
        var gen = Object.FindAnyObjectByType<GeneradorCurvasGuanajuato>();
        if (gen != null)
        {
            gen.transform.position = Vector3.zero;
            gen.transform.rotation = Quaternion.identity;
            gen.transform.localScale = Vector3.one;
            Debug.Log("[Cinemática] Carretera normalizada en el origen (0, 0, 0).");
        }

        // 3. Crear vehículos
        GameObject camion = CrearCamion();
        Light azul, roja;
        GameObject ambulancia = CrearAmbulancia(out azul, out roja);

        // 4. Posicionar el camión al inicio de la carretera en el carril derecho (+3.0m)
        if (gen != null && gen.puntosDeControl != null && gen.puntosDeControl.Count >= 4)
        {
            Vector3 posInicio, fwdInicio, upInicio, rightInicio;
            Quaternion rotInicio;
            if (gen.EvaluarRutaCompleta(0f, 3.0f, out posInicio, out rotInicio, out fwdInicio, out upInicio, out rightInicio))
            {
                camion.transform.SetPositionAndRotation(posInicio + upInicio * 0.12f, rotInicio);
            }
        }

        // 5. Configurar el Manager de la Cinemática
        GameObject manager = new GameObject("ManagerCinematica");
        CinematicaAccidenteCamion cin = manager.AddComponent<CinematicaAccidenteCamion>();
        cin.camionTransform = camion.transform;
        cin.ambulanciaObjeto = ambulancia;
        cin.sirenaAzul = azul;
        cin.sirenaRoja = roja;
        cin.generadorCarretera = gen;
        cin.escenaNivel1 = "Nivel1_Limbo";

        var chofer = Object.FindAnyObjectByType<ChoferController>(FindObjectsInactive.Include);
        if (chofer != null) cin.choferJugador = chofer.gameObject;

        Camera cam = Camera.main;
        if (cam != null)
        {
            cin.camaraPrincipal = cam;
            cin.camaraJugadorScript = cam.GetComponent<CamaraTerceraPersona>();
        }

        bool nivelExiste = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Nivel1_Limbo.unity") != null;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("<color=#00FFAA>[Cinemática] ¡Camión, ambulancia y manager montados con éxito!</color>");
        EditorUtility.DisplayDialog("Evitando El Fin",
            "Cinemática montada con éxito en esta escena.\n\n" +
            (nivelExiste ? "Nivel1_Limbo ya existe en el proyecto. ¡Dale Play a la escena para disfrutar la cinemática!"
                         : "Aviso: Ejecuta 'EvitandoElFin > 6. Crear Escena Nivel1_Limbo' para que la transición funcione al terminar la cinemática."),
            "Entendido");
    }

    static void BorrarSiExiste(string nombre)
    {
        GameObject go = GameObject.Find(nombre);
        if (go != null) Undo.DestroyObjectImmediate(go);
    }

    static Material Mat(Color c, float metal = 0f, float suavidad = 0.4f)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        Material m = new Material(sh);
        m.color = c;
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", suavidad);
        return m;
    }

    static GameObject Parte(string nombre, PrimitiveType tipo, Transform padre, Vector3 pos, Vector3 escala, Material mat, Vector3? rot = null)
    {
        GameObject g = GameObject.CreatePrimitive(tipo);
        g.name = nombre;
        g.transform.SetParent(padre, false);
        g.transform.localPosition = pos;
        g.transform.localScale = escala;
        if (rot.HasValue) g.transform.localRotation = Quaternion.Euler(rot.Value);
        g.GetComponent<MeshRenderer>().sharedMaterial = mat;

        // Quitar colliders para evitar que interfieran con raycasts del asfalto o cinemática
        Collider col = g.GetComponent<Collider>();
        if (col != null) Object.DestroyImmediate(col);

        return g;
    }

    static void Rueda(Transform padre, string nombre, Vector3 pos, Material llanta)
    {
        Parte(nombre, PrimitiveType.Cylinder, padre, pos, new Vector3(1.05f, 0.18f, 1.05f), llanta, new Vector3(0f, 0f, 90f));
    }

    // ------------------------------------------------------------------ CAMIÓN DE CARGA (PROPS)

    static GameObject CrearCamion()
    {
        GameObject root = new GameObject("Camion_Cinematica");

        Material rojo = Mat(new Color(0.62f, 0.09f, 0.09f), 0.3f, 0.65f);
        Material blanco = Mat(new Color(0.88f, 0.88f, 0.86f), 0.05f, 0.45f);
        Material negro = Mat(new Color(0.05f, 0.05f, 0.06f), 0f, 0.2f);
        Material vidrio = Mat(new Color(0.08f, 0.12f, 0.16f), 0.6f, 0.9f);
        Material luz = Mat(new Color(1f, 0.95f, 0.75f), 0f, 0.9f);
        if (luz.HasProperty("_EmissionColor"))
        {
            luz.EnableKeyword("_EMISSION");
            luz.SetColor("_EmissionColor", new Color(2f, 1.8f, 1.2f));
        }

        // Chasis, cabina, cofre, parabrisas, parrilla
        Parte("Chasis", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.55f, -0.8f), new Vector3(2.5f, 0.35f, 9.5f), negro);
        Parte("Cabina", PrimitiveType.Cube, root.transform, new Vector3(0f, 1.9f, 2.6f), new Vector3(2.5f, 2.3f, 2.2f), rojo);
        Parte("Cofre", PrimitiveType.Cube, root.transform, new Vector3(0f, 1.15f, 3.9f), new Vector3(2.4f, 1.0f, 1.3f), rojo);
        Parte("Parabrisas", PrimitiveType.Cube, root.transform, new Vector3(0f, 2.35f, 3.72f), new Vector3(2.25f, 1.05f, 0.08f), vidrio, new Vector3(-12f, 0f, 0f));
        Parte("Defensa", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.55f, 4.6f), new Vector3(2.6f, 0.35f, 0.25f), negro);
        Parte("Parrilla", PrimitiveType.Cube, root.transform, new Vector3(0f, 1.05f, 4.57f), new Vector3(1.7f, 0.6f, 0.06f), negro);

        // Caja de carga
        Parte("CajaCarga", PrimitiveType.Cube, root.transform, new Vector3(0f, 2.25f, -2.4f), new Vector3(2.6f, 3.0f, 6.4f), blanco);
        Parte("FranjaCaja", PrimitiveType.Cube, root.transform, new Vector3(0f, 1.35f, -2.4f), new Vector3(2.64f, 0.35f, 6.45f), rojo);

        // Ruedas
        float[] zs = { 3.5f, -1.3f, -3.6f };
        foreach (float z in zs)
        {
            Rueda(root.transform, "Rueda_I_" + z, new Vector3(-1.35f, 0.52f, z), negro);
            Rueda(root.transform, "Rueda_D_" + z, new Vector3(1.35f, 0.52f, z), negro);
        }

        // Calaveras traseras (luces de frenos)
        Material matCalavera = Mat(new Color(0.85f, 0.05f, 0.05f), 0f, 0.9f);
        if (matCalavera.HasProperty("_EmissionColor"))
        {
            matCalavera.EnableKeyword("_EMISSION");
            matCalavera.SetColor("_EmissionColor", new Color(1.8f, 0.1f, 0.1f));
        }
        Parte("Calavera_I", PrimitiveType.Cube, root.transform, new Vector3(-1.15f, 0.75f, -5.62f), new Vector3(0.32f, 0.28f, 0.08f), matCalavera);
        Parte("Calavera_D", PrimitiveType.Cube, root.transform, new Vector3(1.15f, 0.75f, -5.62f), new Vector3(0.32f, 0.28f, 0.08f), matCalavera);

        // Faros y luz delantera
        Parte("Faro_I", PrimitiveType.Sphere, root.transform, new Vector3(-0.85f, 1.1f, 4.62f), new Vector3(0.35f, 0.3f, 0.2f), luz);
        Parte("Faro_D", PrimitiveType.Sphere, root.transform, new Vector3(0.85f, 1.1f, 4.62f), new Vector3(0.35f, 0.3f, 0.2f), luz);

        GameObject luzFaros = new GameObject("LuzFaros");
        luzFaros.transform.SetParent(root.transform, false);
        luzFaros.transform.localPosition = new Vector3(0f, 1.1f, 4.7f);
        Light l = luzFaros.AddComponent<Light>();
        l.type = LightType.Spot;
        l.spotAngle = 70f;
        l.range = 65f;
        l.intensity = 7f;
        l.color = new Color(1f, 0.94f, 0.78f);

        return root;
    }

    // ------------------------------------------------------------------ AMBULANCIA (PROPS)

    static GameObject CrearAmbulancia(out Light azul, out Light roja)
    {
        GameObject root = new GameObject("Ambulancia_Rescate");

        Material blanco = Mat(new Color(0.94f, 0.94f, 0.95f), 0.1f, 0.6f);
        Material rojo = Mat(new Color(0.8f, 0.06f, 0.06f), 0f, 0.5f);
        Material negro = Mat(new Color(0.05f, 0.05f, 0.06f), 0f, 0.2f);
        Material vidrio = Mat(new Color(0.1f, 0.15f, 0.2f), 0.6f, 0.9f);
        Material matAzul = Mat(new Color(0.1f, 0.25f, 1f), 0f, 0.8f);
        Material matRoja = Mat(new Color(1f, 0.1f, 0.1f), 0f, 0.8f);

        Parte("Caja", PrimitiveType.Cube, root.transform, new Vector3(0f, 1.6f, -0.6f), new Vector3(2.4f, 2.5f, 4.2f), blanco);
        Parte("Cabina", PrimitiveType.Cube, root.transform, new Vector3(0f, 1.25f, 2.2f), new Vector3(2.4f, 1.8f, 1.8f), blanco);
        Parte("Parabrisas", PrimitiveType.Cube, root.transform, new Vector3(0f, 1.6f, 3.05f), new Vector3(2.15f, 0.85f, 0.07f), vidrio, new Vector3(-14f, 0f, 0f));
        Parte("Chasis", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.5f, 0.3f), new Vector3(2.35f, 0.3f, 6.4f), negro);

        // Franjas y cruz roja
        Parte("Franja", PrimitiveType.Cube, root.transform, new Vector3(0f, 1.25f, -0.6f), new Vector3(2.44f, 0.3f, 4.25f), rojo);
        foreach (float x in new[] { -1.21f, 1.21f })
        {
            Parte("CruzH", PrimitiveType.Cube, root.transform, new Vector3(x, 2.1f, -0.6f), new Vector3(0.04f, 0.2f, 0.8f), rojo);
            Parte("CruzV", PrimitiveType.Cube, root.transform, new Vector3(x, 2.1f, -0.6f), new Vector3(0.04f, 0.8f, 0.2f), rojo);
        }

        // Ruedas
        foreach (float z in new[] { 2.2f, -1.6f })
        {
            Rueda(root.transform, "Rueda_I_" + z, new Vector3(-1.2f, 0.45f, z), negro);
            Rueda(root.transform, "Rueda_D_" + z, new Vector3(1.2f, 0.45f, z), negro);
        }

        // Sirenas luminosas en el techo
        Parte("SirenaAzulVisual", PrimitiveType.Cube, root.transform, new Vector3(-0.5f, 2.95f, 1.6f), new Vector3(0.7f, 0.18f, 0.25f), matAzul);
        Parte("SirenaRojaVisual", PrimitiveType.Cube, root.transform, new Vector3(0.5f, 2.95f, 1.6f), new Vector3(0.7f, 0.18f, 0.25f), matRoja);

        GameObject sA = new GameObject("SirenaAzul");
        sA.transform.SetParent(root.transform, false);
        sA.transform.localPosition = new Vector3(-0.5f, 3.2f, 1.6f);
        azul = sA.AddComponent<Light>();
        azul.type = LightType.Point;
        azul.color = new Color(0.1f, 0.35f, 1f);
        azul.range = 30f;
        azul.intensity = 10f;

        GameObject sR = new GameObject("SirenaRoja");
        sR.transform.SetParent(root.transform, false);
        sR.transform.localPosition = new Vector3(0.5f, 3.2f, 1.6f);
        roja = sR.AddComponent<Light>();
        roja.type = LightType.Point;
        roja.color = new Color(1f, 0.1f, 0.1f);
        roja.range = 30f;
        roja.intensity = 10f;

        root.SetActive(false);
        return root;
    }
}
