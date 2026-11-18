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

    [Header("Game Feel (Camera Shake)")]
    public bool habilitarCameraShake = true;
    public float shakeDisparoDuracion = 0.07f;
    public float shakeDisparoIntensidad = 0.12f;

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private Camera mainCamera;
    private Vector2 movement;
    private Vector2 mousePos;
    private float anguloApuntado;
    private bool tienePistola;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;
    }

    void Start()
    {
        // Bloquear rotación física para que las colisiones de enemigos y paredes nunca giren a Bryan
        rb.freezeRotation = true;
        rb.angularVelocity = 0f;

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

        // 2. Animación de caminar (solo usa la velocidad total)
        if (anim != null)
            anim.SetFloat("Speed", movement.sqrMagnitude);

        // 3. Evaluar si tiene pistola
        bool estadoPistolaAnterior = tienePistola;
        tienePistola = anim != null && anim.GetBool("tienePistola");

        // Si se acaba de desarmar, restablecer orientación upright de inmediato
        if (estadoPistolaAnterior && !tienePistola)
        {
            ResetearOrientacionDesarmado();
        }

        // Mostrar u ocultar el contador de balas en pantalla según si trae arma
        if (contenedorArmaUI != null && contenedorArmaUI.activeSelf != tienePistola)
            contenedorArmaUI.SetActive(tienePistola);

        // 4. Lógica dependiente del estado del arma
        if (tienePistola)
        {
            // Solo rastrear cursor si está armado
            if (mainCamera != null)
                mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);

            // Asegurar que no quede invertido el sprite mientras apunta en 360°
            if (spriteRenderer != null)
                spriteRenderer.flipX = false;

            // Disparar con Clic Izquierdo
            if (Input.GetMouseButtonDown(0) && Time.time >= proximoDisparo)
            {
                if (municionActual > 0)
                {
                    Disparar();
                }
                else
                {
                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlaySinBalas();

                    Debug.Log("¡Click! Presiona 'R' para recargar.");
                }
            }

            // Recargar con R
            if (Input.GetKeyDown(KeyCode.R))
                Recargar();
        }
        else
        {
            // Desarmado: Invertir sprite horizontalmente según la dirección de movimiento
            if (spriteRenderer != null)
            {
                if (movement.x > 0.01f)
                    spriteRenderer.flipX = false;
                else if (movement.x < -0.01f)
                    spriteRenderer.flipX = true;
            }
        }
    }

    void FixedUpdate()
    {
        // Aplicar movimiento físico
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);

        if (tienePistola)
        {
            // Calcular ángulo hacia el cursor y rotar al jugador en 360°
            Vector2 direccion = mousePos - rb.position;
            if (direccion.sqrMagnitude > 0.001f)
            {
                anguloApuntado = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg + offsetRotacion;
                rb.MoveRotation(anguloApuntado);
            }
        }
        else
        {
            // Bloqueo estricto de rotación desarmado
            if (rb.rotation != 0f)
            {
                rb.rotation = 0f;
                rb.angularVelocity = 0f;
            }
        }
    }

    private void ResetearOrientacionDesarmado()
    {
        rb.rotation = 0f;
        rb.angularVelocity = 0f;
        transform.rotation = Quaternion.identity;
    }

    void Disparar()
    {
        proximoDisparo = Time.time + tiempoEntreDisparos;
        municionActual--;
        ActualizarUI();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayDisparo();

        if (habilitarCameraShake && CameraFollow.Instance != null)
            CameraFollow.Instance.Shake(shakeDisparoDuracion, shakeDisparoIntensidad);

        // Instanciar la bala en la punta del cañón y con la rotación actual de Bryan
        if (bulletPrefab != null && firePoint != null)
        {
            Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        }
    }

    void Recargar()
    {
        if (municionActual == tamañoCargador || municionTotal <= 0) return;

        int balasNecesarias = tamañoCargador - municionActual;
        int balasARecargar = Mathf.Min(balasNecesarias, municionTotal);

        municionActual += balasARecargar;
        municionTotal -= balasARecargar;
        ActualizarUI();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayRecarga();
    }

    public void AgregarMunicion(int cantidad)
    {
        municionTotal += cantidad;
        ActualizarUI();
    }

    void ActualizarUI()
    {
        if (textoMunicion != null)
        {
            textoMunicion.text = municionActual.ToString() + " / " + municionTotal.ToString();
            textoMunicion.color = (municionActual <= 3) ? Color.red : Color.white;
        }
    }
}
