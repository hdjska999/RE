using UnityEngine;
using System.Collections; // Necesario para el tiempo de espera

public class PickupItem : MonoBehaviour
{
    public ItemData itemInfo; // Aquí metes la info del arma
    private bool yaSeRecogio = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        // Revisa si el que lo pisó es Bryan y si no se ha agarrado ya
        if (other.CompareTag("Player") && !yaSeRecogio)
        {
            yaSeRecogio = true;
            StartCoroutine(SecuenciaRecoger(other.gameObject));
        }
    }

    IEnumerator SecuenciaRecoger(GameObject player)
    {
        // 1. Intentamos meterlo al inventario "invisible"
        bool pickedUp = InventoryManager.Instance.AddItem(itemInfo);
        
        if (pickedUp)
        {
            // Ocultamos la pistola del piso para que parezca que la quitó
            GetComponent<SpriteRenderer>().enabled = false;
            GetComponent<Collider2D>().enabled = false;

            // 2. Buscamos el Animator y el script de Bryan
            Animator anim = player.GetComponent<Animator>();
            PlayerMovement movimiento = player.GetComponent<PlayerMovement>(); // O como se llame tu script de moverte

            if (anim != null)
            {
                // Desactivamos el movimiento para que no camine mientras se agacha
                if (movimiento != null) movimiento.enabled = false;

                // Activamos la animación de agacharse
                anim.SetTrigger("recoger");

                // Esperamos lo que dura la animación (ej: 0.5 segundos)
                yield return new WaitForSeconds(0.5f);

                // Le decimos al Animator que ya traemos la pistola encima
                if (itemInfo.isWeapon)
                {
                    anim.SetBool("tienePistola", true);
                }

                // Devolvemos el control al jugador
                if (movimiento != null) movimiento.enabled = true;
            }

            // 3. Destruimos el objeto del suelo definitivamente
            Destroy(gameObject);
        }
        else
        {
            yaSeRecogio = false; // Por si el inventario estaba lleno y no pudo agarrarla
        }
    }

    void Start()
    {
        // Si ya cargamos partida y Bryan ya tiene un arma, destruimos esta pistola del suelo
        if (itemInfo != null && itemInfo.isWeapon)
        {
            if (SaveSystem.Instance != null && SaveSystem.Instance.DebeCargarDatos())
            {
                if (PlayerPrefs.GetInt("TienePistola", 0) == 1)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}