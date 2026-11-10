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
        if (existingPlayer == null)
        {
            BuildAndSaveScene();
        }
        else
        {
            existingPlayer.SetupHeroVisual();
            LevelBuilder2D.EnsureRobotsInScene();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }
    }

    [MenuItem("GameDev/Construir ou Resetar Fase 2D")]
    public static void BuildAndSaveScene()
    {
        LevelBuilder2D.BuildScene();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[GameDev] Fase 2D com o Boneco Pixel Art e obstáculos atualizada com sucesso!");
    }
}
#endif
