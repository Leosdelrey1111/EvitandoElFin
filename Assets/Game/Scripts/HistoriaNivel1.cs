using System.Collections.Generic;

/// <summary>
/// Guion del Nivel 1 (Sierra de Guanajuato). Basado en la leyenda de la joven convertida en piedra
/// junto a la Cueva Vieja del Cerro de La Bufa: solo un valiente puede romper el hechizo cargándola cuesta abajo
/// sin mirar atrás; si voltea, la joven se vuelve serpiente. De ahí salen los enemigos y las habilidades del jefe.
/// </summary>
public static class HistoriaNivel1
{
    public const string NombreGuia = "TONALLI";
    public const string NombreJefe = "LA BUFA";
    public const string SubtituloJefe = "La Joven de Piedra";

    public const string TituloRecuerdo = "RECUERDO 1  ·  LA PROMESA";
    public const string TextoRecuerdo =
        "Una carretera de noche. Un teléfono que vibra sobre el tablero y una voz que dice:\n" +
        "«Llega antes del amanecer.»\nPrometí que llegaría… ¿a quién?";

    static readonly Dictionary<string, string[]> lineas = new Dictionary<string, string[]>
    {
        { "intro", new[] {
            "¡Por fin despiertas! Soy Tonalli, un alebrije. Tu alma se soltó del cuerpo en el accidente y vine a guiarte.",
            "Esta es la Sierra de Guanajuato. Bajo el Cerro de La Bufa se cuenta una leyenda… y esta noche se está haciendo realidad.",
            "Dicen que una joven fue convertida en piedra junto a la Cueva Vieja. Un hechizo la retiene y solo un valiente podría romperlo.",
            "Reúne tres tótems: son los pedazos de ese encanto. Con ellos se abre la Cueva Vieja. Si te pierdes, pulsa R y te marco el camino."
        } },
        { "sierra", new[] {
            "Mira esos cerros de cantera. Dicen que La Bufa guarda en sus entrañas paredes de oro y pisos de plata.",
            "Por eso tantos subieron a buscarla… y por eso la montaña tiene guardianes."
        } },
        { "serpientes", new[] {
            "¡Cuidado, serpientes! No son comunes: nacen del hechizo. La leyenda dice que la joven, al cargarla, se vuelve serpiente.",
            "Son rápidas pero frágiles. Golpea con combo y esquiva cuando se enrosquen."
        } },
        { "comerciante", new[] {
            "Mira, una luz entre los nopales. Es Don Chuy, un mercader de ánimas: cambia tus almas por ayuda.",
            "Acércate y pulsa E. Cuantas más almas recojas, mejores cosas podrás comprarle. Los tótems no se gastan: esos son para La Bufa."
        } },
        { "totem1", new[] {
            "¡Un tótem! Siente cómo vibra: guarda un pedazo del encanto que la mantiene dormida. Faltan dos."
        } },
        { "arroyo", new[] {
            "Este arroyo lleva siglos seco. Dicen que bajaba agua clara desde La Bufa, hasta que el hechizo cayó sobre la sierra.",
            "Mira bien las piedras del cauce: algunas parecen demasiado quietas… ¡Cuidado con lo que se arrastra!"
        } },
        { "tesoro", new[] {
            "¿Ves esas bocaminas? Muchos entraron por la plata y ninguno volvió a salir. La Bufa no perdona la codicia."
        } },
        { "hombres_piedra", new[] {
            "Allá adelante… ¿ves esas figuras? Son hombres de piedra. Dicen que son los jóvenes que intentaron salvarla.",
            "Cargaron a la joven rumbo a la basílica sin mirar atrás… pero todos voltearon. Ahora vigilan el sendero, atrapados como ella.",
            "Son lentos, pero su puño duele. Esquiva cuando se agachen."
        } },
        { "totem2", new[] {
            "Dos tótems. La piedra de La Bufa se siente cada vez más fría… ya casi nos escucha."
        } },
        { "cruces", new[] {
            "Esas cruces con veladoras las dejaron quienes subieron a la sierra y no regresaron. Alguien las sigue encendiendo cada noche.",
            "Una veladora le muestra el camino a las ánimas perdidas. Por eso no estás tan solo como crees."
        } },
        { "comadres", new[] {
            "Escucha… ¿murmullos? Son las Comadres: dos viejas que se pelearon por un hombre y quedaron hechas roca.",
            "Desde entonces solo hacen una cosa: chismear. Y sus murmullos hieren como piedras. ¡Muévete y acércate para callarlas!"
        } },
        { "totem3", new[] {
            "¡Tres tótems! El encanto se debilita. La Cueva Vieja está abierta al final del sendero. Ahí duerme ella: La Bufa."
        } },
        { "corona", new[] {
            "Allá, entre la niebla: la corona de La Bufa. Esas rocas guardan la Cueva Vieja, donde la joven duerme hecha piedra.",
            "Lo que sigue es lo más difícil, Chofer. Respira, afina el esquive… y no mires atrás."
        } },
        { "cueva_cerrada", new[] {
            "Esta es la Cueva Vieja, pero sigue sellada. Vuelve cuando tengas los tres tótems."
        } },
        { "cueva", new[] {
            "Es aquí. Una joven que durmió siglos hecha piedra no despierta de buen humor. Entra con cuidado.",
            "Si te muestra el aviso rojo en el suelo, ¡esquiva o salta! Y no te quedes quieto: ella solo se detiene cuando falla."
        } },
        { "jefe_despierta", new[] {
            "¡Despertó! Ella es La Bufa. El hechizo la volvió monstruo, pero dentro de la piedra sigue siendo una joven.",
            "Aprovecha cuando se estrelle contra el suelo: queda aturdida un momento."
        } },
        { "jefe_invoca", new[] {
            "Llamó a sus pretendientes de piedra. ¡Ocúpate de ellos o te rodearán!"
        } },
        { "jefe_fase2", new[] {
            "¡No mires atrás! La leyenda se cumple: la piedra se rompe y ella se vuelve serpiente.",
            "Su onda de choque se esquiva ¡saltando! Y cuidado con las mordidas desde el aire."
        } },
        { "jefe_final", new[] {
            "Está herida. Un último esfuerzo, Chofer. ¡Casi la liberas!"
        } },
        { "jefe_derrotado", new[] {
            "¡Lo lograste! El hechizo se rompió. Mira cómo la luz la libera… ya no es de piedra.",
            "Cruza el portal espiritual: tu camino sigue. Y llevas contigo un recuerdo que ya habías perdido."
        } },
    };

