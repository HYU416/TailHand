using UnityEngine;


public enum AnimeState
{
    Idle = 0,
    Run = 1,
}

public class PlayerMotion : MonoBehaviour
{

    [SerializeField] private Animator animator;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SwitchMotion(AnimeState state)
    {
        animator.SetInteger("State", (int)state);
    }
}
