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

    [Header("Selector de Armas (1 / 2 / 3)")]
    [Tooltip("Íconos por arma en orden: Pistola, Subfusil, Escopeta")]
    public Sprite[] iconosArmas = new Sprite[3];
    [Tooltip("Fondos de los 3 slots del selector")]
    public Image[] slotsArmas = new Image[3];
    public TextMeshProUGUI textoNombreArma;
    public Color colorSlotActivo = new Color(1f, 0.82f, 0.25f, 0.95f);
    public Color colorSlotInactivo = new Color(0.1f, 0.1f, 0.1f, 0.6f);
    public Color colorSlotBloqueado = new Color(0.05f, 0.05f, 0.05f, 0.3f);

    private PlayerHealth saludJugador;
    private PlayerMovement jugador;
    private float fillObjetivo = 1f;
    private float fillActual = 1f;
    private int ultimoIndiceArma = -1;
    private float pulsoCambioArma = 0f;

    [Header("Aviso de munición (en el texto del arma)")]
    public float duracionAvisoMunicion = 1.4f;
    public Color colorAvisoRecogida = new Color(0.45f, 1f, 0.45f, 1f);
    public Color colorAvisoLleno = new Color(1f, 0.35f, 0.3f, 1f);
    private float finAviso = -1f;
    private Color colorNombreOriginal = new Color(1f, 0.85f, 0.35f, 1f);
    private bool colorNombreGuardado = false;

    void Awake()
    {
        if (saludJugador == null)
            saludJugador = FindAnyObjectByType<PlayerHealth>();
        if (jugador == null)
            jugador = FindAnyObjectByType<PlayerMovement>();
    }

    void OnEnable()
    {
        if (saludJugador != null)
            saludJugador.OnVidaCambiada += ActualizarVida;
        if (jugador != null)
        {
            jugador.OnArmaOMunicionCambiada += ActualizarArma;
            jugador.OnAvisoMunicion += MostrarAvisoMunicion;
        }
        ActualizarArma();
    }

    void OnDisable()
    {
        if (saludJugador != null)
            saludJugador.OnVidaCambiada -= ActualizarVida;
        if (jugador != null)
        {
            jugador.OnArmaOMunicionCambiada -= ActualizarArma;
            jugador.OnAvisoMunicion -= MostrarAvisoMunicion;
        }
    }

    /// <summary>Muestra "+12 PISTOLA" o "MUNICIÓN AL MÁXIMO" un momento en lugar del nombre del arma.</summary>
    public void MostrarAvisoMunicion(string texto, bool recogida)
    {
        if (textoNombreArma == null) return;
        if (!colorNombreGuardado) { colorNombreOriginal = textoNombreArma.color; colorNombreGuardado = true; }
        textoNombreArma.text = texto;
        textoNombreArma.color = recogida ? colorAvisoRecogida : colorAvisoLleno;
        finAviso = Time.time + duracionAvisoMunicion;
        pulsoCambioArma = 0.6f;
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
        ActualizarArma();
    }

    void Update()
    {
        // Fin del aviso de munición: vuelve a mostrar el nombre del arma
        if (finAviso > 0f && Time.time >= finAviso)
        {
            finAviso = -1f;
            if (textoNombreArma != null) textoNombreArma.color = colorNombreOriginal;
            ActualizarArma();
        }

        // Pequeño "golpe" de escala en el ícono al cambiar de arma
        if (iconoArma != null && pulsoCambioArma > 0f)
        {
            pulsoCambioArma = Mathf.MoveTowards(pulsoCambioArma, 0f, Time.deltaTime * 4f);
            iconoArma.rectTransform.localScale = Vector3.one * (1f + pulsoCambioArma * 0.25f);
        }

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

    /// <summary>Refresca ícono, nombre y selector del arma equipada.</summary>
    public void ActualizarArma()
    {
        if (jugador == null) return;
        ArmaConfig arma = jugador.ArmaActual;
        int indice = jugador.IndiceArma;

        if (indice != ultimoIndiceArma)
        {
            if (ultimoIndiceArma >= 0) pulsoCambioArma = 1f;
            ultimoIndiceArma = indice;
        }

        // Ícono: primero el definido en el HUD, luego el del ArmaConfig
        Sprite icono = null;
        if (iconosArmas != null && indice >= 0 && indice < iconosArmas.Length) icono = iconosArmas[indice];
        if (icono == null && arma != null) icono = arma.icono;
        if (iconoArma != null && icono != null)
        {
            iconoArma.sprite = icono;
            iconoArma.preserveAspect = true;
        }

        if (textoNombreArma != null && arma != null && finAviso < 0f)
            textoNombreArma.text = arma.nombre;

        if (slotsArmas != null)
        {
            for (int i = 0; i < slotsArmas.Length; i++)
            {
                if (slotsArmas[i] == null) continue;
                bool activo = i == indice;
                bool desbloqueada = jugador.ArmaDisponible(i);
                slotsArmas[i].color = activo ? colorSlotActivo : (desbloqueada ? colorSlotInactivo : colorSlotBloqueado);
                slotsArmas[i].rectTransform.localScale = activo ? Vector3.one * 1.12f : Vector3.one;
                TextMeshProUGUI num = slotsArmas[i].GetComponentInChildren<TextMeshProUGUI>(true);
                if (num != null) num.color = desbloqueada ? Color.white : new Color(1f, 1f, 1f, 0.2f);
            }
        }
    }

    public void SetVisibilidadArma(bool visible)
    {
        if (contenedorArma != null)
            contenedorArma.SetActive(visible);
    }
}
