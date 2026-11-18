using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ZombieEnemy : MonoBehaviour
{
    [Header("Configuración del Zombi")]
    public float speed = 2.2f;
    public Transform player;

    [Header("Detección y Evasión de Muros")]
    [Tooltip("Distancia de los rayos para detectar obstáculos")]
    public float distanciaDeteccion = 1.2f;
    [Tooltip("Radio del CircleCast para considerar el grosor del cuerpo del zombie")]
    public float radioCuerpo = 0.35f;
    [Tooltip("Capas consideradas obstáculos (Paredes/Muros)")]
    public LayerMask capaObstaculos = ~0; // Por defecto detecta todo excepto lo filtrado
    [Tooltip("Suavidad de giro al esquivar")]
    public float velocidadGiro = 8f;

    [Header("Depuración Visual")]
    public bool dibujarGizmos = true;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Vector2 direccionDeseada;
    private Vector2 direccionActual;

    // Ángulos de los rayos sensores (en grados) para encontrar salida a izquierda y derecha
    private static readonly float[] angulosPrueba = new float[] { 25f, -25f, 50f, -50f, 75f, -75f, 90f, -90f, 115f, -115f };

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Asegurar que las físicas no giren la orientación del zombie
        rb.freezeRotation = true;
    }

    void Start()
    {
        if (player == null)
        {
            GameObject jugadorObj = GameObject.FindGameObjectWithTag("Player");
            if (jugadorObj != null)
                player = jugadorObj.transform;
        }

        // Si la máscara no se configuró, excluimos enemigos y triggers para no auto-bloquearse
        if (capaObstaculos == ~0)
        {
            int capaEnemigo = gameObject.layer;
            int capaIgnoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
            capaObstaculos = ~( (1 << capaEnemigo) | (1 << capaIgnoreRaycast) );
        }
    }

    void FixedUpdate()
    {
        if (player == null) return;

        Vector2 direccionAlJugador = ((Vector2)player.position - rb.position).normalized;

        // 1. Calcular la mejor dirección evitando muros
        direccionDeseada = CalcularDireccionEvasion(direccionAlJugador);

        // 2. Suavizar el cambio de dirección para un deslizamiento natural alrededor de esquinas
        direccionActual = Vector2.MoveTowards(direccionActual, direccionDeseada, velocidadGiro * Time.fixedDeltaTime);

        // 3. Aplicar movimiento físico
        rb.MovePosition(rb.position + direccionActual * speed * Time.fixedDeltaTime);

        // 4. Orientación del sprite (flipX)
        if (direccionActual.x < -0.05f && spriteRenderer != null)
        {
            spriteRenderer.flipX = true;
        }
        else if (direccionActual.x > 0.05f && spriteRenderer != null)
        {
            spriteRenderer.flipX = false;
        }
    }

    Vector2 CalcularDireccionEvasion(Vector2 direccionAlJugador)
    {
        // Prueba inicial: ¿Camino libre directo al jugador?
        RaycastHit2D impactoFrontal = Physics2D.CircleCast(rb.position, radioCuerpo, direccionAlJugador, distanciaDeteccion, capaObstaculos);

        // Si no golpeó ningún muro (o el impacto fue el propio jugador), avanzar directo
        if (impactoFrontal.collider == null || impactoFrontal.collider.CompareTag("Player"))
        {
            return direccionAlJugador;
        }

        // Si hay una pared enfrente, evaluar rayos en abanico alternando izquierda y derecha
        for (int i = 0; i < angulosPrueba.Length; i++)
        {
            Vector2 direccionPrueba = RotarVector2(direccionAlJugador, angulosPrueba[i]);
            RaycastHit2D impactoPrueba = Physics2D.CircleCast(rb.position, radioCuerpo, direccionPrueba, distanciaDeteccion, capaObstaculos);

            if (impactoPrueba.collider == null || impactoPrueba.collider.CompareTag("Player"))
            {
                // Encontró una abertura libre para rodear la esquina
                return direccionPrueba;
            }
        }

        // Si está rodeado, intentar deslizarse a lo largo de la normal de la superficie
        Vector2 normalSuperficie = impactoFrontal.normal;
        Vector2 direccionDeslizamiento = new Vector2(-normalSuperficie.y, normalSuperficie.x);
        if (Vector2.Dot(direccionDeslizamiento, direccionAlJugador) < 0f)
        {
            direccionDeslizamiento = -direccionDeslizamiento;
        }

        return direccionDeslizamiento;
    }

    Vector2 RotarVector2(Vector2 vector, float grados)
    {
        float rad = grados * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos);
    }

    void OnDrawGizmosSelected()
    {
        if (!dibujarGizmos) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radioCuerpo);

        if (player != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + direccionDeseada * distanciaDeteccion);
        }
    }
}
