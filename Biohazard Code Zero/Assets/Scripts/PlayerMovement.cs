using UnityEngine;
using TMPro;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Configuración")]
    public float moveSpeed = 5f;

    [Header("Sistema de Disparo")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float tiempoEntreDisparos = 0.5f;
    private float proximoDisparo = 0f;

    [Header("Munición y Recarga")]
    public int tamañoCargador = 15;
    public int municionActual;
    public int municionTotal = 30;

    [Header("Interfaz (UI)")]
    public TextMeshProUGUI textoMunicion;
    public GameObject contenedorArmaUI; // NUEVO: El grupo que contiene el ícono y el texto

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private Vector2 movement;
    private Vector2 ultimaDireccionDisparo = Vector2.right;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        municionActual = tamañoCargador;
        ActualizarUI();

        // Ocultamos la UI del arma al iniciar el juego
        if (contenedorArmaUI != null)
        {
            contenedorArmaUI.SetActive(false);
        }
    }

    void Update()
    {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
        movement = movement.normalized;

        bool tienePistola = anim.GetBool("tienePistola");

        // NUEVO: Mostrar la UI solo si tiene el arma equipada
        if (tienePistola && contenedorArmaUI != null && !contenedorArmaUI.activeSelf)
        {
            contenedorArmaUI.SetActive(true);
        }
        else if (!tienePistola && contenedorArmaUI != null && contenedorArmaUI.activeSelf)
        {
            contenedorArmaUI.SetActive(false);
        }

        if (tienePistola)
        {
            // === MODO COMBATE ===
            if (movement.x != 0 || movement.y != 0)
            {
                anim.SetFloat("apuntandoX", movement.x);
                anim.SetFloat("apuntandoY", movement.y);
                ultimaDireccionDisparo = movement;
            }

            if (Input.GetKeyDown(KeyCode.Space) && Time.time >= proximoDisparo)
            {
                if (municionActual > 0)
                {
                    Disparar();
                }
                else
                {
                    Debug.Log("¡Click! Arma vacía. Presiona 'R' para recargar.");
                }
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                Recargar();
            }
        }
        else
        {
            // === MODO EXPLORACIÓN ===
            anim.SetFloat("Speed", movement.sqrMagnitude);

            if (movement.x != 0 || movement.y != 0)
            {
                anim.SetFloat("X", movement.x);
                anim.SetFloat("Y", movement.y);
            }
        }

        if (movement.x < 0) spriteRenderer.flipX = true;
        else if (movement.x > 0) spriteRenderer.flipX = false;
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }

    void Disparar()
    {
        proximoDisparo = Time.time + tiempoEntreDisparos;
        municionActual--;
        ActualizarUI();

        GameObject balaClon = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        float angulo = Mathf.Atan2(ultimaDireccionDisparo.y, ultimaDireccionDisparo.x) * Mathf.Rad2Deg;
        balaClon.transform.rotation = Quaternion.Euler(0, 0, angulo);
    }

    void Recargar()
    {
        if (municionActual == tamañoCargador || municionTotal <= 0) return;

        int balasNecesarias = tamañoCargador - municionActual;
        int balasARecargar = Mathf.Min(balasNecesarias, municionTotal);

        municionActual += balasARecargar;
        municionTotal -= balasARecargar;

        ActualizarUI();
    }

    void ActualizarUI()
    {
        if (textoMunicion != null)
        {
            textoMunicion.text = municionActual.ToString() + " / " + municionTotal.ToString();
        }
    }
}