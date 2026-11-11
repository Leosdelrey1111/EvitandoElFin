# Cinemática del accidente: qué hay y qué falta generar en Meshy

Escena: `SampleScene` (carretera). Guion: tráiler por la sierra de noche y con lluvia → falla de frenos → derrape → choque con la defensa → caída por la ladera → llega la ambulancia → paramédicos sacan al Chofer en camilla → la ambulancia se va → el alma se desprende.

## 1. Lo que ya está hecho (todo por código)
- **Asfalto mojado**: oscurecido, con charcos brillantes y reflejos en tiempo real de faros, sirenas y bengalas.
- **Lluvia que repica**: anillos de salpicadura en el pavimento, rocío que levantan las llantas, gotas que resbalan en el lente.
- **Tormenta**: relámpagos con trueno (uno cae justo en el choque) y haz de luz de los faros del camión bajo la lluvia.
- **Cámara**: plano holandés (inclinada) durante el derrape y corte a un plano bajo, al ras del asfalto, en el rescate.
- **Imagen de cine**: contraste frío, grano, bloom; la aberración cromática y el desenfoque suben en el derrape y el choque.
- **Derrape**: marcas negras de las llantas sobre el asfalto.
- **Rescate**: bengalas rojas sobre el pavimento mojado; el Chofer (el modelo de Meshy que ya tienes) va acostado en la camilla bajo una manta térmica; paramédicos provisionales con chaleco reflectante y paso animado.
- **Vehículos** con más detalle (espejos, escapes, guardafangos, luces de marcha, placa, barra de luces, faros, luces traseras, estrella de la vida).

Para aplicarlo: menú `EvitandoElFin / 5b. Mejorar cinemática` (o `5` para rehacer los vehículos con los detalles nuevos).

## 2. Lo que hace falta generar (en orden de importancia)

### A. Personajes (humanoides con rig, pose A, 1.75 m)
Meshy: *Text to 3D* → **Auto-rig** (biped humanoide) → descargar FBX con textura.

**1. Paramédico (hombre)** → archivo `Paramedico.fbx`
> Mexican male paramedic, 30 years old, stocky, short black hair, dark green EMS uniform with orange high-visibility reflective vest with silver stripes, black work boots, dark green cap, utility belt, stethoscope around the neck, realistic proportions, A-pose, game character, PBR textures.

**2. Paramédica (mujer)** → archivo `Paramedica.fbx`
> Mexican female paramedic, 28 years old, hair tied in a ponytail, navy blue EMS uniform with orange high-visibility reflective vest, black boots, medical gloves, small first-aid bag on the hip, realistic proportions, A-pose, game character, PBR textures.

*(Opcional)* **3. Conductor de la ambulancia** y **4. Policía de caminos** para llenar la escena.

### B. Animaciones de los paramédicos (Meshy permite de 2 a 10 s; todas en el sitio, sin desplazamiento)
Describe cada una en el cuadro de animación de Meshy. Nombres de archivo sugeridos entre paréntesis.

| Animación | Duración | Bucle | Descripción para Meshy |
|---|---|---|---|
| Idle alerta (`Idle`) | 4 s | sí | Standing alert, weight shifting slightly, looking around, hands relaxed at the sides, tense emergency worker. |
| Caminar cargando camilla (`CargarCamilla`) | 3 s | sí | Walking forward at a steady pace while carrying a stretcher: both arms extended low in front of the body at waist height gripping handles, shoulders tense, short quick steps. In place. |
| Correr hacia la escena (`Correr`) | 2 s | sí | Running fast with urgency, arms pumping, leaning forward slightly. In place. |
| Revisar pulso (`Revisar`) | 5 s | no | Kneeling on one knee, reaching forward with one hand to check a pulse on the neck, then looking up and shouting something to the partner. |
| Llamar con señas (`Llamar`) | 3 s | no | Waving one arm wide over the head calling a partner, shouting "over here", urgent. |
| Hablar por radio (`Radio`) | 4 s | sí | Holding a shoulder radio near the mouth with one hand, talking, other hand pointing, looking worried. |

### C. Animaciones del Chofer para esta escena (el modelo ya existe)
| Animación | Duración | Bucle | Descripción para Meshy |
|---|---|---|---|
| Tendido inconsciente (`Tendido`) | 4 s | sí | Lying flat on the back, unconscious, chest rising and falling very slowly, head tilted to one side, arms resting at the sides. |
| Sentado conduciendo (`Conduciendo`) | 4 s | sí | Sitting in a truck seat, both hands on the steering wheel, tired eyes, small head movements, looking at the road. *(solo si algún día se ve la cabina)* |
| Frenazo (`Frenazo`) | 2 s | no | Sitting and gripping a steering wheel with panic, slamming the right foot down, body thrown forward, then jerking back. *(opcional)* |

### D. Modelos estáticos (sin rig; Meshy *Text to 3D*)
Hoy son bloques de color hechos por código. Se pueden reemplazar para que se vea de verdad.

**5. Camilla** → `Camilla.fbx`
> Ambulance stretcher gurney, white metal frame, blue mattress with orange straps, folding legs with small black wheels, side rails, realistic, lying horizontally, game prop.

**6. Ambulancia mexicana** → `Ambulancia.fbx` *(mejor con las 4 llantas como objetos separados; si Meshy las une, las ruedas dejarán de girar)*
> Mexican ambulance van, white body, red stripe, blue star of life on the sides, roof light bar, rear double doors, Mexican license plate, slightly wet and dirty, realistic, game vehicle.

**7. Tráiler de carga** → `Camion.fbx`
> Heavy Mexican cargo truck, red cab with white box trailer, chrome exhaust stacks, big side mirrors, dented front bumper, mud flaps, dirty and wet, realistic, game vehicle.

**8. Utilería**: bengala de emergencia, triángulos reflectantes, conos naranjas, botiquín rojo, manta térmica plateada.

## 3. Cómo entregármelos
1. Guarda los modelos en `Assets/Game/Models/Cinematica/` con los nombres de arriba.
2. Guarda las animaciones en `Assets/Game/Animations/Cinematica/` (un FBX por clip o el pack completo, como hiciste con el Chofer).
3. Avísame. Yo hago lo siguiente: configurar el rig como Humanoid, armar el Animator de los paramédicos con tus clips, acomodar escala y posición, ajustar la camilla, reemplazar el camión y la ambulancia (con sus luces y ruedas) y ajustar las cámaras a los modelos nuevos.

El código ya busca modelos en `Assets/Game/Resources/Cinematica/` con los nombres `Paramedico`, `Paramedica` y `Camilla`. Si pones ahí un prefab con ese nombre, se usa en lugar del provisional sin tocar código. Si el prefab tiene un Animator con un parámetro bool `Caminando` y/o float `Velocidad`, se mueve solo según avance el personaje.

## 4. Ajustes rápidos (Inspector del objeto `ManagerCinematica`)
- `Pos Chofer En Camilla` / `Rot Chofer En Camilla`: si el Chofer se ve flotando o hundido en la camilla.
- `Bengalas De Emergencia`: apagar o prender las bengalas.
- `Lluvia Nocturna`: apagar o prender toda la tormenta.
- En el componente `Ambiente Lluvia Cinematica`: oscurecimiento y brillo del asfalto, reflejos en tiempo real (apágalos si va lento), relámpagos y su frecuencia, gotas en el lente, haz de faros y post-procesado.
