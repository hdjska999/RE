using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Salud del Jugador")]
    public int vidaMaxima = 100;
    public int vidaActual;

    [Header("Efecto de Daño")]
    public Color colorDaño = Color.red; // Color del flash
    public float duracionFlash = 0.15f; // Cuánto dura en rojo

    private SpriteRenderer spriteRenderer;
    private Color colorOriginal;

    void Start()
    {
        // Si venimos de "Continue", restauramos la vida guardada; si no, vidaMaxima
        if (SaveSystem.Instance != null && SaveSystem.Instance.DebeCargarDatos())
        {
            vidaActual = PlayerPrefs.GetInt("VidaActual", vidaMaxima);
        }
        else
        {
            vidaActual = vidaMaxima;
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            colorOriginal = spriteRenderer.color;
        }
    }

    public void RecibirDaño(int daño)
    {
        vidaActual -= daño;
        Debug.Log("¡Bryan fue atacado! Vida restante: " + vidaActual);

        if (vidaActual <= 0)
        {
            Morir();
        }
        else if (spriteRenderer != null)
        {
            // Detenemos cualquier flash anterior y empezamos uno nuevo
            StopAllCoroutines();
            StartCoroutine(FlashDaño());
        }
    }

    // Esta corrutina hace el cambio de color ida y vuelta
    IEnumerator FlashDaño()
    {
        spriteRenderer.color = colorDaño;
        yield return new WaitForSeconds(duracionFlash);
        spriteRenderer.color = colorOriginal;
    }

    void Morir()
    {
        Debug.Log("¡Estás muerto! GAME OVER.");
        gameObject.SetActive(false); // Oculta a Bryan
    }
}