using UnityEngine;

public class MakeAppearTrajectory : MonoBehaviour
{
    [SerializeField] GameObject[] trajectoryPrefab;
    [SerializeField] int minCount;
    [SerializeField] int maxCount;

    public void MakeTrajectory(Vector3 pos)
    {
        var appearCount = Random.Range(minCount, maxCount + 1);

        for (int i = 0; i < appearCount; i++)
        {
            var appearKind = Random.Range(0, trajectoryPrefab.Length);
            Vector3 randomDirection = new Vector3(
            Random.Range(-1.0f, 1.0f),
            Random.Range(0.7f, 1.0f), // 0.2‚ð‰Á‚¦‚é‚±‚Æ‚ÅA^‰¡‚â­‚µ‰º‚És‚«‚É‚­‚­‚·‚é
            Random.Range(-1.0f, 1.0f)
            ).normalized;

            GameObject obj = Instantiate(trajectoryPrefab[appearKind], pos, new Quaternion(0.0f, 0.0f, 0.0f, 1.0f));
            // ‚«”ò‚Î‚·
            obj.GetComponent<Rigidbody>().AddForce(randomDirection * 1.7f, ForceMode.Impulse);

        }
    }
}
