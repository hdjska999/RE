using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Guarda todo el estado actual de Bryan en la escena
    public void GuardarPartida(GameObject player)
    {
        PlayerHealth salud = player.GetComponent<PlayerHealth>();
        PlayerMovement mov = player.GetComponent<PlayerMovement>();
        Animator anim = player.GetComponent<Animator>();

        PlayerPrefs.SetInt("HayPartida", 1);
        PlayerPrefs.SetString("EscenaGuardada", SceneManager.GetActiveScene().name);

        // 1. Guardar Vida (de PlayerHealth.cs)
        if (salud != null)
        {
            PlayerPrefs.SetInt("VidaActual", salud.vidaActual);
        }

        // 2. Guardar Munición (de PlayerMovement.cs)
        if (mov != null)
        {
            PlayerPrefs.SetInt("MunicionActual", mov.municionActual);
            PlayerPrefs.SetInt("MunicionTotal", mov.municionTotal);
        }

        // 3. Guardar si ya equipó la pistola (del Animator)
        if (anim != null)
        {
            bool tienePistola = anim.GetBool("tienePistola");
            PlayerPrefs.SetInt("TienePistola", tienePistola ? 1 : 0);
        }

        // 4. Guardar Posición exacta de Bryan
        PlayerPrefs.SetFloat("PosX", player.transform.position.x);
        PlayerPrefs.SetFloat("PosY", player.transform.position.y);

        PlayerPrefs.Save();
        Debug.Log("¡Partida guardada! Vida: " + salud.vidaActual + " | Balas: " + mov.municionActual + "/" + mov.municionTotal);
    }

    public bool DebeCargarDatos()
    {
        return PlayerPrefs.GetInt("CargarDesdeGuardado", 0) == 1;
    }
}