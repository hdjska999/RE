using UnityEngine;

public class MercenariesDrop : MonoBehaviour
{
    public enum TipoDrop
    {
        Municion,
        Curacion,
        TiempoExtra
    }

    [Header("Configuración del Drop")]
    public TipoDrop tipo = TipoDrop.Municion;
    public int cantidad = 15; // Balas, puntos de vida o segundos extra
    public float tiempoDeVida = 15f; // Desaparece si no lo agarras para no saturar el mapa

    void Start()
    {
        Destroy(gameObject, tiempoDeVida);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        switch (tipo)
        {
            case TipoDrop.Municion:
                PlayerMovement movimiento = other.GetComponent<PlayerMovement>();
                if (movimiento != null)
                {
                    movimiento.AgregarMunicion(cantidad);
                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlayRecogerMunicion();
                }
                break;

            case TipoDrop.Curacion:
                PlayerHealth salud = other.GetComponent<PlayerHealth>();
                if (salud != null)
                {
                    // Si ya tiene toda la vida, no gasta el botiquín
                    if (salud.vidaActual >= salud.vidaMaxima) return;
                    salud.Curar(cantidad);
                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlayRecogerCuracion();
                }
                break;

            case TipoDrop.TiempoExtra:
                if (MercenariesManager.Instance != null)
                {
                    MercenariesManager.Instance.AgregarTiempo(cantidad);
                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlayRecogerTiempo();
                }
                break;
        }

        Destroy(gameObject);
    }
}