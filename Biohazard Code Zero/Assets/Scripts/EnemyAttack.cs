using UnityEngine;

/// <summary>
/// Ataque telegrafiado del infectado:
/// Persigue -> AVISO (se detiene, brilla y crece) -> GOLPE (estocada corta) -> RECUPERACIÓN -> cooldown.
/// El aviso da tiempo a Bryan para leer el golpe y esquivarlo (Espacio / Clic derecho).
/// </summary>
public class EnemyAttack : MonoBehaviour
{
    [Header("Daño del Zombie")]
    public int dañoAtaque = 20; // 5 toques para matarte (100 de vida)
    public float tiempoEntreAtaques = 1.5f;

    [Header("Ataque Telegrafiado")]
    [Tooltip("Separación entre colliders para empezar el aviso de ataque")]
    public float distanciaInicioAtaque = 0.35f;
    [Tooltip("Separación máxima a la que el golpe conecta al terminar el aviso")]
    public float alcanceGolpe = 0.75f;
    [Tooltip("Duración del aviso antes del golpe (ventana para esquivar)")]
    public float tiempoAviso = 0.45f;
    [Tooltip("Tiempo quieto después del golpe")]
    public float tiempoRecuperacion = 0.35f;
    [Tooltip("Distancia de la estocada al golpear")]
    public float distanciaEstocada = 0.35f;

    [Header("Aviso Visual / Sonoro")]
    public Color colorAviso = new Color(1f, 0.92f, 0.35f, 1f);
    [Tooltip("Cuánto crece el sprite durante el aviso (0.15 = 15%)")]
    public float escalaAviso = 0.15f;
    public bool sonidoAviso = true;

    private enum Estado { Persiguiendo, Aviso, Recuperacion }
    private Estado estado = Estado.Persiguiendo;

    private float proximoAtaque = 0f;
    private float finEstado = 0f;
    private bool contactoConJugador = false;

    private Transform jugador;
    private PlayerHealth saludJugador;
    private Collider2D miCollider;
    private Collider2D colliderJugador;
    private ZombieEnemy zombie;
    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private Color colorBase;

    void Awake()
    {
        miCollider = GetComponent<Collider2D>();
        zombie = GetComponent<ZombieEnemy>();
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            jugador = p.transform;
            saludJugador = p.GetComponent<PlayerHealth>();
            colliderJugador = p.GetComponent<Collider2D>();
        }
        if (sr != null) colorBase = sr.color;
    }

    void Update()
    {
        if (jugador == null || saludJugador == null || saludJugador.EstaMuerto) { CancelarAviso(); return; }

        switch (estado)
        {
            case Estado.Persiguiendo:
                if (Time.time >= proximoAtaque && (contactoConJugador || SeparacionConJugador() <= distanciaInicioAtaque))
                {
                    IniciarAviso();
                }
                break;

            case Estado.Aviso:
                // Un disparo que lo empuja (knockback) interrumpe el ataque
                if (zombie != null && zombie.EnKnockback)
                {
                    CancelarAviso();
                    proximoAtaque = Time.time + tiempoRecuperacion;
                    break;
                }

                // Feedback: parpadeo y crecimiento progresivo
                float progreso = 1f - Mathf.Clamp01((finEstado - Time.time) / Mathf.Max(0.01f, tiempoAviso));
                if (sr != null)
                {
                    float pulso = (Mathf.Sin(Time.time * 40f) + 1f) * 0.5f;
                    sr.color = Color.Lerp(colorBase, colorAviso, Mathf.Lerp(0.35f, 1f, pulso));
                }
                if (zombie != null) zombie.EscalaExtra = escalaAviso * progreso;

                if (Time.time >= finEstado) EjecutarGolpe();
                break;

            case Estado.Recuperacion:
                if (Time.time >= finEstado)
                {
                    estado = Estado.Persiguiendo;
                    if (zombie != null) zombie.MovimientoBloqueado = false;
                }
                break;
        }
    }

    void IniciarAviso()
    {
        estado = Estado.Aviso;
        finEstado = Time.time + tiempoAviso;
        if (zombie != null)
        {
            zombie.MovimientoBloqueado = true;
            zombie.DispararAnimacionAtaque();
        }
        if (sonidoAviso && AudioManager.Instance != null) AudioManager.Instance.PlayZombieAviso();
    }

    void EjecutarGolpe()
    {
        // Estocada corta hacia Bryan
        if (rb != null && jugador != null)
        {
            Vector2 dir = ((Vector2)jugador.position - rb.position).normalized;
            rb.MovePosition(rb.position + dir * distanciaEstocada);
        }

        // ¿Sigue en rango? Si Bryan esquivó (i-frames) o se alejó, el golpe falla
        if (SeparacionConJugador() <= alcanceGolpe)
        {
            saludJugador.RecibirDaño(dañoAtaque); // PlayerHealth ignora el daño durante i-frames
        }

        RestaurarVisual();
        estado = Estado.Recuperacion;
        finEstado = Time.time + tiempoRecuperacion;
        proximoAtaque = Time.time + tiempoEntreAtaques;
    }

    void CancelarAviso()
    {
        if (estado == Estado.Aviso)
        {
            RestaurarVisual();
            estado = Estado.Persiguiendo;
        }
        if (zombie != null && estado != Estado.Recuperacion) zombie.MovimientoBloqueado = false;
    }

    void RestaurarVisual()
    {
        if (sr != null) sr.color = colorBase;
        if (zombie != null) zombie.EscalaExtra = 0f;
    }

    float SeparacionConJugador()
    {
        if (miCollider != null && colliderJugador != null && miCollider.enabled && colliderJugador.enabled)
        {
            ColliderDistance2D d = Physics2D.Distance(miCollider, colliderJugador);
            if (d.isValid) return Mathf.Max(0f, d.distance);
        }
        return jugador != null ? Vector2.Distance(transform.position, jugador.position) - 1.2f : float.MaxValue;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player")) contactoConJugador = true;
    }

    // Se ejecuta constantemente mientras el zombie esté tocando algo
    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player")) contactoConJugador = true;
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player")) contactoConJugador = false;
    }

    void OnDisable()
    {
        RestaurarVisual();
    }
}
