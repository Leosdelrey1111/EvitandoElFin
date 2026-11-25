using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Generador ambiental de la Cañada de Guanajuato (Nivel 3).
/// Enmarcado en el descenso de la Sierra de Guanajuato hacia la cañada virreinal:
///  - Desfiladeros rocosos de la sierra que enmarcan los callejones empinados.
///  - Escalinatas de piedra de cantera que bajan con el desnivel de la montaña.
///  - Fachadas virreinales de colores vivos (terracota, ocre mostaza, azul añil, rosa mexicano y encalado)
///    adosadas directamente a los riscos.
///  - El auténtico Callejón del Beso: dos balcones de herrería separados por apenas 70 cm en la peña.
///  - Túneles subterráneos mineros de cantera rosa con faroles coloniales.
///  - Plazuela colonial con fuente de cantera y puesto de Don Chuy.
///  - Monumental Atrio y torres campanario de la Basílica de Guanajuato (Arena del Clérigo Inquisidor).
///  - Hitos narrativos integrados para disparar las cinemáticas de cámara de la leyenda.
/// </summary>
public class PaisajeCiudadColonial : MonoBehaviour
{
    [Header("Materiales")]
    public Material matCanteraRosa;
    public Material matAdoquin;
    public Material matHerreria;
    public Material matDorado;
    public Material matFuegoPurpura;
    public Material matFloraVerde;

    static readonly Color[] ColoresFachadas =
    {
        new Color(0.85f, 0.35f, 0.22f), // Terracota
        new Color(0.92f, 0.72f, 0.22f), // Ocre mostaza
        new Color(0.18f, 0.42f, 0.68f), // Azul añil virreinal
        new Color(0.88f, 0.45f, 0.55f), // Rosa mexicano
        new Color(0.92f, 0.88f, 0.80f)  // Encalado cantera
    };

    void Awake()
    {
        if (transform.Find("Escenario_Cañada_Guanajuato") != null || GameObject.Find("Escenario_Cañada_Guanajuato") != null) return;
        CrearMaterialesSiFaltan();
        ConstruirCañadaColonialCompleta();
    }

