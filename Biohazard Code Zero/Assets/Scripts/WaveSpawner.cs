using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WaveSpawner : MonoBehaviour
{
    [System.Serializable]
    public class TipoEnemigoOleada
    {
        public string nombre = "Corredor";
        public GameObject prefab;
        [Tooltip("Oleada a partir de la cual empieza a aparecer")]
        [Min(1)] public int oleadaMinima = 2;
        [Tooltip("Peso relativo al aparecer (el Lycan base pesa 1)")]
        public float peso = 0.35f;
        [Tooltip("Peso que se suma por cada oleada después de la mínima (aparición progresiva)")]
        public float pesoExtraPorOleada = 0.15f;
    }

    [Header("Configuración de Enemigos")]
    public GameObject lycanPrefab;
    public Transform[] puntosDeSpawn;
    [Range(1, 20)]
    public int maxEnemigosEnEscena = 6; // Tope máximo de zombies vivos al mismo tiempo

    [Header("Tipos adicionales de infectados (progresivos)")]
    public List<TipoEnemigoOleada> tiposExtra = new List<TipoEnemigoOleada>();

    [Header("Progresión de Oleadas")]
    public int enemigosInicialesPorOleada = 5;
    public int incrementoPorOleada = 3;
    public float tiempoEntreSpawns = 1.5f;
    public float tiempoEntreOleadas = 4f;
    public float distanciaMinimaAlJugador = 4f;

    [Header("Dificultad progresiva por oleada")]
    [Tooltip("Velocidad extra de los enemigos por cada oleada (0.06 = +6% por oleada)")]
    public float velocidadExtraPorOleada = 0.06f;
    [Tooltip("Vida extra de los enemigos por cada oleada (0.08 = +8% por oleada)")]
    public float vidaExtraPorOleada = 0.08f;

    [Header("Tocho garantizado (suelta el arma de premio en Stage 2 y 3)")]
    public GameObject tochoGarantizado;
    [Tooltip("Oleada en la que aparece sí o sí un Tocho (0 = desactivado)")]
    public int oleadaTochoGarantizado = 0;

    [Header("UI Opcional")]
    public TextMeshProUGUI textoOleada;
    [Tooltip("Texto grande centrado que anuncia cada oleada (lo crea Tools > Biohazard)")]
    public TextMeshProUGUI textoAnuncioOleada;

    public int OleadaActual => oleadaActual;

    private int oleadaActual = 0;
    private Transform playerTransform;

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;

        if (textoAnuncioOleada != null) textoAnuncioOleada.gameObject.SetActive(false);

        StartCoroutine(CicloDeOleadas());
    }

    bool PartidaTerminada()
    {
        return MercenariesManager.Instance != null && MercenariesManager.Instance.JuegoTerminado;
    }

    int OleadasTotales()
    {
        return MercenariesManager.Instance != null ? MercenariesManager.Instance.oleadasParaVictoria : 0;
    }

    IEnumerator CicloDeOleadas()
    {
        yield return new WaitForSeconds(2f);

        while (!PartidaTerminada())
        {
            oleadaActual++;
            ActualizarTextoOleada();
            if (MercenariesManager.Instance != null) MercenariesManager.Instance.oleadaActual = oleadaActual;

            float multCantidad = ConfigStage.Instance != null ? ConfigStage.Instance.multiplicadorCantidad : 1f;
            int cantidadEnEstaOleada = Mathf.Max(1, Mathf.RoundToInt((enemigosInicialesPorOleada + (oleadaActual - 1) * incrementoPorOleada) * multCantidad));
            int indiceTocho = (tochoGarantizado != null && oleadaActual == oleadaTochoGarantizado) ? Mathf.Min(2, cantidadEnEstaOleada - 1) : -1;
            float intervaloActual = Mathf.Max(0.6f, tiempoEntreSpawns - (oleadaActual * 0.08f));

            for (int i = 0; i < cantidadEnEstaOleada; i++)
            {
                // Se frena en seco si ya hay el límite de vivos en la escena
                while (ContarEnemigosVivos() >= maxEnemigosEnEscena)
                {
                    if (PartidaTerminada()) yield break;
                    yield return new WaitForSeconds(0.5f);
                }

                if (PartidaTerminada()) yield break;
                SpawnearEnemigo(i == indiceTocho ? tochoGarantizado : null);
                yield return new WaitForSeconds(intervaloActual);
            }

            // Espera a que limpies la oleada antes de anunciar la siguiente
            while (ContarEnemigosVivos() > 0)
            {
                if (PartidaTerminada()) yield break;
                yield return new WaitForSeconds(0.5f);
            }

            // ¿Era la oleada final? -> victoria
            if (MercenariesManager.Instance != null && MercenariesManager.Instance.OleadaCompletada(oleadaActual))
                yield break;

            yield return new WaitForSeconds(tiempoEntreOleadas);
        }
    }

    int ContarEnemigosVivos()
    {
        // Cuenta por script EnemyHealth (más seguro que depender solo del Tag)
        return FindObjectsByType<EnemyHealth>().Length;
    }

    GameObject ElegirPrefab()
    {
        float pesoTotal = lycanPrefab != null ? 1f : 0f;
        if (tiposExtra != null)
        {
            foreach (TipoEnemigoOleada t in tiposExtra)
                if (t != null && t.prefab != null && oleadaActual >= t.oleadaMinima)
                    pesoTotal += Mathf.Max(0f, t.peso + t.pesoExtraPorOleada * (oleadaActual - t.oleadaMinima));
        }

        float tirada = Random.Range(0f, pesoTotal);
        if (lycanPrefab != null)
        {
            if (tirada < 1f) return lycanPrefab;
            tirada -= 1f;
        }

        if (tiposExtra != null)
        {
            foreach (TipoEnemigoOleada t in tiposExtra)
            {
                if (t == null || t.prefab == null || oleadaActual < t.oleadaMinima) continue;
                float p = Mathf.Max(0f, t.peso + t.pesoExtraPorOleada * (oleadaActual - t.oleadaMinima));
                if (tirada < p) return t.prefab;
                tirada -= p;
            }
        }
        return lycanPrefab;
    }

    void SpawnearEnemigo(GameObject forzado = null)
    {
        if (puntosDeSpawn == null || puntosDeSpawn.Length == 0) return;
        GameObject prefab = forzado != null ? forzado : ElegirPrefab();
        if (prefab == null) return;

        Transform puntoElegido = ElegirPuntoSeguro();
        if (puntoElegido == null) return;
        GameObject nuevo = Instantiate(prefab, puntoElegido.position, Quaternion.identity);
        nuevo.tag = "Enemy";

        // Escalado de dificultad: base del Stage x progresión por oleada
        float multVel = (ConfigStage.Instance != null ? ConfigStage.Instance.multiplicadorVelocidad : 1f) * (1f + velocidadExtraPorOleada * (oleadaActual - 1));
        float multVida = (ConfigStage.Instance != null ? ConfigStage.Instance.multiplicadorVida : 1f) * (1f + vidaExtraPorOleada * (oleadaActual - 1));
        ZombieEnemy z = nuevo.GetComponent<ZombieEnemy>();
        if (z != null) z.speed *= multVel;
        EnemyHealth h = nuevo.GetComponent<EnemyHealth>();
        if (h != null) h.vidaMaxima *= multVida; // Se aplica antes de Start()
    }

    Transform ElegirPuntoSeguro()
    {
        for (int intento = 0; intento < 12; intento++)
        {
            Transform candidato = puntosDeSpawn[Random.Range(0, puntosDeSpawn.Length)];
            if (candidato == null) continue;
            if (playerTransform == null || Vector2.Distance(candidato.position, playerTransform.position) >= distanciaMinimaAlJugador)
            {
                return candidato;
            }
        }
        return puntosDeSpawn[Random.Range(0, puntosDeSpawn.Length)];
    }

    void ActualizarTextoOleada()
    {
        int total = OleadasTotales();
        string texto = total > 0 ? ("OLEADA " + oleadaActual + " / " + total) : ("OLEADA " + oleadaActual);
        if (total > 0 && oleadaActual == total) texto = "OLEADA FINAL";

        if (textoOleada != null)
            textoOleada.text = texto;

        MostrarAnuncio(texto);
    }

    /// <summary>Muestra un texto grande temporal en pantalla (oleadas, armas obtenidas...).</summary>
    public void MostrarAnuncio(string texto)
    {
        if (textoAnuncioOleada == null) return;
        StopCoroutine(nameof(RutinaAnuncio));
        StartCoroutine(nameof(RutinaAnuncio), texto);
    }

    IEnumerator RutinaAnuncio(string texto)
    {
        textoAnuncioOleada.text = texto;
        textoAnuncioOleada.gameObject.SetActive(true);
        Color c = textoAnuncioOleada.color;
        float dur = 2.2f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float alfa = t < 0.25f ? t / 0.25f : (t > dur - 0.6f ? (dur - t) / 0.6f : 1f);
            c.a = Mathf.Clamp01(alfa);
            textoAnuncioOleada.color = c;
            yield return null;
        }
        textoAnuncioOleada.gameObject.SetActive(false);
    }
}
