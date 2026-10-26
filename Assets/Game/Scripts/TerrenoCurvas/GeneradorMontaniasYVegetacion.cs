using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Generador del paisaje de Sierra Abierta y Despejada de Guanajuato:
/// - Colinas suaves y onduladas con pendientes amables (10°-18°) para máxima visibilidad y combate 360°.
/// - Sin muros opresivos ni precipicios verticales: la cámara de tercera persona orbita libremente sin atascos ni oclusiones.
/// - Paleta cálida y luminosa del Bajío guanajuatense: pastizales dorados, verde olivo/agave y suaves vetas de cantera rosa.
/// - Flora autóctona bien dosificada (agaves y nopales) en las orillas para dejar el campo de batalla despejado.
/// - Señalamientos oficiales SCT reglamentarios.
/// </summary>
public class GeneradorMontaniasYVegetacion : MonoBehaviour
{
    [Header("Sierra Abierta y Colinas Suaves")]
    [Tooltip("Altura máxima suave de las lomas serranas (ideal 6 a 9m para no tapar la cámara)")]
    public float alturaColinas = 7.5f;
    [Tooltip("Desnivel suave del valle lateral")]
    public float desnivelValle = 4.5f;
    [Tooltip("Anchura del horizonte visible a ambos lados")]
    public float anchuraPaisajeLateral = 105f;

    [Header("Vegetación y Señalética")]
    [Range(10, 45)]
    public int cantidadElementosVegetacion = 25;
    public bool agregarSenalesSCT = true;

    [Header("Materiales")]
    public Material materialSierraGuanajuato;
    public Material materialVegetacionVerde;
    public Material materialMagueyAzul;
    public Material materialTunaRoja;

    private GameObject contenedorPaisaje;
    private GameObject contenedorVegetacion;

    public void GenerarTodo(List<GeneradorCurvasGuanajuato.PuntoRuta> ruta, float bordeIzquierdoMontana, float bordeDerechoCanon)
    {
        Limpiar();
        AsegurarMateriales();

        contenedorPaisaje = new GameObject("Paisaje_Sierra_Guanajuato");
        contenedorPaisaje.transform.SetParent(transform, false);

        // 1. Construir colinas suaves y praderas abiertas para máxima visibilidad
        ConstruirSierraAbiertaDespejada(ruta, bordeIzquierdoMontana, bordeDerechoCanon);

        // 2. Señalamientos Oficiales SCT de Carretera Mexicana
        if (agregarSenalesSCT)
        {
            ConstruirSenalesMexicanasSCT(ruta, bordeIzquierdoMontana);
        }

        // 3. Vegetación Autóctona del Bajío (Magueyes y nopales dosificados en orillas)
        ConstruirVegetacionDelBajio(ruta, bordeIzquierdoMontana);

        Debug.Log("<color=#55FF55><b>[Evitando el Fin]</b> ¡Sierra abierta y colinas suaves de Guanajuato generadas con éxito (Máxima visibilidad de combate)!</color>");
    }

    public void Limpiar()
    {
        Transform p = transform.Find("Paisaje_Sierra_Guanajuato");
        if (p != null) DestruirObjeto(p.gameObject);

        Transform v = transform.Find("Vegetacion_Sierra_Guanajuato");
        if (v != null) DestruirObjeto(v.gameObject);

        Transform t = transform.Find("Tunel_Colonial_Guanajuato");
        if (t != null) DestruirObjeto(t.gameObject);

        Transform c = transform.Find("Pueblito_Guanajuato_Valle");
        if (c != null) DestruirObjeto(c.gameObject);
    }

    private void DestruirObjeto(GameObject go)
    {
        if (Application.isPlaying) Destroy(go);
        else DestroyImmediate(go);
    }

    private void AsegurarMateriales()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");

        // 1. Material Sierra Abierta con modulación de Vertex Colors (Pastizal dorado + Cantera rosa)
        if (materialSierraGuanajuato == null)
        {
            materialSierraGuanajuato = new Material(urpLit);
            materialSierraGuanajuato.name = "Mat_Sierra_Abierta_Guanajuato";
            materialSierraGuanajuato.SetColor("_BaseColor", Color.white);
            materialSierraGuanajuato.SetFloat("_Smoothness", 0.12f);
            materialSierraGuanajuato.SetTexture("_BaseMap", GenerarTexturaSierraBajio());
        }

