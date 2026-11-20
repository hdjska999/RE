using UnityEngine;

/// <summary>
/// Zombie Ácido / Tóxico: al morir deja un charco de ácido temporal (~4 s)
/// que hace daño periódico a Bryan si lo pisa.
/// </summary>
[RequireComponent(typeof(EnemyHealth))]
public class ZombieToxico : MonoBehaviour
{
    public Sprite spriteCharco;
    public float duracionCharco = 4f;
    public float radioCharco = 1.4f;
    public int dañoPorTick = 6;
    public float intervaloTick = 0.6f;

    private EnemyHealth salud;

    void Awake()
    {
        salud = GetComponent<EnemyHealth>();
    }

    void OnEnable()
    {
        if (salud != null) salud.OnMuerte += AlMorir;
    }

    void OnDisable()
    {
        if (salud != null) salud.OnMuerte -= AlMorir;
    }

    void AlMorir(EnemyHealth e)
    {
        CharcoAcido.Crear(transform.position, spriteCharco, radioCharco, duracionCharco, dañoPorTick, intervaloTick);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayAcido();
    }
}
