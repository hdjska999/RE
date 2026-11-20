using UnityEngine;

/// <summary>
/// "Fantasma" que se desvanece, usado para la estela del esquive de Bryan.
/// Se crea por código desde PlayerMovement; no hace falta añadirlo a ningún objeto.
/// </summary>
public class EstelaDash : MonoBehaviour
{
    private SpriteRenderer sr;
    private float vida = 0.22f;
    private float edad = 0f;
    private Color colorInicial;

    public static void Crear(SpriteRenderer origen, Transform transformOrigen)
    {
        if (origen == null || origen.sprite == null) return;

        GameObject go = new GameObject("EstelaDash");
        go.transform.SetPositionAndRotation(transformOrigen.position, transformOrigen.rotation);
        go.transform.localScale = transformOrigen.lossyScale;

        SpriteRenderer copia = go.AddComponent<SpriteRenderer>();
        copia.sprite = origen.sprite;
        copia.flipX = origen.flipX;
        copia.flipY = origen.flipY;
        copia.sortingLayerID = origen.sortingLayerID;
        copia.sortingOrder = origen.sortingOrder - 1;
        copia.sharedMaterial = origen.sharedMaterial;
        copia.color = new Color(0.55f, 0.85f, 1f, 0.55f);

        EstelaDash estela = go.AddComponent<EstelaDash>();
        estela.sr = copia;
        estela.colorInicial = copia.color;
    }

    void Update()
    {
        edad += Time.deltaTime;
        float t = Mathf.Clamp01(edad / vida);
        if (sr != null)
        {
            Color c = colorInicial;
            c.a = Mathf.Lerp(colorInicial.a, 0f, t);
            sr.color = c;
        }
        if (t >= 1f) Destroy(gameObject);
    }
}
