#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Aplica y GUARDA los valores del sistema de munición realista:
/// cargador / reserva máxima / reserva inicial por arma en el Bryan de los 3 stages,
/// y probabilidad de drop de munición más baja en los prefabs de zombies.
/// Solo toca PlayerMovement.armas y EnemyHealth.probabilidadMunicion (no altera escenarios).
/// </summary>
public static class MigrarMunicionRealista
{
    static readonly string[] Escenas =
    {
        "Assets/Scenes/SampleScene.unity",
        "Assets/Scenes/Stage2_Laboratorio.unity",
        "Assets/Scenes/Stage3_Laboratorio.unity",
        "Assets/Scenes/Stage3_RaccoonCity.unity"
    };

    [MenuItem("Tools/Biohazard Code Zero/Munición realista (cargadores por arma y drops)", false, 60)]
    public static void Aplicar()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string escenaOriginal = SceneManager.GetActiveScene().path;
        int escenasCambiadas = 0;

        foreach (string ruta in Escenas)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ruta) == null) continue;
            EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
            PlayerMovement jugador = Object.FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Include);
            if (jugador == null || jugador.armas == null) continue;

            Undo.RecordObject(jugador, "Munición realista");
            bool cambio = false;
            foreach (ArmaConfig a in jugador.armas)
            {
                if (a == null) continue;
                a.versionMunicion = 0;      // fuerza los valores realistas por tipo de arma
                cambio |= a.MigrarMunicion();
            }
            if (cambio)
            {
                EditorUtility.SetDirty(jugador);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                escenasCambiadas++;
            }
        }

        // Drops de munición más escasos (survival horror)
        int prefabs = 0;
        prefabs += AjustarDrop("Assets/Prefabs/Lycan.prefab", 70f);
        prefabs += AjustarDrop("Assets/Prefabs/Lycan_Corredor.prefab", 55f);
        prefabs += AjustarDrop("Assets/Prefabs/Lycan_Escupidor.prefab", 60f);
        prefabs += AjustarDrop("Assets/Prefabs/Lycan_Toxico.prefab", 60f);
        prefabs += AjustarDrop("Assets/Prefabs/Lycan_Bruto.prefab", 100f);
        AssetDatabase.SaveAssets();

        if (!string.IsNullOrEmpty(escenaOriginal) && AssetDatabase.LoadAssetAtPath<SceneAsset>(escenaOriginal) != null)
            EditorSceneManager.OpenScene(escenaOriginal, OpenSceneMode.Single);

        string msg = "Munición realista aplicada.\n\n" +
            "• Pistola 15 / 60, Escopeta 8 / 32, Subfusil 30 / 120 (en " + escenasCambiadas + " escenas).\n" +
            "• Probabilidad de drop: Lycan 70%, Corredor 55%, Escupidor/Tóxico 60%, Tocho 100% (" + prefabs + " prefabs).\n\n" +
            "Puedes ajustar todo en el Inspector: Player > PlayerMovement > Armas.";
        EditorUtility.DisplayDialog("Biohazard Code Zero", msg, "OK");
        Debug.Log("[Biohazard] " + msg);
    }

    static int AjustarDrop(string ruta, float probabilidad)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(ruta) == null) return 0;
        GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
        EnemyHealth h = raiz.GetComponent<EnemyHealth>();
        if (h != null) h.probabilidadMunicion = probabilidad;
        PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
        PrefabUtility.UnloadPrefabContents(raiz);
        return h != null ? 1 : 0;
    }
}
#endif
