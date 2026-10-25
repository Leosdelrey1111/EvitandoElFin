using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Genera bancos de niebla baja y bruma suave en las curvas y cañadas de Guanajuato.
/// Utiliza texturas radiales difusas para que no se vean bordes poligonales ni cuadros planos.
/// </summary>
public class NieblaSierraAnimada : MonoBehaviour
{
    [Header("Configuración de la Niebla")]
    public int cantidadBancosNiebla = 8;
    public float velocidadOndulacion = 0.6f;
    public float amplitudOndulacion = 0.4f;
    public Color colorNiebla = new Color(0.75f, 0.85f, 0.95f, 0.18f); // Bruma suave y translúcida

    private List<Transform> bancosTransform = new List<Transform>();
    private List<Vector3> posicionesBase = new List<Vector3>();
    private List<float> desfases = new List<float>();

    public void GenerarBancosDeNiebla(List<GeneradorCurvasGuanajuato.PuntoRuta> ruta, float distanciaLateral)
    {
        Shader shaderTransp = Shader.Find("Universal Render Pipeline/Lit");
        if (shaderTransp == null) shaderTransp = Shader.Find("Standard");

        Material matNiebla = new Material(shaderTransp);
        matNiebla.name = "Mat_Niebla_Sierra_Suave";

        // Configuración de transparencia suave en URP
        matNiebla.SetFloat("_Surface", 1); // Transparent
        matNiebla.SetFloat("_Blend", 0);   // Alpha blend
        matNiebla.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        matNiebla.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        matNiebla.SetInt("_ZWrite", 0);
        matNiebla.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        matNiebla.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        matNiebla.SetColor("_BaseColor", colorNiebla);
        matNiebla.SetFloat("_Smoothness", 0f);

        // Textura circular con desvanecimiento radial suave para eliminar bordes cuadrados
        Texture2D texBrumaSuave = GenerarTexturaBrumaCircular();
        matNiebla.SetTexture("_BaseMap", texBrumaSuave);

        int intervalo = Mathf.Max(1, ruta.Count / cantidadBancosNiebla);

        for (int i = 0; i < ruta.Count; i += intervalo)
        {
            GeneradorCurvasGuanajuato.PuntoRuta pr = ruta[i];

            // Crear planos de bruma en los valles a ambos lados
            CrearCapaNiebla(pr.posicion - pr.derecha * (distanciaLateral + 6f) - Vector3.up * 2.5f, matNiebla);
            CrearCapaNiebla(pr.posicion + pr.derecha * (distanciaLateral + 6f) - Vector3.up * 2.5f, matNiebla);
        }
    }

    private void CrearCapaNiebla(Vector3 pos, Material mat)
    {
        GameObject nieblaObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        nieblaObj.name = "Bruma_Sierra";
        nieblaObj.transform.SetParent(transform, false);
        nieblaObj.transform.position = pos;
        nieblaObj.transform.rotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);

        float escala = Random.Range(22f, 34f);
        nieblaObj.transform.localScale = new Vector3(escala, escala, 1f);

        Collider c = nieblaObj.GetComponent<Collider>();
        if (c != null)
        {
            if (Application.isPlaying) Destroy(c);
            else DestroyImmediate(c);
        }

        nieblaObj.GetComponent<Renderer>().sharedMaterial = mat;

        bancosTransform.Add(nieblaObj.transform);
        posicionesBase.Add(pos);
        desfases.Add(Random.Range(0f, 10f));
    }

    void Update()
    {
        float t = Time.time * velocidadOndulacion;

        for (int i = 0; i < bancosTransform.Count; i++)
        {
            if (bancosTransform[i] == null) continue;

            float desfase = desfases[i];
            Vector3 pos = posicionesBase[i];
            pos.y += Mathf.Sin(t + desfase) * amplitudOndulacion;
            pos.x += Mathf.Cos((t + desfase) * 0.4f) * 0.25f;
            bancosTransform[i].position = pos;

            bancosTransform[i].Rotate(0f, 0f, 0.8f * Time.deltaTime);
        }
    }

    private Texture2D GenerarTexturaBrumaCircular()
    {
        int res = 128;
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, true);
        tex.name = "Tex_Bruma_Radial";
        tex.wrapMode = TextureWrapMode.Clamp;
        Vector2 centro = new Vector2(res * 0.5f, res * 0.5f);
        float radioMax = res * 0.5f;

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), centro) / radioMax;
                float alpha = Mathf.Clamp01(1f - dist);
                // Caída suave no lineal para que no haya líneas cuadradas visibles
                alpha = Mathf.SmoothStep(0f, 1f, alpha);
                alpha = Mathf.Pow(alpha, 2.2f);

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        return tex;
    }
}
