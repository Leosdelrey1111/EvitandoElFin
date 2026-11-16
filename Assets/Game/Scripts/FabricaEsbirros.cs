using UnityEngine;

/// <summary>
/// Construye en runtime los esbirros del Nivel 1 (Serpiente y Hombre de Piedra) con modelo procedural,
/// CharacterController, Poder Espiritual e IA. Sirve como "placeholder" jugable hasta tener los modelos de Blender.
/// </summary>
public static class FabricaEsbirros
{
    static Shader shaderLit;
    static readonly System.Collections.Generic.Dictionary<string, Material> cache = new System.Collections.Generic.Dictionary<string, Material>();

    public static EsbirroBaseIA Crear(EsbirroBaseIA.TipoEsbirro tipo, Vector3 posicion)
    {
        switch (tipo)
        {
            case EsbirroBaseIA.TipoEsbirro.HombrePiedra: return CrearHombrePiedra(posicion);
            case EsbirroBaseIA.TipoEsbirro.Comadre: return CrearComadre(posicion);
            default: return CrearSerpiente(posicion);
        }
    }

    // ------------------------------------------------------------------ SERPIENTE

    static EsbirroBaseIA CrearSerpiente(Vector3 pos)
    {
        GameObject root = new GameObject("Serpiente");
        root.transform.position = pos;
        root.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        Etiquetar(root);

        CharacterController cc = root.AddComponent<CharacterController>();
        cc.height = 0.9f; cc.radius = 0.4f; cc.center = new Vector3(0f, 0.45f, 0f);
        cc.stepOffset = 0.3f;

        Material piel = Mat("piel_serpiente", new Color(0.32f, 0.38f, 0.16f), 0.35f);
        Material vientre = Mat("vientre_serpiente", new Color(0.78f, 0.72f, 0.45f), 0.3f);
        Material ojo = MatEmisivo("ojo_serpiente", new Color(1f, 0.85f, 0.1f), new Color(2.2f, 1.6f, 0.1f));

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);

        // Cuerpo: esferas decrecientes formando una "S" detrás de la cabeza
        int segmentos = 8;
        for (int i = 0; i < segmentos; i++)
        {
            float k = i / (float)(segmentos - 1);
            float radio = Mathf.Lerp(0.30f, 0.09f, k);
            float z = -0.25f - i * 0.30f;
            float x = Mathf.Sin(i * 0.9f) * 0.28f;
            Parte(PrimitiveType.Sphere, "Seg" + i, visual.transform, new Vector3(x, radio, z), Vector3.one * radio * 2f, i % 2 == 0 ? piel : vientre);
        }
        // Cabeza
        GameObject cabeza = Parte(PrimitiveType.Sphere, "Cabeza", visual.transform, new Vector3(0f, 0.38f, 0.12f), new Vector3(0.52f, 0.34f, 0.62f), piel);
        Parte(PrimitiveType.Sphere, "OjoI", cabeza.transform, new Vector3(-0.38f, 0.25f, 0.46f), Vector3.one * 0.3f, ojo);
        Parte(PrimitiveType.Sphere, "OjoD", cabeza.transform, new Vector3(0.38f, 0.25f, 0.46f), Vector3.one * 0.3f, ojo);

        PoderEspiritual vida = root.AddComponent<PoderEspiritual>();
        vida.maxPoder = 30f; vida.actualPoder = 30f;

