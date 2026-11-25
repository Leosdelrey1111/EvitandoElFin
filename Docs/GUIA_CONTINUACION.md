# GUÍA DE CONTINUACIÓN - Completar Implementación de Niveles

**Estado Actual:** 50% Completado  
**Tiempo Estimado Restante:** 8-12 horas de desarrollo

---

## ✅ LO QUE YA FUNCIONA

Estos sistemas están completamente implementados y funcionando:

1. ✅ **Sistema de Presentación de Enemigos** - Cinemáticas automáticas al encontrar enemigos nuevos
2. ✅ **Persistencia de Mejoras** - Las compras de Don Chuy se guardan entre niveles
3. ✅ **Diálogos Correctos por Nivel** - Totems/Cirios/Sellos hablan correctamente
4. ✅ **Distribución de Enemigos** - Cada nivel tiene los enemigos apropiados
5. ✅ **Ajuste de Alturas** - Enemigos ya no flotan ni se hunden incorrectamente
6. ✅ **Herramienta de Audios** - Reporte completo de qué audios generar

---

## 🎯 TAREAS PENDIENTES (Orden Recomendado)

### TAREA #1: Generar Audios (CRÍTICO) ⏱️ 2-3 horas

**Problema:** Nivel 2 y 3 no tienen archivos de audio.

**Solución:**

#### Opción A: Usar ElevenLabs (Recomendado)

```bash
# Paso 1: Generar el reporte en Unity
Unity → Menu: EvitandoElFin → Herramientas → Reporte de Audios Faltantes
→ Click en "Generar Reporte Completo"
→ Se abre: Assets/Docs/Reporte_Audios_Faltantes.txt

# Paso 2: Ir a ElevenLabs
Navegador → https://elevenlabs.io
→ Crear cuenta (tiene plan gratuito con 10,000 caracteres/mes)
→ Ir a "Speech Synthesis"

# Paso 3: Para cada audio del reporte
Para CADA línea marcada con "✗" (faltante):
  1. Copiar el TEXTO de la línea
  2. Seleccionar voz apropiada:
     - Tonalli: Voz masculina cálida (ej: "Antoni" o "Arnold")
     - General Momia: Voz masculina grave (ej: "Josh")
     - Clérigo Inquisidor: Voz masculina solemne (ej: "Sam")
  3. Ajustar configuración:
     - Stability: 50-60%
     - Clarity: 70-80%
     - Style Exaggeration: 0% (natural)
  4. Click "Generate"
  5. Descargar el archivo
  6. Renombrar exactamente como indica el reporte (ej: "intro_panteon_0.mp3")
  7. Mover a: Assets/Game/Resources/Voz/

# Paso 4: Verificar
Unity → Menu: EvitandoElFin → Herramientas → Reporte de Audios Faltantes
→ Click en "Verificar Audios Existentes"
→ Debe mostrar 0 faltantes
```

**Total de audios a generar:**
- Nivel 2: ~40 archivos
- Nivel 3: ~45 archivos
- **Total: ~85 archivos de audio**

**Tiempo estimado:** 1.5-2 minutos por audio = 2-3 horas

---

### TAREA #2: Reposicionar Don Chuy ⏱️ 30 minutos

**Problema:** Don Chuy no está en posición estratégica en cada nivel.

**Solución:**

#### Nivel 1 (Cerro de La Bufa)
**Archivo:** `Assets/Game/Scripts/Editor/Nivel1HistoriaSetup.cs`

```csharp
// Buscar la línea que crea "Comerciante_DonChuy"
// Cambiar la posición a:
Vector2 localCom = new Vector2(/* coordenadas antes de Cueva Vieja */);

// Ubicación recomendada: 
// Buscar punto del sendero cercano a la cueva pero visible
// Ejemplo: justo después del tercer tótem, antes de "corona"
```

#### Nivel 2 (Panteón de Santa Paula)
**Archivo:** `Assets/Game/Scripts/PaisajePanteon.cs`

```csharp
// En ConstruirCapillaDonChuy(), cambiar posición:
// ACTUAL: (14f, 0, 30f)
// NUEVO: Antes del Gran Mausoleo, ejemplo:
ConstruirCapillaDonChuy(raiz.transform, new Vector3(14f, 0, 95f));
// (ajustar según dónde esté el Mausoleo en tu mapa)
```

