using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// IA de persecución del infectado con navegación anti-atasco:
/// - Wall-following con COMPROMISO DE LADO: al chocar con una estructura (U, L, mostrador curvo)
///   elige rodear por la izquierda o la derecha y mantiene ese lado hasta liberar la esquina.
/// - Detección de bloqueo por desplazamiento real: si en 0.35 s casi no avanzó, entra en rodeo
///   (o cambia de lado si ya estaba rodeando).
/// - Fuerza de separación entre zombies para que no se amontonen.
/// </summary>
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

    [Header("Anti-atasco (rodeo con compromiso de lado)")]
    [Tooltip("Ventana para medir si el zombie realmente avanza")]
    public float ventanaAtasco = 0.35f;
    [Tooltip("Fracción mínima de la distancia esperada que debe recorrer en la ventana (0.25 = 25%)")]
    [Range(0.05f, 0.9f)] public float avanceMinimo = 0.25f;
    [Tooltip("Tiempo máximo rodeando antes de reintentar por el otro lado")]
    public float tiempoMaximoRodeo = 4f;
    [Tooltip("Tiempo mínimo de rodeo antes de volver a encarar al jugador")]
    public float tiempoMinimoRodeo = 0.3f;
    [Tooltip("Cuánto se despega del muro mientras lo rodea")]
    public float empujeDesdeMuro = 0.25f;

    [Header("Separación entre zombies")]
    public bool usarSeparacion = true;
    public float radioSeparacion = 0.9f;
    public float fuerzaSeparacion = 0.65f;

    [Header("Comportamiento a distancia (Escupidor)")]
    [Tooltip("0 = persigue cuerpo a cuerpo. >0 = intenta mantenerse a esta distancia de Bryan")]
    public float distanciaPreferida = 0f;
    [Tooltip("Margen alrededor de la distancia preferida antes de acercarse o alejarse")]
    public float margenDistancia = 1.2f;

    [Header("Animator (parámetros opcionales)")]
    [Tooltip("Float con la velocidad de movimiento (0 = quieto)")]
    public string paramVelocidad = "Velocidad";
    [Tooltip("Bool true mientras camina")]
    public string paramCaminando = "Caminando";
    [Tooltip("Trigger que se dispara al iniciar el ataque telegrafiado")]
    public string paramAtacar = "Atacar";
    [Tooltip("Bool true mientras sufre knockback")]
    public string paramAturdido = "Aturdido";

    [Header("Animación procedural (si no hay clips)")]
    [Tooltip("Balanceo/estiramiento al caminar para dar vida al sprite estático")]
    public bool animacionProcedural = true;
    public float frecuenciaPaso = 9f;
    public float intensidadPaso = 0.045f;

    [Header("Depuración Visual")]
    public bool dibujarGizmos = true;

    // ===== Estado consultable por EnemyAttack =====
    public bool EnKnockback => Time.time < knockbackHasta;
    public bool MovimientoBloqueado { get; set; }
    /// <summary>Escala adicional temporal (la usa el aviso de ataque). 0 = normal.</summary>
    public float EscalaExtra { get; set; }
    public bool Rodeando => rodeando;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator anim;
    private Collider2D miCollider;
    private Vector2 direccionDeseada;
    private Vector2 direccionActual;
    private Vector3 escalaBase;
    private float faseCaminar;
    private float semillaRodeo;

    // Knockback
    private Vector2 velocidadKnockback;
    private float knockbackHasta = -1f;

    // Anti-atasco
    private bool rodeando = false;
    private int ladoRodeo = 1;           // +1 = rodea por la izquierda (antihorario), -1 = por la derecha
    private float inicioRodeo;
    private Vector2 ultimaNormal = Vector2.zero;
    private Vector2 posicionVentana;
    private float inicioVentana;
    private float distanciaEsperadaVentana;
    private int cambiosDeLado = 0;

    private static readonly Collider2D[] bufferVecinos = new Collider2D[16];
    private readonly HashSet<int> parametrosAnimator = new HashSet<int>();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        miCollider = GetComponent<Collider2D>();
        escalaBase = transform.localScale;
        faseCaminar = Random.Range(0f, 10f);
        semillaRodeo = Random.Range(0f, 100f);
        ladoRodeo = Random.value < 0.5f ? 1 : -1;

        // Asegurar que las físicas no giren la orientación del zombie
        rb.freezeRotation = true;

        if (anim != null && anim.runtimeAnimatorController != null)
        {
            foreach (AnimatorControllerParameter p in anim.parameters)
                parametrosAnimator.Add(p.nameHash);
        }
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
            capaObstaculos = ~((1 << capaEnemigo) | (1 << capaIgnoreRaycast));
        }

        ReiniciarVentana();
    }

    /// <summary>Empuja al zombie en 'direccion' (la del disparo). Lo llama EnemyHealth.</summary>
    public void AplicarKnockback(Vector2 direccion, float fuerza, float duracion)
    {
        velocidadKnockback = direccion.normalized * fuerza * 3f;
        knockbackHasta = Time.time + Mathf.Max(0.02f, duracion);
        direccionActual = Vector2.zero; // Pierde el impulso que traía hacia Bryan
    }

    /// <summary>Lo llama EnemyAttack al empezar el aviso de ataque.</summary>
    public void DispararAnimacionAtaque()
    {
        SetTrigger(paramAtacar);
    }

    void FixedUpdate()
    {
        float velocidadReal = 0f;

        // 0. Knockback: tiene prioridad sobre la IA
        if (EnKnockback)
        {
            rb.MovePosition(rb.position + velocidadKnockback * Time.fixedDeltaTime);
            velocidadKnockback = Vector2.Lerp(velocidadKnockback, Vector2.zero, 10f * Time.fixedDeltaTime);
            ActualizarAnimator(0f);
            ReiniciarVentana();
            return;
        }

        if (player == null)
        {
            ActualizarAnimator(0f);
            return;
        }

        Vector2 direccionAlJugador = ((Vector2)player.position - rb.position).normalized;

        if (!MovimientoBloqueado)
        {
            // 1. Objetivo: perseguir, o mantener distancia si es Escupidor
            Vector2 objetivo = direccionAlJugador;
            if (distanciaPreferida > 0f)
            {
                float dist = Vector2.Distance(player.position, rb.position);
                if (dist < distanciaPreferida - margenDistancia) objetivo = -direccionAlJugador;            // muy cerca: retrocede
                else if (dist <= distanciaPreferida + margenDistancia)                                      // en rango: rodea a Bryan
                    objetivo = new Vector2(-direccionAlJugador.y, direccionAlJugador.x) * (Mathf.Sin(Time.time * 0.35f + semillaRodeo) > 0f ? 1f : -1f) * 0.6f;
            }

            // 2. Navegación anti-atasco + separación
            Vector2 navegacion = objetivo.sqrMagnitude > 0.0001f ? Navegar(objetivo.normalized) * Mathf.Min(1f, objetivo.magnitude) : Vector2.zero;
            if (usarSeparacion) navegacion += Separacion() * (rodeando ? 0.4f : 1f);
            direccionDeseada = navegacion.sqrMagnitude > 1f ? navegacion.normalized : navegacion;

            // 3. Suavizar el giro (más rápido mientras rodea para pegarse al contorno)
            float giro = rodeando ? velocidadGiro * 1.6f : velocidadGiro;
            direccionActual = Vector2.MoveTowards(direccionActual, direccionDeseada, giro * Time.fixedDeltaTime);

            // 4. Aplicar movimiento físico
            Vector2 paso = direccionActual * speed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + paso);
            velocidadReal = direccionActual.magnitude * speed;
            distanciaEsperadaVentana += paso.magnitude;

            // 5. ¿Está atascado? (desplazamiento real vs. esperado)
            ComprobarAtasco(objetivo);
        }
        else
        {
            // Quieto durante el ataque: mira hacia Bryan
            direccionActual = Vector2.zero;
            direccionDeseada = direccionAlJugador;
            ReiniciarVentana();
        }

        // Orientación del sprite (flipX)
        Vector2 mirar = MovimientoBloqueado ? direccionAlJugador : direccionActual;
        if (spriteRenderer != null)
        {
            if (mirar.x < -0.05f) spriteRenderer.flipX = true;
            else if (mirar.x > 0.05f) spriteRenderer.flipX = false;
        }

        ActualizarAnimator(velocidadReal);
    }

    // =====================================================================
    // NAVEGACIÓN
    // =====================================================================

    Vector2 Navegar(Vector2 dirObjetivo)
    {
        RaycastHit2D frontal = Lanzar(dirObjetivo, distanciaDeteccion);

        if (!rodeando)
        {
            if (!EsObstaculo(frontal)) return dirObjetivo; // Camino libre

            // Obstáculo enfrente: elegir lado y comprometerse
            EntrarEnRodeo(dirObjetivo, frontal.normal);
        }
        else
        {
            // ¿Ya liberó la esquina? Vuelve a encarar al jugador si hay línea directa
            bool tiempoMinimo = Time.time - inicioRodeo >= tiempoMinimoRodeo;
            if (tiempoMinimo && LineaLibreHaciaJugador(dirObjetivo))
            {
                SalirDeRodeo();
                return dirObjetivo;
            }

            // Lleva demasiado rodeando: probar por el otro lado
            if (Time.time - inicioRodeo > tiempoMaximoRodeo)
            {
                CambiarLado();
            }
        }

        return SeguirMuro(dirObjetivo);
    }

    void EntrarEnRodeo(Vector2 dirObjetivo, Vector2 normal)
    {
        rodeando = true;
        inicioRodeo = Time.time;
        ultimaNormal = normal;
        cambiosDeLado = 0;

        // Elegir el lado con más espacio libre (y desempatar con la semilla del zombie)
        float libreIzq = EspacioLibre(Rotar(dirObjetivo, 70f)) + EspacioLibre(Rotar(dirObjetivo, 110f)) * 0.5f;
        float libreDer = EspacioLibre(Rotar(dirObjetivo, -70f)) + EspacioLibre(Rotar(dirObjetivo, -110f)) * 0.5f;
        if (Mathf.Abs(libreIzq - libreDer) < 0.15f) ladoRodeo = Mathf.Sin(semillaRodeo) > 0f ? 1 : -1;
        else ladoRodeo = libreIzq > libreDer ? 1 : -1;
    }

    void SalirDeRodeo()
    {
        rodeando = false;
        cambiosDeLado = 0;
        ReiniciarVentana();
    }

    void CambiarLado()
    {
        ladoRodeo = -ladoRodeo;
        inicioRodeo = Time.time;
        cambiosDeLado++;
        ReiniciarVentana();
    }

    /// <summary>Wall-following: avanza tangente al muro por el lado elegido, despegándose un poco.</summary>
    Vector2 SeguirMuro(Vector2 dirObjetivo)
    {
        // Actualizar la normal del muro: primero hacia el jugador, luego hacia el muro que rodeamos
        RaycastHit2D hacia = Lanzar(dirObjetivo, distanciaDeteccion * 1.5f);
        if (EsObstaculo(hacia)) ultimaNormal = hacia.normal;
        else if (ultimaNormal.sqrMagnitude > 0.01f)
        {
            RaycastHit2D lateral = Lanzar(-ultimaNormal, distanciaDeteccion * 1.2f);
            if (EsObstaculo(lateral)) ultimaNormal = lateral.normal;
        }
        if (ultimaNormal.sqrMagnitude < 0.01f) ultimaNormal = -dirObjetivo;

        // Tangente al muro según el lado comprometido (+90° izquierda, -90° derecha)
        Vector2 tangente = Rotar(ultimaNormal, 90f * ladoRodeo);
        Vector2 dir = (tangente + ultimaNormal * empujeDesdeMuro).normalized;

        // Esquina interior: si la tangente está bloqueada, girar alejándose del muro
        for (int i = 0; i < 7; i++)
        {
            Vector2 prueba = Rotar(dir, -ladoRodeo * 25f * i);
            RaycastHit2D h = Lanzar(prueba, distanciaDeteccion * 0.8f);
            if (!EsObstaculo(h)) return prueba;
            if (i == 0) ultimaNormal = h.normal; // nuevo muro: seguir su contorno
        }

        // Totalmente encerrado: deslizar a lo largo de la normal
        return ultimaNormal;
    }

    void ComprobarAtasco(Vector2 objetivo)
    {
        if (Time.time - inicioVentana < ventanaAtasco) return;

        float avanceReal = Vector2.Distance(rb.position, posicionVentana);
        bool intentaMoverse = distanciaEsperadaVentana > 0.05f;
        bool atascado = intentaMoverse && avanceReal < distanciaEsperadaVentana * avanceMinimo;

        if (atascado)
        {
            if (!rodeando)
            {
                RaycastHit2D h = Lanzar(objetivo.normalized, distanciaDeteccion * 1.5f);
                EntrarEnRodeo(objetivo.normalized, EsObstaculo(h) ? h.normal : -objetivo.normalized);
            }
            else if (cambiosDeLado < 3)
            {
                CambiarLado();
            }
            else
            {
                // Último recurso: impulso en dirección aleatoria para despegarse
                ultimaNormal = Random.insideUnitCircle.normalized;
                cambiosDeLado = 0;
                inicioRodeo = Time.time;
            }
        }

        ReiniciarVentana();
    }

    void ReiniciarVentana()
    {
        if (rb == null) return;
        posicionVentana = rb.position;
        inicioVentana = Time.time;
        distanciaEsperadaVentana = 0f;
    }

    /// <summary>Empuje suave lejos de los zombies cercanos.</summary>
    Vector2 Separacion()
    {
        ContactFilter2D filtro = ContactFilter2D.noFilter;
        filtro.useTriggers = false;
        int n = Physics2D.OverlapCircle(rb.position, radioSeparacion, filtro, bufferVecinos);
        Vector2 fuerza = Vector2.zero;
        for (int i = 0; i < n; i++)
        {
            Collider2D c = bufferVecinos[i];
            if (c == null || c == miCollider || c.isTrigger) continue;
            if (c.attachedRigidbody == rb) continue;
            if (c.GetComponent<ZombieEnemy>() == null) continue;

            Vector2 lejos = rb.position - (Vector2)c.transform.position;
            float d = lejos.magnitude;
            if (d < 0.001f) lejos = Random.insideUnitCircle;
            else lejos /= d;
            fuerza += lejos * (1f - Mathf.Clamp01(d / radioSeparacion));
        }
        return fuerza * fuerzaSeparacion;
    }

    bool LineaLibreHaciaJugador(Vector2 dir)
    {
        float dist = Mathf.Min(Vector2.Distance(rb.position, player.position), distanciaDeteccion * 3f);
        RaycastHit2D h = Physics2D.CircleCast(rb.position, radioCuerpo * 0.9f, dir, dist, capaObstaculos);
        return !EsObstaculo(h);
    }

    float EspacioLibre(Vector2 dir)
    {
        float alcance = distanciaDeteccion * 2.5f;
        RaycastHit2D h = Physics2D.CircleCast(rb.position, radioCuerpo, dir, alcance, capaObstaculos);
        return EsObstaculo(h) ? h.distance : alcance;
    }

    RaycastHit2D Lanzar(Vector2 dir, float distancia)
    {
        return Physics2D.CircleCast(rb.position, radioCuerpo, dir, distancia, capaObstaculos);
    }

    bool EsObstaculo(RaycastHit2D h)
    {
        if (h.collider == null || h.collider.isTrigger) return false;
        if (h.collider == miCollider || h.collider.attachedRigidbody == rb) return false;
        if (h.collider.CompareTag("Player")) return false;
        return true;
    }

    static Vector2 Rotar(Vector2 v, float grados)
    {
        float rad = grados * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    // =====================================================================
    // VISUAL / ANIMATOR
    // =====================================================================

    void LateUpdate()
    {
        // Balanceo procedural al caminar + escala extra del aviso de ataque
        float extra = 1f + EscalaExtra;
        if (animacionProcedural && !MovimientoBloqueado && !EnKnockback && direccionActual.sqrMagnitude > 0.04f)
        {
            faseCaminar += Time.deltaTime * frecuenciaPaso * Mathf.Max(0.6f, speed / 2.2f);
            float s = Mathf.Sin(faseCaminar) * intensidadPaso;
            transform.localScale = new Vector3(escalaBase.x * (1f + s) * extra, escalaBase.y * (1f - s) * extra, escalaBase.z);
        }
        else
        {
            transform.localScale = new Vector3(escalaBase.x * extra, escalaBase.y * extra, escalaBase.z);
        }
    }

    void ActualizarAnimator(float velocidad)
    {
        if (anim == null || parametrosAnimator.Count == 0) return;
        SetFloat(paramVelocidad, velocidad);
        SetBool(paramCaminando, velocidad > 0.05f);
        SetBool(paramAturdido, EnKnockback);
    }

    void SetFloat(string nombre, float v)
    {
        if (!string.IsNullOrEmpty(nombre) && parametrosAnimator.Contains(Animator.StringToHash(nombre))) anim.SetFloat(nombre, v);
    }

    void SetBool(string nombre, bool v)
    {
        if (!string.IsNullOrEmpty(nombre) && parametrosAnimator.Contains(Animator.StringToHash(nombre))) anim.SetBool(nombre, v);
    }

    void SetTrigger(string nombre)
    {
        if (anim != null && !string.IsNullOrEmpty(nombre) && parametrosAnimator.Contains(Animator.StringToHash(nombre))) anim.SetTrigger(nombre);
    }

    void OnDrawGizmosSelected()
    {
        if (!dibujarGizmos) return;

        Gizmos.color = rodeando ? Color.yellow : Color.green;
        Gizmos.DrawWireSphere(transform.position, radioCuerpo);

        if (player != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + direccionDeseada * distanciaDeteccion);
            if (rodeando)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, (Vector2)transform.position + ultimaNormal * 0.8f);
            }
        }
    }
}
