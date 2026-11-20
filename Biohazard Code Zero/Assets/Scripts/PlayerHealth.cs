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

    /// <summary>True mientras Bryan tiene frames de invencibilidad (esquive).</summary>
    public bool EsInvulnerable => Time.time < invulnerableHasta;
    public bool EstaMuerto => estaMuerto;

    private SpriteRenderer spriteRenderer;
    private Color colorOriginal;
    private bool estaMuerto = false;
    private float invulnerableHasta = -1f;

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

    /// <summary>Activa i-frames durante 'segundos' (lo usa el Dash táctico).</summary>
    public void HacerInvulnerable(float segundos)
    {
        invulnerableHasta = Mathf.Max(invulnerableHasta, Time.time + segundos);
    }

    public void RecibirDaño(int daño)
    {
        if (estaMuerto) return;

        // Esquive: el golpe no conecta durante los i-frames
        if (EsInvulnerable) return;

        // Con la partida terminada (resultados en pantalla) ya no se recibe daño
        if (MercenariesManager.Instance != null && MercenariesManager.Instance.JuegoTerminado) return;

        vidaActual = Mathf.Max(0, vidaActual - daño);
        Debug.Log("¡Bryan fue atacado! Vida restante: " + vidaActual);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayDañoBryan();

        if (habilitarCameraShake && CameraFollow.Instance != null)
            CameraFollow.Instance.Shake(shakeDañoDuracion, shakeDañoIntensidad);

        OnVidaCambiada?.Invoke(vidaActual, vidaMaxima);

        if (spriteRenderer != null)
        {
            StopCoroutine(nameof(EfectoFlash));
            StartCoroutine(nameof(EfectoFlash));
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
        // Tiempo real: si Bryan muere y el juego se congela, el flash igual termina
        yield return new WaitForSecondsRealtime(duracionFlash);
        spriteRenderer.color = colorOriginal;
    }

    void Morir()
    {
        if (estaMuerto) return;
        estaMuerto = true;

        Debug.Log("¡Estás muerto! GAME OVER.");

        // Bryan deja de moverse y disparar de inmediato
        PlayerMovement movimiento = GetComponent<PlayerMovement>();
        if (movimiento != null) movimiento.enabled = false;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.angularVelocity = 0f; }

        if (MercenariesManager.Instance != null)
        {
            // Muestra resultados y congela toda la simulación del nivel
            MercenariesManager.Instance.TerminarPartida("¡HAS MUERTO!", false);
        }
        else
        {
            Time.timeScale = 0f;
            if (CameraFollow.Instance != null) CameraFollow.Instance.Congelar();
        }
    }
}
