#!/usr/bin/env python3
"""
Script para generar automáticamente los 85 archivos de audio faltantes
usando la API de ElevenLabs.

Requisitos:
- pip install elevenlabs requests
- API Key de ElevenLabs (obtenerla en https://elevenlabs.io)

Uso:
1. Configurar ELEVENLABS_API_KEY en línea 20
2. Ejecutar: python generar_audios_nivel2y3.py
"""

import os
import time
import requests
from pathlib import Path

# ============================================================================
# CONFIGURACIÓN - EDITAR ESTAS LÍNEAS
# ============================================================================

# Tu API key de ElevenLabs (obtenerla en https://elevenlabs.io/app/settings)
ELEVENLABS_API_KEY = "TU_API_KEY_AQUI"

# Voice IDs de ElevenLabs (puedes cambiarlos según tus preferencias)
VOICE_ID_TONALLI = "21m00Tcm4TlvDq8ikWAM"  # Josh (voz masculina cálida)
VOICE_ID_GENERAL = "VR6AewLTigWG4xSOukaG"  # Antoni (voz masculina formal)
VOICE_ID_INQUISIDOR = "yoZ06aMxZJJ28mfd3POQ"  # Sam (voz masculina profunda)

# Directorio de salida (relativo a este script)
OUTPUT_DIR = "../Assets/Game/Resources/Voz"

# ============================================================================
# DATOS - NO EDITAR DEBAJO DE ESTA LÍNEA
# ============================================================================

AUDIOS_NIVEL2 = [
    # Intro Panteón
    ("intro_panteon_0.mp3", "Tonalli", "¡Mira hacia arriba, Chofer! Estamos en lo más alto de la Sierra de Guanajuato."),
    ("intro_panteon_1.mp3", "Tonalli", "En este collado rocoso fundaron el Panteón de Santa Paula hace casi dos siglos."),
    ("intro_panteon_2.mp3", "Tonalli", "La tierra de esta sierra tiene tantos minerales que los cuerpos nunca se pudrieron... quedaron momificados en la cantera."),
    ("intro_panteon_3.mp3", "Tonalli", "Pero esta noche la niebla de ultratumba los ha levantado. Debemos encender los 3 Cirios de Santa Paula para abrir la Cripta del General."),
    
    # Collado Sierra
    ("collado_sierra_0.mp3", "Tonalli", "El viento de la sierra sopla helado entre los riscos. Las veladoras apenas se sostienen."),
    ("collado_sierra_1.mp3", "Tonalli", "Fíjate bien en el camino de tepetate: las lápidas y las cruces vigilan cada curva."),
    
    # Momias
    ("momias_0.mp3", "Tonalli", "¡Cuidado con las peñas! Esas figuras vendadas son las Momias de Santa Paula."),
    ("momias_1.mp3", "Tonalli", "La peste del cólera se las llevó en 1833. Son veloces y atacan en jauría: esquiva sus zarpazos laterales y golpéalas con combo."),
    
    # Cirio 1
    ("cirio1_0.mp3", "Tonalli", "¡El primer Cirio de Santa Paula está ardiendo! Su flama esmeralda disipa la niebla del desfiladero."),
    
    # Catrines
    ("catrines_0.mp3", "Tonalli", "Allá adelante... ¿ves a ese espectro con levita, bombín y bastón? Es un Catrín de época."),
    ("catrines_1.mp3", "Tonalli", "Su bastón dispara proyectiles de fuego fatuo turquesa. Acércate en zigzag y no te quedes quieto."),
    
    # Criptas Antiguas
    ("criptas_antiguas_0.mp3", "Tonalli", "Mira esas gavetas talladas en la roca viva del cerro. Familias enteras descansan aquí desde tiempos virreinales."),
    ("criptas_antiguas_1.mp3", "Tonalli", "Respeta las tumbas, Chofer. Solo buscamos el camino de regreso al mundo de los vivos."),
    
    # Cirio 2
    ("cirio2_0.mp3", "Tonalli", "¡Dos cirios encendidos! La piedra de la cumbre está retumbando... el viejo batallón militar se está alineando."),
    
    # Cuervos
    ("cuervos_0.mp3", "Tonalli", "¡Graznidos en las cruces altas! Son Cuervos de Cripta."),
    ("cuervos_1.mp3", "Tonalli", "Bajan en picada cuando menos lo esperas. Esquiva justo antes de que toquen el suelo y contragolpea."),
    
    # Cirio 3
    ("cirio3_0.mp3", "Tonalli", "¡Los tres cirios están encendidos! Las rejas del Gran Mausoleo en la peña se han abierto de par en par."),
    ("cirio3_1.mp3", "Tonalli", "En la cumbre espera el General... el militar que juró proteger la sierra y ni la muerte le quitó el sable."),
    
    # Cripta
    ("cripta_cerrada_0.mp3", "Tonalli", "Las rejas de hierro forjado del Mausoleo siguen selladas por el frío espectral. Necesitas los 3 Cirios de Santa Paula."),
    ("cripta_0.mp3", "Tonalli", "Este es el Gran Mausoleo. El aire huele a pólvora vieja y cempasúchil marchito."),
    ("cripta_1.mp3", "Tonalli", "El General no descansará hasta medir su sable con un alma digna. ¡Entra con valor!"),
    
    # Jefe General
    ("jefe_general_0.mp3", "Tonalli", "¡Desenvainó su sable de mando! Cuidado con su estocada frontal telegrafiada en verde."),
    ("jefe_general_1.mp3", "Tonalli", "Cuando falle su golpe o gire en 360°, ¡aprovecha para castigarlo por la espalda!"),
    ("jefe_general_furia_0.mp3", "Tonalli", "¡La furia del batallón! Se prendió en fuego fatuo esmeralda."),
    ("jefe_general_furia_1.mp3", "Tonalli", "¡Salta cuando clave su sable en la tierra o la onda de lápidas te alcanzará!"),
    ("jefe_general_derrota_0.mp3", "Tonalli", "¡El General rinde honores! Mira cómo su sable se convierte en ceniza dorada."),
    ("jefe_general_derrota_1.mp3", "Tonalli", "Has recuperado el Rosario de Plata: el recuerdo de tu familia en Guanajuato."),
    ("jefe_general_derrota_2.mp3", "Tonalli", "Cruza el portal del risco: el desfiladero nos lleva directo hacia los callejones coloniales de la ciudad."),
    
    # General (voz del jefe)
    ("general_despierta.mp3", "General", "«¡Alto ahí, intruso! Este camposanto militar está bajo mi mando.»"),
    ("general_fase2.mp3", "General", "«¡Por el batallón de Santa Paula! ¡Fuego fatuo a discreción!»"),
    ("general_derrotado.mp3", "General", "«Buen combate, soldado... has demostrado tener un alma digna de cruzar la sierra.»"),
]

