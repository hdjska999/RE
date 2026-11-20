using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("Referencias UI")]
    public Button continueButton;
    public TextMeshProUGUI continueText;

    [Header("Selector de Stage (se desbloquea al completar el juego)")]
    public Button botonSeleccionStage;
    public TextMeshProUGUI textoSeleccionStage;
    public GameObject panelSeleccionStage;
    [Tooltip("Botones de Stage 1, 2 y 3 dentro del panel")]
    public Button[] botonesStage = new Button[3];
    [Tooltip("Texto de récord / rango bajo cada botón de Stage")]
    public TextMeshProUGUI[] textosInfoStage = new TextMeshProUGUI[3];

    void Start()
    {
        Time.timeScale = 1f;
        Cursor.visible = true;
        if (panelSeleccionStage != null) panelSeleccionStage.SetActive(false);

        // Continuar: hay progreso nuevo (BCZ) o partida del sistema de guardado anterior
        bool puedeContinuar = ProgresoJuego.HayProgreso || PlayerPrefs.GetInt("HayPartida", 0) == 1;
        ConfigurarBoton(continueButton, continueText, puedeContinuar);

        // Seleccionar Stage: solo tras terminar el juego por primera vez
        ConfigurarBoton(botonSeleccionStage, textoSeleccionStage, ProgresoJuego.JuegoCompletado);
        if (textoSeleccionStage != null && !ProgresoJuego.JuegoCompletado)
            textoSeleccionStage.text = "SELECCIONAR STAGE  <size=60%>(COMPLETA EL JUEGO)</size>";

        // Viene del botón SELECCIONAR STAGE de la pantalla de resultados
        if (PlayerPrefs.GetInt("BCZ_AbrirSelector", 0) == 1)
        {
            PlayerPrefs.DeleteKey("BCZ_AbrirSelector");
            if (ProgresoJuego.JuegoCompletado) AbrirSelectorStages();
        }
    }

    void Update()
    {
        if (panelSeleccionStage != null && panelSeleccionStage.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            CerrarSelectorStages();
    }

    void ConfigurarBoton(Button boton, TextMeshProUGUI texto, bool activo)
    {
        if (boton == null) return;
        boton.interactable = activo;
        if (texto != null)
        {
            Color c = texto.color;
            c.a = activo ? 1f : 0.25f;
            texto.color = c;
        }
    }

    public void EmpezarNuevoJuego()
    {
        // Ya no se borra TODO: se conservan récords, volumen y el selector de stages
        ProgresoJuego.ReiniciarProgresoPartida();
        SceneManager.LoadScene(ProgresoJuego.ESCENA_STAGE1);
    }

    public void ContinuarPartida()
    {
        if (ProgresoJuego.HayProgreso)
        {
            SceneManager.LoadScene(ProgresoJuego.EscenaParaContinuar());
            return;
        }

        // Compatibilidad con el guardado anterior (SavePoint / SaveSystem)
        if (PlayerPrefs.GetInt("HayPartida", 0) == 1)
        {
            PlayerPrefs.SetInt("CargarDesdeGuardado", 1);
            PlayerPrefs.Save();
            string escenaGuardada = PlayerPrefs.GetString("EscenaGuardada", ProgresoJuego.ESCENA_STAGE1);
            SceneManager.LoadScene(escenaGuardada);
        }
    }

    // ===================== SELECTOR DE STAGES =====================
    public void AbrirSelectorStages()
    {
        if (!ProgresoJuego.JuegoCompletado || panelSeleccionStage == null) return;
        panelSeleccionStage.SetActive(true);

        for (int i = 0; i < 3; i++)
        {
            int numero = i + 1;
            string escena = ProgresoJuego.EscenaDeStage(numero);
            bool disponible = ProgresoJuego.StageDisponible(numero) && Application.CanStreamedLevelBeLoaded(escena);
            if (botonesStage != null && i < botonesStage.Length && botonesStage[i] != null)
                botonesStage[i].interactable = disponible;

            if (textosInfoStage != null && i < textosInfoStage.Length && textosInfoStage[i] != null)
            {
                int record = ProgresoJuego.Record(escena);
                string rango = ProgresoJuego.MejorRango(escena);
                textosInfoStage[i].text = record > 0 ? ("RÉCORD " + record.ToString("N0") + "   RANGO " + rango) : "SIN RÉCORD";
            }
        }
    }

    public void CerrarSelectorStages()
    {
        if (panelSeleccionStage != null) panelSeleccionStage.SetActive(false);
    }

    public void JugarStage1() => JugarStage(1);
    public void JugarStage2() => JugarStage(2);
    public void JugarStage3() => JugarStage(3);

    public void JugarStage(int numero)
    {
        if (!ProgresoJuego.StageDisponible(numero)) return;
        SceneManager.LoadScene(ProgresoJuego.EscenaDeStage(numero));
    }

    public void Creditos()
    {
        SceneManager.LoadScene(ProgresoJuego.ESCENA_CREDITOS);
    }

    public void SalirDelJuego()
    {
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
