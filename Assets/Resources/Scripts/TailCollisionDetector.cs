using UnityEngine;

public class TailCollisionDetector : MonoBehaviour
{
    public PlayerCatchEnemy playerCatch;
    
    private void OnTriggerEnter(UnityEngine.Collider other)
    {
        string objName = other.gameObject.name;

        bool isTarget =
            objName == "BOM(Clone)" ||
            objName == "NO BOM(Clone)"||
            objName == "Flint(Clone)"||
            objName == "Missile(Clone)"||
            objName == "Razer(Clone)"||
            objName == "Rubble(Clone)"||
            objName == "Obsidian(Clone)";

        if (isTarget)
        {
            Debug.Log("あたってまーーーーす");
            playerCatch.touchingTarget = other.transform;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.transform == playerCatch.touchingTarget)
        {
            playerCatch.touchingTarget = null;
        }
    }
}