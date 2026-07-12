using UnityEngine;


public enum EPhase
{
    Phase1,
    Phase2,
    Phase3,
    Phase4
}

public enum EBig_GState
{
    Normal,
    Core
}

public enum EBig_GAnimeState
{
    Idle = 0,
    Drumming_L = 1,
    Drumming_R = 2,
    ShockWave = 3,
    NoiseCannon = 4,
    Down = 5,
}

public class Big_G : MonoBehaviour
{


    private EPhase phase = EPhase.Phase1;
    private EBig_GState state = EBig_GState.Normal;
    [SerializeField] private Core core;
    [SerializeField] private Big_GArmController[] armController;
    [SerializeField] private Big_GMotion big_gMotion;
    [SerializeField] private EBig_GAnimeState animeState;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (core == null)
            Destroy(this.gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        if (phase >= EPhase.Phase4)
            return;

        if (core != null && !core.IsAlive())
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

        phase = (EPhase)((int)phase + 1);
    }

    public void EnterPhase4()
    {
        if (phase == EPhase.Phase4)
            return;

        phase = EPhase.Phase4;
        Debug.Log("Big_G: Phase4");
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
        big_gMotion.SwitchMotion(EBig_GAnimeState.Down);
    }

    public void RegenerateBarrier()
    {
        for (int i = 0; i < armController.Length; ++i)
        {
            armController[i].ReattachArm();
        }
        big_gMotion.SwitchMotion(EBig_GAnimeState.Idle);
    }

    public void SwitchAnimation(EBig_GAnimeState state, bool bBody = true, bool bL_Hand = true, bool bR_Hand = true)
    {
        animeState = state;
        if (big_gMotion)
            big_gMotion.SwitchMotion(animeState, bBody, bL_Hand, bR_Hand);
    }

    public int GetCurrentAnimation()
    {
        return (int)animeState;
    }
}
