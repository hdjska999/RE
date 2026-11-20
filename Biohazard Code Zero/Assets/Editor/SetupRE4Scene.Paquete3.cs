#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// PAQUETE 3 de Biohazard: Code Zero (Tools > Biohazard Code Zero):
/// música de menú/combate, arreglo de Créditos, Stage 3 "Raccoon City de noche", título metálico,
/// aura de pickups y pantalla de resultados estilo monitor táctico de RE4 Remake.
/// </summary>
public static partial class SetupRE4Scene
{
    private const string RUTA_STAGE3_VIEJA = "Assets/Scenes/Stage3_Laboratorio.unity";
    private const string RUTA_PREFAB_AUDIO = "Assets/Prefabs/AudioManager.prefab";
    private const string CLIP_MENU = "bgm_main_menu";
    private const string CLIP_COMBATE = "bgm_wesker_mercenaries";

    // =====================================================================
    // MENÚ
    // =====================================================================

    [MenuItem(MENU + "★★★ Aplicar PAQUETE 3 completo (IA, música, Raccoon City, resultados)", false, 2)]
    public static void MenuPaquete3()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        AudioClip menu = null, combate = null;
        try
        {
            Progreso("Renombrando Stage 3 a Raccoon City...", 0.05f);
            P3_RenombrarStage3();
            menu = P3_BuscarClip(CLIP_MENU);
            combate = P3_BuscarClip(CLIP_COMBATE);

            for (int n = 1; n <= 3; n++)
            {
                Progreso("Stage " + n + ": resultados, aura y música...", 0.1f + n * 0.2f);
                if (n == 3)
                {
                    bool existia = AssetDatabase.LoadAssetAtPath<SceneAsset>(RUTA_STAGE3) != null;
                    P2_CrearStage3(); // abre (o crea) y construye Raccoon City
                    if (!existia) P2_ConfigurarStage(3);
                }
                else EditorSceneManager.OpenScene(n == 1 ? RUTA_STAGE1 : RUTA_STAGE2, OpenSceneMode.Single);

                P3_ConfigurarStageComun(n, menu, combate);
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                if (n == 1) P3_CrearPrefabAudio(menu, combate);
            }

            Progreso("Menú principal...", 0.8f);
            EditorSceneManager.OpenScene(RUTA_MAINMENU, OpenSceneMode.Single);
            P3_TituloMenu();
            P3_AsegurarAudioEnEscena(menu, combate);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

            Progreso("Créditos...", 0.9f);
            EditorSceneManager.OpenScene(RUTA_CREDITOS, OpenSceneMode.Single);
            P3_ArreglarCreditos();
            P3_AsegurarAudioEnEscena(menu, combate);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

            P3_RevisarBuildSettings();
            EditorSceneManager.OpenScene(RUTA_MAINMENU, OpenSceneMode.Single);
            AssetDatabase.SaveAssets();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        string musica = (menu != null && combate != null) ? "• Música: menú y combate asignadas.\n"
            : "• Música: FALTA " + (menu == null ? "'" + CLIP_MENU + "' " : "") + (combate == null ? "'" + CLIP_COMBATE + "'" : "") +
              " en Assets/Audio (mientras tanto suena bgm_ambiente_tenso). Cópialas y ejecuta '11. Música de menú y combate'.\n";
        string msg = "¡PAQUETE 3 aplicado!\n\n" +
            "• IA anti-atasco (rodeo con compromiso de lado + separación) en todos los zombies.\n" + musica +
            "• Créditos: script perdido reparado y botón VOLVER funcionando.\n" +
            "• Stage 3 convertido en Stage3_RaccoonCity (calles de noche, patrullas, autos en llamas, farolas).\n" +
            "• Título metálico blanco-cian en el Menú Principal.\n" +
            "• Aura pulsante en las armas tiradas.\n" +
            "• Pantalla de resultados estilo monitor táctico RE4 en los 3 stages.\n\nEscenas guardadas.";
        EditorUtility.DisplayDialog("Biohazard Code Zero - Paquete 3", msg, "¡A jugar!");
        Debug.Log("[Biohazard P3] " + msg);
    }

