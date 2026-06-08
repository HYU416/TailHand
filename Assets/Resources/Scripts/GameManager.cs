using UnityEngine;

public class GameManager : MonoBehaviour
{
    private void Start()
    {
        // ゲーム開始時にカーソルを非表示＆ロック
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        // ESCでゲーム終了
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            QuitGame();
        }
    }

    private void QuitGame()
    {
        // 終了前にカーソルを表示
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnApplicationQuit()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}