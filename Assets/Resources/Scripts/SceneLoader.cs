using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static class SceneName
    {
        public const string Title = "Title";
        public const string Stage1 = "Stage1";
        public const string Stage2 = "Stage2";
        public const string Stage3 = "Stage3";
    }

   public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
