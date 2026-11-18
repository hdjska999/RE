using UnityEngine;
using UnityEngine.SceneManagement;

public class CreditsController : MonoBehaviour
{
    [Header("Scroll Automático")]
    public RectTransform contenedorCreditos;
    public float velocidadScroll = 50f;
    public float limiteSuperiorY = 900f; // Altura máxima hasta donde sube el texto

    void Update()
    {
        // Sube los créditos hasta llegar al límite
        if (contenedorCreditos != null && contenedorCreditos.anchoredPosition.y < limiteSuperiorY)
        {
            contenedorCreditos.anchoredPosition += Vector2.up * velocidadScroll * Time.deltaTime;
        }

        // Permite salir presionando Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            VolverAlMenu();
        }
    }

    public void VolverAlMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}