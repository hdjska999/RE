using UnityEngine;

/// <summary>Destello de explosión (círculo que se expande y desvanece).</summary>
public class ExplosionVisual : MonoBehaviour
{
    private SpriteRenderer anillo, nucleo;
    private float radio, edad;
    private const float DURACION = 0.45f;

    public static void Crear(Vector2 pos, float radio, Sprite sprite)
    {
        if (sprite == null) return;
        GameObject go = new GameObject("Explosion");
        go.transform.position = pos;
        ExplosionVisual ev = go.AddComponent<ExplosionVisual>();
        ev.radio = radio;
        ev.anillo = NuevaCapa(go.transform, sprite, new Color(1f, 0.55f, 0.1f, 0.85f), 8);
        ev.nucleo = NuevaCapa(go.transform, sprite, new Color(1f, 0.95f, 0.7f, 1f), 9);
    }

    static SpriteRenderer NuevaCapa(Transform padre, Sprite sprite, Color color, int orden)
    {
        GameObject g = new GameObject("Capa");
        g.transform.SetParent(padre, false);
        SpriteRenderer s = g.AddComponent<SpriteRenderer>();
        s.sprite = sprite;
        s.color = color;
        s.sortingOrder = orden;
        return s;
    }

    void Update()
    {
        edad += Time.deltaTime;
        float t = Mathf.Clamp01(edad / DURACION);
        float tamSprite = anillo.sprite.bounds.size.x;
        float diam = radio * 2f / Mathf.Max(0.01f, tamSprite);

        anillo.transform.localScale = Vector3.one * diam * Mathf.Lerp(0.3f, 1.1f, 1f - (1f - t) * (1f - t));
        Color ca = anillo.color; ca.a = 0.85f * (1f - t); anillo.color = ca;

        nucleo.transform.localScale = Vector3.one * diam * Mathf.Lerp(0.5f, 0.2f, t);
        Color cn = nucleo.color; cn.a = 1f - Mathf.Clamp01(t * 2.5f); nucleo.color = cn;

        if (t >= 1f) Destroy(gameObject);
    }
}
