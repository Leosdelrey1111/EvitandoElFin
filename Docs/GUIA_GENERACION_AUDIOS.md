# Guía de Generación de Audios - Evitando El Fin

## 📊 Resumen

**Total de audios a generar:** 85 archivos
- **Nivel 2 (Panteón):** 32 archivos
- **Nivel 3 (Ciudad):** 53 archivos

**Tiempo estimado:** 2-3 horas (dependiendo del servicio usado)

---

## 🎙️ Configuración de Voces

### Voz 1: Tonalli (Guía/Narrador)
**Perfil:**
- Género: Masculino
- Edad aparente: 40-50 años
- Tono: Cálido, sabio, compasivo
- Acento: Mexicano neutro (sin regionalismos marcados)
- Ritmo: Pausado, narrativo, contemplativo
- Volumen: Medio-alto (para destacar sobre música ambiente)

**Configuración ElevenLabs recomendada:**
- Voz sugerida: "Josh" o "Arnold" con ajuste de estabilidad alta
- Stability: 70-80%
- Clarity + Similarity Enhancement: 70-75%
- Style: 30-40%

**Archivos:** 76 archivos (la mayoría del contenido)

---

### Voz 2: General Momia (Jefe Nivel 2)
**Perfil:**
- Género: Masculino
- Edad aparente: 50-60 años
- Tono: Autoritario, militar, formal
- Acento: Mexicano formal (tono de mando)
- Ritmo: Pausado, sentencioso
- **Efecto especial:** Reverb moderado (eco de cripta de piedra)

**Configuración ElevenLabs recomendada:**
- Voz sugerida: "Antoni" o "Arnold"
- Stability: 75-85%
- Clarity + Similarity Enhancement: 65-70%
- Style: 50-60%
- **Post-procesamiento:** Agregar reverb en Audacity/Adobe Audition

**Archivos:** 3 archivos
- `general_despierta.mp3`
- `general_fase2.mp3`
- `general_derrotado.mp3`

---

### Voz 3: Clérigo Inquisidor (Jefe Nivel 3)
**Perfil:**
- Género: Masculino
- Edad aparente: 55-65 años
- Tono: Solemne, intimidante, sentencioso
- Acento: Español colonial/castellano antiguo
- Ritmo: Lento, pausado, ceremonioso
- **Efecto especial:** Ligero reverb (eco de catedral)

**Configuración ElevenLabs recomendada:**
- Voz sugerida: "Callum" o "Antoni" con ajuste dramático
- Stability: 80-90%
- Clarity + Similarity Enhancement: 70-75%
- Style: 60-70%
- **Post-procesamiento:** Agregar ligero reverb de catedral

**Archivos:** 3 archivos
- `inquisidor_despierta.mp3`
- `inquisidor_fase2.mp3`
- `inquisidor_derrotado.mp3`

---

## 📁 Estructura de Archivos

**Ubicación de destino:** `Assets/Game/Resources/Voz/`

**Formato requerido:**
- Formato: MP3
- Bitrate: 128-192 kbps (suficiente para voz)
- Sample rate: 44100 Hz o 22050 Hz
- Mono o Estéreo (Mono recomendado para ahorrar espacio)

---

## 🔄 Proceso de Generación Recomendado

### Opción A: ElevenLabs (Recomendado - Calidad Alta)
**URL:** https://elevenlabs.io

**Ventajas:**
- Voces muy naturales
- Excelente pronunciación en español
- Control fino de emociones
- 10,000 caracteres gratis/mes (suficiente para ~15-20 archivos)

**Proceso:**
1. Crear cuenta en ElevenLabs
2. Seleccionar voz para Tonalli (ej: "Josh" configurado a español)
3. Copiar texto del reporte
4. Generar audio
5. Descargar como MP3
6. Renombrar archivo con el nombre exacto requerido
7. Colocar en `Assets/Game/Resources/Voz/`

**Truco para ahorrar caracteres:**
- Genera varios archivos seguidos en una sola sesión
- Luego edita el audio largo en Audacity para separarlo

---

### Opción B: Azure Speech Services
**URL:** https://azure.microsoft.com/es-es/services/cognitive-services/text-to-speech/

