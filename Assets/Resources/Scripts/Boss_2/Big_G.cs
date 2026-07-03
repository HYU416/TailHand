using UnityEngine;


public enum EPhase
{
    Phase1,
    Phase2,
    Phase3
}

public enum EBig_GState
{
    Normal,
    Core
}

public class Big_G : MonoBehaviour
{


    private EPhase phase = EPhase.Phase1;
    private EBig_GState state = EBig_GState.Normal;
    [SerializeField] private Core core;
    [SerializeField] private Big_GArmController[] armController;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (core == null)
            Destroy(this.gameObject);
    }

    // Update is called once per frame
    void Update()
    {
            if (core != null)
                if (!core.IsAlive())
                    Destroy(this.gameObject);
    }

    public EPhase GetCurrentPhase()
    {
        return phase;
    }

    public EBig_GState GetCurrentState()
    {
        return state;
    }

    public void PhaseUp()
    {
        if (phase == EPhase.Phase3)
            return;
        phase = (EPhase)((int)phase + 1);
    }

    public void SwitchState(EBig_GState s)
    {
        state = s;
    }

    public void DestroyedBarrier()
    {
        for (int i = 0; i < armController.Length; ++i)
        {
            armController[i].DetachArm();
        }
    }

    public void RegenerateBarrier()
    {
        for (int i = 0; i < armController.Length; ++i)
        {
            armController[i].ReattachArm();
        }
    }
}