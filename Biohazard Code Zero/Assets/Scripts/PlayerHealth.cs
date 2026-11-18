using UnityEngine;
using System.Collections;
using System;

public class PlayerHealth : MonoBehaviour
{
    [Header("Salud del Jugador")]
    public int vidaMaxima = 100;
    public int vidaActual;

    [Header("Efecto de Daño")]
    public Color colorDaño = Color.red; // Color del flash
    public float duracionFlash = 0.15f; // Cuánto dura en rojo

    [Header("Game Feel - Camera Shake")]
    public bool habilitarCameraShake = true;
    public float shakeDañoDuracion = 0.22f;
    public float shakeDañoIntensidad = 0.40f;

    // Evento para notificar al HUD sin acoplamiento
    public event Action<int, int> OnVidaCambiada;
    public float PorcentajeVida => vidaMaxima > 0 ? (float)vidaActual / vidaMaxima : 0f;

    private SpriteRenderer spriteRenderer;
    private Color colorOriginal;
    private bool estaMuerto = false;

    void Start()
    {
        vidaActual = vidaMaxima;
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            colorOriginal = spriteRenderer.color;
        }

        OnVidaCambiada?.Invoke(vidaActual, vidaMaxima);
    }

    public void RecibirDaño(int daño)
    {
        if (estaMuerto) return;

        vidaActual = Mathf.Max(0, vidaActual - daño);
        Debug.Log("¡Bryan fue atacado! Vida restante: " + vidaActual);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayDañoBryan();

        if (habilitarCameraShake && CameraFollow.Instance != null)
            CameraFollow.Instance.Shake(shakeDañoDuracion, shakeDañoIntensidad);

        OnVidaCambiada?.Invoke(vidaActual, vidaMaxima);

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

        OnVidaCambiada?.Invoke(vidaActual, vidaMaxima);
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