        EsbirroBaseIA ia = root.AddComponent<EsbirroBaseIA>();
        ia.tipo = EsbirroBaseIA.TipoEsbirro.Serpiente;
        ia.velocidadPersecucion = 5.2f;
        ia.velocidadPatrulla = 1.6f;
        ia.rangoDeteccion = 11f;
        ia.rangoAtaque = 1.5f;
        ia.danio = 10f;
        ia.cooldownAtaque = 1.2f;
        ia.tiempoTelegrafiado = 0.4f;
        ia.cantidadAlmasADropear = 2;
        ia.alturaBarraVida = 1.1f;
        return ia;
    }

    // ------------------------------------------------------------------ HOMBRE DE PIEDRA

    static EsbirroBaseIA CrearHombrePiedra(Vector3 pos)
    {
        GameObject root = new GameObject("HombreDePiedra");
        root.transform.position = pos;
        root.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        Etiquetar(root);

        CharacterController cc = root.AddComponent<CharacterController>();
        cc.height = 2.5f; cc.radius = 0.75f; cc.center = new Vector3(0f, 1.25f, 0f);
        cc.stepOffset = 0.4f;

        Material roca = Mat("roca_piedra", new Color(0.36f, 0.35f, 0.34f), 0.15f);
        Material rocaClara = Mat("roca_piedra_clara", new Color(0.46f, 0.44f, 0.40f), 0.15f);
        Material musgo = Mat("musgo_piedra", new Color(0.24f, 0.36f, 0.2f), 0.1f);
        Material ojo = MatEmisivo("ojo_piedra", new Color(0.3f, 0.95f, 1f), new Color(0.5f, 2.4f, 2.6f));

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);

        Parte(PrimitiveType.Cube, "Torso", visual.transform, new Vector3(0f, 1.55f, 0f), new Vector3(1.35f, 1.35f, 0.85f), roca);
        Parte(PrimitiveType.Cube, "Pecho", visual.transform, new Vector3(0.1f, 1.8f, 0.3f), new Vector3(0.8f, 0.6f, 0.4f), rocaClara);
        Parte(PrimitiveType.Cube, "Hombro", visual.transform, new Vector3(-0.45f, 2.05f, -0.1f), new Vector3(0.5f, 0.35f, 0.5f), musgo);
        GameObject cabeza = Parte(PrimitiveType.Cube, "Cabeza", visual.transform, new Vector3(0f, 2.45f, 0.05f), new Vector3(0.7f, 0.65f, 0.65f), rocaClara);
        Parte(PrimitiveType.Cube, "OjoI", cabeza.transform, new Vector3(-0.22f, 0.1f, 0.52f), new Vector3(0.22f, 0.1f, 0.1f), ojo);
        Parte(PrimitiveType.Cube, "OjoD", cabeza.transform, new Vector3(0.22f, 0.1f, 0.52f), new Vector3(0.22f, 0.1f, 0.1f), ojo);
        // Brazos grandes
        Parte(PrimitiveType.Cube, "BrazoI", visual.transform, new Vector3(-1.0f, 1.45f, 0.1f), new Vector3(0.55f, 1.5f, 0.6f), roca);
        Parte(PrimitiveType.Cube, "BrazoD", visual.transform, new Vector3(1.0f, 1.45f, 0.1f), new Vector3(0.55f, 1.5f, 0.6f), roca);
        Parte(PrimitiveType.Cube, "PuñoI", visual.transform, new Vector3(-1.0f, 0.65f, 0.15f), new Vector3(0.7f, 0.6f, 0.7f), rocaClara);
        Parte(PrimitiveType.Cube, "PuñoD", visual.transform, new Vector3(1.0f, 0.65f, 0.15f), new Vector3(0.7f, 0.6f, 0.7f), rocaClara);
        // Piernas
        Parte(PrimitiveType.Cube, "PiernaI", visual.transform, new Vector3(-0.4f, 0.45f, 0f), new Vector3(0.55f, 0.9f, 0.6f), roca);
        Parte(PrimitiveType.Cube, "PiernaD", visual.transform, new Vector3(0.4f, 0.45f, 0f), new Vector3(0.55f, 0.9f, 0.6f), roca);

        // Luz tenue en los ojos
        GameObject luz = new GameObject("LuzOjos");
        luz.transform.SetParent(cabeza.transform, false);
        luz.transform.localPosition = new Vector3(0f, 0.1f, 0.8f);
        Light l = luz.AddComponent<Light>();
        l.type = LightType.Point; l.color = new Color(0.3f, 0.95f, 1f); l.range = 3.5f; l.intensity = 0.9f; l.shadows = LightShadows.None;

        PoderEspiritual vida = root.AddComponent<PoderEspiritual>();
        vida.maxPoder = 120f; vida.actualPoder = 120f;

        EsbirroBaseIA ia = root.AddComponent<EsbirroBaseIA>();
        ia.tipo = EsbirroBaseIA.TipoEsbirro.HombrePiedra;
        ia.velocidadPersecucion = 2.3f;
        ia.velocidadPatrulla = 1.0f;
        ia.rangoDeteccion = 9f;
        ia.rangoAtaque = 2.1f;
        ia.danio = 22f;
        ia.cooldownAtaque = 2.4f;
        ia.tiempoTelegrafiado = 0.85f;
        ia.resistenciaEmpuje = 2.8f;
        ia.cantidadAlmasADropear = 5;
        ia.alturaBarraVida = 3.2f;
        return ia;
    }

    // ------------------------------------------------------------------ COMADRE (Las Dos Comadres, hechas piedra por chismosas)

    static EsbirroBaseIA CrearComadre(Vector3 pos)
    {
        GameObject root = new GameObject("Comadre");
        root.transform.position = pos;
        root.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        Etiquetar(root);

        CharacterController cc = root.AddComponent<CharacterController>();
        cc.height = 1.7f; cc.radius = 0.5f; cc.center = new Vector3(0f, 0.85f, 0f);
        cc.stepOffset = 0.35f;

        Material roca = Mat("roca_comadre", new Color(0.42f, 0.40f, 0.43f), 0.15f);
        Material rebozo = Mat("rebozo_comadre", new Color(0.55f, 0.18f, 0.36f), 0.1f);
        Material cabello = Mat("cabello_comadre", new Color(0.62f, 0.62f, 0.66f), 0.1f);
        Material boca = MatEmisivo("boca_comadre", new Color(0.9f, 0.4f, 1f), new Color(2.2f, 0.8f, 2.6f));

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);

        // falda larga, torso encorvado, rebozo, cabeza y rodete
        Parte(PrimitiveType.Cylinder, "Falda", visual.transform, new Vector3(0f, 0.55f, 0f), new Vector3(1.0f, 0.55f, 1.0f), roca);
        Parte(PrimitiveType.Sphere, "Torso", visual.transform, new Vector3(0f, 1.2f, 0.08f), new Vector3(0.82f, 0.95f, 0.7f), roca);
        Parte(PrimitiveType.Sphere, "Rebozo", visual.transform, new Vector3(0f, 1.38f, 0.0f), new Vector3(1.0f, 0.55f, 0.85f), rebozo);
        GameObject cabeza = Parte(PrimitiveType.Sphere, "Cabeza", visual.transform, new Vector3(0f, 1.78f, 0.2f), Vector3.one * 0.46f, roca);
        Parte(PrimitiveType.Sphere, "Rodete", cabeza.transform, new Vector3(0f, 0.55f, -0.55f), Vector3.one * 0.62f, cabello);
        Parte(PrimitiveType.Sphere, "Boca", cabeza.transform, new Vector3(0f, -0.25f, 0.46f), new Vector3(0.45f, 0.14f, 0.2f), boca);
        Parte(PrimitiveType.Sphere, "OjoI", cabeza.transform, new Vector3(-0.28f, 0.12f, 0.42f), new Vector3(0.16f, 0.1f, 0.12f), boca);
        Parte(PrimitiveType.Sphere, "OjoD", cabeza.transform, new Vector3(0.28f, 0.12f, 0.42f), new Vector3(0.16f, 0.1f, 0.12f), boca);
        // brazos delgados con manos junto a la boca (cuchicheando)
        Parte(PrimitiveType.Capsule, "BrazoI", visual.transform, new Vector3(-0.5f, 1.35f, 0.25f), new Vector3(0.18f, 0.4f, 0.18f), roca).transform.localRotation = Quaternion.Euler(40f, 0f, -20f);
        Parte(PrimitiveType.Capsule, "BrazoD", visual.transform, new Vector3(0.5f, 1.35f, 0.25f), new Vector3(0.18f, 0.4f, 0.18f), roca).transform.localRotation = Quaternion.Euler(40f, 0f, 20f);

        GameObject luz = new GameObject("LuzMurmullo");
        luz.transform.SetParent(cabeza.transform, false);
        luz.transform.localPosition = new Vector3(0f, -0.2f, 0.9f);
        Light l = luz.AddComponent<Light>();
        l.type = LightType.Point; l.color = new Color(0.9f, 0.4f, 1f); l.range = 4f; l.intensity = 0.9f; l.shadows = LightShadows.None;

        PoderEspiritual vida = root.AddComponent<PoderEspiritual>();
        vida.maxPoder = 45f; vida.actualPoder = 45f;

        EsbirroBaseIA ia = root.AddComponent<EsbirroBaseIA>();
        ia.tipo = EsbirroBaseIA.TipoEsbirro.Comadre;
        ia.velocidadPersecucion = 2.8f;
        ia.velocidadPatrulla = 1.0f;
        ia.rangoDeteccion = 17f;
        ia.rangoAtaque = 10f;
        ia.danio = 12f;
        ia.cooldownAtaque = 2.4f;
        ia.tiempoTelegrafiado = 0.75f;
        ia.ataqueADistancia = true;
        ia.velocidadProyectil = 11f;
        ia.colorProyectil = new Color(0.9f, 0.4f, 1f);
        ia.cantidadAlmasADropear = 4;
        ia.alturaBarraVida = 2.4f;
        return ia;
    }

    // ------------------------------------------------------------------ utilidades

    static void Etiquetar(GameObject go)
    {
        try { go.tag = "Enemigo"; }
        catch (UnityException) { Debug.LogWarning("[FabricaEsbirros] Falta el tag 'Enemigo' en Project Settings > Tags. Los golpes no registrarán."); }
    }

    static GameObject Parte(PrimitiveType tipo, string nombre, Transform padre, Vector3 pos, Vector3 escala, Material mat)
    {
        GameObject g = GameObject.CreatePrimitive(tipo);
        g.name = nombre;
        g.transform.SetParent(padre, false);
        g.transform.localPosition = pos;
        g.transform.localScale = escala;
        Collider c = g.GetComponent<Collider>();
        if (c != null) { if (Application.isPlaying) Object.Destroy(c); else Object.DestroyImmediate(c); }
        g.GetComponent<Renderer>().sharedMaterial = mat;
        return g;
    }

    static Shader Shader_()
    {
        if (shaderLit == null)
        {
            shaderLit = Shader.Find("Universal Render Pipeline/Lit");
            if (shaderLit == null) shaderLit = Shader.Find("Standard");
        }
        return shaderLit;
    }

    static Material Mat(string clave, Color c, float suavidad)
    {
        Material m;
        if (cache.TryGetValue(clave, out m) && m != null) return m;
        m = new Material(Shader_());
        m.name = clave;
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", suavidad);
        cache[clave] = m;
        return m;
    }

    static Material MatEmisivo(string clave, Color c, Color emision)
    {
        Material m = Mat(clave, c, 0.6f);
        m.EnableKeyword("_EMISSION");
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", emision);
        return m;
    }
}
