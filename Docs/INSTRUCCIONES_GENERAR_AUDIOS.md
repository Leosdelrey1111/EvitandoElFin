# ¡Los Audios Ya Están Listos Para Generar!

## 🎉 Buenas Noticias

El sistema de generación de audios **ya está implementado y funcional**. Solo necesitas ejecutar un menú en Unity.

---

## 📋 Pasos Para Generar Todos Los Audios (Nivel 1, 2 y 3)

### Paso 1: Abrir Unity
1. Abre el proyecto en Unity
2. Espera a que termine de compilar

### Paso 2: Generar Voces
1. En el menú superior, haz clic en: **`EvitandoElFin`**
2. Selecciona: **`9. Generar voces del Nivel 1 (Tonalli, La Bufa y Don Chuy)`**
   
   > **Nota:** Aunque dice "Nivel 1", este menú genera audios para **los 3 niveles**

3. Espera 2-5 minutos (dependiendo de tu PC)

### Paso 3: Verificar
1. Los audios se generan en: `Assets/Game/Resources/Voz/`
2. Verifica que se crearon los 85 archivos nuevos:
   - `intro_panteon_0.wav`
   - `intro_ciudad_0.wav`
   - `general_despierta.wav`
   - `inquisidor_despierta.wav`
   - etc.

---

## 🎙️ Sistema de Voces

El sistema usa **3 perfiles de voz** configurados en `Tools/GenerarVoces.ps1`:

### Tonalli (Guía/Narrador)
```powershell
'Tonalli' = @{ tono = '+30%'; ritmo = '+8%' }
```
- Voz **aguda y ligera** (alebrije)
- Usada para: 76 archivos de narración

### General Momia (Jefe Nivel 2)
```powershell
'LaBufa' = @{ tono = '-30%'; ritmo = '-15%' }
```
- Voz **grave y lenta**
- Usada para: 3 archivos del jefe

### Clérigo Inquisidor (Jefe Nivel 3)
```powershell
'LaBufa' = @{ tono = '-30%'; ritmo = '-15%' }
```
- Mismo perfil que La Bufa
- Voz **grave y lenta**
- Usada para: 3 archivos del jefe

---

## ⚙️ Opciones Avanzadas

### Opción A: Voces Neuronales de Windows (Mejor Calidad)
Si quieres voces **más naturales y realistas**:

1. En Unity, menú: **`EvitandoElFin`** → **`9c. Instalar voces neuronales (Piper)`**
2. Espera ~5 minutos (descarga 150 MB)
3. Luego ejecuta: **`EvitandoElFin`** → **`9b. Regenerar TODAS las voces`**

**Ventajas:**
- Voces neurales mucho más naturales
- Español de México auténtico
- Sin internet necesario (después de la instalación)

**Desventaja:**
- Requiere Python 3 instalado
- Descarga 150 MB

### Opción B: Regenerar Todos Los Audios
Si ya generaste audios pero quieres rehacerlos:

1. Menú: **`EvitandoElFin`** → **`9b. Regenerar TODAS las voces (sobrescribe)`**
2. **⚠️ ADVERTENCIA:** Esto sobrescribe TODOS los audios, incluso si los reemplazaste con grabaciones reales

---

## 🔊 Voces por Personaje

Los audios se asignan automáticamente según el perfil de voz en `HistoriaNivel1`, `HistoriaNivel2` y `HistoriaNivel3`:

```csharp
// Ejemplo de HistoriaNivel2.cs
yield return new HistoriaNivel1.Frase("intro_panteon_0", "¡Mira hacia arriba, Chofer!", VozPerfil.Tonalli);
yield return new HistoriaNivel1.Frase("general_despierta", "«¡Alto ahí, intruso!»", VozPerfil.LaBufa);
```

El sistema lee estos datos y genera automáticamente:
- `intro_panteon_0.wav` con voz de Tonalli
- `general_despierta.wav` con voz de La Bufa (grave)

---

## 🎯 ¿Por Qué No Se Generaron Antes?

Los audios del Nivel 2 y 3 **no se generaron automáticamente** porque:

1. El menú dice "Nivel 1" (confuso, pero genera los 3 niveles)
2. Solo genera audios **si no existen** (para no sobrescribir grabaciones reales)
3. Los archivos `.wav` no estaban en el repositorio

**Solución:** Simplemente ejecuta el menú **una vez** y todos los audios se crean.

---

## 📂 Archivos Generados

Después de ejecutar el menú, tendrás en `Assets/Game/Resources/Voz/`:

### Nivel 1 (ya existentes):
- ✅ 46 archivos (intro, sierra, serpientes, etc.)

### Nivel 2 (nuevos):
- 🆕 32 archivos (intro_panteon, momias, general, etc.)

### Nivel 3 (nuevos):
- 🆕 53 archivos (intro_ciudad, monjes, inquisidor, etc.)

**Total: 131 archivos de audio**

---

## 🐛 Solución de Problemas

### Error: "No hay ninguna voz en español instalada"
**Solución:**
1. Windows → Configuración → Hora e idioma → Voz
2. Agregar voz en español (España o México)
3. Reintentar

### Error: "No encuentro Tools/GenerarVoces.ps1"
**Solución:**
- El archivo debe estar en la raíz del proyecto: `<Proyecto>/Tools/GenerarVoces.ps1`
- Si no existe, el repositorio está incompleto

### Los audios suenan robotizados
**Solución:**
- Usa la **Opción A** (voces neuronales Piper)
- O reemplaza los archivos .wav con grabaciones reales manteniendo el mismo nombre

---

## ✅ Verificación Final

Después de generar los audios:

1. **Verificar archivos:** 
   ```
   Assets/Game/Resources/Voz/intro_panteon_0.wav
   Assets/Game/Resources/Voz/intro_ciudad_0.wav
   Assets/Game/Resources/Voz/general_despierta.wav
   Assets/Game/Resources/Voz/inquisidor_despierta.wav
   ```

2. **Probar en el juego:**
   - Jugar Nivel 2 → Debe escucharse a Tonalli narrando
   - Jugar Nivel 3 → Debe escucharse a Tonalli narrando
   - Jefes deben tener diálogos con voz

3. **Usar herramienta de verificación:**
   - Menú: `EvitandoElFin/Herramientas/Reporte de Audios Faltantes`
   - Debe reportar **0 archivos faltantes**

---

## 🎉 ¡Eso Es Todo!

El sistema está completamente implementado y funcional. Solo necesitas:

1. **Ejecutar el menú en Unity** (1 clic)
2. **Esperar 2-5 minutos**
3. **¡Listo!** Todos los audios están generados

**Ubicación del menú:**
```
Unity → EvitandoElFin → 9. Generar voces del Nivel 1 (Tonalli, La Bufa y Don Chuy)
```

---

**Fecha:** 23 de Noviembre 2026  
**Autor:** Sistema automatizado de generación de voces  
**Versión:** 1.0
