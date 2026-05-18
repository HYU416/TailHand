using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EffectPlayer : MonoBehaviour
{
    [SerializeField]
    private EffectType effectType;

    [SerializeField]
    private ParticleSystem mainParticle;
    public ParticleSystem MainParticle => mainParticle;

    // 子オブジェクトを含むすべてのパーティクルシステム
    private ParticleSystem[] allParticleSystems;

    [SerializeField]
    private List<EffectEvent> events = new();

    [SerializeField]
    private int frameRate = 60;

    public int FrameRate => frameRate;

    [SerializeField]
    private float playSpeed = 1f;

    public float PlaySpeed
    {
        get => playSpeed;
        set => playSpeed = Mathf.Max(0.01f, value);
    }

    private bool isPlaying;

    private int currentFrame;
    private float elapsedTime;
    private int currentEventIndex;
    private Dictionary<int, Collider> activeColliders = new();

    public List<EffectEvent> Events
    {
        get
        {
            return events;
        }
    }

    private void Awake()
    {
        events.Sort((a, b) =>
        {
            return a.frame.CompareTo(b.frame);
        });

        // 子オブジェクトを含むすべてのパーティクルシステムを取得
        allParticleSystems = GetComponentsInChildren<ParticleSystem>();
    }

    public void EffectStart()
    {
        isPlaying = true;

        currentFrame = -1;
        elapsedTime = 0f;
        currentEventIndex = 0;

        RemoveAllHitColliders();

        // すべてのパーティクルシステムを再生
        if (allParticleSystems != null && allParticleSystems.Length > 0)
        {
            foreach (ParticleSystem particle in allParticleSystems)
            {
                if (particle != null)
                {
                    var main = particle.main;
                    main.simulationSpeed = playSpeed;

                    particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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
        if (!isPlaying) return;

        if (mainParticle == null) return;

        elapsedTime += Time.deltaTime * playSpeed;

        int frame = Mathf.FloorToInt(elapsedTime * frameRate);
        Debug.Log($"{effectType} Frame : {frame}");
        while (currentFrame < frame)
        {
            currentFrame++;

            ExecuteFrame(currentFrame);

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

        if (!anyAlive)
        {
            EffectStop();
        }
    }

    private void ExecuteFrame(int frame)
    {
        while (currentEventIndex < events.Count)
        {
            EffectEvent e = events[currentEventIndex];

            if (e.frame != frame)
                break;

            ExecuteEvent(e);

            currentEventIndex++;
        }
    }

    private void ExecuteEvent(EffectEvent e)
    {
        switch (e.type)
        {
            case EffectEventType.Hit:

                CreateHitCollider(e);

                break;

            case EffectEventType.Sound:
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

            case EffectEventType.CameraShake:

                CameraShakeManager.Shake
                    (
                        e.shakePower,
                        e.shakeTime,
                        e.shakeAxis,
                        e.shakeCurve
                     );

                break;

            case EffectEventType.Function:
                e.onEvent.Invoke();
                break;
        }
    }

    private void CreateHitCollider(EffectEvent e)
    {
        RemoveHitCollider(e.hitId);

        GameObject obj = new GameObject($"Hit_{e.hitId}");

        obj.transform.SetParent(transform);

        obj.transform.localPosition = Vector3.zero;

        obj.transform.localRotation = Quaternion.identity;

        Collider created = null;

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = obj.AddComponent<Rigidbody>();
        }

        rb.isKinematic = true;
        rb.useGravity = false;

        if (e.hitTag != null) obj.tag = e.hitTag;
        switch (e.colliderType)
        {
            case HitColliderType.Sphere:

                SphereCollider sphere = obj.AddComponent<SphereCollider>();

                sphere.isTrigger = true;

                sphere.center = e.hitOffset;

                sphere.radius = e.hitRadius;

                created = sphere;

                break;

            case HitColliderType.Box:

                BoxCollider box = obj.AddComponent<BoxCollider>();

                box.isTrigger = true;

                box.center = e.hitOffset;

                box.size = e.hitBoxSize;

                created = box;

                break;

            case HitColliderType.Capsule:

                CapsuleCollider capsule = obj.AddComponent<CapsuleCollider>();

                capsule.isTrigger = true;

                capsule.center = e.hitOffset;

                capsule.radius = e.capsuleRadius;

                capsule.height = e.capsuleHeight;

                capsule.direction = (int)e.capsuleDirection;

                created = capsule;

                break;
        }

        
        if (created != null)
        {
            activeColliders.Add(e.hitId, created);
        }

       
    }

    private void RemoveHitCollider(int id)
    {
        if (activeColliders.ContainsKey(id))
        {
            Collider col = activeColliders[id];

            if (col != null)
            {
                Destroy(col.gameObject);
            }

            activeColliders.Remove(id);
        }
    }

    private void RemoveAllHitColliders()
    {
        foreach (Collider col in activeColliders.Values)
        {
            if (col != null)
            {
                Destroy(col.gameObject);
            }
        }

        activeColliders.Clear();
    }

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

    public void SetEffectType(EffectType type)
    {
        effectType = type;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"{effectType} Hit : {other.name}");
    }
}