    public static string[] Lineas(string id)
    {
        string[] l;
        return lineas.TryGetValue(id, out l) ? l : null;
    }

    /// <summary>Clave del archivo de voz de la línea i de una historia (Assets/Game/Resources/Voz/clave.wav).</summary>
    public static string ClaveLinea(string id, int i) { return id + "_" + i; }

    // ================================================================== VOCES

    public enum VozPerfil { Tonalli, LaBufa, DonChuy }

    public struct Frase
    {
        public string clave, texto;
        public VozPerfil voz;
        public Frase(string clave, string texto, VozPerfil voz) { this.clave = clave; this.texto = texto; this.voz = voz; }
    }

    // ---- frases sueltas del jefe y del comerciante (con voz propia)
    public const string BufaDespierta = "«Nadie me ha cargado hasta el final…»";
    public const string BufaInvocaPiedra = "«¡Mis pretendientes… despierten!»";
    public const string BufaInvocaSerpientes = "«¡Hijas de la cueva… a mí!»";
    public const string BufaFase2 = "«¡No… mires… atrás!»";
    public const string BufaDerrotada = "«Gracias… por no mirar atrás…»";

    public const string ChuySaludo = "«Almas por ayuda, joven. Aquí nada se pierde: todo se cambia.»";
    public const string ChuySinAlmas = "«Te faltan almas, joven. Vence a los guardianes y vuelve.»";
    public const string ChuyLleno = "«Tu alma ya está llena. Guarda tus almas para después.»";
    public const string ChuyAgotado = "«Ya tienes todo lo que te puedo dar de esto.»";
    public static readonly string[] ChuyGracias =
    {
        "«Trato hecho. Que La Bufa no te alcance.»",
        "«Buena elección. Úsalo con valor.»",
        "«Hasta los muertos agradecen un buen negocio.»",
        "«Con esto el camino pesa menos.»"
    };

