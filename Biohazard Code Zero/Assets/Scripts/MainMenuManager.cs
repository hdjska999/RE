using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("Referencias UI")]
    public Button continueButton;
    public TextMeshProUGUI continueText;

    void Start()
    {
        bool tienePartidaGuardada = PlayerPrefs.GetInt("HayPartida", 0) == 1;

        if (continueButton != null)
        {
            continueButton.interactable = tienePartidaGuardada;

            if (!tienePartidaGuardada && continueText != null)
            {
                Color c = continueText.color;
                c.a = 0.25f;
                continueText.color = c;
            }
        }
    }

    public void EmpezarNuevoJuego()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.SetInt("CargarDesdeGuardado", 0);
        PlayerPrefs.Save();

        SceneManager.LoadScene("SampleScene");
    }

    public void ContinuarPartida()
    {
        if (PlayerPrefs.GetInt("HayPartida", 0) == 1)
        {
            PlayerPrefs.SetInt("CargarDesdeGuardado", 1);
            PlayerPrefs.Save();

            string escenaGuardada = PlayerPrefs.GetString("EscenaGuardada", "SampleScene");
            SceneManager.LoadScene(escenaGuardada);
        }
    }

    public void Creditos()
    {
        SceneManager.LoadScene("CreditsScene");
    }

    public void SalirDelJuego()
    {
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}