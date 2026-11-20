using UnityEngine;

/// <summary>
/// Pickup único de arma (Escopeta en Stage 2, Subfusil en Stage 3).
/// Se crea por código desde ConfigStage al morir el primer Tocho, con aura luminosa pulsante (AuraPickup).
/// </summary>
public class PickupArma : MonoBehaviour
{
    public TipoArma tipo = TipoArma.Escopeta;

    public static PickupArma Crear(Vector3 posicion, TipoArma tipo, Sprite icono, Sprite spriteAura)
    {
        GameObject go = new GameObject("PickupArma_" + tipo);
        go.transform.position = new Vector3(posicion.x, posicion.y, 0f);

        // Ícono del arma (flota dentro del aura)
        GameObject ic = new GameObject("Icono");
        ic.transform.SetParent(go.transform, false);
        SpriteRenderer sr = ic.AddComponent<SpriteRenderer>();
        sr.sprite = icono;
        sr.sortingOrder = 6;
        if (icono != null)
        {
            float ancho = icono.bounds.size.x;
            float escala = ancho > 0.01f ? 1.4f / ancho : 1f;
            ic.transform.localScale = Vector3.one * escala;
        }

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.8f;

        // Aura dorada/verde RE4 + luz tenue para que destaque sobre cualquier piso
        AuraPickup aura = go.AddComponent<AuraPickup>();
        aura.spriteAura = spriteAura;
        aura.objetivoFlotante = ic.transform;
        aura.diametro = 2.4f;
        if (tipo == TipoArma.Subfusil)
        {
            aura.colorAura = new Color(0.3f, 0.85f, 1f, 0.75f); // cian para el subfusil
            aura.colorLuz = new Color(0.45f, 0.85f, 1f, 1f);
        }

        PickupArma p = go.AddComponent<PickupArma>();
        p.tipo = tipo;
        return p;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        PlayerMovement jugador = other.GetComponent<PlayerMovement>();
        if (jugador == null) return;

        jugador.DesbloquearArmaEnPartida(tipo);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPickupArma();

        WaveSpawner spawner = FindAnyObjectByType<WaveSpawner>();
        string nombre = tipo == TipoArma.Escopeta ? "ESCOPETA" : (tipo == TipoArma.Subfusil ? "SUBFUSIL" : "PISTOLA");
        if (spawner != null) spawner.MostrarAnuncio("¡" + nombre + " OBTENIDA!");

        Destroy(gameObject);
    }
}
