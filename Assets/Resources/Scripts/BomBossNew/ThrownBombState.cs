using UnityEngine;

public class ThrownBombState : MonoBehaviour
{
    [Header("プレイヤーが投げた爆弾か")]
    [SerializeField] private bool thrownByPlayer;

    [Header("投げられた判定後、何秒間だけ有効にするか")]
    [SerializeField] private float thrownActiveTime = 5.0f;

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
        gameObject.layer = LayerMask.NameToLayer("Tail");

        Debug.Log("ThrownBombState: 爆弾をプレイヤー投げ状態にしました");
    }

    public void ClearThrownByPlayer()
    {
        thrownByPlayer = false;
        thrownTimer = 0.0f;

        Debug.Log("ThrownBombState: 爆弾の投げ状態を解除しました");
    }
}