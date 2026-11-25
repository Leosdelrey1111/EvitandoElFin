using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Cinemática final del Nivel 3 (Basílica de Guanajuato):
/// Al cruzar el Portal Dorado tras vencer al Clérigo Inquisidor, el alma del Chofer
/// regresa a su cuerpo en el mundo real bajo la luz del amanecer y despierta vivo.
/// </summary>
public class CinematicaRegresoAlma : MonoBehaviour
{
    public static bool Reproducir(System.Action alTerminar)
    {
        if (Object.FindAnyObjectByType<CinematicaRegresoAlma>() != null) return false;
        GameObject go = new GameObject("Cinematica_RegresoAlma");
        CinematicaRegresoAlma c = go.AddComponent<CinematicaRegresoAlma>();
        c.StartCoroutine(c.Rutina(alTerminar));
        return true;
    }

    Material Mat(Color c, float emision = 0f)
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material m = new Material(s);
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (emision > 0f)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * emision);
        }
        return m;
    }

    GameObject Parte(Transform p, string n, PrimitiveType t, Vector3 pos, Vector3 esc, Vector3 rot, Material m)
    {
        GameObject g = GameObject.CreatePrimitive(t);
        g.name = n;
        g.transform.SetParent(p, false);
        g.transform.localPosition = pos;
        g.transform.localRotation = Quaternion.Euler(rot);
        g.transform.localScale = esc;
        g.GetComponent<Renderer>().sharedMaterial = m;
        Collider c = g.GetComponent<Collider>();
        if (c != null) Destroy(c);
        return g;
    }

    IEnumerator Rutina(System.Action alTerminar)
    {
        // Pausar control del jugador y ocultar HUD
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        ChoferController chofer = player != null ? player.GetComponent<ChoferController>() : null;
        if (chofer != null) chofer.controlBloqueado = true;

        CamaraTerceraPersona camTP = Object.FindAnyObjectByType<CamaraTerceraPersona>();
        if (camTP != null) camTP.enabled = false;

        HUDMexicano hud = Object.FindAnyObjectByType<HUDMexicano>();
        if (hud != null) hud.gameObject.SetActive(false);

        Camera cam = Camera.main;
        Vector3 posCamOrig = cam != null ? cam.transform.position : Vector3.zero;
        Quaternion rotCamOrig = cam != null ? cam.transform.rotation : Quaternion.identity;
        float fovOrig = cam != null ? cam.fieldOfView : 60f;

        // Escenario aislado en lo alto (y = 320) para la escena del despertar al amanecer
        GameObject escenario = new GameObject("Escenario_RegresoAlma");
        escenario.transform.position = new Vector3(0f, 320f, 0f);
        Transform r = escenario.transform;

        Material pisoMat = Mat(new Color(0.22f, 0.24f, 0.28f));
        Material sabanaMat = Mat(new Color(0.88f, 0.90f, 0.94f));
        Material metalMat = Mat(new Color(0.55f, 0.58f, 0.62f));
        Material pielMat = Mat(new Color(0.84f, 0.64f, 0.50f));
        Material camisaMat = Mat(new Color(0.32f, 0.65f, 0.68f));
        Material pantalonMat = Mat(new Color(0.18f, 0.24f, 0.34f));
        Material gorraMat = Mat(new Color(0.16f, 0.42f, 0.72f));
        Material oroMat = Mat(new Color(1f, 0.85f, 0.35f), 2.2f);
        Material almaMat = Mat(new Color(0.25f, 0.95f, 0.85f), 3.2f);
        Material monitorMat = Mat(new Color(0.15f, 0.95f, 0.45f), 2.0f);

        Parte(r, "Piso", PrimitiveType.Cube, new Vector3(0f, -0.25f, 0f), new Vector3(24f, 0.5f, 24f), Vector3.zero, pisoMat);

        // Camilla de urgencias / hospital al amanecer
        Parte(r, "BaseCamilla", PrimitiveType.Cube, new Vector3(0f, 0.55f, 0f), new Vector3(1.2f, 0.14f, 2.5f), Vector3.zero, metalMat);
        Parte(r, "Colchon", PrimitiveType.Cube, new Vector3(0f, 0.70f, 0f), new Vector3(1.12f, 0.18f, 2.38f), Vector3.zero, sabanaMat);
        Parte(r, "Almohada", PrimitiveType.Cube, new Vector3(0f, 0.84f, 0.9f), new Vector3(0.7f, 0.12f, 0.4f), Vector3.zero, sabanaMat);

        // Cuerpo del Chofer acostado
        Transform cuerpo = new GameObject("CuerpoChofer").transform;
        cuerpo.SetParent(r, false);
        cuerpo.localPosition = new Vector3(0f, 0.86f, 0f);
        Parte(cuerpo, "Piernas", PrimitiveType.Cube, new Vector3(0f, 0.05f, -0.55f), new Vector3(0.42f, 0.2f, 0.9f), Vector3.zero, pantalonMat);
        Parte(cuerpo, "Torso", PrimitiveType.Cube, new Vector3(0f, 0.10f, 0.20f), new Vector3(0.52f, 0.24f, 0.7f), Vector3.zero, camisaMat);
        Parte(cuerpo, "Cabeza", PrimitiveType.Sphere, new Vector3(0f, 0.14f, 0.82f), new Vector3(0.36f, 0.36f, 0.36f), Vector3.zero, pielMat);
        Parte(cuerpo, "Rosario", PrimitiveType.Sphere, new Vector3(0f, 0.24f, 0.28f), new Vector3(0.12f, 0.04f, 0.18f), Vector3.zero, oroMat);
        Parte(cuerpo, "GorraMesita", PrimitiveType.Sphere, new Vector3(0.88f, 0.0f, 0.85f), new Vector3(0.28f, 0.14f, 0.28f), Vector3.zero, gorraMat);

        // Monitor de signos vitales junto a la camilla
        Parte(r, "PosteMonitor", PrimitiveType.Cylinder, new Vector3(-0.95f, 0.7f, 0.7f), new Vector3(0.08f, 0.7f, 0.08f), Vector3.zero, metalMat);
        Parte(r, "PantallaMonitor", PrimitiveType.Cube, new Vector3(-0.95f, 1.45f, 0.7f), new Vector3(0.45f, 0.32f, 0.15f), new Vector3(0f, 25f, 0f), monitorMat);

        // Orbe luminoso del alma que desciende del cielo hacia el pecho del Chofer
        GameObject alma = Parte(r, "AlmaChofer", PrimitiveType.Sphere, new Vector3(0f, 9f, -4f), Vector3.one * 0.45f, Vector3.zero, almaMat);
        Light luzAlma = alma.AddComponent<Light>();
        luzAlma.type = LightType.Point;
        luzAlma.color = new Color(0.35f, 1f, 0.88f);
        luzAlma.range = 10f;
        luzAlma.intensity = 3.5f;

        // Luz cálida de amanecer
        GameObject luzSol = new GameObject("LuzAmanecer");
        luzSol.transform.SetParent(r, false);
        luzSol.transform.localPosition = new Vector3(4f, 5f, 3f);
        Light lSol = luzSol.AddComponent<Light>();
        lSol.type = LightType.Point;
        lSol.color = new Color(1f, 0.82f, 0.48f);
        lSol.range = 25f;
        lSol.intensity = 2.8f;

        // UI cinematográfica (franjas negras + subtítulo)
        Canvas canvas = UIMexicana.CrearCanvas("Canvas_RegresoAlma", 60);
        canvas.transform.SetParent(transform, false);
        Image fade = UIMexicana.Imagen("Fade", canvas.transform, UIMexicana.Blanco(), new Color(0f, 0f, 0f, 1f));
        UIMexicana.Estirar(fade.rectTransform);

        Image barraSup = UIMexicana.Imagen("BarraSup", canvas.transform, UIMexicana.Blanco(), Color.black);
        barraSup.rectTransform.anchorMin = new Vector2(0f, 1f); barraSup.rectTransform.anchorMax = new Vector2(1f, 1f);
        barraSup.rectTransform.pivot = new Vector2(0.5f, 1f); barraSup.rectTransform.sizeDelta = new Vector2(0f, 90f);

        Image barraInf = UIMexicana.Imagen("BarraInf", canvas.transform, UIMexicana.Blanco(), Color.black);
        barraInf.rectTransform.anchorMin = new Vector2(0f, 0f); barraInf.rectTransform.anchorMax = new Vector2(1f, 0f);
        barraInf.rectTransform.pivot = new Vector2(0.5f, 0f); barraInf.rectTransform.sizeDelta = new Vector2(0f, 110f);

        TextMeshProUGUI txt = UIMexicana.Texto("Subtitulo", barraInf.transform, "", 24, UIMexicana.Crema, TextAlignmentOptions.Center, FontStyles.Normal, 1f);
        UIMexicana.Estirar(txt.rectTransform);
        txt.rectTransform.offsetMin = new Vector2(80f, 10f); txt.rectTransform.offsetMax = new Vector2(-80f, -10f);

        // Sonido suave de latido
        AudioSource aud = gameObject.AddComponent<AudioSource>();
        aud.spatialBlend = 0f;
        AudioClip latido = UIMexicana.Pluck(new[] { 110f, 146.83f }, 0.35f, 0.14f);
        AudioClip despertar = UIMexicana.Pluck(new[] { 392f, 493.88f, 587.33f, 783.99f }, 0.9f, 0.14f);

        // Toma 1: Desvanecimiento dorado y llegada al amanecer (3.5s)
        txt.text = "El Portal Dorado se cierra detrás de ti... el camino de regreso por fin se abre.";
        float t = 0f;
        while (t < 3.5f)
        {
            t += Time.deltaTime;
            float e = Mathf.Clamp01(t / 3.5f);
            fade.color = new Color(0f, 0f, 0f, Mathf.Lerp(1f, 0f, Mathf.Clamp01(e * 2f)));
            alma.transform.localPosition = Vector3.Lerp(new Vector3(0f, 7.5f, -3.5f), new Vector3(0f, 3.2f, -0.5f), e);
            if (cam != null)
            {
                cam.transform.position = r.position + Vector3.Lerp(new Vector3(3.4f, 3.2f, -3.5f), new Vector3(2.4f, 2.2f, -1.6f), e);
                cam.transform.LookAt(cuerpo.position + Vector3.up * 0.4f);
                cam.fieldOfView = Mathf.Lerp(54f, 46f, e);
            }
            yield return null;
        }

        // Toma 2: El alma desciende hasta el pecho del Chofer (3.5s)
        txt.text = "El latido de tu corazón te llama. Tu alma desciende de vuelta a tu cuerpo...";
        if (aud != null && latido != null) aud.PlayOneShot(latido, 0.55f);
        t = 0f;
        while (t < 3.5f)
        {
            t += Time.deltaTime;
            float e = Mathf.Clamp01(t / 3.5f);
            float eSuave = e * e * (3f - 2f * e);
            alma.transform.localPosition = Vector3.Lerp(new Vector3(0f, 3.2f, -0.5f), new Vector3(0f, 1.05f, 0.25f), eSuave);
            alma.transform.localScale = Vector3.one * Mathf.Lerp(0.45f, 0.18f, eSuave);
            luzAlma.intensity = Mathf.Lerp(3.5f, 6.0f, eSuave);
            if (cam != null)
            {
                cam.transform.position = r.position + Vector3.Lerp(new Vector3(2.4f, 2.2f, -1.6f), new Vector3(1.5f, 1.85f, 0.2f), eSuave);
                cam.transform.LookAt(cuerpo.position + new Vector3(0f, 0.2f, 0.35f));
                cam.fieldOfView = Mathf.Lerp(46f, 40f, eSuave);
            }
            yield return null;
        }

        // Toma 3: Destello al entrar el alma y el Chofer despierta (3.5s)
        alma.SetActive(false);
        if (aud != null && despertar != null) aud.PlayOneShot(despertar, 0.7f);
        txt.text = "¡Has evitado el fin! Despiertas con vida: tu familia te espera en Guanajuato.";
        t = 0f;
        while (t < 3.5f)
        {
            t += Time.deltaTime;
            float e = Mathf.Clamp01(t / 3.5f);
            fade.color = new Color(1f, 0.96f, 0.85f, Mathf.Lerp(0.75f, 0f, Mathf.Clamp01(e * 2.5f)));
            lSol.intensity = Mathf.Lerp(2.8f, 4.5f, e);
            // El Chofer se incorpora ligeramente al despertar
            cuerpo.localRotation = Quaternion.Euler(Mathf.Lerp(0f, -18f, Mathf.Clamp01(e * 1.4f)), 0f, 0f);
            if (cam != null)
            {
                cam.transform.position = r.position + Vector3.Lerp(new Vector3(1.5f, 1.85f, 0.2f), new Vector3(1.9f, 2.1f, 1.3f), e);
                cam.transform.LookAt(cuerpo.position + new Vector3(0f, 0.35f, 0.65f));
            }
            yield return null;
        }

        // Fundido final a negro antes de mostrar la pantalla de victoria
        t = 0f;
        while (t < 0.8f)
        {
            t += Time.deltaTime;
            fade.color = new Color(0f, 0f, 0f, Mathf.Clamp01(t / 0.8f));
            yield return null;
        }

        if (cam != null)
        {
            cam.transform.position = posCamOrig;
            cam.transform.rotation = rotCamOrig;
            cam.fieldOfView = fovOrig;
        }
        Destroy(escenario);
        Destroy(gameObject);

        alTerminar?.Invoke();
    }
}
