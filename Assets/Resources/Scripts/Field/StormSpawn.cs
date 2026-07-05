using UnityEngine;

public class StormSpawn : MonoBehaviour
{
    [SerializeField]
    [Header("竜巻のPrefab")]
    private GameObject stormPrefab;
    [SerializeField]
    [Header("竜巻の出現範囲")]
    private Collider spawnRange;
    [SerializeField]
    [Header("竜巻の出現範囲外")]
    private Collider spawnOutOfRange;
    [SerializeField]
    [Header("竜巻の出現間隔")]
    private float spawnInterval = 5f; 
    private float spawnTimer = 0f;
    // 竜巻の初期回転を保持する変数
    private Vector3 initialRotation;
    private void Start()
    {
        // 竜巻の初期回転を取得
        initialRotation = stormPrefab.transform.rotation.eulerAngles;
    }
    // Update is called once per frame
    void Update()
    {
        if(spawnTimer > spawnInterval)
        {
            while (true)
            {
                // 竜巻の出現範囲内のランダムな位置を取得
                Vector3 spawnPosition = new Vector3(
                    Random.Range(spawnRange.bounds.min.x, spawnRange.bounds.max.x),
                   100.0f,
                    Random.Range(spawnRange.bounds.min.z, spawnRange.bounds.max.z)
                );

                Vector3 checkPos = new Vector3(spawnPosition.x, spawnOutOfRange.bounds.center.y, spawnPosition.z);
                // 竜巻の出現範囲外にいない場合、竜巻を出現させる
                if (!spawnOutOfRange.bounds.Contains(checkPos))
                {
                    RaycastHit hit;
                    if (!Physics.Raycast(spawnPosition, Vector3.down, out hit, Mathf.Infinity, LayerMask.GetMask("FieldBack")))
                    {
                        continue; // 床に当たらなかったら作らない
                    }
                    //竜巻を中心(0.0f, 0.0f, 0.0f)に向けた状態で出現させる
                    GameObject storm = Instantiate(stormPrefab, spawnPosition, Quaternion.identity);

                    //下にレイを飛ばして当たったオブジェクトの位置に竜巻を配置する
                    //FeildBackのレイヤーに当たった場合のみ配置するf
                    //レイの始点は竜巻の出現位置、方向は下方向、距離は無限大、レイヤーマスクはFieldBack
                   if (Physics.Raycast(spawnPosition, Vector3.down, out hit, Mathf.Infinity, LayerMask.GetMask("FieldBack")))
                    {
                        storm.transform.position = hit.point;
                    }
                    //デバック用でレイの始点と終点をログに出力する
                    Debug.DrawLine(spawnPosition, spawnPosition + Vector3.down * 100f   , Color.red, 5f);


                    storm.transform.LookAt(Vector3.zero);
                    //竜巻のX、Z回転を元に戻す
                    storm.transform.rotation = Quaternion.Euler(initialRotation.x, storm.transform.rotation.eulerAngles.y, initialRotation.z);
                    break;
                }
            }
            spawnTimer = 0f;
        }

        spawnTimer += Time.deltaTime;
    }
}
