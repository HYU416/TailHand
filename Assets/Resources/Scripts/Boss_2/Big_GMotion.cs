using UnityEngine;

public class Big_GMotion : MonoBehaviour
{
    [SerializeField] private Animator animator_body;
    [SerializeField] private Animator animator_L_Hand;
    [SerializeField] private Animator animator_R_Hand;

    public void SwitchMotion(EBig_GAnimeState state)
    {
        SwitchBodyMotion(state);
        SwitchL_HandMotion(state);
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
