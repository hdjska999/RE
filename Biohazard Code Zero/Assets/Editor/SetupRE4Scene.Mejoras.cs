#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// Paquete de mejoras "Biohazard Code Zero" (menú Tools > Biohazard Code Zero).
/// Extiende SetupRE4Scene (clase parcial) y reutiliza sus helpers de audio, sprites y UI.
/// Todo es idempotente: se puede ejecutar varias veces sin duplicar objetos.
/// </summary>
public static partial class SetupRE4Scene
{
    private const string MENU = "Tools/Biohazard Code Zero/";
    private const string RUTA_STAGE1 = "Assets/Scenes/SampleScene.unity";
    private const string NOMBRE_STAGE2 = "Stage2_Laboratorio";
    private const string RUTA_STAGE2 = "Assets/Scenes/Stage2_Laboratorio.unity";
    private const string RUTA_LYCAN = "Assets/Prefabs/Lycan.prefab";
    private const string RUTA_CORREDOR = "Assets/Prefabs/Lycan_Corredor.prefab";
    private const string RUTA_BRUTO = "Assets/Prefabs/Lycan_Bruto.prefab";
    private const string RUTA_ANIMATOR_ZOMBIE = "Assets/Prefabs/Zombie.controller";
    private const string CAPA_OBSTACULOS = "Obstaculos";
    private const string CARPETA_GEN = "Assets/Sprites/Generados";

    // =====================================================================
    // MENÚ
    // =====================================================================

    [MenuItem(MENU + "★ Aplicar TODAS las mejoras (Stage 1 + Stage 2)", false, 0)]
    public static void MenuAplicarTodo()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        try
        {
            EditorUtility.DisplayProgressBar("Biohazard Code Zero", "Abriendo Stage 1...", 0.05f);
            EditorSceneManager.OpenScene(RUTA_STAGE1, OpenSceneMode.Single);

            EditorUtility.DisplayProgressBar("Biohazard Code Zero", "Generando audio procedural...", 0.15f);
            RegenerarAudioInterno();

            EditorUtility.DisplayProgressBar("Biohazard Code Zero", "Creando enemigos (Animator, Corredor, Bruto)...", 0.35f);
            CrearEnemigosInterno();

            EditorUtility.DisplayProgressBar("Biohazard Code Zero", "Construyendo HUD y resultados...", 0.5f);
            ConstruirHUDInterno();
            ConfigurarGameplayEscena(true);

            EditorUtility.DisplayProgressBar("Biohazard Code Zero", "Ampliando Stage 1...", 0.65f);
            AmpliarStage1Interno();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

            EditorUtility.DisplayProgressBar("Biohazard Code Zero", "Creando Stage 2 - Laboratorio...", 0.8f);
            CrearStage2Interno();

            EditorSceneManager.OpenScene(RUTA_STAGE1, OpenSceneMode.Single);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        string msg = "¡Mejoras aplicadas!\n\n" +
            "• Audio: gruñidos guturales nuevos + sonidos de subfusil, escopeta, dash y aviso de ataque.\n" +
            "• Enemigos: Animator 'Zombie.controller', Lycan_Corredor (oleada 2+) y Lycan_Bruto (oleada 3+).\n" +
            "• HUD: barra de combo (6.5 s), selector de armas 1/2/3, indicador de esquive, anuncio de oleada.\n" +
            "• Resultados: rango S/A/B/C, Game Over siempre al frente, botones Reintentar / Siguiente / Menú.\n" +
            "• Stage 1 ampliado (~50x28) con colisiones en mostrador, columnas y camillas.\n" +
            "• Stage 2 'Stage2_Laboratorio' creado y agregado a Build Settings.\n\n" +
            "Escenas guardadas. Dale Play en SampleScene.";
        EditorUtility.DisplayDialog("Biohazard Code Zero", msg, "¡A jugar!");
        Debug.Log("[Biohazard] " + msg);
    }

    [MenuItem(MENU + "1. Regenerar audio (zombies guturales, armas, dash)", false, 20)]
    public static void MenuAudio()
    {
        RegenerarAudioInterno();
        MarcarEscenaSucia();
        EditorUtility.DisplayDialog("Biohazard Code Zero", "Audio regenerado y vinculado al AudioManager de la escena.", "OK");
    }

    [MenuItem(MENU + "2. Crear o actualizar enemigos (Animator, Corredor, Bruto)", false, 21)]
    public static void MenuEnemigos()
    {
        CrearEnemigosInterno();
        ConfigurarGameplayEscena(SceneManager.GetActiveScene().path != RUTA_STAGE2);
        MarcarEscenaSucia();
        EditorUtility.DisplayDialog("Biohazard Code Zero", "Prefabs listos:\n• Lycan (Animator + capa de obstáculos)\n• Lycan_Corredor\n• Lycan_Bruto\n\nWaveSpawner de la escena actualizado.", "OK");
    }

    [MenuItem(MENU + "3. Construir HUD (combo, armas, esquive, rangos S-A-B-C)", false, 22)]
    public static void MenuHUD()
    {
        ConstruirHUDInterno();
        ConfigurarGameplayEscena(SceneManager.GetActiveScene().path != RUTA_STAGE2);
        MarcarEscenaSucia();
        EditorUtility.DisplayDialog("Biohazard Code Zero", "HUD actualizado en la escena actual. Recuerda guardar (Ctrl+S).", "OK");
    }