#### Nivel 3 (Ciudad Colonial)
**Archivo:** `Assets/Game/Scripts/PaisajeCiudadColonial.cs`

```csharp
// En ConstruirPlazuelaDonChuy(), cambiar posición:
// ACTUAL: (16f, 0, 32f)
// NUEVO: Antes de la Basílica, ejemplo:
ConstruirPlazuelaDonChuy(raiz.transform, new Vector3(0f, 0, 110f));
// (ajustar según dónde esté la Basílica en tu mapa)
```

**Verificación:**
1. Jugar cada nivel
2. Buscar a Don Chuy
3. Verificar que esté visible pero no estorbe
4. Verificar que esté ANTES del jefe final

---

### TAREA #3: Arreglar Entrada al Mausoleo (Nivel 2) ⏱️ 1 hora

**Problema:** No se puede entrar al Mausoleo para pelear con el General Momia.

**Archivo:** `Assets/Game/Scripts/PaisajePanteon.cs`

**Buscar función:** `ConstruirGranMausoleo()`

**Implementar:**

```csharp
void ConstruirGranMausoleo(Transform padre, Vector3 pos)
{
    GameObject mausoleo = new GameObject("GranMausoleo");
    mausoleo.transform.SetParent(padre, false);
    mausoleo.transform.localPosition = pos;

    // ... construcción visual del mausoleo ...

    // AÑADIR: Entrada con trigger
    GameObject entrada = GameObject.CreatePrimitive(PrimitiveType.Cube);
    entrada.name = "EntradaMausoleo";
    entrada.transform.SetParent(mausoleo.transform, false);
    entrada.transform.localPosition = new Vector3(0f, 1.5f, 3f); // Frente del mausoleo
    entrada.transform.localScale = new Vector3(3f, 3f, 1f);
    
    // Hacer invisible
    entrada.GetComponent<Renderer>().enabled = false;
    
    // Configurar como trigger
    Collider col = entrada.GetComponent<Collider>();
    col.isTrigger = true;
    
    // Añadir script de puerta
    PuertaMausoleo script = entrada.AddComponent<PuertaMausoleo>();
    script.ciriosRequeridos = 3;
}
```

**Crear nuevo script:** `Assets/Game/Scripts/PuertaMausoleo.cs`

```csharp
using UnityEngine;

public class PuertaMausoleo : MonoBehaviour
{
    public int ciriosRequeridos = 3;
    private bool puertaAbierta = false;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        InventarioEspiritual inv = other.GetComponent<InventarioEspiritual>();
        if (inv == null) return;

        if (inv.cantidadTotems >= ciriosRequeridos)
        {
            if (!puertaAbierta)
            {
                puertaAbierta = true;
                AbrirPuerta();
            }
            // Teletransportar al jugador dentro de la arena del jefe
            other.transform.position = transform.position + Vector3.forward * 5f;
        }
        else
        {
            AvisosJuego.Mostrar($"Necesitas los {ciriosRequeridos} Cirios de Santa Paula para entrar", 3f);
        }
    }

    void AbrirPuerta()
    {
        // Efecto visual/sonoro de puertas abriéndose
        AvisosJuego.Mostrar("¡Las rejas del Gran Mausoleo se abren!", 3f);
        // Aquí puedes añadir animación de puertas si quieres
    }
}
```

---

### TAREA #4: Arreglar Colisiones del Jefe Inquisidor ⏱️ 30 minutos

**Problema:** No puedes golpear al Clérigo Inquisidor.

**Archivo:** `Assets/Game/Scripts/JefeInquisidor.cs`

**Buscar:** Función `Start()` o `Awake()`

**Verificar/Añadir:**

```csharp
void Start()
{
    // ... código existente ...

    // VERIFICAR: CharacterController
    CharacterController cc = GetComponent<CharacterController>();
    if (cc == null)
    {
        cc = gameObject.AddComponent<CharacterController>();
    }
    cc.height = 2.0f;
    cc.radius = 0.6f;
    cc.center = new Vector3(0f, 1.0f, 0f);

    // VERIFICAR: Collider para recibir golpes
    CapsuleCollider capCol = GetComponent<CapsuleCollider>();
    if (capCol == null)
    {
        capCol = gameObject.AddComponent<CapsuleCollider>();
    }
    capCol.height = 2.0f;
    capCol.radius = 0.6f;
    capCol.center = new Vector3(0f, 1.0f, 0f);
    capCol.isTrigger = false; // IMPORTANTE: NO trigger

    // ... resto del código ...
}
```

