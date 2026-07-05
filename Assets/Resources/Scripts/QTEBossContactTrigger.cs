using UnityEngine;

/// <summary>
/// プレイヤーがボスに接触したタイミングで QTE を開始します。
/// </summary>
[RequireComponent(typeof(Collider))]
public class QTEBossContactTrigger : MonoBehaviour
{
    [SerializeField] private QTESceneController sceneController;

    void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (sceneController != null)
        {
            sceneController.NotifyPlayerContact();
        }
    }
}
