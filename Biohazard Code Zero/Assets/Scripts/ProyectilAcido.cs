using UnityEngine;

/// <summary>Escupitajo de ácido del Zombie Escupidor. Se crea por código.</summary>
public class ProyectilAcido : MonoBehaviour
{
    private Vector2 direccion;
    private float velocidad;
    private int daño;
    private float vida = 3f;

    public static ProyectilAcido Crear(Vector2 pos, Vector2 dir, float velocidad, int daño, Sprite sprite, Color color)
    {
        GameObject go = new GameObject("ProyectilAcido");
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        go.transform.localScale = Vector3.one * 0.45f;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = 6;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = sprite != null ? Mathf.Max(0.1f, sprite.bounds.extents.x * 0.8f) : 0.5f;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        ProyectilAcido p = go.AddComponent<ProyectilAcido>();
        p.direccion = dir.normalized;
        p.velocidad = velocidad;
        p.daño = daño;
        return p;
    }

    void Update()
    {
        transform.position += (Vector3)(direccion * velocidad * Time.deltaTime);
        transform.Rotate(0f, 0f, 540f * Time.deltaTime);
        vida -= Time.deltaTime;
        if (vida <= 0f) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth salud = other.GetComponent<PlayerHealth>();
            if (salud != null) salud.RecibirDaño(daño); // Si Bryan está esquivando (i-frames) no le hace daño
            if (AudioManager.Instance != null) AudioManager.Instance.PlayAcido();
            Destroy(gameObject);
            return;
        }

        // Se deshace contra muros y obstáculos (ignora triggers, zombies y otras balas)
        if (!other.isTrigger && !other.CompareTag("Enemy") && other.GetComponent<EnemyHealth>() == null)
            Destroy(gameObject);
    }
}