    /// <summary>Todas las frases que llevan voz. El menú "9. Generar voces" las convierte en archivos de audio.</summary>
    public static IEnumerable<Frase> TodasLasFrases()
    {
        foreach (KeyValuePair<string, string[]> kv in lineas)
            for (int i = 0; i < kv.Value.Length; i++)
                yield return new Frase(ClaveLinea(kv.Key, i), kv.Value[i], VozPerfil.Tonalli);

        yield return new Frase("bufa_despierta", BufaDespierta, VozPerfil.LaBufa);
        yield return new Frase("bufa_invoca_piedra", BufaInvocaPiedra, VozPerfil.LaBufa);
        yield return new Frase("bufa_invoca_serpientes", BufaInvocaSerpientes, VozPerfil.LaBufa);
        yield return new Frase("bufa_fase2", BufaFase2, VozPerfil.LaBufa);
        yield return new Frase("bufa_derrotada", BufaDerrotada, VozPerfil.LaBufa);

        yield return new Frase("chuy_saludo", ChuySaludo, VozPerfil.DonChuy);
        yield return new Frase("chuy_sinalmas", ChuySinAlmas, VozPerfil.DonChuy);
        yield return new Frase("chuy_lleno", ChuyLleno, VozPerfil.DonChuy);
        yield return new Frase("chuy_agotado", ChuyAgotado, VozPerfil.DonChuy);
        for (int i = 0; i < ChuyGracias.Length; i++)
            yield return new Frase("chuy_gracias_" + i, ChuyGracias[i], VozPerfil.DonChuy);
    }

    // ================================================================== CÓDICE (libro de la historia)

    public const string TituloPrologo = "Prólogo · La leyenda de La Bufa";
    public const string TextoPrologo =
        "En el Cerro de La Bufa, junto a la Cueva Vieja, una joven fue convertida en piedra por un hechizo que nadie recuerda.\n\n" +
        "Cuenta la leyenda que solo un valiente puede romperlo: debe cargarla en la espalda desde la cueva hasta la basílica, " +
        "camino abajo, y sin mirar atrás ni una sola vez. A mitad del trayecto la joven se vuelve serpiente, y quien voltea huye " +
        "o cae desmayado. Por eso, hasta hoy, ella sigue esperando.\n\n" +
        "Cerca de ahí, Las Dos Comadres murmuran convertidas en roca, y las bocaminas guardan una plata que nadie pudo sacar.";

    /// <summary>Orden y título de los capítulos del libro. El texto sale de las líneas que cuenta Tonalli.</summary>
    public static readonly string[,] Capitulos =
    {
        { "intro", "El despertar en la sierra" },
        { "sierra", "La plata de La Bufa" },
        { "serpientes", "Las serpientes del hechizo" },
        { "totem1", "El primer tótem" },
        { "arroyo", "El arroyo seco" },
        { "tesoro", "Las bocaminas" },
        { "hombres_piedra", "Los hombres de piedra" },
        { "totem2", "El segundo tótem" },
        { "cruces", "Las cruces con veladoras" },
        { "comadres", "Las Dos Comadres" },
        { "totem3", "El tercer tótem" },
        { "comerciante", "Don Chuy, mercader de ánimas" },
        { "corona", "La corona de La Bufa" },
        { "cueva", "La Cueva Vieja" },
        { "jefe_despierta", "La Bufa despierta" },
        { "jefe_invoca", "Los pretendientes de piedra" },
        { "jefe_fase2", "¡No mires atrás!" },
        { "jefe_final", "El último esfuerzo" },
        { "jefe_derrotado", "El hechizo se rompe" },
    };