    [MenuItem(MENU + "4. Ampliar Stage 1 (límites, obstáculos, spawns)", false, 23)]
    public static void MenuAmpliarStage1()
    {
        if (SceneManager.GetActiveScene().path != RUTA_STAGE1)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(RUTA_STAGE1, OpenSceneMode.Single);
        }
        AmpliarStage1Interno();
        MarcarEscenaSucia();
        EditorUtility.DisplayDialog("Biohazard Code Zero", "Stage 1 ampliado. Revisa la vista Scene y guarda (Ctrl+S).", "OK");
    }

    [MenuItem(MENU + "5. Crear o actualizar Stage 2 - Laboratorio", false, 24)]
    public static void MenuStage2()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        CrearStage2Interno();
        EditorUtility.DisplayDialog("Biohazard Code Zero", "Stage2_Laboratorio listo, guardado y agregado a Build Settings.", "OK");
    }

    private static void MarcarEscenaSucia()
    {
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    // =====================================================================
    // 1. AUDIO
    // =====================================================================

    private static void RegenerarAudioInterno()
    {
        // Base (incluye los nuevos gruñidos de daño / muerte) y vinculación del AudioManager
        GenerarAudioProceduralYVincular();

        int sr = 44100;
        AudioClip aviso = ObtenerOGenerarClip("Assets/Audio/sfx_zombie_aviso.wav", () =>
            CrearWavData(SintetizarGruñidoZombie(sr, 0.5f, 68f, 88f, 0.9f, 450f, 560f, 0.25f, 0.6f, 3), sr), true);
        AudioClip subfusil = ObtenerOGenerarClip("Assets/Audio/sfx_disparo_subfusil.wav", () => CrearWavData(SintetizarDisparoSubfusil(sr), sr), true);
        AudioClip escopeta = ObtenerOGenerarClip("Assets/Audio/sfx_disparo_escopeta.wav", () => CrearWavData(SintetizarDisparoEscopeta(sr), sr), true);
        AudioClip cambio = ObtenerOGenerarClip("Assets/Audio/sfx_cambio_arma.wav", () => CrearWavData(SintetizarCambioArma(sr), sr), true);
        AudioClip dash = ObtenerOGenerarClip("Assets/Audio/sfx_dash.wav", () => CrearWavData(SintetizarDash(sr), sr), true);
        AudioClip dashListo = ObtenerOGenerarClip("Assets/Audio/sfx_dash_listo.wav", () => CrearWavData(SintetizarDashListo(sr), sr), true);

        AudioManager am = Object.FindAnyObjectByType<AudioManager>(FindObjectsInactive.Include);
        if (am != null)
        {
            Undo.RecordObject(am, "Vincular audio extra");
            am.sfxZombieAviso = aviso;
            am.sfxDisparoSubfusil = subfusil;
            am.sfxDisparoEscopeta = escopeta;
            am.sfxCambioArma = cambio;
            am.sfxDash = dash;
            am.sfxDashListo = dashListo;
            EditorUtility.SetDirty(am);
        }
    }

    /// <summary>
    /// Gruñido de infectado: fuente glotal tipo diente de sierra con fase cerrada, duplicación de periodo
    /// (subarmónicos = voz rota/rasposa), jitter, aliento, 3 formantes resonantes y saturación suave.
    /// </summary>
    private static float[] SintetizarGruñidoZombie(int sr, float dur, float f0Ini, float f0Fin, float rasp,
        float f1Ini, float f1Fin, float ataque, float curvaCaida, int semilla)
    {
        System.Random rnd = new System.Random(semilla);
        int n = (int)(sr * dur);
        float[] salida = new float[n];
        float[,] st = new float[3, 4];
        double fase = 0.0;
        int ciclo = 0;
        float ampCiclo = 1f, jit = 0f, jitT = 0f, lp = 0f, lpOut = 0f, lp2 = 0f;
        int pasoJit = Mathf.Max(1, sr / 60);

        for (int i = 0; i < n; i++)
        {
            float t = (float)i / sr;
            float tn = t / dur;

            if (i % pasoJit == 0) jitT = (float)(rnd.NextDouble() * 2.0 - 1.0);
            jit += (jitT - jit) * 0.002f;

            float f0 = (f0Ini + (f0Fin - f0Ini) * Mathf.Pow(tn, 0.8f)) * (1f + 0.05f * jit + 0.015f * Mathf.Sin(2f * Mathf.PI * 5.5f * t));
            fase += f0 / sr;
            if (fase >= 1.0)
            {
                fase -= 1.0;
                ciclo++;
                ampCiclo = (ciclo % 2 == 0 ? 1f : 1f - 0.55f * rasp) * (1f + (float)(rnd.NextDouble() * 0.5 - 0.25) * rasp);
            }

            float ph = (float)fase;
            float g = ph < 0.85f ? (1f - 2f * ph) : (1f - 2f * 0.85f) * (1f - (ph - 0.85f) / 0.15f);
            float src = g * ampCiclo;
            float ruido = (float)(rnd.NextDouble() * 2.0 - 1.0);
            src += ruido * (0.10f + 0.22f * rasp) * (0.5f + 0.5f * Mathf.Abs(g));

            float f1 = f1Ini + (f1Fin - f1Ini) * tn;
            float y = 0f;
            y += Resonar(st, 0, src, f1, 4f, sr) * 1f;
            y += Resonar(st, 1, src, f1 * 2.1f, 5f, sr) * 0.35f;
            y += Resonar(st, 2, src, 2400f, 7f, sr) * 0.08f;

            lp += (src - lp) * 0.04f; // cuerpo grave (~280 Hz)
            y += lp * 0.9f;

            float env = t < ataque ? t / ataque : Mathf.Pow(Mathf.Max(0f, 1f - (t - ataque) / Mathf.Max(0.0001f, dur - ataque)), curvaCaida);
            float trem = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * (28f - 12f * tn) * t);
            float v = y * env * (1f - 0.5f * rasp + 0.5f * rasp * trem);
            v = (float)System.Math.Tanh(v * 1.5f);

            lpOut += (v - lpOut) * 0.22f;
            lp2 += (lpOut - lp2) * 0.35f;
            salida[i] = lp2;
        }

        Normalizar(salida, 0.9f);
        AplicarFades(salida, sr, 0.004f, 0.02f);
        return salida;
    }

    /// <summary>Pasa-banda biquad (RBJ, ganancia de pico 0 dB). st[k] = x1, x2, y1, y2.</summary>
    private static float Resonar(float[,] st, int k, float x, float f, float q, int sr)
    {
        float w0 = 2f * Mathf.PI * Mathf.Min(f, sr * 0.45f) / sr;
        float a = Mathf.Sin(w0) / (2f * q);
        float a0 = 1f + a;
        float b0 = a / a0, b2 = -a / a0, a1 = -2f * Mathf.Cos(w0) / a0, a2 = (1f - a) / a0;
        float y = b0 * x + b2 * st[k, 1] - a1 * st[k, 2] - a2 * st[k, 3];
        st[k, 1] = st[k, 0]; st[k, 0] = x;
        st[k, 3] = st[k, 2]; st[k, 2] = y;
        return y;
    }

    private static void Normalizar(float[] s, float pico)
    {
        float max = 0f;
        for (int i = 0; i < s.Length; i++) max = Mathf.Max(max, Mathf.Abs(s[i]));
        if (max < 1e-5f) return;
        float k = pico / max;
        for (int i = 0; i < s.Length; i++) s[i] *= k;
    }

    private static void AplicarFades(float[] s, int sr, float fadeIn, float fadeOut)
    {
        int nIn = Mathf.Max(1, (int)(sr * fadeIn));
        int nOut = Mathf.Max(1, (int)(sr * fadeOut));
        for (int i = 0; i < s.Length && i < nIn; i++) s[i] *= (float)i / nIn;
        for (int i = 0; i < nOut && i < s.Length; i++) s[s.Length - 1 - i] *= (float)i / nOut;
    }

    private static float[] SintetizarDisparoSubfusil(int sr)
    {
        System.Random rnd = new System.Random(11);
        float dur = 0.13f;
        int n = (int)(sr * dur);
        float[] s = new float[n];
        float prev = 0f, lpf = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / sr;
            float ruido = (float)(rnd.NextDouble() * 2.0 - 1.0);
            float snap = (ruido - prev) * Mathf.Exp(-t * 260f) * 1.6f;
            prev = ruido;
            float blast = ruido * Mathf.Exp(-t * 55f);
            lpf += (ruido - lpf) * 0.18f;
            float cuerpo = lpf * Mathf.Exp(-t * 45f) * 1.8f;
            float sub = Mathf.Sin(2f * Mathf.PI * 95f * t) * Mathf.Exp(-t * 50f) * 0.5f;
            float v = snap * 0.8f + blast * 0.9f + cuerpo + sub;
            s[i] = (float)System.Math.Tanh(v * 2f);
        }
        Normalizar(s, 0.85f);
        AplicarFades(s, sr, 0.0005f, 0.015f);
        return s;
    }

    private static float[] SintetizarDisparoEscopeta(int sr)
    {
        System.Random rnd = new System.Random(12);
        float dur = 0.6f;
        int n = (int)(sr * dur);
        float[] s = new float[n];
        float prev = 0f, lpf = 0f, lpf2 = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / sr;
            float ruido = (float)(rnd.NextDouble() * 2.0 - 1.0);
            float snap = (ruido - prev) * Mathf.Exp(-t * 120f) * 1.4f;
            prev = ruido;
            float blast = ruido * Mathf.Exp(-t * 14f);
            lpf += (ruido - lpf) * 0.08f;
            lpf2 += (lpf - lpf2) * 0.08f;
            float rumble = lpf2 * Mathf.Exp(-t * 7f) * 6f;
            float sub = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(55f, 38f, t / dur) * t) * Mathf.Exp(-t * 10f) * 1.1f;
            float v = snap * 0.7f + blast * 0.8f + rumble + sub;
            s[i] = (float)System.Math.Tanh(v * 2.4f);
        }
        Normalizar(s, 0.95f);
        AplicarFades(s, sr, 0.0005f, 0.05f);
        return s;
    }

    private static float[] SintetizarCambioArma(int sr)
    {
        System.Random rnd = new System.Random(13);
        float dur = 0.2f;
        int n = (int)(sr * dur);
        float[] s = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / sr;
            float v = 0f;
            float ruido = (float)(rnd.NextDouble() * 2.0 - 1.0);
            if (t < 0.06f) v += (Mathf.Sin(2f * Mathf.PI * 2600f * t) * 0.6f + ruido * 0.5f) * Mathf.Exp(-t * 90f);
            if (t >= 0.09f) { float d = t - 0.09f; v += (Mathf.Sin(2f * Mathf.PI * 1400f * d) * 0.7f + ruido * 0.45f) * Mathf.Exp(-d * 70f); }
            s[i] = v;
        }
        Normalizar(s, 0.7f);
        return s;
    }

    private static float[] SintetizarDash(int sr)
    {
        System.Random rnd = new System.Random(14);
        float dur = 0.24f;
        int n = (int)(sr * dur);
        float[] s = new float[n];
        float[,] st = new float[1, 4];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / sr;
            float tn = t / dur;
            float ruido = (float)(rnd.NextDouble() * 2.0 - 1.0);
            float fc = tn < 0.4f ? Mathf.Lerp(500f, 2300f, tn / 0.4f) : Mathf.Lerp(2300f, 700f, (tn - 0.4f) / 0.6f);
            float v = Resonar(st, 0, ruido, fc, 1.6f, sr);
            s[i] = v * Mathf.Sin(Mathf.PI * tn);
        }
        Normalizar(s, 0.7f);
        return s;
    }

    private static float[] SintetizarDashListo(int sr)
    {
        float dur = 0.16f;
        int n = (int)(sr * dur);
        float[] s = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / sr;
            s[i] = (Mathf.Sin(2f * Mathf.PI * 1320f * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * 1980f * t) * 0.35f) * Mathf.Exp(-t * 20f);
        }
        Normalizar(s, 0.5f);
        AplicarFades(s, sr, 0.002f, 0.01f);
        return s;
    }

    // =====================================================================
    // 2. ENEMIGOS
    // =====================================================================

    private static void CrearEnemigosInterno()
    {
        int capa = AsegurarCapa(CAPA_OBSTACULOS);
        LayerMask mascara = 1 << capa;
        AnimatorController ctrl = CrearAnimatorZombie();

        // Lycan base
        GameObject raiz = PrefabUtility.LoadPrefabContents(RUTA_LYCAN);
        ConfigurarZombieBase(raiz, ctrl, mascara);
        PrefabUtility.SaveAsPrefabAsset(raiz, RUTA_LYCAN);
        PrefabUtility.UnloadPrefabContents(raiz);

        // Variante 1: Infectado Corredor (rápido, poca vida)
        CrearOActualizarVariante(RUTA_CORREDOR, "Lycan_Corredor", go =>
        {
            go.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = new Color(1f, 0.62f, 0.55f, 1f);

            ZombieEnemy z = go.GetComponent<ZombieEnemy>();
            if (z != null) { z.speed = 3.7f; z.frecuenciaPaso = 15f; z.intensidadPaso = 0.06f; z.velocidadGiro = 12f; z.capaObstaculos = mascara; }

            EnemyHealth h = go.GetComponent<EnemyHealth>();
            if (h != null) { h.vidaMaxima = 2f; h.multiplicadorPuntos = 1.3f; h.resistenciaKnockback = 1.4f; h.probabilidadMunicion = 70f; }

            EnemyAttack a = go.GetComponent<EnemyAttack>();
            if (a != null) { a.dañoAtaque = 12; a.tiempoAviso = 0.32f; a.tiempoEntreAtaques = 1.1f; a.tiempoRecuperacion = 0.25f; }
        });

        // Variante 2: Bruto (lento, mucha vida, golpe fuerte)
        CrearOActualizarVariante(RUTA_BRUTO, "Lycan_Bruto", go =>
        {
            go.transform.localScale = new Vector3(2.15f, 2.15f, 1f);
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = new Color(0.62f, 0.78f, 0.55f, 1f);

            Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
            if (rb != null) rb.mass = 6f;

            ZombieEnemy z = go.GetComponent<ZombieEnemy>();
            if (z != null) { z.speed = 1.35f; z.frecuenciaPaso = 6f; z.intensidadPaso = 0.035f; z.radioCuerpo = 0.55f; z.capaObstaculos = mascara; }

            EnemyHealth h = go.GetComponent<EnemyHealth>();
            if (h != null) { h.vidaMaxima = 12f; h.multiplicadorPuntos = 2.5f; h.resistenciaKnockback = 0.25f; h.probabilidadMunicion = 100f; }

            EnemyAttack a = go.GetComponent<EnemyAttack>();
            if (a != null)
            {
                a.dañoAtaque = 35; a.tiempoAviso = 0.7f; a.tiempoEntreAtaques = 2f; a.tiempoRecuperacion = 0.55f;
                a.distanciaInicioAtaque = 0.45f; a.alcanceGolpe = 0.95f; a.distanciaEstocada = 0.5f; a.escalaAviso = 0.2f;
            }
        });

        AssetDatabase.SaveAssets();
    }

    private static void ConfigurarZombieBase(GameObject go, AnimatorController ctrl, LayerMask mascara)
    {
        Animator anim = go.GetComponent<Animator>();
        if (anim == null) anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = ctrl;
        anim.applyRootMotion = false;
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        ZombieEnemy z = go.GetComponent<ZombieEnemy>();
        if (z != null) z.capaObstaculos = mascara;

        if (go.GetComponent<EnemyAttack>() == null) go.AddComponent<EnemyAttack>();

        Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
        if (rb != null) rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private static void CrearOActualizarVariante(string ruta, string nombre, System.Action<GameObject> configurar)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(ruta) == null)
        {
            GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RUTA_LYCAN);
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            inst.name = nombre;
            configurar(inst);
            PrefabUtility.SaveAsPrefabAsset(inst, ruta); // Instancia de prefab => se guarda como Prefab Variant
            Object.DestroyImmediate(inst);
        }
        else
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
            configurar(raiz);
            PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
            PrefabUtility.UnloadPrefabContents(raiz);
        }
    }

    private static AnimatorController CrearAnimatorZombie()
    {
        AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(RUTA_ANIMATOR_ZOMBIE);
        bool nuevo = ctrl == null;
        if (nuevo) ctrl = AnimatorController.CreateAnimatorControllerAtPath(RUTA_ANIMATOR_ZOMBIE);

        AsegurarParametro(ctrl, "Velocidad", AnimatorControllerParameterType.Float);
        AsegurarParametro(ctrl, "Caminando", AnimatorControllerParameterType.Bool);
        AsegurarParametro(ctrl, "Atacar", AnimatorControllerParameterType.Trigger);
        AsegurarParametro(ctrl, "Aturdido", AnimatorControllerParameterType.Bool);

        if (nuevo)
        {
            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
            AnimatorState idle = sm.AddState("Idle", new Vector3(300, 0, 0));
            AnimatorState caminar = sm.AddState("Caminar", new Vector3(300, 120, 0));
            AnimatorState atacar = sm.AddState("Atacar", new Vector3(560, 60, 0));
            AnimatorState aturdido = sm.AddState("Aturdido", new Vector3(560, 180, 0));
            sm.defaultState = idle;

            AnimatorStateTransition t;
            t = idle.AddTransition(caminar); t.hasExitTime = false; t.duration = 0.05f; t.AddCondition(AnimatorConditionMode.If, 0, "Caminando");
            t = caminar.AddTransition(idle); t.hasExitTime = false; t.duration = 0.05f; t.AddCondition(AnimatorConditionMode.IfNot, 0, "Caminando");
            t = sm.AddAnyStateTransition(atacar); t.hasExitTime = false; t.duration = 0.02f; t.canTransitionToSelf = false; t.AddCondition(AnimatorConditionMode.If, 0, "Atacar");
            t = atacar.AddTransition(idle); t.hasExitTime = true; t.exitTime = 0.9f; t.duration = 0.05f;
            t = sm.AddAnyStateTransition(aturdido); t.hasExitTime = false; t.duration = 0.02f; t.canTransitionToSelf = false; t.AddCondition(AnimatorConditionMode.If, 0, "Aturdido");
            t = aturdido.AddTransition(idle); t.hasExitTime = false; t.duration = 0.05f; t.AddCondition(AnimatorConditionMode.IfNot, 0, "Aturdido");
        }

        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();
        return ctrl;
    }

    private static void AsegurarParametro(AnimatorController ctrl, string nombre, AnimatorControllerParameterType tipo)
    {
        foreach (AnimatorControllerParameter p in ctrl.parameters)
            if (p.name == nombre) return;
        ctrl.AddParameter(nombre, tipo);
    }

    private static int AsegurarCapa(string nombre)
    {
        int existente = LayerMask.NameToLayer(nombre);
        if (existente >= 0) return existente;

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0) return 0;
        SerializedObject tm = new SerializedObject(assets[0]);
        SerializedProperty capas = tm.FindProperty("layers");
        for (int i = 8; i < capas.arraySize; i++)
        {
            SerializedProperty sp = capas.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(sp.stringValue))
            {
                sp.stringValue = nombre;
                tm.ApplyModifiedProperties();
                return i;
            }
        }
        Debug.LogWarning("[Biohazard] No hay capas libres; se usará Default para obstáculos.");
        return 0;
    }

    // =====================================================================
    // 3. HUD
    // =====================================================================

    private static void ConstruirHUDInterno()
    {
        Canvas canvas = ObtenerOConfigurarCanvas();
        TMP_FontAsset fuente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RUTA_FUENTE_CINZEL);
        Sprite blanco = SpriteGenerado("UI_Blanco", 100f, false, () => TexturaSolida(8, 8, Color.white));
        Sprite circulo = SpriteGenerado("UI_Circulo", 100f, false, () => TexturaCirculo(128, Color.white, 0.04f));
        Sprite anillo = GenerarSpriteAnilloHueco();

        // El Canvas está en Screen Space - Camera: con Sorting Order 0 los sprites pueden taparlo.
        Undo.RecordObject(canvas, "Canvas por encima de sprites");
        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay && canvas.sortingOrder < 100) canvas.sortingOrder = 100;

        // Estructura base (solo si falta)
        if (canvas.transform.Find("ContenedorArmaUI") == null || canvas.transform.Find("ContenedorArmaUI/BarraVidaRadial") == null)
            ConfigurarHUDRadial(canvas, anillo);
        if (canvas.transform.Find("ViseraSuperiorRE4") == null)
            ConfigurarViseraSuperiorRE4(canvas, GenerarSpriteVisera(), GenerarSpriteCalavera(), GenerarSpriteReloj(), GenerarSpriteEstrella());

        MercenariesManager merc = Object.FindAnyObjectByType<MercenariesManager>(FindObjectsInactive.Include);
        WaveSpawner spawner = Object.FindAnyObjectByType<WaveSpawner>(FindObjectsInactive.Include);
        PlayerMovement jugador = Object.FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Include);

        ConstruirBarraCombo(canvas, merc, fuente, blanco);
        ConstruirTextosOleada(canvas, spawner, fuente);
        ConstruirSelectorArmas(canvas, jugador, fuente, blanco);
        ConstruirIndicadorDash(canvas, fuente, circulo, anillo);
        ConstruirPanelResultados(canvas, merc, fuente, blanco);
    }

    private static void ConstruirBarraCombo(Canvas canvas, MercenariesManager merc, TMP_FontAsset fuente, Sprite blanco)
    {
        Transform cont = canvas.transform.Find("ViseraSuperiorRE4/SeccionTiempo/Contenedor_Combo");
        if (cont == null) return;

        RectTransform rc = (RectTransform)cont;
        Undo.RecordObject(rc, "Combo");
        rc.anchorMin = rc.anchorMax = new Vector2(0.5f, 0.5f);
        rc.pivot = new Vector2(0.5f, 0.5f);
        rc.anchoredPosition = new Vector2(0f, -66f);
        rc.sizeDelta = new Vector2(260f, 46f);

        Transform tCombo = cont.Find("Combo");
        if (tCombo != null)
        {
            TextMeshProUGUI tmp = tCombo.GetComponent<TextMeshProUGUI>();
            Undo.RecordObject(tmp, "Combo texto");
            Undo.RecordObject(tmp.rectTransform, "Combo texto");
            tmp.rectTransform.anchorMin = tmp.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            tmp.rectTransform.anchoredPosition = new Vector2(0f, 10f);
            tmp.rectTransform.sizeDelta = new Vector2(260f, 28f);
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.85f, 0.2f, 1f);
            if (fuente != null) tmp.font = fuente;
        }

        // Riel (fondo oscuro) + barra de relleno dentro
        GameObject riel = ObtenerOCrearUI(cont, "RielCombo", typeof(Image));
        Colocar(riel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -12f), new Vector2(224f, 12f));
        Image imgRiel = riel.GetComponent<Image>();
        imgRiel.sprite = blanco;
        imgRiel.color = new Color(0f, 0f, 0f, 0.65f);
        imgRiel.raycastTarget = false;

        Transform tBarra = riel.transform.Find("BarraCombo");
        Transform tSuelta = cont.Find("BarraCombo");
        if (tBarra == null && tSuelta != null)
        {
            Undo.SetTransformParent(tSuelta, riel.transform, "Mover BarraCombo al riel");
            tBarra = tSuelta;
        }
        else if (tBarra != null && tSuelta != null)
        {
            Undo.DestroyObjectImmediate(tSuelta.gameObject); // duplicado creado por el setup base
        }
        GameObject barra = tBarra != null ? tBarra.gameObject : ObtenerOCrearUI(riel.transform, "BarraCombo", typeof(Image));
        if (!barra.activeSelf) { Undo.RecordObject(barra, "Activar barra"); barra.SetActive(true); }

        RectTransform rb = barra.GetComponent<RectTransform>();
        Undo.RecordObject(rb, "Barra combo");
        rb.anchorMin = Vector2.zero;
        rb.anchorMax = Vector2.one;
        rb.pivot = new Vector2(0.5f, 0.5f);
        rb.offsetMin = new Vector2(2f, 2f);
        rb.offsetMax = new Vector2(-2f, -2f);

        Image img = barra.GetComponent<Image>();
        Undo.RecordObject(img, "Barra combo");
        img.sprite = blanco; // Sin sprite, una Image 'Filled' no se rellena: por eso antes no se veía
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = (int)Image.OriginHorizontal.Left;
        img.fillAmount = 1f;
        img.color = new Color(1f, 0.85f, 0.2f, 1f);
        img.raycastTarget = false;

        if (merc != null)
        {
            Undo.RecordObject(merc, "Vincular combo");
            merc.barraCombo = img;
            merc.contenedorBarraCombo = riel;
            if (cont.Find("Combo") != null) merc.textoCombo = cont.Find("Combo").GetComponent<TextMeshProUGUI>();
            EditorUtility.SetDirty(merc);
        }
    }

    private static void ConstruirTextosOleada(Canvas canvas, WaveSpawner spawner, TMP_FontAsset fuente)
    {
        // Indicador permanente "OLEADA 1 / 4" bajo la visera
        Transform tOleada = canvas.transform.Find("Oleada");
        TextMeshProUGUI oleada = null;
        if (tOleada != null)
        {
            Undo.RecordObject(tOleada.gameObject, "Oleada");
            tOleada.gameObject.SetActive(true);
            oleada = tOleada.GetComponent<TextMeshProUGUI>();
            ConfigurarTMP(oleada, fuente, "OLEADA 1 / 4", 20, new Color(0.85f, 0.85f, 0.85f, 0.9f), TextAlignmentOptions.Center);
            Colocar(tOleada.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -122f), new Vector2(420f, 30f));
        }

        // Anuncio grande al iniciar cada oleada
        GameObject anuncioGo = ObtenerOCrearUI(canvas.transform, "AnuncioOleada", typeof(TextMeshProUGUI));
        TextMeshProUGUI anuncio = anuncioGo.GetComponent<TextMeshProUGUI>();
        ConfigurarTMP(anuncio, fuente, "OLEADA 1", 86, new Color(0.85f, 0.08f, 0.06f, 1f), TextAlignmentOptions.Center);
        Colocar(anuncioGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 190f), new Vector2(1400f, 130f));
        anuncioGo.SetActive(false);

        if (spawner != null)
        {
            Undo.RecordObject(spawner, "Vincular textos oleada");
            if (oleada != null) spawner.textoOleada = oleada;
            spawner.textoAnuncioOleada = anuncio;
            EditorUtility.SetDirty(spawner);
        }
    }

    private static void ConstruirSelectorArmas(Canvas canvas, PlayerMovement jugador, TMP_FontAsset fuente, Sprite blanco)
    {
        Transform cont = canvas.transform.Find("ContenedorArmaUI");
        if (cont == null) return;
        HUDMercenariesController hud = cont.GetComponent<HUDMercenariesController>();

        // Íconos: pistola = el que ya tenías; subfusil y escopeta generados
        Sprite iconoPistola = (hud != null && hud.iconosArmas != null && hud.iconosArmas.Length > 0 && hud.iconosArmas[0] != null) ? hud.iconosArmas[0]
            : (hud != null && hud.iconoArma != null && hud.iconoArma.sprite != null ? hud.iconoArma.sprite : null);
        if (iconoPistola == null) iconoPistola = SpriteGenerado("Icono_Pistola", 100f, false, TexturaIconoPistola);
        Sprite iconoSubfusil = SpriteGenerado("Icono_Subfusil", 100f, false, TexturaIconoSubfusil);
        Sprite iconoEscopeta = SpriteGenerado("Icono_Escopeta", 100f, false, TexturaIconoEscopeta);

        // Nombre del arma
        GameObject nombreGo = ObtenerOCrearUI(cont, "TextoNombreArma", typeof(TextMeshProUGUI));
        TextMeshProUGUI nombre = nombreGo.GetComponent<TextMeshProUGUI>();
        ConfigurarTMP(nombre, fuente, "PISTOLA", 22, new Color(1f, 0.85f, 0.35f, 1f), TextAlignmentOptions.Right);
        Colocar(nombreGo, new Vector2(1f, 0f), new Vector2(1f, 0.5f), new Vector2(-140f, 108f), new Vector2(220f, 28f));

        // Selector 1 / 2 / 3
        GameObject selector = ObtenerOCrearUI(cont, "SelectorArmas");
        Colocar(selector, new Vector2(1f, 0f), new Vector2(1f, 0.5f), new Vector2(-140f, 142f), new Vector2(176f, 32f));
        Image[] slots = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject slot = ObtenerOCrearUI(selector.transform, "Slot" + (i + 1), typeof(Image));
            Colocar(slot, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(28f + i * 60f, 0f), new Vector2(52f, 30f));
            Image img = slot.GetComponent<Image>();
            img.sprite = blanco;
            img.color = i == 0 ? new Color(1f, 0.82f, 0.25f, 0.95f) : new Color(0.1f, 0.1f, 0.1f, 0.6f);
            img.raycastTarget = false;
            slots[i] = img;

            GameObject num = ObtenerOCrearUI(slot.transform, "Num", typeof(TextMeshProUGUI));
            ConfigurarTMP(num.GetComponent<TextMeshProUGUI>(), fuente, (i + 1).ToString(), 20, Color.white, TextAlignmentOptions.Center);
            Colocar(num, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(52f, 30f));
        }

        if (hud != null)
        {
            Undo.RecordObject(hud, "Configurar selector de armas");
            hud.iconosArmas = new Sprite[] { iconoPistola, iconoSubfusil, iconoEscopeta };
            hud.slotsArmas = slots;
            hud.textoNombreArma = nombre;
            EditorUtility.SetDirty(hud);
        }

        if (jugador != null)
        {
            Undo.RecordObject(jugador, "Íconos de armas");
            if (jugador.armas == null || jugador.armas.Length < 3)
                jugador.armas = new ArmaConfig[] { ArmaConfig.CrearPistola(), ArmaConfig.CrearSubfusil(), ArmaConfig.CrearEscopeta() };
            jugador.armas[0].icono = iconoPistola;
            jugador.armas[1].icono = iconoSubfusil;
            jugador.armas[2].icono = iconoEscopeta;
            EditorUtility.SetDirty(jugador);
        }
    }

    private static void ConstruirIndicadorDash(Canvas canvas, TMP_FontAsset fuente, Sprite circulo, Sprite anillo)
    {
        GameObject raiz = ObtenerOCrearUI(canvas.transform, "IndicadorDash", typeof(HUDDashIndicator));
        Colocar(raiz, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(110f, 120f), new Vector2(90f, 90f));

        GameObject fondo = ObtenerOCrearUI(raiz.transform, "Fondo", typeof(Image));
        Colocar(fondo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90f, 90f));
        Image imgFondo = fondo.GetComponent<Image>();
        imgFondo.sprite = circulo;
        imgFondo.color = new Color(0f, 0f, 0f, 0.6f);
        imgFondo.raycastTarget = false;

        GameObject relleno = ObtenerOCrearUI(raiz.transform, "Relleno", typeof(Image));
        Colocar(relleno, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90f, 90f));
        Image imgRelleno = relleno.GetComponent<Image>();
        imgRelleno.sprite = anillo;
        imgRelleno.type = Image.Type.Filled;
        imgRelleno.fillMethod = Image.FillMethod.Radial360;
        imgRelleno.fillOrigin = (int)Image.Origin360.Top;
        imgRelleno.fillClockwise = true;
        imgRelleno.fillAmount = 1f;
        imgRelleno.color = new Color(0.45f, 0.9f, 1f, 1f);
        imgRelleno.raycastTarget = false;

        GameObject icono = ObtenerOCrearUI(raiz.transform, "Icono", typeof(TextMeshProUGUI));
        ConfigurarTMP(icono.GetComponent<TextMeshProUGUI>(), fuente, "DASH", 20, Color.white, TextAlignmentOptions.Center);
        Colocar(icono, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90f, 30f));

        GameObject etiqueta = ObtenerOCrearUI(raiz.transform, "Etiqueta", typeof(TextMeshProUGUI));
        TextMeshProUGUI tmpEtiqueta = etiqueta.GetComponent<TextMeshProUGUI>();
        ConfigurarTMP(tmpEtiqueta, fuente, "ESPACIO / CLIC DER.", 14, new Color(0.85f, 0.85f, 0.85f, 1f), TextAlignmentOptions.Center);
        Colocar(etiqueta, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(220f, 22f));

        HUDDashIndicator ind = raiz.GetComponent<HUDDashIndicator>();
        Undo.RecordObject(ind, "Indicador dash");
        ind.fondo = imgFondo;
        ind.rellenoCooldown = imgRelleno;
        ind.etiqueta = icono.GetComponent<TextMeshProUGUI>();
        EditorUtility.SetDirty(ind);
    }

    private static void ConstruirPanelResultados(Canvas canvas, MercenariesManager merc, TMP_FontAsset fuente, Sprite blanco)
    {
        GameObject panel = merc != null && merc.panelGameOver != null ? merc.panelGameOver : null;
        if (panel == null)
        {
            Transform t = canvas.transform.Find("PanelGameOver");
            panel = t != null ? t.gameObject : ObtenerOCrearUI(canvas.transform, "PanelGameOver", typeof(Image));
        }

        // Pantalla completa, opaca y siempre encima de todo
        RectTransform rp = panel.GetComponent<RectTransform>();
        Undo.RecordObject(rp, "Panel resultados");
        rp.anchorMin = Vector2.zero;
        rp.anchorMax = Vector2.one;
        rp.pivot = new Vector2(0.5f, 0.5f);
        rp.offsetMin = Vector2.zero;
        rp.offsetMax = Vector2.zero;
        rp.localScale = Vector3.one;

        Image fondo = panel.GetComponent<Image>();
        if (fondo == null) fondo = Undo.AddComponent<Image>(panel);
        Undo.RecordObject(fondo, "Fondo resultados");
        fondo.sprite = blanco;
        fondo.color = new Color(0.03f, 0f, 0f, 0.94f);
        fondo.raycastTarget = true; // bloquea clics al juego

        Canvas sub = panel.GetComponent<Canvas>();
        if (sub == null) sub = Undo.AddComponent<Canvas>(panel);
        Undo.RecordObject(sub, "Canvas Game Over");
        sub.overrideSorting = true;
        SortingLayer[] capas = SortingLayer.layers;
        if (capas.Length > 0) sub.sortingLayerID = capas[capas.Length - 1].id;
        sub.sortingOrder = 5000;
        if (panel.GetComponent<GraphicRaycaster>() == null) Undo.AddComponent<GraphicRaycaster>(panel);

        Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Game Over al final");
        panel.transform.SetAsLastSibling();

        // Imagen decorativa existente (si la hay) detrás de los textos
        Transform imgVieja = panel.transform.Find("Image");
        if (imgVieja != null) imgVieja.SetAsFirstSibling();

        // Título (reutiliza el texto "GAME OVER" existente)
        Transform tTitulo = panel.transform.Find("TituloResultado");
        if (tTitulo == null)
        {
            Transform viejo = panel.transform.Find("Text (TMP)");
            if (viejo != null) { Undo.RecordObject(viejo.gameObject, "Renombrar título"); viejo.gameObject.name = "TituloResultado"; tTitulo = viejo; }
        }
        GameObject titulo = tTitulo != null ? tTitulo.gameObject : ObtenerOCrearUI(panel.transform, "TituloResultado", typeof(TextMeshProUGUI));
        TextMeshProUGUI tmpTitulo = titulo.GetComponent<TextMeshProUGUI>();
        ConfigurarTMP(tmpTitulo, fuente, "GAME OVER", 76, new Color(0.9f, 0.15f, 0.12f, 1f), TextAlignmentOptions.Center);
        Colocar(titulo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 340f), new Vector2(1500f, 110f));

        GameObject etiqueta = ObtenerOCrearUI(panel.transform, "EtiquetaRango", typeof(TextMeshProUGUI));
        ConfigurarTMP(etiqueta.GetComponent<TextMeshProUGUI>(), fuente, "RANGO", 30, new Color(0.75f, 0.75f, 0.75f, 1f), TextAlignmentOptions.Center);
        Colocar(etiqueta, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 235f), new Vector2(400f, 40f));

        GameObject rango = ObtenerOCrearUI(panel.transform, "Rango", typeof(TextMeshProUGUI));
        TextMeshProUGUI tmpRango = rango.GetComponent<TextMeshProUGUI>();
        ConfigurarTMP(tmpRango, fuente, "S", 230, new Color(1f, 0.82f, 0.2f, 1f), TextAlignmentOptions.Center);
        Colocar(rango, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 95f), new Vector2(500f, 250f));

        GameObject resumen = ObtenerOCrearUI(panel.transform, "Resumen", typeof(TextMeshProUGUI));
        TextMeshProUGUI tmpResumen = resumen.GetComponent<TextMeshProUGUI>();
        ConfigurarTMP(tmpResumen, fuente, "PUNTAJE 0", 30, Color.white, TextAlignmentOptions.Center);
        tmpResumen.richText = true;
        tmpResumen.lineSpacing = 12f;
        Colocar(resumen, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -135f), new Vector2(1300f, 260f));

        GameObject btnReintentar = CrearBoton(panel.transform, "BotonReintentar", "REINTENTAR", new Vector2(-360f, -345f), fuente, blanco);
        GameObject btnSiguiente = CrearBoton(panel.transform, "BotonSiguienteStage", "SIGUIENTE STAGE", new Vector2(0f, -345f), fuente, blanco);
        GameObject btnMenu = CrearBoton(panel.transform, "BotonMenu", "MENÚ PRINCIPAL", new Vector2(360f, -345f), fuente, blanco);

        if (merc != null)
        {
            AsignarClick(btnReintentar, merc.ReiniciarPartida);
            AsignarClick(btnSiguiente, merc.SiguienteStage);
            AsignarClick(btnMenu, merc.IrAlMenuPrincipal);

            Undo.RecordObject(merc, "Vincular resultados");
            merc.panelGameOver = panel;
            merc.textoTituloResultado = tmpTitulo;
            merc.textoRango = tmpRango;
            merc.textoResumen = tmpResumen;
            merc.botonSiguienteStage = btnSiguiente;
            merc.sortingOrderGameOver = 5000;
            EditorUtility.SetDirty(merc);
        }

        if (panel.activeSelf) { Undo.RecordObject(panel, "Ocultar panel"); panel.SetActive(false); }
    }

    private static GameObject CrearBoton(Transform padre, string nombre, string texto, Vector2 pos, TMP_FontAsset fuente, Sprite blanco)
    {
        GameObject go = ObtenerOCrearUI(padre, nombre, typeof(Image), typeof(Button));
        Colocar(go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(320f, 72f));
        Image img = go.GetComponent<Image>();
        img.sprite = blanco;
        img.color = Color.white;
        img.raycastTarget = true;

        Button b = go.GetComponent<Button>();
        b.targetGraphic = img;
        ColorBlock cb = b.colors;
        cb.normalColor = new Color(0.32f, 0.04f, 0.04f, 0.95f);
        cb.highlightedColor = new Color(0.65f, 0.08f, 0.06f, 1f);
        cb.pressedColor = new Color(0.9f, 0.2f, 0.1f, 1f);
        cb.selectedColor = cb.highlightedColor;
        b.colors = cb;

        GameObject lbl = ObtenerOCrearUI(go.transform, "Texto", typeof(TextMeshProUGUI));
        ConfigurarTMP(lbl.GetComponent<TextMeshProUGUI>(), fuente, texto, 26, Color.white, TextAlignmentOptions.Center);
        Colocar(lbl, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320f, 72f));
        EditorUtility.SetDirty(go);
        return go;
    }

    private static void AsignarClick(GameObject boton, UnityAction accion)
    {
        Button b = boton.GetComponent<Button>();
        if (b == null) return;
        Undo.RecordObject(b, "OnClick");
        while (b.onClick.GetPersistentEventCount() > 0)
            UnityEventTools.RemovePersistentListener(b.onClick, 0);
        UnityEventTools.AddPersistentListener(b.onClick, accion);
        EditorUtility.SetDirty(b);
    }

    // ---------- Helpers de UI ----------

    private static GameObject ObtenerOCrearUI(Transform padre, string nombre, params System.Type[] componentes)
    {
        Transform t = padre.Find(nombre);
        GameObject go;
        if (t != null)
        {
            go = t.gameObject;
        }
        else
        {
            go = new GameObject(nombre, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(padre, false);
            Undo.RegisterCreatedObjectUndo(go, "Crear " + nombre);
        }
        foreach (System.Type c in componentes)
            if (go.GetComponent(c) == null) Undo.AddComponent(go, c);
        return go;
    }

    private static RectTransform Colocar(GameObject go, Vector2 ancla, Vector2 pivote, Vector2 pos, Vector2 tam)
    {
        RectTransform r = go.GetComponent<RectTransform>();
        Undo.RecordObject(r, "Colocar " + go.name);
        r.anchorMin = ancla;
        r.anchorMax = ancla;
        r.pivot = pivote;
        r.anchoredPosition = pos;
        r.sizeDelta = tam;
        r.localScale = Vector3.one;
        return r;
    }

    private static void ConfigurarTMP(TextMeshProUGUI tmp, TMP_FontAsset fuente, string texto, float tam, Color color, TextAlignmentOptions alineacion)
    {
        if (tmp == null) return;
        Undo.RecordObject(tmp, "Texto " + tmp.name);
        if (fuente != null) tmp.font = fuente;
        if (!string.IsNullOrEmpty(texto)) tmp.text = texto;
        tmp.fontSize = tam;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = color;
        tmp.alignment = alineacion;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Overflow;
        EditorUtility.SetDirty(tmp);
    }

    // =====================================================================
    // CONFIGURACIÓN DE GAMEPLAY EN LA ESCENA ACTIVA
    // =====================================================================

    private static void ConfigurarGameplayEscena(bool esStage1)
    {
        MercenariesManager merc = Object.FindAnyObjectByType<MercenariesManager>(FindObjectsInactive.Include);
        if (merc != null)
        {
            Undo.RecordObject(merc, "Configurar MercenariesManager");
            merc.tiempoVentanaCombo = Mathf.Max(6.5f, merc.tiempoVentanaCombo);
            if (merc.oleadasParaVictoria <= 0) merc.oleadasParaVictoria = 4;
            merc.escenaSiguiente = esStage1 ? NOMBRE_STAGE2 : "CreditsScene";
            EditorUtility.SetDirty(merc);
        }

        PlayerMovement jugador = Object.FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Include);
        if (jugador != null)
        {
            Undo.RecordObject(jugador, "Munición inicial");
            jugador.municionTotal = Mathf.Max(60, jugador.municionTotal); // 3 armas comparten reserva
            EditorUtility.SetDirty(jugador);
        }

        WaveSpawner spawner = Object.FindAnyObjectByType<WaveSpawner>(FindObjectsInactive.Include);
        GameObject corredor = AssetDatabase.LoadAssetAtPath<GameObject>(RUTA_CORREDOR);
        GameObject bruto = AssetDatabase.LoadAssetAtPath<GameObject>(RUTA_BRUTO);
        if (spawner != null && (corredor != null || bruto != null))
        {
            Undo.RecordObject(spawner, "Tipos de enemigo por oleada");
            spawner.tiposExtra = new List<WaveSpawner.TipoEnemigoOleada>();
            if (corredor != null)
                spawner.tiposExtra.Add(new WaveSpawner.TipoEnemigoOleada { nombre = "Corredor", prefab = corredor, oleadaMinima = esStage1 ? 2 : 1, peso = 0.35f, pesoExtraPorOleada = 0.15f });
            if (bruto != null)
                spawner.tiposExtra.Add(new WaveSpawner.TipoEnemigoOleada { nombre = "Bruto", prefab = bruto, oleadaMinima = esStage1 ? 3 : 2, peso = 0.18f, pesoExtraPorOleada = 0.1f });
            spawner.maxEnemigosEnEscena = Mathf.Max(spawner.maxEnemigosEnEscena, 10);
            EditorUtility.SetDirty(spawner);
        }
    }

    // =====================================================================
    // 4. STAGE 1 AMPLIADO
    // =====================================================================

    // Coordenadas medidas sobre HospitalBg en una vista de 896 x 500 px
    private const float REF_W = 896f, REF_H = 500f;

    private static void AmpliarStage1Interno()
    {
        int capa = AsegurarCapa(CAPA_OBSTACULOS);
        GameObject bgGo = BuscarEnEscena("HospitalBg_0");
        SpriteRenderer bg = bgGo != null ? bgGo.GetComponent<SpriteRenderer>() : null;
        if (bg == null || bg.sprite == null)
        {
            Debug.LogWarning("[Biohazard] No se encontró HospitalBg_0; no se puede ampliar el Stage 1.");
            return;
        }

        // 1. Escalar el fondo para cubrir ~50 x 28 unidades (antes ~27.5 x 15.4)
        Vector2 tamSprite = bg.sprite.bounds.size;
        float k = Mathf.Max(50f / tamSprite.x, 28f / tamSprite.y);
        Undo.RecordObject(bg.transform, "Escalar fondo");
        bg.transform.localScale = new Vector3(k, k, 1f);
        Vector2 c = bg.transform.position;
        float W = tamSprite.x * k, H = tamSprite.y * k;

        System.Func<float, float, Vector2> P = (px, py) => new Vector2(c.x + (px / REF_W - 0.5f) * W, c.y + (0.5f - py / REF_H) * H);
        System.Func<float, float, Vector2> S = (pw, ph) => new Vector2(pw / REF_W * W, ph / REF_H * H);

        // 2. Desactivar muros antiguos (se conservan por si quieres revertir)
        foreach (string viejo in new[] { "Square", "Square (1)", "Square (2)", "Square (3)" })
        {
            GameObject go = BuscarEnEscena(viejo);
            if (go != null && go.transform.parent == null && go.GetComponent<BoxCollider2D>() != null && go.activeSelf)
            {
                Undo.RecordObject(go, "Desactivar muro viejo");
                go.SetActive(false);
            }
        }

        // 3. Muros perimetrales nuevos (invisibles: el arte ya dibuja las paredes)
        float insetX = W * 0.011f, insetY = H * 0.038f, grosor = 2f;
        Transform limites = ObtenerOCrearRaiz("Limites_Stage").transform;
        CrearSolido(limites, "Muro_Izq", new Vector2(c.x - W / 2f + insetX - grosor / 2f, c.y), new Vector2(grosor, H + 4f), 0f, capa);
        CrearSolido(limites, "Muro_Der", new Vector2(c.x + W / 2f - insetX + grosor / 2f, c.y), new Vector2(grosor, H + 4f), 0f, capa);
        CrearSolido(limites, "Muro_Sup", new Vector2(c.x, c.y + H / 2f - insetY + grosor / 2f), new Vector2(W + 4f, grosor), 0f, capa);
        CrearSolido(limites, "Muro_Inf", new Vector2(c.x, c.y - H / 2f + insetY - grosor / 2f), new Vector2(W + 4f, grosor), 0f, capa);

        // 4. Obstáculos tácticos sobre el mobiliario dibujado en el fondo
        Transform obst = ObtenerOCrearRaiz("Obstaculos_Stage1").transform;
        CrearSolido(obst, "Recepcion_Base", P(452, 262), S(240, 48), 0f, capa);
        CrearSolido(obst, "Recepcion_BrazoIzq", P(337, 202), S(40, 104), 0f, capa);
        CrearSolido(obst, "Recepcion_BrazoDer", P(567, 202), S(40, 104), 0f, capa);
        CrearSolido(obst, "Columna_Derecha", P(692, 102), S(68, 146), 0f, capa);
        CrearSolido(obst, "Columna_Izquierda", P(224, 348), S(80, 106), 0f, capa);
        CrearSolido(obst, "Camilla_SupIzq", P(305, 68), S(100, 34), 20f, capa);
        CrearSolido(obst, "Camilla_SupDer", P(578, 68), S(98, 34), -20f, capa);
        CrearSolido(obst, "Camilla_Inf", P(318, 422), S(112, 36), 22f, capa);
        CrearSolido(obst, "Sillas_Inferiores", P(627, 432), S(168, 34), 0f, capa);

        // 5. Reubicar a Bryan y la pistola fuera del mostrador
        GameObject player = BuscarEnEscena("Player");
        if (player != null) { Undo.RecordObject(player.transform, "Mover Bryan"); player.transform.position = P(450, 350); }
        GameObject pickup = BuscarEnEscena("BryanHGAim_0");
        if (pickup != null) { Undo.RecordObject(pickup.transform, "Mover pistola"); pickup.transform.position = P(520, 365); }

        // 6. SpawnPoints repartidos por todo el perímetro
        Vector2[] spawns =
        {
            P(110, 70), P(450, 85), P(790, 200), P(800, 455), P(460, 458),
            P(190, 470), P(90, 250), P(760, 300), P(240, 160), P(640, 262)
        };
        ConfigurarSpawnPoints(spawns);

        // 7. Capa de obstáculos y cámara
        CameraFollow cam = Object.FindAnyObjectByType<CameraFollow>(FindObjectsInactive.Include);
        if (cam != null) { Undo.RecordObject(cam, "Cámara"); cam.mapBackground = bg; EditorUtility.SetDirty(cam); }

        MarcarEscenaSucia();
    }

    private static void ConfigurarSpawnPoints(Vector2[] posiciones)
    {
        GameObject raiz = BuscarEnEscena("SpawnPoints");
        if (raiz == null) raiz = ObtenerOCrearRaiz("SpawnPoints");

        List<Transform> puntos = new List<Transform>();
        for (int i = 0; i < posiciones.Length; i++)
        {
            string nombre = "Spawn_" + (i + 1);
            Transform t = raiz.transform.Find(nombre);
            if (t == null)
            {
                GameObject go = new GameObject(nombre);
                go.transform.SetParent(raiz.transform, false);
                Undo.RegisterCreatedObjectUndo(go, "Crear " + nombre);
                t = go.transform;
            }
            Undo.RecordObject(t, "Mover spawn");
            t.position = new Vector3(posiciones[i].x, posiciones[i].y, 0f);
            puntos.Add(t);
        }

        WaveSpawner spawner = Object.FindAnyObjectByType<WaveSpawner>(FindObjectsInactive.Include);
        if (spawner != null)
        {
            Undo.RecordObject(spawner, "Spawn points");
            spawner.puntosDeSpawn = puntos.ToArray();
            spawner.distanciaMinimaAlJugador = Mathf.Max(spawner.distanciaMinimaAlJugador, 7f);
            EditorUtility.SetDirty(spawner);
        }
    }

    // ---------- Helpers de escena ----------

    private static GameObject BuscarEnEscena(string nombre)
    {
        foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
                if (t.name == nombre) return t.gameObject;
        }
        return null;
    }

    private static GameObject ObtenerOCrearRaiz(string nombre)
    {
        foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            if (raiz.name == nombre) return raiz;
        GameObject go = new GameObject(nombre);
        Undo.RegisterCreatedObjectUndo(go, "Crear " + nombre);
        return go;
    }

    /// <summary>Crea (o actualiza) un sólido con BoxCollider2D. Con sprite se vuelve visible.</summary>
    private static GameObject CrearSolido(Transform padre, string nombre, Vector2 posMundo, Vector2 tam, float rotZ, int capa,
        Sprite sprite = null, bool tiled = false, int orden = -1, Color? color = null)
    {
        Transform t = padre.Find(nombre);
        GameObject go;
        if (t != null) go = t.gameObject;
        else
        {
            go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            Undo.RegisterCreatedObjectUndo(go, "Crear " + nombre);
        }

        Undo.RecordObject(go, "Solido");
        Undo.RecordObject(go.transform, "Solido");
        go.layer = capa;
        go.isStatic = false;
        go.transform.position = new Vector3(posMundo.x, posMundo.y, 0f);
        go.transform.rotation = Quaternion.Euler(0f, 0f, rotZ);
        go.transform.localScale = Vector3.one;

        BoxCollider2D bc = go.GetComponent<BoxCollider2D>();
        if (bc == null) bc = Undo.AddComponent<BoxCollider2D>(go);
        bc.offset = Vector2.zero;
        bc.size = tam;

        if (sprite != null)
        {
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = Undo.AddComponent<SpriteRenderer>(go);
            sr.sprite = sprite;
            sr.sortingOrder = orden;
            sr.color = color ?? Color.white;
            if (tiled)
            {
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = tam;
            }
            else
            {
                sr.drawMode = SpriteDrawMode.Simple;
                Vector2 b = sprite.bounds.size;
                go.transform.localScale = new Vector3(tam.x / b.x, tam.y / b.y, 1f);
                bc.size = b;
            }
            EditorUtility.SetDirty(sr);
        }

        EditorUtility.SetDirty(bc);
        return go;
    }

    private static SpriteRenderer CrearDecorado(Transform padre, string nombre, Vector2 pos, Vector2 tam, Sprite sprite, int orden, Color color, bool tiled)
    {
        Transform t = padre.Find(nombre);
        GameObject go;
        if (t != null) go = t.gameObject;
        else
        {
            go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            Undo.RegisterCreatedObjectUndo(go, "Crear " + nombre);
        }
        Undo.RecordObject(go.transform, "Decorado");
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        go.transform.rotation = Quaternion.identity;

        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = Undo.AddComponent<SpriteRenderer>(go);
        sr.sprite = sprite;
        sr.sortingOrder = orden;
        sr.color = color;
        if (tiled)
        {
            go.transform.localScale = Vector3.one;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = tam;
        }
        else
        {
            sr.drawMode = SpriteDrawMode.Simple;
            Vector2 b = sprite.bounds.size;
            go.transform.localScale = new Vector3(tam.x / b.x, tam.y / b.y, 1f);
        }
        EditorUtility.SetDirty(sr);
        return sr;
    }

    // =====================================================================
    // 5. STAGE 2 - LABORATORIO
    // =====================================================================

    private static void CrearStage2Interno()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RUTA_STAGE2) == null)
        {
            // Se parte de una copia del Stage 1: conserva Bryan, Canvas HUD, GameManager, WaveSpawner y AudioManager
            if (!AssetDatabase.CopyAsset(RUTA_STAGE1, RUTA_STAGE2))
            {
                Debug.LogError("[Biohazard] No se pudo copiar " + RUTA_STAGE1);
                return;
            }
            AssetDatabase.Refresh();
        }

        EditorSceneManager.OpenScene(RUTA_STAGE2, OpenSceneMode.Single);
        ConstruirLaboratorio();
        ConfigurarGameplayEscena(false);

        MercenariesManager merc = Object.FindAnyObjectByType<MercenariesManager>(FindObjectsInactive.Include);
        if (merc != null)
        {
            Undo.RecordObject(merc, "Stage 2");
            merc.tiempoRestante = Mathf.Max(merc.tiempoRestante, 150f);
            merc.umbralRangoS = Mathf.Max(merc.umbralRangoS, 260000);
            merc.umbralRangoA = Mathf.Max(merc.umbralRangoA, 160000);
            merc.umbralRangoB = Mathf.Max(merc.umbralRangoB, 80000);
            EditorUtility.SetDirty(merc);
        }
        WaveSpawner spawner = Object.FindAnyObjectByType<WaveSpawner>(FindObjectsInactive.Include);
        if (spawner != null)
        {
            Undo.RecordObject(spawner, "Stage 2 oleadas");
            spawner.enemigosInicialesPorOleada = Mathf.Max(spawner.enemigosInicialesPorOleada, 6);
            spawner.incrementoPorOleada = Mathf.Max(spawner.incrementoPorOleada, 4);
            EditorUtility.SetDirty(spawner);
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AgregarABuildSettings(RUTA_STAGE2, RUTA_STAGE1);
    }

    private static void ConstruirLaboratorio()
    {
        int capa = AsegurarCapa(CAPA_OBSTACULOS);
        const float W = 44f, H = 26f;

        // Limpiar lo heredado del Stage 1
        GameObject bgHospital = BuscarEnEscena("HospitalBg_0");
        if (bgHospital != null && bgHospital.activeSelf) { Undo.RecordObject(bgHospital, "Ocultar hospital"); bgHospital.SetActive(false); }
        foreach (string heredado in new[] { "Obstaculos_Stage1", "Limites_Stage" })
        {
            GameObject g = BuscarEnEscena(heredado);
            if (g != null) Undo.DestroyObjectImmediate(g);
        }

        // Texturas procedurales
        Sprite suelo = SpriteGenerado("Lab_Suelo", 64f, true, TexturaSueloLaboratorio);
        Sprite muro = SpriteGenerado("Lab_Muro", 64f, true, TexturaMuroMetal);
        Sprite mesa = SpriteGenerado("Lab_Mesa", 64f, false, TexturaMesaLaboratorio);
        Sprite tanque = SpriteGenerado("Lab_Tanque", 64f, false, TexturaTanque);
        Sprite caja = SpriteGenerado("Lab_Caja", 64f, false, TexturaCaja);
        Sprite mancha = SpriteGenerado("UI_Circulo", 100f, false, () => TexturaCirculo(128, Color.white, 0.04f));

        Transform raiz = ObtenerOCrearRaiz("Escenario_Laboratorio").transform;

        // Suelo (también sirve de límite para la cámara)
        SpriteRenderer srSuelo = CrearDecorado(raiz, "Suelo_Laboratorio", Vector2.zero, new Vector2(W + 2f, H + 2f), suelo, -5, Color.white, true);

        // Manchas de cultivo verde en el piso
        Vector2[] manchas = { new Vector2(-12, 7), new Vector2(10, -3), new Vector2(3, 9), new Vector2(-6, -9), new Vector2(17, 8), new Vector2(-18, -6) };
        for (int i = 0; i < manchas.Length; i++)
        {
            float tam = 1.6f + (i % 3) * 0.8f;
            CrearDecorado(raiz, "Mancha_" + (i + 1), manchas[i], new Vector2(tam * 1.4f, tam), mancha, -4, new Color(0.3f, 1f, 0.45f, 0.18f), false);
        }

        // Muros visibles
        Transform limites = ObtenerOCrearRaiz("Limites_Stage").transform;
        float g2 = 1.4f;
        CrearSolido(limites, "Muro_Izq", new Vector2(-W / 2f + g2 / 2f, 0f), new Vector2(g2, H), 0f, capa, muro, true, -1);
        CrearSolido(limites, "Muro_Der", new Vector2(W / 2f - g2 / 2f, 0f), new Vector2(g2, H), 0f, capa, muro, true, -1);
        CrearSolido(limites, "Muro_Sup", new Vector2(0f, H / 2f - g2 / 2f), new Vector2(W, g2), 0f, capa, muro, true, -1);
        CrearSolido(limites, "Muro_Inf", new Vector2(0f, -H / 2f + g2 / 2f), new Vector2(W, g2), 0f, capa, muro, true, -1);

        // Obstáculos tácticos del laboratorio
        Transform obst = ObtenerOCrearRaiz("Obstaculos_Laboratorio").transform;
        CrearSolido(obst, "Mesa_1", new Vector2(-8f, 4f), new Vector2(5f, 1.6f), 0f, capa, mesa, false, -1);
        CrearSolido(obst, "Mesa_2", new Vector2(8f, 4f), new Vector2(5f, 1.6f), 0f, capa, mesa, false, -1);
        CrearSolido(obst, "Mesa_3", new Vector2(-8f, -6f), new Vector2(5f, 1.6f), 0f, capa, mesa, false, -1);
        CrearSolido(obst, "Mesa_4", new Vector2(8f, -6f), new Vector2(5f, 1.6f), 0f, capa, mesa, false, -1);
        CrearSolido(obst, "Tanque_1", new Vector2(0f, 7.5f), new Vector2(2.4f, 2.4f), 0f, capa, tanque, false, -1);
        CrearSolido(obst, "Tanque_2", new Vector2(-15.5f, -0.5f), new Vector2(2.4f, 2.4f), 0f, capa, tanque, false, -1);
        CrearSolido(obst, "Tanque_3", new Vector2(15.5f, -0.5f), new Vector2(2.4f, 2.4f), 0f, capa, tanque, false, -1);
        CrearSolido(obst, "Caja_1", new Vector2(-18f, 8.5f), new Vector2(1.4f, 1.4f), 0f, capa, caja, false, -1);
        CrearSolido(obst, "Caja_2", new Vector2(-16.5f, 8.5f), new Vector2(1.4f, 1.4f), 0f, capa, caja, false, -1);
        CrearSolido(obst, "Caja_3", new Vector2(18f, -8.5f), new Vector2(1.4f, 1.4f), 0f, capa, caja, false, -1);
        CrearSolido(obst, "Caja_4", new Vector2(18f, -7f), new Vector2(1.4f, 1.4f), 0f, capa, caja, false, -1);
        CrearSolido(obst, "Caja_5", new Vector2(-3.5f, -10f), new Vector2(1.4f, 1.4f), 0f, capa, caja, false, -1);
        CrearSolido(obst, "Caja_6", new Vector2(5f, 10f), new Vector2(1.4f, 1.4f), 0f, capa, caja, false, -1);

        // Bryan y pistola
        GameObject player = BuscarEnEscena("Player");
        if (player != null) { Undo.RecordObject(player.transform, "Mover Bryan"); player.transform.position = new Vector3(0f, -2f, 0f); }
        GameObject pickup = BuscarEnEscena("BryanHGAim_0");
        if (pickup != null) { Undo.RecordObject(pickup.transform, "Mover pistola"); pickup.transform.position = new Vector3(2.5f, -2f, 0f); }

        // Spawns
        ConfigurarSpawnPoints(new Vector2[]
        {
            new Vector2(-19.5f, 10.5f), new Vector2(19.5f, 10.5f), new Vector2(-19.5f, -10.5f), new Vector2(19.5f, -10.5f),
            new Vector2(0f, 11f), new Vector2(0f, -11f), new Vector2(-19.5f, 4f), new Vector2(19.5f, -3.5f),
            new Vector2(-11f, -10.5f), new Vector2(11f, 10.5f)
        });

        // Cámara: limitar al suelo del laboratorio
        CameraFollow cam = Object.FindAnyObjectByType<CameraFollow>(FindObjectsInactive.Include);
        if (cam != null) { Undo.RecordObject(cam, "Cámara"); cam.mapBackground = srSuelo; EditorUtility.SetDirty(cam); }

        // Iluminación fría de laboratorio (Light2D vía SerializedObject para no depender del ensamblado de URP)
        GameObject luzGo = BuscarEnEscena("Global Light 2D");
        if (luzGo != null)
        {
            Component luz = luzGo.GetComponent("Light2D");
            if (luz != null)
            {
                SerializedObject so = new SerializedObject(luz);
                SerializedProperty col = so.FindProperty("m_Color");
                SerializedProperty inten = so.FindProperty("m_Intensity");
                if (col != null) col.colorValue = new Color(0.72f, 0.9f, 1f, 1f);
                if (inten != null) inten.floatValue = 0.85f;
                so.ApplyModifiedProperties();
            }
        }
    }

    private static void AgregarABuildSettings(string ruta, string despuesDe)
    {
        List<EditorBuildSettingsScene> escenas = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (escenas.Exists(e => e.path == ruta)) return;
        int idx = escenas.FindIndex(e => e.path == despuesDe);
        EditorBuildSettingsScene nueva = new EditorBuildSettingsScene(ruta, true);
        if (idx >= 0) escenas.Insert(idx + 1, nueva);
        else escenas.Add(nueva);
        EditorBuildSettings.scenes = escenas.ToArray();
    }

    // =====================================================================
    // TEXTURAS PROCEDURALES
    // =====================================================================

    private static Sprite SpriteGenerado(string nombre, float ppu, bool tileable, System.Func<Texture2D> generador)
    {
        string ruta = CARPETA_GEN + "/" + nombre + ".png";
        Sprite existente = AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
        if (existente != null) return existente;

        if (!AssetDatabase.IsValidFolder("Assets/Sprites")) AssetDatabase.CreateFolder("Assets", "Sprites");
        if (!AssetDatabase.IsValidFolder(CARPETA_GEN)) AssetDatabase.CreateFolder("Assets/Sprites", "Generados");

        Texture2D tex = generador();
        string rutaCompleta = Path.Combine(Application.dataPath, ruta.Substring("Assets/".Length));
        File.WriteAllBytes(rutaCompleta, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);

        TextureImporter imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (imp != null)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.spritePixelsPerUnit = ppu;
            imp.filterMode = FilterMode.Bilinear;
            imp.wrapMode = tileable ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            TextureImporterSettings st = new TextureImporterSettings();
            imp.ReadTextureSettings(st);
            st.spriteMeshType = tileable ? SpriteMeshType.FullRect : SpriteMeshType.Tight;
            imp.SetTextureSettings(st);
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }

    private static Texture2D NuevaTextura(int w, int h)
    {
        return new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
    }

    private static Texture2D TexturaSolida(int w, int h, Color c)
    {
        Texture2D t = NuevaTextura(w, h);
        Color[] px = new Color[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = c;
        t.SetPixels(px);
        t.Apply();
        return t;
    }

    private static Texture2D TexturaCirculo(int tam, Color c, float suavizado)
    {
        Texture2D t = NuevaTextura(tam, tam);
        Color[] px = new Color[tam * tam];
        float r = tam * 0.5f;
        for (int y = 0; y < tam; y++)
            for (int x = 0; x < tam; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                float a = Mathf.Clamp01((1f - d) / Mathf.Max(0.001f, suavizado));
                px[y * tam + x] = new Color(c.r, c.g, c.b, c.a * a);
            }
        t.SetPixels(px);
        t.Apply();
        return t;
    }

    private static float Ruido(int x, int y, int semilla)
    {
        unchecked
        {
            int h = x * 374761393 + y * 668265263 + semilla * 1442695041;
            h = (h ^ (h >> 13)) * 1274126177;
            return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
        }
    }

    private static Texture2D TexturaSueloLaboratorio()
    {
        int tam = 128; // 2 x 2 baldosas por textura (2 unidades a 64 ppu)
        Texture2D t = NuevaTextura(tam, tam);
        t.wrapMode = TextureWrapMode.Repeat;
        Color[] px = new Color[tam * tam];
        for (int y = 0; y < tam; y++)
            for (int x = 0; x < tam; x++)
            {
                int bx = x / 64, by = y / 64;
                float variacion = (Ruido(bx, by, 7) - 0.5f) * 0.03f;
                Color baseC = new Color(0.17f + variacion, 0.2f + variacion, 0.215f + variacion, 1f);
                float n = (Ruido(x, y, 3) - 0.5f) * 0.035f;
                Color c = new Color(baseC.r + n, baseC.g + n, baseC.b + n, 1f);
                int lx = x % 64, ly = y % 64;
                bool junta = lx < 2 || ly < 2;
                bool brillo = lx == 2 || ly == 62;
                if (junta) c = new Color(0.07f, 0.08f, 0.09f, 1f);
                else if (brillo) c = new Color(c.r + 0.05f, c.g + 0.06f, c.b + 0.06f, 1f);
                if (Ruido(x / 3, y / 3, 11) > 0.985f) c = new Color(0.1f, 0.25f, 0.12f, 1f); // salpicaduras
                px[y * tam + x] = c;
            }
        t.SetPixels(px);
        t.Apply();
        return t;
    }

    private static Texture2D TexturaMuroMetal()
    {
        int tam = 64;
        Texture2D t = NuevaTextura(tam, tam);
        t.wrapMode = TextureWrapMode.Repeat;
        Color[] px = new Color[tam * tam];
        for (int y = 0; y < tam; y++)
            for (int x = 0; x < tam; x++)
            {
                float n = (Ruido(x, y, 21) - 0.5f) * 0.04f;
                Color c = new Color(0.2f + n, 0.215f + n, 0.24f + n, 1f);
                if (x < 2 || y < 2) c = new Color(0.08f, 0.085f, 0.1f, 1f);
                else if (x == 2 || y == 2) c = new Color(0.32f, 0.34f, 0.37f, 1f);
                bool remache = (Mathf.Abs(x - 8) < 2 || Mathf.Abs(x - 56) < 2) && (Mathf.Abs(y - 8) < 2 || Mathf.Abs(y - 56) < 2);
                if (remache) c = new Color(0.45f, 0.47f, 0.5f, 1f);
                // Franja de peligro amarilla/negra en el centro
                if (y >= 28 && y < 36) c = ((x + y) / 6) % 2 == 0 ? new Color(0.75f, 0.6f, 0.08f, 1f) : new Color(0.06f, 0.06f, 0.06f, 1f);
                px[y * tam + x] = c;
            }
        t.SetPixels(px);
        t.Apply();
        return t;
    }

    private static Texture2D TexturaMesaLaboratorio()
    {
        int w = 160, h = 52;
        Texture2D t = NuevaTextura(w, h);
        Color[] px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float n = (Ruido(x, y, 31) - 0.5f) * 0.03f;
                Color c = new Color(0.55f + n, 0.58f + n, 0.6f + n, 1f);
                if (x < 3 || y < 3 || x >= w - 3 || y >= h - 3) c = new Color(0.18f, 0.19f, 0.21f, 1f);
                else if (y < 8) c = new Color(0.4f, 0.42f, 0.44f, 1f);
                px[y * w + x] = c;
            }
        // Frascos de colores
        Color[] colores = { new Color(0.2f, 0.95f, 0.35f), new Color(0.3f, 0.6f, 1f), new Color(0.95f, 0.2f, 0.2f), new Color(0.9f, 0.85f, 0.2f) };
        for (int k = 0; k < 7; k++)
        {
            int cx = 18 + k * 20, cy = 28 + (k % 2) * 6, r = 6 + (k % 3);
            Color col = colores[k % colores.Length];
            for (int y = cy - r; y <= cy + r; y++)
                for (int x = cx - r; x <= cx + r; x++)
                {
                    if (x < 0 || y < 0 || x >= w || y >= h) continue;
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    if (d <= r) px[y * w + x] = d > r - 1.5f ? new Color(0.1f, 0.1f, 0.12f, 1f) : Color.Lerp(col, Color.white, d < 2f ? 0.5f : 0f);
                }
        }
        t.SetPixels(px);
        t.Apply();
        return t;
    }

    private static Texture2D TexturaTanque()
    {
        int tam = 128;
        Texture2D t = NuevaTextura(tam, tam);
        Color[] px = new Color[tam * tam];
        float r = tam * 0.5f;
        for (int y = 0; y < tam; y++)
            for (int x = 0; x < tam; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                Color c = Color.clear;
                if (d <= 1f)
                {
                    if (d > 0.82f) c = new Color(0.22f, 0.24f, 0.27f, 1f);           // aro metálico
                    else if (d > 0.78f) c = new Color(0.45f, 0.48f, 0.52f, 1f);      // borde brillante
                    else c = Color.Lerp(new Color(0.35f, 1f, 0.5f, 1f), new Color(0.04f, 0.28f, 0.1f, 1f), d / 0.78f); // líquido
                    // Reflejo del vidrio
                    float dx = x - r * 0.65f, dy = y - r * 1.35f;
                    if (d < 0.78f && dx * dx + dy * dy < 90f) c = Color.Lerp(c, Color.white, 0.55f);
                    // Antialias del borde exterior
                    c.a *= Mathf.Clamp01((1f - d) * r * 0.5f);
                }
                px[y * tam + x] = c;
            }
        t.SetPixels(px);
        t.Apply();
        return t;
    }

    private static Texture2D TexturaCaja()
    {
        int tam = 64;
        Texture2D t = NuevaTextura(tam, tam);
        Color[] px = new Color[tam * tam];
        for (int y = 0; y < tam; y++)
            for (int x = 0; x < tam; x++)
            {
                float n = (Ruido(x, y / 4, 41) - 0.5f) * 0.06f;
                Color c = new Color(0.42f + n, 0.29f + n, 0.15f + n, 1f);
                if (y % 16 == 0) c = new Color(0.22f, 0.14f, 0.07f, 1f);
                if (x < 4 || y < 4 || x >= tam - 4 || y >= tam - 4) c = new Color(0.3f, 0.2f, 0.1f, 1f);
                if (Mathf.Abs(x - y) < 3 || Mathf.Abs(x - (tam - 1 - y)) < 3) c = new Color(0.33f, 0.22f, 0.11f, 1f);
                px[y * tam + x] = c;
            }
        t.SetPixels(px);
        t.Apply();
        return t;
    }

    // ---------- Íconos de armas (siluetas) ----------

    private static Texture2D TexturaIconoPistola()
    {
        return DibujarSilueta(256, 96, new List<Vector2[]>
        {
            new[] { new Vector2(60, 52), new Vector2(205, 52), new Vector2(205, 74), new Vector2(60, 74) },
            new[] { new Vector2(68, 52), new Vector2(102, 52), new Vector2(90, 8), new Vector2(56, 8) },
            new[] { new Vector2(102, 52), new Vector2(122, 52), new Vector2(118, 36), new Vector2(102, 38) }
        });
    }

    private static Texture2D TexturaIconoSubfusil()
    {
        return DibujarSilueta(256, 96, new List<Vector2[]>
        {
            new[] { new Vector2(44, 50), new Vector2(192, 50), new Vector2(192, 72), new Vector2(44, 72) },   // cajón
            new[] { new Vector2(192, 57), new Vector2(246, 57), new Vector2(246, 66), new Vector2(192, 66) }, // cañón
            new[] { new Vector2(120, 50), new Vector2(140, 50), new Vector2(134, 6), new Vector2(114, 6) },   // cargador
            new[] { new Vector2(70, 50), new Vector2(90, 50), new Vector2(82, 16), new Vector2(62, 16) },     // empuñadura
            new[] { new Vector2(44, 70), new Vector2(8, 64), new Vector2(8, 54), new Vector2(44, 58) }        // culata plegable
        });
    }

    private static Texture2D TexturaIconoEscopeta()
    {
        return DibujarSilueta(256, 96, new List<Vector2[]>
        {
            new[] { new Vector2(70, 60), new Vector2(252, 60), new Vector2(252, 69), new Vector2(70, 69) },   // cañón
            new[] { new Vector2(112, 51), new Vector2(232, 51), new Vector2(232, 59), new Vector2(112, 59) }, // tubo
            new[] { new Vector2(132, 45), new Vector2(188, 45), new Vector2(188, 62), new Vector2(132, 62) }, // corredera
            new[] { new Vector2(56, 52), new Vector2(112, 52), new Vector2(112, 72), new Vector2(56, 72) },   // cajón
            new[] { new Vector2(56, 70), new Vector2(8, 64), new Vector2(4, 40), new Vector2(22, 40), new Vector2(56, 55) }, // culata
            new[] { new Vector2(62, 52), new Vector2(76, 52), new Vector2(72, 38), new Vector2(60, 40) }      // gatillo
        });
    }

    private static Texture2D DibujarSilueta(int w, int h, List<Vector2[]> poligonos)
    {
        Texture2D t = NuevaTextura(w, h);
        bool[] lleno = new bool[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                foreach (Vector2[] poly in poligonos)
                    if (PuntoEnPoligono(p, poly)) { lleno[y * w + x] = true; break; }
            }

        Color[] px = new Color[w * h];
        Color relleno = new Color(0.92f, 0.92f, 0.9f, 1f);
        Color borde = new Color(0.05f, 0.05f, 0.05f, 1f);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                if (lleno[i]) { px[i] = relleno; continue; }
                bool vecino = false;
                for (int dy = -2; dy <= 2 && !vecino; dy++)
                    for (int dx = -2; dx <= 2 && !vecino; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx >= 0 && ny >= 0 && nx < w && ny < h && lleno[ny * w + nx]) vecino = true;
                    }
                px[i] = vecino ? borde : Color.clear;
            }
        t.SetPixels(px);
        t.Apply();
        return t;
    }
}
#endif
