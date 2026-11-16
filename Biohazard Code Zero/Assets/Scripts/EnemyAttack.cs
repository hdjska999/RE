using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [Header("Daño del Zombie")]
    public int dañoAtaque = 20; // 5 toques para matarte (100 de vida)
    public float tiempoEntreAtaques = 1.5f; 
    private float proximoAtaque = 0f;

    // Se ejecuta constantemente mientras el zombie esté tocando algo
    void OnCollisionStay2D(Collision2D collision)
    {
        // Verificamos si lo que está tocando es a Bryan y si ya pasó el tiempo de espera
        if (collision.gameObject.CompareTag("Player") && Time.time >= proximoAtaque)
        {
            PlayerHealth vidaBryan = collision.gameObject.GetComponent<PlayerHealth>();

            if (vidaBryan != null)
            {
                vidaBryan.RecibirDaño(dañoAtaque);
                
                // Reiniciamos el cronómetro para el siguiente mordisco
                proximoAtaque = Time.time + tiempoEntreAtaques;
            }
        }
    }
}