using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class MercenariesManager : MonoBehaviour
{
    public static MercenariesManager Instance;

    [Header("Configuración de Tiempo (Segundos)")]
    public float tiempoRestante = 120f; // 2 minutos iniciales
    public float segundosPorKill = 3f; // Segundos que suma cada zombie eliminado
    private bool juegoTerminado = false;

    [Header("Puntaje y Combos")]
    public int puntajeTotal = 0;
    public int puntosBasePorKill = 1000;
    public int comboActual = 0;
    public int totalBajas = 0; // Conteo total estilo Mercenarios RE4
    public float tiempoVentanaCombo = 4f; // Segundos máximos entre bajas para mantener el combo
    private float temporizadorCombo = 0f;

    [Header("Referencias UI (TextMeshPro)")]
    public TextMeshProUGUI textoTiempo;
    public TextMeshProUGUI textoPuntaje;
    public TextMeshProUGUI textoCombo;
    public TextMeshProUGUI textoBajas; // Opcional en el panel superior
    public Image barraCombo; // Barra de tiempo restante del combo estilo RE4
    public GameObject panelGameOver;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        Time.timeScale = 1f;
        if (panelGameOver != null) panelGameOver.SetActive(false);
        ActualizarUI();
    }

    void Update()
    {
        if (juegoTerminado) return;

        // 1. Cuenta regresiva del tiempo
        if (tiempoRestante > 0)
        {
            tiempoRestante -= Time.deltaTime;
            if (tiempoRestante <= 0)
            {
                tiempoRestante = 0;
                TerminarPartida("¡TIEMPO AGOTADO!");
            }
        }

        // 2. Ventana del multiplicador de combo
        if (comboActual > 0)
        {
            temporizadorCombo -= Time.deltaTime;
            if (temporizadorCombo <= 0)
            {
                comboActual = 0;
            }
        }

        if (barraCombo != null)
        {
            barraCombo.gameObject.SetActive(comboActual > 0);
            if (comboActual > 0 && tiempoVentanaCombo > 0f)
            {
                barraCombo.fillAmount = Mathf.Clamp01(temporizadorCombo / tiempoVentanaCombo);
            }
        }

        ActualizarUI();
    }

    public void RegistrarBaja()
    {
        if (juegoTerminado) return;

        totalBajas++;
        comboActual++;
        temporizadorCombo = tiempoVentanaCombo;

        int puntosGanados = puntosBasePorKill * comboActual;
        puntajeTotal += puntosGanados;

        tiempoRestante += segundosPorKill;

        ActualizarUI();
    }

    public void AgregarTiempo(float segundosExtra)
    {
        if (juegoTerminado) return;
        tiempoRestante += segundosExtra;
        ActualizarUI();
    }

    public void TerminarPartida(string motivo)
    {
        juegoTerminado = true;
        Debug.Log(motivo + " | Puntaje Final: " + puntajeTotal);
        if (panelGameOver != null) panelGameOver.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ReiniciarPartida()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void ActualizarUI()
    {
        // Reloj con formato MM:SS.ms (ej. 01:45.38)
        if (textoTiempo != null)
        {
            int minutos = Mathf.FloorToInt(tiempoRestante / 60f);
            int segundos = Mathf.FloorToInt(tiempoRestante % 60f);
            int milisegundos = Mathf.FloorToInt((tiempoRestante * 100f) % 100f);
            textoTiempo.text = string.Format("{0:00}:{1:00}.{2:00}", minutos, segundos, milisegundos);

            // Efecto visual de alarma roja cuando queda poco tiempo (< 20 segundos)
            if (tiempoRestante <= 20f && tiempoRestante > 0f)
            {
                textoTiempo.color = Color.Lerp(new Color(1f, 0.2f, 0.2f, 1f), Color.white, Mathf.PingPong(Time.time * 4f, 1f));
            }
            else
            {
                textoTiempo.color = Color.white;
            }
        }

        if (textoPuntaje != null)
            textoPuntaje.text = puntajeTotal.ToString("N0");

        if (textoCombo != null)
        {
            textoCombo.gameObject.SetActive(comboActual > 1);
            textoCombo.text = "x" + comboActual + " COMBO";
        }

        if (textoBajas != null)
        {
            textoBajas.text = totalBajas.ToString();
        }
    }
}
