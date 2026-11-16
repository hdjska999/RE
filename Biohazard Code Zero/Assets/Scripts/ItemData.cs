using UnityEngine;

[CreateAssetMenu(fileName = "NuevoObjeto", menuName = "Inventario/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite itemIcon; // La imagen que saldrá en la cuadrícula
    public bool isWeapon;
    [TextArea] public string description;
}