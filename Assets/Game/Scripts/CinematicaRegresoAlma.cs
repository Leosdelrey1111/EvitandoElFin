using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Cinemática final de producción del Nivel 3 (Basílica de Guanajuato):
/// Al cruzar el Portal Dorado tras vencer al Clérigo Inquisidor, el alma del Chofer
/// regresa a su cuerpo real (prefab Chofer) en la camilla del hospital al amanecer.
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

    Material Mat(Color c, float emision = 0f, float suavidad = 0.35f)
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material m = new Material(s);
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", suavidad);
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

    GameObject InstanciarActorChofer(Transform padre, Vector3 posLocal)
    {
        GameObject prefab = Resources.Load<GameObject>("Chofer") ?? Resources.Load<GameObject>("Player");
        #if UNITY_EDITOR
        if (prefab == null)
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Resources/Chofer.prefab");
        #endif

        GameObject contenedor = new GameObject("ActorChoferReal");
        contenedor.transform.SetParent(padre, false);
        contenedor.transform.localPosition = posLocal;

        if (prefab != null)
        {
            GameObject inst = Instantiate(prefab, contenedor.transform, false);
            inst.name = "ModeloChofer";
            inst.tag = "Untagged";

            foreach (MonoBehaviour mb in inst.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb is Animator) continue;
                mb.enabled = false;
                Destroy(mb);
            }
            foreach (CharacterController cc in inst.GetComponentsInChildren<CharacterController>(true)) Destroy(cc);
            foreach (Collider col in inst.GetComponentsInChildren<Collider>(true)) Destroy(col);

            Animator anim = inst.GetComponentInChildren<Animator>();
            if (anim != null) anim.applyRootMotion = false;

            // Acostar boca arriba sobre el colchón (cabeza hacia +Z, pies hacia -Z)
            inst.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            inst.transform.localPosition = Vector3.zero;

            float minY = float.MaxValue;
            foreach (Renderer r in inst.GetComponentsInChildren<Renderer>())
                if (r.bounds.min.y < minY) minY = r.bounds.min.y;
            if (minY < float.MaxValue)
                inst.transform.position += Vector3.up * (contenedor.transform.position.y - minY);
        }
        else
        {
            Parte(contenedor.transform, "CuerpoRespaldo", PrimitiveType.Cube, new Vector3(0f, 0.14f, 0f), new Vector3(0.55f, 0.25f, 1.5f), Vector3.zero, Mat(new Color(0.32f, 0.65f, 0.68f)));
        }
        return contenedor;
    }

    IEnumerator Rutina(System.Action alTerminar)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        ChoferController choferCtrl = player != null ? player.GetComponent<ChoferController>() : null;
        if (choferCtrl != null) choferCtrl.controlBloqueado = true;

        CamaraTerceraPersona camTP = Object.FindAnyObjectByType<CamaraTerceraPersona>();
        if (camTP != null) camTP.enabled = false;

        HUDMexicano hud = Object.FindAnyObjectByType<HUDMexicano>();
        if (hud != null) hud.gameObject.SetActive(false);

        Camera cam = Camera.main;
        Vector3 posCamOrig = cam != null ? cam.transform.position : Vector3.zero;
        Quaternion rotCamOrig = cam != null ? cam.transform.rotation : Quaternion.identity;
        float fovOrig = cam != null ? cam.fieldOfView : 60f;

        GameObject sala = new GameObject("SalaHospital_RegresoAlma");
        sala.transform.position = new Vector3(0f, 320f, 0f);
        Transform r = sala.transform;

        Material pisoMat = Mat(new Color(0.76f, 0.78f, 0.80f), 0f, 0.6f);
        Material paredMat = Mat(new Color(0.86f, 0.88f, 0.86f), 0f, 0.2f);
        Material zocaloMat = Mat(new Color(0.28f, 0.42f, 0.46f));
        Material metalMat = Mat(new Color(0.65f, 0.68f, 0.72f), 0f, 0.75f);
        Material sabanaMat = Mat(new Color(0.92f, 0.94f, 0.96f));
        Material cobijaMat = Mat(new Color(0.24f, 0.52f, 0.58f));
        Material maderaMat = Mat(new Color(0.42f, 0.28f, 0.18f));
        Material cieloAmanecer = Mat(new Color(1f, 0.74f, 0.42f), 1.6f);
        Material oroMat = Mat(new Color(1f, 0.86f, 0.35f), 2.4f);
        Material almaMat = Mat(new Color(0.25f, 0.95f, 0.85f), 3.4f);
        Material monitorMat = Mat(new Color(0.06f, 0.12f, 0.14f));
        Material pulsoMat = Mat(new Color(0.18f, 0.95f, 0.48f), 2.8f);
        Material cempasuchilMat = Mat(new Color(1f, 0.58f, 0.08f), 0.7f);

        Parte(r, "Piso", PrimitiveType.Cube, new Vector3(0f, -0.15f, 0f), new Vector3(11f, 0.3f, 11f), Vector3.zero, pisoMat);
        Parte(r, "Techo", PrimitiveType.Cube, new Vector3(0f, 4.6f, 0f), new Vector3(11f, 0.25f, 11f), Vector3.zero, paredMat);
        Parte(r, "ParedFondo", PrimitiveType.Cube, new Vector3(0f, 2.25f, 4.5f), new Vector3(11f, 4.5f, 0.3f), Vector3.zero, paredMat);
        Parte(r, "ParedDer", PrimitiveType.Cube, new Vector3(4.5f, 2.25f, 0f), new Vector3(0.3f, 4.5f, 11f), Vector3.zero, paredMat);
        Parte(r, "ZocaloFondo", PrimitiveType.Cube, new Vector3(0f, 0.55f, 4.34f), new Vector3(10.8f, 1.1f, 0.04f), Vector3.zero, zocaloMat);

        Parte(r, "ParedIzqBaja", PrimitiveType.Cube, new Vector3(-4.2f, 0.6f, 0f), new Vector3(0.3f, 1.2f, 11f), Vector3.zero, paredMat);
        Parte(r, "ParedIzqAlta", PrimitiveType.Cube, new Vector3(-4.2f, 4.0f, 0f), new Vector3(0.3f, 1.2f, 11f), Vector3.zero, paredMat);
        Parte(r, "MarcoVentana1", PrimitiveType.Cube, new Vector3(-4.2f, 2.3f, -2.5f), new Vector3(0.35f, 2.4f, 0.2f), Vector3.zero, metalMat);
        Parte(r, "MarcoVentana2", PrimitiveType.Cube, new Vector3(-4.2f, 2.3f, 2.5f), new Vector3(0.35f, 2.4f, 0.2f), Vector3.zero, metalMat);
        Parte(r, "CieloAmanecer", PrimitiveType.Cube, new Vector3(-6.2f, 2.5f, 0f), new Vector3(0.2f, 5.5f, 12f), Vector3.zero, cieloAmanecer);

        Parte(r, "EstructuraCama", PrimitiveType.Cube, new Vector3(0f, 0.52f, 0.3f), new Vector3(1.35f, 0.14f, 2.6f), Vector3.zero, metalMat);
        for (int ix = -1; ix <= 1; ix += 2)
            for (int iz = -1; iz <= 1; iz += 2)
                Parte(r, "Pata" + ix + iz, PrimitiveType.Cylinder, new Vector3(ix * 0.58f, 0.26f, 0.3f + iz * 1.15f), new Vector3(0.08f, 0.26f, 0.08f), Vector3.zero, metalMat);
        Parte(r, "Cabecera", PrimitiveType.Cube, new Vector3(0f, 0.92f, 1.56f), new Vector3(1.4f, 0.72f, 0.08f), Vector3.zero, metalMat);
        Parte(r, "Piecera", PrimitiveType.Cube, new Vector3(0f, 0.75f, -0.96f), new Vector3(1.4f, 0.42f, 0.08f), Vector3.zero, metalMat);
        Parte(r, "BarandalI", PrimitiveType.Cube, new Vector3(-0.68f, 0.84f, 0.4f), new Vector3(0.05f, 0.22f, 1.35f), Vector3.zero, metalMat);
        Parte(r, "BarandalD", PrimitiveType.Cube, new Vector3(0.68f, 0.84f, 0.4f), new Vector3(0.05f, 0.22f, 1.35f), Vector3.zero, metalMat);
        Parte(r, "Colchon", PrimitiveType.Cube, new Vector3(0f, 0.68f, 0.3f), new Vector3(1.24f, 0.22f, 2.45f), Vector3.zero, sabanaMat);
        Parte(r, "Almohada", PrimitiveType.Cube, new Vector3(0f, 0.84f, 1.24f), new Vector3(0.75f, 0.12f, 0.42f), Vector3.zero, sabanaMat);
        Parte(r, "Cobija", PrimitiveType.Cube, new Vector3(0f, 0.82f, -0.32f), new Vector3(1.26f, 0.08f, 1.25f), Vector3.zero, cobijaMat);

        GameObject actorChofer = InstanciarActorChofer(r, new Vector3(0f, 0.80f, 0.35f));
        Parte(r, "RosarioPecho", PrimitiveType.Sphere, new Vector3(0f, 1.05f, 0.55f), new Vector3(0.14f, 0.04f, 0.20f), Vector3.zero, oroMat);

        Parte(r, "Mesita", PrimitiveType.Cube, new Vector3(1.35f, 0.4f, 1.0f), new Vector3(0.65f, 0.8f, 0.65f), Vector3.zero, maderaMat);
        Parte(r, "Florero", PrimitiveType.Cylinder, new Vector3(1.35f, 0.92f, 1.0f), new Vector3(0.18f, 0.12f, 0.18f), Vector3.zero, zocaloMat);
        Parte(r, "Cempasuchil", PrimitiveType.Sphere, new Vector3(1.35f, 1.08f, 1.0f), new Vector3(0.32f, 0.24f, 0.32f), Vector3.zero, cempasuchilMat);

        Parte(r, "PosteSuero", PrimitiveType.Cylinder, new Vector3(1.05f, 1.05f, 1.75f), new Vector3(0.05f, 1.05f, 0.05f), Vector3.zero, metalMat);
        Parte(r, "BolsaSuero", PrimitiveType.Cube, new Vector3(0.92f, 1.85f, 1.75f), new Vector3(0.14f, 0.28f, 0.08f), Vector3.zero, sabanaMat);

        Parte(r, "BaseMonitor", PrimitiveType.Cylinder, new Vector3(-1.15f, 0.7f, 1.35f), new Vector3(0.06f, 0.7f, 0.06f), Vector3.zero, metalMat);
        Parte(r, "CajaMonitor", PrimitiveType.Cube, new Vector3(-1.15f, 1.52f, 1.35f), new Vector3(0.55f, 0.42f, 0.22f), new Vector3(0f, 25f, 0f), monitorMat);
        GameObject barraPulso = Parte(r, "PulsoMonitor", PrimitiveType.Cube, new Vector3(-1.10f, 1.52f, 1.24f), new Vector3(0.38f, 0.05f, 0.03f), new Vector3(0f, 25f, 0f), pulsoMat);

        GameObject alma = Parte(r, "AlmaChofer", PrimitiveType.Sphere, new Vector3(-3.2f, 2.8f, 0.4f), Vector3.one * 0.42f, Vector3.zero, almaMat);
        Light luzAlma = alma.AddComponent<Light>();
        luzAlma.type = LightType.Point;
        luzAlma.color = new Color(0.35f, 1f, 0.88f);
        luzAlma.range = 10f;
        luzAlma.intensity = 3.5f;

        GameObject luzVentana = new GameObject("LuzAmanecer");
        luzVentana.transform.SetParent(r, false);
        luzVentana.transform.localPosition = new Vector3(-3.0f, 2.9f, 0.4f);
        Light lSol = luzVentana.AddComponent<Light>();
        lSol.type = LightType.Point;
        lSol.color = new Color(1f, 0.80f, 0.46f);
        lSol.range = 16f;
        lSol.intensity = 3.4f;

        GameObject luzTecho = new GameObject("LuzSala");
        luzTecho.transform.SetParent(r, false);
        luzTecho.transform.localPosition = new Vector3(0f, 4.1f, 0f);
        Light lTecho = luzTecho.AddComponent<Light>();
        lTecho.type = LightType.Point;
        lTecho.color = new Color(0.82f, 0.92f, 1f);
        lTecho.range = 15f;
        lTecho.intensity = 1.8f;

        Canvas canvas = UIMexicana.CrearCanvas("Canvas_RegresoAlma", 60);
        canvas.transform.SetParent(transform, false);
        Image fade = UIMexicana.Imagen("Fade", canvas.transform, UIMexicana.Blanco(), new Color(0f, 0f, 0f, 1f));
        UIMexicana.Estirar(fade.rectTransform);

        Image barraSup = UIMexicana.Imagen("BarraSup", canvas.transform, UIMexicana.Blanco(), Color.black);
        barraSup.rectTransform.anchorMin = new Vector2(0f, 1f); barraSup.rectTransform.anchorMax = new Vector2(1f, 1f);
        barraSup.rectTransform.pivot = new Vector2(0.5f, 1f); barraSup.rectTransform.sizeDelta = new Vector2(0f, 88f);

        Image barraInf = UIMexicana.Imagen("BarraInf", canvas.transform, UIMexicana.Blanco(), Color.black);
        barraInf.rectTransform.anchorMin = new Vector2(0f, 0f); barraInf.rectTransform.anchorMax = new Vector2(1f, 0f);
        barraInf.rectTransform.pivot = new Vector2(0.5f, 0f); barraInf.rectTransform.sizeDelta = new Vector2(0f, 108f);

        TextMeshProUGUI txt = UIMexicana.Texto("Subtitulo", barraInf.transform, "", 23, UIMexicana.Crema, TextAlignmentOptions.Center, FontStyles.Normal, 1f);
        UIMexicana.Estirar(txt.rectTransform);
        txt.rectTransform.offsetMin = new Vector2(80f, 10f); txt.rectTransform.offsetMax = new Vector2(-80f, -10f);

        AudioSource aud = gameObject.AddComponent<AudioSource>();
        aud.spatialBlend = 0f;
        AudioClip latido = UIMexicana.Pluck(new[] { 110f, 146.83f }, 0.35f, 0.14f);
        AudioClip despertar = UIMexicana.Pluck(new[] { 392f, 493.88f, 587.33f, 783.99f }, 0.9f, 0.14f);

        txt.text = "El Portal Dorado se cierra detrás de ti... el amanecer ilumina el mundo de los vivos.";
        float t = 0f;
        while (t < 3.8f)
        {
            t += Time.deltaTime;
            float e = Mathf.Clamp01(t / 3.8f);
            fade.color = new Color(0f, 0f, 0f, Mathf.Lerp(1f, 0f, Mathf.Clamp01(e * 2.2f)));
            alma.transform.localPosition = Vector3.Lerp(new Vector3(-3.2f, 2.7f, 0.4f), new Vector3(-0.8f, 2.1f, 0.45f), e);
            barraPulso.transform.localScale = new Vector3(0.38f, 0.04f + Mathf.Abs(Mathf.Sin(t * 4f)) * 0.08f, 0.03f);
            if (cam != null)
            {
                cam.transform.position = r.position + Vector3.Lerp(new Vector3(2.8f, 2.5f, -2.8f), new Vector3(2.1f, 2.0f, -1.6f), e);
                cam.transform.LookAt(r.position + new Vector3(0f, 1.0f, 0.45f));
                cam.fieldOfView = Mathf.Lerp(54f, 46f, e);
            }
            yield return null;
        }

        txt.text = "Tu corazón vuelve a latir con fuerza. Tu alma regresa a tu cuerpo...";
        if (aud != null && latido != null) aud.PlayOneShot(latido, 0.6f);
        t = 0f;
        while (t < 3.6f)
        {
            t += Time.deltaTime;
            float e = Mathf.Clamp01(t / 3.6f);
            float eSuave = e * e * (3f - 2f * e);
            alma.transform.localPosition = Vector3.Lerp(new Vector3(-0.8f, 2.1f, 0.45f), new Vector3(0f, 1.06f, 0.55f), eSuave);
            alma.transform.localScale = Vector3.one * Mathf.Lerp(0.42f, 0.16f, eSuave);
            luzAlma.intensity = Mathf.Lerp(3.5f, 6.5f, eSuave);
            barraPulso.transform.localScale = new Vector3(0.38f, 0.05f + Mathf.Abs(Mathf.Sin(t * 6f)) * 0.12f, 0.03f);
            if (cam != null)
            {
                cam.transform.position = r.position + Vector3.Lerp(new Vector3(2.1f, 2.0f, -1.6f), new Vector3(1.45f, 1.65f, 0.1f), eSuave);
                cam.transform.LookAt(r.position + new Vector3(0f, 0.95f, 0.6f));
                cam.fieldOfView = Mathf.Lerp(46f, 39f, eSuave);
            }
            yield return null;
        }

        alma.SetActive(false);
        if (aud != null && despertar != null) aud.PlayOneShot(despertar, 0.75f);
        txt.text = "¡Has evitado el fin! Despiertas con vida: tu familia te espera en Guanajuato.";
        t = 0f;
        while (t < 3.8f)
        {
            t += Time.deltaTime;
            float e = Mathf.Clamp01(t / 3.8f);
            fade.color = new Color(1f, 0.96f, 0.85f, Mathf.Lerp(0.8f, 0f, Mathf.Clamp01(e * 2.4f)));
            lSol.intensity = Mathf.Lerp(3.4f, 5.2f, e);
            barraPulso.transform.localScale = new Vector3(0.38f, 0.06f + Mathf.Abs(Mathf.Sin(t * 7f)) * 0.14f, 0.03f);
            actorChofer.transform.localRotation = Quaternion.Euler(Mathf.Lerp(0f, -18f, Mathf.Clamp01(e * 1.4f)), 0f, 0f);
            if (cam != null)
            {
                cam.transform.position = r.position + Vector3.Lerp(new Vector3(1.45f, 1.65f, 0.1f), new Vector3(1.65f, 1.85f, 1.25f), e);
                cam.transform.LookAt(r.position + new Vector3(0f, 1.05f, 0.85f));
            }
            yield return null;
        }

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
        Destroy(sala);
        Destroy(gameObject);

        alTerminar?.Invoke();
    }
}
