using UnityEngine;

public class DudBombState : MonoBehaviour
{
    [Header("プレイヤーが投げた不発弾か")]
    [SerializeField] private bool thrownByPlayer;

    [Header("投げられた判定後、何秒間だけ有効にするか")]
    [SerializeField] private float thrownActiveTime = 8.0f;

    private float thrownTimer;

    public bool IsThrownByPlayer
    {
        get { return thrownByPlayer; }
    }

    private void Update()
    {
        if (!thrownByPlayer)
        {
            return;
        }

        thrownTimer -= Time.deltaTime;

        if (thrownTimer <= 0.0f)
        {
            ClearThrownByPlayer();
        }
    }

    public void MarkThrownByPlayer()
    {
        thrownByPlayer = true;
        thrownTimer = thrownActiveTime;

        Debug.Log("DudBombState: 不発弾を投げ状態にしました");
    }

    public void ClearThrownByPlayer()
    {
        thrownByPlayer = false;
        thrownTimer = 0.0f;

        Debug.Log("DudBombState: 不発弾の投げ状態を解除しました");
    }
}