    [MenuItem(MENU + "11. Música de menú y combate (todas las escenas)", false, 60)]
    public static void MenuP3Musica()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        AudioClip menu = P3_BuscarClip(CLIP_MENU), combate = P3_BuscarClip(CLIP_COMBATE);
        foreach (string ruta in new[] { RUTA_STAGE1, RUTA_STAGE2, RUTA_STAGE3, RUTA_MAINMENU, RUTA_CREDITOS })
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ruta) == null) continue;
            EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
            P3_AsegurarAudioEnEscena(menu, combate);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
        P3_CrearPrefabAudio(menu, combate);
        EditorSceneManager.OpenScene(RUTA_MAINMENU, OpenSceneMode.Single);
        EditorUtility.DisplayDialog("Biohazard Code Zero", "Música configurada.\nMenú: " + (menu != null ? menu.name : "(falta " + CLIP_MENU + ")") +
            "\nCombate: " + (combate != null ? combate.name : "(falta " + CLIP_COMBATE + ")"), "OK");
    }

    [MenuItem(MENU + "12. Créditos - arreglar botón Volver", false, 61)]
    public static void MenuP3Creditos()
    {
        if (!AbrirEscenaSiHaceFalta(RUTA_CREDITOS)) return;
        int n = P3_ArreglarCreditos();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        P3_RevisarBuildSettings();
        EditorUtility.DisplayDialog("Biohazard Code Zero", "Créditos reparados (" + n + " scripts perdidos eliminados). El botón VOLVER regresa a MainMenu.", "OK");
    }

    [MenuItem(MENU + "13. Stage 3 - Raccoon City de noche", false, 62)]
    public static void MenuP3Raccoon()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        bool existia = AssetDatabase.LoadAssetAtPath<SceneAsset>(RUTA_STAGE3) != null || AssetDatabase.LoadAssetAtPath<SceneAsset>(RUTA_STAGE3_VIEJA) != null;
        P2_CrearStage3();
        if (!existia) P2_ConfigurarStage(3);
        P3_ConfigurarStageComun(3, P3_BuscarClip(CLIP_MENU), P3_BuscarClip(CLIP_COMBATE));
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        P3_RevisarBuildSettings();
        EditorUtility.DisplayDialog("Biohazard Code Zero", "Stage3_RaccoonCity listo y guardado.", "OK");
    }

    [MenuItem(MENU + "14. Título metálico del Menú Principal", false, 63)]
    public static void MenuP3Titulo()
    {
        if (!AbrirEscenaSiHaceFalta(RUTA_MAINMENU)) return;
        P3_TituloMenu();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Biohazard Code Zero", "Título actualizado.", "OK");
    }

    [MenuItem(MENU + "15. Pantalla de Resultados RE4 (stage abierto)", false, 64)]
    public static void MenuP3Resultados()
    {
        MercenariesManager merc = Object.FindAnyObjectByType<MercenariesManager>(FindObjectsInactive.Include);
        if (merc == null) { EditorUtility.DisplayDialog("Biohazard Code Zero", "Abre primero un Stage jugable.", "OK"); return; }
        P3_ResultadosMonitor(merc);
        MarcarEscenaSucia();
        EditorUtility.DisplayDialog("Biohazard Code Zero", "Pantalla de resultados reconstruida. Guarda con Ctrl+S.", "OK");
    }

    [MenuItem(MENU + "16. Aura de pickups de armas (stage abierto)", false, 65)]
    public static void MenuP3Aura()
    {
        P3_AuraPickups();
        MarcarEscenaSucia();
        EditorUtility.DisplayDialog("Biohazard Code Zero", "Aura aplicada a la pistola del suelo y a las armas que suelta el Tocho. Guarda con Ctrl+S.", "OK");
    }

    // =====================================================================
    // CONFIGURACIÓN COMÚN POR STAGE
    // =====================================================================

    private static void P3_ConfigurarStageComun(int n, AudioClip menu, AudioClip combate)
    {
        MercenariesManager merc = Object.FindAnyObjectByType<MercenariesManager>(FindObjectsInactive.Include);
        if (merc != null)
        {
            Undo.RecordObject(merc, "Stage P3");
            merc.escenaSiguiente = ProgresoJuego.EscenaSiguiente(SceneManager.GetActiveScene().name);
            EditorUtility.SetDirty(merc);

            ConfigStage cfg = merc.GetComponent<ConfigStage>();
            if (cfg != null)
            {
                Undo.RecordObject(cfg, "ConfigStage P3");
                cfg.numeroStage = n;
                if (n == 3) cfg.nombreVisible = "STAGE 3 - RACCOON CITY";
                EditorUtility.SetDirty(cfg);
            }

            // Subtítulo del menú de pausa
            Canvas canvas = CanvasRaiz();
            if (canvas != null)
            {
                Transform sub = canvas.transform.Find("PanelPausa/SubtituloPausa");
                if (sub != null && cfg != null) { TextMeshProUGUI t = sub.GetComponent<TextMeshProUGUI>(); Undo.RecordObject(t, "Sub"); t.text = cfg.nombreVisible; }
            }

            P3_ResultadosMonitor(merc);
        }

        P3_AuraPickups();
        P3_AsegurarAudioEnEscena(menu, combate);
        MarcarEscenaSucia();
    }

    // =====================================================================
    // 1. MÚSICA
    // =====================================================================

    private static AudioClip P3_BuscarClip(string nombre)
    {
        foreach (string guid in AssetDatabase.FindAssets(nombre + " t:AudioClip"))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.Equals(Path.GetFileNameWithoutExtension(ruta), nombre, System.StringComparison.OrdinalIgnoreCase)) continue;

            // Música larga: streaming para no cargarla entera en memoria
            AudioImporter ai = AssetImporter.GetAtPath(ruta) as AudioImporter;
            if (ai != null)
            {
                AudioImporterSampleSettings st = ai.defaultSampleSettings;
                if (st.loadType != AudioClipLoadType.Streaming)
                {
                    st.loadType = AudioClipLoadType.Streaming;
                    ai.defaultSampleSettings = st;
                    ai.SaveAndReimport();
                }
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
        }
        return null;
    }

    private static void P3_AsignarMusica(AudioManager am, AudioClip menu, AudioClip combate)
    {
        if (am == null) return;
        Undo.RecordObject(am, "Música por escena");
        if (menu != null) am.musicaMenu = menu;
        if (combate != null) am.musicaCombate = combate;
        if (am.musicaPrincipal == null) am.musicaPrincipal = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/bgm_ambiente_tenso.wav");
        am.escenasDeMenu = new[] { "MainMenu", "CreditsScene" };
        am.escenasSinMusica = new[] { "IntroScene" };
        am.duracionFundido = 1.2f;
        EditorUtility.SetDirty(am);
    }

    /// <summary>Crea (o actualiza) el prefab AudioManager a partir del de SampleScene, para que el menú tenga todos los SFX.</summary>
    private static void P3_CrearPrefabAudio(AudioClip menu, AudioClip combate)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RUTA_PREFAB_AUDIO);
        if (prefab == null)
        {
            if (SceneManager.GetActiveScene().path != RUTA_STAGE1) EditorSceneManager.OpenScene(RUTA_STAGE1, OpenSceneMode.Single);
            AudioManager am = Object.FindAnyObjectByType<AudioManager>(FindObjectsInactive.Include);
            if (am == null) return;
            P3_AsignarMusica(am, menu, combate);
            PrefabUtility.SaveAsPrefabAssetAndConnect(am.gameObject, RUTA_PREFAB_AUDIO, InteractionMode.AutomatedAction);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
        else
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(RUTA_PREFAB_AUDIO);
            P3_AsignarMusica(raiz.GetComponent<AudioManager>(), menu, combate);
            PrefabUtility.SaveAsPrefabAsset(raiz, RUTA_PREFAB_AUDIO);
            PrefabUtility.UnloadPrefabContents(raiz);
        }
    }

    /// <summary>Garantiza un AudioManager con la música asignada en la escena abierta (el menú usa el prefab).</summary>
    private static void P3_AsegurarAudioEnEscena(AudioClip menu, AudioClip combate)
    {
        AudioManager am = Object.FindAnyObjectByType<AudioManager>(FindObjectsInactive.Include);
        if (am == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RUTA_PREFAB_AUDIO);
            if (prefab != null)
            {
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, SceneManager.GetActiveScene());
                Undo.RegisterCreatedObjectUndo(inst, "AudioManager");
                am = inst.GetComponent<AudioManager>();
            }
        }
        P3_AsignarMusica(am, menu, combate);
    }

    // =====================================================================
    // 3. CRÉDITOS: BOTÓN VOLVER
    // =====================================================================

    private static int P3_ArreglarCreditos()
    {
        // a) Quitar componentes con script perdido (GUID que ya no existe tras un merge)
        int quitados = 0;
        foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                int n = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                if (n > 0)
                {
                    Undo.RegisterCompleteObjectUndo(t.gameObject, "Quitar scripts perdidos");
                    quitados += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                }
            }

        // b) CreditsController real
        GameObject ccGo = BuscarEnEscena("CreditsController");
        if (ccGo == null) { ccGo = new GameObject("CreditsController"); Undo.RegisterCreatedObjectUndo(ccGo, "CreditsController"); }
        CreditsController cc = ccGo.GetComponent<CreditsController>();
        if (cc == null) cc = Undo.AddComponent<CreditsController>(ccGo);
        Undo.RecordObject(cc, "CreditsController");
        GameObject cont = BuscarEnEscena("ContenidoCreditos");
        cc.desplazamientoAutomatico = false;
        if (cont != null)
        {
            cc.contenedorCreditos = cont.GetComponent<RectTransform>();
            CanvasGroup cg = cont.GetComponent<CanvasGroup>();
            cc.grupoCreditos = cg;
            if (cg != null) { Undo.RecordObject(cg, "CG"); cg.blocksRaycasts = false; cg.interactable = false; }
        }
        EditorUtility.SetDirty(cc);

        // c) Botón VOLVER: gráfico que reciba clics + OnClick hacia VolverAlMenu
        GameObject back = BuscarEnEscena("Back");
        if (back != null)
        {
            Sprite blanco = SpriteGenerado("UI_Blanco", 100f, false, () => TexturaSolida(8, 8, Color.white));
            Image img = back.GetComponent<Image>();
            if (img == null) img = Undo.AddComponent<Image>(back);
            Undo.RecordObject(img, "Back img");
            img.sprite = blanco;
            img.color = new Color(1f, 1f, 1f, 0f); // invisible pero recibe el raycast
            img.raycastTarget = true;

            Button b = back.GetComponent<Button>();
            if (b == null) b = Undo.AddComponent<Button>(back);
            Undo.RecordObject(b, "Back btn");
            b.targetGraphic = img;
            b.transition = Selectable.Transition.None; // el resaltado lo hace MenuButtonHover
            b.interactable = true;
            AsignarClick(back, cc.VolverAlMenu);
            back.transform.SetAsLastSibling();
            EditorUtility.SetDirty(b);

            // Nada encima del botón debe bloquear el raycast
            Canvas canvas = back.GetComponentInParent<Canvas>();
            foreach (Graphic g in canvas.GetComponentsInChildren<Graphic>(true))
            {
                if (g.transform == back.transform || g.transform.IsChildOf(back.transform)) continue;
                if (g.raycastTarget) { Undo.RecordObject(g, "Raycast"); g.raycastTarget = false; }
            }
        }

        // d) EventSystem
        if (Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include) == null)
        {
            GameObject es = new GameObject("EventSystem", typeof(EventSystem));
            System.Type modulo = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (modulo != null) es.AddComponent(modulo); else es.AddComponent<StandaloneInputModule>();
            Undo.RegisterCreatedObjectUndo(es, "EventSystem");
        }

        MarcarEscenaSucia();
        return quitados;
    }

    private static void P3_RevisarBuildSettings()
    {
        List<EditorBuildSettingsScene> escenas = new List<EditorBuildSettingsScene>();
        HashSet<string> vistas = new HashSet<string>();
        foreach (EditorBuildSettingsScene e in EditorBuildSettings.scenes)
        {
            string ruta = e.path == RUTA_STAGE3_VIEJA ? RUTA_STAGE3 : e.path;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ruta) == null || !vistas.Add(ruta)) continue;
            escenas.Add(new EditorBuildSettingsScene(ruta, true));
        }
        foreach (string necesaria in new[] { RUTA_MAINMENU, RUTA_STAGE1, RUTA_STAGE2, RUTA_STAGE3, RUTA_CREDITOS })
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(necesaria) != null && vistas.Add(necesaria))
                escenas.Add(new EditorBuildSettingsScene(necesaria, true));
        }
        EditorBuildSettings.scenes = escenas.ToArray();
    }

    // =====================================================================
    // 4. STAGE 3: RACCOON CITY DE NOCHE
    // =====================================================================

    /// <summary>Renombra Stage3_Laboratorio a Stage3_RaccoonCity conservando el GUID.</summary>
    private static void P3_RenombrarStage3()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RUTA_STAGE3) != null) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RUTA_STAGE3_VIEJA) == null) return;
        bool estabaAbierta = SceneManager.GetActiveScene().path == RUTA_STAGE3_VIEJA;
        if (estabaAbierta) EditorSceneManager.OpenScene(RUTA_MAINMENU, OpenSceneMode.Single);
        string error = AssetDatabase.MoveAsset(RUTA_STAGE3_VIEJA, RUTA_STAGE3);
        if (!string.IsNullOrEmpty(error)) Debug.LogError("[Biohazard P3] " + error);
        P3_RevisarBuildSettings();
    }

    private static void P3_LayoutRaccoonCity()
    {
        int capa = AsegurarCapa(CAPA_OBSTACULOS);
        const float W = 48f, H = 28f;

        foreach (string viejo in new[] { "Escenario_Laboratorio", "Obstaculos_Laboratorio", "Escenario_Laboratorio_B", "Obstaculos_Laboratorio_B",
                                         "Limites_Stage", "Obstaculos_Stage1", "Escenario_RaccoonCity", "Obstaculos_RaccoonCity", "Luces_RaccoonCity" })
        {
            GameObject g = BuscarEnEscena(viejo);
            if (g != null) Undo.DestroyObjectImmediate(g);
        }
        GameObject bgHospital = BuscarEnEscena("HospitalBg_0");
        if (bgHospital != null && bgHospital.activeSelf) bgHospital.SetActive(false);

        // Zombie suelto heredado de la copia del Stage 1 (atacaba a Bryan al iniciar)
        foreach (GameObject r in SceneManager.GetActiveScene().GetRootGameObjects())
            if (r.name == "Lycan" && r.GetComponent<ZombieEnemy>() != null) Undo.DestroyObjectImmediate(r);

        Sprite asfalto = SpriteGenerado("RC_Asfalto", 64f, true, TexturaAsfalto);
        Sprite acera = SpriteGenerado("RC_Acera", 64f, true, TexturaAcera);
        Sprite ladrillo = SpriteGenerado("RC_Ladrillo", 64f, true, TexturaLadrillo);
        Sprite patrulla = SpriteGenerado("RC_Patrulla", 64f, false, TexturaPatrulla);
        Sprite quemado = SpriteGenerado("RC_AutoQuemado", 64f, false, TexturaAutoQuemado);
        Sprite barricada = SpriteGenerado("RC_Barricada", 64f, false, TexturaBarricada);
        Sprite contenedor = SpriteGenerado("RC_Contenedor", 64f, false, TexturaContenedor);
        Sprite farola = SpriteGenerado("RC_Farola", 64f, false, TexturaFarola);
        Sprite blanco = SpriteGenerado("UI_Blanco", 100f, false, () => TexturaSolida(8, 8, Color.white));
        Sprite mancha = SpriteGenerado("UI_Circulo", 100f, false, () => TexturaCirculo(128, Color.white, 0.04f));

        Transform raiz = ObtenerOCrearRaiz("Escenario_RaccoonCity").transform;

        // Suelo: asfalto agrietado y húmedo (también limita la cámara)
        SpriteRenderer srSuelo = CrearDecorado(raiz, "Asfalto", Vector2.zero, new Vector2(W + 2f, H + 2f), asfalto, -6, Color.white, true);

        // Banquetas en las 4 esquinas + bordillos
        Color colorBordillo = new Color(0.62f, 0.62f, 0.58f, 1f);
        int k = 0;
        foreach (Vector2 e in new[] { new Vector2(-1, 1), new Vector2(1, 1), new Vector2(-1, -1), new Vector2(1, -1) })
        {
            k++;
            CrearDecorado(raiz, "Banqueta_" + k, new Vector2(e.x * 15.5f, e.y * 10.5f), new Vector2(17f, 7f), acera, -5, Color.white, true);
            CrearDecorado(raiz, "BordilloH_" + k, new Vector2(e.x * 15.5f, e.y * 7f), new Vector2(17f, 0.22f), blanco, -4, colorBordillo, false);
            CrearDecorado(raiz, "BordilloV_" + k, new Vector2(e.x * 7f, e.y * 10.5f), new Vector2(0.22f, 7f), blanco, -4, colorBordillo, false);
        }

        // Marcas viales: línea amarilla discontinua, líneas blancas y pasos peatonales
        Color amarillo = new Color(0.85f, 0.68f, 0.12f, 0.85f);
        Color blancoVial = new Color(0.85f, 0.85f, 0.82f, 0.75f);
        k = 0;
        for (float x = -22.5f; x <= 22.5f; x += 2.8f)
        {
            if (Mathf.Abs(x) < 8f) continue;
            CrearDecorado(raiz, "LineaAmarilla_" + (k++), new Vector2(x, 0f), new Vector2(1.6f, 0.16f), blanco, -4, amarillo, false);
        }
        k = 0;
        for (float y = -12.5f; y <= 12.5f; y += 2.8f)
        {
            if (Mathf.Abs(y) < 8f) continue;
            CrearDecorado(raiz, "LineaBlanca_" + (k++), new Vector2(0f, y), new Vector2(0.16f, 1.6f), blanco, -4, blancoVial, false);
        }
        k = 0;
        for (float y = -5f; y <= 5.01f; y += 1.1f)
        {
            CrearDecorado(raiz, "Cebra_O_" + k, new Vector2(-6f, y), new Vector2(1.5f, 0.55f), blanco, -4, blancoVial, false);
            CrearDecorado(raiz, "Cebra_E_" + k, new Vector2(6f, y), new Vector2(1.5f, 0.55f), blanco, -4, blancoVial, false);
            k++;
        }
        k = 0;
        for (float x = -5f; x <= 5.01f; x += 1.1f)
        {
            CrearDecorado(raiz, "Cebra_N_" + k, new Vector2(x, 6f), new Vector2(0.55f, 1.5f), blanco, -4, blancoVial, false);
            CrearDecorado(raiz, "Cebra_S_" + k, new Vector2(x, -6f), new Vector2(0.55f, 1.5f), blanco, -4, blancoVial, false);
            k++;
        }

        // Manchas de aceite y sangre
        Vector2[] manchas = { new Vector2(-12, -4), new Vector2(9, 4.5f), new Vector2(-4, 3), new Vector2(14, -5), new Vector2(-19, 4), new Vector2(3, -8.5f), new Vector2(18, 9), new Vector2(-9, 10) };
        for (int i = 0; i < manchas.Length; i++)
        {
            float tam = 1.2f + (i % 3) * 0.7f;
            Color c = i % 2 == 0 ? new Color(0.03f, 0.03f, 0.04f, 0.45f) : new Color(0.3f, 0.02f, 0.02f, 0.4f);
            CrearDecorado(raiz, "Mancha_" + i, manchas[i], new Vector2(tam * 1.5f, tam), mancha, -4, c, false);
        }

        // Edificios (muros de ladrillo) en el perímetro
        Transform limites = ObtenerOCrearRaiz("Limites_Stage").transform;
        float g2 = 1.6f;
        CrearSolido(limites, "Muro_Izq", new Vector2(-W / 2f + g2 / 2f, 0f), new Vector2(g2, H), 0f, capa, ladrillo, true, -1);
        CrearSolido(limites, "Muro_Der", new Vector2(W / 2f - g2 / 2f, 0f), new Vector2(g2, H), 0f, capa, ladrillo, true, -1);
        CrearSolido(limites, "Muro_Sup", new Vector2(0f, H / 2f - g2 / 2f), new Vector2(W, g2), 0f, capa, ladrillo, true, -1);
        CrearSolido(limites, "Muro_Inf", new Vector2(0f, -H / 2f + g2 / 2f), new Vector2(W, g2), 0f, capa, ladrillo, true, -1);

        // Obstáculos tácticos
        Transform obst = ObtenerOCrearRaiz("Obstaculos_RaccoonCity").transform;
        CrearSolido(obst, "Patrulla_1", new Vector2(-10.5f, 2.6f), new Vector2(1.6f, 3.2f), 90f, capa, patrulla, false, -1);
        CrearSolido(obst, "Patrulla_2", new Vector2(11f, -2.8f), new Vector2(1.6f, 3.2f), -88f, capa, patrulla, false, -1);
        CrearSolido(obst, "AutoQuemado_1", new Vector2(-17.5f, -3.2f), new Vector2(1.6f, 3.2f), 80f, capa, quemado, false, -1);
        CrearSolido(obst, "AutoQuemado_2", new Vector2(16.5f, 3.4f), new Vector2(1.6f, 3.2f), 100f, capa, quemado, false, -1);
        CrearSolido(obst, "AutoQuemado_3", new Vector2(2.6f, 10.2f), new Vector2(1.6f, 3.2f), 12f, capa, quemado, false, -1);
        CrearSolido(obst, "AutoQuemado_4", new Vector2(-2.8f, -10.4f), new Vector2(1.6f, 3.2f), -18f, capa, quemado, false, -1);
        CrearSolido(obst, "Barricada_1", new Vector2(-6.5f, 9.6f), new Vector2(3.2f, 0.7f), 0f, capa, barricada, false, -1);
        CrearSolido(obst, "Barricada_2", new Vector2(6.5f, -9.6f), new Vector2(3.2f, 0.7f), 0f, capa, barricada, false, -1);
        CrearSolido(obst, "Barricada_3", new Vector2(-14f, -1.8f), new Vector2(3.2f, 0.7f), 90f, capa, barricada, false, -1);
        CrearSolido(obst, "Barricada_4", new Vector2(14f, 1.8f), new Vector2(3.2f, 0.7f), 90f, capa, barricada, false, -1);
        CrearSolido(obst, "Contenedor_1", new Vector2(-20.5f, 10.6f), new Vector2(2.2f, 1.4f), 0f, capa, contenedor, false, -1);
        CrearSolido(obst, "Contenedor_2", new Vector2(20.5f, -10.6f), new Vector2(2.2f, 1.4f), 0f, capa, contenedor, false, -1);

        Vector2[] farolas = { new Vector2(-8.2f, 8.2f), new Vector2(8.2f, 8.2f), new Vector2(-8.2f, -8.2f), new Vector2(8.2f, -8.2f),
                              new Vector2(-17f, 8.2f), new Vector2(17f, -8.2f), new Vector2(-17f, -8.2f), new Vector2(17f, 8.2f) };
        for (int i = 0; i < farolas.Length; i++)
            CrearSolido(obst, "Farola_" + (i + 1), farolas[i], new Vector2(0.7f, 0.7f), 0f, capa, farola, false, 0);

        // Iluminación: noche fría + farolas ámbar/frías + fuego + torretas de patrulla
        Transform luces = ObtenerOCrearRaiz("Luces_RaccoonCity").transform;
        for (int i = 0; i < farolas.Length; i++)
        {
            bool fria = i == 1 || i == 6;
            Color c = fria ? new Color(0.62f, 0.8f, 1f, 1f) : new Color(1f, 0.72f, 0.38f, 1f);
            P3_Luz(luces, "LuzFarola_" + (i + 1), farolas[i], c, 1.15f, 6.5f, i == 4 ? (LuzParpadeante.Modo?)LuzParpadeante.Modo.FarolaFallando : null);
        }
        P3_Luz(luces, "Fuego_1", new Vector2(-17.5f, -3.2f), new Color(1f, 0.45f, 0.1f, 1f), 1.5f, 3.8f, LuzParpadeante.Modo.Fuego);
        P3_Luz(luces, "Fuego_2", new Vector2(16.5f, 3.4f), new Color(1f, 0.45f, 0.1f, 1f), 1.5f, 3.8f, LuzParpadeante.Modo.Fuego);
        P3_Luz(luces, "Fuego_3", new Vector2(2.6f, 10.2f), new Color(1f, 0.5f, 0.12f, 1f), 1.3f, 3.4f, LuzParpadeante.Modo.Fuego);
        P3_Luz(luces, "Fuego_4", new Vector2(-2.8f, -10.4f), new Color(1f, 0.5f, 0.12f, 1f), 1.3f, 3.4f, LuzParpadeante.Modo.Fuego);
        P3_Luz(luces, "Sirena_1", new Vector2(-10.5f, 2.6f), new Color(1f, 0.15f, 0.15f, 1f), 1.2f, 3.6f, LuzParpadeante.Modo.Sirena);
        P3_Luz(luces, "Sirena_2", new Vector2(11f, -2.8f), new Color(0.2f, 0.4f, 1f, 1f), 1.2f, 3.6f, LuzParpadeante.Modo.Sirena);

        GameObject luzGo = BuscarEnEscena("Global Light 2D");
        if (luzGo != null)
        {
            Component luz = luzGo.GetComponent("Light2D");
            if (luz != null)
            {
                SerializedObject so = new SerializedObject(luz);
                SerializedProperty col = so.FindProperty("m_Color");
                SerializedProperty inten = so.FindProperty("m_Intensity");
                if (col != null) col.colorValue = new Color(0.42f, 0.5f, 0.72f, 1f);
                if (inten != null) inten.floatValue = 0.42f;
                so.ApplyModifiedProperties();
            }
        }

        // Material iluminado (el mismo de Bryan) para que las luces 2D afecten al escenario
        GameObject player = BuscarEnEscena("Player");
        Material lit = player != null && player.GetComponent<SpriteRenderer>() != null ? player.GetComponent<SpriteRenderer>().sharedMaterial : null;
        if (lit != null)
            foreach (Transform r in new[] { raiz, limites, obst })
                foreach (SpriteRenderer sr in r.GetComponentsInChildren<SpriteRenderer>(true)) sr.sharedMaterial = lit;

        if (player != null) player.transform.position = new Vector3(0f, -0.5f, 0f);
        GameObject pickup = BuscarEnEscena("BryanHGAim_0");
        if (pickup != null) pickup.transform.position = new Vector3(2.2f, -0.5f, 0f);

        ConfigurarSpawnPoints(new Vector2[]
        {
            new Vector2(-21.5f, 2.5f), new Vector2(-21.5f, -2.5f), new Vector2(21.5f, 2.5f), new Vector2(21.5f, -2.5f),
            new Vector2(-2.5f, 12f), new Vector2(2.5f, -12f), new Vector2(-12f, 11.3f), new Vector2(12f, -11.3f),
            new Vector2(-12f, -11.3f), new Vector2(12f, 11.3f)
        });

        CameraFollow cam = Object.FindAnyObjectByType<CameraFollow>(FindObjectsInactive.Include);
        if (cam != null) { Undo.RecordObject(cam, "Cámara"); cam.mapBackground = srSuelo; EditorUtility.SetDirty(cam); }

        MarcarEscenaSucia();
    }

    private static void P3_Luz(Transform padre, string nombre, Vector2 pos, Color color, float intensidad, float radio, LuzParpadeante.Modo? modo)
    {
        Transform t = padre.Find(nombre);
        GameObject go = t != null ? t.gameObject : new GameObject(nombre);
        if (t == null) { go.transform.SetParent(padre, false); Undo.RegisterCreatedObjectUndo(go, "Luz"); }
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        Component luz = Luz2DUtil.CrearPuntual(go, color, intensidad, radio);
        if (luz != null) EditorUtility.SetDirty(luz);

        if (modo.HasValue)
        {
            LuzParpadeante lp = go.GetComponent<LuzParpadeante>();
            if (lp == null) lp = Undo.AddComponent<LuzParpadeante>(go);
            lp.modo = modo.Value;
            lp.intensidadBase = intensidad;
            lp.colorA = color;
            if (modo.Value == LuzParpadeante.Modo.Sirena) { lp.colorA = new Color(1f, 0.12f, 0.12f, 1f); lp.colorB = new Color(0.15f, 0.35f, 1f, 1f); lp.velocidad = 8f; }
            EditorUtility.SetDirty(lp);
        }
    }

    // =====================================================================
    // 5. TÍTULO METÁLICO DEL MENÚ PRINCIPAL
    // =====================================================================

    private static void P3_TituloMenu()
    {
        TMP_FontAsset fuente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RUTA_FUENTE_CINZEL);
        GameObject titulo = BuscarEnEscena("TitleText");
        if (titulo != null && fuente != null)
        {
            TextMeshProUGUI tmp = titulo.GetComponent<TextMeshProUGUI>();
            Undo.RecordObject(tmp, "Título metálico");
            Material mat = P3_MaterialTMP(fuente, "Cinzel_TituloMetal", new Color(0.04f, 0.1f, 0.16f, 1f), 0.14f, new Color(0.25f, 0.8f, 1f, 0.45f), 0.25f, 0.65f, 0f);
            tmp.font = fuente;
            tmp.fontSharedMaterial = mat;
            tmp.text = "BIOHAZARD: CODE ZERO";
            tmp.color = Color.white;
            tmp.enableVertexGradient = true;
            Color arriba = new Color(0.97f, 0.99f, 1f, 1f);
            Color abajo = new Color(0.58f, 0.78f, 0.9f, 1f);
            tmp.colorGradient = new VertexGradient(arriba, arriba, abajo, abajo);
            tmp.characterSpacing = 10f;
            tmp.fontStyle = FontStyles.Bold;
            EditorUtility.SetDirty(tmp);
        }

        GameObject sub = BuscarEnEscena("Subtitulo");
        if (sub != null)
        {
            TextMeshProUGUI s = sub.GetComponent<TextMeshProUGUI>();
            Undo.RecordObject(s, "Subtítulo");
            s.color = new Color(0.62f, 0.86f, 0.95f, 0.85f);
            EditorUtility.SetDirty(s);
        }

        // Selector de stages: nombre nuevo del Stage 3
        GameObject lbl3 = BuscarEnEscena("BotonStage3");
        if (lbl3 != null)
        {
            Transform t = lbl3.transform.Find("Texto");
            if (t != null) { TextMeshProUGUI l = t.GetComponent<TextMeshProUGUI>(); Undo.RecordObject(l, "Stage 3"); l.text = "STAGE 3  -  RACCOON CITY"; EditorUtility.SetDirty(l); }
        }
        MarcarEscenaSucia();
    }

    /// <summary>Material TMP con contorno + resplandor (underlay), guardado como asset junto a la fuente.</summary>
    private static Material P3_MaterialTMP(TMP_FontAsset fuente, string nombre, Color contorno, float grosor, Color brillo, float dilatacion, float suavidad, float desplazamientoY)
    {
        string ruta = "Assets/Fonts/" + nombre + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null)
        {
            m = new Material(fuente.material) { name = nombre };
            AssetDatabase.CreateAsset(m, ruta);
        }
        else
        {
            m.shader = fuente.material.shader;
            m.SetTexture("_MainTex", fuente.material.GetTexture("_MainTex"));
        }
        m.SetColor("_FaceColor", Color.white);
        m.EnableKeyword("OUTLINE_ON");
        m.SetColor("_OutlineColor", contorno);
        m.SetFloat("_OutlineWidth", grosor);
        m.EnableKeyword("UNDERLAY_ON");
        m.SetColor("_UnderlayColor", brillo);
        m.SetFloat("_UnderlayDilate", dilatacion);
        m.SetFloat("_UnderlaySoftness", suavidad);
        m.SetFloat("_UnderlayOffsetX", 0f);
        m.SetFloat("_UnderlayOffsetY", desplazamientoY);
        EditorUtility.SetDirty(m);
        return m;
    }

    // =====================================================================
    // 6. AURA DE PICKUPS
    // =====================================================================

    private static void P3_AuraPickups()
    {
        Sprite aura = SpriteGenerado("FX_Aura", 100f, false, TexturaAura);

        // Arma que suelta el Tocho
        ConfigStage cfg = Object.FindAnyObjectByType<ConfigStage>(FindObjectsInactive.Include);
        if (cfg != null) { Undo.RecordObject(cfg, "Aura"); cfg.spriteBrillo = aura; EditorUtility.SetDirty(cfg); }

        // Pistola tirada al inicio del nivel
        GameObject pistola = BuscarEnEscena("BryanHGAim_0");
        if (pistola != null)
        {
            AuraPickup a = pistola.GetComponent<AuraPickup>();
            if (a == null) a = Undo.AddComponent<AuraPickup>(pistola);
            Undo.RecordObject(a, "Aura pistola");
            a.spriteAura = aura;
            a.diametro = 2f;
            a.ordenDetras = 8; // la pistola usa sortingOrder 10
            a.flotacion = 0.08f;
            a.crearLuz = true;
            EditorUtility.SetDirty(a);
        }
    }

    // =====================================================================
    // 7. PANTALLA DE RESULTADOS ESTILO MONITOR TÁCTICO (RE4 REMAKE)
    // =====================================================================

    private static void P3_ResultadosMonitor(MercenariesManager merc)
    {
        if (merc == null || merc.panelGameOver == null) return;
        GameObject panel = merc.panelGameOver;
        TMP_FontAsset fuente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RUTA_FUENTE_CINZEL);
        Sprite blanco = SpriteGenerado("UI_Blanco", 100f, false, () => TexturaSolida(8, 8, Color.white));
        Sprite scan = SpriteGenerado("UI_Scanlines", 100f, true, TexturaScanlines);
        Color cian = new Color(0.4f, 0.88f, 0.98f, 1f);

        Image fondo = panel.GetComponent<Image>();
        if (fondo != null) { Undo.RecordObject(fondo, "Fondo"); fondo.color = new Color(0f, 0.012f, 0.018f, 0.93f); }

        foreach (string viejo in new[] { "EtiquetaRango", "Resumen", "Image" })
        {
            Transform t = panel.transform.Find(viejo);
            if (t != null && t.gameObject.activeSelf) { Undo.RecordObject(t.gameObject, "Ocultar"); t.gameObject.SetActive(false); }
        }

        // Monitor
        GameObject mon = ObtenerOCrearUI(panel.transform, "Monitor", typeof(Image), typeof(CanvasGroup));
        Colocar(mon, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(1560f, 880f));
        Image imgMon = mon.GetComponent<Image>(); imgMon.sprite = blanco; imgMon.color = new Color(0.015f, 0.085f, 0.1f, 0.9f); imgMon.raycastTarget = false;

        P3_Linea(mon.transform, "BordeSup", new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1560f, 2f), cian * new Color(1, 1, 1, 0.75f), blanco);
        P3_Linea(mon.transform, "BordeInf", new Vector2(0.5f, 0f), Vector2.zero, new Vector2(1560f, 2f), cian * new Color(1, 1, 1, 0.75f), blanco);
        P3_Linea(mon.transform, "BordeIzq", new Vector2(0f, 0.5f), Vector2.zero, new Vector2(2f, 880f), cian * new Color(1, 1, 1, 0.75f), blanco);
        P3_Linea(mon.transform, "BordeDer", new Vector2(1f, 0.5f), Vector2.zero, new Vector2(2f, 880f), cian * new Color(1, 1, 1, 0.75f), blanco);
        // Esquinas tipo visor
        Vector2[] esq = { new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(1, 0) };
        for (int i = 0; i < 4; i++)
        {
            Vector2 a = esq[i];
            float sx = a.x == 0 ? 1f : -1f, sy = a.y == 0 ? 1f : -1f;
            P3_Linea(mon.transform, "EsquinaH_" + i, a, new Vector2(sx * 30f, sy * 2f), new Vector2(64f, 5f), cian, blanco);
            P3_Linea(mon.transform, "EsquinaV_" + i, a, new Vector2(sx * 2f, sy * 30f), new Vector2(5f, 64f), cian, blanco);
        }

        // Encabezado
        Material matHeader = fuente != null ? P3_MaterialTMP(fuente, "Cinzel_ResultsGlow", new Color(0f, 0.08f, 0.1f, 1f), 0.06f, new Color(0.2f, 0.85f, 1f, 0.5f), 0.35f, 0.7f, 0f) : null;
        GameObject header = ObtenerOCrearUI(mon.transform, "Encabezado", typeof(TextMeshProUGUI));
        ConfigurarTMP(header.GetComponent<TextMeshProUGUI>(), fuente, "RESULTS", 66, Color.white, TextAlignmentOptions.Center);
        header.GetComponent<TextMeshProUGUI>().characterSpacing = 26f;
        if (matHeader != null) header.GetComponent<TextMeshProUGUI>().fontSharedMaterial = matHeader;
        Colocar(header, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -62f), new Vector2(900f, 80f));
        P3_Linea(mon.transform, "LineaEncabezado", new Vector2(0.5f, 1f), new Vector2(0f, -106f), new Vector2(700f, 2f), cian * new Color(1, 1, 1, 0.6f), blanco);

        // Motivo (reutiliza el título anterior: ¡HAS MUERTO! / ¡STAGE COMPLETADO!)
        Transform tMotivo = mon.transform.Find("TituloResultado");
        if (tMotivo == null)
        {
            Transform viejo = panel.transform.Find("TituloResultado");
            if (viejo != null) { Undo.SetTransformParent(viejo, mon.transform, "Motivo"); tMotivo = viejo; }
        }
        GameObject motivo = tMotivo != null ? tMotivo.gameObject : ObtenerOCrearUI(mon.transform, "TituloResultado", typeof(TextMeshProUGUI));
        if (!motivo.activeSelf) motivo.SetActive(true);
        ConfigurarTMP(motivo.GetComponent<TextMeshProUGUI>(), fuente, "¡STAGE COMPLETADO!", 24, new Color(1f, 0.85f, 0.3f, 1f), TextAlignmentOptions.Center);
        motivo.GetComponent<TextMeshProUGUI>().characterSpacing = 8f;
        Colocar(motivo, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -132f), new Vector2(1100f, 36f));

        // Columna izquierda: estadísticas
        GameObject col = ObtenerOCrearUI(mon.transform, "ColumnaStats");
        Colocar(col, new Vector2(0.5f, 0.5f), new Vector2(0f, 1f), new Vector2(-700f, 255f), new Vector2(720f, 580f));
        string[] etiquetas = { "STAGE / MODO", "TIEMPO TOTAL", "BAJAS", "COMBO MÁXIMO", "PUNTAJE FINAL" };
        float[] tamValor = { 30f, 46f, 46f, 46f, 54f };
        TextMeshProUGUI[] valores = new TextMeshProUGUI[etiquetas.Length];
        for (int i = 0; i < etiquetas.Length; i++)
        {
            GameObject bloque = ObtenerOCrearUI(col.transform, "Stat_" + i);
            Colocar(bloque, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -i * 104f), new Vector2(720f, 96f));
            GameObject lbl = ObtenerOCrearUI(bloque.transform, "Etiqueta", typeof(TextMeshProUGUI));
            ConfigurarTMP(lbl.GetComponent<TextMeshProUGUI>(), fuente, etiquetas[i], 17, cian, TextAlignmentOptions.Left);
            lbl.GetComponent<TextMeshProUGUI>().characterSpacing = 8f;
            Colocar(lbl, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(720f, 24f));
            GameObject val = ObtenerOCrearUI(bloque.transform, "Valor", typeof(TextMeshProUGUI));
            ConfigurarTMP(val.GetComponent<TextMeshProUGUI>(), fuente, "0", tamValor[i], i == 4 ? new Color(1f, 0.93f, 0.75f, 1f) : Color.white, TextAlignmentOptions.Left);
            Colocar(val, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -26f), new Vector2(720f, 64f));
            valores[i] = val.GetComponent<TextMeshProUGUI>();
        }
        GameObject extra = ObtenerOCrearUI(col.transform, "Extra", typeof(TextMeshProUGUI));
        ConfigurarTMP(extra.GetComponent<TextMeshProUGUI>(), fuente, "OLEADA 5 / 5", 17, new Color(0.75f, 0.8f, 0.82f, 1f), TextAlignmentOptions.Left);
        extra.GetComponent<TextMeshProUGUI>().richText = true;
        Colocar(extra, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -5 * 104f - 4f), new Vector2(900f, 26f));

        P3_Linea(mon.transform, "Divisor", new Vector2(0.5f, 0.5f), new Vector2(70f, 0f), new Vector2(2f, 560f), cian * new Color(1, 1, 1, 0.3f), blanco);

        // Columna derecha: letra de rango gigante
        GameObject etRango = ObtenerOCrearUI(mon.transform, "EtiquetaRangoMonitor", typeof(TextMeshProUGUI));
        ConfigurarTMP(etRango.GetComponent<TextMeshProUGUI>(), fuente, "RANGO", 24, cian, TextAlignmentOptions.Center);
        etRango.GetComponent<TextMeshProUGUI>().characterSpacing = 16f;
        Colocar(etRango, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(400f, 250f), new Vector2(500f, 34f));

        Transform tRango = mon.transform.Find("Rango");
        if (tRango == null)
        {
            Transform viejo = merc.textoRango != null ? merc.textoRango.transform : panel.transform.Find("Rango");
            if (viejo != null) { Undo.SetTransformParent(viejo, mon.transform, "Rango"); tRango = viejo; }
        }
        GameObject rango = tRango != null ? tRango.gameObject : ObtenerOCrearUI(mon.transform, "Rango", typeof(TextMeshProUGUI));
        if (!rango.activeSelf) rango.SetActive(true);
        TextMeshProUGUI tmpRango = rango.GetComponent<TextMeshProUGUI>();
        ConfigurarTMP(tmpRango, fuente, "S", 380, Color.white, TextAlignmentOptions.Center);
        if (fuente != null) tmpRango.fontSharedMaterial = P3_MaterialTMP(fuente, "Cinzel_RangoGlow", new Color(0.12f, 0.08f, 0f, 1f), 0.08f, new Color(1f, 0.75f, 0.2f, 0.55f), 0.45f, 0.8f, 0f);
        tmpRango.enableVertexGradient = true;
        tmpRango.colorGradient = new VertexGradient(Color.white, Color.white, new Color(1f, 0.85f, 0.45f, 1f), new Color(1f, 0.85f, 0.45f, 1f));
        Colocar(rango, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(400f, 20f), new Vector2(560f, 400f));

        GameObject umbrales = ObtenerOCrearUI(mon.transform, "Umbrales", typeof(TextMeshProUGUI));
        ConfigurarTMP(umbrales.GetComponent<TextMeshProUGUI>(), fuente,
            "S " + merc.umbralRangoS.ToString("N0") + "+     A " + merc.umbralRangoA.ToString("N0") + "+     B " + merc.umbralRangoB.ToString("N0") + "+",
            16, new Color(0.7f, 0.75f, 0.78f, 1f), TextAlignmentOptions.Center);
        Colocar(umbrales, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(400f, -215f), new Vector2(620f, 26f));

        // Fila de botones al frente
        GameObject fila = ObtenerOCrearUI(mon.transform, "FilaBotones", typeof(HorizontalLayoutGroup));
        Colocar(fila, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 46f), new Vector2(1440f, 70f));
        HorizontalLayoutGroup hlg = fila.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 26f; hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true; hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;

        GameObject bSig = P3_BotonMonitor(fila.transform, panel.transform, "BotonSiguienteStage", "SIGUIENTE STAGE", fuente, blanco, 0);
        GameObject bRei = P3_BotonMonitor(fila.transform, panel.transform, "BotonReintentar", "REINTENTAR", fuente, blanco, 1);
        GameObject bSel = P3_BotonMonitor(fila.transform, panel.transform, "BotonSeleccionarStage", "SELECCIONAR STAGE", fuente, blanco, 2);
        GameObject bMen = P3_BotonMonitor(fila.transform, panel.transform, "BotonMenu", "MENÚ PRINCIPAL", fuente, blanco, 3);
        AsignarClick(bSig, merc.SiguienteStage);
        AsignarClick(bRei, merc.ReiniciarPartida);
        AsignarClick(bSel, merc.IrASeleccionStage);
        AsignarClick(bMen, merc.IrAlMenuPrincipal);

        // Líneas de escaneo CRT por encima de todo (no bloquean clics)
        GameObject sl = ObtenerOCrearUI(mon.transform, "Scanlines", typeof(Image));
        RectTransform rsl = sl.GetComponent<RectTransform>();
        Undo.RecordObject(rsl, "Scan");
        rsl.anchorMin = Vector2.zero; rsl.anchorMax = Vector2.one; rsl.offsetMin = Vector2.zero; rsl.offsetMax = Vector2.zero; rsl.localScale = Vector3.one;
        Image isl = sl.GetComponent<Image>(); isl.sprite = scan; isl.type = Image.Type.Tiled; isl.color = new Color(0f, 0f, 0f, 0.3f); isl.raycastTarget = false;
        sl.transform.SetAsLastSibling();

        Undo.RecordObject(merc, "Resultados RE4");
        merc.textoTituloResultado = motivo.GetComponent<TextMeshProUGUI>();
        merc.textoRango = tmpRango;
        merc.textoStatStage = valores[0];
        merc.textoStatTiempo = valores[1];
        merc.textoStatBajas = valores[2];
        merc.textoStatCombo = valores[3];
        merc.textoStatPuntaje = valores[4];
        merc.textoStatExtra = extra.GetComponent<TextMeshProUGUI>();
        merc.botonSiguienteStage = bSig;
        merc.botonSeleccionarStage = bSel;
        merc.grupoResultados = mon.GetComponent<CanvasGroup>();
        merc.colorRangoS = new Color(1f, 0.84f, 0.3f, 1f);
        merc.colorRangoA = new Color(1f, 0.95f, 0.82f, 1f);
        merc.colorRangoB = new Color(0.82f, 0.94f, 1f, 1f);
        merc.colorRangoC = new Color(0.68f, 0.72f, 0.75f, 1f);
        EditorUtility.SetDirty(merc);

        panel.transform.SetAsLastSibling();
        if (panel.activeSelf) panel.SetActive(false);
    }

    private static void P3_Linea(Transform padre, string nombre, Vector2 ancla, Vector2 pos, Vector2 tam, Color color, Sprite blanco)
    {
        GameObject go = ObtenerOCrearUI(padre, nombre, typeof(Image));
        Colocar(go, ancla, new Vector2(0.5f, 0.5f), pos, tam);
        Image i = go.GetComponent<Image>(); i.sprite = blanco; i.color = color; i.raycastTarget = false;
    }

    private static GameObject P3_BotonMonitor(Transform fila, Transform panel, string nombre, string texto, TMP_FontAsset fuente, Sprite blanco, int orden)
    {
        Transform t = fila.Find(nombre);
        if (t == null)
        {
            Transform viejo = panel.Find(nombre);
            if (viejo != null) { Undo.SetTransformParent(viejo, fila, "Botón"); t = viejo; }
        }
        GameObject go = t != null ? t.gameObject : CrearBoton(fila, nombre, texto, Vector2.zero, fuente, blanco);
        go.transform.SetSiblingIndex(Mathf.Min(orden, fila.childCount - 1));

        LayoutElement le = go.GetComponent<LayoutElement>();
        if (le == null) le = Undo.AddComponent<LayoutElement>(go);
        le.preferredWidth = 320f; le.preferredHeight = 64f;

        Image img = go.GetComponent<Image>();
        if (img != null) { img.sprite = blanco; img.color = Color.white; img.raycastTarget = true; }
        Button b = go.GetComponent<Button>();
        if (b != null)
        {
            Undo.RecordObject(b, "Estilo botón");
            ColorBlock cb = b.colors;
            cb.normalColor = new Color(0.03f, 0.14f, 0.17f, 0.95f);
            cb.highlightedColor = new Color(0.1f, 0.42f, 0.48f, 1f);
            cb.pressedColor = new Color(0.3f, 0.8f, 0.88f, 1f);
            cb.selectedColor = cb.highlightedColor;
            cb.disabledColor = new Color(0.05f, 0.08f, 0.09f, 0.6f);
            b.colors = cb;
            b.targetGraphic = img;
        }
        TextMeshProUGUI lbl = go.GetComponentInChildren<TextMeshProUGUI>(true);
        if (lbl != null)
        {
            ConfigurarTMP(lbl, fuente, texto, 21, Color.white, TextAlignmentOptions.Center);
            lbl.characterSpacing = 5f;
            Colocar(lbl.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320f, 64f));
        }
        if (!go.activeSelf) go.SetActive(true);
        EditorUtility.SetDirty(go);
        return go;
    }

    // =====================================================================
    // TEXTURAS PROCEDURALES DEL PAQUETE 3
    // =====================================================================

    private static float RuidoSuave(int x, int y, int celda, int periodo, int semilla)
    {
        int cx = x / celda, cy = y / celda;
        float fx = (x % celda) / (float)celda, fy = (y % celda) / (float)celda;
        fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
        float a = Ruido(cx % periodo, cy % periodo, semilla), b = Ruido((cx + 1) % periodo, cy % periodo, semilla);
        float c = Ruido(cx % periodo, (cy + 1) % periodo, semilla), d = Ruido((cx + 1) % periodo, (cy + 1) % periodo, semilla);
        return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
    }

    private static Texture2D TexturaAura()
    {
        int tam = 128;
        Texture2D t = NuevaTextura(tam, tam);
        Color[] px = new Color[tam * tam];
        for (int y = 0; y < tam; y++)
            for (int x = 0; x < tam; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(64f, 64f)) / 64f;
                float halo = Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f);
                float anillo = Mathf.Exp(-Mathf.Pow((d - 0.55f) / 0.06f, 2f)) * 0.5f;
                px[y * tam + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(halo + anillo));
            }
        t.SetPixels(px);
        t.Apply();
        return t;
    }

    private static Texture2D TexturaScanlines()
    {
        Texture2D t = NuevaTextura(4, 4);
        t.wrapMode = TextureWrapMode.Repeat;
        t.filterMode = FilterMode.Point;
        Color[] px = new Color[16];
        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
                px[y * 4 + x] = y < 2 ? new Color(0f, 0f, 0f, 1f) : new Color(0f, 0f, 0f, 0f);
        t.SetPixels(px);
        t.Apply();
        return t;
    }

    private static Texture2D TexturaAsfalto()
    {
        int tam = 128;
        Texture2D t = NuevaTextura(tam, tam);
        t.wrapMode = TextureWrapMode.Repeat;
        Color[] px = new Color[tam * tam];
        for (int y = 0; y < tam; y++)
            for (int x = 0; x < tam; x++)
            {
                float grano = (Ruido(x, y, 101) - 0.5f) * 0.05f;
                float manchas = (RuidoSuave(x, y, 16, 8, 102) - 0.5f) * 0.05f;
                float v = 0.12f + grano + manchas;
                Color c = new Color(v, v + 0.003f, v + 0.012f, 1f);
                // Charcos / brillo húmedo azulado
                float humedo = RuidoSuave(x, y, 32, 4, 103);
                if (humedo > 0.62f)
                {
                    float k = Mathf.Clamp01((humedo - 0.62f) / 0.15f);
                    c = Color.Lerp(c, new Color(0.16f, 0.19f, 0.24f, 1f), k * 0.8f);
                }
                // Piedritas
                if (Ruido(x, y, 104) > 0.985f) c = new Color(0.25f, 0.25f, 0.26f, 1f);
                px[y * tam + x] = c;
            }
        // Grietas (caminatas aleatorias que hacen wrap para ser tileables)
        System.Random rnd = new System.Random(105);
        for (int g = 0; g < 7; g++)
        {
            float x = rnd.Next(tam), y = rnd.Next(tam);
            float ang = (float)(rnd.NextDouble() * Mathf.PI * 2f);
            int largo = 30 + rnd.Next(50);
            for (int i = 0; i < largo; i++)
            {
                ang += (float)(rnd.NextDouble() - 0.5) * 0.8f;
                x += Mathf.Cos(ang); y += Mathf.Sin(ang);
                int ix = ((int)x % tam + tam) % tam, iy = ((int)y % tam + tam) % tam;
                px[iy * tam + ix] = new Color(0.04f, 0.04f, 0.045f, 1f);
                int ix2 = (ix + 1) % tam;
                px[iy * tam + ix2] = Color.Lerp(px[iy * tam + ix2], new Color(0.05f, 0.05f, 0.055f, 1f), 0.6f);
            }
        }
        t.SetPixels(px);
        t.Apply();
        return t;
    }

    private static Texture2D TexturaAcera()
    {
        int tam = 64;
        Texture2D t = NuevaTextura(tam, tam);
        t.wrapMode = TextureWrapMode.Repeat;
        Color[] px = new Color[tam * tam];
        for (int y = 0; y < tam; y++)
            for (int x = 0; x < tam; x++)
            {
                float n = (Ruido(x, y, 111) - 0.5f) * 0.05f + (RuidoSuave(x, y, 8, 8, 112) - 0.5f) * 0.06f;
                float v = 0.34f + n;
                Color c = new Color(v, v, v + 0.01f, 1f);
                if (x % 32 < 1 || y % 32 < 1) c = new Color(0.2f, 0.2f, 0.21f, 1f);
                if (Ruido(x / 4, y / 4, 113) > 0.96f) c *= 0.8f; // chicles / manchas
                c.a = 1f;
                px[y * tam + x] = c;
            }
        t.SetPixels(px);
        t.Apply();
        return t;
    }

    private static Texture2D TexturaLadrillo()
    {
        int tam = 64;
        Texture2D t = NuevaTextura(tam, tam);
        t.wrapMode = TextureWrapMode.Repeat;
        Color[] px = new Color[tam * tam];
        for (int y = 0; y < tam; y++)
            for (int x = 0; x < tam; x++)
            {
                int fila = y / 8;
                int desplaz = (fila % 2) * 8;
                int bx = (x + desplaz) / 16;
                bool junta = (y % 8 == 0) || ((x + desplaz) % 16 == 0);
                float var = (Ruido(bx, fila, 121) - 0.5f) * 0.08f;
                float n = (Ruido(x, y, 122) - 0.5f) * 0.03f;
                Color c = junta ? new Color(0.13f, 0.12f, 0.11f, 1f) : new Color(0.3f + var + n, 0.14f + var * 0.5f + n, 0.11f + n, 1f);
                px[y * tam + x] = c;
            }
        t.SetPixels(px);
        t.Apply();
        return t;
    }

    private static bool EnRectRedondeado(float x, float y, float x0, float y0, float x1, float y1, float r)
    {
        if (x < x0 || x > x1 || y < y0 || y > y1) return false;
        float cx = Mathf.Clamp(x, x0 + r, x1 - r), cy = Mathf.Clamp(y, y0 + r, y1 - r);
        return (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r;
    }

    private static Texture2D TexturaPatrulla()
    {
        int w = 96, h = 192;
        Texture2D t = NuevaTextura(w, h);
        Color[] px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color c = Color.clear;
                if (EnRectRedondeado(x, y, 8, 4, 87, 187, 18))
                {
                    c = new Color(0.07f, 0.07f, 0.08f, 1f);                                         // carrocería negra
                    if (y > 62 && y < 132) c = new Color(0.86f, 0.86f, 0.84f, 1f);                  // puertas blancas
                    if (EnRectRedondeado(x, y, 18, 132, 77, 156, 8)) c = new Color(0.08f, 0.12f, 0.18f, 1f); // parabrisas
                    if (EnRectRedondeado(x, y, 18, 40, 77, 60, 6)) c = new Color(0.08f, 0.12f, 0.18f, 1f);   // medallón
                    if (y >= 94 && y < 104 && x > 20 && x < 76) c = x < 48 ? new Color(0.95f, 0.1f, 0.1f, 1f) : new Color(0.15f, 0.3f, 1f, 1f); // torreta
                    if (y > 172 && (x < 26 || x > 69)) c = new Color(0.95f, 0.92f, 0.7f, 1f);       // faros
                    if (x < 11 || x > 84) c *= 0.7f;                                                // sombra lateral
                    c.a = 1f;
                }
                if ((x < 9 || x > 86) && ((y > 26 && y < 50) || (y > 140 && y < 164))) c = new Color(0.03f, 0.03f, 0.03f, 1f); // llantas
                px[y * w + x] = c;
            }
        t.SetPixels(px);
        t.Apply();
        return t;
    }

    private static Texture2D TexturaAutoQuemado()
    {
        int w = 96, h = 192;
        Texture2D t = NuevaTextura(w, h);
        Color[] px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color c = Color.clear;
                if (EnRectRedondeado(x, y, 8, 4, 87, 187, 16))
                {
                    float oxido = RuidoSuave(x, y, 12, 16, 131);
                    c = Color.Lerp(new Color(0.08f, 0.07f, 0.06f, 1f), new Color(0.32f, 0.16f, 0.07f, 1f), Mathf.Clamp01((oxido - 0.4f) * 2f));
                    if (EnRectRedondeado(x, y, 18, 128, 77, 154, 6) || EnRectRedondeado(x, y, 18, 40, 77, 60, 6))
                        c = Ruido(x / 2, y / 2, 132) > 0.7f ? new Color(0.5f, 0.55f, 0.6f, 1f) : new Color(0.02f, 0.02f, 0.02f, 1f); // vidrio roto
                    if (Ruido(x / 3, y / 3, 133) > 0.94f && y > 60 && y < 128) c = new Color(1f, 0.5f, 0.12f, 1f);           // brasas
                    if (x < 11 || x > 84) c *= 0.7f;
                    c.a = 1f;
                }
                if ((x < 9 || x > 86) && ((y > 26 && y < 50) || (y > 140 && y < 164))) c = new Color(0.05f, 0.04f, 0.04f, 1f);
                px[y * w + x] = c;
            }
        t.SetPixels(px);
        t.Apply();
        return t;
    }

    private static Texture2D TexturaBarricada()
    {
        int w = 192, h = 48;
        Texture2D t = NuevaTextura(w, h);
        Color[] px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color c = Color.clear;
                if (y >= 12 && y < 36 && x >= 4 && x < w - 4)
                {
                    c = ((x + y) / 12) % 2 == 0 ? new Color(0.95f, 0.45f, 0.08f, 1f) : new Color(0.92f, 0.92f, 0.88f, 1f);
                    if (y == 12 || y == 35) c = new Color(0.1f, 0.1f, 0.1f, 1f);
                }
                if ((x < 18 || x >= w - 18) && (y < 12 || y >= 36) && (x % 18) > 4 && (x % 18) < 12) c = new Color(0.15f, 0.15f, 0.15f, 1f); // patas
                px[y * w + x] = c;
            }
        t.SetPixels(px);
        t.Apply();
        return t;
    }

    private static Texture2D TexturaContenedor()
    {
        int w = 128, h = 80;
        Texture2D t = NuevaTextura(w, h);
        Color[] px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float n = (Ruido(x, y, 141) - 0.5f) * 0.04f;
                Color c = new Color(0.1f + n, 0.24f + n, 0.14f + n, 1f);
                if (x % 16 < 2) c *= 0.7f;                                  // nervaduras de la tapa
                if (x < 3 || y < 3 || x >= w - 3 || y >= h - 3) c = new Color(0.04f, 0.08f, 0.05f, 1f);
                if (RuidoSuave(x, y, 10, 13, 142) > 0.72f) c = Color.Lerp(c, new Color(0.35f, 0.18f, 0.08f, 1f), 0.6f); // óxido
                c.a = 1f;
                px[y * w + x] = c;
            }
        t.SetPixels(px);
        t.Apply();
        return t;
    }

    private static Texture2D TexturaFarola()
    {
        int tam = 48;
        Texture2D t = NuevaTextura(tam, tam);
        Color[] px = new Color[tam * tam];
        for (int y = 0; y < tam; y++)
            for (int x = 0; x < tam; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(24f, 24f));
                Color c = Color.clear;
                if (d < 20f) c = new Color(0.16f, 0.17f, 0.19f, 1f);
                if (d < 16f) c = new Color(0.28f, 0.29f, 0.31f, 1f);
                if (d < 9f) c = new Color(1f, 0.9f, 0.62f, 1f);           // lámpara
                if (d >= 19f && d < 20f) c = new Color(0.05f, 0.05f, 0.06f, 1f);
                px[y * tam + x] = c;
            }
        t.SetPixels(px);
        t.Apply();
        return t;
    }
}
#endif
