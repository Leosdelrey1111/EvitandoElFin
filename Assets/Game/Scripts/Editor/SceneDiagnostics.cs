using UnityEngine;
using UnityEditor;

public static class SceneDiagnostics
{
    [MenuItem("EvitandoElFin/3. Diagnosticar y Configurar Escena Automáticamente")]
    public static void DiagnosticarYConfigurar()
    {
        Debug.Log("================ INICIANDO DIAGNÓSTICO DE ESCENA ================");

        // 1. Diagnóstico del Jugador
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            var choferController = Object.FindAnyObjectByType<ChoferController>();
            if (choferController != null)
            {
                player = choferController.gameObject;
                player.tag = "Player";
                Debug.LogWarning("[Diagnóstico] Se encontró el Chofer pero no tenía el Tag 'Player'. Se le ha asignado automáticamente.");
            }
            else
            {
                Debug.LogError("[Diagnóstico] ❌ No se encontró ningún Jugador ni ChoferController en la escena. Asegúrate de colocar al Chofer.");
            }
        }
        else
        {
            Debug.Log("[Diagnóstico] ✅ Jugador detectado correctamente con Tag 'Player'.");
        }

        if (player != null)
        {
            // Verificar componentes vitales del jugador
            if (player.GetComponent<PoderEspiritual>() == null)
            {
                player.AddComponent<PoderEspiritual>();
                Debug.LogWarning("[Diagnóstico] Se añadió 'PoderEspiritual' (Salud) al Jugador.");
            }
            if (player.GetComponent<InventarioEspiritual>() == null)
            {
                player.AddComponent<InventarioEspiritual>();
                Debug.LogWarning("[Diagnóstico] Se añadió 'InventarioEspiritual' al Jugador.");
            }
            if (player.GetComponent<SistemaCombateChofer>() == null)
            {
                player.AddComponent<SistemaCombateChofer>();
                Debug.LogWarning("[Diagnóstico] Se añadió 'SistemaCombateChofer' al Jugador.");
            }
        }

        // 2. Diagnóstico de la Cámara
        Camera cam = Camera.main;
        if (cam == null)
        {
            cam = Object.FindAnyObjectByType<Camera>();
            if (cam != null)
            {
                cam.tag = "MainCamera";
                Debug.LogWarning("[Diagnóstico] Cámara encontrada y configurada con Tag 'MainCamera'.");
            }
        }

        // Asegurar EventSystem (necesario para clics en UI)
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("[Diagnóstico] Se creó el EventSystem necesario para la UI.");
        }

        if (cam != null && player != null)
        {
            CamaraTerceraPersona camScript = cam.GetComponent<CamaraTerceraPersona>();
            if (camScript == null)
            {
                camScript = cam.gameObject.AddComponent<CamaraTerceraPersona>();
                camScript.objetivo = player.transform;
                Debug.LogWarning("[Diagnóstico] Se vinculó 'CamaraTerceraPersona' a la cámara principal siguiendo al Jugador.");
            }
            else if (camScript.objetivo == null)
            {
                camScript.objetivo = player.transform;
                Debug.Log("[Diagnóstico] Se asignó el Jugador como objetivo de la cámara.");
            }
        }

        // 3. Diagnóstico de GameManager y HUD
        GameManager gm = Object.FindAnyObjectByType<GameManager>();
        if (gm == null)
        {
            Debug.LogWarning("[Diagnóstico] No hay GameManager en la escena. Ejecutando creación automática...");
            LevelUIBuilder.MontarManagersYPaneles();
        }
        else
        {
            Debug.Log("[Diagnóstico] ✅ GameManager detectado y activo.");
        }

        // 4. Diagnóstico de Tótems y Portal
        TotemColeccionable[] totems = Object.FindObjectsByType<TotemColeccionable>(FindObjectsSortMode.None);
        Debug.Log($"[Diagnóstico] Tótems en el nivel: {totems.Length} {(totems.Length >= 3 ? "✅" : "⚠️ (Recomendado mínimo 3)")}");

        PortalVictoria portal = Object.FindAnyObjectByType<PortalVictoria>();
        if (portal == null)
        {
            Debug.LogWarning("[Diagnóstico] ⚠️ No hay Portal de Victoria en la escena. Generando uno...");
            QuickSpawnerTool.CrearPortalSalida();
        }
        else
        {
            Debug.Log("[Diagnóstico] ✅ Portal de Victoria detectado.");
        }

        // 5. Diagnóstico de Esbirros
        EsbirroBaseIA[] esbirros = Object.FindObjectsByType<EsbirroBaseIA>(FindObjectsSortMode.None);
        Debug.Log($"[Diagnóstico] Esbirros enemigos en el nivel: {esbirros.Length} {(esbirros.Length > 0 ? "✅" : "⚠️ (No hay enemigos)")}");

        Debug.Log("================ DIAGNÓSTICO COMPLETADO ================");
        EditorUtility.DisplayDialog("Diagnóstico Evitando El Fin",
            $"Revisión completada:\n\n" +
            $"• Jugador: {(player != null ? "Correcto" : "Falta")}\n" +
            $"• Cámara: {(cam != null ? "Configurada" : "Revisar")}\n" +
            $"• Tótems: {totems.Length}\n" +
            $"• Enemigos: {esbirros.Length}\n" +
            $"• Portal de Salida: {(portal != null ? "Activo" : "Creado")}\n\n" +
            $"Revisa la consola para más detalles.", "Entendido");
    }
}
