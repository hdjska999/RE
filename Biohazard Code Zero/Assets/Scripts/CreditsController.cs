using UnityEngine;
using UnityEngine.SceneManagement;

public class CreditsController : MonoBehaviour
{
    [Header("Scroll Automático")]
    [Tooltip("Desactívalo para que los créditos queden fijos y centrados (lo configura Tools > Biohazard Code Zero)")]
    public bool desplazamientoAutomatico = true;
    public RectTransform contenedorCreditos;
    public float velocidadScroll = 50f;
    public float limiteSuperiorY = 900f; // Altura máxima hasta donde sube el texto

    [Header("Aparición suave")]
    public CanvasGroup grupoCreditos;
    public float duracionFade = 1.2f;

    private float t;

    void Start()
    {
        Time.timeScale = 1f;
        Cursor.visible = true;
        if (grupoCreditos != null) grupoCreditos.alpha = 0f;
    }

    void Update()
    {
        t += Time.deltaTime;
        if (grupoCreditos != null && duracionFade > 0f) grupoCreditos.alpha = Mathf.Clamp01(t / duracionFade);

        // Sube los créditos hasta llegar al límite
        if (desplazamientoAutomatico && contenedorCreditos != null && contenedorCreditos.anchoredPosition.y < limiteSuperiorY)
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
        Time.timeScale = 1f;
        Cursor.visible = true;
        SceneManager.LoadScene("MainMenu");
    }
}
