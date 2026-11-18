using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject pausePanel;
    public PlayerMovement scriptJugador; // Para evitar que Bryan dispare mientras está en pausa

    private bool estaPausado = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (estaPausado)
            {
                ReanudarJuego();
            }
            else
            {
                PausarJuego();
            }
        }
    }

    public void PausarJuego()
    {
        estaPausado = true;
        pausePanel.SetActive(true);
        
        // Congela el tiempo del juego (zombies, balas y animaciones se detienen)
        Time.timeScale = 0f; 

        if (scriptJugador != null) scriptJugador.enabled = false;
    }

    public void ReanudarJuego()
    {
        estaPausado = false;
        pausePanel.SetActive(false);
        
        // Devuelve el tiempo a la normalidad
        Time.timeScale = 1f; 

        if (scriptJugador != null) scriptJugador.enabled = true;
    }

    public void VolverAlMenuPrincipal()
    {
        // CRÍTICO: Siempre restaurar Time.timeScale a 1 antes de cambiar de escena,
        // de lo contrario el menú o la siguiente partida cargarán congelados.
        Time.timeScale = 1f; 
        SceneManager.LoadScene("MainMenu");
    }
}
