/// <summary>
/// GameScene の最終攻撃演出から QTEScene へ移行したことを伝えます。
/// </summary>
public static class QTETransitionContext
{
    static bool enteredFromFinalAttack;

    public static void MarkEnteredFromFinalAttack()
    {
        enteredFromFinalAttack = true;
    }

    public static bool ConsumeEnteredFromFinalAttack()
    {
        if (!enteredFromFinalAttack)
        {
            return false;
        }

        enteredFromFinalAttack = false;
        return true;
    }
}
