using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    // Esta función se activará al hacer clic en el botón
    public void EmpezarNuevoJuego()
    {
        SceneManager.LoadScene("IntroScene"); // Debe llamarse exactamente como tu escena
    }
}