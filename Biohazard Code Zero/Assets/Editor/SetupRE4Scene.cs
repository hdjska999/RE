#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.IO;

public static partial class SetupRE4Scene
{
    private const string CARPETA_AUDIO = "Assets/Audio";
    private const string RUTA_SPRITE_ANILLO = "Assets/Sprites/AnilloVidaRE4.png";
    private const string RUTA_SPRITE_CALAVERA = "Assets/Sprites/IconoBajasRE4.png";
    private const string RUTA_SPRITE_RELOJ = "Assets/Sprites/IconoRelojRE4.png";
    private const string RUTA_SPRITE_ESTRELLA = "Assets/Sprites/IconoPuntajeRE4.png";
    private const string RUTA_SPRITE_VISERA = "Assets/Sprites/ViseraDegradadaRE4.png";
    private const string RUTA_FUENTE_CINZEL = "Assets/Fonts/Cinzel-VariableFont_wght SDF.asset";

    [MenuItem("Tools/Configurar Escena RE4")]
    public static void ConfigurarEscena()
    {
        Undo.SetCurrentGroupName("Configurar Escena RE4");
        int group = Undo.GetCurrentGroup();

        // 1. Configurar Canvas y CanvasScaler en 1920x1080 (Match 0.5)
        Canvas canvas = ObtenerOConfigurarCanvas();

        // 2. Generar audio procedural realista y vincular al AudioManager
        GenerarAudioProceduralYVincular();

        // 3. Generar todos los sprites necesarios por código
        Sprite spriteAnillo = GenerarSpriteAnilloHueco();
        Sprite spriteCalavera = GenerarSpriteCalavera();
        Sprite spriteReloj = GenerarSpriteReloj();
        Sprite spriteEstrella = GenerarSpriteEstrella();
        Sprite spriteVisera = GenerarSpriteVisera();

        // 4. Configurar HUD Radial abajo a la derecha con márgenes seguros
        ConfigurarHUDRadial(canvas, spriteAnillo);

        // 5. Configurar Visera Superior RE4 con márgenes y los 3 bloques horizontales
        ConfigurarViseraSuperiorRE4(canvas, spriteVisera, spriteCalavera, spriteReloj, spriteEstrella);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Undo.CollapseUndoOperations(group);

        string mensaje = "¡Ajustes de HUD, Audio y Game Feel aplicados con éxito!\n\n" +
            "1. [CANVAS] CanvasScaler ajustado a 'Scale With Screen Size' (1920x1080, Match 0.5).\n" +
            "2. [MÁRGENES] Visera superior y HUD inferior derecho ajustados con padding seguro para no cortarse.\n" +
            "3. [AUDIO REALISTA] Disparo seco de pólvora + Gruñidos guturales rasposos de zombie con caída de frecuencia al morir.\n" +
            "4. [GAME FEEL] Camera Shake configurado en PlayerMovement (disparos) y PlayerHealth (daño).";

        EditorUtility.DisplayDialog("Biohazard Code Zero - Setup RE4", mensaje, "Aceptar");
        Debug.Log("[SetupRE4Scene] " + mensaje);
    }

    // ==========================================
    // CONFIGURACIÓN DEL CANVAS Y ESCALADOR
    // ==========================================

