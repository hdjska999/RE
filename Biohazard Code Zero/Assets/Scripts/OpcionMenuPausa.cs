using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Fila del menú de pausa estilo RE4: al pasar el mouse la selecciona y al hacer clic la confirma.
/// esSubmenu = true para las filas de Volumen / Opciones.
/// </summary>
public class OpcionMenuPausa : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public PauseManager pause;
    public int indice;
    public bool esSubmenu = false;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (pause == null) return;
        if (esSubmenu) pause.SeleccionarOpcion(indice);
        else pause.Seleccionar(indice);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (pause == null) return;
        if (esSubmenu)
        {
            if (indice == 3) pause.CerrarOpciones(); // Fila "VOLVER"
        }
        else
        {
            pause.Confirmar(indice);
        }
    }
}