AUDIOS_NIVEL3 = [
    # Intro Ciudad
    ("intro_ciudad_0.mp3", "Tonalli", "¡Hemos llegado a la cañada de la sierra, Chofer!"),
    ("intro_ciudad_1.mp3", "Tonalli", "Mira cómo las casas coloniales trepan por los riscos de cantera rosa, amarillo ocre y añil."),
    ("intro_ciudad_2.mp3", "Tonalli", "Esta es la ciudad virreinal de Guanajuato... una joya nacida de las minas y de las peñas."),
    ("intro_ciudad_3.mp3", "Tonalli", "Pero las campanas de la basílica no doblan a misa: doblan por una condena inquisitorial que atrapó a este valle en el purgatorio."),
    ("intro_ciudad_4.mp3", "Tonalli", "Debemos romper los 3 Sellos Inquisitoriales ocultos en las escalinatas para abrir las puertas de la Basílica."),
    
    # Mirador Pípila
    ("mirador_pipila_0.mp3", "Tonalli", "Desde este mirador en la sierra se contempla toda la cañada. Los techos de teja y los tiros de mina bajan hasta el fondo."),
    ("mirador_pipila_1.mp3", "Tonalli", "Sigue el sendero empedrado: las sombras virreinales no quieren que llegues a la basílica."),
    
    # Monjes
    ("monjes_0.mp3", "Tonalli", "¡Cuidado con esas figuras de hábito oscuro! Son Monjes Penitentes de la Inquisición."),
    ("monjes_1.mp3", "Tonalli", "Blanden candelabros de plata candente que estallan en fuego purificador. Espera a que golpeen el suelo y esquiva hacia atrás."),
    
    # Sello 1
    ("sello1_0.mp3", "Tonalli", "¡El primer Sello Inquisitorial ha sido destruido! Una campana de bronce dobló en la montaña."),
    
    # Callejón del Beso
    ("callejon_beso_0.mp3", "Tonalli", "Detente un momento y mira hacia arriba... ¿ves esos dos balcones de hierro casi tocándose?"),
    ("callejon_beso_1.mp3", "Tonalli", "Apenas setenta centímetros de distancia en la grieta de la peña. Este es el legendario Callejón del Beso."),
    ("callejon_beso_2.mp3", "Tonalli", "Aquí Carmen y Don Luis se juraron amor eterno a escondidas, desafiando la ira y la condena de su padre y del clérigo."),
    ("callejon_beso_3.mp3", "Tonalli", "Su amor inocente dejó una bendición en este rincón: siente cómo tu alma recupera energía espiritual."),
    
    # Túneles Mineros
    ("tuneles_mineros_0.mp3", "Tonalli", "Estos arcos de cantera sostienen los túneles subterráneos de Guanajuato. Por aquí corría la plata y los ríos de la sierra."),
    ("tuneles_mineros_1.mp3", "Tonalli", "Las sombras acechan en cada recodo oscuro. No te detengas: la luz de los faroles coloniales te protege."),
    
    # Sello 2
    ("sello2_0.mp3", "Tonalli", "¡Dos sellos rotos! El gran portón de cantera de la Basílica empieza a crujir."),
    ("sello2_1.mp3", "Tonalli", "La Inquisición siente el peso de tu fe, Juan. Solo falta un sello."),
    
    # Sombras Callejón
    ("sombras_callejon_0.mp3", "Tonalli", "¡Sombras en las cornisas! Son los guardianes del tribunal virreinal."),
    ("sombras_callejon_1.mp3", "Tonalli", "Se deslizan entre los empedrados. Mantén el ritmo con tu combo y no permitas que te rodeen en las esquinas estrechas."),
    
    # Sello 3
    ("sello3_0.mp3", "Tonalli", "¡Los tres Sellos Inquisitoriales han caído! El atrio de la Basílica de Guanajuato está totalmente abierto."),
    ("sello3_1.mp3", "Tonalli", "Allí aguarda el Clérigo Inquisidor, el juez implacable que retiene tu alma en este juicio eterno."),
    
    # Basílica
    ("basilica_cerrada_0.mp3", "Tonalli", "Las puertas de madera y hierro de la Basílica siguen selladas por los rezos inquisitoriales. Encuentra los 3 Sellos."),
    ("basilica_0.mp3", "Tonalli", "El monumental atrio de la Basílica... entre las dos grandes torres campanario."),
    ("basilica_1.mp3", "Tonalli", "Este es el juicio final de tu existencia, Chofer. Por tu familia, por tu vida... ¡no vaciles!"),
    
    # Jefe Inquisidor
    ("jefe_inquisidor_0.mp3", "Tonalli", "¡El Inquisidor abre su libro de sentencias! Mira los círculos dorados de fuego sagrado en el suelo."),
    ("jefe_inquisidor_1.mp3", "Tonalli", "¡Esquiva los pilares de luz y arremete contra él cuando cierre el libro para dictar sentencia!"),
    ("jefe_inquisidor_furia_0.mp3", "Tonalli", "¡La máscara de devoción ha caído! Desplegó alas de cantera y fuego negro: ¡es la Sombra de la Herejía!"),
    ("jefe_inquisidor_furia_1.mp3", "Tonalli", "¡Cuidado con las campanas que caen de la torre! ¡Salta para librar la onda expansiva!"),
    ("jefe_inquisidor_derrota_0.mp3", "Tonalli", "¡La condena se ha roto para siempre! Guanajuato entero resplandece bajo el manto místico."),
    ("jefe_inquisidor_derrota_1.mp3", "Tonalli", "Has recuperado el último fragmento de tu ser: El Amanecer y el Regreso."),
    ("jefe_inquisidor_derrota_2.mp3", "Tonalli", "Mira ese portal dorado que emana del atrio: es el latido de tu corazón en el mundo real."),
    ("jefe_inquisidor_derrota_3.mp3", "Tonalli", "¡Gracias por tu valor, Juan! Yo soy Tonalli, y mi misión como guía ha terminado. ¡Corre hacia el portal y vuelve a casa!"),
    
    # Inquisidor (voz del jefe)
    ("inquisidor_despierta.mp3", "Inquisidor", "«¡En el nombre del tribunal de la fe, tu alma queda sentenciada al fuego!»"),
    ("inquisidor_fase2.mp3", "Inquisidor", "«¡Que las campanas de la basílica doblen tu condena final!»"),
    ("inquisidor_derrotado.mp3", "Inquisidor", "«La herejía no era tuya... era mi soberbia. Vuelve a la luz del amanecer, caminante.»"),
]

