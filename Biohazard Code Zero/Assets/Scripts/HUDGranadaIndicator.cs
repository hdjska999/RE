using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Indicador de la granada: arco radial + segundos restantes del cooldown de 30 s.</summary>
public class HUDGranadaIndicator : MonoBehaviour
{
    public Image rellenoCooldown;
    public TextMeshProUGUI textoCentro;
    public Color colorListo = new Color(1f, 0.6f, 0.15f, 1f);
    public Color colorRecargando = new Color(0.35f, 0.35f, 0.35f, 0.9f);

    private LanzadorGranadas lanzador;
    private float destello;

    void Awake()
    {
        lanzador = FindAnyObjectByType<LanzadorGranadas>();
    }

    void OnEnable()
    {
        if (lanzador != null) lanzador.OnGranadaLista += AlEstarLista;
    }

    void OnDisable()
    {
        if (lanzador != null) lanzador.OnGranadaLista -= AlEstarLista;
    }

    void AlEstarLista()
    {
        destello = 1f;
    }

    void Update()
    {
        if (lanzador == null) return;
        bool lista = lanzador.Lista;

        if (rellenoCooldown != null)
        {
            rellenoCooldown.fillAmount = lanzador.ProgresoCooldown;
            rellenoCooldown.color = lista ? colorListo : colorRecargando;
        }

        if (textoCentro != null)
        {
            textoCentro.text = lista ? "G" : Mathf.CeilToInt(lanzador.SegundosRestantes).ToString();
            textoCentro.color = lista ? Color.white : new Color(1f, 1f, 1f, 0.7f);
        }

        destello = Mathf.MoveTowards(destello, 0f, Time.deltaTime * 3f);
        transform.localScale = Vector3.one * (1f + destello * 0.25f);
    }
}
