using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EffectPlayer : MonoBehaviour
{
    // エフェクトのタイプ
    [SerializeField]
    private EffectType effectType;
    // メインのパーティクル
    [SerializeField]
    private ParticleSystem mainParticle;
    // メインパーティクル取得用プロパティ
    public ParticleSystem MainParticle => mainParticle;
    // 子オブジェクトを含むすべてのパーティクルシステム
    private ParticleSystem[] allParticleSystems;
    // イベントリスト
    [SerializeField]
    private List<EffectEvent> events = new();
    // フレームレート
    [SerializeField]
    private int frameRate = 60;
    // フレームレート取得用プロパティ
    public int FrameRate => frameRate;
    // 再生速度
    [SerializeField]
    private float playSpeed = 1f;
    // 再生速度プロパティ
    public float PlaySpeed
    {
        get => playSpeed;
        // 0以下にならないよう制限
        set => playSpeed = Mathf.Max(0.01f, value);
    }

    // 再生中フラグ
    private bool isPlaying;
    // 現在フレーム
    private int currentFrame;
    // 経過時間
    private float elapsedTime;
    // 実行中イベントインデックス
    private int currentEventIndex;
    // 現在有効な当たり判定管理
    private Dictionary<int, Collider> activeColliders = new();
    // イベントリスト取得用プロパティ
    public List<EffectEvent> Events
    {
        get
        {
            return events;
        }
    }

    private void Awake()
    {
        // フレーム順にイベントをソート
        events.Sort((a, b) =>
        {
            return a.frame.CompareTo(b.frame);
        });

        // 子オブジェクトを含むすべてのパーティクルシステムを取得
        allParticleSystems = GetComponentsInChildren<ParticleSystem>();
    }

    public void EffectStart()
    {
        // 再生開始
        isPlaying = true;
        // 状態初期化
        currentFrame = -1;
        elapsedTime = 0f;
        currentEventIndex = 0;
        // 残っている当たり判定削除
        RemoveAllHitColliders();

        // すべてのパーティクルシステムを再生
        if (allParticleSystems != null && allParticleSystems.Length > 0)
        {
            foreach (ParticleSystem particle in allParticleSystems)
            {
                if (particle != null)
                {
                    // 再生速度設定
                    var main = particle.main;
                    main.simulationSpeed = playSpeed;
                    // 停止
                    particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    // 再生
                    particle.Play(true);
                }
            }
        }
        else if (mainParticle != null)
        {
            // フォールバック: allParticleSystemsが空の場合
            mainParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            mainParticle.Play(true);
        }
    }

    // エフェクト停止
    public void EffectStop()
    {
        isPlaying = false;

        // すべてのパーティクルシステムを停止
        if (allParticleSystems != null && allParticleSystems.Length > 0)
        {
            foreach (ParticleSystem particle in allParticleSystems)
            {
                if (particle != null)
                {
                    particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }
        else if (mainParticle != null)
        {
            mainParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        RemoveAllHitColliders();

        EffectManager.Instance.Release(effectType, this);
    }

    private void Update()
    {
        // 再生中でなければ終了
        if (!isPlaying) return;
        // メインパーティクル未設定なら終了
        if (mainParticle == null) return;
        // 経過時間更新
        elapsedTime += Time.deltaTime * playSpeed;
        // 現在フレーム計算
        int frame = Mathf.FloorToInt(elapsedTime * frameRate);
        // Debug.Log($"{effectType} Frame : {frame}");
        // フレーム更新処理
        while (currentFrame < frame)
        {
            currentFrame++;
            // フレームイベント実行
            ExecuteFrame(currentFrame);
            // 当たり判定更新
            UpdateHitColliders(currentFrame);
        }

        // すべてのパーティクルが停止したかチェック
        bool anyAlive = false;
        if (allParticleSystems != null && allParticleSystems.Length > 0)
        {
            foreach (ParticleSystem particle in allParticleSystems)
            {
                if (particle != null && particle.IsAlive(true))
                {
                    anyAlive = true;
                    break;
                }
            }
        }
        else if (mainParticle != null)
        {
            anyAlive = mainParticle.IsAlive(true);
        }
        // 全停止なら終了処理
        if (!anyAlive)
        {
            EffectStop();
        }
    }

    // 指定フレームのイベント実行
    private void ExecuteFrame(int frame)
    {
        while (currentEventIndex < events.Count)
        {
            // 現在イベント取得
            EffectEvent e = events[currentEventIndex];
            // 対象フレームでなければ終了
            if (e.frame != frame) break;
            // イベント実行
            ExecuteEvent(e);

            currentEventIndex++;
        }
    }

    // イベント実行
    private void ExecuteEvent(EffectEvent e)
    {
        //イベントの種類で処理分岐
        switch (e.type)
        {
            // 当たり判定イベント
            case EffectEventType.Hit:
                //コライダー生成
                CreateHitCollider(e);
                break;
            // サウンドイベント
            case EffectEventType.Sound:
                //BGMかSEかで再生方法分岐
                if (e.useBGM)
                {
                    MySoundManeger.Play(gameObject, e.bgm);
                    Debug.Log($"{effectType} Play BGM : {e.bgm}");
                }
                else
                {
                    MySoundManeger.Play(gameObject, e.se);
                    Debug.Log($"{effectType} Play SE : {e.se}");
                }

                break;
            // カメラシェイクイベント
            case EffectEventType.CameraShake:

                //カメラシェイク
                CameraShakeManager.Shake
                    (
                        e.shakePower,
                        e.shakeTime,
                        e.shakeAxis,
                        e.shakeCurve
                     );

                break;

            // 関数イベント
            case EffectEventType.Function:
                //関数イベント実行
                e.onEvent.Invoke();
                break;
        }
    }

    private void CreateHitCollider(EffectEvent e)
    {
        //同じIDの当たり判定があれば削除
        RemoveHitCollider(e.hitId);

        GameObject obj;

        //trueならEffectPlayer自身に当たり判定を作る。falseなら子オブジェクトに当たり判定を作る
        if(e.useMainParentForHit)
        {
            obj = gameObject;
        }
        else
        {
            //子オブジェクト生成
            obj = new GameObject($"HitCollider_{e.hitId}");
            obj.transform.parent = transform;
            //初期化
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
        }

        
        //生成したコライダー保存用
        Collider created = null;
        //rigidbody追加（なければ）
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = obj.AddComponent<Rigidbody>();
        }
        //kinematicにして重力無効化
        if(!e.useMainParentForHit)  rb.isKinematic = true;
        rb.useGravity = false;
        //タグ設定
        if (!string.IsNullOrEmpty(e.hitTag))
        {
            obj.tag = e.hitTag;
        }
        //コライダー生成
        switch (e.colliderType)
        {
            //スフィアコライダー
            case HitColliderType.Sphere:

                //必要な情報を設定
                SphereCollider sphere = obj.AddComponent<SphereCollider>();
                sphere.isTrigger = true;
                sphere.center = e.hitOffset;
                sphere.radius = e.hitRadius;
                //作成Collider保持
                created = sphere;

                break;
            //ボックスコライダー
            case HitColliderType.Box:
                //必要な情報を設定
                BoxCollider box = obj.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.center = e.hitOffset;
                box.size = e.hitBoxSize;
                //作成Collider保持
                created = box;

                break;
            //カプセルコライダー
            case HitColliderType.Capsule:

                //必要な情報を設定
                CapsuleCollider capsule = obj.AddComponent<CapsuleCollider>();
                capsule.isTrigger = true;
                capsule.center = e.hitOffset;
                capsule.radius = e.capsuleRadius;
                capsule.height = e.capsuleHeight;
                capsule.direction = (int)e.capsuleDirection;
                //作成Collider保持
                created = capsule;

                break;
        }

        // 正常生成時のみ登録
        if (created != null)
        {
            activeColliders.Add(e.hitId, created);
        }

       
    }

    // 指定IDの当たり判定を削除
    private void RemoveHitCollider(int id)
    {
        // 登録済み判定
        if (activeColliders.ContainsKey(id))
        {
            // Collider取得
            Collider col = activeColliders[id];
            // 存在するなら削除
            if (col != null)
            {
                Destroy(col.gameObject);
            }
            // Dictionaryから削除
            activeColliders.Remove(id);
        }
    }

    // すべての当たり判定を削除
    private void RemoveAllHitColliders()
    {
        // 全Collider削除
        foreach (Collider col in activeColliders.Values)
        {
            if (col != null)
            {
                Destroy(col.gameObject);
            }
        }
        // Dictionary初期化
        activeColliders.Clear();
    }

    // 終了フレームを超えた当たり判定を削除
    private void UpdateHitColliders(int frame)
    {
        foreach (EffectEvent e in events)
        {
            if (e.type != EffectEventType.Hit) continue;

            if (frame > e.endFrame)
            {
                RemoveHitCollider(e.hitId);
            }
        }
    }

    // EffectType設定
    public void SetEffectType(EffectType type)
    {
        effectType = type;
    }

    // 再生速度設定
    public void SetPlaySpeed( float speed)
    {
        playSpeed = Mathf.Max(0.01f, speed);
    }


    public void SetEffectPos(Vector3 pos)
    {
        transform.position = pos;
    }

    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log($"{effectType} Hit : {other.name}");
    }

}