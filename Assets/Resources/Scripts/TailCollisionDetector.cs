using UnityEngine;

public class TailCollisionDetector : MonoBehaviour
{
    public PlayerCatchEnemy playerCatch;

    private void OnTriggerEnter(Collider other)
    {
        CheckTarget(other);
    }

    private void OnTriggerStay(Collider other)
    {
        CheckTarget(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (playerCatch == null) return;

        Transform target = GetTargetTransform(other);

        if (target == playerCatch.touchingTarget)
        {
            playerCatch.touchingTarget = null;
        }
    }

    private void CheckTarget(Collider other)
    {
        if (playerCatch == null) return;

        Transform target = GetTargetTransform(other);
        if (target == null) return;

        if (!IsCatchableTarget(target.gameObject))
        {
            return;
        }

        Debug.Log("íÕÇﬂÇÈëŒè€Ç…ìñÇΩÇ¡ÇƒÇ¢Ç‹Ç∑ÅF" + target.name);
        playerCatch.touchingTarget = target;
    }

    private bool IsCatchableTarget(GameObject targetObject)
    {
        if (targetObject == null)
        {
            return false;
        }

        string objName = targetObject.name.Replace("(Clone)", "").Trim();

        bool isCatchable =
            objName == "NO BOM" ||
            objName == "Flint" ||
            objName == "Rubble" ||
            objName == "Obsidian";

        return isCatchable;
    }

    private Transform GetTargetTransform(Collider other)
    {
        if (other == null)
        {
            return null;
        }

        if (other.attachedRigidbody != null)
        {
            return other.attachedRigidbody.transform;
        }

        return other.transform;
    }
}