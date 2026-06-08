using UnityEngine;
using UnityEngine.UI;

public class TItleScene : MonoBehaviour
{
    [SerializeField] private GameObject stageSelectPanel;
    [SerializeField] private GameObject startButtonPanel;
    [SerializeField] private GameObject NavigationPanel;
    [SerializeField] private SceneLoader sceneLoader;
    [Header("1行に並べるステージの数")]
    [SerializeField] private int columnCount = 3;
    [Header("コントローラー入力のクールダウン")]
    [SerializeField] private float moveCooldown = 0.2f;
    [Header("各ステージのPanel")]
    [SerializeField] private Image[] stagePanels;
    [Header("各ステージのシーンの名前")]
    [SerializeField] private string[] stageSceneNames;

    private int currentIndex = 0;
    private float nextMoveTime = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        stageSelectPanel.SetActive(false);
        NavigationPanel.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        // ステージ一覧が表示されたら
        if (stageSelectPanel.activeSelf)
        {
            startButtonPanel.SetActive(false);

            if (Time.time > nextMoveTime)
            {
                float h = Input.GetAxisRaw("Horizontal");
                float v = Input.GetAxisRaw("Vertical");

                if (Input.GetKeyDown(KeyCode.RightArrow) || h > 0.5f)
                {
                    currentIndex += 1;
                    nextMoveTime = Time.time + moveCooldown;
                }
                if (Input.GetKeyDown(KeyCode.LeftArrow) || h < -0.5f)
                { 
                    currentIndex -= 1;
                    nextMoveTime = Time.time + moveCooldown;
                }
                if (Input.GetKeyDown(KeyCode.UpArrow) || v > 0.5f)
                { 
                    currentIndex -= columnCount;
                    nextMoveTime = Time.time + moveCooldown;
                }
                if (Input.GetKeyDown(KeyCode.DownArrow) || v < -0.5f)
                {
                    currentIndex += columnCount;
                    nextMoveTime = Time.time + moveCooldown;
                }

                currentIndex = Mathf.Clamp(currentIndex, 0, stagePanels.Length - 1);
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetButtonDown("Submit"))
            {
                sceneLoader.LoadScene(stageSceneNames[currentIndex]);
            }

            for (int i = 0; i < stagePanels.Length; i++)
            {
                stagePanels[i].color = i == currentIndex ? Color.orange : Color.white;
            }
        }
        else
        {
            // StartButtonでStage一覧表示
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetButtonDown("Submit"))
            {
                OnClickStart();
            }
        }
    }

    public void OnClickStart()
    {
        stageSelectPanel.SetActive(true);
        NavigationPanel.SetActive(true);
    }
}
