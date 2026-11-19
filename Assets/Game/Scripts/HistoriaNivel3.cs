using System.Collections.Generic;

/// <summary>
/// Guion, narrativa y capítulos del Nivel 3: La Cañada de Guanajuato (Los Callejones y la Basílica).
/// La Sierra de Guanajuato desciende abruptamente hacia una cañada donde los riscos se convierten
/// en callejones estrechos, acueductos, túneles subterráneos de piedra y el Callejón del Beso,
/// culminando en el atrio de la Basílica de Guanajuato donde aguarda el Clérigo Inquisidor.
/// </summary>
public static class HistoriaNivel3
{
    public const string NombreGuia = "TONALLI";
    public const string NombreJefe = "EL CLÉRIGO INQUISIDOR";
    public const string SubtituloJefe = "La Sombra del Callejón del Beso";

    public const string TituloRecuerdo = "RECUERDO 3  ·  EL AMANECER Y EL REGRESO";
    public const string TextoRecuerdo =
        "El sonido rítmico de un monitor cardíaco en la oscuridad. El olor a ozono, asfalto y lluvia fresca.\n" +
        "La voz temblorosa de un paramédico que grita con júbilo sobre el ruido de las sirenas:\n" +
        "«¡Recuperamos el pulso! ¡Respira por sí solo! ¡Llegaremos al hospital a tiempo!»\n" +
        "La luz dorada del amanecer asoma por los cerros de Guanajuato. ¡He vencido a la muerte!";

    static readonly Dictionary<string, string[]> lineas = new Dictionary<string, string[]>
    {
        { "intro_ciudad", new[] {
            "¡Hemos llegado a la cañada de la sierra, Chofer!",
            "Mira cómo las casas coloniales trepan por los riscos de cantera rosa, amarillo ocre y añil.",
            "Esta es la ciudad virreinal de Guanajuato... una joya nacida de las minas y de las peñas.",
            "Pero las campanas de la basílica no doblan a misa: doblan por una condena inquisitorial que atrapó a este valle en el purgatorio.",
            "Debemos romper los 3 Sellos Inquisitoriales ocultos en las escalinatas para abrir las puertas de la Basílica."
        } },
        { "mirador_pipila", new[] {
            "Desde este mirador en la sierra se contempla toda la cañada. Los techos de teja y los tiros de mina bajan hasta el fondo.",
            "Sigue el sendero empedrado: las sombras virreinales no quieren que llegues a la basílica."
        } },
        { "monjes", new[] {
            "¡Cuidado con esas figuras de hábito oscuro! Son Monjes Penitentes de la Inquisición.",
            "Blanden candelabros de plata candente que estallan en fuego purificador. Espera a que golpeen el suelo y esquiva hacia atrás."
        } },
        { "sello1", new[] {
            "¡El primer Sello Inquisitorial ha sido destruido! Una campana de bronce dobló en la montaña."
        } },
        { "callejon_beso", new[] {
            "Detente un momento y mira hacia arriba... ¿ves esos dos balcones de hierro casi tocándose?",
            "Apenas setenta centímetros de distancia en la grieta de la peña. Este es el legendario Callejón del Beso.",
            "Aquí Carmen y Don Luis se juraron amor eterno a escondidas, desafiando la ira y la condena de su padre y del clérigo.",
            "Su amor inocente dejó una bendición en este rincón: siente cómo tu alma recupera energía espiritual."
        } },
        { "tuneles_mineros", new[] {
            "Estos arcos de cantera sostienen los túneles subterráneos de Guanajuato. Por aquí corría la plata y los ríos de la sierra.",
            "Las sombras acechan en cada recodo oscuro. No te detengas: la luz de los faroles coloniales te protege."
        } },
        { "sello2", new[] {
            "¡Dos sellos rotos! El gran portón de cantera de la Basílica empieza a crujir.",
            "La Inquisición siente el peso de tu fe, Juan. Solo falta un sello."
        } },
        { "sombras_callejon", new[] {
            "¡Sombras en las cornisas! Son los guardianes del tribunal virreinal.",
            "Se deslizan entre los empedrados. Mantén el ritmo con tu combo y no permitas que te rodeen en las esquinas estrechas."
        } },
        { "sello3", new[] {
            "¡Los tres Sellos Inquisitoriales han caído! El atrio de la Basílica de Guanajuato está totalmente abierto.",
            "Allí aguarda el Clérigo Inquisidor, el juez implacable que retiene tu alma en este juicio eterno."
        } },
        { "basilica_cerrada", new[] {
            "Las puertas de madera y hierro de la Basílica siguen selladas por los rezos inquisitoriales. Encuentra los 3 Sellos."
        } },
        { "basilica", new[] {
            "El monumental atrio de la Basílica... entre las dos grandes torres campanario.",
            "Este es el juicio final de tu existencia, Chofer. Por tu familia, por tu vida... ¡no vaciles!"
        } },
        { "jefe_inquisidor", new[] {
            "¡El Inquisidor abre su libro de sentencias! Mira los círculos dorados de fuego sagrado en el suelo.",
            "¡Esquiva los pilares de luz y arremete contra él cuando cierre el libro para dictar sentencia!"
        } },
        { "jefe_inquisidor_furia", new[] {
            "¡La máscara de devoción ha caído! Desplegó alas de cantera y fuego negro: ¡es la Sombra de la Herejía!",
            "¡Cuidado con las campanas que caen de la torre! ¡Salta para librar la onda expansiva!"
        } },
        { "jefe_inquisidor_derrota", new[] {
            "¡La condena se ha roto para siempre! Guanajuato entero resplandece bajo el manto místico.",
            "Has recuperado el último fragmento de tu ser: El Amanecer y el Regreso.",
            "Mira ese portal dorado que emana del atrio: es el latido de tu corazón en el mundo real.",
            "¡Gracias por tu valor, Juan! Yo soy Tonalli, y mi misión como guía ha terminado. ¡Corre hacia el portal y vuelve a casa!"
        } }
    };

