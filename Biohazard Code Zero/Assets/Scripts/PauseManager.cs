using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Menú de pausa estilo RE4 Remake: fondo oscuro translúcido, columna izquierda minimalista
/// (CONTINUAR / REINICIAR / VOLUMEN - OPCIONES / SALIR AL MENÚ) con barra indicadora de selección.
/// Navegación con mouse o teclado (W/S, flechas, Enter, Esc). Funciona con Time.timeScale = 0.
/// </summary>
public class PauseManager : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject pausePanel;
    public PlayerMovement scriptJugador; // Para evitar que Bryan dispare mientras está en pausa

    [Header("Menú estilo RE4 Remake (lo construye Tools > Biohazard Code Zero)")]
    [Tooltip("Filas en orden: 0 Continuar, 1 Reiniciar, 2 Volumen/Opciones, 3 Salir al menú")]
    public RectTransform[] opciones = new RectTransform[0];
    public RectTransform barraSeleccion;
    public RectTransform fondoSeleccion;
    public Color colorTextoNormal = new Color(0.62f, 0.62f, 0.62f, 1f);
    public Color colorTextoSeleccionado = Color.white;
    public float desplazamientoSeleccion = 18f;

    [Header("Submenú de Volumen / Opciones")]
    public GameObject panelOpciones;
    public Slider sliderGeneral;
    public Slider sliderMusica;
    public Slider sliderEfectos;
    [Tooltip("Filas del submenú: General, Música, Efectos, Volver")]
    public RectTransform[] filasOpciones = new RectTransform[0];
    public RectTransform barraSeleccionOpciones;

    public bool EstaPausado => estaPausado;

    private bool estaPausado = false;
    private bool enOpciones = false;
    private int seleccion = 0;
    private int seleccionOpcion = 0;
    private float[] desplazamientos = new float[0];

    void Start()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(false);
        desplazamientos = new float[opciones != null ? opciones.Length : 0];

        ConfigurarSlider(sliderGeneral, v => { if (AudioManager.Instance != null) AudioManager.Instance.SetVolumenGeneral(v); else AudioListener.volume = v; });
        ConfigurarSlider(sliderMusica, v => { if (AudioManager.Instance != null) AudioManager.Instance.SetVolumenMusica(v); });
        ConfigurarSlider(sliderEfectos, v => { if (AudioManager.Instance != null) AudioManager.Instance.SetVolumenSFX(v); });
    }

    void ConfigurarSlider(Slider s, UnityEngine.Events.UnityAction<float> accion)
    {
        if (s == null) return;
        s.minValue = 0f;
        s.maxValue = 1f;
        s.onValueChanged.AddListener(accion);
    }

    void Update()
    {
        // Tras el Game Over / Resultados no se puede pausar ni reanudar
        if (MercenariesManager.Instance != null && MercenariesManager.Instance.JuegoTerminado) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!estaPausado) PausarJuego();
            else if (enOpciones) CerrarOpciones();
            else ReanudarJuego();
            return;
        }

        if (!estaPausado) return;

        if (enOpciones) NavegarOpciones();
        else NavegarMenu();

        AnimarSeleccion();
    }

    // ===================== NAVEGACIÓN =====================
    void NavegarMenu()
    {
        int n = opciones != null ? opciones.Length : 0;
        if (n == 0) return;
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) Seleccionar((seleccion - 1 + n) % n);
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) Seleccionar((seleccion + 1) % n);
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space)) Confirmar(seleccion);
    }

    void NavegarOpciones()
    {
        int n = filasOpciones != null && filasOpciones.Length > 0 ? filasOpciones.Length : 4;
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) SeleccionarOpcion((seleccionOpcion - 1 + n) % n);
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) SeleccionarOpcion((seleccionOpcion + 1) % n);

        Slider s = SliderDeFila(seleccionOpcion);
        if (s != null)
        {
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) s.value = Mathf.Clamp01(s.value - 0.05f);
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) s.value = Mathf.Clamp01(s.value + 0.05f);
        }
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
        {
            CerrarOpciones();
        }
    }

    Slider SliderDeFila(int i)
    {
        switch (i) { case 0: return sliderGeneral; case 1: return sliderMusica; case 2: return sliderEfectos; default: return null; }
    }

    /// <summary>Lo llaman las filas (OpcionMenuPausa) al pasar el mouse.</summary>
    public void Seleccionar(int indice)
    {
        if (opciones == null || indice < 0 || indice >= opciones.Length) return;
        if (indice != seleccion && AudioManager.Instance != null) AudioManager.Instance.PlayHover();
        seleccion = indice;
    }

    public void SeleccionarOpcion(int indice)
    {
        if (indice != seleccionOpcion && AudioManager.Instance != null) AudioManager.Instance.PlayHover();
        seleccionOpcion = indice;
    }

    /// <summary>Lo llaman las filas al hacer clic, o Enter con el teclado.</summary>
    public void Confirmar(int indice)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
        switch (indice)
        {
            case 0: ReanudarJuego(); break;
            case 1: ReiniciarNivel(); break;
            case 2: AbrirOpciones(); break;
            case 3: VolverAlMenuPrincipal(); break;
        }
    }

    // ===================== ANIMACIÓN (tiempo no escalado) =====================
    void AnimarSeleccion()
    {
        float dt = Time.unscaledDeltaTime;

        if (opciones != null && opciones.Length > 0)
        {
            if (desplazamientos.Length != opciones.Length) desplazamientos = new float[opciones.Length];
            for (int i = 0; i < opciones.Length; i++)
            {
                if (opciones[i] == null) continue;
                bool activo = i == seleccion && !enOpciones;
                TextMeshProUGUI t = opciones[i].GetComponentInChildren<TextMeshProUGUI>();
                if (t == null) continue;
                t.color = Color.Lerp(t.color, activo || (i == seleccion && enOpciones) ? colorTextoSeleccionado : colorTextoNormal, dt * 14f);
                desplazamientos[i] = Mathf.Lerp(desplazamientos[i], i == seleccion ? desplazamientoSeleccion : 0f, dt * 14f);
                t.rectTransform.anchoredPosition = new Vector2(desplazamientos[i], t.rectTransform.anchoredPosition.y);
            }
            RectTransform destino = opciones[Mathf.Clamp(seleccion, 0, opciones.Length - 1)];
            MoverIndicador(barraSeleccion, destino, dt);
            MoverIndicador(fondoSeleccion, destino, dt);
        }

        if (enOpciones && filasOpciones != null && filasOpciones.Length > 0)
        {
            for (int i = 0; i < filasOpciones.Length; i++)
            {
                if (filasOpciones[i] == null) continue;
                TextMeshProUGUI t = filasOpciones[i].GetComponentInChildren<TextMeshProUGUI>();
                if (t != null) t.color = Color.Lerp(t.color, i == seleccionOpcion ? colorTextoSeleccionado : colorTextoNormal, dt * 14f);
            }
            MoverIndicador(barraSeleccionOpciones, filasOpciones[Mathf.Clamp(seleccionOpcion, 0, filasOpciones.Length - 1)], dt);
        }
    }

    void MoverIndicador(RectTransform indicador, RectTransform destino, float dt)
    {
        if (indicador == null || destino == null) return;
        Vector2 p = indicador.anchoredPosition;
        p.y = Mathf.Lerp(p.y, destino.anchoredPosition.y, dt * 18f);
        indicador.anchoredPosition = p;
    }

    // ===================== ESTADOS =====================
    public void PausarJuego()
    {
        estaPausado = true;
        enOpciones = false;
        seleccion = 0;
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
            pausePanel.transform.SetAsLastSibling();
        }
        if (panelOpciones != null) panelOpciones.SetActive(false);

        // Congela el tiempo del juego (zombies, balas y animaciones se detienen)
        Time.timeScale = 0f;
        if (scriptJugador != null) scriptJugador.enabled = false;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();

        // Coloca el indicador directamente sobre la primera opción
        if (opciones != null && opciones.Length > 0 && opciones[0] != null)
        {
            float y = opciones[0].anchoredPosition.y;
            if (barraSeleccion != null) barraSeleccion.anchoredPosition = new Vector2(barraSeleccion.anchoredPosition.x, y);
            if (fondoSeleccion != null) fondoSeleccion.anchoredPosition = new Vector2(fondoSeleccion.anchoredPosition.x, y);
        }
    }

    public void ReanudarJuego()
    {
        estaPausado = false;
        enOpciones = false;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(false);

        // Devuelve el tiempo a la normalidad
        Time.timeScale = 1f;
        if (scriptJugador != null) scriptJugador.enabled = true;
        PlayerPrefs.Save();
    }

    public void AbrirOpciones()
    {
        enOpciones = true;
        seleccionOpcion = 0;
        if (panelOpciones != null) panelOpciones.SetActive(true);

        // Refleja los volúmenes actuales sin disparar eventos
        if (sliderGeneral != null) sliderGeneral.SetValueWithoutNotify(AudioManager.Instance != null ? AudioManager.Instance.VolumenGeneral : AudioListener.volume);
        if (sliderMusica != null && AudioManager.Instance != null) sliderMusica.SetValueWithoutNotify(AudioManager.Instance.VolumenMusica);
        if (sliderEfectos != null && AudioManager.Instance != null) sliderEfectos.SetValueWithoutNotify(AudioManager.Instance.VolumenSFX);

        if (barraSeleccionOpciones != null && filasOpciones != null && filasOpciones.Length > 0 && filasOpciones[0] != null)
            barraSeleccionOpciones.anchoredPosition = new Vector2(barraSeleccionOpciones.anchoredPosition.x, filasOpciones[0].anchoredPosition.y);
    }

    public void CerrarOpciones()
    {
        enOpciones = false;
        if (panelOpciones != null) panelOpciones.SetActive(false);
        PlayerPrefs.Save();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
    }

    public void ReiniciarNivel()
    {
        Time.timeScale = 1f;
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void VolverAlMenuPrincipal()
    {
        // CRÍTICO: Siempre restaurar Time.timeScale a 1 antes de cambiar de escena,
        // de lo contrario el menú o la siguiente partida cargarán congelados.
        Time.timeScale = 1f;
        PlayerPrefs.Save();
        Cursor.visible = true;
        SceneManager.LoadScene("MainMenu");
    }
}
