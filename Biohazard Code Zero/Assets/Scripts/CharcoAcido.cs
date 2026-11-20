using UnityEngine;

/// <summary>Charco de ácido temporal que daña a Bryan mientras lo pisa. Se crea por código.</summary>
public class CharcoAcido : MonoBehaviour
{
    private SpriteRenderer sr;
    private float duracion;
    private float edad;
    private int daño;
    private float intervalo;
    private float proximoTick;
    private bool jugadorDentro;
    private PlayerHealth salud;
    private float radio;

    public static CharcoAcido Crear(Vector3 pos, Sprite sprite, float radio, float duracion, int daño, float intervalo)
    {
        GameObject go = new GameObject("CharcoAcido");
        go.transform.position = new Vector3(pos.x, pos.y, 0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(0.45f, 1f, 0.25f, 0.55f);
        sr.sortingOrder = -1; // bajo los personajes, sobre el suelo
        if (sprite != null)
        {
            float tam = sprite.bounds.size.x;
            go.transform.localScale = new Vector3(radio * 2.2f / tam, radio * 1.6f / tam, 1f);
        }

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = sprite != null ? sprite.bounds.extents.x * 0.85f : radio;

        CharcoAcido c = go.AddComponent<CharcoAcido>();
        c.sr = sr;
        c.duracion = duracion;
        c.daño = daño;
        c.intervalo = intervalo;
        c.radio = radio;
        return c;
    }

    void Update()
    {
        edad += Time.deltaTime;

        if (sr != null)
        {
            float aparicion = Mathf.Clamp01(edad / 0.25f);
            float desvanecer = Mathf.Clamp01((duracion - edad) / 0.8f);
            Color c = sr.color;
            c.a = 0.55f * aparicion * desvanecer * (0.85f + 0.15f * Mathf.Sin(edad * 9f));
            sr.color = c;
        }

        if (jugadorDentro && salud != null && Time.time >= proximoTick)
        {
            proximoTick = Time.time + intervalo;
            salud.RecibirDaño(daño);
        }

        if (edad >= duracion) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        salud = other.GetComponent<PlayerHealth>();
        jugadorDentro = true;
        proximoTick = Mathf.Max(proximoTick, Time.time + 0.15f);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) jugadorDentro = false;
    }
}
