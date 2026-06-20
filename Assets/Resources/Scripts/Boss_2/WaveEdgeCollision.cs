using UnityEngine;
using System;

public class WaveEdgeCollision : MonoBehaviour
{
    // 波動情報
    [System.Serializable]
    public struct FWaveAngle
    {
        public float startAngle;
        public float endAngle;
    }


    [SerializeField] private float speed;               // 波の広がる速度
    [SerializeField] private float lifeTime;            // 消滅するまでの時間
    [SerializeField] private float waveThickness;       // 波の縁の太さ(外縁からどの範囲を線上にするか)
    [SerializeField] private LayerMask targetLayer;     // 当てたい対象のレイヤー

    [SerializeField] private FWaveAngle[] waveAngle;    // ウェーブの判定を取る角度範囲
    [SerializeField] private float waveAngleSpeed;      // ウェーブ角度の範囲を進める速度

    private float currentRadius = 0f;
    private int shockWaveArrayNum = 0;                  // 衝撃はの配列番号

    private LineRenderer[] lineRenderers;
    private int vertexCount = 60;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ResizeArray();
        // デバッグ中のみ実行
#if UNITY_EDITOR
        InitializeLineRenderers();
#endif
    }

    // Update is called once per frame
    void Update()
    {
        float deltaTime = Time.deltaTime;

        if (waveAngle.Length > 0)
        {
            // 時間経過で半径を広げる
            currentRadius += speed * deltaTime;
            UpdateWaveAngle(deltaTime);


            // 縁の判定チェック
            CheckEdgeCollision();

            // デバッグ中のみ実行
#if UNITY_EDITOR
            UpdateLineRenderer();
#endif

        }

        // オブジェクトを削除
        if (lifeTime <= 0.0f)
        {
            Destroy(gameObject);
            return;
        }
        lifeTime -= deltaTime;

    }

    private void UpdateWaveAngle(float deltaTime)
    {
        // 波動の角度を進める
        for (int i = 0; i < waveAngle.Length; ++i)
        {
            waveAngle[i].startAngle += waveAngleSpeed * deltaTime;
            waveAngle[i].endAngle += waveAngleSpeed * deltaTime;
        }
    }

    private void CheckEdgeCollision()
    {
        // 外半径の内側にいるコライダーを取得
        // 内側は距離ではじく
        Collider[] hits = Physics.OverlapSphere(transform.position, currentRadius, targetLayer);

        foreach (var hit in hits)
        {
            // 中心から対象への距離を計算
            float distance = Vector3.Distance(transform.position, hit.transform.position);

            // 外半径 - 線分範囲で内側の判定を取る
            float innerRadius = Mathf.Max(0f, currentRadius - waveThickness);

            // 外半径と内半径で線上の判定のみを取る
            if (distance >= innerRadius && distance <= currentRadius)
            {
                // 自身の正面方向を0度としてターゲットの角度を求める
                Vector3 localPos = transform.InverseTransformPoint(hit.transform.position);
                // 0〜360度になるように補完
                float angle = Mathf.Atan2(localPos.x, localPos.z) * Mathf.Rad2Deg;
                if (angle < 0) 
                    angle += 360f;

                // ヒットしたか判定
                if (IsHitWave(angle))
                {
                    // ヒット処理
                    Debug.Log("WaveHit");
                }
            }
        }
    }

    private bool IsHitWave(float targetAngle)
    {
        // 衝撃波または波動へのヒット判定
        foreach (var angle in waveAngle)
        {
            if (angle.startAngle <= targetAngle && targetAngle <= angle.endAngle)
                return true;
        }
        return false;
    }

    private void ResizeArray()
    {
        // 配列の最後尾に衝撃波を追加
        int resize = waveAngle.Length + 1;
        Array.Resize(ref waveAngle, resize);
        resize -= 1;
        waveAngle[resize].startAngle = 0.0f;
        waveAngle[resize].endAngle = 360.0f;
        shockWaveArrayNum = resize;
    }

    private void InitializeLineRenderers()
    {
        if (waveAngle.Length == 0)
            return;

        // 要素数分の配列を確保
        lineRenderers = new LineRenderer[waveAngle.Length];

        for (int i = 0; i < waveAngle.Length; i++)
        {
            GameObject lineObj = new GameObject($"WaveLine_Debug_{i}");
            lineObj.transform.SetParent(this.transform);
            // LineRendererを追加
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();

            // 各種プロパティの設定
            lr.startWidth = 0.1f;
            lr.endWidth = 0.1f;
            lr.loop = true; // 外周と内周を繋いで閉じる
            lr.useWorldSpace = true;

            lr.material = Canvas.GetDefaultCanvasMaterial();
            // 配列に保存
            lineRenderers[i] = lr;
        }
    }

    private void UpdateLineRenderer()
    {
        // 安全対策：配列が未初期化、または中身が空なら処理しない
        if (lineRenderers == null || waveAngle == null || lineRenderers.Length != waveAngle.Length) return;

        float innerRadius = Mathf.Max(0f, currentRadius - waveThickness);

        // カラー設定
        Color targetColor = Color.red;
        // 波動は少し上に表示
        float correctionPos = 0.2f;

        for (int i = 0; i < lineRenderers.Length; ++i)
        {
            if (lineRenderers[i] == null) continue;

            if (i == shockWaveArrayNum)
            {
                targetColor = Color.blue;
                correctionPos = 0.0f;
            }

            // グラデーション作成
            Gradient gradient = new Gradient();
            gradient.SetKeys(
        new GradientColorKey[] { new GradientColorKey(targetColor, 0.0f), new GradientColorKey(targetColor, 1.0f) },
        new GradientAlphaKey[] { new GradientAlphaKey(targetColor.a, 0.0f), new GradientAlphaKey(targetColor.a, 1.0f) });
            lineRenderers[i].colorGradient = gradient;


            FWaveAngle wave = waveAngle[i];

            float startAngle = wave.startAngle;
            float endAngle = wave.endAngle;
            float angleRange = endAngle - startAngle;

            // 1つのウェーブの全頂点数（外周分 + 内周分）を設定
            int totalVertices = vertexCount * 2;
            lineRenderers[i].positionCount = totalVertices;

            // 綺麗に閉じるためにループをONにする
            lineRenderers[i].loop = true;

            // 外周の弧を描く
            for (int j = 0; j < vertexCount; j++)
            {
                float progress = (float)j / (vertexCount - 1);
                float currentAngle = startAngle + (progress * angleRange);

                Vector3 dir = Quaternion.AngleAxis(currentAngle, transform.up) * transform.forward;
                Vector3 pointPosition = transform.position + dir * currentRadius;
                pointPosition.y += correctionPos;

                lineRenderers[i].SetPosition(j, pointPosition);
            }

            // 内周の弧を逆順に描いて戻る
            for (int j = 0; j < vertexCount; j++)
            {
                float progress = (float)j / (vertexCount - 1);
                
                float currentAngle = endAngle - (progress * angleRange);

                Vector3 dir = Quaternion.AngleAxis(currentAngle, transform.up) * transform.forward;
                Vector3 pointPosition = transform.position + dir * innerRadius;
                pointPosition.y += correctionPos;

                // 後半のインデックス（vertexCount + j）に座標をセット
                lineRenderers[i].SetPosition(vertexCount + j, pointPosition);
            }
        }
    }


    public void SetLayerMask(LayerMask layer)
    {
        targetLayer = layer;
    }
}
