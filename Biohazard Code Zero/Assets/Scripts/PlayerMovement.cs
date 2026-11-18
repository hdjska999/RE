using UnityEngine;
using TMPro;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Configuración Hotline")]
    public float moveSpeed = 7f;
    [Tooltip("Deja en 0 si tu sprite mira hacia la derecha por defecto")]
    public float offsetRotacion = 0f;

    [Header("Sistema de Disparo")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float tiempoEntreDisparos = 0.2f;
    private float proximoDisparo = 0f;

    [Header("Munición y Recarga")]
    public int tamañoCargador = 15;
    public int municionActual;
    public int municionTotal = 45;

    [Header("Interfaz (UI)")]
    public TextMeshProUGUI textoMunicion;
    public GameObject contenedorArmaUI;

    private Rigidbody2D rb;
    private Animator anim;
    private Vector2 movement;
    private Vector2 mousePos;
    private float anguloApuntado;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        municionActual = tamañoCargador;
        ActualizarUI();

        if (contenedorArmaUI != null)
            contenedorArmaUI.SetActive(false);
    }

    void Update()
    {
        // 1. Movimiento libre con WASD
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
        movement = movement.normalized;

        // 2. Obtener posición del mouse en el mundo
        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // 3. Animación de caminar (solo usa la velocidad total)
        anim.SetFloat("Speed", movement.sqrMagnitude);

        bool tienePistola = anim.GetBool("tienePistola");

        // Mostrar u ocultar el contador de balas en pantalla según si trae arma
        if (contenedorArmaUI != null && contenedorArmaUI.activeSelf != tienePistola)
            contenedorArmaUI.SetActive(tienePistola);

        if (tienePistola)
        {
            // Disparar con Clic Izquierdo
            if (Input.GetMouseButtonDown(0) && Time.time >= proximoDisparo)
            {
                if (municionActual > 0)
                    Disparar();
                else
                    Debug.Log("¡Click! Presiona 'R' para recargar.");
            }

            // Recargar con R
            if (Input.GetKeyDown(KeyCode.R))
                Recargar();
        }
    }

    void FixedUpdate()
    {
        // Aplicar movimiento físico
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);

        // Calcular ángulo hacia el cursor y rotar al jugador en 360°
        Vector2 direccion = mousePos - rb.position;
        anguloApuntado = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg + offsetRotacion;
        rb.MoveRotation(anguloApuntado);
    }

    void Disparar()
    {
        proximoDisparo = Time.time + tiempoEntreDisparos;
        municionActual--;
        ActualizarUI();

        // Instanciar la bala en la punta del cañón y con la rotación actual de Bryan
        Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
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

    public void AgregarMunicion(int cantidad)
    {
        municionTotal += cantidad;
        ActualizarUI();
    }

    void ActualizarUI()
    {
        if (textoMunicion != null)
            textoMunicion.text = municionActual.ToString() + " / " + municionTotal.ToString();
    }
}