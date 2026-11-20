using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Salud del Enemigo")]
    [Tooltip("Pistola = 1 de daño por bala, Subfusil = 0.5, Escopeta = 0.9 por perdigón")]
    public float vidaMaxima = 2f;
    private float vidaActual;

    [Header("Tipo")]
    [Tooltip("Marca a los Tochos/Brutos: el primero que muere en Stage 2/3 suelta el arma de premio")]
    public bool esTocho = false;

    /// <summary>Se dispara justo antes de destruir al enemigo (lo usan el Zombie Tóxico, etc.).</summary>
    public event System.Action<EnemyHealth> OnMuerte;

    [Header("Puntaje")]
    [Tooltip("Multiplica los puntos base que otorga esta baja (Bruto = 2, Corredor = 1.2)")]
    public float multiplicadorPuntos = 1f;

    [Header("Efecto Visual de Daño")]
    public Color colorDaño = Color.red;
    public float duracionFlash = 0.1f;

    [Header("Knockback (retroceso al recibir disparo)")]
    [Tooltip("Multiplicador de retroceso. 0 = inmune (útil para enemigos pesados)")]
    public float resistenciaKnockback = 1f;
    [Tooltip("Duración en segundos del aturdimiento por retroceso")]
    public float duracionKnockback = 0.12f;

    [Header("Drops de Loot al Morir")]
    public GameObject prefabMunicion;
    public GameObject prefabCuracion;
    [Tooltip("Vida máxima que debe tener Bryan para que puedan salir hierbas verdes")]
    public int umbralPocaVida = 40;
    [Range(0f, 100f)]
    public float probabilidadHierbaPocaVida = 75f;
    [Range(0f, 100f)]
    [Tooltip("Probabilidad de soltar caja de munición (100 = siempre). Survival horror: Lycan 70, Corredor 55, Escupidor/Tóxico 60, Tocho 100")]
    public float probabilidadMunicion = 70f;

    public float VidaActual => vidaActual;
    public bool EstaMuerto => estaMuerto;

    private SpriteRenderer spriteRenderer;
    private ZombieEnemy zombie;
    private Color colorOriginal;
    private bool estaMuerto = false;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        zombie = GetComponent<ZombieEnemy>();
    }

    void Start()
    {
        vidaActual = vidaMaxima;

        if (spriteRenderer != null)
        {
            colorOriginal = spriteRenderer.color;
        }
    }

    /// <summary>Compatibilidad con código anterior (daño entero, sin dirección).</summary>
    public void RecibirDaño(int cantidadDaño)
    {
        RecibirDaño((float)cantidadDaño, Vector2.zero, 0f);
    }

    /// <summary>Daño con dirección del disparo para aplicar knockback.</summary>
    public void RecibirDaño(float cantidadDaño, Vector2 direccionImpacto, float fuerzaKnockback)
    {
        if (estaMuerto) return;

        vidaActual -= cantidadDaño;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayImpactoBala();
            if (vidaActual > 0f) AudioManager.Instance.PlayDañoZombie();
        }

        // Retroceso físico breve en la dirección del disparo (opuesta al tirador)
        if (zombie != null && fuerzaKnockback > 0f && resistenciaKnockback > 0f && direccionImpacto.sqrMagnitude > 0.0001f)
        {
            zombie.AplicarKnockback(direccionImpacto.normalized, fuerzaKnockback * resistenciaKnockback, duracionKnockback);
        }

        if (spriteRenderer != null)
        {
            StopCoroutine(nameof(EfectoFlash));
            StartCoroutine(nameof(EfectoFlash));
        }

        if (vidaActual <= 0f)
        {
            Morir();
        }
    }

    IEnumerator EfectoFlash()
    {
        spriteRenderer.color = colorDaño;
        yield return new WaitForSeconds(duracionFlash);
        if (!estaMuerto) spriteRenderer.color = colorOriginal;
    }

    void Morir()
    {
        if (estaMuerto) return;
        estaMuerto = true;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMuerteZombie();
        }

        if (MercenariesManager.Instance != null)
        {
            MercenariesManager.Instance.RegistrarBaja(multiplicadorPuntos);
        }

        OnMuerte?.Invoke(this);
        if (esTocho && ConfigStage.Instance != null) ConfigStage.Instance.NotificarMuerteTocho(transform.position);

        SoltarLoot();
        Destroy(gameObject);
    }

    void SoltarLoot()
    {
        // 1. Caja de munición (munición unificada para las 3 armas)
        if (prefabMunicion != null && Random.Range(0f, 100f) <= probabilidadMunicion)
        {
            Instantiate(prefabMunicion, transform.position, Quaternion.identity);
        }

        // 2. Si Bryan tiene poca vida, suelta hierba verde
        if (prefabCuracion != null)
        {
            PlayerHealth saludJugador = FindAnyObjectByType<PlayerHealth>();
            if (saludJugador != null && saludJugador.vidaActual <= umbralPocaVida)
            {
                if (Random.Range(0f, 100f) <= probabilidadHierbaPocaVida)
                {
                    // Se desplaza ligeramente a un lado para que no se encime con la caja de balas
                    Vector2 offset = new Vector2(0.45f, 0.2f);
                    Instantiate(prefabCuracion, (Vector2)transform.position + offset, Quaternion.identity);
                }
            }
        }
    }
}
