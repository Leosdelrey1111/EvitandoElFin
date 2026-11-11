using UnityEngine;

/// <summary>
/// Personaje de la cinemática (paramédicos). Dos modos:
///  1) MODELO REAL: si existe un prefab en Assets/Game/Resources/Cinematica/&lt;nombre&gt; (por ejemplo "Paramedico" o "Paramedica", generado en Meshy),
///     se usa ese modelo. Si tiene Animator, se le mandan los parámetros "Caminando" (bool) y "Velocidad" (float) según se mueva el actor.
///  2) PROVISIONAL: un muñeco articulado con chaleco reflectante cuyas piernas y brazos se mueven solos al caminar.
/// Calcula su velocidad a partir de cuánto se desplaza, así que basta con mover el objeto.
/// </summary>
public class ActorCaminante : MonoBehaviour
{
    public Transform piernaI, piernaD, brazoI, brazoD, torso;
    [Tooltip("true = lleva la camilla: los brazos van al frente y casi no se balancean.")]
    public bool brazosAgarrando;
    [Tooltip("true = agarra la camilla por detrás (el que va al frente, caminando de frente).")]
    public bool brazosAtras;
    public Animator animator;
    public float amplitudPierna = 36f, amplitudBrazo = 26f;

    Vector3 ultimaPos;
    float velocidad, fase;
    bool tieneCaminando, tieneVelocidad;

    void Start()
    {
        ultimaPos = transform.position;
        if (animator != null && animator.runtimeAnimatorController != null)
            foreach (AnimatorControllerParameter p in animator.parameters)
            {
                if (p.name == "Caminando" && p.type == AnimatorControllerParameterType.Bool) tieneCaminando = true;
                if (p.name == "Velocidad" && p.type == AnimatorControllerParameterType.Float) tieneVelocidad = true;
            }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0.0001f) return;

        Vector3 d = transform.position - ultimaPos; d.y = 0f;
        velocidad = Mathf.Lerp(velocidad, d.magnitude / dt, 1f - Mathf.Exp(-9f * dt));
        ultimaPos = transform.position;

        if (animator != null)
        {
            if (tieneCaminando) animator.SetBool("Caminando", velocidad > 0.25f);
            if (tieneVelocidad) animator.SetFloat("Velocidad", velocidad);
            return;
        }

        // muñeco provisional: ciclo de paso proporcional a la distancia recorrida
        float k = Mathf.Clamp01(velocidad / 1.2f);
        fase += velocidad * dt * 3.4f;
        float s = Mathf.Sin(fase);

        if (piernaI != null) piernaI.localRotation = Quaternion.Euler(s * amplitudPierna * k, 0f, 0f);
        if (piernaD != null) piernaD.localRotation = Quaternion.Euler(-s * amplitudPierna * k, 0f, 0f);

        if (brazosAgarrando)
        {
            float b = Mathf.Sin(fase * 0.5f) * 3f * k;
            float ang = brazosAtras ? 70f : -72f;
            if (brazoI != null) brazoI.localRotation = Quaternion.Euler(ang + b, 0f, 8f);
            if (brazoD != null) brazoD.localRotation = Quaternion.Euler(ang - b, 0f, -8f);
        }
        else
        {
            if (brazoI != null) brazoI.localRotation = Quaternion.Euler(-s * amplitudBrazo * k, 0f, 4f);
            if (brazoD != null) brazoD.localRotation = Quaternion.Euler(s * amplitudBrazo * k, 0f, -4f);
        }