**Si el jefe está flotando:**

```csharp
void Update()
{
    if (estado == Estado.Muerto) return;

    // Aplicar gravedad si no está en el suelo
    if (!controller.isGrounded)
    {
        velocidadY += Physics.gravity.y * Time.deltaTime;
    }
    else
    {
        velocidadY = -2f; // Pequeña fuerza hacia abajo para mantenerlo pegado
    }

    Vector3 movimiento = new Vector3(0, velocidadY, 0);
    controller.Move(movimiento * Time.deltaTime);

    // ... resto del código de combate ...
}
```

---

### TAREA #5: Implementar Cierre de Arena (Nivel 3) ⏱️ 1 hora

**Problema:** Arena del Inquisidor no se cierra como en Nivel 1.

**Crear nuevo script:** `Assets/Game/Scripts/ArenaJefeCerrada.cs`

```csharp
using UnityEngine;
using System.Collections.Generic;

public class ArenaJefeCerrada : MonoBehaviour
{
    public Transform centroArena;
    public float radioArena = 15f;
    public int numeroMuros = 16;
    public float alturaMuro = 8f;

    private List<GameObject> muros = new List<GameObject>();
    private bool arenaCerrada = false;

    public void CerrarArena()
    {
        if (arenaCerrada) return;
        arenaCerrada = true;

        // Crear muros invisibles en círculo
        for (int i = 0; i < numeroMuros; i++)
        {
            float angulo = i * (360f / numeroMuros);
            Vector3 posicion = centroArena.position + 
                Quaternion.Euler(0, angulo, 0) * Vector3.forward * radioArena;

            GameObject muro = GameObject.CreatePrimitive(PrimitiveType.Cube);
            muro.name = "MuroArena_" + i;
            muro.transform.position = posicion;
            muro.transform.LookAt(centroArena);
            muro.transform.localScale = new Vector3(radioArena * 0.5f, alturaMuro, 0.5f);

            // Hacer semitransparente con niebla
            Material mat = muro.GetComponent<Renderer>().material;
            mat.color = new Color(0.1f, 0.1f, 0.15f, 0.6f);
            mat.SetFloat("_Mode", 3); // Transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;

            muros.Add(muro);
        }

        AvisosJuego.Mostrar("¡El Atrio se cierra! No hay escapatoria.", 3f);
    }

    public void AbrirArena()
    {
        foreach (GameObject muro in muros)
        {
            if (muro != null) Destroy(muro);
        }
        muros.Clear();
        arenaCerrada = false;

        AvisosJuego.Mostrar("¡El portal de regreso se abre!", 3f);
    }
}
```

**Integrar en JefeInquisidor.cs:**

```csharp
// Arriba en variables privadas:
private ArenaJefeCerrada arena;

void Start()
{
    // ... código existente ...

    // Buscar o crear sistema de arena
    arena = FindObjectOfType<ArenaJefeCerrada>();
    if (arena == null)
    {
        GameObject arenaGo = new GameObject("ArenaJefeCerrada");
        arena = arenaGo.AddComponent<ArenaJefeCerrada>();
        arena.centroArena = transform;
        arena.radioArena = 18f;
    }
}

void IniciarCombate()
{
    estado = Estado.Combate;
    
    // CERRAR ARENA
    if (arena != null) arena.CerrarArena();
    
    // ... resto del código de inicio de combate ...
}

void Morir()
{
    estado = Estado.Muriendo;
    
    // ABRIR ARENA
    if (arena != null) arena.AbrirArena();
    
    // ... resto del código de muerte ...
}
```

---

### TAREA #6: Cinemáticas de Jefes ⏱️ 2 horas

**Para cada jefe (General Momia e Inquisidor), añadir en `CinematicaNarrativa.cs`:**