    public void CrearMaterialesSiFaltan()
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        if (matCanteraRosa == null)
        {
            matCanteraRosa = new Material(s);
            matCanteraRosa.color = new Color(0.82f, 0.62f, 0.55f);
        }
        if (matAdoquin == null)
        {
            matAdoquin = new Material(s);
            matAdoquin.color = new Color(0.42f, 0.40f, 0.38f);
        }
        if (matHerreria == null)
        {
            matHerreria = new Material(s);
            matHerreria.color = new Color(0.1f, 0.1f, 0.12f);
        }
        if (matDorado == null)
        {
            matDorado = new Material(s);
            Color d = new Color(1f, 0.8f, 0.2f);
            matDorado.color = d;
            matDorado.EnableKeyword("_EMISSION");
            matDorado.SetColor("_EmissionColor", d * 1.8f);
        }
        if (matFuegoPurpura == null)
        {
            matFuegoPurpura = new Material(s);
            Color p = new Color(0.6f, 0.15f, 0.85f);
            matFuegoPurpura.color = p;
            matFuegoPurpura.EnableKeyword("_EMISSION");
            matFuegoPurpura.SetColor("_EmissionColor", p * 2.2f);
        }
        if (matFloraVerde == null)
        {
            matFloraVerde = new Material(s);
            matFloraVerde.color = new Color(0.25f, 0.42f, 0.22f);
        }
    }

    public void ConstruirCañadaColonialCompleta()
    {
        GameObject raiz = new GameObject("Escenario_Cañada_Guanajuato");
        raiz.transform.SetParent(transform, false);

        float largoCañada = 160f;
        float anchoCañada = 50f;

        // 1. Suelo adoquinado en rampa que desciende por la montaña
        GameObject calle = GameObject.CreatePrimitive(PrimitiveType.Plane);
        calle.name = "CalleAdoquinada";
        calle.transform.SetParent(raiz.transform, false);
        calle.transform.localScale = new Vector3(anchoCañada / 10f, 1f, largoCañada / 10f);
        calle.transform.localPosition = new Vector3(0, 0, largoCañada / 2f);
        calle.GetComponent<Renderer>().material = matAdoquin;

        // 2. Riscos de la Sierra flanqueando ambos lados de la ciudad
        ConstruirRiscosSierra(raiz.transform, largoCañada, anchoCañada);

        // 3. Casas coloniales con balcones de herrería a izquierda y derecha
        int contadorCasas = 0;
        for (float z = 12f; z < largoCañada - 35f; z += 14f)
        {
            float curvaX = Mathf.Sin(z * 0.09f) * 4f;

            Color colI = ColoresFachadas[contadorCasas % ColoresFachadas.Length];
            ConstruirCasaColonial(raiz.transform, new Vector3(-12f + curvaX, 0, z), colI, true);

            Color colD = ColoresFachadas[(contadorCasas + 2) % ColoresFachadas.Length];
            ConstruirCasaColonial(raiz.transform, new Vector3(12f + curvaX, 0, z), colD, false);

            // Faroles coloniales más frecuentes para mejor iluminación
            ConstruirFarolColonial(raiz.transform, new Vector3(curvaX, 4.5f, z));
            
            // Faroles adicionales cada 3 casas
            if (contadorCasas % 3 == 0)
            {
                ConstruirFarolColonial(raiz.transform, new Vector3(-6f + curvaX, 2.5f, z + 7f));
                ConstruirFarolColonial(raiz.transform, new Vector3(6f + curvaX, 2.5f, z + 7f));
            }
            
            // Macetas con flores cada 2 casas para color
            if (contadorCasas % 2 == 0)
            {
                ConstruirMacetaFlores(raiz.transform, new Vector3(-10f + curvaX, 0, z + 2f));
                ConstruirMacetaFlores(raiz.transform, new Vector3(10f + curvaX, 0, z + 2f));
            }

            contadorCasas++;
        }
        
        // Señalización hacia la Basílica (flechas en el suelo con luces)
        for (float z = 50f; z < largoCañada - 40f; z += 30f)
        {
            ConstruirSenalBasilica(raiz.transform, new Vector3(0, 0.1f, z));
        }

        // 4. El icónico Callejón del Beso (z = 52f)
        ConstruirCallejonDelBeso(raiz.transform, new Vector3(0, 0, 52f));

        // 5. Arcos de cantera de los túneles subterráneos de Guanajuato (z = 82f)
        ConstruirArcosTunelSubterraneo(raiz.transform, new Vector3(0, 0, 82f));

        // 6. Plazuela con fuente colonial y puesto de Don Chuy (z = 32f)
        ConstruirPlazuelaDonChuy(raiz.transform, new Vector3(16f, 0, 32f));

        // 7. Monumental Atrio y Basílica de Guanajuato (Arena del Inquisidor)
        ConstruirAtrioBasílica(raiz.transform, new Vector3(0, 0, largoCañada - 20f));

        // 8. Los 3 Sellos Inquisitoriales (Coleccionables / Tótems de Nivel 3)
        ConstruirSelloColeccionable(raiz.transform, new Vector3(-6f, 0.8f, 26f), "Sello_1");
        ConstruirSelloColeccionable(raiz.transform, new Vector3(3f, 1.8f, 52f), "Sello_2"); // En el peldaño del Callejón del Beso
        ConstruirSelloColeccionable(raiz.transform, new Vector3(-7f, 0.8f, 98f), "Sello_3");

        // 9. Altar colonial (Punto de Control)
        ConstruirAltarCiudad(raiz.transform, new Vector3(0, 0, 68f));

        // 10. Hitos Narrativos (disparan cinemáticas de cámara con diálogo)
        CrearHitoNarrativo(raiz.transform, "intro_ciudad", new Vector3(0, 0, 8f), 12f);
        CrearHitoNarrativo(raiz.transform, "mirador_pipila", new Vector3(0, 0, 26f), 10f);
        CrearHitoNarrativo(raiz.transform, "monjes", new Vector3(0, 0, 42f), 11f);
        CrearHitoNarrativo(raiz.transform, "callejon_beso", new Vector3(0, 0, 52f), 12f);
        CrearHitoNarrativo(raiz.transform, "tuneles_mineros", new Vector3(0, 0, 82f), 11f);
        CrearHitoNarrativo(raiz.transform, "sombras_callejon", new Vector3(0, 0, 105f), 10f);
        CrearHitoNarrativo(raiz.transform, "basilica", new Vector3(0, 0, largoCañada - 36f), 14f);
    }

    void ConstruirRiscosSierra(Transform padre, float largo, float ancho)
    {
        GameObject cordillera = new GameObject("Riscos_Cañada_Sierra");
        cordillera.transform.SetParent(padre, false);

        int bloques = Mathf.RoundToInt(largo / 14f);
        for (int i = 0; i <= bloques; i++)
        {
            float z = i * 14f;
            float altoI = 20f + Mathf.PerlinNoise(i * 0.35f, 4.2f) * 16f;
            float altoD = 20f + Mathf.PerlinNoise(i * 0.35f, 9.8f) * 16f;

            GameObject riscoI = GameObject.CreatePrimitive(PrimitiveType.Cube);
            riscoI.transform.SetParent(cordillera.transform, false);
            riscoI.transform.localPosition = new Vector3(-ancho / 2f - 10f, altoI / 2f, z);
            riscoI.transform.localScale = new Vector3(22f, altoI, 16f);
            riscoI.transform.localRotation = Quaternion.Euler(Random.Range(-5f, 5f), Random.Range(-10f, 10f), 0);
            riscoI.GetComponent<Renderer>().material = matCanteraRosa;

            GameObject riscoD = GameObject.CreatePrimitive(PrimitiveType.Cube);
            riscoD.transform.SetParent(cordillera.transform, false);
            riscoD.transform.localPosition = new Vector3(ancho / 2f + 10f, altoD / 2f, z);
            riscoD.transform.localScale = new Vector3(22f, altoD, 16f);
            riscoD.transform.localRotation = Quaternion.Euler(Random.Range(-5f, 5f), Random.Range(-10f, 10f), 0);
            riscoD.GetComponent<Renderer>().material = matCanteraRosa;
        }
    }

    void ConstruirCasaColonial(Transform padre, Vector3 pos, Color colorPared, bool ladoIzquierdo)
    {
        GameObject casa = new GameObject("CasaColonial");
        casa.transform.SetParent(padre, false);
        casa.transform.localPosition = pos;

        Material matPared = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        matPared.color = colorPared;

        GameObject cuerpo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cuerpo.transform.SetParent(casa.transform, false);
        cuerpo.transform.localPosition = new Vector3(0, 4.5f, 0);
        cuerpo.transform.localScale = new Vector3(7f, 9f, 12f);
        cuerpo.GetComponent<Renderer>().material = matPared;

        // Marco de cantera en puerta
        GameObject puerta = GameObject.CreatePrimitive(PrimitiveType.Cube);
        puerta.transform.SetParent(casa.transform, false);
        float xFront = ladoIzquierdo ? 3.55f : -3.55f;
        puerta.transform.localPosition = new Vector3(xFront, 1.4f, 0);
        puerta.transform.localScale = new Vector3(0.15f, 2.7f, 1.5f);
        puerta.GetComponent<Renderer>().material = matCanteraRosa;
        Destroy(puerta.GetComponent<Collider>());

        // Balcón de herrería en planta alta
        ConstruirBalconHerreria(casa.transform, new Vector3(xFront, 5.4f, 0), ladoIzquierdo);
    }

    void ConstruirBalconHerreria(Transform padre, Vector3 pos, bool ladoIzquierdo)
    {
        GameObject balcon = new GameObject("Balcon");
        balcon.transform.SetParent(padre, false);
        balcon.transform.localPosition = pos;

        GameObject baseBalcon = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseBalcon.transform.SetParent(balcon.transform, false);
        baseBalcon.transform.localPosition = new Vector3(ladoIzquierdo ? 0.65f : -0.65f, 0, 0);
        baseBalcon.transform.localScale = new Vector3(1.3f, 0.28f, 2.4f);
        baseBalcon.GetComponent<Renderer>().material = matCanteraRosa;

        GameObject barandal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        barandal.transform.SetParent(balcon.transform, false);
        barandal.transform.localPosition = new Vector3(ladoIzquierdo ? 1.25f : -1.25f, 0.55f, 0);
        barandal.transform.localScale = new Vector3(0.08f, 0.95f, 2.4f);
        barandal.GetComponent<Renderer>().material = matHerreria;
        Destroy(barandal.GetComponent<Collider>());
    }

    void ConstruirCallejonDelBeso(Transform padre, Vector3 pos)
    {
        GameObject callejon = new GameObject("Hito_CallejonDelBeso");
        callejon.transform.SetParent(padre, false);
        callejon.transform.localPosition = pos;

        // Escalera estrecha de piedra de cantera (los 7 escalones de la leyenda)
        for (int i = 0; i < 7; i++)
        {
            GameObject peldanio = GameObject.CreatePrimitive(PrimitiveType.Cube);
            peldanio.transform.SetParent(callejon.transform, false);
            peldanio.transform.localPosition = new Vector3(0, (i + 1) * 0.24f, i * 0.7f);
            peldanio.transform.localScale = new Vector3(3.4f, 0.24f, 0.75f);
            peldanio.GetComponent<Renderer>().material = matCanteraRosa;
        }

        // Los dos balcones enfrentados a solo 70 cm de distancia
        GameObject balconCarmen = GameObject.CreatePrimitive(PrimitiveType.Cube);
        balconCarmen.name = "Balcon_Carmen";
        balconCarmen.transform.SetParent(callejon.transform, false);
        balconCarmen.transform.localPosition = new Vector3(-0.7f, 5.0f, 2.2f);
        balconCarmen.transform.localScale = new Vector3(0.95f, 0.85f, 1.8f);
        balconCarmen.GetComponent<Renderer>().material = matHerreria;

        GameObject balconLuis = GameObject.CreatePrimitive(PrimitiveType.Cube);
        balconLuis.name = "Balcon_DonLuis";
        balconLuis.transform.SetParent(callejon.transform, false);
        balconLuis.transform.localPosition = new Vector3(0.7f, 5.0f, 2.2f);
        balconLuis.transform.localScale = new Vector3(0.95f, 0.85f, 1.8f);
        balconLuis.GetComponent<Renderer>().material = matHerreria;

        // Placa dorada en la pared
        GameObject placa = GameObject.CreatePrimitive(PrimitiveType.Cube);
        placa.transform.SetParent(callejon.transform, false);
        placa.transform.localPosition = new Vector3(0, 2.0f, 5.0f);
        placa.transform.localScale = new Vector3(1.4f, 0.8f, 0.1f);
        placa.GetComponent<Renderer>().material = matDorado;
    }

    void ConstruirArcosTunelSubterraneo(Transform padre, Vector3 pos)
    {
        GameObject tunel = new GameObject("Arcos_Tunel_Cantera");
        tunel.transform.SetParent(padre, false);
        tunel.transform.localPosition = pos;

        for (int i = 0; i < 3; i++)
        {
            GameObject pilarI = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pilarI.transform.SetParent(tunel.transform, false);
            pilarI.transform.localPosition = new Vector3(-7f, 3.5f, i * 6f);
            pilarI.transform.localScale = new Vector3(1.5f, 7f, 1.5f);
            pilarI.GetComponent<Renderer>().material = matCanteraRosa;

            GameObject pilarD = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pilarD.transform.SetParent(tunel.transform, false);
            pilarD.transform.localPosition = new Vector3(7f, 3.5f, i * 6f);
            pilarD.transform.localScale = new Vector3(1.5f, 7f, 1.5f);
            pilarD.GetComponent<Renderer>().material = matCanteraRosa;

            GameObject arco = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arco.transform.SetParent(tunel.transform, false);
            arco.transform.localPosition = new Vector3(0, 7.2f, i * 6f);
            arco.transform.localScale = new Vector3(15.5f, 1.4f, 1.8f);
            arco.GetComponent<Renderer>().material = matCanteraRosa;
        }
    }

    void ConstruirFarolColonial(Transform padre, Vector3 pos)
    {
        GameObject farol = new GameObject("FarolColonial");
        farol.transform.SetParent(padre, false);
        farol.transform.localPosition = pos;

        GameObject cabina = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cabina.transform.SetParent(farol.transform, false);
        cabina.transform.localScale = new Vector3(0.5f, 0.7f, 0.5f);
        cabina.GetComponent<Renderer>().material = matDorado;
        Destroy(cabina.GetComponent<Collider>());

        Light l = farol.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.76f, 0.35f);
        l.range = 11f;
        l.intensity = 2.4f;
    }

    void ConstruirPlazuelaDonChuy(Transform padre, Vector3 pos)
    {
        GameObject plazuela = new GameObject("Plazuela_DonChuy");
        plazuela.transform.SetParent(padre, false);
        plazuela.transform.localPosition = pos;

        GameObject fuente = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        fuente.transform.SetParent(plazuela.transform, false);
        fuente.transform.localPosition = new Vector3(0, 0.5f, 0);
        fuente.transform.localScale = new Vector3(5f, 0.5f, 5f);
        fuente.GetComponent<Renderer>().material = matCanteraRosa;

        GameObject puesto = new GameObject("Puesto_DonChuy_Ciudad");
        puesto.transform.SetParent(plazuela.transform, false);
        puesto.transform.localPosition = new Vector3(3.5f, 0, 0);

        GameObject toldo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        toldo.transform.SetParent(puesto.transform, false);
        toldo.transform.localPosition = new Vector3(0, 2.4f, 0);
        toldo.transform.localScale = new Vector3(3.5f, 0.18f, 2.8f);
        toldo.GetComponent<Renderer>().material = matCanteraRosa;

        puesto.AddComponent<ComercianteMercader>();
    }

    void ConstruirAtrioBasílica(Transform padre, Vector3 pos)
    {
        GameObject atrio = new GameObject("AtrioBasílica_ArenaFinal");
        atrio.transform.SetParent(padre, false);
        atrio.transform.localPosition = pos;

        // Gran explanada ceremonial de cantera rosa
        GameObject explanada = GameObject.CreatePrimitive(PrimitiveType.Cube);
        explanada.transform.SetParent(atrio.transform, false);
        explanada.transform.localPosition = new Vector3(0, 0.3f, 0);
        explanada.transform.localScale = new Vector3(34f, 0.6f, 34f);
        explanada.GetComponent<Renderer>().material = matCanteraRosa;

        // Fachada monumental de la Basílica de Guanajuato
        GameObject fachada = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fachada.transform.SetParent(atrio.transform, false);
        fachada.transform.localPosition = new Vector3(0, 8.5f, 16f);
        fachada.transform.localScale = new Vector3(28f, 17f, 4.5f);
        fachada.GetComponent<Renderer>().material = matCanteraRosa;

        // Dos torres campanario virreinales
        ConstruirTorreCampanario(atrio.transform, new Vector3(-11f, 0, 16f));
        ConstruirTorreCampanario(atrio.transform, new Vector3(11f, 0, 16f));

        // Jefe Inquisidor en el centro del atrio
        GameObject bossGo = new GameObject("Jefe_ClerigoInquisidor");
        bossGo.transform.position = atrio.transform.position + Vector3.up * 0.6f;
        bossGo.AddComponent<JefeInquisidor>();

        // Sistema de arena cerrada
        ArenaAtrioBasilica arenaScript = atrio.AddComponent<ArenaAtrioBasilica>();
        arenaScript.jefe = bossGo.transform;
        arenaScript.radioActivacion = 18f;
        arenaScript.radioArena = 16f;
        arenaScript.alturaBarrera = 5f;

        // Portal Dorado Final del Juego (Retorno a la vida)
        GameObject portal = new GameObject("PortalRetornoMundoVivo");
        portal.transform.position = atrio.transform.position + Vector3.forward * 13f + Vector3.up * 1.2f;
        portal.AddComponent<BoxCollider>().size = new Vector3(5f, 5f, 2f);
        portal.AddComponent<PortalVictoria>();
    }

    void ConstruirTorreCampanario(Transform padre, Vector3 pos)
    {
        GameObject torre = GameObject.CreatePrimitive(PrimitiveType.Cube);
        torre.name = "Campanario";
        torre.transform.SetParent(padre, false);
        torre.transform.localPosition = pos + Vector3.up * 13f;
        torre.transform.localScale = new Vector3(6f, 26f, 6f);
        torre.GetComponent<Renderer>().material = matCanteraRosa;

        GameObject campana = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        campana.transform.SetParent(torre.transform, false);
        campana.transform.localPosition = new Vector3(0, 0.3f, 0);
        campana.transform.localScale = new Vector3(0.6f, 0.14f, 0.6f);
        campana.GetComponent<Renderer>().material = matDorado;
        Destroy(campana.GetComponent<Collider>());
    }

    void ConstruirSelloColeccionable(Transform padre, Vector3 pos, string id)
    {
        GameObject totem = new GameObject("Totem_" + id);
        totem.transform.SetParent(padre, false);
        totem.transform.localPosition = pos;

        GameObject escudo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        escudo.transform.SetParent(totem.transform, false);
        escudo.transform.localScale = new Vector3(0.75f, 0.1f, 0.75f);
        escudo.transform.localRotation = Quaternion.Euler(90, 0, 0);
        escudo.GetComponent<Renderer>().material = matDorado;
        Destroy(escudo.GetComponent<Collider>());

        SphereCollider sc = totem.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 1.3f;

        TotemColeccionable tc = totem.AddComponent<TotemColeccionable>();
        tc.idTotem = id;
        tc.valorTotem = 1;
    }

    void ConstruirAltarCiudad(Transform padre, Vector3 pos)
    {
        GameObject altar = new GameObject("Altar_Ciudad_Guanajuato");
        altar.transform.SetParent(padre, false);
        altar.transform.localPosition = pos;

        GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pedestal.transform.SetParent(altar.transform, false);
        pedestal.transform.localPosition = new Vector3(0, 1.3f, 0);
        pedestal.transform.localScale = new Vector3(1.3f, 2.6f, 1.3f);
        pedestal.GetComponent<Renderer>().material = matCanteraRosa;

        SphereCollider sc = altar.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 4.5f;

        PuntoControl pc = altar.AddComponent<PuntoControl>();
        pc.idCheckpoint = "Nivel3_AltarBasílica";
        pc.nombreAltar = "Altar de la Basílica de Guanajuato";
    }

    void CrearHitoNarrativo(Transform padre, string idHistoria, Vector3 pos, float radio)
    {
        GameObject hito = new GameObject("Hito_" + idHistoria);
        hito.transform.SetParent(padre, false);
        hito.transform.localPosition = pos;
        HitoNarrativo h = hito.AddComponent<HitoNarrativo>();
        h.idHistoria = idHistoria;
        h.radio = radio;
    }

    void ConstruirMacetaFlores(Transform padre, Vector3 pos)
    {
        GameObject maceta = new GameObject("Maceta_Flores");
        maceta.transform.SetParent(padre, false);
        maceta.transform.localPosition = pos;

        // Base de maceta
        GameObject base_maceta = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        base_maceta.transform.SetParent(maceta.transform, false);
        base_maceta.transform.localPosition = new Vector3(0, 0.3f, 0);
        base_maceta.transform.localScale = new Vector3(0.5f, 0.3f, 0.5f);
        base_maceta.GetComponent<Renderer>().material = matCanteraRosa;

        // Flores (cempasúchil)
        for (int i = 0; i < 3; i++)
        {
            float angulo = i * 120f * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Sin(angulo) * 0.2f, 0.6f, Mathf.Cos(angulo) * 0.2f);
            
            GameObject flor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flor.transform.SetParent(maceta.transform, false);
            flor.transform.localPosition = offset;
            flor.transform.localScale = Vector3.one * 0.15f;
            
            Material matFlor = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            matFlor.color = new Color(1f, 0.6f, 0.1f); // Naranja cempasúchil
            flor.GetComponent<Renderer>().material = matFlor;
            Destroy(flor.GetComponent<Collider>());
        }
    }

    void ConstruirSenalBasilica(Transform padre, Vector3 pos)
    {
        GameObject senal = new GameObject("Senal_Basilica");
        senal.transform.SetParent(padre, false);
        senal.transform.localPosition = pos;

        // Flecha en el suelo apuntando hacia adelante
        GameObject flecha = GameObject.CreatePrimitive(PrimitiveType.Cube);
        flecha.transform.SetParent(senal.transform, false);
        flecha.transform.localPosition = new Vector3(0, 0.05f, 0);
        flecha.transform.localScale = new Vector3(1.5f, 0.1f, 2.5f);
        flecha.transform.localRotation = Quaternion.Euler(0, 0, 0);
        
        Material matSenal = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        matSenal.color = new Color(1f, 0.85f, 0.3f); // Dorado
        matSenal.EnableKeyword("_EMISSION");
        matSenal.SetColor("_EmissionColor", new Color(1f, 0.85f, 0.3f) * 0.3f);
        flecha.GetComponent<Renderer>().material = matSenal;
        
        // Punta de flecha
        GameObject punta = GameObject.CreatePrimitive(PrimitiveType.Cube);
        punta.transform.SetParent(senal.transform, false);
        punta.transform.localPosition = new Vector3(0, 0.05f, 1.8f);
        punta.transform.localScale = new Vector3(1.2f, 0.1f, 0.8f);
        punta.transform.localRotation = Quaternion.Euler(0, 45, 0);
        punta.GetComponent<Renderer>().material = matSenal;

        GameObject punta2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        punta2.transform.SetParent(senal.transform, false);
        punta2.transform.localPosition = new Vector3(0, 0.05f, 1.8f);
        punta2.transform.localScale = new Vector3(1.2f, 0.1f, 0.8f);
        punta2.transform.localRotation = Quaternion.Euler(0, -45, 0);
        punta2.GetComponent<Renderer>().material = matSenal;

        // Luz suave en la señal
        GameObject luzGo = new GameObject("LuzSenal");
        luzGo.transform.SetParent(senal.transform, false);
        luzGo.transform.localPosition = new Vector3(0, 1f, 0);
        Light luz = luzGo.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = new Color(1f, 0.85f, 0.3f);
        luz.range = 6f;
        luz.intensity = 0.4f;
        luz.shadows = LightShadows.None;
    }
}
