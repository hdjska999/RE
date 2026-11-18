using UnityEngine;

public class TimeBonusPickup : MonoBehaviour
{
    public float segundosExtra = 30f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (MercenariesManager.Instance != null)
            {
                MercenariesManager.Instance.AgregarTiempo(segundosExtra);
            }
            Destroy(gameObject);
        }
    }
}