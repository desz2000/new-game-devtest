#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class LevelSceneSetup
{
    static LevelSceneSetup()
    {
        EditorApplication.delayCall += AutoSetupSceneIfEmpty;
    }

    private static void AutoSetupSceneIfEmpty()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        BoxPlayer2D existingPlayer = Object.FindObjectOfType<BoxPlayer2D>();
        ParallaxBackdrop2D existingBackdrop = Object.FindObjectOfType<ParallaxBackdrop2D>();
        if (existingPlayer == null || existingBackdrop == null)
        {
            BuildAndSaveScene();
        }
        else
        {
            existingPlayer.SetupHeroVisual();
            if (existingPlayer.GetComponent<GameUI2D>() == null)
            {
                GameUI2D ui = existingPlayer.gameObject.AddComponent<GameUI2D>();
                ui.player = existingPlayer;
            }
            LevelBuilder2D.EnsureRobotsInScene();
            LevelBuilder2D.EnsureCheckpointsInScene();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }
    }

    [MenuItem("GameDev/Construir ou Resetar Fase 2D")]
    public static void BuildAndSaveScene()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(activeScene.path))
        {
            const string defaultScenePath = "Assets/Scenes/SampleScene.unity";
            if (System.IO.File.Exists(defaultScenePath))
            {
                activeScene = EditorSceneManager.OpenScene(defaultScenePath, OpenSceneMode.Single);
            }
        }

        LevelBuilder2D.BuildScene();
        activeScene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(activeScene);
        if (!string.IsNullOrEmpty(activeScene.path))
        {
            EditorSceneManager.SaveScene(activeScene);
        }
        else
        {
            EditorSceneManager.SaveOpenScenes();
        }
        Debug.Log("[GameDev] Fase 2D Duck Souls x Celeste 32x32 atualizada com sucesso!");
    }
}
#endif