# ============================================================================
# FUNCIONES
# ============================================================================

def obtener_voice_id(personaje):
    """Retorna el voice ID según el personaje."""
    if personaje == "Tonalli":
        return VOICE_ID_TONALLI
    elif personaje == "General":
        return VOICE_ID_GENERAL
    elif personaje == "Inquisidor":
        return VOICE_ID_INQUISIDOR
    else:
        return VOICE_ID_TONALLI

def generar_audio(texto, nombre_archivo, personaje):
    """
    Genera un archivo de audio usando la API de ElevenLabs.
    
    Args:
        texto: El texto a convertir en audio
        nombre_archivo: Nombre del archivo de salida (ej: "intro_panteon_0.mp3")
        personaje: "Tonalli", "General" o "Inquisidor"
    
    Returns:
        True si se generó exitosamente, False en caso contrario
    """
    voice_id = obtener_voice_id(personaje)
    
    url = f"https://api.elevenlabs.io/v1/text-to-speech/{voice_id}"
    headers = {
        "Accept": "audio/mpeg",
        "Content-Type": "application/json",
        "xi-api-key": ELEVENLABS_API_KEY
    }
    
    # Configuración de voz (ajustada para español mexicano)
    data = {
        "text": texto,
        "model_id": "eleven_multilingual_v2",
        "voice_settings": {
            "stability": 0.75,
            "similarity_boost": 0.70,
            "style": 0.35,
            "use_speaker_boost": True
        }
    }
    
    try:
        print(f"  Generando: {nombre_archivo}...", end=" ")
        response = requests.post(url, json=data, headers=headers)
        
        if response.status_code == 200:
            # Crear directorio si no existe
            output_path = Path(OUTPUT_DIR)
            output_path.mkdir(parents=True, exist_ok=True)
            
            # Guardar archivo
            filepath = output_path / nombre_archivo
            with open(filepath, "wb") as f:
                f.write(response.content)
            
            print("✓")
            return True
        else:
            print(f"✗ Error {response.status_code}: {response.text}")
            return False
            
    except Exception as e:
        print(f"✗ Excepción: {str(e)}")
        return False

