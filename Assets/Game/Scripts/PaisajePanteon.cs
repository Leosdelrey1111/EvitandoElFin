using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Generador ambiental de la Sierra de Santa Paula (Nivel 2).
/// Enmarcado en las altas cumbres de la Sierra de Guanajuato de noche:
///  - Riscos y montañas de cantera rosa que cierran el desfiladero.
///  - Muros de gavetas funerarias talladas directamente en la peña viva.
///  - Sendero serpenteante de tepetate con desniveles, lápidas y cruces con veladoras.
///  - Flora nativa de la sierra: nopales con tunas, biznagas, magueyes y mezquites combinados con cipreses funerarios.
///  - Faroles de gas con fuego fatuo esmeralda.
///  - Gran Mausoleo del General en la cumbre rocosa (Arena del Jefe).
///  - Capilla de Don Chuy y los 3 Cirios de Santa Paula.
///  - Hitos narrativos integrados para disparar las cinemáticas de cámara de la leyenda.
/// </summary>
public class PaisajePanteon : MonoBehaviour
{
    [Header("Materiales")]
    public Material matCanteraRosa;
    public Material matTepetate;
    public Material matPiedraOscura;
    public Material matHerreria;
    public Material matFuegoFatuo;
    public Material matCempasuchil;
    public Material matFloraVerde;

    [Header("Dimensiones de la Sierra")]
    public float anchoValle = 70f;
    public float largoSierra = 160f;

    void Awake()
    {
        if (transform.Find("Escenario_Sierra_SantaPaula") != null || GameObject.Find("Escenario_Sierra_SantaPaula") != null) return;
        CrearMaterialesSiFaltan();
        ConstruirSierraPanteonCompleta();
    }

