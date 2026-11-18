using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class MenuButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Referencias")]
    public RectTransform underlineRect; 
    public TextMeshProUGUI buttonText;
    
    [Header("Colores")]
    public Color hoverColor = Color.white;
    private Color normalColor; // Se hizo privada para evitar interferencias del Inspector

    [Header("Animación")]
    public float animSpeed = 15f; 
    
    private Coroutine scaleCoroutine;

    void Start()
    {
        // El script guarda automáticamente el color original que le diste al texto
        if (buttonText != null)
        {
            normalColor = buttonText.color;
        }
        
        // Aplastamos la línea a tamaño 0
        if (underlineRect != null)
        {
            underlineRect.localScale = new Vector3(0, 1, 1);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (buttonText != null) buttonText.color = hoverColor;
        
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayHover();

        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScaleUnderline(1f));
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayClick();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Regresa exactamente al color que guardó al inicio
        if (buttonText != null) buttonText.color = normalColor;
        
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScaleUnderline(0f));
    }

    IEnumerator ScaleUnderline(float targetScaleX)
    {
        if (underlineRect == null) yield break;

        Vector3 currentScale = underlineRect.localScale;
        
        while (Mathf.Abs(currentScale.x - targetScaleX) > 0.01f)
        {
            currentScale.x = Mathf.Lerp(currentScale.x, targetScaleX, Time.unscaledDeltaTime * animSpeed);
            underlineRect.localScale = currentScale;
            yield return null;
        }
        
        currentScale.x = targetScaleX;
        underlineRect.localScale = currentScale;
    }
}