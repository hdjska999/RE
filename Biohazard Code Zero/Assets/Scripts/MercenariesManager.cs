using UnityEngine;
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
    public float tiempoVentanaCombo = 4f; // Segundos máximos entre bajas para mantener el combo
    private float temporizadorCombo = 0f;

    [Header("Referencias UI (TextMeshPro)")]
    public TextMeshProUGUI textoTiempo;
    public TextMeshProUGUI textoPuntaje;
    public TextMeshProUGUI textoCombo;
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

        ActualizarUI();
    }

    // Suma puntos, sube el combo y agrega segundos extra al reloj
    public void RegistrarBaja()
    {
        if (juegoTerminado) return;

        comboActual++;
        temporizadorCombo = tiempoVentanaCombo;

        int puntosGanados = puntosBasePorKill * comboActual;
        puntajeTotal += puntosGanados;

        tiempoRestante += segundosPorKill;

        ActualizarUI();
    }

    // Por si también dejas relojes fijos en algunas esquinas del hospital
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
        if (textoTiempo != null)
        {
            int minutos = Mathf.FloorToInt(tiempoRestante / 60);
            int segundos = Mathf.FloorToInt(tiempoRestante % 60);
            textoTiempo.text = string.Format("{0:00}:{1:00}", minutos, segundos);
        }

        if (textoPuntaje != null)
            textoPuntaje.text = "PTS: " + puntajeTotal.ToString("N0");

        if (textoCombo != null)
        {
            textoCombo.gameObject.SetActive(comboActual > 1);
            textoCombo.text = "x" + comboActual + " COMBO!";
        }
    }
}