        // el torso sube y baja un poco con cada paso
        if (torso != null)
            torso.localPosition = new Vector3(0f, 1.32f + Mathf.Abs(s) * 0.025f * k, 0f);
    }

    // ================================================================== FÁBRICA

    /// <summary>Crea un paramédico. Usa el modelo de Resources/Cinematica/&lt;recurso&gt; si existe; si no, el muñeco provisional.</summary>
    public static GameObject Crear(string nombre, string recurso, Color uniforme, bool agarraCamilla)
    {
        GameObject real = string.IsNullOrEmpty(recurso) ? null : Resources.Load<GameObject>("Cinematica/" + recurso);
        if (real != null)
        {
            GameObject go = Instantiate(real);
            go.name = nombre;
            ActorCaminante a = go.AddComponent<ActorCaminante>();
            a.animator = go.GetComponentInChildren<Animator>();
            a.brazosAgarrando = agarraCamilla;
            return go;
        }
        return CrearProvisional(nombre, uniforme, agarraCamilla);
    }

    static Material Mat(Color c, float suavidad = 0.25f)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        Material m = new Material(sh);
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", suavidad);
        return m;
    }

    static GameObject Parte(PrimitiveType tipo, string nombre, Transform padre, Vector3 pos, Vector3 escala, Material mat)
    {
        GameObject g = GameObject.CreatePrimitive(tipo);
        g.name = nombre;
        g.transform.SetParent(padre, false);
        g.transform.localPosition = pos;
        g.transform.localScale = escala;
        g.GetComponent<Renderer>().sharedMaterial = mat;
        Collider c = g.GetComponent<Collider>();
        if (c != null) Destroy(c);
        return g;
    }

    static GameObject CrearProvisional(string nombre, Color uniforme, bool agarraCamilla)
    {
        GameObject root = new GameObject(nombre);
        ActorCaminante a = root.AddComponent<ActorCaminante>();
        a.brazosAgarrando = agarraCamilla;

        Material tela = Mat(uniforme, 0.3f);
        Material pantalon = Mat(new Color(uniforme.r * 0.45f, uniforme.g * 0.45f, uniforme.b * 0.45f), 0.25f);
        Material chaleco = Mat(new Color(1f, 0.62f, 0.05f), 0.4f);
        Material reflejante = Mat(new Color(0.9f, 0.92f, 0.85f), 0.8f);
        Material piel = Mat(new Color(0.78f, 0.58f, 0.45f), 0.3f);
        Material negro = Mat(new Color(0.05f, 0.05f, 0.06f), 0.4f);
        Material gorra = Mat(new Color(0.08f, 0.14f, 0.12f), 0.3f);

        // torso con chaleco reflectante
        GameObject torso = Parte(PrimitiveType.Cube, "Torso", root.transform, new Vector3(0f, 1.32f, 0f), new Vector3(0.5f, 0.62f, 0.28f), tela);
        Parte(PrimitiveType.Cube, "Chaleco", torso.transform, new Vector3(0f, 0f, 0.02f), new Vector3(1.04f, 0.98f, 1.06f), chaleco);
        Parte(PrimitiveType.Cube, "FranjaH", torso.transform, new Vector3(0f, -0.12f, 0.03f), new Vector3(1.06f, 0.1f, 1.08f), reflejante);
        Parte(PrimitiveType.Cube, "FranjaV1", torso.transform, new Vector3(-0.25f, 0.2f, 0.03f), new Vector3(0.1f, 0.5f, 1.08f), reflejante);
        Parte(PrimitiveType.Cube, "FranjaV2", torso.transform, new Vector3(0.25f, 0.2f, 0.03f), new Vector3(0.1f, 0.5f, 1.08f), reflejante);
        a.torso = torso.transform;

        // cabeza con gorra
        GameObject cabeza = Parte(PrimitiveType.Sphere, "Cabeza", root.transform, new Vector3(0f, 1.78f, 0f), Vector3.one * 0.25f, piel);
        Parte(PrimitiveType.Cylinder, "Gorra", cabeza.transform, new Vector3(0f, 0.38f, 0.04f), new Vector3(1.05f, 0.18f, 1.05f), gorra);
        Parte(PrimitiveType.Cube, "Visera", cabeza.transform, new Vector3(0f, 0.26f, 0.55f), new Vector3(0.9f, 0.06f, 0.5f), gorra);

        // piernas (pivote en la cadera)
        a.piernaI = Pierna(root.transform, "PiernaI", -0.12f, pantalon, negro);
        a.piernaD = Pierna(root.transform, "PiernaD", 0.12f, pantalon, negro);

        // brazos (pivote en el hombro)
        a.brazoI = Brazo(root.transform, "BrazoI", -0.33f, tela, piel);
        a.brazoD = Brazo(root.transform, "BrazoD", 0.33f, tela, piel);
        return root;
    }

    static Transform Pierna(Transform padre, string nombre, float x, Material pantalon, Material bota)
    {
        GameObject pivote = new GameObject(nombre);
        pivote.transform.SetParent(padre, false);
        pivote.transform.localPosition = new Vector3(x, 0.98f, 0f);
        Parte(PrimitiveType.Capsule, "Muslo", pivote.transform, new Vector3(0f, -0.42f, 0f), new Vector3(0.18f, 0.44f, 0.18f), pantalon);
        Parte(PrimitiveType.Cube, "Bota", pivote.transform, new Vector3(0f, -0.92f, 0.05f), new Vector3(0.18f, 0.12f, 0.3f), bota);
        return pivote.transform;
    }

    static Transform Brazo(Transform padre, string nombre, float x, Material manga, Material piel)
    {
        GameObject pivote = new GameObject(nombre);
        pivote.transform.SetParent(padre, false);
        pivote.transform.localPosition = new Vector3(x, 1.58f, 0f);
        Parte(PrimitiveType.Capsule, "Brazo", pivote.transform, new Vector3(0f, -0.28f, 0f), new Vector3(0.12f, 0.3f, 0.12f), manga);
        Parte(PrimitiveType.Sphere, "Mano", pivote.transform, new Vector3(0f, -0.6f, 0f), Vector3.one * 0.12f, piel);
        return pivote.transform;
    }
}
