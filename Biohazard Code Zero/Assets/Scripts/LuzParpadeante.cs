using UnityEngine;

/// <summary>
/// Hace parpadear una Light2D del mismo objeto: fuego (autos incendiados), farola fallando
/// o torreta de patrulla (alterna rojo y azul).
/// </summary>
public class LuzParpadeante : MonoBehaviour
{
    public enum Modo { Fuego, FarolaFallando, Sirena }

    public Modo modo = Modo.Fuego;
    public float intensidadBase = 1.3f;
    [Range(0f, 1f)] public float variacion = 0.35f;
    public float velocidad = 9f;
    public Color colorA = new Color(1f, 0.45f, 0.1f, 1f);
    public Color colorB = new Color(0.2f, 0.4f, 1f, 1f);

    private Component luz;
    private float semilla;

    void Start()
    {
        semilla = Random.Range(0f, 100f);
        if (Luz2DUtil.TipoLight2D != null) luz = GetComponent(Luz2DUtil.TipoLight2D);
    }

    void Update()
    {
        if (luz == null) return;
        float t = Time.time * velocidad + semilla;
        switch (modo)
        {
            case Modo.Fuego:
                float ruido = Mathf.PerlinNoise(t, semilla) * 0.7f + Mathf.PerlinNoise(t * 2.3f, semilla + 7f) * 0.3f;
                Luz2DUtil.Establecer(luz, "intensity", intensidadBase * (1f - variacion + variacion * 2f * ruido));
                break;

            case Modo.FarolaFallando:
                bool apagon = Mathf.PerlinNoise(t * 0.3f, semilla) > 0.78f && Mathf.Sin(t * 6f) > 0f;
                Luz2DUtil.Establecer(luz, "intensity", apagon ? intensidadBase * 0.15f : intensidadBase);
                break;

            case Modo.Sirena:
                bool fase = Mathf.Repeat(t * 0.25f, 1f) < 0.5f;
                Luz2DUtil.Establecer(luz, "color", fase ? colorA : colorB);
                Luz2DUtil.Establecer(luz, "intensity", intensidadBase * (0.6f + 0.4f * Mathf.Abs(Mathf.Sin(t * 1.5f))));
                break;
        }
    }
}
