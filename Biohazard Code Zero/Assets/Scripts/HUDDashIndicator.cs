using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Indicador del cooldown del esquive (Dash). Va en un objeto del HUD siempre visible.
/// El relleno radial se llena mientras se recarga y destella cuando vuelve a estar listo.
/// </summary>
public class HUDDashIndicator : MonoBehaviour
{
    public Image rellenoCooldown;
    public Image fondo;
    public TextMeshProUGUI etiqueta;
    public Color colorListo = new Color(0.45f, 0.9f, 1f, 1f);
    public Color colorRecargando = new Color(0.35f, 0.35f, 0.35f, 0.9f);

    private PlayerMovement jugador;
    private float destello = 0f;

    void Awake()
    {
        jugador = FindAnyObjectByType<PlayerMovement>();
    }

    void OnEnable()
    {
        if (jugador != null) jugador.OnDashListo += AlEstarListo;
    }

    void OnDisable()
    {
        if (jugador != null) jugador.OnDashListo -= AlEstarListo;
    }

    void AlEstarListo()
    {
        destello = 1f;
    }

    void Update()
    {
        if (jugador == null) return;

        float progreso = jugador.ProgresoCooldownDash;
        bool listo = jugador.DashListo;

        if (rellenoCooldown != null)
        {
            rellenoCooldown.fillAmount = progreso;
            rellenoCooldown.color = listo ? colorListo : colorRecargando;
        }

        if (etiqueta != null)
            etiqueta.color = listo ? Color.white : new Color(1f, 1f, 1f, 0.45f);

        destello = Mathf.MoveTowards(destello, 0f, Time.deltaTime * 3f);
        transform.localScale = Vector3.one * (1f + destello * 0.25f);
    }
}
