using System;
using UnityEngine;

/// <summary>Los 4 estados de un nivel (Issue #10): Intro, Jugando, Pausa y Fin (con victoria o derrota).</summary>
public enum EstadoDeNivel { Intro, Jugando, Pausa, Fin }
public enum ResultadoNivel { Ninguno, Victoria, Derrota }

/// <summary>
/// Máquina de estados del nivel. Es la única fuente de verdad de "en qué momento del nivel estamos":
///  - Intro:   cinemática de despertar con el control bloqueado (DespertarConfusion).
///  - Jugando: exploración, combate, tienda y jefe (GameManager / Chofer).
///  - Pausa:   Time.timeScale = 0 y menú de pausa (GameManager).
///  - Fin:     victoria (jefe vencido + portal) o derrota (Poder Espiritual en 0).
/// Los demás sistemas se suscriben a OnCambio en vez de adivinar el estado.
/// </summary>
public static class EstadoNivel
{
    public static EstadoDeNivel Actual { get; private set; }
    public static ResultadoNivel Resultado { get; private set; }

    /// <summary>(estado anterior, estado nuevo)</summary>
    public static event Action<EstadoDeNivel, EstadoDeNivel> OnCambio;

    static EstadoDeNivel antesDePausa = EstadoDeNivel.Jugando;

    public static bool EnJuego { get { return Actual == EstadoDeNivel.Jugando; } }
    public static bool EnPausa { get { return Actual == EstadoDeNivel.Pausa; } }
    public static bool Terminado { get { return Actual == EstadoDeNivel.Fin; } }

    /// <summary>Se llama al empezar el nivel: vuelve a Jugando sin avisar a nadie.</summary>
    public static void Reiniciar()
    {
        Actual = EstadoDeNivel.Jugando;
        Resultado = ResultadoNivel.Ninguno;
        antesDePausa = EstadoDeNivel.Jugando;
    }

    public static void Cambiar(EstadoDeNivel nuevo)
    {
        if (Actual == nuevo) return;
        // Una vez terminado el nivel no se vuelve a ningún otro estado
        if (Actual == EstadoDeNivel.Fin) return;

        EstadoDeNivel previo = Actual;
        Actual = nuevo;
        Debug.Log("[EstadoNivel] " + previo + " → " + nuevo + (nuevo == EstadoDeNivel.Fin ? " (" + Resultado + ")" : ""));
        if (OnCambio != null) OnCambio(previo, nuevo);
    }

    public static void Pausar()
    {
        if (Actual == EstadoDeNivel.Pausa || Actual == EstadoDeNivel.Fin) return;
        antesDePausa = Actual;
        Cambiar(EstadoDeNivel.Pausa);
    }

    public static void Reanudar()
    {
        if (Actual != EstadoDeNivel.Pausa) return;
        Cambiar(antesDePausa);
    }

    public static void Terminar(ResultadoNivel resultado)
    {
        if (Actual == EstadoDeNivel.Fin) return;
        Resultado = resultado;
        Cambiar(EstadoDeNivel.Fin);
    }
}
