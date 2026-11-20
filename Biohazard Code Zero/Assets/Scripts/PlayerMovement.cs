using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
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

    [Header("Arsenal (1 = Pistola, 2 = Subfusil, 3 = Escopeta)")]
    [Tooltip("Configuración de cada arma. Cambia con 1, 2, 3 o con la rueda del mouse.")]
    public ArmaConfig[] armas = new ArmaConfig[]
    {
        ArmaConfig.CrearPistola(),
        ArmaConfig.CrearSubfusil(),
        ArmaConfig.CrearEscopeta()
    };
    [SerializeField] private int armaInicial = 0;
    [Tooltip("Si el cargador está vacío y aprietas disparar, recarga automáticamente")]
    public bool recargaAutomatica = true;

    [Header("Munición (cargador y reserva INDEPENDIENTES por arma: ver la lista Armas)")]
    [Tooltip("Balas en el cargador del arma equipada (solo lectura en juego)")]
    public int municionActual;
    [Tooltip("Reserva del arma equipada (solo lectura en juego). La capacidad de cada arma se configura en Armas > Reserva Maxima.")]
    public int municionTotal = 45;

    [Header("Animación de recoger / equipar arma")]
    [Tooltip("Nombre (o Tag) del estado del Animator de Bryan que reproduce la animación de recoger el arma")]
    public string estadoAnimRecoger = "Recoger";
    [Tooltip("Margen inicial en segundos mientras el trigger 'Recoger' entra al estado")]
    public float margenInicioRecoger = 0.12f;

    // Campos heredados de la versión anterior (se conservan para no perder datos serializados)
    [HideInInspector] public float tiempoEntreDisparos = 0.2f;
    [HideInInspector] public int tamañoCargador = 15;

    [Header("Interfaz (UI)")]
    public TextMeshProUGUI textoMunicion;
    public GameObject contenedorArmaUI;

    [Header("Game Feel (Camera Shake)")]
    public bool habilitarCameraShake = true;
    public float shakeDisparoDuracion = 0.07f;
    public float shakeDisparoIntensidad = 0.12f;

    [Header("Esquive / Dash Táctico")]
    public KeyCode teclaDash = KeyCode.Space;
    [Tooltip("También esquivar con clic derecho")]
    public bool dashConClicDerecho = true;
    public float velocidadDash = 19f;
    public float duracionDash = 0.17f;
    [Tooltip("Frames de invencibilidad extra al terminar el dash (segundos)")]
    public float iFramesExtra = 0.08f;
    [Tooltip("Tiempo de recarga del esquive (1.5 - 2 s recomendado)")]
    public float cooldownDash = 1.6f;
    [Tooltip("Crea una estela de 'fantasmas' durante el dash")]
    public bool estelaDash = true;

    [Header("Recarga")]
    [Tooltip("Trigger del Animator de Bryan al empezar a recargar (opcional)")]
    public string paramTriggerRecarga = "Recargar";
    [Tooltip("Bool del Animator de Bryan mientras recarga (opcional)")]
    public string paramBoolRecargando = "Recargando";

    // ===== Eventos para el HUD (sin acoplamiento) =====
    public event Action OnArmaOMunicionCambiada;
    public event Action OnDashListo;
    public event Action OnDisparo;
    /// <summary>Aviso breve para el HUD (munición al máximo, munición recogida...).</summary>
    public event Action<string, bool> OnAvisoMunicion;

    // ===== Propiedades públicas =====
    public ArmaConfig ArmaActual => (armas != null && armas.Length > 0) ? armas[indiceArma] : null;
    public int IndiceArma => indiceArma;
    public bool TienePistola => tienePistola;
    public bool EstaEsquivando => Time.time < dashTerminaEn;
    public bool DashListo => Time.time >= dashDisponibleEn;
    public bool EstaRecargando => recargando;
    /// <summary>True mientras se reproduce la animación de recoger/equipar el arma (rotación bloqueada).</summary>
    public bool EquipandoArma => equipandoArma;
    /// <summary>0 = empezó a recargar, 1 = terminó.</summary>
    public float ProgresoRecarga => recargando && duracionRecargaActual > 0f ? Mathf.Clamp01(1f - (finRecarga - Time.time) / duracionRecargaActual) : 0f;
    public bool ArmaDisponible(int indice) => disponible != null && indice >= 0 && indice < disponible.Length && disponible[indice];
    /// <summary>0 = recién usado, 1 = listo.</summary>
    public float ProgresoCooldownDash => cooldownDash <= 0f ? 1f : Mathf.Clamp01(1f - (dashDisponibleEn - Time.time) / cooldownDash);

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private PlayerHealth salud;
    private Camera mainCamera;
    private Vector2 movement;
    private Vector2 mousePos;
    private Vector2 ultimaDireccion = Vector2.right;
    private float anguloApuntado;
    private bool tienePistola;
    private int indiceArma;
    private float proximoDisparo = 0f;

    // Dash
    private Vector2 direccionDash;
    private float dashTerminaEn = -1f;
    private float dashDisponibleEn = -1f;
    private bool avisoDashListoPendiente = false;
    private float proximoFantasma = 0f;

    // Retroceso del arma (escopeta)
    private Vector2 velocidadRetroceso;

    // Animación de recoger arma
    private bool equipandoArma = false;
    private float inicioRecoger = -10f;

    // Recarga con tiempo
    private bool recargando = false;
    private float finRecarga = 0f;
    private float duracionRecargaActual = 0f;

    // Armas disponibles en este stage (progresión)
    private bool[] disponible;
    private readonly HashSet<int> parametrosAnimator = new HashSet<int>();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        salud = GetComponent<PlayerHealth>();
        mainCamera = Camera.main;

        if (armas == null || armas.Length == 0)
        {
            armas = new ArmaConfig[] { ArmaConfig.CrearPistola(), ArmaConfig.CrearEscopeta(), ArmaConfig.CrearSubfusil() };
        }

        if (anim != null && anim.runtimeAnimatorController != null)
            foreach (AnimatorControllerParameter p in anim.parameters) parametrosAnimator.Add(p.nameHash);

        // Configuraciones viejas (reserva compartida) -> cargadores y reservas realistas por arma
        foreach (ArmaConfig a in armas)
            if (a != null) a.MigrarMunicion();
    }

    int StageActual()
    {
        if (ConfigStage.Instance != null) return ConfigStage.Instance.NumeroStage;
        return ProgresoJuego.NumeroStage(SceneManager.GetActiveScene().name);
    }

    /// <summary>Recalcula qué armas puede usar Bryan según el stage y el progreso guardado.</summary>
    public void RecalcularArmasDisponibles()
    {
        int stage = StageActual();
        disponible = new bool[armas.Length];
        for (int i = 0; i < armas.Length; i++)
            disponible[i] = armas[i] != null && ProgresoJuego.ArmaUsableEnStage(armas[i].tipo, stage);
        if (armas.Length > 0 && !disponible[Mathf.Clamp(indiceArma, 0, armas.Length - 1)])
        {
            for (int i = 0; i < armas.Length; i++) if (disponible[i]) { indiceArma = i; break; }
        }
    }

    /// <summary>Lo llama PickupArma: desbloquea el arma para siempre y la equipa.</summary>
    public void DesbloquearArmaEnPartida(TipoArma tipo)
    {
        ProgresoJuego.DesbloquearArma(tipo);
        for (int i = 0; i < armas.Length; i++)
        {
            if (armas[i] != null && armas[i].tipo == tipo)
            {
                disponible[i] = true;
                armas[i].municionEnCargador = armas[i].tamañoCargador;
                armas[i].reservaActual = Mathf.Clamp(Mathf.Max(armas[i].reservaActual, armas[i].ReservaInicialReal), 0, armas[i].ReservaMaximaReal); // Regalo de munición
                EquiparArma(i);
                break;
            }
        }
        ActualizarUI();
    }

    void Start()
    {
        // Bloquear rotación física para que las colisiones de enemigos y paredes nunca giren a Bryan
        rb.freezeRotation = true;
        rb.angularVelocity = 0f;

        // Cada arma empieza con el cargador lleno y su reserva inicial (sin exceder la capacidad máxima)
        foreach (ArmaConfig a in armas)
        {
            if (a == null) continue;
            if (a.municionEnCargador < 0) a.municionEnCargador = a.tamañoCargador;
            if (a.reservaActual < 0) a.reservaActual = a.ReservaInicialReal;
            a.reservaActual = Mathf.Clamp(a.reservaActual, 0, a.ReservaMaximaReal);
        }

        indiceArma = Mathf.Clamp(armaInicial, 0, armas.Length - 1);
        foreach (ArmaConfig a in armas)
            if (a != null && a.tiempoRecarga <= 0.01f) a.tiempoRecarga = ArmaConfig.RecargaPorDefecto(a.tipo);
        RecalcularArmasDisponibles();
        SincronizarMunicionActual();
        ActualizarUI();

        if (contenedorArmaUI != null)
            contenedorArmaUI.SetActive(false);
    }

    bool JuegoActivo()
    {
        if (Time.timeScale <= 0f) return false;
        if (MercenariesManager.Instance != null && MercenariesManager.Instance.JuegoTerminado) return false;
        return true;
    }

    void Update()
    {
        if (!JuegoActivo())
        {
            movement = Vector2.zero;
            return;
        }

        // 1. Movimiento libre con WASD
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
        movement = movement.normalized;
        if (movement.sqrMagnitude > 0.01f) ultimaDireccion = movement;

        // 2. Animación de caminar (solo usa la velocidad total)
        if (anim != null)
            anim.SetFloat("Speed", EstaEsquivando ? 1f : movement.sqrMagnitude);

        // 3. Evaluar si tiene pistola
        bool estadoPistolaAnterior = tienePistola;
        tienePistola = anim != null && anim.GetBool("tienePistola");

        // Si se acaba de desarmar, restablecer orientación upright de inmediato
        if (estadoPistolaAnterior && !tienePistola)
        {
            ResetearOrientacionDesarmado();
        }
        if (!estadoPistolaAnterior && tienePistola)
        {
            inicioRecoger = Time.time; // Arranca el bloqueo de rotación de la animación de recoger
            NotificarCambio();
        }
        equipandoArma = CalcularEquipandoArma();

        // Mostrar u ocultar el contador de balas en pantalla según si trae arma
        if (contenedorArmaUI != null && contenedorArmaUI.activeSelf != tienePistola)
            contenedorArmaUI.SetActive(tienePistola);

        // 4. Esquive (disponible armado o desarmado)
        ProcesarDash();

        // 5. Lógica dependiente del estado del arma
        if (tienePistola)
        {
            // Solo rastrear cursor si está armado
            if (mainCamera != null)
                mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);

            // Asegurar que no quede invertido el sprite mientras apunta en 360°
            if (spriteRenderer != null)
                spriteRenderer.flipX = false;

            // Durante la animación de recoger/equipar no se apunta, dispara ni recarga
            if (equipandoArma) return;

            ProcesarCambioDeArma();
            ActualizarRecarga();
            ProcesarDisparo();

            // Recargar con R
            if (Input.GetKeyDown(KeyCode.R))
                IniciarRecarga();
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
        Vector2 velocidad;

        if (EstaEsquivando)
        {
            velocidad = direccionDash * velocidadDash;
        }
        else
        {
            velocidad = movement * moveSpeed;
        }

        // Retroceso del arma que se disipa rápido
        velocidad += velocidadRetroceso;
        velocidadRetroceso = Vector2.MoveTowards(velocidadRetroceso, Vector2.zero, 40f * Time.fixedDeltaTime);

        // Aplicar movimiento físico
        rb.MovePosition(rb.position + velocidad * Time.fixedDeltaTime);

        if (tienePistola && !equipandoArma)
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
            // Bloqueo estricto de rotación: desarmado o a mitad de la animación de recoger el arma
            rb.rotation = 0f;
            rb.angularVelocity = 0f;
            transform.rotation = Quaternion.identity;
        }
    }

    /// <summary>
    /// True mientras el Animator está en el estado de recoger (o entrando a él), más un margen
    /// inicial porque el trigger "Recoger" tarda un frame en llegar al estado.
    /// </summary>
    bool CalcularEquipandoArma()
    {
        if (anim == null || !tienePistola) return false;
        if (Time.time - inicioRecoger < margenInicioRecoger) return true;
        if (string.IsNullOrEmpty(estadoAnimRecoger)) return false;

        AnimatorStateInfo actual = anim.GetCurrentAnimatorStateInfo(0);
        if (actual.IsName(estadoAnimRecoger) || actual.IsTag(estadoAnimRecoger)) return true;
        if (anim.IsInTransition(0))
        {
            AnimatorStateInfo siguiente = anim.GetNextAnimatorStateInfo(0);
            if (siguiente.IsName(estadoAnimRecoger) || siguiente.IsTag(estadoAnimRecoger)) return true;
        }
        return false;
    }

    // =====================================================
    // ESQUIVE / DASH TÁCTICO
    // =====================================================
    void ProcesarDash()
    {
        // Aviso sonoro/visual cuando el esquive vuelve a estar listo
        if (avisoDashListoPendiente && DashListo)
        {
            avisoDashListoPendiente = false;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayDashListo();
            OnDashListo?.Invoke();
        }

        // Estela de fantasmas
        if (EstaEsquivando && estelaDash && spriteRenderer != null && Time.time >= proximoFantasma)
        {
            proximoFantasma = Time.time + 0.035f;
            EstelaDash.Crear(spriteRenderer, transform);
        }

        bool pidioDash = Input.GetKeyDown(teclaDash) || (dashConClicDerecho && Input.GetMouseButtonDown(1));
        if (!pidioDash) return;

        if (!DashListo)
        {
            // Anti-spam: clic seco indicando que aún no está listo
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySinBalas();
            return;
        }

        // Dirección: la de movimiento; si está quieto, hacia donde apunta (o la última dirección)
        Vector2 dir = movement;
        if (dir.sqrMagnitude < 0.01f)
        {
            if (tienePistola && mainCamera != null)
                dir = ((Vector2)mainCamera.ScreenToWorldPoint(Input.mousePosition) - rb.position);
            else
                dir = ultimaDireccion;
        }
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;

        direccionDash = dir.normalized;
        dashTerminaEn = Time.time + duracionDash;
        dashDisponibleEn = Time.time + duracionDash + cooldownDash;
        avisoDashListoPendiente = true;

        if (salud != null) salud.HacerInvulnerable(duracionDash + iFramesExtra);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayDash();
        if (habilitarCameraShake && CameraFollow.Instance != null) CameraFollow.Instance.Shake(0.06f, 0.08f);
    }

    // =====================================================
    // ARMAS
    // =====================================================
    void ProcesarCambioDeArma()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) EquiparArma(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) EquiparArma(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) EquiparArma(2);

        float rueda = Input.mouseScrollDelta.y;
        if (rueda > 0.01f) EquiparArma(SiguienteDisponible(-1));
        else if (rueda < -0.01f) EquiparArma(SiguienteDisponible(1));
    }

    int SiguienteDisponible(int paso)
    {
        int n = armas.Length;
        for (int k = 1; k <= n; k++)
        {
            int i = ((indiceArma + paso * k) % n + n) % n;
            if (ArmaDisponible(i)) return i;
        }
        return indiceArma;
    }

    public void EquiparArma(int indice)
    {
        if (armas == null || indice < 0 || indice >= armas.Length || indice == indiceArma) return;
        if (!ArmaDisponible(indice))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySinBalas();
            return;
        }

        CancelarRecarga();
        indiceArma = indice;
        // Pequeña pausa al cambiar para que no se pueda "cancelar" la cadencia
        proximoDisparo = Mathf.Max(proximoDisparo, Time.time + 0.15f);
        SincronizarMunicionActual();
        ActualizarUI();

        if (AudioManager.Instance != null) AudioManager.Instance.PlayCambioArma();
    }

    void ProcesarDisparo()
    {
        ArmaConfig arma = ArmaActual;
        if (arma == null) return;

        if (recargando) return; // No se dispara mientras recarga
        bool quiereDisparar = arma.automatica ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);
        if (!quiereDisparar || Time.time < proximoDisparo) return;

        if (arma.municionEnCargador > 0)
        {
            Disparar(arma);
        }
        else
        {
            proximoDisparo = Time.time + 0.25f; // Evita spam del clic vacío en armas automáticas
            if (recargaAutomatica && arma.reservaActual > 0)
            {
                IniciarRecarga();
            }
            else
            {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySinBalas();
                Debug.Log("¡Click! Sin munición. Recoge cajas de munición.");
            }
        }
    }

    void Disparar(ArmaConfig arma)
    {
        proximoDisparo = Time.time + arma.cadencia;
        arma.municionEnCargador--;
        SincronizarMunicionActual();
        ActualizarUI();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayDisparoArma(arma.tipo);
        OnDisparo?.Invoke();

        if (habilitarCameraShake && CameraFollow.Instance != null)
            CameraFollow.Instance.Shake(arma.shakeDuracion, arma.shakeIntensidad);

        if (bulletPrefab == null || firePoint == null) return;

        int n = Mathf.Max(1, arma.proyectilesPorDisparo);
        for (int i = 0; i < n; i++)
        {
            float offset = 0f;
            if (n > 1)
                offset = Mathf.Lerp(-arma.dispersionGrados * 0.5f, arma.dispersionGrados * 0.5f, (float)i / (n - 1));
            offset += UnityEngine.Random.Range(-arma.imprecisionGrados, arma.imprecisionGrados);

            Quaternion rot = firePoint.rotation * Quaternion.Euler(0f, 0f, offset);
            GameObject balaGo = Instantiate(bulletPrefab, firePoint.position, rot);

            Bullet bala = balaGo.GetComponent<Bullet>();
            if (bala != null)
            {
                // La escopeta pierde daño con la distancia (fuerte a quemarropa)
                float dañoMinimo = arma.tipo == TipoArma.Escopeta ? 0.45f : 1f;
                bala.Configurar(arma.velocidadBala, arma.daño, arma.knockback, arma.vidaBala, dañoMinimo);
            }
        }

        // Retroceso marcado (escopeta) en dirección contraria al disparo
        if (arma.retrocesoJugador > 0f)
        {
            Vector2 haciaAtras = -(Vector2)firePoint.right;
            velocidadRetroceso += haciaAtras * arma.retrocesoJugador;
        }
    }

    // =====================================================
    // RECARGA CON TIEMPO
    // =====================================================
    public void IniciarRecarga()
    {
        ArmaConfig arma = ArmaActual;
        if (arma == null || recargando) return;
        if (arma.municionEnCargador >= arma.tamañoCargador || arma.reservaActual <= 0) return;

        recargando = true;
        duracionRecargaActual = arma.TiempoRecargaReal;
        finRecarga = Time.time + duracionRecargaActual;

        SetTriggerAnim(paramTriggerRecarga);
        SetBoolAnim(paramBoolRecargando, true);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayCambioArma();
        ActualizarUI();
    }

    void ActualizarRecarga()
    {
        if (!recargando || Time.time < finRecarga) return;
        recargando = false;
        SetBoolAnim(paramBoolRecargando, false);
        Recargar();
    }

    void CancelarRecarga()
    {
        if (!recargando) return;
        recargando = false;
        SetBoolAnim(paramBoolRecargando, false);
    }

    void SetTriggerAnim(string nombre)
    {
        if (anim != null && !string.IsNullOrEmpty(nombre) && parametrosAnimator.Contains(Animator.StringToHash(nombre))) anim.SetTrigger(nombre);
    }

    void SetBoolAnim(string nombre, bool valor)
    {
        if (anim != null && !string.IsNullOrEmpty(nombre) && parametrosAnimator.Contains(Animator.StringToHash(nombre))) anim.SetBool(nombre, valor);
    }

    /// <summary>Transfiere balas de la reserva al cargador (al terminar el tiempo de recarga).</summary>
    void Recargar()
    {
        ArmaConfig arma = ArmaActual;
        if (arma == null) return;
        if (arma.municionEnCargador >= arma.tamañoCargador || arma.reservaActual <= 0) { ActualizarUI(); return; }

        int balasNecesarias = arma.tamañoCargador - arma.municionEnCargador;
        int balasARecargar = Mathf.Min(balasNecesarias, arma.reservaActual);

        arma.municionEnCargador += balasARecargar;
        arma.reservaActual -= balasARecargar;
        SincronizarMunicionActual();
        ActualizarUI();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayRecarga();
    }

    /// <summary>
    /// Reparte una caja de munición: primero llena la reserva del arma equipada (sin pasar su máximo)
    /// y el excedente pasa a las demás armas desbloqueadas que no estén llenas.
    /// 'cantidad' son unidades de caja: cada arma las convierte con su Factor Caja Municion
    /// (Pistola 1 bala, Escopeta 0.4 cartuchos, Subfusil 2 balas por unidad).
    /// Devuelve las unidades que NO cupieron (0 = se usó toda la caja).
    /// </summary>
    public int AgregarMunicion(int cantidad)
    {
        if (cantidad <= 0 || armas == null || armas.Length == 0) return Mathf.Max(0, cantidad);

        float unidades = cantidad;
        System.Text.StringBuilder resumen = new System.Text.StringBuilder();

        // Orden: arma equipada primero, luego el resto en orden de la lista
        for (int k = 0; k < armas.Length && unidades > 0.001f; k++)
        {
            int i = (indiceArma + k) % armas.Length;
            ArmaConfig a = armas[i];
            if (a == null || !ArmaDisponible(i)) continue;
            if (a.reservaActual < 0) a.reservaActual = 0;

            int espacio = a.EspacioEnReserva;
            if (espacio <= 0) continue;

            float factor = a.FactorCajaReal;
            int balas = Mathf.Max(1, Mathf.FloorToInt(unidades * factor + 0.0001f));
            int agregadas = Mathf.Min(balas, espacio);
            a.reservaActual += agregadas;
            unidades = Mathf.Max(0f, unidades - agregadas / factor);

            if (resumen.Length > 0) resumen.Append("  ");
            resumen.Append("+").Append(agregadas).Append(" ").Append(a.nombre);
        }

        int sobrante = Mathf.CeilToInt(unidades - 0.001f);
        SincronizarMunicionActual();
        ActualizarUI();

        if (resumen.Length == 0)
            OnAvisoMunicion?.Invoke("MUNICIÓN AL MÁXIMO", false);
        else
            OnAvisoMunicion?.Invoke(resumen.ToString(), true);

        return Mathf.Clamp(sobrante, 0, cantidad);
    }

    /// <summary>True si todas las armas desbloqueadas tienen la reserva llena.</summary>
    public bool MunicionLlena()
    {
        if (armas == null) return true;
        for (int i = 0; i < armas.Length; i++)
            if (armas[i] != null && ArmaDisponible(i) && armas[i].EspacioEnReserva > 0) return false;
        return true;
    }

    void SincronizarMunicionActual()
    {
        ArmaConfig arma = ArmaActual;
        municionActual = arma != null ? Mathf.Max(0, arma.municionEnCargador) : 0;
        municionTotal = arma != null ? Mathf.Max(0, arma.reservaActual) : 0;
    }

    private void ResetearOrientacionDesarmado()
    {
        rb.rotation = 0f;
        rb.angularVelocity = 0f;
        transform.rotation = Quaternion.identity;
    }

    void NotificarCambio()
    {
        OnArmaOMunicionCambiada?.Invoke();
    }

    void ActualizarUI()
    {
        if (textoMunicion != null)
        {
            if (recargando)
            {
                textoMunicion.text = "RECARGANDO";
                textoMunicion.color = new Color(1f, 0.85f, 0.3f, 1f);
            }
            else
            {
                textoMunicion.text = municionActual.ToString() + " / " + municionTotal.ToString();
                ArmaConfig arma = ArmaActual;
                int umbral = arma != null ? Mathf.Max(1, arma.tamañoCargador / 5) : 3;
                textoMunicion.color = (municionActual <= umbral) ? Color.red : Color.white;
            }
        }
        NotificarCambio();
    }
}
