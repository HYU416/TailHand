using UnityEngine;

public class Core : MonoBehaviour
{
    [System.Serializable]
    public struct FStatus
    {
        public int[] hps;
        [HideInInspector] public int hp;
        [HideInInspector] public bool bAlive;
    }

    [SerializeField] private FStatus status;
    [SerializeField] private CoreBarrierManager barrierManager;
    [SerializeField] private Big_G big_g;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (status.hps.Length != 3)
            Destroy(this.gameObject);
        if (barrierManager == null)
            Destroy(this.gameObject);
        if (big_g == null)
            Destroy(this.gameObject);

        status.hp = status.hps[0];
        status.bAlive = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
            TakeDamage();
    }

    private void HPSet()
    {
        if (big_g == null)
            return;

        switch (big_g.GetCurrentPhase())
        {
            case EPhase.Phase2:
                status.hp = status.hps[1]; break;
            case EPhase.Phase3:
                status.hp = status.hps[2]; break;
        }
    }

    private void TakeDamage()
    {
        status.hp--;
        Debug.Log("Core" + status.hp);

        if (status.hp <= 0)
        {
            if (big_g.GetCurrentPhase() == EPhase.Phase3)
            {
                Debug.Log("Phase3_コア破壊");
                status.bAlive = false;
                return;
            }
            // コアが破壊されたのでフェーズを上げる
            big_g.PhaseUp();
            big_g.SwitchState(EBig_GState.Normal);
            HPSet();
            // バリアの再生
            barrierManager.Regenerate();
        }
    }

    void OnTriggerEnter(Collider col)
    {
        if (big_g != null && barrierManager != null)
        {
            // バリアが破壊された状態か
            if (big_g.GetCurrentState() == EBig_GState.Core)
            {
                if (col.gameObject.CompareTag("Projectile"))
                {
                    TakeDamage();

                    EighthNoteController eighth = col.GetComponent<EighthNoteController>();
                    if (eighth != null)
                    {
                        barrierManager.TakeDamage();
                        eighth.Hit();
                        return;
                    }
                    DupletNoteController duplet = col.GetComponent<DupletNoteController>();
                    if (duplet != null)
                    {
                        barrierManager.TakeDamage();
                        duplet.Hit();
                        return;
                    }
                }
            }
        }
    }

    public bool IsAlive()
    {
        return status.bAlive;
    }
}