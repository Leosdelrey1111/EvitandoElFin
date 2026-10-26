using UnityEngine;
using UnityEditor;

public static class QuickSpawnerTool
{
    [MenuItem("EvitandoElFin/Generar Elementos/Tótem Coleccionable")]
    public static void CrearTotemEnEscena()
    {
        Vector3 spawnPos = ObtenerPosicionFrenteCamara();

        GameObject totem = new GameObject("Totem_Espiritual");
        totem.transform.position = spawnPos;

        // Visual base (Prisma/Cilindro estilizado con brillo)
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visual.name = "Visual";
        visual.transform.SetParent(totem.transform, false);
        visual.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);

        // Remover collider del visual secundario para evitar conflictos
        var colVisual = visual.GetComponent<Collider>();
        if (colVisual != null) Object.DestroyImmediate(colVisual);

        // Decoración superior (Cristal de energía)
        GameObject cristal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cristal.name = "GemaEspiritual";
        cristal.transform.SetParent(totem.transform, false);
        cristal.transform.localPosition = new Vector3(0, 1.1f, 0);
        cristal.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);
        var colCristal = cristal.GetComponent<Collider>();
        if (colCristal != null) Object.DestroyImmediate(colCristal);

        // Collider Trigger principal
        SphereCollider sc = totem.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 1.0f;
        sc.center = new Vector3(0, 0.6f, 0);

        // Script de comportamiento
        TotemColeccionable tc = totem.AddComponent<TotemColeccionable>();
        tc.valorTotem = 1;
        tc.curacionEspiritual = 15f;
        tc.velocidadRotacion = 75f;
        tc.amplitudFlotacion = 0.3f;

        // Luz tenue para ambiente místico
        GameObject luz = new GameObject("LuzEspiritual");
        luz.transform.SetParent(totem.transform, false);
        luz.transform.localPosition = new Vector3(0, 1.1f, 0);
        Light lightComp = luz.AddComponent<Light>();
        lightComp.type = LightType.Point;
        lightComp.color = new Color(0.2f, 1f, 0.85f);
        lightComp.range = 6f;
        lightComp.intensity = 2f;

        Selection.activeGameObject = totem;
        Undo.RegisterCreatedObjectUndo(totem, "Crear Totem Coleccionable");
        Debug.Log($"[QuickSpawner] Tótem creado en {spawnPos}.");
    }

    [MenuItem("EvitandoElFin/Generar Elementos/Esbirro Enemigo (Con IA y Combate)")]
    public static void CrearEsbirroEnEscena()
    {
        Vector3 spawnPos = ObtenerPosicionFrenteCamara();

        GameObject esbirro = new GameObject("Esbirro_Enemigo");
        esbirro.transform.position = spawnPos;

        // CharacterController para movimiento e impacto
        CharacterController cc = esbirro.AddComponent<CharacterController>();
        cc.center = new Vector3(0, 1f, 0);
        cc.height = 2f;
        cc.radius = 0.5f;

        // Visual (Cuerpo oscuro/sombrío)
        GameObject cuerpo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        cuerpo.name = "CuerpoVisual";
        cuerpo.transform.SetParent(esbirro.transform, false);
        cuerpo.transform.localPosition = new Vector3(0, 1f, 0);
        cuerpo.transform.localScale = new Vector3(0.9f, 1f, 0.9f);
        var colCuerpo = cuerpo.GetComponent<Collider>();
        if (colCuerpo != null) Object.DestroyImmediate(colCuerpo);

        // Ojos amenazantes / Indicador frontal
        GameObject ojos = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ojos.name = "Ojos";
        ojos.transform.SetParent(cuerpo.transform, false);
        ojos.transform.localPosition = new Vector3(0, 0.5f, 0.4f);
        ojos.transform.localScale = new Vector3(0.5f, 0.15f, 0.2f);
        var colOjos = ojos.GetComponent<Collider>();
        if (colOjos != null) Object.DestroyImmediate(colOjos);

        // Hitbox de ataque cuerpo a cuerpo frontal
        GameObject hitboxGO = new GameObject("HitboxAtaqueEnemigo");
        hitboxGO.transform.SetParent(esbirro.transform, false);
        hitboxGO.transform.localPosition = new Vector3(0, 1f, 1.2f);
        BoxCollider boxCol = hitboxGO.AddComponent<BoxCollider>();
        boxCol.isTrigger = true;
        boxCol.size = new Vector3(1.2f, 1.5f, 1.5f);
        HitboxAtaque hitbox = hitboxGO.AddComponent<HitboxAtaque>();
        hitbox.danio = 15f;
        hitbox.tagObjetivo = "Player";
        hitboxGO.SetActive(false);

        // Salud y Comportamiento IA
        PoderEspiritual vida = esbirro.AddComponent<PoderEspiritual>();
        vida.maxPoder = 40f;

        EsbirroBaseIA ia = esbirro.AddComponent<EsbirroBaseIA>();
        ia.velocidadPersecucion = 3.5f;
        ia.rangoDeteccion = 10f;
        ia.rangoAtaque = 1.8f;
        ia.danio = 15f;
        ia.hitboxAtaqueEnemigo = hitboxGO;

        Selection.activeGameObject = esbirro;
        Undo.RegisterCreatedObjectUndo(esbirro, "Crear Esbirro Enemigo");
        Debug.Log($"[QuickSpawner] Esbirro Enemigo colocado en {spawnPos}.");
    }

    [MenuItem("EvitandoElFin/Generar Elementos/Portal de Salida (Victoria)")]
    public static void CrearPortalSalida()
    {
        Vector3 spawnPos = ObtenerPosicionFrenteCamara();

        GameObject portalGO = new GameObject("Portal_Salida_Victoria");
        portalGO.transform.position = spawnPos;

        // Estructura visual de columnas
        GameObject arco = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        arco.name = "ColumnaPortal";
        arco.transform.SetParent(portalGO.transform, false);
        arco.transform.localScale = new Vector3(3.5f, 0.2f, 3.5f);
        arco.transform.localPosition = Vector3.zero;

        // Vórtice central
        GameObject vortice = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        vortice.name = "VorticeCentral";
        vortice.transform.SetParent(portalGO.transform, false);
        vortice.transform.localPosition = new Vector3(0, 2f, 0);
        vortice.transform.localScale = new Vector3(2.5f, 4f, 0.5f);
        var colVortice = vortice.GetComponent<Collider>();
        if (colVortice != null) Object.DestroyImmediate(colVortice);

        // Luz de portal
        GameObject luz = new GameObject("LuzPortal");
        luz.transform.SetParent(portalGO.transform, false);
        luz.transform.localPosition = new Vector3(0, 2f, 0);
        Light l = luz.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(0.1f, 1f, 0.6f);
        l.range = 10f;
        l.intensity = 3f;

        // Collider y Script de Victoria
        BoxCollider bc = portalGO.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size = new Vector3(3.5f, 4f, 2f);
        bc.center = new Vector3(0, 2f, 0);

        PortalVictoria pv = portalGO.AddComponent<PortalVictoria>();
        pv.efectosPortalActivo = vortice;

        Selection.activeGameObject = portalGO;
        Undo.RegisterCreatedObjectUndo(portalGO, "Crear Portal de Victoria");
        Debug.Log($"[QuickSpawner] Portal de Salida creado en {spawnPos}.");
    }

    private static Vector3 ObtenerPosicionFrenteCamara()
    {
        Camera cam = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.camera : Camera.main;
        if (cam != null)
        {
            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 50f))
            {
                return hit.point + Vector3.up * 0.1f;
            }
            return cam.transform.position + cam.transform.forward * 5f;
        }
        return Vector3.zero;
    }
}
