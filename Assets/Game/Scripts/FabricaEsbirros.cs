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
            case EsbirroBaseIA.TipoEsbirro.Momia: return CrearMomia(posicion);
            case EsbirroBaseIA.TipoEsbirro.Catrin: return CrearCatrin(posicion);
            case EsbirroBaseIA.TipoEsbirro.Cuervo: return CrearCuervo(posicion);
            case EsbirroBaseIA.TipoEsbirro.Monje: return CrearMonje(posicion);
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

    // ------------------------------------------------------------------ MOMIA DE SANTA PAULA (Nivel 2)

    static EsbirroBaseIA CrearMomia(Vector3 pos)
    {
        GameObject root = new GameObject("Momia");
        root.transform.position = pos;
        root.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        Etiquetar(root);

        CharacterController cc = root.AddComponent<CharacterController>();
        cc.height = 1.8f; cc.radius = 0.45f; cc.center = new Vector3(0f, 0.9f, 0f);

        Material pergamino = Mat("piel_momia", new Color(0.58f, 0.50f, 0.38f), 0.2f);
        Material venda = Mat("venda_momia", new Color(0.72f, 0.68f, 0.58f), 0.15f);
        Material ojoEspectral = MatEmisivo("ojo_momia", new Color(0.1f, 0.8f, 0.4f), new Color(0.4f, 2.2f, 0.9f));

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);

        // Tronco encorvado
        GameObject torso = Parte(PrimitiveType.Cube, "Torso", visual.transform, new Vector3(0f, 1.05f, 0f), new Vector3(0.55f, 0.7f, 0.32f), venda);
        torso.transform.localRotation = Quaternion.Euler(8f, 0f, 0f);

        // Cabeza con mandíbula abierta típica de las momias de Guanajuato
        GameObject cabeza = Parte(PrimitiveType.Cube, "Cabeza", visual.transform, new Vector3(0f, 1.55f, 0.08f), new Vector3(0.32f, 0.38f, 0.34f), pergamino);
        Parte(PrimitiveType.Cube, "Mandibula", cabeza.transform, new Vector3(0f, -0.4f, 0.12f), new Vector3(0.7f, 0.35f, 0.6f), pergamino);
        Parte(PrimitiveType.Sphere, "OjoI", cabeza.transform, new Vector3(-0.25f, 0.15f, 0.46f), Vector3.one * 0.22f, ojoEspectral);
        Parte(PrimitiveType.Sphere, "OjoD", cabeza.transform, new Vector3(0.25f, 0.15f, 0.46f), Vector3.one * 0.22f, ojoEspectral);

        // Brazos estirados al frente
        Parte(PrimitiveType.Cube, "BrazoI", visual.transform, new Vector3(-0.35f, 1.15f, 0.32f), new Vector3(0.18f, 0.18f, 0.72f), venda);
        Parte(PrimitiveType.Cube, "BrazoD", visual.transform, new Vector3(0.35f, 1.15f, 0.32f), new Vector3(0.18f, 0.18f, 0.72f), venda);

        // Piernas
        Parte(PrimitiveType.Cube, "PiernaI", visual.transform, new Vector3(-0.16f, 0.42f, 0f), new Vector3(0.2f, 0.85f, 0.22f), pergamino);
        Parte(PrimitiveType.Cube, "PiernaD", visual.transform, new Vector3(0.16f, 0.42f, 0f), new Vector3(0.2f, 0.85f, 0.22f), pergamino);

        PoderEspiritual vida = root.AddComponent<PoderEspiritual>();
        vida.maxPoder = 45f; vida.actualPoder = 45f;

        EsbirroBaseIA ia = root.AddComponent<EsbirroBaseIA>();
        ia.tipo = EsbirroBaseIA.TipoEsbirro.Momia;
        ia.velocidadPersecucion = 4.2f;
        ia.velocidadPatrulla = 1.8f;
        ia.rangoDeteccion = 11f;
        ia.rangoAtaque = 1.7f;
        ia.danio = 14f;
        ia.cooldownAtaque = 1.4f;
        ia.tiempoTelegrafiado = 0.5f;
        ia.colorTelegrafiado = new Color(0.2f, 0.9f, 0.4f, 0.65f); // Verde cripta
        ia.cantidadAlmasADropear = 3;
        ia.alturaBarraVida = 2.0f;
        return ia;
    }

    // ------------------------------------------------------------------ CATRÍN ESPECTRAL (Nivel 2)

    static EsbirroBaseIA CrearCatrin(Vector3 pos)
    {
        GameObject root = new GameObject("Catrin");
        root.transform.position = pos;
        root.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        Etiquetar(root);

        CharacterController cc = root.AddComponent<CharacterController>();
        cc.height = 2.0f; cc.radius = 0.42f; cc.center = new Vector3(0f, 1.0f, 0f);

        Material levita = Mat("traje_catrin", new Color(0.12f, 0.12f, 0.16f), 0.4f);
        Material camisa = Mat("camisa_catrin", new Color(0.9f, 0.88f, 0.82f), 0.3f);
        Material oro = Mat("dorado_catrin", new Color(0.92f, 0.72f, 0.18f), 0.8f);
        Material ojoTurquesa = MatEmisivo("ojo_catrin", new Color(0.1f, 0.8f, 0.9f), new Color(0.3f, 2.4f, 2.5f));

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);

        // Cuerpo estilizado y alto
        Parte(PrimitiveType.Cube, "Torso", visual.transform, new Vector3(0f, 1.15f, 0f), new Vector3(0.5f, 0.75f, 0.28f), levita);
        Parte(PrimitiveType.Cube, "Pechera", visual.transform, new Vector3(0f, 1.25f, 0.1f), new Vector3(0.24f, 0.45f, 0.1f), camisa);
        Parte(PrimitiveType.Cube, "Corbata", visual.transform, new Vector3(0f, 1.35f, 0.16f), new Vector3(0.12f, 0.2f, 0.04f), oro);

        // Cabeza con sombrero de copa
        GameObject cabeza = Parte(PrimitiveType.Sphere, "Cabeza", visual.transform, new Vector3(0f, 1.65f, 0f), Vector3.one * 0.36f, camisa);
        Parte(PrimitiveType.Sphere, "OjoI", cabeza.transform, new Vector3(-0.25f, 0.1f, 0.45f), Vector3.one * 0.22f, ojoTurquesa);
        Parte(PrimitiveType.Sphere, "OjoD", cabeza.transform, new Vector3(0.25f, 0.1f, 0.45f), Vector3.one * 0.22f, ojoTurquesa);

        // Sombrero de copa
        Parte(PrimitiveType.Cylinder, "AlaSombrero", visual.transform, new Vector3(0f, 1.82f, 0f), new Vector3(0.6f, 0.03f, 0.6f), levita);
        Parte(PrimitiveType.Cylinder, "CopaSombrero", visual.transform, new Vector3(0f, 2.05f, 0f), new Vector3(0.42f, 0.22f, 0.42f), levita);
        Parte(PrimitiveType.Cylinder, "CintaSombrero", visual.transform, new Vector3(0f, 1.88f, 0f), new Vector3(0.43f, 0.04f, 0.43f), oro);

        // Bastón elegante en mano derecha
        Parte(PrimitiveType.Cylinder, "Baston", visual.transform, new Vector3(0.38f, 0.75f, 0.25f), new Vector3(0.05f, 0.7f, 0.05f), levita);
        Parte(PrimitiveType.Sphere, "PomoBaston", visual.transform, new Vector3(0.38f, 1.45f, 0.25f), Vector3.one * 0.14f, oro);

        // Piernas finas
        Parte(PrimitiveType.Cube, "PiernaI", visual.transform, new Vector3(-0.15f, 0.45f, 0f), new Vector3(0.18f, 0.9f, 0.2f), levita);
        Parte(PrimitiveType.Cube, "PiernaD", visual.transform, new Vector3(0.15f, 0.45f, 0f), new Vector3(0.18f, 0.9f, 0.2f), levita);

        PoderEspiritual vida = root.AddComponent<PoderEspiritual>();
        vida.maxPoder = 55f; vida.actualPoder = 55f;

        EsbirroBaseIA ia = root.AddComponent<EsbirroBaseIA>();
        ia.tipo = EsbirroBaseIA.TipoEsbirro.Catrin;
        ia.velocidadPersecucion = 3.6f;
        ia.velocidadPatrulla = 1.4f;
        ia.rangoDeteccion = 15f;
        ia.rangoAtaque = 6.5f; // Ataca a media distancia con bastón
        ia.danio = 16f;
        ia.cooldownAtaque = 2.0f;
        ia.tiempoTelegrafiado = 0.65f;
        ia.colorTelegrafiado = new Color(0.1f, 0.7f, 0.9f, 0.7f); // Turquesa
        ia.cantidadAlmasADropear = 4;
        ia.alturaBarraVida = 2.3f;
        return ia;
    }

    // ------------------------------------------------------------------ CUERVO DE PANTEÓN (Nivel 2)

    static EsbirroBaseIA CrearCuervo(Vector3 pos)
    {
        GameObject root = new GameObject("CuervoPanteon");
        root.transform.position = pos;
        root.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        Etiquetar(root);

        CharacterController cc = root.AddComponent<CharacterController>();
        cc.height = 1.0f; cc.radius = 0.4f; cc.center = new Vector3(0f, 0.8f, 0f);

        Material pluma = Mat("pluma_cuervo", new Color(0.1f, 0.1f, 0.15f), 0.6f);
        Material pico = Mat("pico_cuervo", new Color(0.2f, 0.18f, 0.15f), 0.4f);
        Material ojoRojo = MatEmisivo("ojo_cuervo", new Color(1f, 0.1f, 0.1f), new Color(2.5f, 0.2f, 0.1f));

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);

        // Cuerpo de ave
        Parte(PrimitiveType.Sphere, "Cuerpo", visual.transform, new Vector3(0f, 0.8f, 0f), new Vector3(0.4f, 0.35f, 0.6f), pluma);
        GameObject cabeza = Parte(PrimitiveType.Sphere, "Cabeza", visual.transform, new Vector3(0f, 1.0f, 0.26f), Vector3.one * 0.28f, pluma);
        Parte(PrimitiveType.Cube, "Pico", cabeza.transform, new Vector3(0f, -0.1f, 0.45f), new Vector3(0.2f, 0.2f, 0.6f), pico);
        Parte(PrimitiveType.Sphere, "OjoI", cabeza.transform, new Vector3(-0.35f, 0.2f, 0.2f), Vector3.one * 0.25f, ojoRojo);
        Parte(PrimitiveType.Sphere, "OjoD", cabeza.transform, new Vector3(0.35f, 0.2f, 0.2f), Vector3.one * 0.25f, ojoRojo);

        // Alas extendidas
        Parte(PrimitiveType.Cube, "AlaI", visual.transform, new Vector3(-0.48f, 0.88f, 0f), new Vector3(0.65f, 0.06f, 0.35f), pluma);
        Parte(PrimitiveType.Cube, "AlaD", visual.transform, new Vector3(0.48f, 0.88f, 0f), new Vector3(0.65f, 0.06f, 0.35f), pluma);

        PoderEspiritual vida = root.AddComponent<PoderEspiritual>();
        vida.maxPoder = 25f; vida.actualPoder = 25f;

        EsbirroBaseIA ia = root.AddComponent<EsbirroBaseIA>();
        ia.tipo = EsbirroBaseIA.TipoEsbirro.Cuervo;
        ia.velocidadPersecucion = 5.8f; // Rápido en picada
        ia.velocidadPatrulla = 2.4f;
        ia.rangoDeteccion = 14f;
        ia.rangoAtaque = 1.4f;
        ia.danio = 9f;
        ia.cooldownAtaque = 1.1f;
        ia.tiempoTelegrafiado = 0.4f;
        ia.colorTelegrafiado = new Color(0.9f, 0.2f, 0.1f, 0.7f);
        ia.cantidadAlmasADropear = 2;
        ia.alturaBarraVida = 1.4f;
        return ia;
    }

    // ------------------------------------------------------------------ MONJE INQUISIDOR (Nivel 3)

    static EsbirroBaseIA CrearMonje(Vector3 pos)
    {
        GameObject root = new GameObject("MonjePenitente");
        root.transform.position = pos;
        root.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        Etiquetar(root);

        CharacterController cc = root.AddComponent<CharacterController>();
        cc.height = 1.9f; cc.radius = 0.48f; cc.center = new Vector3(0f, 0.95f, 0f);

        Material habito = Mat("habito_monje", new Color(0.24f, 0.18f, 0.13f), 0.2f);
        Material cuerda = Mat("cuerda_monje", new Color(0.82f, 0.76f, 0.62f), 0.3f);
        Material plata = Mat("plata_cruz", new Color(0.85f, 0.85f, 0.9f), 0.8f);
        Material fuegoInquisicion = MatEmisivo("fuego_inquisicion", new Color(1f, 0.45f, 0.05f), new Color(2.5f, 1.2f, 0.2f));

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);

        // Hábito amplio y capucha
        Parte(PrimitiveType.Cylinder, "Sotana", visual.transform, new Vector3(0f, 0.75f, 0f), new Vector3(0.65f, 0.75f, 0.65f), habito);
        Parte(PrimitiveType.Cylinder, "Cordon", visual.transform, new Vector3(0f, 0.85f, 0f), new Vector3(0.68f, 0.06f, 0.68f), cuerda);
        Parte(PrimitiveType.Cube, "Torso", visual.transform, new Vector3(0f, 1.25f, 0f), new Vector3(0.6f, 0.5f, 0.4f), habito);

        // Capucha con sombra profunda y ojos ardientes
        GameObject capucha = Parte(PrimitiveType.Sphere, "Capucha", visual.transform, new Vector3(0f, 1.62f, 0f), new Vector3(0.48f, 0.48f, 0.48f), habito);
        Parte(PrimitiveType.Cube, "SombraRostro", capucha.transform, new Vector3(0f, -0.05f, 0.26f), new Vector3(0.6f, 0.5f, 0.3f), Mat("sombra_rostro", Color.black, 0f));
        Parte(PrimitiveType.Sphere, "OjoI", capucha.transform, new Vector3(-0.2f, 0.02f, 0.38f), Vector3.one * 0.18f, fuegoInquisicion);
        Parte(PrimitiveType.Sphere, "OjoD", capucha.transform, new Vector3(0.2f, 0.02f, 0.38f), Vector3.one * 0.18f, fuegoInquisicion);

        // Cruz de hierro / candelero
        Parte(PrimitiveType.Cube, "MastilCruz", visual.transform, new Vector3(0.35f, 1.2f, 0.35f), new Vector3(0.06f, 0.8f, 0.06f), plata);
        Parte(PrimitiveType.Cube, "BrazoCruz", visual.transform, new Vector3(0.35f, 1.45f, 0.35f), new Vector3(0.35f, 0.06f, 0.06f), plata);
        Parte(PrimitiveType.Sphere, "LlamaCruz", visual.transform, new Vector3(0.35f, 1.65f, 0.35f), Vector3.one * 0.16f, fuegoInquisicion);

        PoderEspiritual vida = root.AddComponent<PoderEspiritual>();
        vida.maxPoder = 65f; vida.actualPoder = 65f;

        EsbirroBaseIA ia = root.AddComponent<EsbirroBaseIA>();
        ia.tipo = EsbirroBaseIA.TipoEsbirro.Monje;
        ia.velocidadPersecucion = 2.9f;
        ia.velocidadPatrulla = 1.2f;
        ia.rangoDeteccion = 12f;
        ia.rangoAtaque = 2.5f; // Golpe de área de fuego sagrado
        ia.danio = 18f;
        ia.cooldownAtaque = 1.8f;
        ia.tiempoTelegrafiado = 0.7f;
        ia.colorTelegrafiado = new Color(1f, 0.5f, 0.1f, 0.7f); // Naranja candente
        ia.cantidadAlmasADropear = 5;
        ia.alturaBarraVida = 2.2f;
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
