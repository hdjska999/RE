using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    [Header("Configuración de Vida")]
    public int vidaMaxima = 4;
    private int vidaActual;

    [Header("Efecto de Daño")]
    public Color colorDaño = Color.red;
    public float duracionFlash = 0.1f;

    private SpriteRenderer spriteRenderer;
    private Color colorOriginal;

    void Start()
    {
        vidaActual = vidaMaxima;
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            colorOriginal = spriteRenderer.color;
        }
    }

    public void RecibirDaño(int daño)
    {
        vidaActual -= daño;
        Debug.Log("¡Zombie atacado! Vida restante: " + vidaActual);

        if (vidaActual <= 0)
        {
            Morir();
        }
        else if (spriteRenderer != null)
        {
            // Reinicia el flash si le das varios tiros seguidos
            StopAllCoroutines();
            StartCoroutine(FlashDaño());
        }
    }

    IEnumerator FlashDaño()
    {
        spriteRenderer.color = colorDaño;
        yield return new WaitForSeconds(duracionFlash);
        spriteRenderer.color = colorOriginal;
    }

    void Morir()
    {
        Debug.Log("¡El zombie ha muerto!");
        Destroy(gameObject);
    }
}