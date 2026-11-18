using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDMercenariesController : MonoBehaviour
{
    [Header("Cluster Inferior Derecho - Salud Radial")]
    [Tooltip("Componente Image configurado en Filled -> Radial 360")]
    public Image barraVidaRadial;
    public float velocidadSuavizado = 8f;

    [Header("Colores de Estado RE4")]
    public Color colorSaludAlta = new Color(0.2f, 0.85f, 0.3f, 1f);     // Verde (> 50%)
    public Color colorSaludMedia = new Color(0.95f, 0.75f, 0.1f, 1f);   // Amarillo (25% - 50%)
    public Color colorSaludCritica = new Color(0.9f, 0.15f, 0.15f, 1f); // Rojo (< 25%)

    [Header("Efecto de Pulso Crítico")]
    public bool habilitarPulsoCritico = true;
    public float velocidadPulso = 6f;

    [Header("Cluster Inferior Derecho - Arma")]
    public Image iconoArma;
    public GameObject contenedorArma;

    private PlayerHealth saludJugador;
    private float fillObjetivo = 1f;
    private float fillActual = 1f;

    void Awake()
    {
        if (saludJugador == null)
            saludJugador = FindAnyObjectByType<PlayerHealth>();
    }

    void OnEnable()
    {
        if (saludJugador != null)
            saludJugador.OnVidaCambiada += ActualizarVida;
    }

    void OnDisable()
    {
        if (saludJugador != null)
            saludJugador.OnVidaCambiada -= ActualizarVida;
    }

    void Start()
    {
        if (saludJugador != null)
        {
            ActualizarVida(saludJugador.vidaActual, saludJugador.vidaMaxima);
            fillActual = fillObjetivo;
            if (barraVidaRadial != null)
                barraVidaRadial.fillAmount = fillActual;
        }
    }

    void Update()
    {
        if (barraVidaRadial == null) return;

        // 1. Suavizado del medidor radial
        fillActual = Mathf.MoveTowards(fillActual, fillObjetivo, velocidadSuavizado * Time.deltaTime);
        barraVidaRadial.fillAmount = fillActual;

        // 2. Determinación de color según el porcentaje
        Color colorBase;
        if (fillActual > 0.5f)
            colorBase = colorSaludAlta;
        else if (fillActual > 0.25f)
            colorBase = colorSaludMedia;
        else
            colorBase = colorSaludCritica;

        // 3. Pulso en estado crítico (< 25%)
        if (habilitarPulsoCritico && fillActual <= 0.25f)
        {
            float factorPulso = (Mathf.Sin(Time.time * velocidadPulso) + 1f) * 0.5f;
            colorBase.a = Mathf.Lerp(0.4f, 1f, factorPulso);
        }
        else
        {
            colorBase.a = 1f;
        }

        barraVidaRadial.color = colorBase;
    }

    public void ActualizarVida(int vidaActual, int vidaMaxima)
    {
        fillObjetivo = vidaMaxima > 0 ? Mathf.Clamp01((float)vidaActual / vidaMaxima) : 0f;
    }

    public void SetVisibilidadArma(bool visible)
    {
        if (contenedorArma != null)
            contenedorArma.SetActive(visible);
    }
}
