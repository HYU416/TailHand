using UnityEngine;

public class Big_GMotion : MonoBehaviour
{
    [SerializeField] private Animator animator_body;
    [SerializeField] private Animator animator_L_Hand;
    [SerializeField] private Animator animator_R_Hand;

    public void SwitchMotion(EBig_GAnimeState state, bool bBody = true, bool bL_Hand = true, bool bR_Hand = true)
    {
        if (bBody)
            SwitchBodyMotion(state);
        if (bL_Hand)
            SwitchL_HandMotion(state);
        if (bR_Hand)
            SwitchR_HandMotion(state);
    }

    public void SwitchBodyMotion(EBig_GAnimeState state)
    {
        animator_body.SetInteger("State", (int)state);
    }
    public void SwitchL_HandMotion(EBig_GAnimeState state)
    {
        animator_L_Hand.SetInteger("State", (int)state);
    }
    public void SwitchR_HandMotion(EBig_GAnimeState state)
    {
        animator_R_Hand.SetInteger("State", (int)state);
    }

}