    public static readonly string[,] Capitulos =
    {
        { "intro_ciudad", "La cañada colonial" },
        { "mirador_pipila", "El mirador de la sierra" },
        { "monjes", "Los Penitentes del candelero" },
        { "sello1", "El primer sello roto" },
        { "callejon_beso", "El Callejón del Beso" },
        { "tuneles_mineros", "Los túneles de cantera" },
        { "sello2", "El crujido del atrio" },
        { "sombras_callejon", "Las sombras del virreinato" },
        { "sello3", "Las puertas de la Basílica" },
        { "basilica", "El Gran Atrio" },
        { "jefe_inquisidor", "El libro de juicios" },
        { "jefe_inquisidor_furia", "La sombra alada" },
        { "jefe_inquisidor_derrota", "El despertar de Juan" }
    };

    public static readonly string[,] Actos =
    {
        { "intro_ciudad", "Los desfiladeros coloniales" },
        { "callejon_beso", "La leyenda de los amantes" },
        { "basilica", "El Juicio en la Basílica" }
    };

    public static string[] Lineas(string id)
    {
        string[] l;
        return lineas.TryGetValue(id, out l) ? l : null;
    }

    public static string ClaveLinea(string id, int i) { return id + "_" + i; }

    public static int IndiceCapitulo(string id)
    {
        for (int i = 0; i < Capitulos.GetLength(0); i++) if (Capitulos[i, 0] == id) return i;
        return -1;
    }

    public static string TituloCapitulo(string id)
    {
        for (int i = 0; i < Capitulos.GetLength(0); i++) if (Capitulos[i, 0] == id) return Capitulos[i, 1];
        return null;
    }

    public static IEnumerable<HistoriaNivel1.Frase> TodasLasFrases()
    {
        foreach (KeyValuePair<string, string[]> kv in lineas)
            for (int i = 0; i < kv.Value.Length; i++)
                yield return new HistoriaNivel1.Frase(ClaveLinea(kv.Key, i), kv.Value[i], HistoriaNivel1.VozPerfil.Tonalli);

        yield return new HistoriaNivel1.Frase("inquisidor_despierta", "«¡En el nombre del tribunal de la fe, tu alma queda sentenciada al fuego!»", HistoriaNivel1.VozPerfil.LaBufa);
        yield return new HistoriaNivel1.Frase("inquisidor_fase2", "«¡Que las campanas de la basílica doblen tu condena final!»", HistoriaNivel1.VozPerfil.LaBufa);
        yield return new HistoriaNivel1.Frase("inquisidor_derrotado", "«La herejía no era tuya... era mi soberbia. Vuelve a la luz del amanecer, caminante.»", HistoriaNivel1.VozPerfil.LaBufa);
    }
}
