using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Salud del Enemigo")]
    public int vidaMaxima = 2;
    private int vidaActual;

    [Header("Efecto Visual de Daño")]
    public Color colorDaño = Color.red;
    public float duracionFlash = 0.1f;

    [Header("Drops de Loot al Morir")]
    public GameObject prefabMunicion;
    public GameObject prefabCuracion;
    [Tooltip("Vida máxima que debe tener Bryan para que puedan salir hierbas verdes")]
    public int umbralPocaVida = 40;
    [Range(0f, 100f)]
    public float probabilidadHierbaPocaVida = 75f;

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
    }

    public void RecibirDaño(int cantidadDaño)
    {
        if (estaMuerto) return;

        vidaActual -= cantidadDaño;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayImpactoBala();
            AudioManager.Instance.PlayDañoZombie();
        }

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

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMuerteZombie();
        }

        if (MercenariesManager.Instance != null)
        {
            MercenariesManager.Instance.RegistrarBaja();
        }

        SoltarLoot();
        Destroy(gameObject);
    }

    void SoltarLoot()
    {
        // 1. Siempre suelta caja de munición
        if (prefabMunicion != null)
        {
            Instantiate(prefabMunicion, transform.position, Quaternion.identity);
        }

        // 2. Si Bryan tiene poca vida, suelta hierba verde
        if (prefabCuracion != null)
        {
            PlayerHealth saludJugador = FindAnyObjectByType<PlayerHealth>();
            if (saludJugador != null && saludJugador.vidaActual <= umbralPocaVida)
            {
                if (Random.Range(0f, 100f) <= probabilidadHierbaPocaVida)
                {
                    // Se desplaza ligeramente a un lado para que no se encime con la caja de balas
                    Vector2 offset = new Vector2(0.45f, 0.2f);
                    Instantiate(prefabCuracion, (Vector2)transform.position + offset, Quaternion.identity);
                }
            }
        }
    }
}