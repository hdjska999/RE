using UnityEngine;

/// <summary>
/// Aura luminosa pulsante para objetos tirados en el suelo (armas): halo con gradiente circular
/// detrás del sprite, anillo interior, flotación leve y una luz 2D tenue para destacar en pisos oscuros.
/// Se puede añadir a cualquier pickup (PickupArma lo agrega solo).
/// </summary>
public class AuraPickup : MonoBehaviour
{
    [Header("Halo")]
    [Tooltip("Sprite con gradiente circular suave (FX_Aura)")]
    public Sprite spriteAura;
    public Color colorAura = new Color(1f, 0.78f, 0.25f, 0.75f);   // dorado RE4
    public Color colorAnillo = new Color(0.35f, 1f, 0.75f, 0.6f);  // verde/cian RE4
    [Tooltip("Diámetro del halo en unidades de mundo")]
    public float diametro = 2.2f;
    public int ordenDetras = 3;

    [Header("Animación")]
    public float velocidadPulso = 3.2f;
    public float amplitudEscala = 0.12f;
    public float alfaMinimo = 0.45f;
    [Tooltip("Altura de la flotación (0 = sin flotar)")]
    public float flotacion = 0.1f;
    public float velocidadFlotacion = 2.6f;
    [Tooltip("Transform que flota (vacío = este objeto)")]
    public Transform objetivoFlotante;

    [Header("Luz 2D (URP)")]
    public bool crearLuz = true;
    public Color colorLuz = new Color(1f, 0.82f, 0.4f, 1f);
    public float intensidadLuz = 1.1f;
    public float radioLuz = 2.4f;

    private SpriteRenderer halo, anillo;
    private Component luz;
    private Vector3 posBaseFlotante;
    private float fase;

    void Start()
    {
        fase = Random.Range(0f, 10f);
        if (objetivoFlotante == null) objetivoFlotante = transform;
        posBaseFlotante = objetivoFlotante.localPosition;

        if (spriteAura != null)
        {
            halo = CrearCapa("Aura_Halo", colorAura, ordenDetras, 1f);
            anillo = CrearCapa("Aura_Anillo", colorAnillo, ordenDetras + 1, 0.55f);
        }
        if (crearLuz) luz = CrearLuz2D();
    }

    SpriteRenderer CrearCapa(string nombre, Color color, int orden, float escalaRelativa)
    {
        Transform existente = transform.Find(nombre);
        GameObject go = existente != null ? existente.gameObject : new GameObject(nombre);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spriteAura;
        sr.color = color;
        sr.sortingOrder = orden;
        float tamSprite = Mathf.Max(0.01f, spriteAura.bounds.size.x);
        Vector3 escalaPadre = transform.lossyScale;
        float k = diametro * escalaRelativa / tamSprite;
        go.transform.localScale = new Vector3(k / Mathf.Max(0.01f, escalaPadre.x), k / Mathf.Max(0.01f, escalaPadre.y), 1f);
        return sr;
    }

    Component CrearLuz2D()
    {
        GameObject go = new GameObject("Aura_Luz");
        go.transform.SetParent(transform, false);
        Component l = Luz2DUtil.CrearPuntual(go, colorLuz, intensidadLuz, radioLuz);
        if (l == null) Destroy(go);
        return l;
    }

    void Update()
    {
        fase += Time.deltaTime;
        float p = (Mathf.Sin(fase * velocidadPulso) + 1f) * 0.5f;   // 0..1

        if (halo != null)
        {
            halo.transform.localScale = EscalaBase(1f) * (1f + amplitudEscala * p);
            Color c = colorAura; c.a = colorAura.a * Mathf.Lerp(alfaMinimo, 1f, p); halo.color = c;
        }
        if (anillo != null)
        {
            float q = (Mathf.Sin(fase * velocidadPulso * 1.6f + 1.3f) + 1f) * 0.5f;
            anillo.transform.localScale = EscalaBase(0.55f) * (1f + amplitudEscala * 1.5f * q);
            Color c = colorAnillo; c.a = colorAnillo.a * Mathf.Lerp(alfaMinimo, 1f, q); anillo.color = c;
        }
        if (luz != null) Luz2DUtil.Establecer(luz, "intensity", intensidadLuz * Mathf.Lerp(0.6f, 1f, p));
        if (flotacion > 0f && objetivoFlotante != null)
        {
            objetivoFlotante.localPosition = posBaseFlotante + Vector3.up * Mathf.Sin(fase * velocidadFlotacion) * flotacion;
        }
    }

    Vector3 EscalaBase(float relativa)
    {
        if (spriteAura == null) return Vector3.one;
        float tamSprite = Mathf.Max(0.01f, spriteAura.bounds.size.x);
        Vector3 escalaPadre = transform.lossyScale;
        float k = diametro * relativa / tamSprite;
        return new Vector3(k / Mathf.Max(0.01f, escalaPadre.x), k / Mathf.Max(0.01f, escalaPadre.y), 1f);
    }
}
