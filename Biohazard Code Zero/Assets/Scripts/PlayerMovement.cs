using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Configuración")]
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private Vector2 movement;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
        movement = movement.normalized;

        // Le decimos al Animator si el personaje se está moviendo (Speed > 0)
        anim.SetFloat("Speed", movement.sqrMagnitude);

        // Guardamos la dirección X y Y solo si se está moviendo 
        // (así cuando sueltes la tecla, se quedará viendo hacia donde caminaba)
        if (movement.x != 0 || movement.y != 0)
        {
            anim.SetFloat("X", movement.x);
            anim.SetFloat("Y", movement.y);
        }

        // Truco del espejo (Voltear sprite)
        if (movement.x < 0) 
        {
            spriteRenderer.flipX = true; // Mira a la izquierda
        }
        else if (movement.x > 0) 
        {
            spriteRenderer.flipX = false; // Mira a la derecha
        }
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }
}