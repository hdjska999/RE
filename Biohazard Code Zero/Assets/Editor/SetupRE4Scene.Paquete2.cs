#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// PAQUETE 2 de Biohazard: Code Zero (menú Tools > Biohazard Code Zero):
/// Menú principal, Créditos, Pausa estilo RE4 Remake, progresión de armas, recarga con tiempo,
/// granada, mira, nuevos zombies animados (Escupidor, Tóxico, Tocho), 5 oleadas y Stage 3.
/// Reutiliza los helpers de SetupRE4Scene (clase parcial). Es idempotente.
/// </summary>
public static partial class SetupRE4Scene
{
    private const string RUTA_STAGE3 = "Assets/Scenes/Stage3_RaccoonCity.unity";
    private const string RUTA_MAINMENU = "Assets/Scenes/MainMenu.unity";
    private const string RUTA_CREDITOS = "Assets/Scenes/CreditsScene.unity";
    private const string RUTA_ESCUPIDOR = "Assets/Prefabs/Lycan_Escupidor.prefab";
    private const string RUTA_TOXICO = "Assets/Prefabs/Lycan_Toxico.prefab";
    private const string RUTA_SHEET_BASE = "Assets/Prefabs/Lyccan.png";
    private const string CARPETA_SHEETS = "Assets/Sprites/Zombies";
    private const string CARPETA_ANIM = "Assets/Prefabs/AnimZombies";
    private const string RUTA_PLAYER_CTRL = "Assets/Prefabs/Player.controller";

    // =====================================================================
    // MENÚ
    // =====================================================================

    [MenuItem(MENU + "★★ Aplicar PAQUETE 2 completo (menús, armas, zombies, Stage 3)", false, 1)]
    public static void MenuPaquete2()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        try
        {
            Progreso("Zombies animados y variantes...", 0.1f);
            P2_CrearZombies();
            P2_AnimatorBryan();

            for (int n = 1; n <= 3; n++)
            {
                Progreso("Configurando Stage " + n + "...", 0.2f + n * 0.15f);
                if (n == 3) P2_CrearStage3();
                else EditorSceneManager.OpenScene(n == 1 ? RUTA_STAGE1 : RUTA_STAGE2, OpenSceneMode.Single);
                P2_ConfigurarStage(n);
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            }

            Progreso("Menú principal...", 0.8f);
            EditorSceneManager.OpenScene(RUTA_MAINMENU, OpenSceneMode.Single);
            P2_ConstruirMenuPrincipal();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

            Progreso("Créditos...", 0.9f);
            EditorSceneManager.OpenScene(RUTA_CREDITOS, OpenSceneMode.Single);
            P2_ConstruirCreditos();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

            AgregarABuildSettings(RUTA_STAGE3, RUTA_STAGE2);
            EditorSceneManager.OpenScene(RUTA_MAINMENU, OpenSceneMode.Single);
            AssetDatabase.SaveAssets();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        string msg = "¡PAQUETE 2 aplicado!\n\n" +
            "• Menú principal: título, fuente Cinzel, CONTINUAR y SELECCIONAR STAGE.\n" +
            "• Créditos centrados (1920x1080) con logos UAT y FI.\n" +
            "• Pausa estilo RE4 Remake con VOLUMEN / OPCIONES en los 3 stages.\n" +
            "• Armas por stage (Escopeta en Stage 2 y Subfusil en Stage 3 los suelta el primer Tocho).\n" +
            "• Recarga con tiempo, granada (G / clic central, 30 s) y mira personalizada.\n" +
            "• Zombies animados: Escupidor, Tóxico, Tocho y Corredor con sprites propios.\n" +
            "• 5 oleadas con dificultad progresiva y Stage3_RaccoonCity en Build Settings.\n\n" +
            "Escenas guardadas. Dale Play desde MainMenu.";
        EditorUtility.DisplayDialog("Biohazard Code Zero - Paquete 2", msg, "¡A jugar!");
        Debug.Log("[Biohazard P2] " + msg);
    }

    [MenuItem(MENU + "6. Menú Principal (título, fuente, Continuar, Seleccionar Stage)", false, 40)]
    public static void MenuP2MainMenu()
    {
        if (!AbrirEscenaSiHaceFalta(RUTA_MAINMENU)) return;
        P2_ConstruirMenuPrincipal();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Biohazard Code Zero", "Menú principal actualizado y guardado.", "OK");
    }

    [MenuItem(MENU + "7. Créditos (centrado responsivo con logos)", false, 41)]
    public static void MenuP2Creditos()
    {
        if (!AbrirEscenaSiHaceFalta(RUTA_CREDITOS)) return;
        P2_ConstruirCreditos();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Biohazard Code Zero", "Créditos reorganizados y guardados.", "OK");
    }

    [MenuItem(MENU + "8. Pausa RE4, armas, granada y mira (escena de Stage abierta)", false, 42)]
    public static void MenuP2Stage()
    {
        int n = ProgresoJuego.NumeroStage(SceneManager.GetActiveScene().name);
        if (n <= 0)
        {
            EditorUtility.DisplayDialog("Biohazard Code Zero", "Abre primero SampleScene, Stage2_Laboratorio o Stage3_RaccoonCity.", "OK");
            return;
        }
        P2_ConfigurarStage(n);
        MarcarEscenaSucia();
        EditorUtility.DisplayDialog("Biohazard Code Zero", "Stage " + n + " actualizado. Guarda con Ctrl+S.", "OK");
    }

    [MenuItem(MENU + "9. Zombies nuevos (sprites, animación, Escupidor, Tóxico, Tocho)", false, 43)]
    public static void MenuP2Zombies()
    {
        P2_CrearZombies();
        EditorUtility.DisplayDialog("Biohazard Code Zero", "Prefabs de zombies actualizados (animación de caminar + variantes).", "OK");
    }