**Ventajas:**
- 5 horas gratis/mes
- Voces en español mexicano nativas
- API para automatización
- Muy buena pronunciación de nombres mexicanos

**Voces recomendadas:**
- Tonalli: `es-MX-JorgeNeural` (masculino, cálido)
- General: `es-MX-LibertoNeural` (masculino, formal)
- Inquisidor: `es-ES-AlvaroNeural` (español castellano)

---

### Opción C: Google Cloud TTS
**URL:** https://cloud.google.com/text-to-speech

**Ventajas:**
- 1 millón de caracteres gratis/mes
- Voces WaveNet de alta calidad
- API REST simple

**Voces recomendadas:**
- `es-US-Neural2-B` (Tonalli)
- `es-US-Standard-B` (General)
- `es-ES-Standard-B` (Inquisidor)

---

## 📝 Lista de Archivos por Prioridad

### 🔴 PRIORIDAD ALTA - Cinemáticas Principales (12 archivos)

Estos son críticos para la experiencia narrativa:

#### Nivel 2:
1. `intro_panteon_0.mp3` - Entrada al panteón
2. `intro_panteon_3.mp3` - Objetivo del nivel
3. `momias_1.mp3` - Instrucción de combate
4. `catrines_1.mp3` - Instrucción de combate
5. `cuervos_1.mp3` - Instrucción de combate
6. `cirio3_1.mp3` - Preparación para jefe

#### Nivel 3:
7. `intro_ciudad_0.mp3` - Entrada a la ciudad
8. `intro_ciudad_4.mp3` - Objetivo del nivel
9. `monjes_1.mp3` - Instrucción de combate
10. `callejon_beso_0.mp3` - Historia del Callejón
11. `sello3_1.mp3` - Preparación para jefe
12. `jefe_inquisidor_derrota_3.mp3` - Cierre del juego

---

### 🟡 PRIORIDAD MEDIA - Diálogos de Jefes (6 archivos)

#### Nivel 2 - General Momia:
1. `general_despierta.mp3`
2. `general_fase2.mp3`
3. `general_derrotado.mp3`

#### Nivel 3 - Clérigo Inquisidor:
4. `inquisidor_despierta.mp3`
5. `inquisidor_fase2.mp3`
6. `inquisidor_derrotado.mp3`

---

### 🟢 PRIORIDAD BAJA - Ambiente y Lore (67 archivos restantes)

Estos enriquecen la experiencia pero no son críticos para jugabilidad.

---

## 🎬 Estrategia de Implementación Eficiente

### Fase 1: Generación Rápida (30 min)
1. Generar los 18 archivos de prioridad alta y media
2. Colocarlos en `Assets/Game/Resources/Voz/`
3. **Probar el juego** - ya será jugable con narrativa básica

### Fase 2: Completar Nivel 2 (45 min)
4. Generar los 26 archivos restantes de Nivel 2
5. Probar flujo completo del Nivel 2

### Fase 3: Completar Nivel 3 (60 min)
6. Generar los 41 archivos restantes de Nivel 3
7. Probar flujo completo del Nivel 3

### Fase 4: Pulido (30 min)
8. Revisar volúmenes y balance
9. Agregar efectos de reverb a jefes
10. Normalizar volumen de todos los archivos

---

## 🛠️ Herramientas de Post-Procesamiento

### Audacity (Gratis)
**URL:** https://www.audacityteam.org/

**Uso:**
1. **Normalizar volumen:** Effect > Normalize (-3.0 dB)
2. **Agregar reverb:** Effect > Reverb
   - General Momia: Room Size 50%, Damping 50%, Reverberance 40%
   - Inquisidor: Room Size 80%, Damping 30%, Reverberance 30%
3. **Exportar como MP3:** File > Export > Export as MP3

### Adobe Audition (Pago)
Alternativa profesional con mejores herramientas de reverb y ecualización.

---

## 📋 Checklist de Validación

Antes de considerar completo cada archivo:

