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
    [Tooltip("ONにすると時間経過では爆発しません")]
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

    //エフェクトのスピードカーブ
    public AnimationCurve effectSpeedCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
    private EffectPlayer damageZoneEffect;

    public bool HasExploded
    {
        get { return hasExploded; }
    }

    private void Start()
    {
        StartExplosionTimer();
        damageZoneEffect = EffectManager.Instance.Play(EffectType.DamageZone, transform.position).GetComponent<EffectPlayer>();
    }

    private void OnDestroy()
    {
        
    }

    private void Update()
    {
        if (!timerStarted) return;
        if (hasExploded) return;
        if (isDudBomb) return;
       elapsedTime += Time.deltaTime;
        
        float remainingTime = explosionTime - elapsedTime;

        //ダメージゾーンポジションを更新
        damageZoneEffect.SetEffectPos(transform.position);
        //エフェクトのスピードを時間経過に応じて変化させる
        float speedMultiplier = effectSpeedCurve.Evaluate(elapsedTime / explosionTime);
        damageZoneEffect.SetPlaySpeed(speedMultiplier);


        if (useBlinkBeforeExplosion &&
            blinkBeforeExplosionTime > 0f &&
            remainingTime <= blinkBeforeExplosionTime)
        {
            UpdateBlink();
        }

        if (elapsedTime >= explosionTime)
        {
            
            ExplodeByTimer();
        }
    }

    private void StartExplosionTimer()
    {
        if (timerStarted) return;

        timerStarted = true;
        elapsedTime = 0f;
        blinkTimer = 0f;
        isBlinkColor = false;

        SetupBlinkRenderer();

        if (isDudBomb)
        {
            if (dudDestroyTime > 0f)
            {
                Destroy(gameObject, dudDestroyTime);
            }

            return;
        }
    }

    private void SetupBlinkRenderer()
    {
        renderers = GetComponentsInChildren<Renderer>();

        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }
    }

    private void UpdateBlink()
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

    private void SetBlinkColor()
    {
        if (renderers == null) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer targetRenderer = renderers[i];

            if (targetRenderer == null) continue;

            targetRenderer.GetPropertyBlock(propertyBlock);

            propertyBlock.SetColor("_Color", blinkColor);
            propertyBlock.SetColor("_BaseColor", blinkColor);

            targetRenderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void ResetBlinkColor()
    {
        if (renderers == null) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer targetRenderer = renderers[i];

            if (targetRenderer == null) continue;

            targetRenderer.SetPropertyBlock(null);
        }
    }

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

    public void ExplodeByTimer()
    {
        if (hasExploded) return;
        if (isDudBomb) return;

        Explode("BOM時間経過爆発: " + gameObject.name);
    }

    public void ExplodeByBossHit()
    {
        if (hasExploded) return;

        Explode("BOMがボス壁またはコアに当たって爆発しました: " + gameObject.name);
    }

    public void ForceExplode()
    {
        if (hasExploded) return;

        Explode("BOM強制爆発: " + gameObject.name);
    }

    private void Explode(string logMessage)
    {
        if (hasExploded) return;

        hasExploded = true;

        ResetBlinkColor();

        Debug.Log(logMessage);

        SpawnExplosionEffect();
        CheckExplosionHit();

        Destroy(gameObject);
    }

    private void SpawnExplosionEffect()
    {
        if (explosionEffectPrefab != null)
        {
            if (transform.localScale.x == 2.0f)
            {
                EffectManager.Instance.Play(EffectType.Explosion2, transform.position);
            }
            else
            {
                EffectManager.Instance.Play(EffectType.Explosion, transform.position);
            }

            return;
        }

        Debug.LogWarning("Explosion Effect Prefab が設定されていません");
    }

    private void CheckExplosionHit()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                Debug.Log("プレイヤーが爆発範囲に入りました。ダメージ：" + damage);

                // PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
                //
                // if (playerHealth != null)
                // {
                //     playerHealth.TakeDamage(damage);
                // }
            }
        }
    }

    private void OnDisable()
    {
        ResetBlinkColor();
        if (EffectManager.IsInitialized)
        {
            if (damageZoneEffect != null)
            {
                EffectManager.Instance.Release(EffectType.DamageZone, damageZoneEffect);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}