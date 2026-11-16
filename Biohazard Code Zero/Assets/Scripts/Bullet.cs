using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 1; // Cada bala quita 1 de vida (así ocupas 4 tiros)

    void Update()
    {
        transform.Translate(Vector3.right * speed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Si choca con un enemigo con el Tag "Enemy"
        if (other.CompareTag("Enemy") || other.name.Contains("Lycan"))
        {
            // Intentamos obtener el script de salud del zombie
            EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();

            if (enemyHealth != null)
            {
                enemyHealth.RecibirDaño(damage); // Le resta 1 de vida
            }

            // Destruimos la bala al impactar
            Destroy(gameObject);
        }
    }
}