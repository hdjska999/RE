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
    [Tooltip("Munición: unidades de caja (cada arma las convierte con su Factor Caja Municion). Curación: vida. Tiempo: segundos.")]
    public int cantidad = 15; // Balas, puntos de vida o segundos extra
    public float tiempoDeVida = 15f; // Desaparece si no lo agarras para no saturar el mapa

    [Header("Munición (survival horror)")]
    [Tooltip("Multiplicador global de la cantidad de munición de la caja (0.8 = 20% menos que el valor 'Cantidad')")]
    [Range(0.2f, 1.5f)] public float multiplicadorMunicion = 0.8f;
    [Tooltip("Variación aleatoria hacia abajo (0.25 = la caja trae entre 75% y 100%)")]
    [Range(0f, 0.6f)] public float variacionMunicion = 0.25f;

    private float proximoIntento = 0f;

    void Start()
    {
        Destroy(gameObject, tiempoDeVida);

        if (tipo == TipoDrop.Municion)
        {
            float k = multiplicadorMunicion * Random.Range(1f - variacionMunicion, 1f);
            cantidad = Mathf.Max(1, Mathf.RoundToInt(cantidad * k));
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        IntentarRecoger(other, true);
    }

    // Si estaba lleno y Bryan sigue encima, vuelve a intentarlo (por si disparó y ya hay espacio)
    private void OnTriggerStay2D(Collider2D other)
    {
        if (Time.time >= proximoIntento) IntentarRecoger(other, false);
    }

    void IntentarRecoger(Collider2D other, bool esEntrada)
    {
        if (!other.CompareTag("Player")) return;
        proximoIntento = Time.time + 0.5f;

        switch (tipo)
        {
            case TipoDrop.Municion:
                PlayerMovement movimiento = other.GetComponent<PlayerMovement>();
                if (movimiento == null) return;

                // Todas las armas desbloqueadas llenas: no se desperdicia la caja
                if (movimiento.MunicionLlena())
                {
                    if (esEntrada) movimiento.AgregarMunicion(cantidad); // solo muestra el aviso "MUNICIÓN AL MÁXIMO"
                    return;
                }

                int sobrante = movimiento.AgregarMunicion(cantidad);
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlayRecogerMunicion();

                if (sobrante > 0)
                {
                    cantidad = sobrante; // Lo que no cupo se queda en el suelo
                    return;
                }
                break;

            case TipoDrop.Curacion:
                PlayerHealth salud = other.GetComponent<PlayerHealth>();
                if (salud == null) return;
                // Si ya tiene toda la vida, no gasta el botiquín
                if (salud.vidaActual >= salud.vidaMaxima) return;
                salud.Curar(cantidad);
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlayRecogerCuracion();
                break;

            case TipoDrop.TiempoExtra:
                if (MercenariesManager.Instance == null) return;
                MercenariesManager.Instance.AgregarTiempo(cantidad);
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlayRecogerTiempo();
                break;
        }

        Destroy(gameObject);
    }
}
