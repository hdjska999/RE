using UnityEngine;

public class PickupItem : MonoBehaviour
{
    [Header("Configuración del Objeto")]
    public ItemData itemData;
    public bool esPistola = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Si hay InventoryManager y itemData, verificamos que sí quepa en el inventario
            if (InventoryManager.Instance != null && itemData != null)
            {
                bool recogido = InventoryManager.Instance.AddItem(itemData);
                if (!recogido) return; // Si está lleno (6 slots), no recoge ni borra el objeto del piso
            }

            // Activar animaciones en el Player
            Animator anim = other.GetComponent<Animator>();
            if (anim != null)
            {
                anim.SetTrigger("Recoger");

                if (esPistola)
                {
                    anim.SetBool("tienePistola", true);
                }
            }

            Destroy(gameObject);
        }
    }
}