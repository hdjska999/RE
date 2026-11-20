using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Valores base (el arma los sobrescribe al disparar)")]
    public float speed = 10f;
    public float damage = 1f; // Pistola = 1 de daño (4 tiros al Lycan)
    [Tooltip("Retroceso físico aplicado al zombie impactado")]
    public float knockback = 3.5f;
    [Tooltip("Segundos antes de autodestruirse si no golpea nada")]
    public float vidaUtil = 2f;
    [Tooltip("Porcentaje mínimo de daño al final del recorrido (caída por distancia, útil para la escopeta)")]
    [Range(0f, 1f)] public float dañoMinimoAlFinal = 1f;

    private float tiempoNacimiento;
    private bool yaImpacto = false;

    /// <summary>Lo llama PlayerMovement justo después de instanciar la bala.</summary>
    public void Configurar(float velocidad, float daño, float fuerzaKnockback, float vida, float dañoMinimo = 1f)
    {
        speed = velocidad;
        damage = daño;
        knockback = fuerzaKnockback;
        vidaUtil = vida;
        dañoMinimoAlFinal = Mathf.Clamp01(dañoMinimo);
    }

    void Start()
    {
        tiempoNacimiento = Time.time;
        Destroy(gameObject, Mathf.Max(0.05f, vidaUtil));
    }

    void Update()
    {
        transform.Translate(Vector3.right * speed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (yaImpacto) return;

        // Si choca con un enemigo con el Tag "Enemy"
        if (other.CompareTag("Enemy") || other.name.Contains("Lycan"))
        {
            EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();
            if (enemyHealth == null) enemyHealth = other.GetComponentInParent<EnemyHealth>();

            if (enemyHealth != null)
            {
                float progreso = vidaUtil > 0f ? Mathf.Clamp01((Time.time - tiempoNacimiento) / vidaUtil) : 0f;
                float factor = Mathf.Lerp(1f, dañoMinimoAlFinal, progreso);
                enemyHealth.RecibirDaño(damage * factor, (Vector2)transform.right, knockback * factor);
            }

            yaImpacto = true;
            Destroy(gameObject);
            return;
        }

        // Muros / obstáculos sólidos: la bala se detiene (ignora triggers, drops y al propio Bryan)
        if (!other.isTrigger && !other.CompareTag("Player"))
        {
            yaImpacto = true;
            Destroy(gameObject);
        }
    }
}
