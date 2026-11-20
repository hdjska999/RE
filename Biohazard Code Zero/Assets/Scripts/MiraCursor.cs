using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mira personalizada que sigue al mouse. Se abre al disparar, muestra un arco de progreso
/// al recargar y se oculta (devolviendo el cursor normal) en pausa y Game Over.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class MiraCursor : MonoBehaviour
{
    public Image reticula;
    public Image arcoRecarga;
    public Color colorNormal = new Color(1f, 1f, 1f, 0.9f);
    public Color colorRecarga = new Color(1f, 0.82f, 0.25f, 0.95f);
    public Color colorDesarmado = new Color(1f, 1f, 1f, 0.35f);
    [Tooltip("Cuánto se abre la mira al disparar (0.35 = 35%)")]
    public float aperturaDisparo = 0.35f;

    private RectTransform rt;
    private RectTransform padre;
    private Canvas canvas;
    private CanvasGroup grupo;
    private PlayerMovement jugador;
    private PlayerHealth salud;
    private float pulso;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        padre = rt.parent as RectTransform;
        canvas = GetComponentInParent<Canvas>();
        grupo = GetComponent<CanvasGroup>();
        if (grupo == null) grupo = gameObject.AddComponent<CanvasGroup>();
        grupo.blocksRaycasts = false;
        grupo.interactable = false;
        jugador = FindAnyObjectByType<PlayerMovement>();
        salud = FindAnyObjectByType<PlayerHealth>();
    }

    void OnEnable()
    {
        if (jugador != null) jugador.OnDisparo += AlDisparar;
    }

    void OnDisable()
    {
        if (jugador != null) jugador.OnDisparo -= AlDisparar;
        Cursor.visible = true;
    }

    void AlDisparar()
    {
        pulso = 1f;
    }

    bool DebeVerse()
    {
        if (Time.timeScale <= 0f) return false;
        if (MercenariesManager.Instance != null && MercenariesManager.Instance.JuegoTerminado) return false;
        if (salud != null && salud.EstaMuerto) return false;
        return Application.isFocused;
    }

    void LateUpdate()
    {
        bool visible = DebeVerse();
        grupo.alpha = visible ? 1f : 0f;
        Cursor.visible = !visible;
        if (!visible) return;

        // Seguir al puntero
        if (padre != null)
        {
            Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(padre, Input.mousePosition, cam, out Vector2 local))
                rt.localPosition = new Vector3(local.x, local.y, 0f);
        }
        rt.SetAsLastSibling();

        // Reacción al disparo
        pulso = Mathf.MoveTowards(pulso, 0f, Time.unscaledDeltaTime * 6f);
        rt.localScale = Vector3.one * (1f + pulso * aperturaDisparo);

        bool armado = jugador != null && jugador.TienePistola;
        bool recargando = jugador != null && jugador.EstaRecargando;

        if (reticula != null)
        {
            reticula.color = !armado ? colorDesarmado : (recargando ? colorRecarga : colorNormal);
            reticula.rectTransform.localRotation = recargando ? Quaternion.Euler(0f, 0f, -Time.unscaledTime * 220f) : Quaternion.identity;
        }

        if (arcoRecarga != null)
        {
            arcoRecarga.enabled = recargando;
            if (recargando) arcoRecarga.fillAmount = jugador.ProgresoRecarga;
        }
    }
}
