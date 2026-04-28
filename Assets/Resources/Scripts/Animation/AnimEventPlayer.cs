using UnityEngine;

public class AnimEventPlayer : MonoBehaviour
{
    public AnimEventDataAsset data;

    float currentTime;
    int prevFrame = -1;

    void Update()
    {
        if (data == null || data.clip == null) return;

        currentTime += Time.deltaTime;

        if (currentTime > data.clip.length)
        {
            currentTime = 0;
        }

        int currentFrame = Mathf.RoundToInt(currentTime * data.frameRate);

        // 同じフレームで2回発火しないように
        if (currentFrame != prevFrame)
        {
            CheckEvents(currentFrame);
            prevFrame = currentFrame;
        }
    }

    void CheckEvents(int frame)
    {
        foreach (var e in data.events)
        {
            if (e.frame == frame)
            {
                ExecuteEvent(e);
            }
        }
    }

    void ExecuteEvent(AnimEventData e)
    {
        switch (e.Type)
        {
            case AnimEventType.Attack:
                Debug.Log("攻撃！");
                break;

            case AnimEventType.SE:
                Debug.Log("SE再生: " + e.list);
                break;

            case AnimEventType.Effect:
                Debug.Log("エフェクト！");
                break;
        }
    }
}
