using UnityEngine;

public class ChangeTag : MonoBehaviour
{
    [SerializeField] private string throwTargetName;
    [SerializeField] private string hasPlayerCatchEnemyName;
    private PlayerCatchEnemy playerCatchEnemy;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameObject tailEnd = null;

        if (!string.IsNullOrEmpty(hasPlayerCatchEnemyName))
        {
            tailEnd = GameObject.Find(hasPlayerCatchEnemyName);
        }
        playerCatchEnemy = tailEnd.GetComponent<PlayerCatchEnemy>();

    }

    // Update is called once per frame
    void Update()
    {
        var obj = playerCatchEnemy.CatchingObjectPtr();
        if (obj == this.gameObject)
            this.gameObject.tag = "Projectile";
    }
}
