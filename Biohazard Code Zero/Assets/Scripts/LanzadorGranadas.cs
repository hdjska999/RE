using System;
using UnityEngine;

/// <summary>
/// Granada táctica de Bryan: tecla G o clic central. Se lanza hacia el cursor,
/// detona ~1.2 s después (daño en área + knockback fuerte + camera shake) y tiene cooldown estricto de 30 s.
/// </summary>
public class LanzadorGranadas : MonoBehaviour
{
    [Header("Controles")]
    public KeyCode teclaGranada = KeyCode.G;
    public bool usarClicCentral = true;

    [Header("Granada")]
    public float cooldown = 30f;
    public float tiempoDetonacion = 1.2f;
    public float alcanceMaximo = 8f;
    public float radioExplosion = 3.2f;
    public float daño = 6f;
    public float knockback = 9f;
    public LayerMask capaObstaculos;

    [Header("Visual")]
    public Sprite spriteGranada;
    public Sprite spriteExplosion;

    public event Action OnGranadaLista;

    public bool Lista => Time.time >= disponibleEn;
    public float SegundosRestantes => Mathf.Max(0f, disponibleEn - Time.time);
    /// <summary>0 = recién lanzada, 1 = lista.</summary>
    public float ProgresoCooldown => cooldown <= 0f ? 1f : Mathf.Clamp01(1f - SegundosRestantes / cooldown);

    private float disponibleEn = -1f;
    private bool avisoPendiente = false;
    private Camera cam;
    private PlayerHealth salud;

    void Awake()
    {
        cam = Camera.main;
        salud = GetComponent<PlayerHealth>();
        if (capaObstaculos.value == 0)
        {
            int capa = LayerMask.NameToLayer("Obstaculos");
            if (capa >= 0) capaObstaculos = 1 << capa;
        }
    }

    void Update()
    {
        if (avisoPendiente && Lista)
        {
            avisoPendiente = false;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayDashListo();
            OnGranadaLista?.Invoke();
        }

        if (Time.timeScale <= 0f) return;
        if (MercenariesManager.Instance != null && MercenariesManager.Instance.JuegoTerminado) return;
        if (salud != null && salud.EstaMuerto) return;

        bool pidio = Input.GetKeyDown(teclaGranada) || (usarClicCentral && Input.GetMouseButtonDown(2));
        if (!pidio) return;

        if (!Lista)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySinBalas();
            return;
        }

        Lanzar();
    }

    void Lanzar()
    {
        if (cam == null) cam = Camera.main;
        Vector2 origen = transform.position;
        Vector2 destino = cam != null ? (Vector2)cam.ScreenToWorldPoint(Input.mousePosition) : origen + Vector2.right * 4f;

        Vector2 delta = destino - origen;
        if (delta.magnitude > alcanceMaximo) destino = origen + delta.normalized * alcanceMaximo;

        // No atraviesa muros: se queda justo antes del obstáculo
        if (capaObstaculos.value != 0)
        {
            RaycastHit2D hit = Physics2D.Linecast(origen, destino, capaObstaculos);
            if (hit.collider != null) destino = hit.point - (destino - origen).normalized * 0.4f;
        }

        Granada.Crear(origen, destino, tiempoDetonacion, radioExplosion, daño, knockback, spriteGranada, spriteExplosion);
        disponibleEn = Time.time + cooldown;
        avisoPendiente = true;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayLanzarGranada();
    }
}
