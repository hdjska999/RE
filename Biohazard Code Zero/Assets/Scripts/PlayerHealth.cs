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
    private bool estaMuerto = false;

    void Start()
    {
        vidaActual = vidaMaxima;
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            // Guardamos el color original de Bryan para poder devolvérselo
            colorOriginal = spriteRenderer.color;
        }
    }

    public void RecibirDaño(int daño)
    {
        if (estaMuerto) return;

        vidaActual -= daño;
        Debug.Log("¡Bryan fue atacado! Vida restante: " + vidaActual);

        if (spriteRenderer != null)
        {
            StopAllCoroutines();
            StartCoroutine(EfectoFlash());
        }

        if (vidaActual <= 0)
        {
            Morir();
        }
    }

    public void Curar(int cantidad)
    {
        if (estaMuerto) return;

        vidaActual = Mathf.Min(vidaActual + cantidad, vidaMaxima);
        Debug.Log("¡Bryan se curó! Vida actual: " + vidaActual);
    }

    IEnumerator EfectoFlash()
    {
        spriteRenderer.color = colorDaño;
        yield return new WaitForSeconds(duracionFlash);
        spriteRenderer.color = colorOriginal;
    }

    void Morir()
    {
        if (estaMuerto) return;
        estaMuerto = true;

        Debug.Log("¡Estás muerto! GAME OVER.");

        if (MercenariesManager.Instance != null)
        {
            MercenariesManager.Instance.TerminarPartida("¡HAS MUERTO!");
        }
        else
        {
            Time.timeScale = 0f;
        }
    }
}