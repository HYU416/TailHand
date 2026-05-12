using UnityEngine;

public class ItemSpawn : MonoBehaviour
{
    [Header("瓦礫の出現率（％）")]
    [Range(0, 100)]
    [SerializeField] private int rubbleSpawnRate = 25;

    [Header("黒曜石の出現率（％）")]
    [Range(0, 100)]
    [SerializeField] private int obsidianSpawnRate = 25;

    [Header("火打石の出現率（％）")]
    [Range(0, 100)]
    [SerializeField] private int flintSpawnRate = 25;

    [Header("何も出ない確率（％）")]
    [Range(0, 100)]
    [SerializeField] private int noneSpawnRate = 25;

    [Header("瓦礫のプレハブ")]
    [SerializeField] private GameObject rubblePrefab;
    [Header("黒曜石のプレハブ")]
    [SerializeField] private GameObject obsidianPrefab;
    [Header("火打石のプレハブ")]
    [SerializeField] private GameObject flintPrefab;

    void Start()
    {
     
    }
    // 出現率を100%に補正するメソッド
    public void NormalizeRates()
    {
        // 出現率の合計を計算
        int totalItemRate = rubbleSpawnRate + obsidianSpawnRate + flintSpawnRate;

        // 100%から何も出ない確率を引いた残りを計算
        int remain = 100 - noneSpawnRate;

        // 全部0なら瓦礫へ
        if (totalItemRate == 0)
        {
            rubbleSpawnRate = remain;
        }
        else
        {
            // 出現率の合計が100%を超える場合、各アイテムの出現率を調整
            float factor = remain / (float)totalItemRate;

            rubbleSpawnRate = Mathf.RoundToInt(rubbleSpawnRate * factor);

            obsidianSpawnRate = Mathf.RoundToInt(obsidianSpawnRate * factor);

            flintSpawnRate = Mathf.RoundToInt(flintSpawnRate * factor);
        }
        // 調整後の出現率の合計を再計算して、100%に補正
        FixTotalTo100();
    }

    // 出現率の合計を100%に補正するメソッド
    private void FixTotalTo100()
    {
        int total = rubbleSpawnRate + obsidianSpawnRate + flintSpawnRate + noneSpawnRate;

        // 100%から現在の合計を引いて、差分を計算
        int diff = 100 - total;
        // 差分を火打石の出現率に加算して、合計を100%に補正
        flintSpawnRate += diff;
    }

    private void OnDestroy()
    {
        //シーン終了時にアイテムをスポーンさせないため、シーン終了時はこのメソッドを呼び出さないようにする
        if (gameObject != null) {
            // シーン終了時はアイテムをスポーンさせない
            if (gameObject.scene.isLoaded)
            {
                // オブジェクトが破壊されるときにアイテムをスポーン
                SpawnItem();
            }
        }
    }

    private void SpawnItem()
    {
        // 0から99までのランダムな整数を生成
        int rand = Random.Range(0, 100);
        // ランダムな整数に基づいてアイテムをスポーン
        if (rand < rubbleSpawnRate)
        {
            Instantiate(rubblePrefab, transform.position, Quaternion.identity);
        }
        else if (rand < rubbleSpawnRate + obsidianSpawnRate)
        {
            Instantiate(obsidianPrefab, transform.position, Quaternion.identity);
        }
        else if (rand < rubbleSpawnRate + obsidianSpawnRate + flintSpawnRate)
        {
            Instantiate(flintPrefab, transform.position, Quaternion.identity);
        }
        // 何も出ない場合は何もしない

       
    }
}