    private static Canvas ObtenerOConfigurarCanvas()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Undo.RegisterCreatedObjectUndo(canvasGo, "Crear Canvas");
        }

        // Configuración requerida: Scale With Screen Size (1920x1080, Match 0.5)
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = Undo.AddComponent<CanvasScaler>(canvas.gameObject);
        }

        Undo.RecordObject(scaler, "Configurar CanvasScaler RE4");
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    // ==========================================
    // GENERACIÓN DE AUDIO PROCEDURAL .WAV
    // ==========================================

    private static void GenerarAudioProceduralYVincular()
    {
        if (!AssetDatabase.IsValidFolder(CARPETA_AUDIO))
        {
            AssetDatabase.CreateFolder("Assets", "Audio");
        }

        // 1. Disparo: Detonación real, seca y explosiva (Ruido blanco + snap + punch grave de 52Hz)
        AudioClip clipDisparo = ObtenerOGenerarClip("Assets/Audio/sfx_disparo.wav", () => {
            int sr = 44100;
            float dur = 0.22f;
            int n = (int)(sr * dur);
            float[] s = new float[n];
            float lpf = 0f;
            float prevNoise = 0f;

            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sr;
                float noise = (Random.value * 2f - 1f);

                float snapEnv = Mathf.Exp(-t * 140f);
                float snap = (noise - prevNoise) * snapEnv * 1.8f;
                prevNoise = noise;

                float blastEnv = Mathf.Exp(-t * 32f);
                float blast = noise * blastEnv * 1.2f;

                float thumpEnv = Mathf.Exp(-t * 28f);
                float subPunch = Mathf.Sin(2f * Mathf.PI * 52f * t) * thumpEnv * 0.85f;

                lpf += (noise - lpf) * 0.12f;
                float lowRumble = lpf * thumpEnv * 2.4f;

                float mezcla = snap * 0.85f + blast * 1.0f + lowRumble * 1.2f + subPunch * 0.7f;
                float inputDrive = Mathf.Clamp(mezcla * 2.2f, -1.25f, 1.25f);
                float sat = inputDrive - (inputDrive * inputDrive * inputDrive) / 3f;

                float fadeOut = (t > dur - 0.02f) ? (dur - t) / 0.02f : 1f;
                s[i] = Mathf.Clamp(sat * 0.95f * fadeOut, -1f, 1f);
            }
            return CrearWavData(s, sr);
        }, true);

        // 2. Recarga: Doble clic mecánico metálico
        AudioClip clipRecarga = ObtenerOGenerarClip("Assets/Audio/sfx_recarga.wav", () => {
            int sr = 44100;
            float dur = 0.38f;
            int n = (int)(sr * dur);
            float[] s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sr;
                float v = 0f;
                if (t >= 0.04f && t < 0.14f)
                {
                    float dt = t - 0.04f;
                    v += (Mathf.Sin(2f * Mathf.PI * 1800f * dt) + (Random.value * 2f - 1f) * 0.4f) * Mathf.Exp(-dt * 65f) * 0.7f;
                }
                if (t >= 0.20f && t < 0.35f)
                {
                    float dt = t - 0.20f;
                    v += (Mathf.Sin(2f * Mathf.PI * 850f * dt) + (Random.value * 2f - 1f) * 0.5f) * Mathf.Exp(-dt * 45f) * 0.85f;
                }
                s[i] = v;
            }
            return CrearWavData(s, sr);
        });

        // 3. Clic sin balas: Percutor metálico seco
        AudioClip clipSinBalas = ObtenerOGenerarClip("Assets/Audio/sfx_sin_balas.wav", () => {
            int sr = 44100;
            float dur = 0.09f;
            int n = (int)(sr * dur);
            float[] s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sr;
                float env = Mathf.Exp(-t * 85f);
                float tone = Mathf.Sin(2f * Mathf.PI * 2200f * t);
                float noise = (Random.value * 2f - 1f) * 0.3f;
                s[i] = (tone + noise) * env * 0.8f;
            }
            return CrearWavData(s, sr);
        });

        // 4. Impacto de bala: Golpe sordo en carne
        AudioClip clipImpacto = ObtenerOGenerarClip("Assets/Audio/sfx_impacto_bala.wav", () => {
            int sr = 44100;
            float dur = 0.16f;
            int n = (int)(sr * dur);
            float[] s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sr;
                float env = Mathf.Exp(-t * 35f);
                float thud = Mathf.Sin(2f * Mathf.PI * 95f * t);
                float squish = (Random.value * 2f - 1f) * Mathf.Exp(-t * 60f);
                s[i] = (thud * 0.65f + squish * 0.5f) * env;
            }
            return CrearWavData(s, sr);
        });

        // 5. Daño Zombie: Gruñido gutural grave y rasposo (fuente glotal + formantes + subarmónicos)
        AudioClip clipDañoZombie = ObtenerOGenerarClip("Assets/Audio/sfx_daño_zombie.wav", () =>
            CrearWavData(SintetizarGruñidoZombie(44100, 0.42f, 95f, 68f, 0.85f, 520f, 430f, 0.03f, 1.3f, 1), 44100), true);

        // 6. Muerte Zombie: Estertor largo con caída de tono (115Hz -> 38Hz) y vocal fry que se apaga
        AudioClip clipMuerteZombie = ObtenerOGenerarClip("Assets/Audio/sfx_muerte_zombie.wav", () =>
            CrearWavData(SintetizarGruñidoZombie(44100, 1.1f, 115f, 38f, 1.0f, 620f, 340f, 0.05f, 1.1f, 2), 44100), true);

        // 7. Daño Bryan: Jadeo / quejido seco
        AudioClip clipDañoBryan = ObtenerOGenerarClip("Assets/Audio/sfx_daño_bryan.wav", () => {
            int sr = 44100;
            float dur = 0.22f;
            int n = (int)(sr * dur);
            float[] s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sr;
                float env = Mathf.Exp(-t * 22f);
                float tone = Mathf.Sin(2f * Mathf.PI * 175f * t);
                float grunt = (Random.value * 2f - 1f) * 0.35f;
                s[i] = (tone * 0.7f + grunt) * env;
            }
            return CrearWavData(s, sr);
        });

        // 8. Recoger Munición: Tintineo de cartuchos metálicos
        AudioClip clipItemMunicion = ObtenerOGenerarClip("Assets/Audio/sfx_item_municion.wav", () => {
            int sr = 44100;
            float dur = 0.22f;
            int n = (int)(sr * dur);
            float[] s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sr;
                float v = Mathf.Sin(2f * Mathf.PI * 1250f * t) * Mathf.Exp(-t * 30f) * 0.6f;
                if (t >= 0.06f)
                {
                    float dt = t - 0.06f;
                    v += Mathf.Sin(2f * Mathf.PI * 1850f * dt) * Mathf.Exp(-dt * 35f) * 0.7f;
                }
                s[i] = v;
            }
            return CrearWavData(s, sr);
        });

        // 9. Recoger Curación (Hierba Verde RE): Acorde cálido trifónico (Do5, Mi5, Sol5)
        AudioClip clipItemCuracion = ObtenerOGenerarClip("Assets/Audio/sfx_item_curacion.wav", () => {
            int sr = 44100;
            float dur = 0.45f;
            int n = (int)(sr * dur);
            float[] s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sr;
                float env = Mathf.Exp(-t * 7f);
                float c5 = Mathf.Sin(2f * Mathf.PI * 523.25f * t);
                float e5 = Mathf.Sin(2f * Mathf.PI * 659.25f * t);
                float g5 = Mathf.Sin(2f * Mathf.PI * 783.99f * t);
                s[i] = (c5 * 0.35f + e5 * 0.35f + g5 * 0.35f) * env;
            }
            return CrearWavData(s, sr);
        });

        // 10. Recoger Tiempo: Campanas arpegiadas de bonificación
        AudioClip clipItemTiempo = ObtenerOGenerarClip("Assets/Audio/sfx_item_tiempo.wav", () => {
            int sr = 44100;
            float dur = 0.36f;
            int n = (int)(sr * dur);
            float[] s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sr;
                float v = 0f;
                if (t < 0.10f) v += Mathf.Sin(2f * Mathf.PI * 880f * t) * Mathf.Exp(-t * 20f);
                if (t >= 0.08f && t < 0.20f) v += Mathf.Sin(2f * Mathf.PI * 1174.66f * (t - 0.08f)) * Mathf.Exp(-(t - 0.08f) * 20f);
                if (t >= 0.16f) v += Mathf.Sin(2f * Mathf.PI * 1760f * (t - 0.16f)) * Mathf.Exp(-(t - 0.16f) * 15f);
                s[i] = v * 0.6f;
            }
            return CrearWavData(s, sr);
        });

        // 11. Botón Hover
        AudioClip clipHover = ObtenerOGenerarClip("Assets/Audio/sfx_ui_hover.wav", () => {
            int sr = 44100;
            float dur = 0.05f;
            int n = (int)(sr * dur);
            float[] s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sr;
                s[i] = Mathf.Sin(2f * Mathf.PI * 1500f * t) * Mathf.Exp(-t * 90f) * 0.4f;
            }
            return CrearWavData(s, sr);
        });

        // 12. Botón Click
        AudioClip clipClick = ObtenerOGenerarClip("Assets/Audio/sfx_ui_click.wav", () => {
            int sr = 44100;
            float dur = 0.08f;
            int n = (int)(sr * dur);
            float[] s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sr;
                s[i] = Mathf.Sin(2f * Mathf.PI * 920f * t) * Mathf.Exp(-t * 60f) * 0.65f;
            }
            return CrearWavData(s, sr);
        });

        // 13. Loop Ambiental Tenso (Heartbeat + Dark Drone)
        AudioClip clipAmbiente = ObtenerOGenerarClip("Assets/Audio/bgm_ambiente_tenso.wav", () => {
            int sr = 44100;
            float dur = 4.8f;
            int n = (int)(sr * dur);
            float[] s = new float[n];
            float intervalLatido = 1.2f;

            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sr;
                float drone = (Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.25f + Mathf.Sin(2f * Mathf.PI * 82.5f * t) * 0.15f);

                float tiempoCiclo = t % intervalLatido;
                float heartbeat = 0f;
                if (tiempoCiclo < 0.18f)
                    heartbeat += Mathf.Sin(2f * Mathf.PI * 48f * tiempoCiclo) * Mathf.Exp(-tiempoCiclo * 25f) * 0.6f;
                if (tiempoCiclo >= 0.14f && tiempoCiclo < 0.35f)
                {
                    float dt = tiempoCiclo - 0.14f;
                    heartbeat += Mathf.Sin(2f * Mathf.PI * 42f * dt) * Mathf.Exp(-dt * 20f) * 0.45f;
                }

                float ventanaLoop = 1f;
                float tiempoFade = 0.08f;
                if (t < tiempoFade) ventanaLoop = t / tiempoFade;
                else if (t > dur - tiempoFade) ventanaLoop = (dur - t) / tiempoFade;

                s[i] = (drone + heartbeat) * ventanaLoop * 0.8f;
            }
            return CrearWavData(s, sr);
        });

        // Vincular referencias con AudioManager
        AudioManager audioMgr = Object.FindAnyObjectByType<AudioManager>();
        if (audioMgr == null)
        {
            GameObject audioGo = new GameObject("AudioManager");
            audioMgr = audioGo.AddComponent<AudioManager>();
            Undo.RegisterCreatedObjectUndo(audioGo, "Crear AudioManager");
        }

        Undo.RecordObject(audioMgr, "Asignar AudioClips al AudioManager");
        audioMgr.musicaPrincipal = clipAmbiente;
        audioMgr.sfxDisparo = clipDisparo;
        audioMgr.sfxRecarga = clipRecarga;
        audioMgr.sfxSinBalas = clipSinBalas;
        audioMgr.sfxImpactoBala = clipImpacto;
        audioMgr.sfxDañoZombie = clipDañoZombie;
        audioMgr.sfxMuerteZombie = clipMuerteZombie;
        audioMgr.sfxDañoBryan = clipDañoBryan;
        audioMgr.sfxItemMunicion = clipItemMunicion;
        audioMgr.sfxItemCuracion = clipItemCuracion;
        audioMgr.sfxItemTiempo = clipItemTiempo;
        audioMgr.sfxBotonHover = clipHover;
        audioMgr.sfxBotonClick = clipClick;

        EditorUtility.SetDirty(audioMgr);
    }

    private static AudioClip ObtenerOGenerarClip(string rutaRelativa, System.Func<byte[]> generadorBytes, bool forzarRegenerar = false)
    {
        if (!forzarRegenerar)
        {
            AudioClip existente = AssetDatabase.LoadAssetAtPath<AudioClip>(rutaRelativa);
            if (existente != null) return existente;
        }

        string rutaCompleta = Path.Combine(Application.dataPath, rutaRelativa.Replace("Assets/", ""));
        byte[] datosWav = generadorBytes();
        File.WriteAllBytes(rutaCompleta, datosWav);

        AssetDatabase.ImportAsset(rutaRelativa, ImportAssetOptions.ForceUpdate);
        return AssetDatabase.LoadAssetAtPath<AudioClip>(rutaRelativa);
    }

    private static byte[] CrearWavData(float[] samples, int sampleRate)
    {
        using (MemoryStream ms = new MemoryStream())
        using (BinaryWriter bw = new BinaryWriter(ms))
        {
            int numSamples = samples.Length;
            int subChunk2Size = numSamples * 2;

            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + subChunk2Size);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

            bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16);
            bw.Write((short)1);
            bw.Write((short)1);
            bw.Write(sampleRate);
            bw.Write(sampleRate * 2);
            bw.Write((short)2);
            bw.Write((short)16);

            bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            bw.Write(subChunk2Size);

            for (int i = 0; i < numSamples; i++)
            {
                float muestra = Mathf.Clamp(samples[i], -1f, 1f);
                short pcm = (short)(muestra * 32767f);
                bw.Write(pcm);
            }

            return ms.ToArray();
        }
    }

    // ==========================================
    // GENERACIÓN PROCEDURAL DE TEXTURAS / SPRITES
    // ==========================================

    private static void AsegurarCarpetaSprites()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Sprites"))
        {
            AssetDatabase.CreateFolder("Assets", "Sprites");
        }
    }

    private static Sprite ImportarTexturaComoSprite(string rutaRelativa, Texture2D tex)
    {
        AsegurarCarpetaSprites();
        string rutaCompleta = Path.Combine(Application.dataPath, rutaRelativa.Replace("Assets/", ""));
        File.WriteAllBytes(rutaCompleta, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(rutaRelativa, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(rutaRelativa) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(rutaRelativa);
    }

    private static Sprite GenerarSpriteAnilloHueco()
    {
        Sprite spriteExistente = AssetDatabase.LoadAssetAtPath<Sprite>(RUTA_SPRITE_ANILLO);
        if (spriteExistente != null) return spriteExistente;

        int tam = 256;
        Texture2D tex = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
        Vector2 centro = new Vector2(tam * 0.5f, tam * 0.5f);
        float rExt = (tam * 0.5f) - 6f;
        float rInt = rExt - 28f;

        Color[] pix = new Color[tam * tam];
        for (int y = 0; y < tam; y++)
        {
            for (int x = 0; x < tam; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), centro);
                float aExt = Mathf.Clamp01(rExt - d + 0.5f);
                float aInt = Mathf.Clamp01(d - rInt + 0.5f);
                pix[y * tam + x] = new Color(1f, 1f, 1f, Mathf.Min(aExt, aInt));
            }
        }
        tex.SetPixels(pix);
        tex.Apply();
        return ImportarTexturaComoSprite(RUTA_SPRITE_ANILLO, tex);
    }

    private static Sprite GenerarSpriteVisera()
    {
        Sprite existente = AssetDatabase.LoadAssetAtPath<Sprite>(RUTA_SPRITE_VISERA);
        if (existente != null) return existente;

        int ancho = 32;
        int alto = 128;
        Texture2D tex = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
        Color[] pix = new Color[ancho * alto];

        for (int y = 0; y < alto; y++)
        {
            float t = (float)y / (alto - 1f);
            float alfa = Mathf.Lerp(145f, 195f, t) / 255f;
            Color col = new Color(0.04f, 0.05f, 0.07f, alfa);

            for (int x = 0; x < ancho; x++)
            {
                pix[y * ancho + x] = col;
            }
        }
        tex.SetPixels(pix);
        tex.Apply();
        return ImportarTexturaComoSprite(RUTA_SPRITE_VISERA, tex);
    }

    private static Sprite GenerarSpriteCalavera()
    {
        Sprite existente = AssetDatabase.LoadAssetAtPath<Sprite>(RUTA_SPRITE_CALAVERA);
        if (existente != null) return existente;

        int tam = 128;
        Texture2D tex = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
        Color[] pix = new Color[tam * tam];

        Vector2[] subOffsets = new Vector2[] {
            new Vector2(-0.25f, -0.25f), new Vector2(0.25f, -0.25f),
            new Vector2(-0.25f, 0.25f), new Vector2(0.25f, 0.25f)
        };

        for (int y = 0; y < tam; y++)
        {
            for (int x = 0; x < tam; x++)
            {
                float acumulado = 0f;
                for (int s = 0; s < 4; s++)
                {
                    float px = x + 0.5f + subOffsets[s].x;
                    float py = y + 0.5f + subOffsets[s].y;

                    bool dentroCranium = Vector2.Distance(new Vector2(px, py), new Vector2(64f, 72f)) <= 35f;
                    bool dentroMandibula = (px >= 46f && px <= 82f && py >= 24f && py <= 52f);
                    if (py < 30f && (px < 50f || px > 78f)) dentroMandibula = false;

                    bool solido = dentroCranium || dentroMandibula;

                    if (solido)
                    {
                        float dOjoIzq = Mathf.Pow((px - 51f) / 8.5f, 2) + Mathf.Pow((py - 64f) / 11f, 2);
                        if (dOjoIzq <= 1f) solido = false;

                        float dOjoDer = Mathf.Pow((px - 77f) / 8.5f, 2) + Mathf.Pow((py - 64f) / 11f, 2);
                        if (dOjoDer <= 1f) solido = false;

                        if (py >= 46f && py <= 55f && Mathf.Abs(px - 64f) <= (55f - py) * 0.7f)
                            solido = false;

                        if (py >= 26f && py <= 36f && (Mathf.Abs(px - 56f) <= 1f || Mathf.Abs(px - 64f) <= 1f || Mathf.Abs(px - 72f) <= 1f))
                            solido = false;
                    }

                    if (solido) acumulado += 0.25f;
                }

                pix[y * tam + x] = new Color(1f, 1f, 1f, acumulado);
            }
        }
        tex.SetPixels(pix);
        tex.Apply();
        return ImportarTexturaComoSprite(RUTA_SPRITE_CALAVERA, tex);
    }

    private static Sprite GenerarSpriteReloj()
    {
        Sprite existente = AssetDatabase.LoadAssetAtPath<Sprite>(RUTA_SPRITE_RELOJ);
        if (existente != null) return existente;

        int tam = 128;
        Texture2D tex = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
        Color[] pix = new Color[tam * tam];

        Vector2[] subOffsets = new Vector2[] {
            new Vector2(-0.25f, -0.25f), new Vector2(0.25f, -0.25f),
            new Vector2(-0.25f, 0.25f), new Vector2(0.25f, 0.25f)
        };

        for (int y = 0; y < tam; y++)
        {
            for (int x = 0; x < tam; x++)
            {
                float acumulado = 0f;
                for (int s = 0; s < 4; s++)
                {
                    float px = x + 0.5f + subOffsets[s].x;
                    float py = y + 0.5f + subOffsets[s].y;

                    float dCentro = Vector2.Distance(new Vector2(px, py), new Vector2(64f, 56f));
                    bool anillo = (dCentro <= 38f && dCentro >= 31f);
                    bool corona = (px >= 58f && px <= 70f && py >= 94f && py <= 106f);
                    bool pivote = (dCentro <= 4.5f);
                    bool aguja1 = (px >= 62.5f && px <= 65.5f && py >= 56f && py <= 82f);
                    float dLinea2 = DistanciaPuntoSegmento(new Vector2(px, py), new Vector2(64f, 56f), new Vector2(81f, 70f));
                    bool aguja2 = dLinea2 <= 1.8f;

                    if (anillo || corona || pivote || aguja1 || aguja2) acumulado += 0.25f;
                }

                pix[y * tam + x] = new Color(1f, 1f, 1f, acumulado);
            }
        }
        tex.SetPixels(pix);
        tex.Apply();
        return ImportarTexturaComoSprite(RUTA_SPRITE_RELOJ, tex);
    }

    private static Sprite GenerarSpriteEstrella()
    {
        Sprite existente = AssetDatabase.LoadAssetAtPath<Sprite>(RUTA_SPRITE_ESTRELLA);
        if (existente != null) return existente;

        int tam = 128;
        Texture2D tex = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
        Color[] pix = new Color[tam * tam];

        Vector2[] verticesEstrella = new Vector2[10];
        for (int i = 0; i < 10; i++)
        {
            float angulo = (i * 36f - 90f) * Mathf.Deg2Rad;
            float r = (i % 2 == 0) ? 44f : 19f;
            verticesEstrella[i] = new Vector2(64f + r * Mathf.Cos(angulo), 64f - r * Mathf.Sin(angulo));
        }

        Vector2[] subOffsets = new Vector2[] {
            new Vector2(-0.25f, -0.25f), new Vector2(0.25f, -0.25f),
            new Vector2(-0.25f, 0.25f), new Vector2(0.25f, 0.25f)
        };

        for (int y = 0; y < tam; y++)
        {
            for (int x = 0; x < tam; x++)
            {
                float acumulado = 0f;
                for (int s = 0; s < 4; s++)
                {
                    Vector2 pt = new Vector2(x + 0.5f + subOffsets[s].x, y + 0.5f + subOffsets[s].y);
                    if (PuntoEnPoligono(pt, verticesEstrella))
                    {
                        acumulado += 0.25f;
                    }
                }
                pix[y * tam + x] = new Color(1f, 1f, 1f, acumulado);
            }
        }
        tex.SetPixels(pix);
        tex.Apply();
        return ImportarTexturaComoSprite(RUTA_SPRITE_ESTRELLA, tex);
    }

    private static float DistanciaPuntoSegmento(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 pa = p - a, ba = b - a;
        float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
        return (pa - ba * h).magnitude;
    }

    private static bool PuntoEnPoligono(Vector2 pt, Vector2[] poly)
    {
        bool inside = false;
        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
        {
            if (((poly[i].y > pt.y) != (poly[j].y > pt.y)) &&
                (pt.x < (poly[j].x - poly[i].x) * (pt.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x))
            {
                inside = !inside;
            }
        }
        return inside;
    }

    // ==========================================
    // CONFIGURACIÓN DE VISERA SUPERIOR RE4
    // ==========================================

    private static void ConfigurarViseraSuperiorRE4(Canvas canvas, Sprite spriteVisera, Sprite spriteCalavera, Sprite spriteReloj, Sprite spriteEstrella)
    {
        TMP_FontAsset fontCinzel = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RUTA_FUENTE_CINZEL);
        MercenariesManager mercMgr = Object.FindAnyObjectByType<MercenariesManager>();

        Transform topBarAntigua = canvas.transform.Find("TopBar_RE4_Mercenaries");
        if (topBarAntigua != null)
        {
            Undo.DestroyObjectImmediate(topBarAntigua.gameObject);
        }

        Transform textoOleadaTrans = canvas.transform.Find("Oleada");
        if (textoOleadaTrans != null)
        {
            Undo.RecordObject(textoOleadaTrans.gameObject, "Ocultar Texto Oleada");
            textoOleadaTrans.gameObject.SetActive(false);
        }

        Transform viseraTrans = canvas.transform.Find("ViseraSuperiorRE4");
        GameObject viseraGo;
        if (viseraTrans == null)
        {
            viseraGo = new GameObject("ViseraSuperiorRE4", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            viseraGo.transform.SetParent(canvas.transform, false);
            Undo.RegisterCreatedObjectUndo(viseraGo, "Crear ViseraSuperiorRE4");
        }
        else
        {
            viseraGo = viseraTrans.gameObject;
        }

        RectTransform viseraRect = viseraGo.GetComponent<RectTransform>();
        viseraRect.anchorMin = new Vector2(0.5f, 1f);
        viseraRect.anchorMax = new Vector2(0.5f, 1f);
        viseraRect.pivot = new Vector2(0.5f, 1f);
        viseraRect.anchoredPosition = new Vector2(0f, 0f);
        viseraRect.sizeDelta = new Vector2(880f, 76f);

        Image imgVisera = viseraGo.GetComponent<Image>();
        imgVisera.sprite = spriteVisera;
        imgVisera.type = Image.Type.Simple;
        imgVisera.color = Color.white;
        imgVisera.raycastTarget = false;

        Transform bordeTrans = viseraGo.transform.Find("BordeInferior");
        if (bordeTrans == null)
        {
            GameObject bordeGo = new GameObject("BordeInferior", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bordeGo.transform.SetParent(viseraGo.transform, false);
            RectTransform bordeRect = bordeGo.GetComponent<RectTransform>();
            bordeRect.anchorMin = new Vector2(0f, 0f);
            bordeRect.anchorMax = new Vector2(1f, 0f);
            bordeRect.pivot = new Vector2(0.5f, 0f);
            bordeRect.anchoredPosition = Vector2.zero;
            bordeRect.sizeDelta = new Vector2(0f, 2f);

            Image imgBorde = bordeGo.GetComponent<Image>();
            imgBorde.color = new Color(0.6f, 0.65f, 0.7f, 0.65f);
            imgBorde.raycastTarget = false;
            Undo.RegisterCreatedObjectUndo(bordeGo, "Crear BordeInferior");
        }

        // 1. IZQUIERDA: Bajas (Con margen interno de 50px para visibilidad total)
        Transform secBajasTrans = viseraGo.transform.Find("SeccionBajas");
        GameObject secBajasGo = secBajasTrans != null ? secBajasTrans.gameObject : new GameObject("SeccionBajas", typeof(RectTransform));
        if (secBajasTrans == null)
        {
            secBajasGo.transform.SetParent(viseraGo.transform, false);
            Undo.RegisterCreatedObjectUndo(secBajasGo, "Crear SeccionBajas");
        }
        RectTransform rSecBajas = secBajasGo.GetComponent<RectTransform>();
        rSecBajas.anchorMin = new Vector2(0f, 0.5f);
        rSecBajas.anchorMax = new Vector2(0f, 0.5f);
        rSecBajas.pivot = new Vector2(0f, 0.5f);
        rSecBajas.anchoredPosition = new Vector2(50f, 0f);
        rSecBajas.sizeDelta = new Vector2(180f, 60f);

        Transform iconoBajasTrans = secBajasGo.transform.Find("IconoBajas");
        GameObject iconoBajasGo = iconoBajasTrans != null ? iconoBajasTrans.gameObject : new GameObject("IconoBajas", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (iconoBajasTrans == null)
        {
            iconoBajasGo.transform.SetParent(secBajasGo.transform, false);
            Undo.RegisterCreatedObjectUndo(iconoBajasGo, "Crear IconoBajas");
        }
        RectTransform rIconoBajas = iconoBajasGo.GetComponent<RectTransform>();
        rIconoBajas.anchorMin = new Vector2(0f, 0.5f);
        rIconoBajas.anchorMax = new Vector2(0f, 0.5f);
        rIconoBajas.pivot = new Vector2(0f, 0.5f);
        rIconoBajas.anchoredPosition = new Vector2(0f, 0f);
        rIconoBajas.sizeDelta = new Vector2(28f, 28f);
        Image imgIconoBajas = iconoBajasGo.GetComponent<Image>();
        imgIconoBajas.sprite = spriteCalavera;
        imgIconoBajas.color = new Color(0.95f, 0.95f, 0.95f, 1f);
        imgIconoBajas.raycastTarget = false;

        TextMeshProUGUI tmpBajas = ObtenerOReparentarTexto(canvas.transform, secBajasGo.transform, "TextoBajas", "0", fontCinzel, 34, Color.white, new Vector2(38f, 0f), new Vector2(130f, 50f), TextAlignmentOptions.Left);

        // 2. CENTRO: Reloj con centésimas
        Transform secTiempoTrans = viseraGo.transform.Find("SeccionTiempo");
        GameObject secTiempoGo = secTiempoTrans != null ? secTiempoTrans.gameObject : new GameObject("SeccionTiempo", typeof(RectTransform));
        if (secTiempoTrans == null)
        {
            secTiempoGo.transform.SetParent(viseraGo.transform, false);
            Undo.RegisterCreatedObjectUndo(secTiempoGo, "Crear SeccionTiempo");
        }
        RectTransform rSecTiempo = secTiempoGo.GetComponent<RectTransform>();
        rSecTiempo.anchorMin = new Vector2(0.5f, 0.5f);
        rSecTiempo.anchorMax = new Vector2(0.5f, 0.5f);
        rSecTiempo.pivot = new Vector2(0.5f, 0.5f);
        rSecTiempo.anchoredPosition = new Vector2(0f, 0f);
        rSecTiempo.sizeDelta = new Vector2(320f, 66f);

        Transform iconoRelojTrans = secTiempoGo.transform.Find("IconoReloj");
        GameObject iconoRelojGo = iconoRelojTrans != null ? iconoRelojTrans.gameObject : new GameObject("IconoReloj", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (iconoRelojTrans == null)
        {
            iconoRelojGo.transform.SetParent(secTiempoGo.transform, false);
            Undo.RegisterCreatedObjectUndo(iconoRelojGo, "Crear IconoReloj");
        }
        RectTransform rIconoReloj = iconoRelojGo.GetComponent<RectTransform>();
        rIconoReloj.anchorMin = new Vector2(0.5f, 0.5f);
        rIconoReloj.anchorMax = new Vector2(0.5f, 0.5f);
        rIconoReloj.pivot = new Vector2(0.5f, 0.5f);
        rIconoReloj.anchoredPosition = new Vector2(-115f, 2f);
        rIconoReloj.sizeDelta = new Vector2(26f, 26f);
        Image imgIconoReloj = iconoRelojGo.GetComponent<Image>();
        imgIconoReloj.sprite = spriteReloj;
        imgIconoReloj.color = new Color(0.9f, 0.93f, 0.98f, 0.95f);
        imgIconoReloj.raycastTarget = false;

        TextMeshProUGUI tmpTiempo = ObtenerOReparentarTexto(canvas.transform, secTiempoGo.transform, "Tiempo", "02:00.00", fontCinzel, 36, Color.white, new Vector2(10f, 2f), new Vector2(220f, 48f), TextAlignmentOptions.Center);

        Transform contComboTrans = secTiempoGo.transform.Find("Contenedor_Combo");
        GameObject contComboGo = contComboTrans != null ? contComboTrans.gameObject : new GameObject("Contenedor_Combo", typeof(RectTransform));
        if (contComboTrans == null)
        {
            contComboGo.transform.SetParent(secTiempoGo.transform, false);
            Undo.RegisterCreatedObjectUndo(contComboGo, "Crear Contenedor_Combo");
        }
        RectTransform rContCombo = contComboGo.GetComponent<RectTransform>();
        rContCombo.anchoredPosition = new Vector2(10f, -24f);
        rContCombo.sizeDelta = new Vector2(180f, 16f);

        TextMeshProUGUI tmpCombo = ObtenerOReparentarTexto(canvas.transform, contComboGo.transform, "Combo", "x2 COMBO", fontCinzel, 13, new Color(1f, 0.85f, 0.2f, 1f), new Vector2(0f, 1f), new Vector2(160f, 14f), TextAlignmentOptions.Center);

        Transform barraComboTrans = contComboGo.transform.Find("BarraCombo");
        GameObject barraComboGo = barraComboTrans != null ? barraComboTrans.gameObject : new GameObject("BarraCombo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (barraComboTrans == null)
        {
            barraComboGo.transform.SetParent(contComboGo.transform, false);
            Undo.RegisterCreatedObjectUndo(barraComboGo, "Crear BarraCombo");
        }
        RectTransform rBarra = barraComboGo.GetComponent<RectTransform>();
        rBarra.anchoredPosition = new Vector2(0f, -8f);
        rBarra.sizeDelta = new Vector2(140f, 3f);
        Image imgBarraCombo = barraComboGo.GetComponent<Image>();
        imgBarraCombo.type = Image.Type.Filled;
        imgBarraCombo.fillMethod = Image.FillMethod.Horizontal;
        imgBarraCombo.fillOrigin = (int)Image.OriginHorizontal.Left;
        imgBarraCombo.color = new Color(1f, 0.85f, 0.2f, 1f);
        imgBarraCombo.raycastTarget = false;

        // 3. DERECHA: Puntaje + Estrella (Con margen interno de 50px)
        Transform secPuntajeTrans = viseraGo.transform.Find("SeccionPuntaje");
        GameObject secPuntajeGo = secPuntajeTrans != null ? secPuntajeTrans.gameObject : new GameObject("SeccionPuntaje", typeof(RectTransform));
        if (secPuntajeTrans == null)
        {
            secPuntajeGo.transform.SetParent(viseraGo.transform, false);
            Undo.RegisterCreatedObjectUndo(secPuntajeGo, "Crear SeccionPuntaje");
        }
        RectTransform rSecPuntaje = secPuntajeGo.GetComponent<RectTransform>();
        rSecPuntaje.anchorMin = new Vector2(1f, 0.5f);
        rSecPuntaje.anchorMax = new Vector2(1f, 0.5f);
        rSecPuntaje.pivot = new Vector2(1f, 0.5f);
        rSecPuntaje.anchoredPosition = new Vector2(-50f, 0f);
        rSecPuntaje.sizeDelta = new Vector2(210f, 60f);

        TextMeshProUGUI tmpPuntaje = ObtenerOReparentarTexto(canvas.transform, secPuntajeGo.transform, "Puntaje", "0", fontCinzel, 32, new Color(1f, 0.94f, 0.8f, 1f), new Vector2(-36f, 0f), new Vector2(160f, 50f), TextAlignmentOptions.Right);

        Transform iconoPuntajeTrans = secPuntajeGo.transform.Find("IconoPuntaje");
        GameObject iconoPuntajeGo = iconoPuntajeTrans != null ? iconoPuntajeTrans.gameObject : new GameObject("IconoPuntaje", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (iconoPuntajeTrans == null)
        {
            iconoPuntajeGo.transform.SetParent(secPuntajeGo.transform, false);
            Undo.RegisterCreatedObjectUndo(iconoPuntajeGo, "Crear IconoPuntaje");
        }
        RectTransform rIconoPuntaje = iconoPuntajeGo.GetComponent<RectTransform>();
        rIconoPuntaje.anchorMin = new Vector2(1f, 0.5f);
        rIconoPuntaje.anchorMax = new Vector2(1f, 0.5f);
        rIconoPuntaje.pivot = new Vector2(1f, 0.5f);
        rIconoPuntaje.anchoredPosition = new Vector2(0f, 0f);
        rIconoPuntaje.sizeDelta = new Vector2(26f, 26f);
        Image imgIconoPuntaje = iconoPuntajeGo.GetComponent<Image>();
        imgIconoPuntaje.sprite = spriteEstrella;
        imgIconoPuntaje.color = new Color(1f, 0.85f, 0.25f, 1f);
        imgIconoPuntaje.raycastTarget = false;

        foreach (Transform hijo in canvas.transform)
        {
            if (hijo != viseraGo.transform && (hijo.name == "TextoBajas" || hijo.name == "BajasSueltas"))
            {
                Undo.DestroyObjectImmediate(hijo.gameObject);
                break;
            }
        }

        if (mercMgr != null)
        {
            Undo.RecordObject(mercMgr, "Vincular UI MercenariesManager");
            mercMgr.textoTiempo = tmpTiempo;
            mercMgr.textoPuntaje = tmpPuntaje;
            mercMgr.textoCombo = tmpCombo;
            mercMgr.textoBajas = tmpBajas;
            mercMgr.barraCombo = imgBarraCombo;
        }
    }

    // ==========================================
    // CONFIGURACIÓN DE HUD RADIAL INFERIOR
    // ==========================================

    private static void ConfigurarHUDRadial(Canvas canvas, Sprite spriteAnillo)
    {
        Transform contenedorArma = canvas.transform.Find("ContenedorArmaUI");
        Transform iconoArmaTransform = null;

        if (contenedorArma == null)
        {
            GameObject contGo = new GameObject("ContenedorArmaUI", typeof(RectTransform));
            contGo.transform.SetParent(canvas.transform, false);
            contenedorArma = contGo.transform;
            Undo.RegisterCreatedObjectUndo(contGo, "Crear ContenedorArmaUI");
        }
        else
        {
            iconoArmaTransform = contenedorArma.Find("IconoArma");
        }

        // Anclar en esquina inferior derecha con margen seguro de 55px
        RectTransform contRect = contenedorArma.GetComponent<RectTransform>();
        contRect.anchorMin = new Vector2(1f, 0f);
        contRect.anchorMax = new Vector2(1f, 0f);
        contRect.pivot = new Vector2(1f, 0f);
        contRect.anchoredPosition = new Vector2(-55f, 55f);
        contRect.sizeDelta = new Vector2(260f, 140f);

        // Centro relativo para el anillo e ícono
        Vector2 posicionCentro = new Vector2(-70f, 70f);

        // Posicionar Ícono del Arma si existe
        if (iconoArmaTransform != null)
        {
            RectTransform iconRect = iconoArmaTransform.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(1f, 0f);
            iconRect.anchorMax = new Vector2(1f, 0f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = posicionCentro;
            iconRect.sizeDelta = new Vector2(85f, 85f);
        }

        // Reposicionar TextoMunicion si existe
        Transform textoMunicionTrans = contenedorArma.Find("TextoMunicion");
        if (textoMunicionTrans != null)
        {
            RectTransform rTexto = textoMunicionTrans.GetComponent<RectTransform>();
            rTexto.anchorMin = new Vector2(1f, 0f);
            rTexto.anchorMax = new Vector2(1f, 0f);
            rTexto.pivot = new Vector2(1f, 0.5f);
            rTexto.anchoredPosition = new Vector2(-140f, 70f);
            rTexto.sizeDelta = new Vector2(120f, 50f);

            TextMeshProUGUI tmpMun = textoMunicionTrans.GetComponent<TextMeshProUGUI>();
            if (tmpMun != null)
            {
                tmpMun.alignment = TextAlignmentOptions.Right;
                tmpMun.fontSize = 32;
            }
        }

        // Fondo del anillo (riel oscuro)
        Transform fondoExistente = contenedorArma.Find("FondoAnilloVida");
        GameObject fondoGo;
        if (fondoExistente == null)
        {
            fondoGo = new GameObject("FondoAnilloVida", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fondoGo.transform.SetParent(contenedorArma, false);
            Undo.RegisterCreatedObjectUndo(fondoGo, "Crear Fondo Anillo Vida");
        }
        else
        {
            fondoGo = fondoExistente.gameObject;
        }

        RectTransform fondoRect = fondoGo.GetComponent<RectTransform>();
        fondoRect.anchorMin = new Vector2(1f, 0f);
        fondoRect.anchorMax = new Vector2(1f, 0f);
        fondoRect.pivot = new Vector2(0.5f, 0.5f);
        fondoRect.anchoredPosition = posicionCentro;
        fondoRect.sizeDelta = new Vector2(120f, 120f);

        Image imgFondo = fondoGo.GetComponent<Image>();
        imgFondo.sprite = spriteAnillo;
        imgFondo.color = new Color(0.12f, 0.12f, 0.12f, 0.65f);
        imgFondo.raycastTarget = false;

        // Barra de Vida Radial
        Transform barraExistente = contenedorArma.Find("BarraVidaRadial");
        GameObject barraGo;
        if (barraExistente == null)
        {
            barraGo = new GameObject("BarraVidaRadial", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            barraGo.transform.SetParent(contenedorArma, false);
            Undo.RegisterCreatedObjectUndo(barraGo, "Crear Barra Vida Radial");
        }
        else
        {
            barraGo = barraExistente.gameObject;
        }

        RectTransform barraRect = barraGo.GetComponent<RectTransform>();
        barraRect.anchorMin = new Vector2(1f, 0f);
        barraRect.anchorMax = new Vector2(1f, 0f);
        barraRect.pivot = new Vector2(0.5f, 0.5f);
        barraRect.anchoredPosition = posicionCentro;
        barraRect.sizeDelta = new Vector2(120f, 120f);

        Image barraImage = barraGo.GetComponent<Image>();
        barraImage.sprite = spriteAnillo;
        barraImage.type = Image.Type.Filled;
        barraImage.fillMethod = Image.FillMethod.Radial360;
        barraImage.fillOrigin = (int)Image.Origin360.Top;
        barraImage.fillClockwise = true;
        barraImage.fillAmount = 1f;
        barraImage.color = new Color(0.2f, 0.85f, 0.3f, 1f);
        barraImage.raycastTarget = false;

        fondoGo.transform.SetAsFirstSibling();
        barraGo.transform.SetSiblingIndex(1);
        if (iconoArmaTransform != null)
        {
            iconoArmaTransform.SetAsLastSibling();
        }

        HUDMercenariesController hud = contenedorArma.GetComponent<HUDMercenariesController>();
        if (hud == null)
        {
            hud = Undo.AddComponent<HUDMercenariesController>(contenedorArma.gameObject);
        }

        Undo.RecordObject(hud, "Configurar HUDMercenariesController");
        hud.barraVidaRadial = barraImage;
        hud.contenedorArma = contenedorArma.gameObject;
        if (iconoArmaTransform != null)
        {
            hud.iconoArma = iconoArmaTransform.GetComponent<Image>();
        }
    }

    private static TextMeshProUGUI ObtenerOReparentarTexto(Transform canvasPadre, Transform nuevoPadre, string nombreBuscado, string textoDefault, TMP_FontAsset fuente, float fontSize, Color color, Vector2 pos, Vector2 tam, TextAlignmentOptions alineacion)
    {
        Transform existenteEnNuevo = nuevoPadre.Find(nombreBuscado);
        if (existenteEnNuevo != null)
        {
            TextMeshProUGUI tmpDirecto = existenteEnNuevo.GetComponent<TextMeshProUGUI>();
            ConfigurarTextoExistente(tmpDirecto, fuente, fontSize, color, pos, tam, alineacion);
            return tmpDirecto;
        }

        Transform existenteEnCanvas = canvasPadre.Find(nombreBuscado);
        if (existenteEnCanvas != null)
        {
            Undo.SetTransformParent(existenteEnCanvas, nuevoPadre, "Migrar " + nombreBuscado);
            TextMeshProUGUI tmpMigrado = existenteEnCanvas.GetComponent<TextMeshProUGUI>();
            ConfigurarTextoExistente(tmpMigrado, fuente, fontSize, color, pos, tam, alineacion);
            return tmpMigrado;
        }

        GameObject nuevoGo = new GameObject(nombreBuscado, typeof(RectTransform), typeof(TextMeshProUGUI));
        nuevoGo.transform.SetParent(nuevoPadre, false);
        Undo.RegisterCreatedObjectUndo(nuevoGo, "Crear " + nombreBuscado);

        TextMeshProUGUI nuevoTmp = nuevoGo.GetComponent<TextMeshProUGUI>();
        nuevoTmp.text = textoDefault;
        ConfigurarTextoExistente(nuevoTmp, fuente, fontSize, color, pos, tam, alineacion);
        return nuevoTmp;
    }

    private static void ConfigurarTextoExistente(TextMeshProUGUI tmp, TMP_FontAsset fuente, float fontSize, Color color, Vector2 pos, Vector2 tam, TextAlignmentOptions alineacion)
    {
        if (tmp == null) return;
        RectTransform rt = tmp.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;

        if (fuente != null) tmp.font = fuente;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = alineacion;
        tmp.color = color;
        tmp.raycastTarget = false;
    }
}
#endif
