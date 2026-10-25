using UnityEngine;

/// <summary>
/// Señalamiento vial de Curva Peligrosa (Chevron) en las Curvas de Guanajuato.
/// Muestra flechas reflectantes que se iluminan secuencialmente ("chasing chevrons"),
/// guiando al Chofer en las curvas más cerradas de la sierra.
/// </summary>
public class SenalCurvaAnimada : MonoBehaviour
{
    [Header("Configuración de Señal")]
    public bool apuntarHaciaInterior = true;
    public int cantidadFlechas = 3;
    public float velocidadSecuencia = 3.5f;

    [Header("Colores")]
    public Color colorApagado = new Color(0.2f, 0.2f, 0.05f);
    public Color colorEncendido = new Color(1.2f, 0.95f, 0.1f); // Amarillo ámbar reflectante intenso

    private GameObject[] flechas;
    private Material[] matsFlechas;

    void Start()
    {
        ConstruirGeometriaSenal();
    }

    void ConstruirGeometriaSenal()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");

        // Poste vertical metálico
        GameObject poste = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        poste.name = "Poste_Soporte";
        poste.transform.SetParent(transform, false);
        poste.transform.localPosition = new Vector3(0, -0.8f, 0);
        poste.transform.localScale = new Vector3(0.12f, 0.8f, 0.12f);

        Collider colPoste = poste.GetComponent<Collider>();
        if (colPoste != null)
        {
            if (Application.isPlaying) Destroy(colPoste);
            else DestroyImmediate(colPoste);
        }

        // Tablero rectangular de fondo (negro/azul obscuro)
        GameObject tablero = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tablero.name = "Tablero_Fondo";
        tablero.transform.SetParent(transform, false);
        tablero.transform.localPosition = new Vector3(0, 0, 0);
        tablero.transform.localScale = new Vector3(1.4f, 0.75f, 0.08f);

        Collider colTablero = tablero.GetComponent<Collider>();
        if (colTablero != null)
        {
            if (Application.isPlaying) Destroy(colTablero);
            else DestroyImmediate(colTablero);
        }

        Material matTablero = new Material(urpLit);
        matTablero.SetColor("_BaseColor", new Color(0.12f, 0.12f, 0.14f));
        tablero.GetComponent<Renderer>().sharedMaterial = matTablero;

        // Flechas Chevrons reflectantes
        flechas = new GameObject[cantidadFlechas];
        matsFlechas = new Material[cantidadFlechas];

        float espacioX = 0.35f;
        float inicioX = -(cantidadFlechas - 1) * espacioX * 0.5f;
        if (!apuntarHaciaInterior) inicioX = -inicioX;

        for (int i = 0; i < cantidadFlechas; i++)
        {
            GameObject f = GameObject.CreatePrimitive(PrimitiveType.Cube);
            f.name = "Flecha_" + i;
            f.transform.SetParent(transform, false);
            float posX = inicioX + (apuntarHaciaInterior ? i * espacioX : -i * espacioX);
            f.transform.localPosition = new Vector3(posX, 0, -0.06f);
            f.transform.localScale = new Vector3(0.22f, 0.45f, 0.05f);

            // Inclinación en rombo/chevron
            f.transform.localRotation = Quaternion.Euler(0, 0, apuntarHaciaInterior ? 35f : -35f);

            Collider colF = f.GetComponent<Collider>();
            if (colF != null)
            {
                if (Application.isPlaying) Destroy(colF);
                else DestroyImmediate(colF);
            }

            Material matF = new Material(urpLit);
            matF.name = "Mat_Flecha_" + i;
            matF.EnableKeyword("_EMISSION");
            matF.SetColor("_BaseColor", colorApagado);
            matF.SetColor("_EmissionColor", colorApagado);
            f.GetComponent<Renderer>().material = matF;

            flechas[i] = f;
            matsFlechas[i] = matF;
        }
    }

    void Update()
    {
        if (matsFlechas == null || matsFlechas.Length == 0) return;

        // Efecto secuencial de encendido continuo (estilo carrera / autopista)
        float tiempo = Time.time * velocidadSecuencia;
        int pasoActivo = Mathf.FloorToInt(tiempo) % (cantidadFlechas + 1);

        for (int i = 0; i < cantidadFlechas; i++)
        {
            if (matsFlechas[i] == null) continue;

            bool encendido = (i == pasoActivo);
            Color colObjetivo = encendido ? colorEncendido : colorApagado;

            matsFlechas[i].SetColor("_BaseColor", encendido ? Color.yellow : colorApagado);
            matsFlechas[i].SetColor("_EmissionColor", colObjetivo);
        }
    }

    void OnDestroy()
    {
        if (matsFlechas != null && Application.isPlaying)
        {
            foreach (var m in matsFlechas)
            {
                if (m != null) Destroy(m);
            }
        }
    }
}
