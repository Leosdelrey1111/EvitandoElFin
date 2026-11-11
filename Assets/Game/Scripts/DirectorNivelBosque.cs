using UnityEngine;

/// <summary>
/// Director del Nivel 1 (Bosque): al empezar reparte almas por el bosque y hace aparecer los esbirros
/// errantes en los puntos marcados en la escena (objetos hijos con prefijo SpawnAlma_, SpawnSerpiente_, SpawnPiedra_).
/// Mantiene los marcadores en la escena (editables a mano) y crea las entidades en runtime para no pesar en el archivo de escena.
/// </summary>
public class DirectorNivelBosque : MonoBehaviour
{
    [Header("Marcadores")]
    public string prefijoAlma = "SpawnAlma_";
    public string prefijoSerpiente = "SpawnSerpiente_";
    public string prefijoPiedra = "SpawnPiedra_";
    public string prefijoComadre = "SpawnComadre_";

    [Header("Reaparición de almas")]
    [Tooltip("Cada cuántos segundos reaparece un alma en un marcador libre (0 = nunca).")]
    public float segundosReaparecerAlma = 25f;
    public int maxAlmasEnNivel = 40;

    [TextArea] public string mensajeObjetivo = "Objetivo: reúne los 3 tótems y cruza el portal espiritual.";

    private Transform[] marcadoresAlma;
    private float proximaReaparicion;

    void Start()
    {
        int almas = 0, serpientes = 0, piedras = 0, comadres = 0;
        var almasMarcadores = new System.Collections.Generic.List<Transform>();

        foreach (Transform m in transform)
        {
            Vector3 suelo;
            if (m.name.StartsWith(prefijoAlma))
            {
                almasMarcadores.Add(m);
                if (UtilTerreno.SueloEn(m.position.x, m.position.z, out suelo))
                {
                    AlmaRecolectable.Crear(suelo + Vector3.up * 1.0f, false, false);
                    almas++;
                }
            }
            else if (m.name.StartsWith(prefijoSerpiente))
            {
                if (UtilTerreno.SueloEn(m.position.x, m.position.z, out suelo))
                {
                    FabricaEsbirros.Crear(EsbirroBaseIA.TipoEsbirro.Serpiente, suelo + Vector3.up * 0.2f);
                    serpientes++;
                }
            }
            else if (m.name.StartsWith(prefijoPiedra))
            {
                if (UtilTerreno.SueloEn(m.position.x, m.position.z, out suelo))
                {
                    FabricaEsbirros.Crear(EsbirroBaseIA.TipoEsbirro.HombrePiedra, suelo + Vector3.up * 0.2f);
                    piedras++;
                }
            }
            else if (m.name.StartsWith(prefijoComadre))
            {
                if (UtilTerreno.SueloEn(m.position.x, m.position.z, out suelo))
                {
                    FabricaEsbirros.Crear(EsbirroBaseIA.TipoEsbirro.Comadre, suelo + Vector3.up * 0.2f);
                    comadres++;
                }
            }
        }

        marcadoresAlma = almasMarcadores.ToArray();
        proximaReaparicion = Time.time + segundosReaparecerAlma;
        Debug.Log("[DirectorNivelBosque] Almas: " + almas + ", serpientes: " + serpientes + ", hombres de piedra: " + piedras + ", comadres: " + comadres);
    }

    void Update()
    {
        if (segundosReaparecerAlma <= 0f || marcadoresAlma == null || marcadoresAlma.Length == 0) return;
        if (Time.time < proximaReaparicion) return;
        proximaReaparicion = Time.time + segundosReaparecerAlma;

        if (Object.FindObjectsByType<AlmaRecolectable>().Length >= maxAlmasEnNivel) return;

        Transform m = marcadoresAlma[Random.Range(0, marcadoresAlma.Length)];
        Vector3 suelo;
        if (m != null && UtilTerreno.SueloEn(m.position.x, m.position.z, out suelo))
        {
            AlmaRecolectable.Crear(suelo + Vector3.up * 1.0f, false, false);
        }
    }
}