- [ ] Nombre de archivo exacto (sensible a mayúsculas/minúsculas)
- [ ] Formato MP3
- [ ] Ubicado en `Assets/Game/Resources/Voz/`
- [ ] Volumen normalizado (-3 dB máximo)
- [ ] Sin silencios largos al inicio/final (máximo 0.5s)
- [ ] Pronunciación correcta de nombres mexicanos
- [ ] Efectos de reverb aplicados (solo jefes)
- [ ] Duración apropiada (no cortado abruptamente)

---

## 🎯 Script de Verificación Rápida

Una vez generados todos los archivos, puedes verificar en Unity usando:

**Menu:** `EvitandoElFin/Herramientas/Reporte de Audios Faltantes`

Este generará un nuevo reporte mostrando solo los archivos que aún faltan.

---

## 💡 Tips y Mejores Prácticas

### 1. Mantén Consistencia
- Usa la misma voz de ElevenLabs para todos los archivos de Tonalli
- Usa la misma configuración (stability, clarity, style)
- Genera todos los archivos de un personaje en la misma sesión

### 2. Prueba Temprano
- Genera 2-3 archivos de prueba primero
- Pruébalos en el juego
- Ajusta configuración si es necesario
- Luego genera el resto en batch

### 3. Organización
- Crea subcarpetas temporales: `Nivel2/`, `Nivel3/`, `Jefes/`
- Genera primero, organiza después
- Usa un script o batch rename para renombrar en masa

### 4. Backup
- Guarda los archivos originales sin procesar
- Mantén una copia de los archivos con reverb por separado
- Version control: haz commit después de cada lote

### 5. Control de Calidad
- Escucha cada archivo al menos una vez
- Verifica pronunciación de palabras clave:
  - Guanajuato
  - Panteón
  - Basílica
  - Inquisidor
  - Tonalli
  - Cempasúchil

---

## 🚀 Automatización (Avanzado)

Si quieres automatizar el proceso con la API de ElevenLabs:

```python
# Ejemplo con ElevenLabs API
import requests
import json

ELEVEN_API_KEY = "tu_api_key_aqui"
VOICE_ID = "21m00Tcm4TlvDq8ikWAM"  # Josh

def generar_audio(texto, nombre_archivo):
    url = f"https://api.elevenlabs.io/v1/text-to-speech/{VOICE_ID}"
    headers = {
        "Accept": "audio/mpeg",
        "Content-Type": "application/json",
        "xi-api-key": ELEVEN_API_KEY
    }
    data = {
        "text": texto,
        "model_id": "eleven_multilingual_v2",
        "voice_settings": {
            "stability": 0.75,
            "similarity_boost": 0.70,
            "style": 0.35
        }
    }
    
    response = requests.post(url, json=data, headers=headers)
    if response.status_code == 200:
        with open(f"Voz/{nombre_archivo}", "wb") as f:
            f.write(response.content)
        print(f"✓ {nombre_archivo}")
    else:
        print(f"✗ Error en {nombre_archivo}")

# Uso
generar_audio("¡Mira hacia arriba, Chofer!", "intro_panteon_0.mp3")
```

---

## 📞 Soporte

Si encuentras problemas:

1. **Pronunciación incorrecta:** Usa spelling hints en ElevenLabs
   - Ejemplo: "Gua-na-hua-to" en vez de "Guanajuato"

2. **Volumen inconsistente:** Normaliza todos con Audacity en batch

3. **Archivos no se cargan en Unity:** 
   - Verifica que están en `Resources/Voz/`
   - Verifica nombres exactos (case-sensitive)
   - Reimporta assets en Unity

---

## ✅ Resumen de Próximos Pasos

1. **Ahora mismo:** Generar los 18 archivos de prioridad alta/media
2. **Esta semana:** Completar Nivel 2 (32 archivos totales)
3. **Próxima semana:** Completar Nivel 3 (53 archivos totales)
4. **Final:** Pulido, balance de volumen, testing completo

**Tiempo total estimado:** 2-3 horas de trabajo activo + 30-60 min de procesamiento

---

**Fecha de creación:** 23 de Noviembre 2026
**Última actualización:** 23 de Noviembre 2026

¡Buena suerte con la generación de audios! 🎙️🎮
