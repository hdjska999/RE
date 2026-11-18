using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Canales de Audio")]
    public AudioSource canalMusica;
    public AudioSource canalSFX;
    public AudioSource canalUI;

    [Header("Música de Fondo")]
    public AudioClip musicaPrincipal;
    [Range(0f, 1f)] public float volumenMusica = 0.75f;

    [Header("SFX Armas (Player)")]
    public AudioClip sfxDisparo;
    public AudioClip sfxRecarga;
    public AudioClip sfxSinBalas;

    [Header("SFX Infectados y Combate")]
    public AudioClip sfxImpactoBala;
    public AudioClip sfxDañoZombie;
    public AudioClip sfxMuerteZombie;
    public AudioClip sfxDañoBryan;

    [Header("SFX Drops y Pickups")]
    public AudioClip sfxItemMunicion;
    public AudioClip sfxItemCuracion;
    public AudioClip sfxItemTiempo;

    [Header("SFX Interfaz (UI)")]
    public AudioClip sfxBotonHover;
    public AudioClip sfxBotonClick;

    [Header("Ajustes de Variación")]
    public bool modularPitch = true;
    public float rangoPitch = 0.05f; // Entre 0.95f y 1.05f

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ConfigurarCanales();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (musicaPrincipal != null && canalMusica != null && !canalMusica.isPlaying)
        {
            ReproducirMusica(musicaPrincipal);
        }
    }

    void ConfigurarCanales()
    {
        if (canalMusica == null)
        {
            canalMusica = gameObject.AddComponent<AudioSource>();
            canalMusica.loop = true;
            canalMusica.playOnAwake = false;
            canalMusica.volume = volumenMusica;
        }

        if (canalSFX == null)
        {
            canalSFX = gameObject.AddComponent<AudioSource>();
            canalSFX.playOnAwake = false;
        }

        if (canalUI == null)
        {
            canalUI = gameObject.AddComponent<AudioSource>();
            canalUI.playOnAwake = false;
            canalUI.ignoreListenerPause = true; // Suena incluso en Pausa
        }
    }

    // ==================== MÚSICA ====================
    public void ReproducirMusica(AudioClip clip, bool loop = true)
    {
        if (clip == null || canalMusica == null) return;
        canalMusica.clip = clip;
        canalMusica.loop = loop;
        canalMusica.volume = volumenMusica;
        canalMusica.Play();
    }

    public void DetenerMusica()
    {
        if (canalMusica != null) canalMusica.Stop();
    }

    // ==================== SFX BASE ====================
    public void PlaySFX(AudioClip clip, float volumen = 1f, bool variarPitch = false)
    {
        if (clip == null || canalSFX == null) return;

        if (variarPitch && modularPitch)
        {
            canalSFX.pitch = Random.Range(1f - rangoPitch, 1f + rangoPitch);
        }
        else
        {
            canalSFX.pitch = 1f;
        }

        canalSFX.PlayOneShot(clip, volumen);
    }

    public void PlayUI(AudioClip clip, float volumen = 1f)
    {
        if (clip == null || canalUI == null) return;
        canalUI.PlayOneShot(clip, volumen);
    }

    // ==================== ACCESOS DIRECTOS ====================
    public void PlayDisparo() => PlaySFX(sfxDisparo, 1f, true);
    public void PlayRecarga() => PlaySFX(sfxRecarga, 0.9f, false);
    public void PlaySinBalas() => PlaySFX(sfxSinBalas, 0.8f, true);

    public void PlayImpactoBala() => PlaySFX(sfxImpactoBala, 0.85f, true);
    public void PlayDañoZombie() => PlaySFX(sfxDañoZombie, 0.9f, true);
    public void PlayMuerteZombie() => PlaySFX(sfxMuerteZombie, 1f, true);
    public void PlayDañoBryan() => PlaySFX(sfxDañoBryan, 1f, false);

    public void PlayRecogerMunicion() => PlaySFX(sfxItemMunicion, 0.9f, false);
    public void PlayRecogerCuracion() => PlaySFX(sfxItemCuracion, 0.9f, false);
    public void PlayRecogerTiempo() => PlaySFX(sfxItemTiempo, 1f, false);

    public void PlayHover() => PlayUI(sfxBotonHover, 0.6f);
    public void PlayClick() => PlayUI(sfxBotonClick, 0.8f);
}
