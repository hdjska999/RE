using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Configuración por nivel (va en el GameManager de cada Stage): dificultad base,
/// arma de premio que suelta el primer "Tocho" y registro de progreso.
/// </summary>
public class ConfigStage : MonoBehaviour
{
    public static ConfigStage Instance { get; private set; }

    [Header("Identidad del Stage")]
    [Tooltip("1, 2 o 3. Con 0 se detecta por el nombre de la escena")]
    public int numeroStage = 0;
    public string nombreVisible = "STAGE 1 - HOSPITAL";

    [Header("Dificultad base del Stage")]
    [Tooltip("Multiplica la cantidad de enemigos por oleada")]
    public float multiplicadorCantidad = 1f;
    [Tooltip("Multiplica la velocidad de los enemigos")]
    public float multiplicadorVelocidad = 1f;
    [Tooltip("Multiplica la vida de los enemigos")]
    public float multiplicadorVida = 1f;

    [Header("Arma de premio (la suelta el primer Tocho/Bruto)")]
    public bool tieneArmaPremio = false;
    public TipoArma armaPremio = TipoArma.Escopeta;
    public Sprite iconoArmaPremio;
    public Sprite spriteBrillo;

    public int NumeroStage => numeroStage;
    private bool premioEntregado = false;

    void Awake()
    {
        Instance = this;
        if (numeroStage <= 0) numeroStage = ProgresoJuego.NumeroStage(SceneManager.GetActiveScene().name);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        ProgresoJuego.RegistrarStageJugado(SceneManager.GetActiveScene().name);

        // Si ya tenías el arma de este nivel, no hace falta soltarla
        if (tieneArmaPremio && ProgresoJuego.ArmaDesbloqueada(armaPremio)) premioEntregado = true;
    }

    /// <summary>Lo llama EnemyHealth cuando muere un enemigo marcado como Tocho.</summary>
    public void NotificarMuerteTocho(Vector3 posicion)
    {
        if (!tieneArmaPremio || premioEntregado) return;
        premioEntregado = true; // Solo uno por nivel
        PickupArma.Crear(posicion, armaPremio, iconoArmaPremio, spriteBrillo);

        WaveSpawner spawner = FindAnyObjectByType<WaveSpawner>();
        if (spawner != null) spawner.MostrarAnuncio("¡EL TOCHO SOLTÓ UN ARMA!");
    }
}