    public struct Bestia
    {
        public string id, nombre, historia, consejo, verso;
        public int numero;
        public Bestia(int numero, string id, string nombre, string verso, string historia, string consejo)
        { this.numero = numero; this.id = id; this.nombre = nombre; this.verso = verso; this.historia = historia; this.consejo = consejo; }
    }

    /// <summary>Fichas del bestiario, con forma de carta de Lotería (número, nombre y versito).</summary>
    public static readonly Bestia[] Bestiario =
    {
        new Bestia(1, "serpiente", "La Serpiente",
            "La serpiente de la cueva,\n¡si volteas, te la lleva!",
            "Nace del hechizo. La leyenda dice que la joven de piedra se vuelve serpiente cuando alguien la carga y mira atrás.",
            "Rápida pero frágil. Remátala con el combo y esquiva cuando se enrosque."),
        new Bestia(2, "hombre_piedra", "El Hombre de Piedra",
            "El que volteó a mirar,\nde piedra se quedó a cuidar.",
            "Uno de los jóvenes que intentó salvar a la joven y volteó. Quedó petrificado, vigilando el sendero para siempre.",
            "Lento pero pega muy fuerte. Esquiva cuando se agache y golpéalo cuando falle."),
        new Bestia(3, "comadre", "La Comadre",
            "La comadre murmuradora,\nde chisme vive a toda hora.",
            "Una de Las Dos Comadres, hechas roca por pelearse por un hombre. Lo único que hacen es chismear, y sus murmullos hieren.",
            "Ataca de lejos. Esquiva el murmullo y acércate para callarla."),
        new Bestia(4, "bufa", "La Joven de Piedra",
            "La joven que fue de piedra,\nno mires atrás, que se enreda.",
            "La Bufa. Piedra por fuera, joven por dentro. Cuando la despiertan, la piedra se rompe y se vuelve serpiente.",
            "Esquiva sus marcas rojas. Queda aturdida tras la embestida. En fase 2, ¡salta la onda de choque!"),
    };

    // ================================================================== ACTOS (estructura de la historia)

    /// <summary>Cada acto empieza en el capítulo indicado y se anuncia con una tarjeta de título.</summary>
    public static readonly string[,] Actos =
    {
        { "intro", "El despertar" },
        { "serpientes", "Los guardianes de la sierra" },
        { "hombres_piedra", "Las piedras que miraron atrás" },
        { "cueva", "La Cueva Vieja" },
        { "jefe_despierta", "La joven de piedra" },
        { "jefe_derrotado", "El hechizo se rompe" },
    };

    /// <summary>Número de acto (1..) al que pertenece un capítulo, o 0 si no pertenece a ninguno.</summary>
    public static int ActoDe(string capituloId)
    {
        int idx = IndiceCapitulo(capituloId);
        if (idx < 0) return 0;
        int acto = 0;
        for (int a = 0; a < Actos.GetLength(0); a++)
            if (IndiceCapitulo(Actos[a, 0]) <= idx) acto = a + 1;
        return acto;
    }

    /// <summary>¿Este capítulo abre un acto nuevo? (entonces se muestra la tarjeta "ACTO N").</summary>
    public static bool AbreActo(string id, out int numero, out string titulo)
    {
        for (int a = 0; a < Actos.GetLength(0); a++)
            if (Actos[a, 0] == id) { numero = a + 1; titulo = Actos[a, 1]; return true; }
        numero = 0; titulo = null; return false;
    }

    public static string TituloActo(int numero)
    {
        return numero >= 1 && numero <= Actos.GetLength(0) ? Actos[numero - 1, 1] : "";
    }

    public static string TituloCapituloDe(string id)
    {
        for (int i = 0; i < Capitulos.GetLength(0); i++) if (Capitulos[i, 0] == id) return Capitulos[i, 1];
        return null;
    }

    public static int IndiceCapitulo(string id)
    {
        for (int i = 0; i < Capitulos.GetLength(0); i++) if (Capitulos[i, 0] == id) return i;
        return -1;
    }
}
