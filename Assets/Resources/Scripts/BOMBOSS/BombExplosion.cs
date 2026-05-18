/*
 * ==========================================================
 * 制作責任者：小林大悟
 *
 * 爆弾の爆発処理を管理するスクリプトです。
 * BossBombShooter.cs 側の攻撃ノードから、
 * 爆発時間、爆発範囲、ダメージ、爆発エフェクト倍率などを
 * 攻撃ごとに変更できるようにしています。
 *
 * 不発弾に設定された場合は爆発しません。
 *
 * 【今回の追加】
 * ・爆発する少し前から点滅できるようにしました。
 * ・爆発何秒前から点滅するか、点滅間隔、点滅色を変更できます。
 * ==========================================================
 */

using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class BombExplosion : MonoBehaviour
{
    [Header("【爆発までの時間】")]
    [Tooltip("爆弾が生成されてから爆発するまでの時間です")]
    public float explosionTime = 7.0f;

    [Header("【爆発の範囲】")]
    [Tooltip("爆発でプレイヤーにダメージを与える範囲です")]
    public float explosionRadius = 3.0f;

    [Header("【プレイヤーへのダメージ】")]
    [Tooltip("爆発に当たったプレイヤーへ与えるダメージです")]
    public int damage = 20;

    [Header("【爆発エフェクトPrefab】")]
    [Tooltip("爆発時に生成するエフェクトPrefabです")]
    public GameObject explosionEffectPrefab;

    [Header("【爆発エフェクトの大きさ倍率】")]
    [Tooltip("爆発エフェクトの見た目の大きさ倍率です")]
    public float explosionEffectScaleMultiplier = 1.0f;

    [Header("【爆発前の点滅設定】")]
    [Tooltip("ONにすると、爆発前に爆弾が点滅します")]
    public bool useBlinkBeforeExplosion = true;

    [Tooltip("爆発する何秒前から点滅を始めるかです")]
    public float blinkBeforeExplosionTime = 3.0f;

    [Tooltip("点滅する間隔です。小さいほど速く点滅します")]
    public float blinkInterval = 0.15f;

    [Tooltip("点滅するときの色です")]
    public Color blinkColor = Color.red;

    [Header("【不発弾にする】")]
    [Tooltip("ONにすると爆発しません。攻撃ノード側からも変更できます")]
    public bool isDudBomb = false;

    [Header("【不発弾を自動で消す時間】")]
    [Tooltip("不発弾がこの秒数を超えたら消えます。0以下なら消えません")]
    public float dudDestroyTime = 0.0f;

    private bool hasExploded = false;
    private bool timerStarted = false;

    private float elapsedTime = 0f;
    private float blinkTimer = 0f;
    private bool isBlinkColor = false;

    private Renderer[] renderers;
    private MaterialPropertyBlock propertyBlock;

    void Start()
    {
        StartExplosionTimer();
    }

    void Update()
    {
        if (!timerStarted) return;
        if (hasExploded) return;
        if (isDudBomb) return;

        elapsedTime += Time.deltaTime;

        float remainingTime = explosionTime - elapsedTime;

        if (useBlinkBeforeExplosion &&
            blinkBeforeExplosionTime > 0f &&
            remainingTime <= blinkBeforeExplosionTime)
        {
            UpdateBlink();
        }

        if (elapsedTime >= explosionTime)
        {
            Explode();
        }
    }

    /// <summary>
    /// 爆発タイマーを開始します。
    /// BossBombShooter側で設定変更された後にStartが呼ばれる想定です。
    /// </summary>
    void StartExplosionTimer()
    {
        if (timerStarted) return;

        timerStarted = true;
        elapsedTime = 0f;
        blinkTimer = 0f;
        isBlinkColor = false;

        SetupBlinkRenderer();

        // 不発弾なら爆発予約をしない
        if (isDudBomb)
        {
            if (dudDestroyTime > 0f)
            {
                Destroy(gameObject, dudDestroyTime);
            }

            return;
        }
    }

    void SetupBlinkRenderer()
    {
        renderers = GetComponentsInChildren<Renderer>();

        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }
    }

    void UpdateBlink()
    {
        if (renderers == null || renderers.Length == 0)
        {
            SetupBlinkRenderer();
        }

        blinkTimer += Time.deltaTime;

        if (blinkTimer < blinkInterval) return;

        blinkTimer = 0f;
        isBlinkColor = !isBlinkColor;

        if (isBlinkColor)
        {
            SetBlinkColor();
        }
        else
        {
            ResetBlinkColor();
        }
    }

    void SetBlinkColor()
    {
        if (renderers == null) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer targetRenderer = renderers[i];

            if (targetRenderer == null) continue;

            targetRenderer.GetPropertyBlock(propertyBlock);

            /*
             * Standard系なら _Color、
             * URP Lit系なら _BaseColor が使われることが多いので、
             * 両方に入れておきます。
             */
            propertyBlock.SetColor("_Color", blinkColor);
            propertyBlock.SetColor("_BaseColor", blinkColor);

            targetRenderer.SetPropertyBlock(propertyBlock);
        }
    }

    void ResetBlinkColor()
    {
        if (renderers == null) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer targetRenderer = renderers[i];

            if (targetRenderer == null) continue;

            /*
             * PropertyBlockを消すことで元のマテリアル色に戻します。
             */
            targetRenderer.SetPropertyBlock(null);
        }
    }

    /// <summary>
    /// BossBombShooterから爆発内容を変更するための関数です。
    /// 攻撃ノードごとに爆発時間、範囲、ダメージなどを変えられます。
    /// </summary>
    public void SetExplosionData(
        float newExplosionTime,
        float newExplosionRadius,
        int newDamage,
        float newEffectScaleMultiplier,
        bool newIsDudBomb,
        float newDudDestroyTime
    )
    {
        SetExplosionData(
            newExplosionTime,
            newExplosionRadius,
            newDamage,
            newEffectScaleMultiplier,
            newIsDudBomb,
            newDudDestroyTime,
            useBlinkBeforeExplosion,
            blinkBeforeExplosionTime,
            blinkInterval,
            blinkColor
        );
    }

    /// <summary>
    /// 点滅設定も含めて爆発内容を変更するための関数です。
    /// </summary>
    public void SetExplosionData(
        float newExplosionTime,
        float newExplosionRadius,
        int newDamage,
        float newEffectScaleMultiplier,
        bool newIsDudBomb,
        float newDudDestroyTime,
        bool newUseBlinkBeforeExplosion,
        float newBlinkBeforeExplosionTime,
        float newBlinkInterval,
        Color newBlinkColor
    )
    {
        explosionTime = newExplosionTime;
        explosionRadius = newExplosionRadius;
        damage = newDamage;
        explosionEffectScaleMultiplier = newEffectScaleMultiplier;
        isDudBomb = newIsDudBomb;
        dudDestroyTime = newDudDestroyTime;

        useBlinkBeforeExplosion = newUseBlinkBeforeExplosion;
        blinkBeforeExplosionTime = newBlinkBeforeExplosionTime;
        blinkInterval = newBlinkInterval;
        blinkColor = newBlinkColor;

        timerStarted = false;
        elapsedTime = 0f;
        blinkTimer = 0f;
        isBlinkColor = false;

        ResetBlinkColor();
        StartExplosionTimer();
    }

    /// <summary>
    /// 爆発処理です。
    /// プレイヤーが範囲内にいればダメージ処理を行います。
    /// </summary>
    void Explode()
    {
        if (hasExploded) return;
        if (isDudBomb) return;

        hasExploded = true;

        ResetBlinkColor();

        Debug.Log("BOM爆発: " + gameObject.name);

        if (explosionEffectPrefab != null)
        {

            //エフェクトの生成
            if (transform.localScale.x  == 2.0f)
            {
                EffectManager.Instance.Play(EffectType.Explosion2, transform.position);
            }
            else
            {
                EffectManager.Instance.Play(EffectType.Explosion, transform.position);
            }
        }
        else
        {
            Debug.LogWarning("Explosion Effect Prefab が設定されていません");
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                Debug.Log("プレイヤーが爆発範囲に入りました。ダメージ：" + damage);

                // プレイヤーHP処理を使う場合はここを有効化してください
                // PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
                // if (playerHealth != null)
                // {
                //     playerHealth.TakeDamage(damage);
                // }
            }
        }

        Destroy(gameObject);
    }

    void OnDisable()
    {
        ResetBlinkColor();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}