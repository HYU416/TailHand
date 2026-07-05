using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static class SceneName
    {
        public const string Title = "Title";
        public const string GameScene = "GameScene";
        public const string QTEScene = "QTEScene";
        public const string Stage1 = "Stage1";
        public const string Stage2 = "Stage2";
        public const string Stage3 = "Stage3";
    }

    public static void Load(string sceneName)
    {
        Time.timeScale = 1f;

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("SceneLoader: シーン名が空です");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                $"SceneLoader: シーン '{sceneName}' を読み込めません。"
                + " File → Build Settings に追加されているか確認してください。"
            );
            return;
        }

        Debug.Log($"SceneLoader: '{sceneName}' を読み込みます");
        SceneManager.LoadScene(sceneName);
    }

    public void LoadScene(string sceneName)
    {
        Load(sceneName);
    }
}
