using System.Collections.Generic;
using UnityEngine;

/// <summary>Granada en vuelo + explosión. Se crea por código desde LanzadorGranadas.</summary>
public class Granada : MonoBehaviour
{
    private Vector2 origen, destino;
    private float tVuelo, tDetonacion, radio, daño, knockback, edad;
    private SpriteRenderer sr;
    private Sprite spriteExplosion;
    private Vector3 escalaBase;

    public static Granada Crear(Vector2 origen, Vector2 destino, float tDetonacion, float radio, float daño, float knockback, Sprite sprite, Sprite spriteExplosion)
    {
        GameObject go = new GameObject("Granada");
        go.transform.position = origen;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 7;
        float tam = sprite != null ? sprite.bounds.size.x : 1f;
        go.transform.localScale = Vector3.one * (0.42f / Mathf.Max(0.01f, tam));

        Granada g = go.AddComponent<Granada>();
        g.sr = sr;
        g.origen = origen;
        g.destino = destino;
        g.tDetonacion = tDetonacion;
        g.tVuelo = Mathf.Clamp(Vector2.Distance(origen, destino) / 14f, 0.15f, Mathf.Min(0.6f, tDetonacion * 0.6f));
        g.radio = radio;
        g.daño = daño;
        g.knockback = knockback;
        g.spriteExplosion = spriteExplosion;
        g.escalaBase = go.transform.localScale;
        return g;
    }

    void Update()
    {
        edad += Time.deltaTime;

        // Vuelo con "arco" (crece a mitad de trayecto) y giro
        float t = Mathf.Clamp01(edad / tVuelo);
        transform.position = Vector2.Lerp(origen, destino, 1f - (1f - t) * (1f - t));
        float arco = Mathf.Sin(t * Mathf.PI) * 0.45f;
        transform.localScale = escalaBase * (1f + arco);
        if (t < 1f) transform.Rotate(0f, 0f, 720f * Time.deltaTime);

        // Parpadeo rojo antes de detonar
        float restante = tDetonacion - edad;
        if (sr != null && restante < 0.6f)
            sr.color = (Mathf.Sin(edad * 40f) > 0f) ? new Color(1f, 0.3f, 0.2f, 1f) : Color.white;

        if (edad >= tDetonacion) Explotar();
    }

    void Explotar()
    {
        Vector2 centro = transform.position;
        HashSet<EnemyHealth> golpeados = new HashSet<EnemyHealth>();
        foreach (Collider2D c in Physics2D.OverlapCircleAll(centro, radio))
        {
            EnemyHealth e = c.GetComponent<EnemyHealth>();
            if (e == null) e = c.GetComponentInParent<EnemyHealth>();
            if (e == null || golpeados.Contains(e)) continue;
            golpeados.Add(e);

            Vector2 dir = (Vector2)e.transform.position - centro;
            float factor = Mathf.Lerp(1f, 0.5f, Mathf.Clamp01(dir.magnitude / radio));
            if (dir.sqrMagnitude < 0.0001f) dir = Random.insideUnitCircle;
            e.RecibirDaño(daño * factor, dir.normalized, knockback * factor);
        }

        if (CameraFollow.Instance != null) CameraFollow.Instance.Shake(0.4f, 0.75f);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayExplosion();

        ExplosionVisual.Crear(centro, radio, spriteExplosion);
        Destroy(gameObject);
    }
}