        // 2. Nopal
        if (materialVegetacionVerde == null)
        {
            materialVegetacionVerde = new Material(urpLit);
            materialVegetacionVerde.name = "Mat_Nopal_Guanajuato";
            materialVegetacionVerde.SetColor("_BaseColor", new Color(0.30f, 0.56f, 0.30f));
            materialVegetacionVerde.SetFloat("_Smoothness", 0.2f);
        }

        // 3. Maguey
        if (materialMagueyAzul == null)
        {
            materialMagueyAzul = new Material(urpLit);
            materialMagueyAzul.name = "Mat_Maguey_Cenizo";
            materialMagueyAzul.SetColor("_BaseColor", new Color(0.34f, 0.52f, 0.48f));
            materialMagueyAzul.SetFloat("_Smoothness", 0.25f);
        }

        // 4. Tuna roja
        if (materialTunaRoja == null)
        {
            materialTunaRoja = new Material(urpLit);
            materialTunaRoja.name = "Mat_Tuna_Roja";
            materialTunaRoja.SetColor("_BaseColor", new Color(0.88f, 0.16f, 0.22f));
            materialTunaRoja.SetFloat("_Smoothness", 0.35f);
        }
    }

    /// <summary>
    /// Construye colinas abiertas, amplias y despejadas a ambos lados de la carretera.
    /// Pendientes suaves que jamás bloquean la cámara orbital en combate.
    /// </summary>
    private void ConstruirSierraAbiertaDespejada(List<GeneradorCurvasGuanajuato.PuntoRuta> ruta, float bordeIzq, float bordeDer)
    {
        // Paleta de Vertex Colors: El Bajío en época de sol (Pastizal dorado, tierra cálida y suave cantera)
        Color colPastizalDorado = new Color(0.78f, 0.74f, 0.54f);  // Lomas doradas soleadas
        Color colPastizalOlivo = new Color(0.56f, 0.64f, 0.46f);   // Zonas bajas y hondonadas
        Color colCanteraSuave = new Color(0.82f, 0.66f, 0.58f);    // Afloramientos sutiles de cantera
        Color colTepetateOrilla = new Color(0.72f, 0.56f, 0.42f);  // Tierra fértil del acotamiento

        // Flanco Izquierdo (Lomas suaves ascendentes)
        ConstruirFlancoColinas(ruta, true, bordeIzq, colPastizalDorado, colPastizalOlivo, colCanteraSuave, colTepetateOrilla, "Lomas_Serranas_Izquierda");

        // Flanco Derecho (Valle abierto suave)
        ConstruirFlancoColinas(ruta, false, bordeDer, colPastizalDorado, colPastizalOlivo, colCanteraSuave, colTepetateOrilla, "Valle_Abierto_Derecha");
    }

    private void ConstruirFlancoColinas(
        List<GeneradorCurvasGuanajuato.PuntoRuta> ruta,
        bool esLadoIzquierdo,
        float bordeCarretera,
        Color colDorado,
        Color colOlivo,
        Color colCantera,
        Color colOrilla,
        string nombreObjeto)
    {
        GameObject go = new GameObject(nombreObjeto);
        go.transform.SetParent(contenedorPaisaje.transform, false);

        MeshFilter mf = go.AddComponent<MeshFilter>();
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = materialSierraGuanajuato;

        int numPasos = ruta.Count;
        int perfiles = 7;
        Vector3[] verts = new Vector3[numPasos * perfiles];
        Vector2[] uvs = new Vector2[numPasos * perfiles];
        Color[] colors = new Color[numPasos * perfiles];
        List<int> tris = new List<int>();

        for (int i = 0; i < numPasos; i++)
        {
            GeneradorCurvasGuanajuato.PuntoRuta pr = ruta[i];
            Vector3 pos = pr.posicion;
            Vector3 r = esLadoIzquierdo ? -pr.derecha : pr.derecha;
            Vector3 up = pr.arriba;
            float distV = pr.distanciaAcumulada * 0.06f;

            // Ondulación suave y orgánica con Perlin noise
            float ruido = Mathf.PerlinNoise(pos.x * 0.02f + 10f, pos.z * 0.02f + 20f);
            float altoColina = alturaColinas + (ruido * 3.5f);
            float bajoValle = desnivelValle + (ruido * 2.0f);

            int bIdx = i * perfiles;

            if (esLadoIzquierdo)
            {
                // 0: Fondo de cuneta (conecta milimétricamente con el asfalto)
                verts[bIdx + 0] = pos + r * bordeCarretera - up * 0.14f;
                uvs[bIdx + 0] = new Vector2(0.0f, distV);
                colors[bIdx + 0] = colOrilla;

                // 1: Borde exterior de acotamiento
                verts[bIdx + 1] = pos + r * (bordeCarretera + 2.5f) + up * 0.22f;
                uvs[bIdx + 1] = new Vector2(0.10f, distV);
                colors[bIdx + 1] = colCantera;

                // 2: Falda baja suave (inclinación muy amable: 12°)
                verts[bIdx + 2] = pos + r * (bordeCarretera + 8.0f) + up * 1.5f;
                uvs[bIdx + 2] = new Vector2(0.30f, distV);
                colors[bIdx + 2] = colDorado;

                // 3: Ladera media de la colina
                verts[bIdx + 3] = pos + r * 22.0f + Vector3.up * (altoColina * 0.55f);
                uvs[bIdx + 3] = new Vector2(0.70f, distV);
                colors[bIdx + 3] = colDorado;

                // 4: Cima redondeada amplia (máxima cota suave: ~8m)
                verts[bIdx + 4] = pos + r * 45.0f + Vector3.up * altoColina;
                uvs[bIdx + 4] = new Vector2(1.20f, distV);
                colors[bIdx + 4] = colOlivo;

                // 5: Descenso posterior suave
                verts[bIdx + 5] = pos + r * 75.0f + Vector3.up * (altoColina * 0.40f);
                uvs[bIdx + 5] = new Vector2(1.80f, distV);
                colors[bIdx + 5] = colOlivo;

                // 6: Horizonte a nivel de suelo
                verts[bIdx + 6] = pos + r * anchuraPaisajeLateral + Vector3.up * 0f;
                uvs[bIdx + 6] = new Vector2(2.40f, distV);
                colors[bIdx + 6] = colOlivo;
            }
            else
            {
                // 0: Borde bajo la defensa metálica (conecta milimétricamente con el asfalto)
                verts[bIdx + 0] = pos + r * bordeCarretera - up * 0.02f;
                uvs[bIdx + 0] = new Vector2(0.0f, distV);
                colors[bIdx + 0] = colOrilla;

                // 1: Talud suave bajo la valla
                verts[bIdx + 1] = pos + r * (bordeCarretera + 2.5f) - Vector3.up * 0.55f;
                uvs[bIdx + 1] = new Vector2(0.10f, distV);
                colors[bIdx + 1] = colCantera;

                // 2: Descenso muy amable hacia el valle
                verts[bIdx + 2] = pos + r * (bordeCarretera + 8.0f) - Vector3.up * 1.6f;
                uvs[bIdx + 2] = new Vector2(0.30f, distV);
                colors[bIdx + 2] = colDorado;

                // 3: Llanura baja del valle
                verts[bIdx + 3] = pos + r * 22.0f - Vector3.up * (bajoValle * 0.70f);
                uvs[bIdx + 3] = new Vector2(0.70f, distV);
                colors[bIdx + 3] = colDorado;

                // 4: Fondo amplio y despejado
                verts[bIdx + 4] = pos + r * 45.0f - Vector3.up * bajoValle;
                uvs[bIdx + 4] = new Vector2(1.20f, distV);
                colors[bIdx + 4] = colOlivo;

                // 5: Pradera exterior
                verts[bIdx + 5] = pos + r * 75.0f - Vector3.up * (bajoValle * 0.50f);
                uvs[bIdx + 5] = new Vector2(1.80f, distV);
                colors[bIdx + 5] = colOlivo;

                // 6: Horizonte a nivel de suelo
                verts[bIdx + 6] = pos + r * anchuraPaisajeLateral + Vector3.up * 0f;
                uvs[bIdx + 6] = new Vector2(2.40f, distV);
                colors[bIdx + 6] = colOlivo;
            }

            if (i < numPasos - 1)
            {
                int nIdx = (i + 1) * perfiles;
                for (int s = 0; s < perfiles - 1; s++)
                {
                    int v0 = bIdx + s;
                    int v1 = bIdx + s + 1;
                    int v2 = nIdx + s;
                    int v3 = nIdx + s + 1;

                    tris.Add(v0); tris.Add(v2); tris.Add(v1);
                    tris.Add(v1); tris.Add(v2); tris.Add(v3);
                }
            }
        }

        Mesh m = new Mesh();
        m.name = nombreObjeto + "_Mesh";
        m.vertices = verts;
        m.uv = uvs;
        m.colors = colors;
        m.triangles = tris.ToArray();
        m.RecalculateNormals();

        mf.sharedMesh = m;

        MeshCollider mc = go.AddComponent<MeshCollider>();
        mc.sharedMesh = m;
    }

    private void ConstruirSenalesMexicanasSCT(List<GeneradorCurvasGuanajuato.PuntoRuta> ruta, float bordeIzquierdo)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");

        Material matSenalAmarilla = new Material(urpLit);
        matSenalAmarilla.name = "Mat_Senal_SCT_Amarilla";
        matSenalAmarilla.SetColor("_BaseColor", new Color(0.98f, 0.82f, 0.05f));
        matSenalAmarilla.SetFloat("_Smoothness", 0.65f);

        Material matPoste = new Material(urpLit);
        matPoste.SetColor("_BaseColor", new Color(0.78f, 0.78f, 0.82f));
        matPoste.SetFloat("_Metallic", 0.75f);

        int[] indicesSenales = new int[] { 8, 28, 55, 82 };
        string[] nombres = new string[] { "SCT_Curva_Peligrosa", "SCT_Zona_Curvas", "SCT_Curva_Cerrada", "SCT_Disminuya_Velocidad" };

        for (int s = 0; s < indicesSenales.Length; s++)
        {
            int idx = indicesSenales[s];
            if (idx >= ruta.Count - 6) continue;

            GeneradorCurvasGuanajuato.PuntoRuta pr = ruta[idx];
            Vector3 posPoste = pr.posicion - pr.derecha * (bordeIzquierdo - 0.25f);

            GameObject senalObj = new GameObject(nombres[s]);
            senalObj.transform.SetParent(contenedorPaisaje.transform, false);
            senalObj.transform.position = posPoste;
            senalObj.transform.rotation = Quaternion.LookRotation(-pr.adelante);

            GameObject poste = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            poste.transform.SetParent(senalObj.transform, false);
            poste.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            poste.transform.localScale = new Vector3(0.08f, 1.1f, 0.08f);
            DestruirCollider(poste);
            poste.GetComponent<Renderer>().sharedMaterial = matPoste;

            GameObject rombo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rombo.name = "Rombo_SCT";
            rombo.transform.SetParent(senalObj.transform, false);
            rombo.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            rombo.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            rombo.transform.localScale = new Vector3(0.75f, 0.75f, 0.04f);
            DestruirCollider(rombo);
            rombo.GetComponent<Renderer>().sharedMaterial = matSenalAmarilla;
        }
    }

    private void ConstruirVegetacionDelBajio(List<GeneradorCurvasGuanajuato.PuntoRuta> ruta, float bordeIzquierdo)
    {
        contenedorVegetacion = new GameObject("Vegetacion_Sierra_Guanajuato");
        contenedorVegetacion.transform.SetParent(transform, false);

        int paso = Mathf.Max(1, ruta.Count / cantidadElementosVegetacion);

        for (int i = 4; i < ruta.Count - 4; i += paso)
        {
            GeneradorCurvasGuanajuato.PuntoRuta pr = ruta[i];
            float distLateral = bordeIzquierdo + Random.Range(1.5f, 4.0f);
            Vector3 pos = pr.posicion - pr.derecha * distLateral + Vector3.up * 0.4f;

            if (Random.value > 0.45f)
            {
                CrearMaguey(pos, contenedorVegetacion.transform);
            }
            else
            {
                CrearNopalConTunas(pos, contenedorVegetacion.transform);
            }
        }
    }

    private void CrearMaguey(Vector3 pos, Transform padre)
    {
        GameObject maguey = new GameObject("Maguey_Guanajuato");
        maguey.transform.SetParent(padre, false);
        maguey.transform.position = pos;
        maguey.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        int numPencas = 6;
        for (int p = 0; p < numPencas; p++)
        {
            GameObject penca = GameObject.CreatePrimitive(PrimitiveType.Cube);
            penca.transform.SetParent(maguey.transform, false);

            float angulo = (360f / numPencas) * p;
            penca.transform.localRotation = Quaternion.Euler(38f, angulo, 0f);
            penca.transform.localPosition = Quaternion.Euler(0f, angulo, 0f) * new Vector3(0f, 0.30f, 0.32f);
            penca.transform.localScale = new Vector3(0.18f, 0.8f, 0.07f);

            DestruirCollider(penca);
            penca.GetComponent<Renderer>().sharedMaterial = materialMagueyAzul;
        }

        maguey.AddComponent<VientoVegetacion>();
        float escala = Random.Range(0.85f, 1.25f);
        maguey.transform.localScale = new Vector3(escala, escala, escala);
    }

    private void CrearNopalConTunas(Vector3 pos, Transform padre)
    {
        GameObject nopal = new GameObject("Nopal_Con_Tunas");
        nopal.transform.SetParent(padre, false);
        nopal.transform.position = pos;
        nopal.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        GameObject basePenca = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        basePenca.transform.SetParent(nopal.transform, false);
        basePenca.transform.localPosition = new Vector3(0f, 0.50f, 0f);
        basePenca.transform.localScale = new Vector3(0.55f, 0.32f, 0.12f);
        DestruirCollider(basePenca);
        basePenca.GetComponent<Renderer>().sharedMaterial = materialVegetacionVerde;

        for (int i = 0; i < 2; i++)
        {
            GameObject pencaHija = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pencaHija.transform.SetParent(nopal.transform, false);
            float dirX = i == 0 ? -0.32f : 0.32f;
            pencaHija.transform.localPosition = new Vector3(dirX, 1.05f, 0f);
            pencaHija.transform.localRotation = Quaternion.Euler(0f, 0f, i == 0 ? 28f : -28f);
            pencaHija.transform.localScale = new Vector3(0.44f, 0.28f, 0.10f);
            DestruirCollider(pencaHija);
            pencaHija.GetComponent<Renderer>().sharedMaterial = materialVegetacionVerde;

            GameObject tuna = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tuna.name = "Tuna_Roja";
            tuna.transform.SetParent(pencaHija.transform, false);
            tuna.transform.localPosition = new Vector3(0f, 0.90f, 0f);
            tuna.transform.localScale = new Vector3(0.32f, 0.40f, 0.32f);
            DestruirCollider(tuna);
            tuna.GetComponent<Renderer>().sharedMaterial = materialTunaRoja;
        }

        nopal.AddComponent<VientoVegetacion>();
        float escala = Random.Range(0.85f, 1.25f);
        nopal.transform.localScale = new Vector3(escala, escala, escala);
    }

    private void DestruirCollider(GameObject go)
    {
        Collider c = go.GetComponent<Collider>();
        if (c != null)
        {
            if (Application.isPlaying) Destroy(c);
            else DestroyImmediate(c);
        }
    }

    /// <summary>
    /// Textura luminosa y soleada del Bajío: suaves tonos dorados de pastizal seco y arenisca cálida.
    /// </summary>
    private Texture2D GenerarTexturaSierraBajio()
    {
        int res = 256;
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, true);
        tex.name = "Tex_Sierra_Bajio_Luminosa";
        tex.wrapMode = TextureWrapMode.Repeat;

        Color colDoradoCalido = new Color(0.88f, 0.82f, 0.68f); // Pastizal dorado soleado
        Color colPajaClara = new Color(0.94f, 0.88f, 0.74f);
        Color colTierraSuave = new Color(0.78f, 0.70f, 0.58f);

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float ruidoOndas = Mathf.Sin(x * 0.08f + y * 0.05f) * 0.08f;
                float granoTierra = (Mathf.PerlinNoise(x * 0.12f, y * 0.12f) - 0.5f) * 0.12f;
                float microDetalle = (Mathf.PerlinNoise(x * 0.28f, y * 0.28f) - 0.5f) * 0.05f;

                float val = 0.5f + ruidoOndas + granoTierra + microDetalle;
                Color c = Color.Lerp(colTierraSuave, colDoradoCalido, Mathf.Clamp01(val));
                if (val > 0.58f) c = Color.Lerp(c, colPajaClara, (val - 0.58f) * 2.2f);

                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return tex;
    }
}