```csharp
// En la función GuionDe(), añadir casos:

case "jefe_general_presentacion":
{
    l.Add(T(t =>
    {
        Vector3 posJefe = /* posición del General Momia */;
        return Vista(posJefe + f * 6f + arriba * 3f, posJefe + arriba * 1.5f, 45f);
    }, () =>
    {
        // Efecto visual al entrar
    }, "EL GENERAL MOMIA", "Centinela del Panteón de Santa Paula", true));
    break;
}

case "jefe_inquisidor_presentacion":
{
    l.Add(T(t =>
    {
        Vector3 posJefe = /* posición del Inquisidor */;
        return Vista(posJefe + f * 7f + arriba * 4f, posJefe + arriba * 2f, 40f);
    }, () =>
    {
        // Efecto visual al entrar
    }, "EL CLÉRIGO INQUISIDOR", "La Sombra del Callejón del Beso", true));
    break;
}
```

---

## 📋 CHECKLIST DE VERIFICACIÓN

Después de cada tarea, verificar:

### Después de Generar Audios:
- [ ] Todos los archivos .mp3 están en `Assets/Game/Resources/Voz/`
- [ ] Los nombres coinciden exactamente con el reporte
- [ ] Unity puede cargar los archivos (no hay errores en consola)
- [ ] Los diálogos suenan al recoger coleccionables
- [ ] La voz de Tonalli suena durante narrativa

### Después de Reposicionar Don Chuy:
- [ ] Don Chuy visible en Nivel 1 antes de Cueva Vieja
- [ ] Don Chuy visible en Nivel 2 antes de Gran Mausoleo
- [ ] Don Chuy visible en Nivel 3 antes de Basílica
- [ ] No obstruye el camino
- [ ] Tiene indicador visual (luz/partículas)

### Después de Arreglar Mausoleo:
- [ ] Puedes acercarte a la entrada
- [ ] Con 0-2 cirios, muestra mensaje de "faltan cirios"
- [ ] Con 3 cirios, te deja entrar
- [ ] Te teletransporta dentro de la arena
- [ ] El jefe está presente y funcional

### Después de Arreglar Colisiones Inquisidor:
- [ ] Puedes golpear al jefe con ataques normales
- [ ] El jefe no flota en el aire
- [ ] Los ataques del jefe te dañan
- [ ] La barra de vida funciona correctamente

### Después de Cerrar Arena:
- [ ] Al iniciar combate, aparecen muros
- [ ] No puedes salir de la arena
- [ ] Al derrotar jefe, muros desaparecen
- [ ] Puedes acceder al portal de salida

---

## 🚨 PROBLEMAS COMUNES Y SOLUCIONES

### "No encuentro el archivo X.cs"
→ Usar búsqueda de Unity: Edit → Find → Find References in Scene
→ O buscar en carpeta: Assets/Game/Scripts/

### "Los audios no suenan"
→ Verificar que están en formato .mp3
→ Verificar que el nombre es EXACTO (mayúsculas/minúsculas)
→ Verificar que están en Resources/Voz/
→ Reimportar en Unity: Click derecho → Reimport

### "Don Chuy no aparece"
→ Verificar que el objeto se llama exactamente "Comerciante_DonChuy" o "Puesto_DonChuy"
→ Verificar que tiene el componente `ComercianteMercader`
→ Buscar en Hierarchy al jugar

### "El jefe no recibe daño"
→ Verificar que tiene `PoderEspiritual` component
→ Verificar que el collider NO es trigger
→ Verificar que está en layer correcto

---

## 📞 NECESITAS AYUDA?

Si te atascas en algo:

1. **Revisar logs de Unity:** Console → Ver errores rojos
2. **Revisar este documento:** Buscar tu problema
3. **Probar en escena de prueba:** Crear escena simple para aislar problema
4. **Documentar el error:** Qué esperabas vs qué pasó

---

**¡Ánimo! Ya llevas 50% completado. Las partes más difíciles (sistemas) ya están implementadas.** 🎮✨

Lo que falta es principalmente:
- Generar archivos de audio (mecánico pero tedioso)
- Ajustar posiciones (rápido)
- Arreglar colisiones (sencillo)

**Tiempo total restante estimado: 8-12 horas de trabajo enfocado.**
