using System.Collections.Generic;
using UnityEngine;

public class EffectPlayer : MonoBehaviour
{
    [SerializeField]
    private EffectType effectType;

    [SerializeField]
    private ParticleSystem mainParticle;
    public ParticleSystem MainParticle => mainParticle;


    [SerializeField]
    private List<EffectEvent> events = new();

    [SerializeField]
    private int frameRate = 60;

    public int FrameRate => frameRate;

    private bool isPlaying;

    private int currentFrame;
    private float elapsedTime;
    private int currentEventIndex;
    private Dictionary<int, Collider> activeColliders =new();

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

        
    }

    public void EffectStart()
    {
        isPlaying = true;

        currentFrame = -1;
        elapsedTime = 0f;
        currentEventIndex = 0;

        RemoveAllHitColliders();

        if (mainParticle != null)
        {
            mainParticle.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);

            mainParticle.Play(true);
        }
    }

    public void EffectStop()
    {
        isPlaying = false;

        if (mainParticle != null)
        {
            mainParticle.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        RemoveAllHitColliders(); 

        EffectManager.Instance.Release( effectType, this);
    }

    

    private void Update()
    {
        if (!isPlaying)return;

        if (mainParticle == null) return;

        elapsedTime +=Time.deltaTime;

        int frame = Mathf.FloorToInt(elapsedTime *frameRate );
        Debug.Log( $"{effectType} Frame : {frame}");
        while (currentFrame < frame)
        {
            currentFrame++;

            ExecuteFrame( currentFrame );

            UpdateHitColliders( currentFrame);
        }

        if (!mainParticle.IsAlive(true))
        {
            EffectStop();
        }
    }

    private void ExecuteFrame(int frame)
    {
        while ( currentEventIndex < events.Count )
        {
            EffectEvent e = events[currentEventIndex];

            if (e.frame != frame)
                break;

            ExecuteEvent(e);

            currentEventIndex++;
        }
    }

    private void ExecuteEvent( EffectEvent e )
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
                    Debug.Log( $"{effectType} Play BGM : {e.bgm}");
                }
                else
                {
                    MySoundManeger.Play(gameObject, e.se);
                    Debug.Log( $"{effectType} Play SE : {e.se}" );
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

    private void CreateHitCollider( EffectEvent e)
    {
        RemoveHitCollider( e.hitId);

        GameObject obj =new GameObject( $"Hit_{e.hitId}");

        obj.transform.SetParent( transform);

        obj.transform.localPosition = Vector3.zero;

        obj.transform.localRotation = Quaternion.identity;

        Collider created = null;

        switch (e.colliderType)
        {
            case HitColliderType.Sphere:

                SphereCollider sphere = obj.AddComponent<SphereCollider >();

                sphere.isTrigger = true;

                sphere.center = e.hitOffset;

                sphere.radius = e.hitRadius;

                created = sphere;

                break;

            case HitColliderType.Box:

                BoxCollider box = obj.AddComponent <BoxCollider >();

                box.isTrigger = true;

                box.center =e.hitOffset;

                box.size = e.hitBoxSize;

                created = box;

                break;

            case HitColliderType.Capsule:

                CapsuleCollider capsule = obj.AddComponent <CapsuleCollider >();

                capsule.isTrigger = true;

                capsule.center =e.hitOffset;

                capsule.radius =e.capsuleRadius;

                capsule.height = e.capsuleHeight;

                capsule.direction =(int)e.capsuleDirection;

                created = capsule;

                break;
        }

        if (created != null)
        {
            activeColliders.Add(e.hitId,created);
        }
    }

    private void RemoveHitCollider(int id)
    {
        if ( activeColliders.ContainsKey( id) )
        {
            Collider col = activeColliders[id];

            if (col != null)
            {
                Destroy(col.gameObject );
            }

            activeColliders.Remove(id);
        }
    }

    private void RemoveAllHitColliders()
    {
        foreach (Collider col in activeColliders.Values )
        {
            if (col != null)
            {
                Destroy( col.gameObject);
            }
        }

        activeColliders.Clear();
    }

    private void UpdateHitColliders(int frame)
    {
        foreach ( EffectEvent e in events )
        {
            if ( e.type != EffectEventType.Hit) continue;

            if (frame > e.endFrame )
            {
                RemoveHitCollider( e.hitId);
            }
        }
    }

    private void OnTriggerEnter(Collider other )
    {
        Debug.Log( $"{effectType} Hit : {other.name}");
    }
}