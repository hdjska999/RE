using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;
    
    [Header("Datos del Inventario")]
    public List<ItemData> items = new List<ItemData>();
    public int maxSlots = 6; // Espacio al estilo clásico

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public bool AddItem(ItemData itemToAdd)
    {
        if (items.Count < maxSlots)
        {
            items.Add(itemToAdd);
            Debug.Log("Recogiste: " + itemToAdd.itemName);
            return true;
        }
        
        Debug.Log("El inventario está lleno bro");
        return false;
    }
}