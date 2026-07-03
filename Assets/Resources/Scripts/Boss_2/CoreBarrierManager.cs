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

        if (status.bDestroy)
            CoreBarrierSetActive(false);
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

    public bool TakeDamage()
    {
        if (big_g == null)
            return false;

        if (big_g.GetCurrentState() == EBig_GState.Normal)
        {
            status.hp--;
            Debug.Log("Bar" + status.hp);
            if (status.hp <= 0)
            {
                if (big_g.GetCurrentPhase() == EPhase.Phase3)
                    Debug.Log("Phase3_ƒoƒŠƒA”j‰ó");

                status.bDestroy = true;
                big_g.SwitchState(EBig_GState.Core);
                big_g.DestroyedBarrier();

                return true;
            }
        }
        return false;
    }

    public void Regenerate()
    {
        status.bDestroy = false;
        CoreBarrierSetActive(true);
        RegenerateCoreBarrier();
    }
}
