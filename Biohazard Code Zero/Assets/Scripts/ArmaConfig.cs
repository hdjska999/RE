using UnityEngine;

/// <summary>
/// Datos de configuración de cada arma de Bryan (Pistola, Subfusil, Escopeta).
/// Se edita desde el Inspector de PlayerMovement (lista "armas").
/// </summary>
public enum TipoArma
{
    Pistola = 0,
    Subfusil = 1,
    Escopeta = 2
}

[System.Serializable]
public class ArmaConfig
{
    [Header("Identidad")]
    public string nombre = "PISTOLA";
    public TipoArma tipo = TipoArma.Pistola;
    [Tooltip("Ícono que se muestra en el HUD (opcional; si es nulo se conserva el actual)")]
    public Sprite icono;

    [Header("Disparo")]
    [Tooltip("Segundos entre disparos")]
    public float cadencia = 0.35f;
    [Tooltip("Si está activo, mantener clic izquierdo dispara en ráfaga automática")]
    public bool automatica = false;
    [Tooltip("Daño por bala / perdigón")]
    public float daño = 1f;
    [Tooltip("Proyectiles por disparo (escopeta = 5)")]
    [Min(1)] public int proyectilesPorDisparo = 1;
    [Tooltip("Apertura total del abanico en grados (solo si hay más de 1 proyectil)")]
    public float dispersionGrados = 0f;
    [Tooltip("Imprecisión aleatoria extra por bala en grados")]
    public float imprecisionGrados = 0f;
    [Tooltip("Velocidad de la bala (unidades/seg)")]
    public float velocidadBala = 14f;
    [Tooltip("Tiempo de vida de la bala (limita el alcance)")]
    public float vidaBala = 1.6f;
    [Tooltip("Fuerza de retroceso aplicada al zombie impactado")]
    public float knockback = 3.5f;

    [Header("Cargador y reserva (independientes por arma)")]
    public int tamañoCargador = 15;
    [Tooltip("Balas máximas que puede guardar en reserva esta arma (Pistola 60, Escopeta 32, Subfusil 120)")]
    public int reservaMaxima = 0;
    [Tooltip("Reserva con la que empieza la partida (o al obtener el arma)")]
    public int reservaInicial = -1;
    [Tooltip("Cuántas balas de ESTA arma vale una caja de munición: balas = cantidad de la caja x este factor (Pistola 1, Escopeta 0.4, Subfusil 2)")]
    public float factorCajaMunicion = 0f;
    [Tooltip("Segundos que tarda en recargar (0 = valor por defecto según el tipo de arma)")]
    public float tiempoRecarga = 0f;
    [HideInInspector] public int municionEnCargador = -1;
    [HideInInspector] public int reservaActual = -1;
    [Tooltip("Versión interna de la configuración de munición (no tocar)")]
    [HideInInspector] public int versionMunicion = 0;

    [Header("Game Feel")]
    [Tooltip("Empuje hacia atrás que sufre Bryan al disparar")]
    public float retrocesoJugador = 0f;
    public float shakeDuracion = 0.07f;
    public float shakeIntensidad = 0.12f;

    // ===== Valores realistas por tipo de arma =====
    public static int CargadorPorDefecto(TipoArma t) => t == TipoArma.Escopeta ? 8 : (t == TipoArma.Subfusil ? 30 : 15);
    public static int ReservaMaximaPorDefecto(TipoArma t) => t == TipoArma.Escopeta ? 32 : (t == TipoArma.Subfusil ? 120 : 60);
    public static int ReservaInicialPorDefecto(TipoArma t) => t == TipoArma.Escopeta ? 8 : (t == TipoArma.Subfusil ? 60 : 30);
    public static float FactorCajaPorDefecto(TipoArma t) => t == TipoArma.Escopeta ? 0.4f : (t == TipoArma.Subfusil ? 2f : 1f);

