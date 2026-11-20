using System.Collections;
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
    private float tiempoInicial;

    [Header("Puntaje y Combos")]
    public int puntajeTotal = 0;
    public int puntosBasePorKill = 1000;
    public int comboActual = 0;
    public int totalBajas = 0; // Conteo total estilo Mercenarios RE4
    [Tooltip("Segundos máximos entre bajas para mantener el combo")]
    public float tiempoVentanaCombo = 6.5f;
    [Tooltip("Tope del multiplicador de combo (0 = sin tope)")]
    public int comboMaximoMultiplicador = 0;
    private float temporizadorCombo = 0f;
    private float tiempoJugado = 0f;
    private int comboMaximo = 0;

    [Header("Objetivo de la Partida")]
    [Tooltip("La partida se gana al completar esta oleada (0 = desactivado)")]
    public int oleadasParaVictoria = 5;
    [Tooltip("Alternativa: ganar al llegar a este número de bajas (0 = desactivado)")]
    public int objetivoBajas = 0;
    [Tooltip("Puntos extra por cada segundo restante al ganar")]
    public int puntosPorSegundoRestante = 100;
    [HideInInspector] public int oleadaActual = 0;

    [Header("Calificación por Rango (puntaje mínimo)")]
    public int umbralRangoS = 200000;
    public int umbralRangoA = 120000;
    public int umbralRangoB = 60000;
    // Menos de B = rango C
    public Color colorRangoS = new Color(1f, 0.82f, 0.2f, 1f);
    public Color colorRangoA = new Color(0.95f, 0.25f, 0.2f, 1f);
    public Color colorRangoB = new Color(0.35f, 0.65f, 1f, 1f);
    public Color colorRangoC = new Color(0.7f, 0.7f, 0.7f, 1f);

    [Header("Referencias UI (TextMeshPro)")]
    public TextMeshProUGUI textoTiempo;
    public TextMeshProUGUI textoPuntaje;
    public TextMeshProUGUI textoCombo;
    public TextMeshProUGUI textoBajas; // Opcional en el panel superior
    public Image barraCombo; // Barra de tiempo restante del combo estilo RE4
    [Tooltip("Opcional: riel/fondo de la barra de combo. Se muestra y oculta junto con la barra.")]
    public GameObject contenedorBarraCombo;
    public GameObject panelGameOver;

    [Header("Pantalla de Resultados (dentro de PanelGameOver)")]
    public TextMeshProUGUI textoTituloResultado;
    public TextMeshProUGUI textoRango;
    public TextMeshProUGUI textoResumen;
    public GameObject botonSiguienteStage;
    [Tooltip("Sorting Order del panel de Game Over (por encima de cualquier sprite)")]
    public int sortingOrderGameOver = 5000;

    [Header("Resultados estilo Monitor Táctico (RE4 Remake)")]
    public TextMeshProUGUI textoStatStage;
    public TextMeshProUGUI textoStatTiempo;
    public TextMeshProUGUI textoStatBajas;
    public TextMeshProUGUI textoStatCombo;
    public TextMeshProUGUI textoStatPuntaje;
    [Tooltip("Línea pequeña bajo el puntaje (bonus de tiempo, récord)")]
    public TextMeshProUGUI textoStatExtra;
    public GameObject botonSeleccionarStage;
    public CanvasGroup grupoResultados;

    [Header("Flujo de Escenas")]
    [Tooltip("Escena que carga 'Siguiente Stage' al ganar (vacío = ninguna)")]
    public string escenaSiguiente = "";
    public string escenaMenuPrincipal = "MainMenu";

    // ===== Propiedades públicas =====
    public bool JuegoTerminado => juegoTerminado;
    public float ProgresoCombo => (comboActual > 0 && tiempoVentanaCombo > 0f) ? Mathf.Clamp01(temporizadorCombo / tiempoVentanaCombo) : 0f;
    public int ComboMaximo => comboMaximo;
    public string UltimoRango { get; private set; } = "C";

    private Color colorBarraLleno;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        Time.timeScale = 1f;
        tiempoInicial = tiempoRestante;
        if (panelGameOver != null) panelGameOver.SetActive(false);
        if (barraCombo != null) colorBarraLleno = barraCombo.color;
        ActualizarUI();
    }

    void Update()
    {
        if (juegoTerminado) return;
        tiempoJugado += Time.deltaTime;

        // 1. Cuenta regresiva del tiempo
        if (tiempoRestante > 0)
        {
            tiempoRestante -= Time.deltaTime;
            if (tiempoRestante <= 0)
            {
                tiempoRestante = 0;
                TerminarPartida("¡TIEMPO AGOTADO!", false);
                return;
            }
        }

        // 2. Ventana del multiplicador de combo
        if (comboActual > 0)
        {
            temporizadorCombo -= Time.deltaTime;
            if (temporizadorCombo <= 0)
            {
                comboActual = 0;
                temporizadorCombo = 0f;
            }
        }

        // 3. Barra visual del combo: se vacía en tiempo real y pasa a rojo al agotarse
        if (barraCombo != null)
        {
            bool visible = comboActual > 0;
            GameObject objetoBarra = contenedorBarraCombo != null ? contenedorBarraCombo : barraCombo.gameObject;
            if (objetoBarra.activeSelf != visible) objetoBarra.SetActive(visible);
            if (contenedorBarraCombo != null && !barraCombo.gameObject.activeSelf) barraCombo.gameObject.SetActive(true);
            if (visible)
            {
                float p = ProgresoCombo;
                barraCombo.fillAmount = p;
                Color urgente = new Color(0.95f, 0.2f, 0.15f, 1f);
                Color c = Color.Lerp(urgente, colorBarraLleno, Mathf.Clamp01(p * 2f));
                if (p < 0.25f) c.a = Mathf.Lerp(0.35f, 1f, (Mathf.Sin(Time.time * 18f) + 1f) * 0.5f);
                barraCombo.color = c;
            }
        }

        ActualizarUI();
    }

    // Compatibilidad con versiones anteriores
    public void RegistrarBaja() => RegistrarBaja(1f);

    public void RegistrarBaja(float multiplicadorPuntos)
    {
        if (juegoTerminado) return;

        totalBajas++;
        comboActual++;
        if (comboMaximoMultiplicador > 0) comboActual = Mathf.Min(comboActual, comboMaximoMultiplicador);
        comboMaximo = Mathf.Max(comboMaximo, comboActual);
        temporizadorCombo = tiempoVentanaCombo;

        int puntosGanados = Mathf.RoundToInt(puntosBasePorKill * comboActual * Mathf.Max(0.1f, multiplicadorPuntos));
        puntajeTotal += puntosGanados;

        tiempoRestante += segundosPorKill;

        ActualizarUI();

        if (objetivoBajas > 0 && totalBajas >= objetivoBajas)
        {
            TerminarPartida("¡OBJETIVO CUMPLIDO!", true);
        }
    }

    /// <summary>WaveSpawner lo llama al limpiar una oleada. Devuelve true si la partida terminó (victoria).</summary>
    public bool OleadaCompletada(int numeroOleada)
    {
        if (juegoTerminado) return true;
        oleadaActual = numeroOleada;

        if (oleadasParaVictoria > 0 && numeroOleada >= oleadasParaVictoria)
        {
            TerminarPartida("¡STAGE COMPLETADO!", true);
            return true;
        }
        return false;
    }

    public void AgregarTiempo(float segundosExtra)
    {
        if (juegoTerminado) return;
        tiempoRestante += segundosExtra;
        ActualizarUI();
    }

    // Compatibilidad: llamadas antiguas con solo el motivo
    public void TerminarPartida(string motivo) => TerminarPartida(motivo, false);

    public void TerminarPartida(string motivo, bool victoria)
    {
        if (juegoTerminado) return;
        juegoTerminado = true;

        int bonusTiempo = 0;
        if (victoria)
        {
            bonusTiempo = Mathf.RoundToInt(tiempoRestante) * puntosPorSegundoRestante;
            puntajeTotal += bonusTiempo;
        }

        UltimoRango = CalcularRango(puntajeTotal);
        bool nuevoRecord = GuardarRecord();

        string escena = SceneManager.GetActiveScene().name;
        ProgresoJuego.GuardarMejorRango(escena, UltimoRango);
        if (victoria) ProgresoJuego.CompletarStage(escena);
        if (string.IsNullOrEmpty(escenaSiguiente) && ProgresoJuego.NumeroStage(escena) > 0)
            escenaSiguiente = ProgresoJuego.EscenaSiguiente(escena);

        Debug.Log(motivo + " | Puntaje Final: " + puntajeTotal + " | Rango: " + UltimoRango);
        ActualizarUI();
        CongelarNivel();
        MostrarResultados(motivo, victoria, bonusTiempo, nuevoRecord);
        Time.timeScale = 0f;
    }

    /// <summary>
    /// Congela por completo el nivel detrás del monitor de resultados: tiempo en 0, Bryan, granadas,
    /// spawner, zombies (IA, ataques y animaciones) y cámara. La UI sigue funcionando (usa tiempo real).
    /// Al Reintentar / Siguiente Stage / Menú se recarga la escena con Time.timeScale = 1.
    /// </summary>
    void CongelarNivel()
    {
        Time.timeScale = 0f;

        PlayerMovement jugador = FindAnyObjectByType<PlayerMovement>();
        if (jugador != null)
        {
            jugador.enabled = false;
            DetenerCuerpo(jugador.gameObject);
        }

        LanzadorGranadas granadas = FindAnyObjectByType<LanzadorGranadas>();
        if (granadas != null) granadas.enabled = false;

        foreach (WaveSpawner spawner in FindObjectsByType<WaveSpawner>())
        {
            spawner.StopAllCoroutines();
            spawner.enabled = false;
        }

        foreach (ZombieEnemy zombie in FindObjectsByType<ZombieEnemy>())
        {
            zombie.enabled = false;
            DetenerCuerpo(zombie.gameObject);
        }
        foreach (EnemyAttack ataque in FindObjectsByType<EnemyAttack>()) ataque.enabled = false;
        foreach (AtaqueEscupidor escupidor in FindObjectsByType<AtaqueEscupidor>()) escupidor.enabled = false;

        if (CameraFollow.Instance != null) CameraFollow.Instance.Congelar();
    }

    static void DetenerCuerpo(GameObject go)
    {
        Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.angularVelocity = 0f; }
        Animator anim = go.GetComponent<Animator>();
        if (anim != null) anim.speed = 0f;
    }

    public string CalcularRango(int puntaje)
    {
        if (puntaje >= umbralRangoS) return "S";
        if (puntaje >= umbralRangoA) return "A";
        if (puntaje >= umbralRangoB) return "B";
        return "C";
    }

    Color ColorDeRango(string rango)
    {
        switch (rango)
        {
            case "S": return colorRangoS;
            case "A": return colorRangoA;
            case "B": return colorRangoB;
            default: return colorRangoC;
        }
    }

    bool GuardarRecord()
    {
        string clave = "Record_" + SceneManager.GetActiveScene().name;
        int record = PlayerPrefs.GetInt(clave, 0);
        if (puntajeTotal > record)
        {
            PlayerPrefs.SetInt(clave, puntajeTotal);
            PlayerPrefs.Save();
            return record > 0;
        }
        return false;
    }

    void MostrarResultados(string motivo, bool victoria, int bonusTiempo, bool nuevoRecord)
    {
        if (panelGameOver == null) return;

        AsegurarPanelAlFrente();
        panelGameOver.SetActive(true);

        if (textoTituloResultado != null)
        {
            textoTituloResultado.text = motivo;
            textoTituloResultado.color = victoria ? new Color(1f, 0.85f, 0.3f, 1f) : new Color(0.9f, 0.15f, 0.12f, 1f);
        }

        if (textoRango != null)
        {
            textoRango.text = UltimoRango;
            textoRango.color = ColorDeRango(UltimoRango);
        }

        bool monitorTactico = textoStatPuntaje != null;
        if (monitorTactico)
        {
            ConfigStage cfg = GetComponent<ConfigStage>();
            string stage = cfg != null ? cfg.nombreVisible : SceneManager.GetActiveScene().name.ToUpper();
            if (textoStatStage != null) textoStatStage.text = stage + "  /  MERCENARIES";
            if (textoStatTiempo != null)
            {
                int m = Mathf.FloorToInt(tiempoJugado / 60f), sg = Mathf.FloorToInt(tiempoJugado % 60f), cs = Mathf.FloorToInt((tiempoJugado * 100f) % 100f);
                textoStatTiempo.text = string.Format("{0:00}:{1:00}.{2:00}", m, sg, cs);
            }
            if (textoStatBajas != null) textoStatBajas.text = totalBajas.ToString();
            if (textoStatCombo != null) textoStatCombo.text = "x" + comboMaximo;
            if (textoStatExtra != null)
            {
                string extra = "OLEADA " + oleadaActual + (oleadasParaVictoria > 0 ? " / " + oleadasParaVictoria : "");
                if (bonusTiempo > 0) extra += "     BONUS DE TIEMPO +" + bonusTiempo.ToString("N0");
                if (nuevoRecord) extra += "     <color=#FFD24A>¡NUEVO RÉCORD!</color>";
                textoStatExtra.text = extra;
            }
            if (textoResumen != null) textoResumen.gameObject.SetActive(false);
            StartCoroutine(AnimarResultados());
        }

        if (botonSeleccionarStage != null)
        {
            botonSeleccionarStage.SetActive(true);
            Button b = botonSeleccionarStage.GetComponent<Button>();
            if (b != null) b.interactable = ProgresoJuego.JuegoCompletado;
        }

        if (textoResumen != null && !monitorTactico)
        {
            int segs = Mathf.Max(0, Mathf.FloorToInt(tiempoRestante));
            string totalOleadas = oleadasParaVictoria > 0 ? (" / " + oleadasParaVictoria) : "";
            string resumen =
                "PUNTAJE  <b>" + puntajeTotal.ToString("N0") + "</b>\n" +
                "BAJAS  " + totalBajas + "      COMBO MÁX  x" + comboMaximo + "\n" +
                "OLEADA  " + oleadaActual + totalOleadas + "      TIEMPO  " + string.Format("{0:00}:{1:00}", segs / 60, segs % 60);
            if (bonusTiempo > 0) resumen += "\nBONUS DE TIEMPO  +" + bonusTiempo.ToString("N0");
            if (nuevoRecord) resumen += "\n<color=#FFD24A>¡NUEVO RÉCORD!</color>";
            resumen += "\n<size=70%>S: " + umbralRangoS.ToString("N0") + "+   A: " + umbralRangoA.ToString("N0") + "+   B: " + umbralRangoB.ToString("N0") + "+</size>";
            textoResumen.text = resumen;
        }

        if (botonSiguienteStage != null)
        {
            bool haySiguiente = victoria && !string.IsNullOrEmpty(escenaSiguiente) && Application.CanStreamedLevelBeLoaded(escenaSiguiente);
            botonSiguienteStage.SetActive(haySiguiente);
            TextMeshProUGUI etiqueta = botonSiguienteStage.GetComponentInChildren<TextMeshProUGUI>(true);
            if (etiqueta != null) etiqueta.text = escenaSiguiente == ProgresoJuego.ESCENA_CREDITOS ? "VER CRÉDITOS" : "SIGUIENTE STAGE";
        }
    }

    /// <summary>
    /// Garantiza que el panel de Game Over tape TODO: lo mueve al final del Canvas y le da
    /// un Canvas anidado con Sorting Order muy alto en la última Sorting Layer.
    /// (El Canvas del HUD está en Screen Space - Camera, así que compite con los sprites).
    /// </summary>
    public void AsegurarPanelAlFrente()
    {
        if (panelGameOver == null) return;

        panelGameOver.transform.SetAsLastSibling();

        Canvas c = panelGameOver.GetComponent<Canvas>();
        if (c == null) c = panelGameOver.AddComponent<Canvas>();
        c.overrideSorting = true;
        SortingLayer[] capas = SortingLayer.layers;
        if (capas != null && capas.Length > 0) c.sortingLayerID = capas[capas.Length - 1].id;
        c.sortingOrder = sortingOrderGameOver;

        if (panelGameOver.GetComponent<GraphicRaycaster>() == null)
            panelGameOver.AddComponent<GraphicRaycaster>();
    }

    /// <summary>Aparición del monitor, conteo del puntaje y "golpe" de la letra de rango (tiempo real: el juego está en pausa).</summary>
    IEnumerator AnimarResultados()
    {
        if (grupoResultados != null) grupoResultados.alpha = 0f;
        if (textoRango != null) textoRango.transform.localScale = Vector3.zero;
        if (textoStatPuntaje != null) textoStatPuntaje.text = "0";

        for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
        {
            if (grupoResultados != null) grupoResultados.alpha = t / 0.35f;
            yield return null;
        }
        if (grupoResultados != null) grupoResultados.alpha = 1f;

        float duracion = 1.1f;
        for (float t = 0f; t < duracion; t += Time.unscaledDeltaTime)
        {
            float k = 1f - Mathf.Pow(1f - t / duracion, 3f);
            if (textoStatPuntaje != null) textoStatPuntaje.text = Mathf.RoundToInt(puntajeTotal * k).ToString("N0");
            yield return null;
        }
        if (textoStatPuntaje != null) textoStatPuntaje.text = puntajeTotal.ToString("N0");
        if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();

        if (textoRango != null)
        {
            for (float t = 0f; t < 0.3f; t += Time.unscaledDeltaTime)
            {
                float e = t / 0.3f;
                textoRango.transform.localScale = Vector3.one * Mathf.Lerp(2.2f, 1f, 1f - (1f - e) * (1f - e));
                yield return null;
            }
            textoRango.transform.localScale = Vector3.one; // (sin camera shake: el nivel está congelado)
            if (AudioManager.Instance != null) AudioManager.Instance.PlayRecogerTiempo();
        }
    }

    /// <summary>Botón SELECCIONAR STAGE: vuelve al menú y abre el selector.</summary>
    public void IrASeleccionStage()
    {
        Time.timeScale = 1f;
        PlayerPrefs.SetInt("BCZ_AbrirSelector", 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(escenaMenuPrincipal);
    }

    public void ReiniciarPartida()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void SiguienteStage()
    {
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(escenaSiguiente))
            SceneManager.LoadScene(escenaSiguiente);
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void IrAlMenuPrincipal()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(escenaMenuPrincipal);
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
