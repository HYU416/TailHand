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

        string objName = target.gameObject.name.Replace("(Clone)", "");

        bool isTarget =
            objName == "BOM" ||
            objName == "NO BOM" ||
            objName == "Flint" ||
            objName == "Missile" ||
            objName == "Razer" ||
            objName == "Rubble" ||
            objName == "Obsidian";

        if (isTarget)
        {
            Debug.Log("îöíeÇæÇüÇüÇüÅIìñÇΩÇ¡ÇΩÇºÇßÇßÇßÅF" + target.name);
            playerCatch.touchingTarget = target;
        }
    }

    private Transform GetTargetTransform(Collider other)
    {
        if (other.attachedRigidbody != null)
        {
            return other.attachedRigidbody.transform;
        }

        return other.transform;
    }
}