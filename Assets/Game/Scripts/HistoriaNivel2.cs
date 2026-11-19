using System.Collections.Generic;

/// <summary>
/// Guion, narrativa y capítulos del Nivel 2: La Sierra de Santa Paula (El Panteón de las Momias).
/// Todo ocurre en las cumbres escarpadas de la Sierra de Guanajuato, donde el camposanto virreinal
/// fue tallado directamente en los riscos de cantera rosa.
/// Basado en la leyenda de las Momias de Guanajuato (el brote de cólera de 1833 y el reposo negado).
/// </summary>
public static class HistoriaNivel2
{
    public const string NombreGuia = "TONALLI";
    public const string NombreJefe = "EL GENERAL MOMIA";
    public const string SubtituloJefe = "Centinela del Panteón de la Sierra";

    public const string TituloRecuerdo = "RECUERDO 2  ·  EL ROSARIO DE PLATA Y LA FAMILIA";
    public const string TextoRecuerdo =
        "Una pequeña casa de adobe y teja en la cañada de Guanajuato. El aroma a café de olla y canela.\n" +
        "Mi esposa atando un rosario de plata bendecida al espejo retrovisor de mi tráiler, y mi hija diciendo:\n" +
        "«Papá, cuídate en las curvas de la sierra. Aquí te esperamos para desayunar.»\n" +
        "Ese rosario... todavía cuelga en la cabina bajo la lluvia. ¡Tengo que volver con ellas!";

    static readonly Dictionary<string, string[]> lineas = new Dictionary<string, string[]>
    {
        { "intro_panteon", new[] {
            "¡Mira hacia arriba, Chofer! Estamos en lo más alto de la Sierra de Guanajuato.",
            "En este collado rocoso fundaron el Panteón de Santa Paula hace casi dos siglos.",
            "La tierra de esta sierra tiene tantos minerales que los cuerpos nunca se pudrieron... quedaron momificados en la cantera.",
            "Pero esta noche la niebla de ultratumba los ha levantado. Debemos encender los 3 Cirios de Santa Paula para abrir la Cripta del General."
        } },
        { "collado_sierra", new[] {
            "El viento de la sierra sopla helado entre los riscos. Las veladoras apenas se sostienen.",
            "Fíjate bien en el camino de tepetate: las lápidas y las cruces vigilan cada curva."
        } },
        { "momias", new[] {
            "¡Cuidado con las peñas! Esas figuras vendadas son las Momias de Santa Paula.",
            "La peste del cólera se las llevó en 1833. Son veloces y atacan en jauría: esquiva sus zarpazos laterales y golpéalas con combo."
        } },
        { "cirio1", new[] {
            "¡El primer Cirio de Santa Paula está ardiendo! Su flama esmeralda disipa la niebla del desfiladero."
        } },
        { "catrines", new[] {
            "Allá adelante... ¿ves a ese espectro con levita, bombín y bastón? Es un Catrín de época.",
            "Su bastón dispara proyectiles de fuego fatuo turquesa. Acércate en zigzag y no te quedes quieto."
        } },
        { "criptas_antiguas", new[] {
            "Mira esas gavetas talladas en la roca viva del cerro. Familias enteras descansan aquí desde tiempos virreinales.",
            "Respeta las tumbas, Chofer. Solo buscamos el camino de regreso al mundo de los vivos."
        } },
        { "cirio2", new[] {
            "¡Dos cirios encendidos! La piedra de la cumbre está retumbando... el viejo batallón militar se está alineando."
        } },
        { "cuervos", new[] {
            "¡Graznidos en las cruces altas! Son Cuervos de Cripta.",
            "Bajan en picada cuando menos lo esperas. Esquiva justo antes de que toquen el suelo y contragolpea."
        } },
        { "cirio3", new[] {
            "¡Los tres cirios están encendidos! Las rejas del Gran Mausoleo en la peña se han abierto de par en par.",
            "En la cumbre espera el General... el militar que juró proteger la sierra y ni la muerte le quitó el sable."
        } },
        { "cripta_cerrada", new[] {
            "Las rejas de hierro forjado del Mausoleo siguen selladas por el frío espectral. Necesitas los 3 Cirios de Santa Paula."
        } },
        { "cripta", new[] {
            "Este es el Gran Mausoleo. El aire huele a pólvora vieja y cempasúchil marchito.",
            "El General no descansará hasta medir su sable con un alma digna. ¡Entra con valor!"
        } },
        { "jefe_general", new[] {
            "¡Desenvainó su sable de mando! Cuidado con su estocada frontal telegrafiada en verde.",
            "Cuando falle su golpe o gire en 360°, ¡aprovecha para castigarlo por la espalda!"
        } },
        { "jefe_general_furia", new[] {
            "¡La furia del batallón! Se prendió en fuego fatuo esmeralda.",
            "¡Salta cuando clave su sable en la tierra o la onda de lápidas te alcanzará!"
        } },
        { "jefe_general_derrota", new[] {
            "¡El General rinde honores! Mira cómo su sable se convierte en ceniza dorada.",
            "Has recuperado el Rosario de Plata: el recuerdo de tu familia en Guanajuato.",
            "Cruza el portal del risco: el desfiladero nos lleva directo hacia los callejones coloniales de la ciudad."
        } }
    };

    public static readonly string[,] Capitulos =
    {
        { "intro_panteon", "El collado de las momias" },
        { "collado_sierra", "El viento de Santa Paula" },
        { "momias", "Los cuerpos de cantera" },
        { "cirio1", "El primer cirio esmeralda" },
        { "catrines", "El Catrín de levita" },
        { "criptas_antiguas", "Las gavetas virreinales" },
        { "cirio2", "El segundo cirio" },
        { "cuervos", "Los cuervos de las cruces" },
        { "cirio3", "Las rejas de la cumbre" },
        { "cripta", "El Gran Mausoleo" },
        { "jefe_general", "El sable del General" },
        { "jefe_general_furia", "Fuego fatuo en la sierra" },
        { "jefe_general_derrota", "El honor del camposanto" }
    };

    public static readonly string[,] Actos =
    {
        { "intro_panteon", "Las sombras de Santa Paula" },
        { "criptas_antiguas", "El camposanto de la sierra" },
        { "cripta", "El Centinela de las Criptas" }
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

        yield return new HistoriaNivel1.Frase("general_despierta", "«¡Alto ahí, intruso! Este camposanto militar está bajo mi mando.»", HistoriaNivel1.VozPerfil.LaBufa);
        yield return new HistoriaNivel1.Frase("general_fase2", "«¡Por el batallón de Santa Paula! ¡Fuego fatuo a discreción!»", HistoriaNivel1.VozPerfil.LaBufa);
        yield return new HistoriaNivel1.Frase("general_derrotado", "«Buen combate, soldado... has demostrado tener un alma digna de cruzar la sierra.»", HistoriaNivel1.VozPerfil.LaBufa);
    }
}
