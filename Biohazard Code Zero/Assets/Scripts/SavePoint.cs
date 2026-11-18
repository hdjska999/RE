using UnityEngine;

public class SavePoint : MonoBehaviour
{
    private bool jugadorCerca = false;
    private GameObject jugadorObj;

    void Update()
    {
        // Presionar E cerca del punto de guardado para guardar
        if (jugadorCerca && Input.GetKeyDown(KeyCode.E))
        {
            if (SaveSystem.Instance != null && jugadorObj != null)
            {
                SaveSystem.Instance.GuardarPartida(jugadorObj);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = true;
            jugadorObj = other.gameObject;
            Debug.Log("Presiona 'E' para guardar tu progreso.");
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = false;
            jugadorObj = null;
        }
    }
}
