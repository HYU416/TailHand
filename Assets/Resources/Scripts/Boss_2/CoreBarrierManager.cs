using UnityEngine;

public class CoreBarrierManager : MonoBehaviour
{
    [System.Serializable]
    public struct FStatus
    {
        public int[] hps;
        [HideInInspector] public int hp;
        [HideInInspector] public bool bDestroy;
    }

    [SerializeField] private FStatus status;
    [SerializeField] private GameObject[] coreBarrier;
    [SerializeField] private Big_G big_g;
    [SerializeField] private GameObject[] breakShellPrefabs;
    private bool bFragmented = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (big_g == null)
            Destroy(this.gameObject);
        if (status.hps.Length != 3)
            Destroy(this.gameObject);
        status.hp = status.hps[0];

        status.bDestroy = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
            TakeDamage();

        if (status.bDestroy && !bFragmented)
        {
            bFragmented = true;
            BreakBarriers();
        }
    }

    private void CoreBarrierSetActive(bool bActive)
    {
        foreach (GameObject obj in coreBarrier)
        {
            if (obj != null)
            {
                obj.SetActive(bActive);
            }
        }
    }

    private void RegenerateCoreBarrier()
    {
        if (big_g == null)
            return;

        switch (big_g.GetCurrentPhase())
        {
            case EPhase.Phase2:
                status.hp = status.hps[1]; break;
            case EPhase.Phase3:
                status.hp = status.hps[2]; break;
            default:
                return;
        }
        big_g.RegenerateBarrier();
    }

    public void TakeDamage()
    {
        if (big_g == null)
            return;

        if (big_g.GetCurrentState() == EBig_GState.Normal)
        {
            status.hp--;
            Debug.Log(status.hp);
            if (status.hp <= 0)
            {
                if (big_g.GetCurrentPhase() == EPhase.Phase3)
                    Debug.Log("Phase3_バリア破壊");

                status.bDestroy = true;
                big_g.SwitchState(EBig_GState.Core);
                big_g.DestroyedBarrier();
            }
        }
    }

    public void Regenerate()
    {
        status.bDestroy = false;
        bFragmented = false;
        CoreBarrierSetActive(true);
        RegenerateCoreBarrier();
    }

    private void BreakBarriers()
    {
        for (int i = 0; i < coreBarrier.Length; i++)
        {
            GameObject original = coreBarrier[i];

            if (original == null)
                continue;

            if (breakShellPrefabs != null &&
                i < breakShellPrefabs.Length &&
                breakShellPrefabs[i] != null)
            {
                GameObject shell = Instantiate(
                    breakShellPrefabs[i],
                    original.transform.parent
                );

                shell.transform.SetLocalPositionAndRotation(
                    original.transform.localPosition,
                    original.transform.localRotation
                );

                shell.transform.localScale =
                    original.transform.localScale;

                // 複製した殻ではゲーム進行用の当たり判定を動かさない
                CoreBarrier shellBarrier =
                    shell.GetComponent<CoreBarrier>();

                if (shellBarrier != null)
                {
                    shellBarrier.enabled = false;
                }

                BreakObject breakObject =
                    shell.GetComponent<BreakObject>();

                if (breakObject != null)
                {
                    breakObject.OnBreak();
                }
                else
                {
                    Debug.LogError(
                        $"{shell.name}: BreakObjectがありません"
                    );
                }

                Destroy(shell, 10f);
            }

            // 本来のCoreBarrierSetActive(false)と同じ処理
            original.SetActive(false);
        }
    }
}
