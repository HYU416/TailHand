using UnityEngine;
using UnityEngine.UI;

public class CatchTargetImage : MonoBehaviour
{
    public PlayerCatchEnemy playerCatch;
    public Image image;

    private void Update()
    {
        if (playerCatch == null || playerCatch.caughtTarget == null)
        {
            image.enabled = false;
            return;
        }

        CatchableIcon icon = playerCatch.caughtTarget.GetComponent<CatchableIcon>();

        if (icon == null)
            icon = playerCatch.caughtTarget.GetComponentInParent<CatchableIcon>();

        if (icon == null)
            icon = playerCatch.caughtTarget.GetComponentInChildren<CatchableIcon>();

        if (icon != null && icon.icon != null)
        {
            image.sprite = icon.icon;
            image.enabled = true;
        }
        else
        {
            image.enabled = false;
        }
    }
}