    [MenuItem(MENU + "10. Crear o actualizar Stage 3 - Raccoon City", false, 44)]
    public static void MenuP2Stage3()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        P2_CrearStage3();
        P2_ConfigurarStage(3);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AgregarABuildSettings(RUTA_STAGE3, RUTA_STAGE2);
        EditorUtility.DisplayDialog("Biohazard Code Zero", "Stage3_RaccoonCity listo y agregado a Build Settings.", "OK");
    }

    [MenuItem(MENU + "Borrar progreso guardado (PlayerPrefs)", false, 80)]
    public static void MenuBorrarProgreso()
    {
        if (!EditorUtility.DisplayDialog("Biohazard Code Zero", "¿Borrar progreso, récords y armas desbloqueadas de este equipo?", "Borrar", "Cancelar")) return;
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }

    [MenuItem(MENU + "Desbloquear todo (pruebas)", false, 81)]
    public static void MenuDesbloquearTodo()
    {
        ProgresoJuego.DesbloquearArma(TipoArma.Escopeta);
        ProgresoJuego.DesbloquearArma(TipoArma.Subfusil);
        ProgresoJuego.RegistrarStageJugado(ProgresoJuego.ESCENA_STAGE3);
        ProgresoJuego.CompletarStage(ProgresoJuego.ESCENA_STAGE3);
        EditorUtility.DisplayDialog("Biohazard Code Zero", "Armas, stages y selector desbloqueados en este equipo.", "OK");
    }

    private static void Progreso(string texto, float p)
    {
        EditorUtility.DisplayProgressBar("Biohazard Code Zero - Paquete 2", texto, p);
    }

    private static bool AbrirEscenaSiHaceFalta(string ruta)
    {
        if (SceneManager.GetActiveScene().path == ruta) return true;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
        EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
        return true;
    }

    // =====================================================================
    // ZOMBIES: ANIMACIÓN + VARIANTES
    // =====================================================================

    private static void P2_CrearZombies()
    {
        P2_GenerarAudio(); // clips nuevos (se vinculan por escena)
        CrearEnemigosInterno(); // asegura Lycan, Corredor y Bruto del paquete 1

        if (!AssetDatabase.IsValidFolder(CARPETA_ANIM)) AssetDatabase.CreateFolder("Assets/Prefabs", "AnimZombies");

        AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(RUTA_ANIMATOR_ZOMBIE);
        ClipsZombie baseClips = P2_ClipsDeHoja(RUTA_SHEET_BASE, "Base");
        P2_AsignarMotions(ctrl, baseClips);

        Sprite circulo = SpriteGenerado("UI_Circulo", 100f, false, () => TexturaCirculo(128, Color.white, 0.04f));
        Sprite charco = SpriteGenerado("FX_Charco", 100f, false, TexturaCharco);
        int capa = AsegurarCapa(CAPA_OBSTACULOS);
        LayerMask mascara = 1 << capa;

        // Base: Lycan con animación real de caminar
        GameObject raiz = PrefabUtility.LoadPrefabContents(RUTA_LYCAN);
        Animator animBase = raiz.GetComponent<Animator>();
        if (animBase != null) animBase.runtimeAnimatorController = ctrl;
        ZombieEnemy zb = raiz.GetComponent<ZombieEnemy>();
        if (zb != null) zb.intensidadPaso = 0.025f;
        PrefabUtility.SaveAsPrefabAsset(raiz, RUTA_LYCAN);
        PrefabUtility.UnloadPrefabContents(raiz);

        // Corredor
        P2_ActualizarVariante(RUTA_CORREDOR, "Lycan_Corredor", "Corredor", ctrl, baseClips, go => { });

        // Tocho / Bruto: sprite propio y marca de Tocho (suelta el arma de premio)
        P2_ActualizarVariante(RUTA_BRUTO, "Lycan_Bruto", "Tocho", ctrl, baseClips, go =>
        {
            EnemyHealth h = go.GetComponent<EnemyHealth>();
            if (h != null) h.esTocho = true;
        });

        // Escupidor
        P2_ActualizarVariante(RUTA_ESCUPIDOR, "Lycan_Escupidor", "Escupidor", ctrl, baseClips, go =>
        {
            go.transform.localScale = new Vector3(1.45f, 1.45f, 1f);
            ZombieEnemy z = go.GetComponent<ZombieEnemy>();
            if (z != null) { z.speed = 2.1f; z.distanciaPreferida = 5.5f; z.margenDistancia = 1.3f; z.capaObstaculos = mascara; }
            EnemyHealth h = go.GetComponent<EnemyHealth>();
            if (h != null) { h.vidaMaxima = 3f; h.multiplicadorPuntos = 1.5f; h.esTocho = false; }
            EnemyAttack a = go.GetComponent<EnemyAttack>();
            if (a != null) a.enabled = false; // ataca a distancia
            AtaqueEscupidor e = go.GetComponent<AtaqueEscupidor>();
            if (e == null) e = go.AddComponent<AtaqueEscupidor>();
            e.spriteProyectil = circulo;
        });

        // Tóxico
        P2_ActualizarVariante(RUTA_TOXICO, "Lycan_Toxico", "Toxico", ctrl, baseClips, go =>
        {
            go.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
            ZombieEnemy z = go.GetComponent<ZombieEnemy>();
            if (z != null) { z.speed = 2.3f; z.capaObstaculos = mascara; }
            EnemyHealth h = go.GetComponent<EnemyHealth>();
            if (h != null) { h.vidaMaxima = 3f; h.multiplicadorPuntos = 1.4f; h.esTocho = false; h.colorDaño = new Color(0.4f, 1f, 0.3f, 1f); }
            EnemyAttack a = go.GetComponent<EnemyAttack>();
            if (a != null) { a.enabled = true; a.dañoAtaque = 15; }
            ZombieToxico t = go.GetComponent<ZombieToxico>();
            if (t == null) t = go.AddComponent<ZombieToxico>();
            t.spriteCharco = charco;
        });

        AssetDatabase.SaveAssets();
    }

    private class ClipsZombie { public AnimationClip idle, caminar, atacar; }

    private static Dictionary<string, Sprite> SpritesDeHoja(string ruta)
    {
        Dictionary<string, Sprite> d = new Dictionary<string, Sprite>();
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(ruta))
            if (o is Sprite s) d[s.name] = s;
        return d;
    }

    private static ClipsZombie P2_ClipsDeHoja(string rutaHoja, string sufijo)
    {
        Dictionary<string, Sprite> sp = SpritesDeHoja(rutaHoja);
        System.Func<int[], Sprite[]> F = idx =>
        {
            List<Sprite> l = new List<Sprite>();
            foreach (int i in idx) if (sp.TryGetValue("Lyccan_" + i, out Sprite s)) l.Add(s);
            return l.ToArray();
        };
        ClipsZombie c = new ClipsZombie();
        c.idle = CrearClipSprites(CARPETA_ANIM + "/Zombie_" + sufijo + "_Idle.anim", F(new[] { 19 }), 6f, true);
        c.caminar = CrearClipSprites(CARPETA_ANIM + "/Zombie_" + sufijo + "_Caminar.anim", F(new[] { 0, 1, 2, 3, 4 }), 9f, true);
        c.atacar = CrearClipSprites(CARPETA_ANIM + "/Zombie_" + sufijo + "_Atacar.anim", F(new[] { 10, 12, 13, 14 }), 10f, false);
        return c;
    }

    private static AnimationClip CrearClipSprites(string ruta, Sprite[] frames, float fps, bool loop)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ruta);
        bool nuevo = clip == null;
        if (nuevo) clip = new AnimationClip();
        clip.frameRate = fps;

        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        int extra = (loop && frames.Length > 1) ? 1 : 0;
        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[Mathf.Max(1, frames.Length + extra)];
        for (int i = 0; i < frames.Length; i++) keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[i] };
        if (extra == 1) keys[frames.Length] = new ObjectReferenceKeyframe { time = frames.Length / fps, value = frames[0] };
        if (frames.Length == 0) keys[0] = new ObjectReferenceKeyframe { time = 0f, value = null };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        AnimationClipSettings ajustes = AnimationUtility.GetAnimationClipSettings(clip);
        ajustes.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, ajustes);

        if (nuevo) AssetDatabase.CreateAsset(clip, ruta);
        else EditorUtility.SetDirty(clip);
        return clip;
    }

    private static void P2_AsignarMotions(AnimatorController ctrl, ClipsZombie c)
    {
        if (ctrl == null) return;
        foreach (ChildAnimatorState cs in ctrl.layers[0].stateMachine.states)
        {
            switch (cs.state.name)
            {
                case "Idle": cs.state.motion = c.idle; break;
                case "Caminar": cs.state.motion = c.caminar; break;
                case "Atacar": cs.state.motion = c.atacar; break;
                case "Aturdido": cs.state.motion = c.idle; break;
            }
        }
        EditorUtility.SetDirty(ctrl);
    }

    private static void P2_ActualizarVariante(string ruta, string nombre, string hoja, AnimatorController ctrl, ClipsZombie baseClips, System.Action<GameObject> extra)
    {
        string rutaHoja = CARPETA_SHEETS + "/Lycan_" + hoja + ".png";
        Dictionary<string, Sprite> sp = SpritesDeHoja(rutaHoja);
        RuntimeAnimatorController controlador = ctrl;

        if (sp.Count > 0)
        {
            ClipsZombie clips = P2_ClipsDeHoja(rutaHoja, hoja);
            string rutaAoc = CARPETA_ANIM + "/Zombie_" + hoja + ".overrideController";
            AnimatorOverrideController aoc = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(rutaAoc);
            if (aoc == null)
            {
                aoc = new AnimatorOverrideController(ctrl);
                AssetDatabase.CreateAsset(aoc, rutaAoc);
            }
            aoc.runtimeAnimatorController = ctrl;
            aoc[baseClips.idle] = clips.idle;
            aoc[baseClips.caminar] = clips.caminar;
            aoc[baseClips.atacar] = clips.atacar;
            EditorUtility.SetDirty(aoc);
            controlador = aoc;
        }

        CrearOActualizarVariante(ruta, nombre, go =>
        {
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = Color.white; // el color ahora viene de la hoja de sprites propia
                if (sp.TryGetValue("Lyccan_19", out Sprite s)) sr.sprite = s;
            }
            Animator a = go.GetComponent<Animator>();
            if (a == null) a = go.AddComponent<Animator>();
            a.runtimeAnimatorController = controlador;
            extra(go);
        });
    }

    private static void P2_AnimatorBryan()
    {
        AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(RUTA_PLAYER_CTRL);
        if (ctrl == null) return;
        AsegurarParametro(ctrl, "Recargar", AnimatorControllerParameterType.Trigger);
        AsegurarParametro(ctrl, "Recargando", AnimatorControllerParameterType.Bool);

        foreach (AnimatorControllerLayer l in ctrl.layers)
            if (l.name == "Recarga") { EditorUtility.SetDirty(ctrl); return; }

        // Capa propia para no alterar la locomoción existente
        ctrl.AddLayer("Recarga");
        AnimatorControllerLayer[] capas = ctrl.layers;
        AnimatorControllerLayer capa = capas[capas.Length - 1];
        capa.defaultWeight = 1f;
        AnimatorState sin = capa.stateMachine.AddState("SinRecarga", new Vector3(300, 0, 0));
        AnimatorState rec = capa.stateMachine.AddState("Recargando", new Vector3(300, 120, 0));
        capa.stateMachine.defaultState = sin;
        AnimatorStateTransition t1 = sin.AddTransition(rec); t1.hasExitTime = false; t1.duration = 0f; t1.AddCondition(AnimatorConditionMode.If, 0, "Recargando");
        AnimatorStateTransition t2 = rec.AddTransition(sin); t2.hasExitTime = false; t2.duration = 0f; t2.AddCondition(AnimatorConditionMode.IfNot, 0, "Recargando");
        ctrl.layers = capas;
        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();
    }

    // =====================================================================
    // CONFIGURACIÓN DE CADA STAGE (progresión, armas, granada, mira, pausa, oleadas)
    // =====================================================================

    private static void P2_ConfigurarStage(int n)
    {
        string escena = SceneManager.GetActiveScene().name;
        Sprite iconoEscopeta = AssetDatabase.LoadAssetAtPath<Sprite>(CARPETA_GEN + "/Icono_Escopeta.png");
        Sprite iconoSubfusil = AssetDatabase.LoadAssetAtPath<Sprite>(CARPETA_GEN + "/Icono_Subfusil.png");
        Sprite circulo = SpriteGenerado("UI_Circulo", 100f, false, () => TexturaCirculo(128, Color.white, 0.04f));

        // --- Asegura el HUD base del paquete 1 ---
        ConstruirHUDInterno();

        // --- ConfigStage en el GameManager ---
        MercenariesManager merc = Object.FindAnyObjectByType<MercenariesManager>(FindObjectsInactive.Include);
        if (merc != null)
        {
            ConfigStage cfg = merc.GetComponent<ConfigStage>();
            if (cfg == null) cfg = Undo.AddComponent<ConfigStage>(merc.gameObject);
            Undo.RecordObject(cfg, "ConfigStage");
            cfg.numeroStage = n;
            cfg.nombreVisible = n == 1 ? "STAGE 1 - HOSPITAL" : (n == 2 ? "STAGE 2 - LABORATORIO" : "STAGE 3 - RACCOON CITY");
            cfg.multiplicadorCantidad = n == 1 ? 1f : (n == 2 ? 1.15f : 1.3f);
            cfg.multiplicadorVelocidad = n == 1 ? 1f : (n == 2 ? 1.08f : 1.15f);
            cfg.multiplicadorVida = n == 1 ? 1f : (n == 2 ? 1.15f : 1.3f);
            cfg.tieneArmaPremio = n >= 2;
            cfg.armaPremio = n == 2 ? TipoArma.Escopeta : TipoArma.Subfusil;
            cfg.iconoArmaPremio = n == 2 ? iconoEscopeta : iconoSubfusil;
            cfg.spriteBrillo = circulo;
            EditorUtility.SetDirty(cfg);

            // --- 5 oleadas, tiempo y rangos por stage ---
            Undo.RecordObject(merc, "Partida 5 oleadas");
            merc.oleadasParaVictoria = 5;
            merc.tiempoRestante = n == 1 ? 150f : (n == 2 ? 180f : 200f);
            merc.umbralRangoS = n == 1 ? 260000 : (n == 2 ? 320000 : 400000);
            merc.umbralRangoA = n == 1 ? 160000 : (n == 2 ? 200000 : 250000);
            merc.umbralRangoB = n == 1 ? 80000 : (n == 2 ? 100000 : 125000);
            merc.escenaSiguiente = ProgresoJuego.EscenaSiguiente(escena);
            EditorUtility.SetDirty(merc);
        }

        // --- Oleadas con enemigos especiales progresivos ---
        WaveSpawner spawner = Object.FindAnyObjectByType<WaveSpawner>(FindObjectsInactive.Include);
        if (spawner != null)
        {
            GameObject corredor = AssetDatabase.LoadAssetAtPath<GameObject>(RUTA_CORREDOR);
            GameObject bruto = AssetDatabase.LoadAssetAtPath<GameObject>(RUTA_BRUTO);
            GameObject escupidor = AssetDatabase.LoadAssetAtPath<GameObject>(RUTA_ESCUPIDOR);
            GameObject toxico = AssetDatabase.LoadAssetAtPath<GameObject>(RUTA_TOXICO);

            Undo.RecordObject(spawner, "Oleadas paquete 2");
            spawner.tiposExtra = new List<WaveSpawner.TipoEnemigoOleada>();
            // oleadaMinima por stage: { Stage1, Stage2, Stage3 }
            AgregarTipo(spawner, "Corredor", corredor, new[] { 2, 1, 1 }[n - 1], 0.35f, 0.12f);
            AgregarTipo(spawner, "Escupidor", escupidor, new[] { 3, 3, 1 }[n - 1], 0.25f, 0.1f);
            AgregarTipo(spawner, "Toxico", toxico, new[] { 4, 2, 1 }[n - 1], 0.25f, 0.1f);
            AgregarTipo(spawner, "Tocho", bruto, new[] { 4, 2, 2 }[n - 1], 0.15f, 0.08f);
            spawner.enemigosInicialesPorOleada = n == 1 ? 5 : (n == 2 ? 6 : 7);
            spawner.incrementoPorOleada = n == 1 ? 3 : 4;
            spawner.velocidadExtraPorOleada = 0.06f;
            spawner.vidaExtraPorOleada = 0.08f;
            spawner.maxEnemigosEnEscena = Mathf.Max(spawner.maxEnemigosEnEscena, 12);
            spawner.tochoGarantizado = n >= 2 ? bruto : null;
            spawner.oleadaTochoGarantizado = n >= 2 ? 2 : 0;
            EditorUtility.SetDirty(spawner);
        }

        // --- Bryan: orden de armas (1 Pistola, 2 Escopeta, 3 Subfusil), recarga y granada ---
        PlayerMovement jugador = Object.FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Include);
        Canvas canvas = CanvasRaiz();
        HUDMercenariesController hud = Object.FindAnyObjectByType<HUDMercenariesController>(FindObjectsInactive.Include);
        if (jugador != null)
        {
            Undo.RecordObject(jugador, "Arsenal paquete 2");
            ArmaConfig p = BuscarArma(jugador, TipoArma.Pistola) ?? ArmaConfig.CrearPistola();
            ArmaConfig e = BuscarArma(jugador, TipoArma.Escopeta) ?? ArmaConfig.CrearEscopeta();
            ArmaConfig s = BuscarArma(jugador, TipoArma.Subfusil) ?? ArmaConfig.CrearSubfusil();
            p.tiempoRecarga = 1.2f; e.tiempoRecarga = 1.8f; s.tiempoRecarga = 1.5f;
            Sprite iconoPistola = p.icono != null ? p.icono : (hud != null && hud.iconosArmas != null && hud.iconosArmas.Length > 0 ? hud.iconosArmas[0] : null);
            p.icono = iconoPistola; e.icono = iconoEscopeta; s.icono = iconoSubfusil;
            jugador.armas = new ArmaConfig[] { p, e, s };
            EditorUtility.SetDirty(jugador);

            if (hud != null)
            {
                Undo.RecordObject(hud, "Iconos en orden");
                hud.iconosArmas = new Sprite[] { iconoPistola, iconoEscopeta, iconoSubfusil };
                EditorUtility.SetDirty(hud);
            }

            LanzadorGranadas lg = jugador.GetComponent<LanzadorGranadas>();
            if (lg == null) lg = Undo.AddComponent<LanzadorGranadas>(jugador.gameObject);
            Undo.RecordObject(lg, "Granada");
            lg.spriteGranada = SpriteGenerado("FX_Granada", 100f, false, TexturaGranada);
            lg.spriteExplosion = circulo;
            lg.capaObstaculos = 1 << AsegurarCapa(CAPA_OBSTACULOS);
            lg.cooldown = 30f;
            lg.tiempoDetonacion = 1.2f;
            EditorUtility.SetDirty(lg);
        }

        if (canvas != null)
        {
            // Limpia duplicados creados por error dentro del panel de Game Over
            if (merc != null && merc.panelGameOver != null)
            {
                foreach (string dup in new[] { "IndicadorGranada", "MiraCursor", "IndicadorDash" })
                {
                    Transform t = merc.panelGameOver.transform.Find(dup);
                    if (t != null) Undo.DestroyObjectImmediate(t.gameObject);
                }
            }
            TMP_FontAsset fuente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RUTA_FUENTE_CINZEL);
            P2_IndicadorGranada(canvas, fuente, circulo);
            P2_Mira(canvas);
            P2_MenuPausa(canvas, fuente, merc != null ? merc.GetComponent<ConfigStage>() : null);
        }

        P2_VincularAudio();
        MarcarEscenaSucia();
    }

    /// <summary>Canvas principal del HUD (raíz), nunca un sub-Canvas como el del panel de Game Over.</summary>
    private static Canvas CanvasRaiz()
    {
        Canvas mejor = null;
        foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        {
            if (c.transform.parent != null && c.transform.parent.GetComponentInParent<Canvas>(true) != null) continue; // anidado
            if (mejor == null || c.GetComponent<CanvasScaler>() != null) mejor = c;
            if (c.transform.Find("ContenedorArmaUI") != null) return c;
        }
        return mejor;
    }

    private static void AgregarTipo(WaveSpawner sp, string nombre, GameObject prefab, int minima, float peso, float extra)
    {
        if (prefab == null) return;
        sp.tiposExtra.Add(new WaveSpawner.TipoEnemigoOleada { nombre = nombre, prefab = prefab, oleadaMinima = minima, peso = peso, pesoExtraPorOleada = extra });
    }

    private static ArmaConfig BuscarArma(PlayerMovement j, TipoArma t)
    {
        if (j.armas == null) return null;
        foreach (ArmaConfig a in j.armas) if (a != null && a.tipo == t) return a;
        return null;
    }

    private static void P2_IndicadorGranada(Canvas canvas, TMP_FontAsset fuente, Sprite circulo)
    {
        Sprite anillo = GenerarSpriteAnilloHueco();
        GameObject raiz = ObtenerOCrearUI(canvas.transform, "IndicadorGranada", typeof(HUDGranadaIndicator));
        Colocar(raiz, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(225f, 120f), new Vector2(90f, 90f));

        GameObject fondo = ObtenerOCrearUI(raiz.transform, "Fondo", typeof(Image));
        Colocar(fondo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90f, 90f));
        Image f = fondo.GetComponent<Image>(); f.sprite = circulo; f.color = new Color(0f, 0f, 0f, 0.6f); f.raycastTarget = false;

        GameObject relleno = ObtenerOCrearUI(raiz.transform, "Relleno", typeof(Image));
        Colocar(relleno, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90f, 90f));
        Image r = relleno.GetComponent<Image>();
        r.sprite = anillo; r.type = Image.Type.Filled; r.fillMethod = Image.FillMethod.Radial360;
        r.fillOrigin = (int)Image.Origin360.Top; r.fillClockwise = true; r.fillAmount = 1f;
        r.color = new Color(1f, 0.6f, 0.15f, 1f); r.raycastTarget = false;

        GameObject centro = ObtenerOCrearUI(raiz.transform, "Centro", typeof(TextMeshProUGUI));
        ConfigurarTMP(centro.GetComponent<TextMeshProUGUI>(), fuente, "G", 30, Color.white, TextAlignmentOptions.Center);
        Colocar(centro, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90f, 40f));

        GameObject etiqueta = ObtenerOCrearUI(raiz.transform, "Etiqueta", typeof(TextMeshProUGUI));
        ConfigurarTMP(etiqueta.GetComponent<TextMeshProUGUI>(), fuente, "GRANADA  G / CLIC CENTRAL", 14, new Color(0.85f, 0.85f, 0.85f, 1f), TextAlignmentOptions.Center);
        Colocar(etiqueta, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(240f, 22f));

        HUDGranadaIndicator ind = raiz.GetComponent<HUDGranadaIndicator>();
        Undo.RecordObject(ind, "Indicador granada");
        ind.rellenoCooldown = r;
        ind.textoCentro = centro.GetComponent<TextMeshProUGUI>();
        EditorUtility.SetDirty(ind);
    }

    private static void P2_Mira(Canvas canvas)
    {
        Sprite mira = SpriteGenerado("UI_Mira", 100f, false, TexturaMira);
        Sprite anillo = GenerarSpriteAnilloHueco();

        GameObject raiz = ObtenerOCrearUI(canvas.transform, "MiraCursor", typeof(CanvasGroup), typeof(MiraCursor));
        Colocar(raiz, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(56f, 56f));
        CanvasGroup cg = raiz.GetComponent<CanvasGroup>();
        cg.blocksRaycasts = false; cg.interactable = false;

        GameObject ret = ObtenerOCrearUI(raiz.transform, "Reticula", typeof(Image));
        Colocar(ret, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(52f, 52f));
        Image imgRet = ret.GetComponent<Image>(); imgRet.sprite = mira; imgRet.raycastTarget = false; imgRet.color = new Color(1f, 1f, 1f, 0.9f);

        GameObject arco = ObtenerOCrearUI(raiz.transform, "ArcoRecarga", typeof(Image));
        Colocar(arco, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(74f, 74f));
        Image imgArco = arco.GetComponent<Image>();
        imgArco.sprite = anillo; imgArco.type = Image.Type.Filled; imgArco.fillMethod = Image.FillMethod.Radial360;
        imgArco.fillOrigin = (int)Image.Origin360.Top; imgArco.fillClockwise = true; imgArco.fillAmount = 0f;
        imgArco.color = new Color(1f, 0.82f, 0.25f, 0.95f); imgArco.raycastTarget = false; imgArco.enabled = false;

        MiraCursor mc = raiz.GetComponent<MiraCursor>();
        Undo.RecordObject(mc, "Mira");
        mc.reticula = imgRet;
        mc.arcoRecarga = imgArco;
        EditorUtility.SetDirty(mc);
        raiz.transform.SetAsLastSibling();
    }

    // ---------------------------------------------------------------------
    // MENÚ DE PAUSA ESTILO RE4 REMAKE
    // ---------------------------------------------------------------------
    private static void P2_MenuPausa(Canvas canvas, TMP_FontAsset fuente, ConfigStage cfg)
    {
        PauseManager pm = Object.FindAnyObjectByType<PauseManager>(FindObjectsInactive.Include);
        if (pm == null) return;
        Sprite blanco = SpriteGenerado("UI_Blanco", 100f, false, () => TexturaSolida(8, 8, Color.white));
        Sprite degradado = SpriteGenerado("UI_DegradadoH", 100f, false, TexturaDegradadoH);
        Sprite circulo = SpriteGenerado("UI_Circulo", 100f, false, () => TexturaCirculo(128, Color.white, 0.04f));

        GameObject panel = pm.pausePanel;
        if (panel == null)
        {
            Transform t = canvas.transform.Find("PanelPausa");
            panel = t != null ? t.gameObject : ObtenerOCrearUI(canvas.transform, "PanelPausa", typeof(Image));
        }
        RectTransform rp = panel.GetComponent<RectTransform>();
        Undo.RecordObject(rp, "Pausa");
        rp.anchorMin = Vector2.zero; rp.anchorMax = Vector2.one; rp.offsetMin = Vector2.zero; rp.offsetMax = Vector2.zero; rp.localScale = Vector3.one;
        Image fondo = panel.GetComponent<Image>();
        if (fondo == null) fondo = Undo.AddComponent<Image>(panel);
        Undo.RecordObject(fondo, "Fondo pausa");
        fondo.sprite = blanco; fondo.color = new Color(0f, 0f, 0f, 0.72f); fondo.raycastTarget = true;

        // Botones antiguos fuera
        foreach (string viejo in new[] { "Reanudar", "MenuPrincipal" })
        {
            Transform t = panel.transform.Find(viejo);
            if (t != null && t.gameObject.activeSelf) { Undo.RecordObject(t.gameObject, "Ocultar viejo"); t.gameObject.SetActive(false); }
        }

        // Columna izquierda con degradado
        GameObject col = ObtenerOCrearUI(panel.transform, "ColumnaIzquierda", typeof(Image));
        RectTransform rc = col.GetComponent<RectTransform>();
        Undo.RecordObject(rc, "Columna");
        rc.anchorMin = new Vector2(0f, 0f); rc.anchorMax = new Vector2(0f, 1f); rc.pivot = new Vector2(0f, 0.5f);
        rc.anchoredPosition = Vector2.zero; rc.sizeDelta = new Vector2(760f, 0f); rc.localScale = Vector3.one;
        Image imgCol = col.GetComponent<Image>(); imgCol.sprite = degradado; imgCol.color = new Color(0.02f, 0.02f, 0.02f, 0.95f); imgCol.raycastTarget = false;
        col.transform.SetAsFirstSibling();

        GameObject linea = ObtenerOCrearUI(panel.transform, "LineaRoja", typeof(Image));
        Colocar(linea, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(92f, 40f), new Vector2(2f, 470f));
        Image il = linea.GetComponent<Image>(); il.sprite = blanco; il.color = new Color(0.65f, 0.08f, 0.06f, 0.85f); il.raycastTarget = false;

        GameObject titulo = ObtenerOCrearUI(panel.transform, "TituloPausa", typeof(TextMeshProUGUI));
        ConfigurarTMP(titulo.GetComponent<TextMeshProUGUI>(), fuente, "PAUSA", 58, new Color(1f, 1f, 1f, 0.95f), TextAlignmentOptions.Left);
        Colocar(titulo, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(118f, 240f), new Vector2(560f, 70f));
        titulo.GetComponent<TextMeshProUGUI>().characterSpacing = 12f;

        GameObject sub = ObtenerOCrearUI(panel.transform, "SubtituloPausa", typeof(TextMeshProUGUI));
        ConfigurarTMP(sub.GetComponent<TextMeshProUGUI>(), fuente, cfg != null ? cfg.nombreVisible : "MERCENARIES", 18, new Color(0.75f, 0.12f, 0.1f, 1f), TextAlignmentOptions.Left);
        Colocar(sub, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(122f, 192f), new Vector2(560f, 26f));

        // Opciones
        GameObject cont = ObtenerOCrearUI(panel.transform, "Opciones");
        Colocar(cont, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(118f, -10f), new Vector2(520f, 320f));

        GameObject fondoSel = ObtenerOCrearUI(cont.transform, "FondoSeleccion", typeof(Image));
        Colocar(fondoSel, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-10f, 105f), new Vector2(470f, 52f));
        Image ifs = fondoSel.GetComponent<Image>(); ifs.sprite = degradado; ifs.color = new Color(1f, 1f, 1f, 0.09f); ifs.raycastTarget = false;
        fondoSel.transform.SetAsFirstSibling();

        string[] etiquetas = { "CONTINUAR", "REINICIAR", "VOLUMEN / OPCIONES", "SALIR AL MENÚ" };
        RectTransform[] filas = new RectTransform[etiquetas.Length];
        for (int i = 0; i < etiquetas.Length; i++)
        {
            GameObject fila = ObtenerOCrearUI(cont.transform, "Opcion_" + i, typeof(Image), typeof(OpcionMenuPausa));
            filas[i] = Colocar(fila, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 105f - i * 70f), new Vector2(460f, 52f));
            Image fi = fila.GetComponent<Image>(); fi.sprite = blanco; fi.color = new Color(0f, 0f, 0f, 0f); fi.raycastTarget = true;
            OpcionMenuPausa op = fila.GetComponent<OpcionMenuPausa>();
            Undo.RecordObject(op, "Opción");
            op.pause = pm; op.indice = i; op.esSubmenu = false;
            EditorUtility.SetDirty(op);

            GameObject txt = ObtenerOCrearUI(fila.transform, "Texto", typeof(TextMeshProUGUI));
            ConfigurarTMP(txt.GetComponent<TextMeshProUGUI>(), fuente, etiquetas[i], 32, new Color(0.62f, 0.62f, 0.62f, 1f), TextAlignmentOptions.Left);
            Colocar(txt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(440f, 52f));
            txt.GetComponent<TextMeshProUGUI>().characterSpacing = 4f;
        }

        GameObject barra = ObtenerOCrearUI(cont.transform, "BarraSeleccion", typeof(Image));
        Colocar(barra, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-22f, 105f), new Vector2(6f, 40f));
        Image ib = barra.GetComponent<Image>(); ib.sprite = blanco; ib.color = new Color(0.8f, 0.1f, 0.08f, 1f); ib.raycastTarget = false;

        GameObject ayuda = ObtenerOCrearUI(panel.transform, "AyudaTeclas", typeof(TextMeshProUGUI));
        ConfigurarTMP(ayuda.GetComponent<TextMeshProUGUI>(), fuente, "W / S  NAVEGAR      ENTER  ACEPTAR      ESC  VOLVER", 16, new Color(0.7f, 0.7f, 0.7f, 0.8f), TextAlignmentOptions.Left);
        Colocar(ayuda, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(118f, -230f), new Vector2(900f, 26f));

        // Submenú VOLUMEN / OPCIONES
        GameObject po = ObtenerOCrearUI(panel.transform, "PanelOpciones", typeof(Image));
        Colocar(po, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(700f, -10f), new Vector2(720f, 380f));
        Image ipo = po.GetComponent<Image>(); ipo.sprite = blanco; ipo.color = new Color(0.03f, 0.03f, 0.03f, 0.92f); ipo.raycastTarget = true;

        GameObject tituloOp = ObtenerOCrearUI(po.transform, "Titulo", typeof(TextMeshProUGUI));
        ConfigurarTMP(tituloOp.GetComponent<TextMeshProUGUI>(), fuente, "VOLUMEN / OPCIONES", 28, Color.white, TextAlignmentOptions.Left);
        Colocar(tituloOp, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(50f, -26f), new Vector2(600f, 40f));

        string[] nombresOp = { "GENERAL", "MÚSICA", "EFECTOS", "VOLVER" };
        RectTransform[] filasOp = new RectTransform[4];
        Slider[] sliders = new Slider[3];
        for (int i = 0; i < 4; i++)
        {
            GameObject fila = ObtenerOCrearUI(po.transform, "Fila_" + i, typeof(Image), typeof(OpcionMenuPausa));
            filasOp[i] = Colocar(fila, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(50f, 80f - i * 66f), new Vector2(620f, 50f));
            Image fi = fila.GetComponent<Image>(); fi.sprite = blanco; fi.color = new Color(0f, 0f, 0f, 0f); fi.raycastTarget = true;
            OpcionMenuPausa op = fila.GetComponent<OpcionMenuPausa>();
            Undo.RecordObject(op, "Opción");
            op.pause = pm; op.indice = i; op.esSubmenu = true;
            EditorUtility.SetDirty(op);

            GameObject txt = ObtenerOCrearUI(fila.transform, "Texto", typeof(TextMeshProUGUI));
            ConfigurarTMP(txt.GetComponent<TextMeshProUGUI>(), fuente, nombresOp[i], 24, new Color(0.62f, 0.62f, 0.62f, 1f), TextAlignmentOptions.Left);
            Colocar(txt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(220f, 50f));

            if (i < 3) sliders[i] = P2_Slider(fila.transform, blanco, circulo);
        }

        GameObject barraOp = ObtenerOCrearUI(po.transform, "BarraSeleccion", typeof(Image));
        Colocar(barraOp, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, 80f), new Vector2(5f, 36f));
        Image ibo = barraOp.GetComponent<Image>(); ibo.sprite = blanco; ibo.color = new Color(0.8f, 0.1f, 0.08f, 1f); ibo.raycastTarget = false;
        po.SetActive(false);

        Undo.RecordObject(pm, "Pausa RE4");
        pm.pausePanel = panel;
        pm.opciones = filas;
        pm.barraSeleccion = barra.GetComponent<RectTransform>();
        pm.fondoSeleccion = fondoSel.GetComponent<RectTransform>();
        pm.panelOpciones = po;
        pm.filasOpciones = filasOp;
        pm.barraSeleccionOpciones = barraOp.GetComponent<RectTransform>();
        pm.sliderGeneral = sliders[0];
        pm.sliderMusica = sliders[1];
        pm.sliderEfectos = sliders[2];
        if (pm.scriptJugador == null) pm.scriptJugador = Object.FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Include);
        EditorUtility.SetDirty(pm);

        panel.transform.SetAsLastSibling();
        if (panel.activeSelf) { Undo.RecordObject(panel, "Ocultar pausa"); panel.SetActive(false); }
    }

    private static Slider P2_Slider(Transform fila, Sprite blanco, Sprite circulo)
    {
        Transform existente = fila.Find("Slider");
        GameObject go;
        if (existente != null) go = existente.gameObject;
        else
        {
            DefaultControls.Resources res = new DefaultControls.Resources { standard = blanco, background = blanco, knob = circulo };
            go = DefaultControls.CreateSlider(res);
            go.name = "Slider";
            go.transform.SetParent(fila, false);
            Undo.RegisterCreatedObjectUndo(go, "Crear slider");
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = LayerMask.NameToLayer("UI");
        }
        Colocar(go, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(250f, 0f), new Vector2(340f, 18f));

        Slider s = go.GetComponent<Slider>();
        s.minValue = 0f; s.maxValue = 1f; s.value = 1f;
        Navigation nav = s.navigation; nav.mode = Navigation.Mode.None; s.navigation = nav;

        Transform bg = go.transform.Find("Background");
        if (bg != null) { Image i = bg.GetComponent<Image>(); i.sprite = blanco; i.type = Image.Type.Simple; i.color = new Color(0.22f, 0.22f, 0.22f, 1f); }
        Transform fill = go.transform.Find("Fill Area/Fill");
        if (fill != null) { Image i = fill.GetComponent<Image>(); i.sprite = blanco; i.type = Image.Type.Simple; i.color = new Color(0.78f, 0.1f, 0.08f, 1f); }
        Transform handle = go.transform.Find("Handle Slide Area/Handle");
        if (handle != null) { Image i = handle.GetComponent<Image>(); i.sprite = circulo; i.color = Color.white; }
        EditorUtility.SetDirty(s);
        return s;
    }

    // =====================================================================
    // MENÚ PRINCIPAL
    // =====================================================================

    private static void P2_ConstruirMenuPrincipal()
    {
        TMP_FontAsset fuente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RUTA_FUENTE_CINZEL);
        Sprite blanco = SpriteGenerado("UI_Blanco", 100f, false, () => TexturaSolida(8, 8, Color.white));
        MainMenuManager mm = Object.FindAnyObjectByType<MainMenuManager>(FindObjectsInactive.Include);
        Canvas canvas = mm != null ? mm.GetComponentInParent<Canvas>().rootCanvas : CanvasRaiz();
        if (mm == null || canvas == null) { Debug.LogWarning("[Biohazard P2] MainMenu sin MainMenuManager o Canvas."); return; }

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            Undo.RecordObject(scaler, "Scaler");
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        // Título
        Transform tTitulo = canvas.transform.Find("TitleText");
        GameObject titulo = tTitulo != null ? tTitulo.gameObject : ObtenerOCrearUI(canvas.transform, "TitleText", typeof(TextMeshProUGUI));
        TextMeshProUGUI tmpTitulo = titulo.GetComponent<TextMeshProUGUI>();
        ConfigurarTMP(tmpTitulo, fuente, "BIOHAZARD: CODE ZERO", 104, new Color(0.8f, 0.07f, 0.05f, 1f), TextAlignmentOptions.Center);
        tmpTitulo.characterSpacing = 6f;
        Colocar(titulo, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -200f), new Vector2(1800f, 150f));

        GameObject subt = ObtenerOCrearUI(canvas.transform, "Subtitulo", typeof(TextMeshProUGUI));
        ConfigurarTMP(subt.GetComponent<TextMeshProUGUI>(), fuente, "MERCENARIES  -  SURVIVAL HORROR", 26, new Color(0.85f, 0.85f, 0.85f, 0.8f), TextAlignmentOptions.Center);
        subt.GetComponent<TextMeshProUGUI>().characterSpacing = 10f;
        Colocar(subt, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -290f), new Vector2(1400f, 40f));

        // Contenedor de botones
        Transform cont = mm.transform;
        RectTransform rCont = cont as RectTransform;
        if (rCont != null)
        {
            Undo.RecordObject(rCont, "Menú");
            rCont.anchorMin = rCont.anchorMax = new Vector2(0.5f, 0.5f);
            rCont.pivot = new Vector2(0.5f, 0.5f);
            rCont.anchoredPosition = new Vector2(0f, -150f);
            rCont.sizeDelta = new Vector2(620f, 5 * 64f + 4 * 16f);
        }
        VerticalLayoutGroup vlg = cont.GetComponent<VerticalLayoutGroup>();
        if (vlg != null)
        {
            Undo.RecordObject(vlg, "Layout menú");
            vlg.spacing = 16f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
        }

        Transform tContinue = cont.Find("ContinueButton");
        Transform tStage = cont.Find("StageSelectButton");
        if (tStage == null && tContinue != null)
        {
            GameObject clon = Object.Instantiate(tContinue.gameObject, cont);
            clon.name = "StageSelectButton";
            Undo.RegisterCreatedObjectUndo(clon, "Botón selector");
            clon.transform.SetSiblingIndex(tContinue.GetSiblingIndex() + 1);
            tStage = clon.transform;
        }

        P2_EstilizarBotonMenu(cont.Find("NewGameButton"), "NUEVA PARTIDA", fuente);
        P2_EstilizarBotonMenu(tContinue, "CONTINUAR", fuente);
        P2_EstilizarBotonMenu(tStage, "SELECCIONAR STAGE", fuente);
        P2_EstilizarBotonMenu(cont.Find("OptionsButton"), "CRÉDITOS", fuente);
        P2_EstilizarBotonMenu(cont.Find("ExitButton"), "SALIR", fuente);

        if (tStage != null) AsignarClick(tStage.gameObject, mm.AbrirSelectorStages);

        // Panel del selector de Stage
        GameObject panel = ObtenerOCrearUI(canvas.transform, "PanelSeleccionStage", typeof(Image));
        RectTransform rp = panel.GetComponent<RectTransform>();
        Undo.RecordObject(rp, "Panel stages");
        rp.anchorMin = Vector2.zero; rp.anchorMax = Vector2.one; rp.offsetMin = Vector2.zero; rp.offsetMax = Vector2.zero; rp.localScale = Vector3.one;
        Image ip = panel.GetComponent<Image>(); ip.sprite = blanco; ip.color = new Color(0f, 0f, 0f, 0.9f); ip.raycastTarget = true;

        GameObject tp = ObtenerOCrearUI(panel.transform, "Titulo", typeof(TextMeshProUGUI));
        ConfigurarTMP(tp.GetComponent<TextMeshProUGUI>(), fuente, "SELECCIONAR STAGE", 60, new Color(0.8f, 0.07f, 0.05f, 1f), TextAlignmentOptions.Center);
        Colocar(tp, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 330f), new Vector2(1400f, 90f));

        string[] nombres = { "STAGE 1  -  HOSPITAL", "STAGE 2  -  LABORATORIO", "STAGE 3  -  RACCOON CITY" };
        UnityAction[] acciones = { mm.JugarStage1, mm.JugarStage2, mm.JugarStage3 };
        Button[] botones = new Button[3];
        TextMeshProUGUI[] infos = new TextMeshProUGUI[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject b = CrearBoton(panel.transform, "BotonStage" + (i + 1), nombres[i], new Vector2(0f, 160f - i * 140f), fuente, blanco);
            Colocar(b, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 160f - i * 140f), new Vector2(760f, 110f));
            Transform lbl = b.transform.Find("Texto");
            if (lbl != null)
            {
                Colocar(lbl.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 14f), new Vector2(760f, 50f));
                lbl.GetComponent<TextMeshProUGUI>().fontSize = 34;
            }
            GameObject info = ObtenerOCrearUI(b.transform, "Info", typeof(TextMeshProUGUI));
            ConfigurarTMP(info.GetComponent<TextMeshProUGUI>(), fuente, "SIN RÉCORD", 18, new Color(1f, 0.85f, 0.35f, 1f), TextAlignmentOptions.Center);
            Colocar(info, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -28f), new Vector2(760f, 28f));
            AsignarClick(b, acciones[i]);
            botones[i] = b.GetComponent<Button>();
            infos[i] = info.GetComponent<TextMeshProUGUI>();
        }
        GameObject volver = CrearBoton(panel.transform, "BotonVolver", "VOLVER", new Vector2(0f, -320f), fuente, blanco);
        AsignarClick(volver, mm.CerrarSelectorStages);
        panel.transform.SetAsLastSibling();
        panel.SetActive(false);

        Undo.RecordObject(mm, "MainMenuManager");
        if (tContinue != null)
        {
            mm.continueButton = tContinue.GetComponent<Button>();
            mm.continueText = tContinue.GetComponentInChildren<TextMeshProUGUI>(true);
        }
        if (tStage != null)
        {
            mm.botonSeleccionStage = tStage.GetComponent<Button>();
            mm.textoSeleccionStage = tStage.GetComponentInChildren<TextMeshProUGUI>(true);
        }
        mm.panelSeleccionStage = panel;
        mm.botonesStage = botones;
        mm.textosInfoStage = infos;
        EditorUtility.SetDirty(mm);
        MarcarEscenaSucia();
    }

    private static void P2_EstilizarBotonMenu(Transform boton, string texto, TMP_FontAsset fuente)
    {
        if (boton == null) return;
        TextMeshProUGUI tmp = boton.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null)
        {
            Undo.RecordObject(tmp, "Botón menú");
            if (fuente != null) tmp.font = fuente;
            tmp.text = texto;
            tmp.fontSize = 36;
            tmp.fontStyle = FontStyles.Bold;
            tmp.characterSpacing = 4f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.rectTransform.sizeDelta = new Vector2(600f, 56f);
            EditorUtility.SetDirty(tmp);
        }
        LayoutElement le = boton.GetComponent<LayoutElement>();
        if (le == null) le = Undo.AddComponent<LayoutElement>(boton.gameObject);
        le.preferredHeight = 64f;
        le.preferredWidth = 600f;
    }

    // =====================================================================
    // CRÉDITOS
    // =====================================================================

    private static void P2_ConstruirCreditos()
    {
        TMP_FontAsset fuente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RUTA_FUENTE_CINZEL);
        GameObject intro = BuscarEnEscena("IntroText");
        if (intro == null) { Debug.LogWarning("[Biohazard P2] No se encontró IntroText en Créditos."); return; }
        Canvas canvas = intro.GetComponentInParent<Canvas>();

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            Undo.RecordObject(scaler, "Scaler");
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        // Fondo negro a pantalla completa
        Transform bg = canvas.transform.Find("BlackBackground");
        if (bg != null)
        {
            RectTransform r = (RectTransform)bg;
            Undo.RecordObject(r, "Fondo");
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero; r.localScale = Vector3.one;
            foreach (Transform h in bg)
            {
                RectTransform rh = h as RectTransform;
                if (rh == null) continue;
                Undo.RecordObject(rh, "Efecto");
                rh.anchorMin = Vector2.zero; rh.anchorMax = Vector2.one; rh.offsetMin = Vector2.zero; rh.offsetMax = Vector2.zero;
            }
        }

        // Ocultar imágenes sueltas antiguas (logo FI gigante y divisor)
        Sprite spriteFI = null, spriteUAT = null;
        foreach (Transform h in canvas.transform)
        {
            if (h.name != "Image") continue;
            Image img = h.GetComponent<Image>();
            if (img != null && img.sprite != null && img.sprite.texture != null && img.sprite.texture.name.Contains("fians")) spriteFI = img.sprite;
            if (h.gameObject.activeSelf) { Undo.RecordObject(h.gameObject, "Ocultar"); h.gameObject.SetActive(false); }
        }
        // fians.png y el escudo vienen cortados en muchos pedazos: se usan copias limpias de una sola pieza
        Sprite fiLimpio = SpriteSimple("Assets/Sprites/Logo_FI.png");
        Sprite uatLimpio = SpriteSimple("Assets/Sprites/Logo_UAT.png");
        spriteFI = fiLimpio != null ? fiLimpio : (spriteFI != null ? spriteFI : PrimerSprite("Assets/Prefabs/fians.png"));
        spriteUAT = uatLimpio != null ? uatLimpio : PrimerSprite("Assets/Prefabs/Escudo_UAT_blanco.png");

        // Contenedor centrado con VerticalLayoutGroup
        GameObject cont = ObtenerOCrearUI(canvas.transform, "ContenidoCreditos", typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(CanvasGroup));
        Colocar(cont, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(1500f, 900f));
        VerticalLayoutGroup vlg = cont.GetComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.spacing = 34f;
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = false; vlg.childForceExpandHeight = false;
        ContentSizeFitter csf = cont.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // 1. Título
        GameObject titulo = ObtenerOCrearUI(cont.transform, "Titulo", typeof(TextMeshProUGUI), typeof(LayoutElement));
        ConfigurarTMP(titulo.GetComponent<TextMeshProUGUI>(), fuente, "BIOHAZARD: CODE ZERO", 80, new Color(0.8f, 0.07f, 0.05f, 1f), TextAlignmentOptions.Center);
        titulo.GetComponent<TextMeshProUGUI>().characterSpacing = 5f;
        titulo.GetComponent<LayoutElement>().preferredWidth = 1500f;
        titulo.transform.SetSiblingIndex(0);

        // 2. Texto de créditos (se reutiliza IntroText)
        if (intro.transform.parent != cont.transform) Undo.SetTransformParent(intro.transform, cont.transform, "Mover créditos");
        TextMeshProUGUI tmp = intro.GetComponent<TextMeshProUGUI>();
        Undo.RecordObject(tmp, "Texto créditos");
        string texto = tmp.text.Replace("\r", "");
        if (texto.StartsWith("BIOHAZARD: CODE ZERO\n")) texto = texto.Substring("BIOHAZARD: CODE ZERO\n".Length);
        while (texto.Contains("\n\n\n")) texto = texto.Replace("\n\n\n", "\n\n");
        tmp.text = texto.Trim('\n');
        if (fuente != null) tmp.font = fuente;
        tmp.fontSize = 30;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.lineSpacing = 8f;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        RectTransform ri = tmp.rectTransform;
        ri.anchorMin = ri.anchorMax = new Vector2(0.5f, 0.5f); ri.pivot = new Vector2(0.5f, 0.5f); ri.localScale = Vector3.one;
        LayoutElement leTexto = intro.GetComponent<LayoutElement>();
        if (leTexto == null) leTexto = Undo.AddComponent<LayoutElement>(intro);
        leTexto.preferredWidth = 1300f;
        intro.transform.SetSiblingIndex(1);
        EditorUtility.SetDirty(tmp);

        // 3. Fila de logos (UAT + Facultad de Ingeniería)
        GameObject logos = ObtenerOCrearUI(cont.transform, "Logos", typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        HorizontalLayoutGroup hlg = logos.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 70f; hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true; hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
        LayoutElement leLogos = logos.GetComponent<LayoutElement>();
        leLogos.preferredHeight = 180f; leLogos.preferredWidth = 900f;
        logos.transform.SetSiblingIndex(2);

        GameObject uat = ObtenerOCrearUI(logos.transform, "LogoUAT", typeof(Image), typeof(LayoutElement));
        Image iu = uat.GetComponent<Image>(); iu.sprite = spriteUAT; iu.preserveAspect = true; iu.raycastTarget = false; iu.color = Color.white;
        LayoutElement leU = uat.GetComponent<LayoutElement>(); leU.preferredWidth = 200f; leU.preferredHeight = 176f;

        GameObject marco = ObtenerOCrearUI(logos.transform, "LogoFI_Marco", typeof(RectMask2D), typeof(LayoutElement));
        LayoutElement leM = marco.GetComponent<LayoutElement>(); leM.preferredWidth = 560f; leM.preferredHeight = 150f;
        GameObject fi = ObtenerOCrearUI(marco.transform, "LogoFI", typeof(Image));
        float altoFI = spriteFI != null && spriteFI.rect.width > 0 ? 560f * spriteFI.rect.height / spriteFI.rect.width : 150f;
        Colocar(fi, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, Mathf.Min(150f, altoFI)));
        Image ifi = fi.GetComponent<Image>(); ifi.sprite = spriteFI; ifi.preserveAspect = true; ifi.raycastTarget = false;

        // Botón VOLVER abajo a la derecha
        Transform back = canvas.transform.Find("Back");
        if (back != null)
        {
            RectTransform rb = (RectTransform)back;
            Undo.RecordObject(rb, "Volver");
            rb.anchorMin = rb.anchorMax = new Vector2(1f, 0f); rb.pivot = new Vector2(1f, 0f);
            rb.anchoredPosition = new Vector2(-90f, 60f); rb.sizeDelta = new Vector2(260f, 70f); rb.localScale = Vector3.one;
            TextMeshProUGUI tb = back.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tb != null) ConfigurarTMP(tb, fuente, "VOLVER", 32, tb.color, TextAlignmentOptions.Center);
            back.SetAsLastSibling();
        }

        CreditsController cc = Object.FindAnyObjectByType<CreditsController>(FindObjectsInactive.Include);
        if (cc != null)
        {
            Undo.RecordObject(cc, "Créditos fijos");
            cc.desplazamientoAutomatico = false;
            cc.contenedorCreditos = cont.GetComponent<RectTransform>();
            cc.grupoCreditos = cont.GetComponent<CanvasGroup>();
            EditorUtility.SetDirty(cc);
        }
        MarcarEscenaSucia();
    }

    /// <summary>Importa (si hace falta) una textura como Sprite de una sola pieza y la devuelve.</summary>
    private static Sprite SpriteSimple(string ruta)
    {
        TextureImporter imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (imp == null) return null;
        if (imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single || !imp.alphaIsTransparency)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }

    private static Sprite PrimerSprite(string ruta)
    {
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(ruta))
            if (o is Sprite s) return s;
        return null;
    }

    // =====================================================================
    // STAGE 3 - LABORATORIO DE BIOCONTENCIÓN
    // =====================================================================

    private static void P2_CrearStage3()
    {
        P3_RenombrarStage3(); // Stage3_Laboratorio -> Stage3_RaccoonCity (conserva el GUID)
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RUTA_STAGE3) == null)
        {
            if (!AssetDatabase.CopyAsset(RUTA_STAGE2, RUTA_STAGE3)) { Debug.LogError("[Biohazard] No se pudo copiar Stage 2."); return; }
            AssetDatabase.Refresh();
        }
        EditorSceneManager.OpenScene(RUTA_STAGE3, OpenSceneMode.Single);
        P3_LayoutRaccoonCity(); // Paquete 3: calles de Raccoon City de noche
    }

    // =====================================================================
    // AUDIO PAQUETE 2
    // =====================================================================

    private static void P2_GenerarAudio()
    {
        int sr = 44100;
        ObtenerOGenerarClip("Assets/Audio/sfx_explosion.wav", () => CrearWavData(SintetizarExplosion(sr), sr), true);
        ObtenerOGenerarClip("Assets/Audio/sfx_lanzar_granada.wav", () => CrearWavData(SintetizarLanzarGranada(sr), sr), true);
        ObtenerOGenerarClip("Assets/Audio/sfx_escupitajo.wav", () => CrearWavData(SintetizarEscupitajo(sr), sr), true);
        ObtenerOGenerarClip("Assets/Audio/sfx_acido.wav", () => CrearWavData(SintetizarAcido(sr), sr), true);
        ObtenerOGenerarClip("Assets/Audio/sfx_pickup_arma.wav", () => CrearWavData(SintetizarPickupArma(sr), sr), true);
    }

    private static void P2_VincularAudio()
    {
        AudioManager am = Object.FindAnyObjectByType<AudioManager>(FindObjectsInactive.Include);
        if (am == null) return;
        Undo.RecordObject(am, "Audio paquete 2");
        am.sfxExplosion = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_explosion.wav");
        am.sfxLanzarGranada = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_lanzar_granada.wav");
        am.sfxEscupitajo = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_escupitajo.wav");
        am.sfxAcido = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_acido.wav");
        am.sfxPickupArma = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_pickup_arma.wav");
        EditorUtility.SetDirty(am);
    }

    private static float[] SintetizarExplosion(int sr)
    {
        System.Random rnd = new System.Random(21);
        float dur = 1.1f;
        int n = (int)(sr * dur);
        float[] s = new float[n];
        float lp1 = 0f, lp2 = 0f, prev = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / sr;
            float ruido = (float)(rnd.NextDouble() * 2.0 - 1.0);
            float golpe = (ruido - prev) * Mathf.Exp(-t * 60f) * 1.2f;
            prev = ruido;
            lp1 += (ruido - lp1) * 0.05f;
            lp2 += (lp1 - lp2) * 0.05f;
            float retumbo = lp2 * Mathf.Exp(-t * 3.2f) * 9f;
            float sub = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(48f, 28f, t / dur) * t) * Mathf.Exp(-t * 4f) * 1.2f;
            float chispas = (rnd.NextDouble() > 0.9985 ? (float)(rnd.NextDouble() * 2 - 1) : 0f) * Mathf.Exp(-t * 2f);
            s[i] = (float)System.Math.Tanh((golpe + ruido * Mathf.Exp(-t * 9f) * 0.7f + retumbo + sub + chispas) * 2.2f);
        }
        Normalizar(s, 0.95f);
        AplicarFades(s, sr, 0.0005f, 0.15f);
        return s;
    }

    private static float[] SintetizarLanzarGranada(int sr)
    {
        System.Random rnd = new System.Random(22);
        float dur = 0.32f;
        int n = (int)(sr * dur);
        float[] s = new float[n];
        float[,] st = new float[1, 4];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / sr;
            float ruido = (float)(rnd.NextDouble() * 2.0 - 1.0);
            float v = 0f;
            if (t < 0.05f) v += (Mathf.Sin(2f * Mathf.PI * 3200f * t) * 0.6f + ruido * 0.3f) * Mathf.Exp(-t * 80f); // seguro (clink)
            float tn = Mathf.Clamp01((t - 0.05f) / 0.27f);
            if (t >= 0.05f) v += Resonar(st, 0, ruido, Mathf.Lerp(800f, 1800f, tn), 1.8f, sr) * Mathf.Sin(Mathf.PI * tn) * 0.8f;
            s[i] = v;
        }
        Normalizar(s, 0.6f);
        return s;
    }

    private static float[] SintetizarEscupitajo(int sr)
    {
        System.Random rnd = new System.Random(23);
        float dur = 0.3f;
        int n = (int)(sr * dur);
        float[] s = new float[n];
        float[,] st = new float[1, 4];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / sr;
            float ruido = (float)(rnd.NextDouble() * 2.0 - 1.0);
            float burbuja = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(600f, 180f, t / dur) * t) * Mathf.Exp(-t * 14f) * 0.5f;
            float flema = Resonar(st, 0, ruido, Mathf.Lerp(1400f, 500f, t / dur), 3f, sr) * Mathf.Exp(-t * 10f) * 1.5f;
            s[i] = burbuja + flema;
        }
        Normalizar(s, 0.75f);
        AplicarFades(s, sr, 0.002f, 0.03f);
        return s;
    }

    private static float[] SintetizarAcido(int sr)
    {
        System.Random rnd = new System.Random(24);
        float dur = 0.6f;
        int n = (int)(sr * dur);
        float[] s = new float[n];
        float prev = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / sr;
            float ruido = (float)(rnd.NextDouble() * 2.0 - 1.0);
            float agudo = (ruido - prev) * 0.6f; // pasa-altos: siseo
            prev = ruido;
            float crepitar = rnd.NextDouble() > 0.995 ? (float)(rnd.NextDouble() * 2 - 1) * 0.8f : 0f;
            float env = Mathf.Min(1f, t / 0.03f) * Mathf.Exp(-t * 4f);
            s[i] = (agudo + crepitar) * env;
        }
        Normalizar(s, 0.55f);
        AplicarFades(s, sr, 0.002f, 0.05f);
        return s;
    }

    private static float[] SintetizarPickupArma(int sr)
    {
        float dur = 0.7f;
        int n = (int)(sr * dur);
        float[] s = new float[n];
        float[] notas = { 392f, 523.25f, 659.25f, 783.99f };
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / sr;
            float v = 0f;
            for (int k = 0; k < notas.Length; k++)
            {
                float inicio = k * 0.07f;
                if (t < inicio) continue;
                float d = t - inicio;
                v += (Mathf.Sin(2f * Mathf.PI * notas[k] * d) + 0.3f * Mathf.Sin(2f * Mathf.PI * notas[k] * 2f * d)) * Mathf.Exp(-d * 5f) * 0.3f;
            }
            s[i] = v;
        }
        Normalizar(s, 0.7f);
        AplicarFades(s, sr, 0.002f, 0.05f);
        return s;
    }

    // =====================================================================
    // TEXTURAS DEL PAQUETE 2
    // =====================================================================

    private static Texture2D TexturaMira()
    {
        int tam = 128;
        Texture2D t = NuevaTextura(tam, tam);
        Color[] px = new Color[tam * tam];
        Vector2 c = new Vector2(tam / 2f, tam / 2f);
        for (int y = 0; y < tam; y++)
            for (int x = 0; x < tam; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - c;
                float d = p.magnitude;
                float a = 0f;
                // Anillo con 4 huecos en los ejes
                bool enHueco = Mathf.Abs(p.x) < 9f || Mathf.Abs(p.y) < 9f;
                if (d > 38f && d < 45f && !enHueco) a = 1f;
                // Marcas hacia el centro
                if ((Mathf.Abs(p.x) < 3f && Mathf.Abs(p.y) > 16f && Mathf.Abs(p.y) < 32f) || (Mathf.Abs(p.y) < 3f && Mathf.Abs(p.x) > 16f && Mathf.Abs(p.x) < 32f)) a = 1f;
                // Punto central
                if (d < 4.5f) a = 1f;
                // Contorno oscuro para que se vea sobre fondos claros
                Color col = new Color(1f, 1f, 1f, a);
                if (a == 0f)
                {
                    bool cerca = (d > 35f && d < 48f && !(Mathf.Abs(p.x) < 6f || Mathf.Abs(p.y) < 6f))
                              || (Mathf.Abs(p.x) < 5f && Mathf.Abs(p.y) > 13f && Mathf.Abs(p.y) < 35f)
                              || (Mathf.Abs(p.y) < 5f && Mathf.Abs(p.x) > 13f && Mathf.Abs(p.x) < 35f)
                              || d < 7f;
                    if (cerca) col = new Color(0f, 0f, 0f, 0.6f);
                }
                px[y * tam + x] = col;
            }
        t.SetPixels(px);
        t.Apply();
        return t;
    }

    private static Texture2D TexturaGranada()
    {
        int tam = 64;
        Texture2D t = NuevaTextura(tam, tam);
        Color[] px = new Color[tam * tam];
        for (int y = 0; y < tam; y++)
            for (int x = 0; x < tam; x++)
            {
                float dx = (x - 32f) / 20f, dy = (y - 28f) / 24f;
                float d = dx * dx + dy * dy;
                Color c = Color.clear;
                if (d < 1f)
                {
                    c = new Color(0.25f, 0.32f, 0.16f, 1f);
                    if ((y / 6) % 2 == 0) c = new Color(0.2f, 0.27f, 0.12f, 1f);           // estrías
                    if (dx < -0.2f && dy > 0.1f && d < 0.5f) c = new Color(0.45f, 0.55f, 0.3f, 1f); // brillo
                    if (d > 0.82f) c = new Color(0.08f, 0.1f, 0.05f, 1f);                    // contorno
                }
                if (x >= 26 && x < 38 && y >= 50 && y < 58) c = new Color(0.55f, 0.55f, 0.58f, 1f); // cuello
                float ax = x - 42f, ay = y - 57f;
                if (Mathf.Abs(Mathf.Sqrt(ax * ax + ay * ay) - 5f) < 1.5f) c = new Color(0.75f, 0.75f, 0.78f, 1f); // anilla
                px[y * tam + x] = c;
            }
        t.SetPixels(px);
        t.Apply();
        return t;
    }

    private static Texture2D TexturaCharco()
    {
        int tam = 128;
        Texture2D t = NuevaTextura(tam, tam);
        Color[] px = new Color[tam * tam];
        for (int y = 0; y < tam; y++)
            for (int x = 0; x < tam; x++)
            {
                Vector2 p = new Vector2(x - 64f, y - 64f);
                float ang = Mathf.Atan2(p.y, p.x);
                float borde = 50f + Mathf.Sin(ang * 5f) * 6f + Mathf.Sin(ang * 9f + 1.3f) * 3f;
                float d = p.magnitude;
                float a = Mathf.Clamp01((borde - d) / 6f);
                float centro = 1f - Mathf.Clamp01(d / borde);
                Color c = new Color(0.75f + centro * 0.25f, 1f, 0.75f + centro * 0.25f, a);
                // Burbujas
                if (Ruido(x / 6, y / 6, 77) > 0.93f && d < borde - 8f) c = new Color(1f, 1f, 1f, a);
                px[y * tam + x] = c;
            }
        t.SetPixels(px);
        t.Apply();
        return t;
    }

    private static Texture2D TexturaDegradadoH()
    {
        int w = 256, h = 8;
        Texture2D t = NuevaTextura(w, h);
        Color[] px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float a = 1f - Mathf.SmoothStep(0.35f, 1f, (float)x / (w - 1));
                px[y * w + x] = new Color(1f, 1f, 1f, a);
            }
        t.SetPixels(px);
        t.Apply();
        return t;
    }

    private static Texture2D TexturaSueloLab3()
    {
        int tam = 128;
        Texture2D t = NuevaTextura(tam, tam);
        t.wrapMode = TextureWrapMode.Repeat;
        Color[] px = new Color[tam * tam];
        for (int y = 0; y < tam; y++)
            for (int x = 0; x < tam; x++)
            {
                float n = (Ruido(x, y, 91) - 0.5f) * 0.03f;
                Color c = new Color(0.11f + n, 0.135f + n, 0.125f + n, 1f);
                int lx = x % 32, ly = y % 32;
                // Placa de metal con patrón de diamante
                if (((lx + ly) % 8 == 0 || (lx - ly + 64) % 8 == 0) && lx > 3 && ly > 3 && lx < 29 && ly < 29) c = new Color(c.r + 0.05f, c.g + 0.06f, c.b + 0.055f, 1f);
                // Juntas y canaletas verdes cada 64 px
                if (lx < 2 || ly < 2) c = new Color(0.05f, 0.06f, 0.055f, 1f);
                if (x % 64 == 32 || y % 64 == 32) c = new Color(0.12f, 0.4f, 0.2f, 1f);
                // Tornillos
                if ((lx == 5 || lx == 27) && (ly == 5 || ly == 27)) c = new Color(0.35f, 0.38f, 0.36f, 1f);
                px[y * tam + x] = c;
            }
        t.SetPixels(px);
        t.Apply();
        return t;
    }
}
#endif
