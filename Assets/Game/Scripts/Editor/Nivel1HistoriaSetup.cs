using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Completa el Nivel 1 con la historia de La Bufa:
///  - Alebrije narrador (Tonalli) + hitos narrativos repartidos por el sendero.
///  - Comadres (enemigos a distancia) en las emboscadas 2 y 3 y errantes en el bosque.
///  - Jefe LA BUFA en la arena del final; el portal pasa al fondo de la arena y se abre al vencerla.
/// Es idempotente: se puede ejecutar varias veces sobre la misma escena sin duplicar nada.
/// </summary>
public static class Nivel1HistoriaSetup
{
    const string RutaEscena = "Assets/Scenes/Nivel1_Bosque.unity";

    // Dónde (en metros recorridos del sendero) viven los enemigos errantes. Después de que Tonalli los presenta, y sin pisar a Don Chuy (~1110-1180 m).
    public static readonly float[] MetrosSerpientes = { 470f, 560f, 700f, 840f, 930f, 1010f, 1050f, 1235f };
    public static readonly float[] MetrosPiedras = { 880f, 990f, 1040f, 1225f };

    [MenuItem("EvitandoElFin/6c. Añadir historia, Comadres y jefe La Bufa a Nivel 1 (sin rehacer)")]
    public static void AplicarEnMenu()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RutaEscena) == null)
        {
            EditorUtility.DisplayDialog("Evitando El Fin", "No existe Nivel1_Bosque. Ejecuta primero el menú 6.", "OK");
            return;
        }
        var activa = EditorSceneManager.GetActiveScene();
        if (activa.path != RutaEscena)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
        }

        string informe = Aplicar();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("[Nivel1] " + informe);
        EditorUtility.DisplayDialog("Evitando El Fin", informe, "Listo");
    }

    /// <summary>Aplica todo a la escena abierta. Devuelve un resumen.</summary>
    public static string Aplicar()
    {
        Terrain terreno = Object.FindAnyObjectByType<Terrain>();
        if (terreno == null) return "No encontré el terreno en la escena abierta.";

        var sb = new System.Text.StringBuilder();

        // ---------------------------------------------------------------- arena y portal
        BosqueTerrenoBuilder.Plaza arena = BosqueTerrenoBuilder.Plazas[4];
        Vector3 centroArena = Mundo(terreno, arena.centro);
        Vector2 ultimo = BosqueTerrenoBuilder.Sendero[BosqueTerrenoBuilder.Sendero.Length - 1];
        Vector2 penultimo = BosqueTerrenoBuilder.Sendero[BosqueTerrenoBuilder.Sendero.Length - 2];
        Vector2 d2 = (ultimo - penultimo).normalized;
        Vector3 dirLlegada = new Vector3(d2.x, 0f, d2.y);

        // la emboscada del portal la reemplaza el jefe
        GameObject embPortal = GameObject.Find("Emboscada_Portal");
        if (embPortal != null) Object.DestroyImmediate(embPortal);

        // portal al fondo de la arena, mirando hacia donde llega el Chofer
        PortalVictoria portal = Object.FindAnyObjectByType<PortalVictoria>();
        if (portal != null)
        {
            Vector3 pp = centroArena + dirLlegada * 12.5f;
            pp.y = Altura(terreno, pp);
            portal.transform.position = pp;
            portal.transform.rotation = Quaternion.LookRotation(-dirLlegada);
            sb.AppendLine("• Portal movido al fondo de la arena (se abre al vencer a La Bufa).");
        }

        // jefe
        JefeLaBufa jefe = Object.FindAnyObjectByType<JefeLaBufa>();
        if (jefe == null)
        {
            GameObject go = new GameObject("Jefe_LaBufa");
            jefe = go.AddComponent<JefeLaBufa>();
        }
        jefe.transform.position = centroArena + Vector3.up * 0.1f;
        jefe.radioArena = 14f;
        jefe.radioDespertar = 11f;
        EditorUtility.SetDirty(jefe);
        sb.AppendLine("• Jefe LA BUFA colocado en la arena (duerme como estatua hasta tener los 3 tótems).");

        GameManager gm = Object.FindAnyObjectByType<GameManager>();
        if (gm != null)
        {
            gm.jefeRequerido = true;
            EditorUtility.SetDirty(gm);
        }

        // ---------------------------------------------------------------- narrador
        if (Object.FindAnyObjectByType<GuiaAlebrije>() == null)
            new GameObject("Narrador_Alebrije").AddComponent<GuiaAlebrije>();
        sb.AppendLine("• Alebrije narrador Tonalli (tecla R: te marca el camino).");

        // ---------------------------------------------------------------- sendero guardado en la escena + cinemáticas narrativas
        SenderoNivel senderoNivel = Object.FindAnyObjectByType<SenderoNivel>();
        if (senderoNivel == null) senderoNivel = new GameObject("Sendero_Nivel1").AddComponent<SenderoNivel>();
        List<Vector2> muestras = BosqueTerrenoBuilder.MuestrasSendero();
        var puntosMundo = new List<Vector3>();
        for (int i = 0; i < muestras.Count; i += 2) puntosMundo.Add(Mundo(terreno, muestras[i]));
        puntosMundo.Add(Mundo(terreno, muestras[muestras.Count - 1]));
        senderoNivel.puntos = puntosMundo.ToArray();
        EditorUtility.SetDirty(senderoNivel);

        if (Object.FindAnyObjectByType<CinematicaNarrativa>() == null)
            new GameObject("Cinematicas_Narrativas").AddComponent<CinematicaNarrativa>();
        sb.AppendLine("• Cinemáticas: serpientes, hombres de piedra, Comadres, y La Bufa (cuando despierta y cuando se rompe el hechizo). ENTER las salta.");

        // ---------------------------------------------------------------- hitos narrativos
        GameObject viejoHitos = GameObject.Find("Hitos_Narrativos");
        if (viejoHitos != null) Object.DestroyImmediate(viejoHitos);
        GameObject hitos = new GameObject("Hitos_Narrativos");
        // Cada beat de la historia llega en su momento: se coloca por metros recorridos sobre el sendero (~1300 m, ~4-5 min caminando)
        Hito(hitos.transform, terreno, "sierra", NodoSendero(3), 14f);                    // ~165 m  · mirador de los cerros
        Hito(hitos.transform, terreno, "serpientes", BosqueTerrenoBuilder.Plazas[1].centro, 34f);   // ~400 m · antes del tótem 1
        Hito(hitos.transform, terreno, "arroyo", NodoSendero(9), 16f);                    // ~485 m  · cruce del arroyo seco
        Hito(hitos.transform, terreno, "tesoro", NodoSendero(12), 16f);                   // ~650 m  · bocamina

        // ---------------------------------------------------------------- Comerciante (Don Chuy), junto al sendero casi al final
        GameObject viejoCom = GameObject.Find("Comerciante_DonChuy");
        if (viejoCom != null) Object.DestroyImmediate(viejoCom);
        Vector2 dCom;
        // La tienda va casi al final (~1140 m, tras el tótem 3 y antes de la Cueva Vieja): al principio el Chofer no tiene almas que gastar
        Vector2 enCom = BosqueTerrenoBuilder.PuntoEnSendero(BosqueTerrenoBuilder.MetrosEnSendero(BosqueTerrenoBuilder.Sendero[21]), out dCom);
        Vector2 perpCom = new Vector2(-dCom.y, dCom.x);
        Vector2 localCom = enCom + perpCom * 7f;
        GameObject com = new GameObject("Comerciante_DonChuy");
        com.transform.position = Mundo(terreno, localCom);
        com.transform.rotation = Quaternion.LookRotation(new Vector3(-perpCom.x, 0f, -perpCom.y));
        com.AddComponent<ComercianteMercader>();
        Hito(hitos.transform, terreno, "comerciante", localCom, 16f);
        sb.AppendLine("• Don Chuy, el Comerciante (E): vende mejoras a cambio de almas.");
        Hito(hitos.transform, terreno, "hombres_piedra", BosqueTerrenoBuilder.Plazas[2].centro, 34f);  // ~780 m · antes del tótem 2
        Hito(hitos.transform, terreno, "cruces", NodoSendero(17), 16f);                                   // ~930 m
        Hito(hitos.transform, terreno, "comadres", BosqueTerrenoBuilder.Plazas[3].centro, 30f);        // ~1060 m · antes del tótem 3
        Hito(hitos.transform, terreno, "corona", NodoSendero(22), 18f);                                   // ~1200 m · se ve la corona de La Bufa
        Hito(hitos.transform, terreno, "cueva", BosqueTerrenoBuilder.Plazas[4].centro, 24f);           // ~1290 m · la Cueva Vieja
        sb.AppendLine("• Hitos de historia repartidos por el sendero (Tonalli narra al llegar a cada uno).");

        // ---------------------------------------------------------------- emboscadas con Comadres
        foreach (ZonaEmboscada z in Object.FindObjectsByType<ZonaEmboscada>())
        {
            if (z.name == "Emboscada_2") z.comadres = 1;
            else if (z.name == "Emboscada_3") z.comadres = 2;
            z.mensajeInicio = "¡Los guardianes de La Bufa te cierran el paso! Click izquierdo: combo  ·  Q: onda espiritual.";
            EditorUtility.SetDirty(z);
        }

        // ---------------------------------------------------------------- comadres errantes
        DirectorNivelBosque director = Object.FindAnyObjectByType<DirectorNivelBosque>();
        if (director != null)
        {
            foreach (Transform m in director.transform.Cast<Transform>().Where(t => t.name.StartsWith("SpawnComadre_")).ToList())
                Object.DestroyImmediate(m.gameObject);

            // Reubica las serpientes y hombres de piedra errantes que ya existan (por si la escena se creó con otras posiciones)
            ReubicarMarcadores(director.transform, "SpawnSerpiente_", MetrosSerpientes, 6f);
            ReubicarMarcadores(director.transform, "SpawnPiedra_", MetrosPiedras, 9f);

            // Las Comadres se presentan al llegar al claro del tótem 3 (~1060 m): las errantes viven pasando ese punto
            float[] metrosComadre = { 1000f, 1070f, 1245f };
            for (int k = 0; k < metrosComadre.Length; k++)
            {
                Vector2 d;
                Vector2 pc = BosqueTerrenoBuilder.PuntoEnSendero(metrosComadre[k], out d);
                Vector2 perp = new Vector2(-d.y, d.x);
                GameObject mk = new GameObject("SpawnComadre_" + k);
                mk.transform.SetParent(director.transform, false);
                mk.transform.position = BosqueTerrenoBuilder.LocalAMundo(pc + perp * (k % 2 == 0 ? 8f : -8f), 0f);
            }
            sb.AppendLine("• Comadres (murmullos a distancia) en las emboscadas 2 y 3 y 2 errantes.");
        }

        sb.AppendLine("\nFlujo del nivel: reúne 3 tótems → entra a la Cueva Vieja → vence a La Bufa → cruza el portal.");
        return sb.ToString();
    }

    // ================================================================== utilidades

    static void ReubicarMarcadores(Transform director, string prefijo, float[] metros, float lateral)
    {
        for (int k = 0; k < metros.Length; k++)
        {
            Transform m = director.Find(prefijo + k);
            if (m == null) continue;
            Vector2 d;
            Vector2 p = BosqueTerrenoBuilder.PuntoEnSendero(metros[k], out d);
            Vector2 perp = new Vector2(-d.y, d.x);
            m.position = BosqueTerrenoBuilder.LocalAMundo(p + perp * (k % 2 == 0 ? lateral : -lateral) * (prefijo == "SpawnPiedra_" ? -1f : 1f), 0f);
        }
    }

    /// <summary>Punto del sendero (local) donde está el nodo i de la lista de puntos de control.</summary>
    static Vector2 NodoSendero(int i)
    {
        Vector2 dir;
        return BosqueTerrenoBuilder.PuntoEnSendero(BosqueTerrenoBuilder.MetrosEnSendero(BosqueTerrenoBuilder.Sendero[i]), out dir);
    }

    static void Hito(Transform padre, Terrain t, string id, Vector2 local, float radio)
    {
        GameObject go = new GameObject("Hito_" + id);
        go.transform.SetParent(padre, false);
        go.transform.position = Mundo(t, local);
        HitoNarrativo h = go.AddComponent<HitoNarrativo>();
        h.idHistoria = id;
        h.radio = radio;
    }

    static Vector3 Mundo(Terrain t, Vector2 local)
    {
        Vector3 p = BosqueTerrenoBuilder.LocalAMundo(local, 0f);
        p.y = Altura(t, p);
        return p;
    }

    static float Altura(Terrain t, Vector3 mundo)
    {
        return t.SampleHeight(mundo) + t.transform.position.y;
    }
}
