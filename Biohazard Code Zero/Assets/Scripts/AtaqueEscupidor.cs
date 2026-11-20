using UnityEngine;

/// <summary>
/// Zombie Escupidor: se mantiene a distancia media (ZombieEnemy.distanciaPreferida) y,
/// tras un aviso visual de 0.55 s (se infla y brilla verde), escupe un proyectil de ácido hacia Bryan.
/// </summary>
[RequireComponent(typeof(ZombieEnemy))]
public class AtaqueEscupidor : MonoBehaviour
{
    [Header("Escupitajo")]
    public float tiempoEntreEscupitajos = 2.8f;
    public float tiempoAviso = 0.55f;
    public float alcanceMaximo = 9f;
    public float velocidadProyectil = 7.5f;
    public int dañoProyectil = 12;
    public Sprite spriteProyectil;
    public Color colorProyectil = new Color(0.55f, 1f, 0.2f, 1f);

    [Header("Aviso")]
    public Color colorAviso = new Color(0.6f, 1f, 0.3f, 1f);
    public float escalaAviso = 0.18f;

    private ZombieEnemy zombie;
    private SpriteRenderer sr;
    private Transform jugador;
    private PlayerHealth salud;
    private Color colorBase;
    private float proximoAtaque;
    private float finAviso = -1f;
    private bool avisando = false;

    void Awake()
    {
        zombie = GetComponent<ZombieEnemy>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) { jugador = p.transform; salud = p.GetComponent<PlayerHealth>(); }
        if (sr != null) colorBase = sr.color;
        proximoAtaque = Time.time + Random.Range(1.2f, 2.4f);
    }

    void Update()
    {
        if (jugador == null || (salud != null && salud.EstaMuerto)) { Cancelar(); return; }

        if (avisando)
        {
            if (zombie != null && zombie.EnKnockback) { Cancelar(); proximoAtaque = Time.time + 1f; return; }

            float t = 1f - Mathf.Clamp01((finAviso - Time.time) / Mathf.Max(0.01f, tiempoAviso));
            if (sr != null) sr.color = Color.Lerp(colorBase, colorAviso, (Mathf.Sin(Time.time * 35f) + 1f) * 0.5f);
            if (zombie != null) zombie.EscalaExtra = escalaAviso * t;

            if (Time.time >= finAviso) Escupir();
            return;
        }

        if (Time.time >= proximoAtaque && Vector2.Distance(jugador.position, transform.position) <= alcanceMaximo)
        {
            avisando = true;
            finAviso = Time.time + tiempoAviso;
            if (zombie != null) { zombie.MovimientoBloqueado = true; zombie.DispararAnimacionAtaque(); }
            if (AudioManager.Instance != null) AudioManager.Instance.PlayZombieAviso();
        }
    }

    void Escupir()
    {
        Vector2 origen = transform.position;
        Vector2 dir = ((Vector2)jugador.position - origen).normalized;
        ProyectilAcido.Crear(origen + dir * 0.6f, dir, velocidadProyectil, dañoProyectil, spriteProyectil, colorProyectil);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayEscupitajo();

        Cancelar();
        proximoAtaque = Time.time + tiempoEntreEscupitajos + Random.Range(-0.4f, 0.4f);
    }

    void Cancelar()
    {
        if (!avisando) return;
        avisando = false;
        if (sr != null) sr.color = colorBase;
        if (zombie != null) { zombie.EscalaExtra = 0f; zombie.MovimientoBloqueado = false; }
    }

    void OnDisable()
    {
        Cancelar();
    }
}
