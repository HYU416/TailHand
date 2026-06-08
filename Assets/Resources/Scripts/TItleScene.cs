using UnityEngine;
using UnityEngine.UI;

public class TItleScene : MonoBehaviour
{
    [SerializeField] private GameObject stageSelectPanel;
    [Header("各ステージのPanel")]
    [SerializeField] private Image[] stagePanels;
    [SerializeField] private GameObject startButtonPanel;
    [SerializeField] private SceneLoader sceneLoader;
    [Header("各ステージのシーンの名前")]
    [SerializeField] private string[] stageSceneNames;

    private int currentIndex = 0;
    public int maxStage = 3;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        stageSelectPanel.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        // ステージ一覧が表示されたら
        if (stageSelectPanel.activeSelf)
        {
            startButtonPanel.SetActive(false);

            if (Input.GetKeyDown(KeyCode.RightArrow)) { currentIndex += 1; }
            if (Input.GetKeyDown(KeyCode.LeftArrow)) { currentIndex -= 1; }
            if (currentIndex < 0) { currentIndex = 0; }
            if (currentIndex >= maxStage) { currentIndex = maxStage - 1; }

            stagePanels[currentIndex].color = Color.orange;

            if (Input.GetKeyDown(KeyCode.Return))
            {
                sceneLoader.LoadScene(stageSceneNames[currentIndex]);
            }

            for (int i = 0; i < maxStage; i++)
            {
                if (i != currentIndex)
                {
                    stagePanels[i].color = Color.white;
                }
            }
        }

        // StartButtonでStage一覧表示
        if(Input.GetKeyDown(KeyCode.Return))
        {
            OnClickStart();
        }
    }

    public void OnClickStart()
    {
        stageSelectPanel.SetActive(true);
    }
}
