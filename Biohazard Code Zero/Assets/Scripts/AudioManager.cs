using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Canales de Audio")]
    public AudioSource canalMusica;
    public AudioSource canalSFX;
    public AudioSource canalUI;

    [Header("Música de Fondo")]
    [Tooltip("Respaldo si falta la música de menú o de combate")]
    public AudioClip musicaPrincipal;
    [Range(0f, 1f)] public float volumenMusica = 0.75f;

    [Header("Música por escena")]
    [Tooltip("Música del Menú Principal y Créditos (bgm_main_menu)")]
    public AudioClip musicaMenu;
    [Tooltip("Tema de combate de los niveles (bgm_wesker_mercenaries)")]
    public AudioClip musicaCombate;
    [Tooltip("Duración total del fundido entre pistas (segundos)")]
    public float duracionFundido = 1.2f;
    public string[] escenasDeMenu = { "MainMenu", "CreditsScene" };
    [Tooltip("Escenas donde no suena música (por ejemplo, la intro con su propio video)")]
    public string[] escenasSinMusica = { "IntroScene" };

    private Coroutine rutinaFundido;

    [Header("SFX Armas (Player)")]
    public AudioClip sfxDisparo;
    public AudioClip sfxRecarga;
    public AudioClip sfxSinBalas;
    public AudioClip sfxDisparoSubfusil;
    public AudioClip sfxDisparoEscopeta;
    public AudioClip sfxCambioArma;

    [Header("SFX Esquive (Dash)")]
    public AudioClip sfxDash;
    public AudioClip sfxDashListo;

    [Header("SFX Infectados y Combate")]
    public AudioClip sfxImpactoBala;
    public AudioClip sfxDañoZombie;
    public AudioClip sfxMuerteZombie;
    public AudioClip sfxDañoBryan;
    [Tooltip("Gruñido corto que avisa que el zombie va a atacar")]
    public AudioClip sfxZombieAviso;

    [Header("SFX Drops y Pickups")]
    public AudioClip sfxItemMunicion;
    public AudioClip sfxItemCuracion;
    public AudioClip sfxItemTiempo;

    [Header("SFX Paquete 2 (granada, ácido, armas)")]
    public AudioClip sfxExplosion;
    public AudioClip sfxLanzarGranada;
    public AudioClip sfxEscupitajo;
    public AudioClip sfxAcido;
    public AudioClip sfxPickupArma;

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
            AplicarVolumenesGuardados();
            SceneManager.sceneLoaded += AlCargarEscena;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= AlCargarEscena;
            Instance = null;
        }
    }

    void Start()
    {
        if (Instance == this) ReproducirMusicaDeEscena(SceneManager.GetActiveScene().name);
    }

    void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        if (Instance == this && modo == LoadSceneMode.Single) ReproducirMusicaDeEscena(escena.name);
    }

    /// <summary>Elige la pista según la escena: menú, combate o silencio.</summary>
    public void ReproducirMusicaDeEscena(string nombreEscena)
    {
        if (System.Array.IndexOf(escenasSinMusica, nombreEscena) >= 0) { CambiarMusica(null); return; }
        bool esMenu = System.Array.IndexOf(escenasDeMenu, nombreEscena) >= 0;
        AudioClip clip = esMenu ? (musicaMenu != null ? musicaMenu : musicaPrincipal)
                                : (musicaCombate != null ? musicaCombate : musicaPrincipal);
        CambiarMusica(clip);
    }

    /// <summary>Cambia de pista con fundido (tiempo no escalado: funciona en pausa y Game Over). Si ya suena, no la reinicia.</summary>
    public void CambiarMusica(AudioClip clip)
    {
        if (canalMusica == null) return;
        if (clip != null && canalMusica.clip == clip && canalMusica.isPlaying) return;
        if (rutinaFundido != null) StopCoroutine(rutinaFundido);
        rutinaFundido = StartCoroutine(Fundido(clip));
    }

    IEnumerator Fundido(AudioClip nuevo)
    {
        float mitad = Mathf.Max(0.01f, duracionFundido * 0.5f);
        if (canalMusica.isPlaying && canalMusica.clip != null)
        {
            float inicio = canalMusica.volume;
            for (float t = 0f; t < mitad; t += Time.unscaledDeltaTime)
            {
                canalMusica.volume = Mathf.Lerp(inicio, 0f, t / mitad);
                yield return null;
            }
        }
        canalMusica.Stop();
        canalMusica.volume = 0f;
        if (nuevo == null) { rutinaFundido = null; yield break; }

        canalMusica.clip = nuevo;
        canalMusica.loop = true;
        canalMusica.Play();
        for (float t = 0f; t < mitad; t += Time.unscaledDeltaTime)
        {
            canalMusica.volume = Mathf.Lerp(0f, volumenMusica, t / mitad);
            yield return null;
        }
        canalMusica.volume = volumenMusica;
        rutinaFundido = null;
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
        if (rutinaFundido != null) { StopCoroutine(rutinaFundido); rutinaFundido = null; }
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
    public void PlayCambioArma() => PlaySFX(sfxCambioArma != null ? sfxCambioArma : sfxRecarga, 0.7f, false);

    public void PlayDisparoArma(TipoArma tipo)
    {
        switch (tipo)
        {
            case TipoArma.Subfusil:
                PlaySFX(sfxDisparoSubfusil != null ? sfxDisparoSubfusil : sfxDisparo, 0.7f, true);
                break;
            case TipoArma.Escopeta:
                PlaySFX(sfxDisparoEscopeta != null ? sfxDisparoEscopeta : sfxDisparo, 1f, true);
                break;
            default:
                PlayDisparo();
                break;
        }
    }

    public void PlayDash() => PlaySFX(sfxDash, 0.8f, true);
    public void PlayDashListo() => PlaySFX(sfxDashListo, 0.45f, false);
    public void PlayZombieAviso() => PlaySFX(sfxZombieAviso != null ? sfxZombieAviso : sfxDañoZombie, 0.75f, true);

    public void PlayImpactoBala() => PlaySFX(sfxImpactoBala, 0.85f, true);
    public void PlayDañoZombie() => PlaySFX(sfxDañoZombie, 0.9f, true);
    public void PlayMuerteZombie() => PlaySFX(sfxMuerteZombie, 1f, true);
    public void PlayDañoBryan() => PlaySFX(sfxDañoBryan, 1f, false);

    public void PlayRecogerMunicion() => PlaySFX(sfxItemMunicion, 0.9f, false);
    public void PlayRecogerCuracion() => PlaySFX(sfxItemCuracion, 0.9f, false);
    public void PlayRecogerTiempo() => PlaySFX(sfxItemTiempo, 1f, false);

    public void PlayExplosion() => PlaySFX(sfxExplosion, 1f, true);
    public void PlayLanzarGranada() => PlaySFX(sfxLanzarGranada != null ? sfxLanzarGranada : sfxDash, 0.7f, true);
    public void PlayEscupitajo() => PlaySFX(sfxEscupitajo, 0.8f, true);
    public void PlayAcido() => PlaySFX(sfxAcido, 0.6f, true);
    public void PlayPickupArma() => PlaySFX(sfxPickupArma != null ? sfxPickupArma : sfxItemTiempo, 1f, false);

    // ==================== VOLUMEN (Opciones del menú de pausa) ====================
    public const string K_VOL_GENERAL = "BCZ_VolGeneral";
    public const string K_VOL_MUSICA = "BCZ_VolMusica";
    public const string K_VOL_SFX = "BCZ_VolSFX";

    public float VolumenGeneral => AudioListener.volume;
    public float VolumenMusica => canalMusica != null ? canalMusica.volume : volumenMusica;
    public float VolumenSFX => canalSFX != null ? canalSFX.volume : 1f;

    public void AplicarVolumenesGuardados()
    {
        AudioListener.volume = PlayerPrefs.GetFloat(K_VOL_GENERAL, 1f);
        volumenMusica = PlayerPrefs.GetFloat(K_VOL_MUSICA, volumenMusica);
        if (canalMusica != null) canalMusica.volume = volumenMusica;
        float sfx = PlayerPrefs.GetFloat(K_VOL_SFX, 1f);
        if (canalSFX != null) canalSFX.volume = sfx;
        if (canalUI != null) canalUI.volume = sfx;
    }

    public void SetVolumenGeneral(float v) { AudioListener.volume = Mathf.Clamp01(v); PlayerPrefs.SetFloat(K_VOL_GENERAL, AudioListener.volume); }
    public void SetVolumenMusica(float v) { volumenMusica = Mathf.Clamp01(v); if (canalMusica != null && rutinaFundido == null) canalMusica.volume = volumenMusica; PlayerPrefs.SetFloat(K_VOL_MUSICA, volumenMusica); }
    public void SetVolumenSFX(float v)
    {
        v = Mathf.Clamp01(v);
        if (canalSFX != null) canalSFX.volume = v;
        if (canalUI != null) canalUI.volume = v;
        PlayerPrefs.SetFloat(K_VOL_SFX, v);
    }

    public void PlayHover() => PlayUI(sfxBotonHover, 0.6f);
    public void PlayClick() => PlayUI(sfxBotonClick, 0.8f);
}
