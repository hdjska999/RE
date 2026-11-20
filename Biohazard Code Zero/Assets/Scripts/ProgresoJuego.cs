using UnityEngine;

/// <summary>
/// Progresión persistente de Biohazard: Code Zero (PlayerPrefs).
/// Stages desbloqueados, último stage jugado, juego completado y armas obtenidas.
/// </summary>
public static class ProgresoJuego
{
    public const string ESCENA_STAGE1 = "SampleScene";
    public const string ESCENA_STAGE2 = "Stage2_Laboratorio";
    public const string ESCENA_STAGE3 = "Stage3_RaccoonCity";
    public const string ESCENA_CREDITOS = "CreditsScene";
    public const int TOTAL_STAGES = 3;

    private const string K_ULTIMO = "BCZ_UltimoStage";
    private const string K_DESBLOQUEADO = "BCZ_StageDesbloqueado";
    private const string K_COMPLETADO = "BCZ_JuegoCompletado";
    private const string K_ARMA = "BCZ_Arma_";

    public static readonly string[] Escenas = { ESCENA_STAGE1, ESCENA_STAGE2, ESCENA_STAGE3 };

    /// <summary>1, 2 o 3 según la escena; 0 si no es un stage jugable.</summary>
    public static int NumeroStage(string escena)
    {
        for (int i = 0; i < Escenas.Length; i++)
            if (Escenas[i] == escena) return i + 1;
        return 0;
    }

    public static string EscenaDeStage(int numero)
    {
        numero = Mathf.Clamp(numero, 1, TOTAL_STAGES);
        return Escenas[numero - 1];
    }

    public static string EscenaSiguiente(string escenaActual)
    {
        int n = NumeroStage(escenaActual);
        if (n <= 0 || n >= TOTAL_STAGES) return ESCENA_CREDITOS;
        return EscenaDeStage(n + 1);
    }

    // ===== Stages =====
    public static bool HayProgreso => PlayerPrefs.HasKey(K_ULTIMO);
    public static string UltimoStageJugado => PlayerPrefs.GetString(K_ULTIMO, ESCENA_STAGE1);
    public static int StageDesbloqueado => Mathf.Clamp(PlayerPrefs.GetInt(K_DESBLOQUEADO, 1), 1, TOTAL_STAGES);
    public static bool JuegoCompletado => PlayerPrefs.GetInt(K_COMPLETADO, 0) == 1;

    public static void RegistrarStageJugado(string escena)
    {
        int n = NumeroStage(escena);
        if (n <= 0) return;
        PlayerPrefs.SetString(K_ULTIMO, escena);
        if (n > StageDesbloqueado) PlayerPrefs.SetInt(K_DESBLOQUEADO, n);
        PlayerPrefs.Save();
    }

    public static void CompletarStage(string escena)
    {
        int n = NumeroStage(escena);
        if (n <= 0) return;
        if (n < TOTAL_STAGES)
        {
            if (n + 1 > StageDesbloqueado) PlayerPrefs.SetInt(K_DESBLOQUEADO, n + 1);
        }
        else
        {
            PlayerPrefs.SetInt(K_COMPLETADO, 1);
        }
        PlayerPrefs.Save();
    }

    public static bool StageDisponible(int numero)
    {
        return JuegoCompletado || numero <= StageDesbloqueado;
    }

    /// <summary>Escena que carga "Continuar": el stage desbloqueado más avanzado (o el último jugado si ya terminó el juego).</summary>
    public static string EscenaParaContinuar()
    {
        if (JuegoCompletado) return UltimoStageJugado;
        return EscenaDeStage(StageDesbloqueado);
    }

    // ===== Armas =====
    public static bool ArmaDesbloqueada(TipoArma tipo)
    {
        if (tipo == TipoArma.Pistola) return true;
        return PlayerPrefs.GetInt(K_ARMA + tipo, 0) == 1;
    }

    public static void DesbloquearArma(TipoArma tipo)
    {
        if (tipo == TipoArma.Pistola) return;
        PlayerPrefs.SetInt(K_ARMA + tipo, 1);
        PlayerPrefs.Save();
    }

    /// <summary>Stage mínimo en el que se puede usar cada arma (Stage 1 = solo pistola).</summary>
    public static int StageMinimoDeArma(TipoArma tipo)
    {
        switch (tipo)
        {
            case TipoArma.Escopeta: return 2;
            case TipoArma.Subfusil: return 3;
            default: return 1;
        }
    }

    /// <summary>¿Puede usarse esta arma en el stage indicado? (0 = escena de pruebas: todo disponible)</summary>
    public static bool ArmaUsableEnStage(TipoArma tipo, int stage)
    {
        if (stage <= 0) return true;
        return stage >= StageMinimoDeArma(tipo) && ArmaDesbloqueada(tipo);
    }

    // ===== Récords =====
    public static int Record(string escena) => PlayerPrefs.GetInt("Record_" + escena, 0);
    public static string MejorRango(string escena) => PlayerPrefs.GetString("BCZ_Rango_" + escena, "-");

    public static void GuardarMejorRango(string escena, string rango)
    {
        string actual = MejorRango(escena);
        if (actual == "-" || ValorRango(rango) > ValorRango(actual))
        {
            PlayerPrefs.SetString("BCZ_Rango_" + escena, rango);
            PlayerPrefs.Save();
        }
    }

    static int ValorRango(string r)
    {
        switch (r) { case "S": return 4; case "A": return 3; case "B": return 2; case "C": return 1; default: return 0; }
    }

    /// <summary>Nueva partida: reinicia avance y armas, pero conserva récords y el selector si ya terminaste el juego.</summary>
    public static void ReiniciarProgresoPartida()
    {
        PlayerPrefs.DeleteKey(K_ULTIMO);
        PlayerPrefs.SetInt(K_DESBLOQUEADO, 1);
        PlayerPrefs.DeleteKey(K_ARMA + TipoArma.Escopeta);
        PlayerPrefs.DeleteKey(K_ARMA + TipoArma.Subfusil);
        // Claves del sistema de guardado anterior
        PlayerPrefs.DeleteKey("HayPartida");
        PlayerPrefs.DeleteKey("EscenaGuardada");
        PlayerPrefs.SetInt("CargarDesdeGuardado", 0);
        PlayerPrefs.Save();
    }
}
