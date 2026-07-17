using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] private Image[] tutorialImages;

    private int currentIndex = 0;
    private bool stickInput = false;

    void Start()
    {
        ShowImage(currentIndex);
    }

    private bool inputLock = false;

    void Update()
    {
        // ESCキー または コントローラーAボタン
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown("Submit"))
        {
            SceneManager.LoadScene("TitleScene");
            return;
        }

        bool left = Input.GetKeyDown(KeyCode.A);
        bool right = Input.GetKeyDown(KeyCode.D);

        float horizontal = Input.GetAxis("Horizontal");

        if (!inputLock)
        {
            if (left || horizontal < -0.5f)
            {
                PreviousImage();
                inputLock = true;
            }
            else if (right || horizontal > 0.5f)
            {
                NextImage();
                inputLock = true;
            }
        }

        // すべての入力が離れたらロック解除
        if (!Input.GetKey(KeyCode.A) &&
            !Input.GetKey(KeyCode.D) &&
            Mathf.Abs(horizontal) < 0.01f)
        {
            inputLock = false;
        }
    }

    void NextImage()
    {
        Debug.Log($"Next : {gameObject.name}");

        currentIndex++;

        if (currentIndex >= tutorialImages.Length)
            currentIndex = 0;

        ShowImage(currentIndex);
    }

    void PreviousImage()
    {
        currentIndex--;

        if (currentIndex < 0)
            currentIndex = tutorialImages.Length - 1;

        ShowImage(currentIndex);
    }

    void ShowImage(int index)
    {
        for (int i = 0; i < tutorialImages.Length; i++)
        {
            tutorialImages[i].gameObject.SetActive(i == index);
        }
    }
}