    /// <summary>Aplica una sola vez los valores realistas a configuraciones viejas (reserva compartida). Devuelve true si cambió algo.</summary>
    public bool MigrarMunicion()
    {
        if (versionMunicion >= 1) return false;
        tamañoCargador = CargadorPorDefecto(tipo);
        reservaMaxima = ReservaMaximaPorDefecto(tipo);
        reservaInicial = ReservaInicialPorDefecto(tipo);
        factorCajaMunicion = FactorCajaPorDefecto(tipo);
        versionMunicion = 1;
        return true;
    }

    public int ReservaMaximaReal => reservaMaxima > 0 ? reservaMaxima : ReservaMaximaPorDefecto(tipo);
    public int ReservaInicialReal => reservaInicial >= 0 ? Mathf.Min(reservaInicial, ReservaMaximaReal) : ReservaInicialPorDefecto(tipo);
    public float FactorCajaReal => factorCajaMunicion > 0.001f ? factorCajaMunicion : FactorCajaPorDefecto(tipo);
    public int EspacioEnReserva => Mathf.Max(0, ReservaMaximaReal - Mathf.Max(0, reservaActual));

    /// <summary>Recarga por defecto: Pistola 1.2 s, Subfusil 1.5 s, Escopeta 1.8 s.</summary>
    public static float RecargaPorDefecto(TipoArma t)
    {
        switch (t)
        {
            case TipoArma.Subfusil: return 1.5f;
            case TipoArma.Escopeta: return 1.8f;
            default: return 1.2f;
        }
    }

    public float TiempoRecargaReal => tiempoRecarga > 0.01f ? tiempoRecarga : RecargaPorDefecto(tipo);

    public static ArmaConfig CrearPistola()
    {
        return new ArmaConfig
        {
            nombre = "PISTOLA", tipo = TipoArma.Pistola,
            cadencia = 0.32f, automatica = false, daño = 1f,
            proyectilesPorDisparo = 1, dispersionGrados = 0f, imprecisionGrados = 1f,
            velocidadBala = 14f, vidaBala = 1.6f, knockback = 3.5f,
            tamañoCargador = 15, reservaMaxima = 60, reservaInicial = 30, factorCajaMunicion = 1f, versionMunicion = 1, tiempoRecarga = 1.2f, retrocesoJugador = 0f,
            shakeDuracion = 0.07f, shakeIntensidad = 0.12f
        };
    }

    public static ArmaConfig CrearSubfusil()
    {
        return new ArmaConfig
        {
            nombre = "SUBFUSIL", tipo = TipoArma.Subfusil,
            cadencia = 0.09f, automatica = true, daño = 0.5f,
            proyectilesPorDisparo = 1, dispersionGrados = 0f, imprecisionGrados = 6f,
            velocidadBala = 17f, vidaBala = 1.3f, knockback = 1.6f,
            tamañoCargador = 30, reservaMaxima = 120, reservaInicial = 60, factorCajaMunicion = 2f, versionMunicion = 1, tiempoRecarga = 1.5f, retrocesoJugador = 0.4f,
            shakeDuracion = 0.04f, shakeIntensidad = 0.06f
        };
    }

    public static ArmaConfig CrearEscopeta()
    {
        return new ArmaConfig
        {
            nombre = "ESCOPETA", tipo = TipoArma.Escopeta,
            cadencia = 0.85f, automatica = false, daño = 0.9f,
            proyectilesPorDisparo = 5, dispersionGrados = 34f, imprecisionGrados = 3f,
            velocidadBala = 15f, vidaBala = 0.38f, knockback = 7f,
            tamañoCargador = 8, reservaMaxima = 32, reservaInicial = 8, factorCajaMunicion = 0.4f, versionMunicion = 1, tiempoRecarga = 1.8f, retrocesoJugador = 5.5f,
            shakeDuracion = 0.16f, shakeIntensidad = 0.32f
        };
    }
}
