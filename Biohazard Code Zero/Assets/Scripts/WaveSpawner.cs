using System.Collections;
using UnityEngine;
using TMPro;

public class WaveSpawner : MonoBehaviour
{
    [Header("Configuración de Enemigos")]
    public GameObject lycanPrefab;
    public Transform[] puntosDeSpawn;
    [Range(1, 20)]
    public int maxEnemigosEnEscena = 6; // Tope máximo de zombies vivos al mismo tiempo

    [Header("Progresión de Oleadas")]
    public int enemigosInicialesPorOleada = 5;
    public int incrementoPorOleada = 3;
    public float tiempoEntreSpawns = 1.5f;
    public float tiempoEntreOleadas = 4f;
    public float distanciaMinimaAlJugador = 4f;

    [Header("UI Opcional")]
    public TextMeshProUGUI textoOleada;

    private int oleadaActual = 0;
    private Transform playerTransform;

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;

        StartCoroutine(CicloDeOleadas());
    }

    IEnumerator CicloDeOleadas()
    {
        yield return new WaitForSeconds(2f);

        while (true)
        {
            oleadaActual++;
            ActualizarTextoOleada();

            int cantidadEnEstaOleada = enemigosInicialesPorOleada + (oleadaActual - 1) * incrementoPorOleada;
            float intervaloActual = Mathf.Max(0.6f, tiempoEntreSpawns - (oleadaActual * 0.08f));

            for (int i = 0; i < cantidadEnEstaOleada; i++)
            {
                // Se frena en seco si ya hay 6 (o el límite que pongas) vivos en la escena
                while (ContarEnemigosVivos() >= maxEnemigosEnEscena)
                {
                    yield return new WaitForSeconds(0.5f);
                }

                SpawnearLycan();
                yield return new WaitForSeconds(intervaloActual);
            }

            // También espera a que limpies la oleada (o queden pocos) antes de anunciar la siguiente
            while (ContarEnemigosVivos() > 0)
            {
                yield return new WaitForSeconds(0.5f);
            }

            yield return new WaitForSeconds(tiempoEntreOleadas);
        }
    }

    int ContarEnemigosVivos()
    {
        // Cuenta por script EnemyHealth (más seguro que depender solo del Tag)
        return FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None).Length;
    }

    void SpawnearLycan()
    {
        if (lycanPrefab == null || puntosDeSpawn.Length == 0) return;

        Transform puntoElegido = ElegirPuntoSeguro();
        GameObject nuevoLycan = Instantiate(lycanPrefab, puntoElegido.position, Quaternion.identity);
        nuevoLycan.tag = "Enemy";
    }

    Transform ElegirPuntoSeguro()
    {
        for (int intento = 0; intento < 10; intento++)
        {
            Transform candidato = puntosDeSpawn[Random.Range(0, puntosDeSpawn.Length)];
            if (playerTransform == null || Vector2.Distance(candidato.position, playerTransform.position) >= distanciaMinimaAlJugador)
            {
                return candidato;
            }
        }
        return puntosDeSpawn[Random.Range(0, puntosDeSpawn.Length)];
    }

    void ActualizarTextoOleada()
    {
        if (textoOleada != null)
            textoOleada.text = "OLEADA " + oleadaActual;
    }
}