def main():
    """Función principal."""
    print("=" * 70)
    print("GENERADOR DE AUDIOS - EVITANDO EL FIN")
    print("=" * 70)
    print()
    
    # Verificar API key
    if ELEVENLABS_API_KEY == "TU_API_KEY_AQUI":
        print("❌ ERROR: Debes configurar tu ELEVENLABS_API_KEY en la línea 20")
        print("   Obtén tu API key en: https://elevenlabs.io/app/settings")
        return
    
    # Combinar todas las listas
    todos_audios = AUDIOS_NIVEL2 + AUDIOS_NIVEL3
    total = len(todos_audios)
    
    print(f"Total de archivos a generar: {total}")
    print(f"- Nivel 2: {len(AUDIOS_NIVEL2)} archivos")
    print(f"- Nivel 3: {len(AUDIOS_NIVEL3)} archivos")
    print(f"\nDirectorio de salida: {OUTPUT_DIR}")
    print()
    
    # Confirmar
    respuesta = input("¿Continuar? (s/n): ")
    if respuesta.lower() != 's':
        print("Cancelado.")
        return
    
    print()
    print("Generando audios...")
    print("-" * 70)
    
    # Generar cada audio
    exitosos = 0
    fallidos = 0
    
    for i, (nombre_archivo, personaje, texto) in enumerate(todos_audios, 1):
        print(f"[{i}/{total}] {personaje}: ", end="")
        
        if generar_audio(texto, nombre_archivo, personaje):
            exitosos += 1
        else:
            fallidos += 1
        
        # Pequeña pausa para no saturar la API
        time.sleep(0.5)
    
    print("-" * 70)
    print()
    print("=" * 70)
    print("RESUMEN")
    print("=" * 70)
    print(f"✓ Exitosos: {exitosos}")
    print(f"✗ Fallidos:  {fallidos}")
    print(f"📁 Ubicación: {OUTPUT_DIR}")
    print()
    
    if fallidos == 0:
        print("🎉 ¡Todos los audios se generaron correctamente!")
    else:
        print("⚠️  Algunos archivos no se generaron. Revisa los errores arriba.")
    
    print()

if __name__ == "__main__":
    main()