    public void CrearMaterialesSiFaltan()
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        if (matCanteraRosa == null)
        {
            matCanteraRosa = new Material(s);
            matCanteraRosa.color = new Color(0.76f, 0.54f, 0.48f); // Cantera rosa de Guanajuato
        }
        if (matTepetate == null)
        {
            matTepetate = new Material(s);
            matTepetate.color = new Color(0.48f, 0.42f, 0.35f); // Suelo de tepetate
        }
        if (matPiedraOscura == null)
        {
            matPiedraOscura = new Material(s);
            matPiedraOscura.color = new Color(0.28f, 0.28f, 0.32f);
        }
        if (matHerreria == null)
        {
            matHerreria = new Material(s);
            matHerreria.color = new Color(0.12f, 0.12f, 0.14f);
        }
        if (matFuegoFatuo == null)
        {
            matFuegoFatuo = new Material(s);
            Color v = new Color(0.15f, 0.95f, 0.45f);
            matFuegoFatuo.color = v;
            matFuegoFatuo.EnableKeyword("_EMISSION");
            matFuegoFatuo.SetColor("_EmissionColor", v * 2.5f);
        }
        if (matCempasuchil == null)
        {
            matCempasuchil = new Material(s);
            Color c = new Color(1f, 0.65f, 0.1f);
            matCempasuchil.color = c;
            matCempasuchil.EnableKeyword("_EMISSION");
            matCempasuchil.SetColor("_EmissionColor", c * 1.6f);
        }
        if (matFloraVerde == null)
        {
            matFloraVerde = new Material(s);
            matFloraVerde.color = new Color(0.24f, 0.36f, 0.22f); // Verde nopal árido
        }
    }

    public void ConstruirSierraPanteonCompleta()
    {
        GameObject raiz = new GameObject("Escenario_Sierra_SantaPaula");
        raiz.transform.SetParent(transform, false);

        // 1. Suelo base del cañón
        GameObject suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
        suelo.name = "SueloTepetate";
        suelo.transform.SetParent(raiz.transform, false);
        suelo.transform.localScale = new Vector3(anchoValle / 10f, 1f, largoSierra / 10f);
        suelo.transform.localPosition = new Vector3(0, 0, largoSierra / 2f);
        suelo.GetComponent<Renderer>().material = matTepetate;

        // 2. Riscos imponentes de la Sierra de Guanajuato flanqueando ambos lados
        ConstruirCordilleraSierra(raiz.transform);

        // 3. Senderos serpenteantes con gavetas talladas en las peñas
        for (float z = 15f; z < largoSierra - 35f; z += 18f)
        {
            float curvaX = Mathf.Sin(z * 0.08f) * 6f;

            // Muros de gavetas empotrados en los riscos
            ConstruirMuroGavetas(raiz.transform, new Vector3(-18f + curvaX, 0, z), true);
            ConstruirMuroGavetas(raiz.transform, new Vector3(18f + curvaX, 0, z + 6f), false);

            // Mausoleos familiares en las terrazas
            ConstruirMausoleo(raiz.transform, new Vector3(-11f + curvaX, 0, z + 4f));
            ConstruirMausoleo(raiz.transform, new Vector3(11f + curvaX, 0, z - 3f));

            // Tumbas con veladoras en la ladera
            ConstruirTumba(raiz.transform, new Vector3(-4f + curvaX, 0, z - 4f));
            ConstruirTumba(raiz.transform, new Vector3(4f + curvaX, 0, z + 3f));

            // Flora nativa serrana (nopales y biznagas) junto a cipreses
            ConstruirNopalera(raiz.transform, new Vector3(-14f + curvaX, 0, z - 2f));
            ConstruirNopalera(raiz.transform, new Vector3(14f + curvaX, 0, z + 5f));
            ConstruirCipres(raiz.transform, new Vector3(-8f + curvaX, 0, z + 8f));
            ConstruirCipres(raiz.transform, new Vector3(8f + curvaX, 0, z - 6f));

            // Farol con fuego fatuo
            ConstruirFarolPanteon(raiz.transform, new Vector3(curvaX, 0, z));
        }

        // 4. Gran Mausoleo Central en la Cumbre (Arena del General Momia)
        ConstruirArenaMausoleoGeneral(raiz.transform, new Vector3(0, 3f, largoSierra - 22f));

        // 5. Capilla y puesto de Don Chuy
        ConstruirCapillaDonChuy(raiz.transform, new Vector3(14f, 0, 30f));

        // 6. Los 3 Cirios de Santa Paula (Coleccionables / Tótems)
        ConstruirCirioColeccionable(raiz.transform, new Vector3(-6f, 0.8f, 32f), "Cirio_1");
        ConstruirCirioColeccionable(raiz.transform, new Vector3(7f, 0.8f, 75f), "Cirio_2");
        ConstruirCirioColeccionable(raiz.transform, new Vector3(-8f, 0.8f, 108f), "Cirio_3");

        // 7. Altar de las Ánimas (Punto de Control)
        ConstruirAltarPanteon(raiz.transform, new Vector3(0, 0, 52f));

        // 8. Hitos Narrativos (disparan cinemáticas de cámara con diálogo)
        CrearHitoNarrativo(raiz.transform, "intro_panteon", new Vector3(0, 0, 8f), 12f);
        CrearHitoNarrativo(raiz.transform, "collado_sierra", new Vector3(0, 0, 28f), 10f);
        CrearHitoNarrativo(raiz.transform, "momias", new Vector3(0, 0, 55f), 11f);
        CrearHitoNarrativo(raiz.transform, "criptas_antiguas", new Vector3(0, 0, 82f), 10f);
        CrearHitoNarrativo(raiz.transform, "catrines", new Vector3(0, 0, 105f), 10f);
        CrearHitoNarrativo(raiz.transform, "cuervos", new Vector3(0, 0, 125f), 10f);
        CrearHitoNarrativo(raiz.transform, "cripta", new Vector3(0, 2f, largoSierra - 35f), 14f);
    }

    void ConstruirCordilleraSierra(Transform padre)
    {
        GameObject cordillera = new GameObject("Riscos_Sierra_Guanajuato");
        cordillera.transform.SetParent(padre, false);

        int bloques = Mathf.RoundToInt(largoSierra / 14f);
        for (int i = 0; i <= bloques; i++)
        {
            float z = i * 14f;
            float altoI = 18f + Mathf.PerlinNoise(i * 0.4f, 2.5f) * 14f;
            float altoD = 18f + Mathf.PerlinNoise(i * 0.4f, 8.2f) * 14f;

            // Risco Izquierdo
            GameObject riscoI = GameObject.CreatePrimitive(PrimitiveType.Cube);
            riscoI.transform.SetParent(cordillera.transform, false);
            riscoI.transform.localPosition = new Vector3(-anchoValle / 2f - 8f, altoI / 2f, z);
            riscoI.transform.localScale = new Vector3(20f, altoI, 16f);
            riscoI.transform.localRotation = Quaternion.Euler(Random.Range(-4f, 4f), Random.Range(-15f, 15f), Random.Range(-4f, 4f));
            riscoI.GetComponent<Renderer>().material = matCanteraRosa;

            // Risco Derecho
            GameObject riscoD = GameObject.CreatePrimitive(PrimitiveType.Cube);
            riscoD.transform.SetParent(cordillera.transform, false);
            riscoD.transform.localPosition = new Vector3(anchoValle / 2f + 8f, altoD / 2f, z);
            riscoD.transform.localScale = new Vector3(20f, altoD, 16f);
            riscoD.transform.localRotation = Quaternion.Euler(Random.Range(-4f, 4f), Random.Range(-15f, 15f), Random.Range(-4f, 4f));
            riscoD.GetComponent<Renderer>().material = matCanteraRosa;
        }
    }

    void ConstruirMuroGavetas(Transform padre, Vector3 pos, bool miraAlEste)
    {
        GameObject muro = GameObject.CreatePrimitive(PrimitiveType.Cube);
        muro.name = "Gavetas_Pena";
        muro.transform.SetParent(padre, false);
        muro.transform.localPosition = pos + Vector3.up * 3f;
        muro.transform.localScale = new Vector3(3f, 6f, 12f);
        muro.GetComponent<Renderer>().material = matCanteraRosa;

        for (int f = 0; f < 3; f++)
        {
            for (int c = 0; c < 3; c++)
            {
                GameObject nicho = GameObject.CreatePrimitive(PrimitiveType.Cube);
                nicho.name = "Nicho_" + f + "_" + c;
                nicho.transform.SetParent(muro.transform, false);
                float x = miraAlEste ? 0.52f : -0.52f;
                float y = (f - 1) * 0.3f;
                float z = (c - 1) * 0.3f;
                nicho.transform.localPosition = new Vector3(x, y, z);
                nicho.transform.localScale = new Vector3(0.1f, 0.22f, 0.22f);
                nicho.GetComponent<Renderer>().material = matPiedraOscura;
                Destroy(nicho.GetComponent<Collider>());
            }
        }
    }

    void ConstruirMausoleo(Transform padre, Vector3 pos)
    {
        GameObject m = new GameObject("Mausoleo");
        m.transform.SetParent(padre, false);
        m.transform.localPosition = pos;

        GameObject cuerpo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cuerpo.transform.SetParent(m.transform, false);
        cuerpo.transform.localPosition = new Vector3(0, 2.2f, 0);
        cuerpo.transform.localScale = new Vector3(5f, 4.4f, 5f);
        cuerpo.GetComponent<Renderer>().material = matCanteraRosa;

        GameObject techo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        techo.transform.SetParent(m.transform, false);
        techo.transform.localPosition = new Vector3(0, 4.8f, 0);
        techo.transform.localScale = new Vector3(5.4f, 0.8f, 5.4f);
        techo.GetComponent<Renderer>().material = matPiedraOscura;

        GameObject cruz = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cruz.transform.SetParent(m.transform, false);
        cruz.transform.localPosition = new Vector3(0, 5.8f, 0);
        cruz.transform.localScale = new Vector3(0.25f, 1.4f, 0.25f);
        cruz.GetComponent<Renderer>().material = matCanteraRosa;
        Destroy(cruz.GetComponent<Collider>());
    }

    void ConstruirTumba(Transform padre, Vector3 pos)
    {
        GameObject t = new GameObject("Tumba");
        t.transform.SetParent(padre, false);
        t.transform.localPosition = pos;

        GameObject losa = GameObject.CreatePrimitive(PrimitiveType.Cube);
        losa.transform.SetParent(t.transform, false);
        losa.transform.localPosition = new Vector3(0, 0.2f, 0);
        losa.transform.localScale = new Vector3(1.3f, 0.4f, 2.4f);
        losa.GetComponent<Renderer>().material = matPiedraOscura;

        GameObject lapida = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lapida.transform.SetParent(t.transform, false);
        lapida.transform.localPosition = new Vector3(0, 0.85f, 1.1f);
        lapida.transform.localScale = new Vector3(1.1f, 1.3f, 0.28f);
        lapida.transform.localRotation = Quaternion.Euler(Random.Range(-5f, 5f), Random.Range(-4f, 4f), 0);
        lapida.GetComponent<Renderer>().material = matCanteraRosa;

        GameObject vela = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        vela.transform.SetParent(t.transform, false);
        vela.transform.localPosition = new Vector3(0.35f, 0.45f, 0.5f);
        vela.transform.localScale = new Vector3(0.14f, 0.18f, 0.14f);
        vela.GetComponent<Renderer>().material = matCempasuchil;
        Destroy(vela.GetComponent<Collider>());
    }

    void ConstruirNopalera(Transform padre, Vector3 pos)
    {
        GameObject nop = new GameObject("NopaleraSierra");
        nop.transform.SetParent(padre, false);
        nop.transform.localPosition = pos;

        for (int i = 0; i < 4; i++)
        {
            GameObject penca = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            penca.transform.SetParent(nop.transform, false);
            penca.transform.localPosition = new Vector3(Mathf.Sin(i * 1.5f) * 0.4f, 0.5f + i * 0.35f, Mathf.Cos(i * 1.5f) * 0.2f);
            penca.transform.localScale = new Vector3(0.6f, 0.8f, 0.15f);
            penca.transform.localRotation = Quaternion.Euler(0, i * 45f, Random.Range(-15f, 15f));
            penca.GetComponent<Renderer>().material = matFloraVerde;
            Destroy(penca.GetComponent<Collider>());

            // Tuna roja
            GameObject tuna = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tuna.transform.SetParent(penca.transform, false);
            tuna.transform.localPosition = new Vector3(0, 0.5f, 0);
            tuna.transform.localScale = Vector3.one * 0.25f;
            tuna.GetComponent<Renderer>().material = matCempasuchil;
            Destroy(tuna.GetComponent<Collider>());
        }
    }

    void ConstruirCipres(Transform padre, Vector3 pos)
    {
        GameObject cip = new GameObject("Cipres");
        cip.transform.SetParent(padre, false);
        cip.transform.localPosition = pos;

        GameObject tronco = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tronco.transform.SetParent(cip.transform, false);
        tronco.transform.localPosition = new Vector3(0, 1.8f, 0);
        tronco.transform.localScale = new Vector3(0.45f, 1.8f, 0.45f);
        tronco.GetComponent<Renderer>().material = matPiedraOscura;

        GameObject copa = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        copa.transform.SetParent(cip.transform, false);
        copa.transform.localPosition = new Vector3(0, 5.2f, 0);
        copa.transform.localScale = new Vector3(1.8f, 4.5f, 1.8f);
        copa.GetComponent<Renderer>().material = matFloraVerde;
    }

    void ConstruirFarolPanteon(Transform padre, Vector3 pos)
    {
        GameObject farol = new GameObject("FarolPanteon");
        farol.transform.SetParent(padre, false);
        farol.transform.localPosition = pos;

        GameObject poste = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        poste.transform.SetParent(farol.transform, false);
        poste.transform.localPosition = new Vector3(0, 2f, 0);
        poste.transform.localScale = new Vector3(0.16f, 2f, 0.16f);
        poste.GetComponent<Renderer>().material = matHerreria;

        GameObject cabina = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cabina.transform.SetParent(farol.transform, false);
        cabina.transform.localPosition = new Vector3(0, 4.1f, 0);
        cabina.transform.localScale = new Vector3(0.55f, 0.7f, 0.55f);
        cabina.GetComponent<Renderer>().material = matFuegoFatuo;
        Destroy(cabina.GetComponent<Collider>());

        Light l = farol.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(0.2f, 0.95f, 0.5f);
        l.range = 11f;
        l.intensity = 2.4f;
    }

    void ConstruirArenaMausoleoGeneral(Transform padre, Vector3 pos)
    {
        GameObject arena = new GameObject("ArenaMausoleoGeneral");
        arena.transform.SetParent(padre, false);
        arena.transform.localPosition = pos;

        // Plataforma monumental de cantera en la cima de la sierra
        GameObject plataforma = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        plataforma.transform.SetParent(arena.transform, false);
        plataforma.transform.localPosition = new Vector3(0, -0.3f, 0);
        plataforma.transform.localScale = new Vector3(32f, 0.6f, 32f);
        plataforma.GetComponent<Renderer>().material = matCanteraRosa;

        // 8 columnas de cantera
        for (int i = 0; i < 8; i++)
        {
            float angulo = i * (360f / 8f) * Mathf.Deg2Rad;
            Vector3 pCol = new Vector3(Mathf.Sin(angulo) * 14.5f, 0, Mathf.Cos(angulo) * 14.5f);
            GameObject col = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            col.transform.SetParent(arena.transform, false);
            col.transform.localPosition = pCol + Vector3.up * 3.5f;
            col.transform.localScale = new Vector3(1.3f, 3.5f, 1.3f);
            col.GetComponent<Renderer>().material = matCanteraRosa;
        }

        // Sepulcro del General
        GameObject cripta = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cripta.name = "SepulcroGeneral";
        cripta.transform.SetParent(arena.transform, false);
        cripta.transform.localPosition = new Vector3(0, 0.9f, 0);
        cripta.transform.localScale = new Vector3(4f, 1.8f, 6f);
        cripta.GetComponent<Renderer>().material = matCanteraRosa;

        // Instanciar Jefe
        GameObject bossGo = new GameObject("Jefe_GeneralMomia");
        bossGo.transform.position = arena.transform.position + Vector3.up * 0.2f;
        bossGo.AddComponent<JefeGeneralMomia>();

        // Portal hacia Nivel 3
        GameObject portal = new GameObject("PortalSalidaPanteon");
        portal.transform.position = arena.transform.position + Vector3.forward * 10f;
        portal.AddComponent<BoxCollider>().size = new Vector3(4f, 4f, 2f);
        portal.AddComponent<PortalVictoria>();
    }

    void ConstruirCapillaDonChuy(Transform padre, Vector3 pos)
    {
        GameObject capilla = new GameObject("Puesto_DonChuy_Panteon");
        capilla.transform.SetParent(padre, false);
        capilla.transform.localPosition = pos;

        GameObject casa = GameObject.CreatePrimitive(PrimitiveType.Cube);
        casa.transform.SetParent(capilla.transform, false);
        casa.transform.localPosition = new Vector3(0, 2.8f, 0);
        casa.transform.localScale = new Vector3(6.5f, 5.6f, 6.5f);
        casa.GetComponent<Renderer>().material = matCanteraRosa;

        capilla.AddComponent<ComercianteMercader>();
    }

    void ConstruirCirioColeccionable(Transform padre, Vector3 pos, string id)
    {
        GameObject totem = new GameObject("Totem_" + id);
        totem.transform.SetParent(padre, false);
        totem.transform.localPosition = pos;

        GameObject cirio = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cirio.transform.SetParent(totem.transform, false);
        cirio.transform.localScale = new Vector3(0.38f, 1.2f, 0.38f);
        cirio.GetComponent<Renderer>().material = matCanteraRosa;
        Destroy(cirio.GetComponent<Collider>());

        GameObject llama = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        llama.transform.SetParent(totem.transform, false);
        llama.transform.localPosition = new Vector3(0, 1.4f, 0);
        llama.transform.localScale = Vector3.one * 0.35f;
        llama.GetComponent<Renderer>().material = matFuegoFatuo;
        Destroy(llama.GetComponent<Collider>());

        SphereCollider sc = totem.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 1.3f;

        TotemColeccionable tc = totem.AddComponent<TotemColeccionable>();
        tc.idTotem = id;
        tc.valorTotem = 1;
    }

    void ConstruirAltarPanteon(Transform padre, Vector3 pos)
    {
        GameObject altar = new GameObject("AltarPanteon_SantaPaula");
        altar.transform.SetParent(padre, false);
        altar.transform.localPosition = pos;

        GameObject cruz = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cruz.transform.SetParent(altar.transform, false);
        cruz.transform.localPosition = new Vector3(0, 2.0f, 0);
        cruz.transform.localScale = new Vector3(0.35f, 3.2f, 0.35f);
        cruz.GetComponent<Renderer>().material = matHerreria;

        GameObject trav = GameObject.CreatePrimitive(PrimitiveType.Cube);
        trav.transform.SetParent(altar.transform, false);
        trav.transform.localPosition = new Vector3(0, 2.7f, 0);
        trav.transform.localScale = new Vector3(1.8f, 0.35f, 0.35f);
        trav.GetComponent<Renderer>().material = matHerreria;

        SphereCollider sc = altar.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 4.5f;

        PuntoControl pc = altar.AddComponent<PuntoControl>();
        pc.idCheckpoint = "Nivel2_AltarSantaPaula";
        pc.nombreAltar = "Altar de Santa Paula";
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
}
