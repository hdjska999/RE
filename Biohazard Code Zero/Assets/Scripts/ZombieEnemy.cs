using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ZombieEnemy : MonoBehaviour
{
    [Header("Configuración del Zombi")]
    public float speed = 2f; // Más lento que el jugador (que tiene 5)
    public Transform player; // El objetivo a perseguir

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Si se nos olvida arrastrar al Player, el zombi lo busca automáticamente
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player").transform;
        }
    }

    void FixedUpdate()
    {
        if (player != null)
        {
            // Calculamos la dirección hacia el jugador
            Vector2 direction = (player.position - transform.position).normalized;

            // Movemos al zombi hacia el jugador usando físicas para que choque con paredes
            rb.MovePosition(rb.position + direction * speed * Time.fixedDeltaTime);

            // Voltear el sprite (Truco del espejo) dependiendo de dónde esté el jugador
            if (direction.x < 0)
            {
                spriteRenderer.flipX = true; // Mira a la izquierda
            }
            else if (direction.x > 0)
            {
                spriteRenderer.flipX = false; // Mira a la derecha
            }
        }
    }
}