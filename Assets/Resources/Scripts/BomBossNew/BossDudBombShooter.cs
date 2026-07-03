using UnityEngine;

public class BossDudBombShooter : MonoBehaviour
{
    [SerializeField] private CameraFollow cameraFollow;

    [Header("発射する不発弾Prefab")]
    [SerializeField] private GameObject dudBombPrefab;

    [Header("発射位置")]
    [SerializeField] private Transform[] firePoints;

    [Header("発射間隔")]
    [SerializeField] private float shootInterval = 3.0f;

    [Header("発射速度")]
    [SerializeField] private float shootPower = 6.0f;

    [Header("上方向への補正")]
    [SerializeField] private float upwardPower = 1.5f;

    [Header("発射位置を前にずらす距離")]
    [SerializeField] private float forwardSpawnOffset = 0.8f;

    [Header("発射開始までの待ち時間")]
    [SerializeField] private float startDelay = 2.0f;

    [Header("自動発射する")]
    [SerializeField] private bool autoShoot = true;

    [Header("全砲台から同時に撃つ")]
    [SerializeField] private bool shootAllFirePoints = false;

    [Header("デバッグ用：Spaceキーで発射")]
    [SerializeField] private bool enableDebugKey = true;

    [Header("デバッグログ")]
    [SerializeField] private bool showDebugLog = true;

    private float timer;
    private int firePointIndex;
    private bool canShoot;
    private bool firstshoot;

    private void Start()
    {

        timer = 0f;
        canShoot = false;
        //Invoke(nameof(EnableShoot), startDelay);
        firstshoot = false;
    }

    private void Update()
    {
        //さいしょにたまがでなくなっちゃったーーーーー(ここせいらがいじりましたよーー
        if (!cameraFollow.Gamestart)
        {
            return;
        }
        else
        {
            if(!firstshoot)
            {
                Invoke(nameof(EnableShoot), startDelay);
                firstshoot = true;
            }
        }
        //ここまで



        if (enableDebugKey && Input.GetKeyDown(KeyCode.Space))
        {
            Shoot();
        }

        if (!autoShoot || !canShoot)
        {
            return;
        }

        timer += Time.deltaTime;

        if (timer >= shootInterval)
        {
            timer = 0f;
            Shoot();
        }
    }

    private void EnableShoot()
    {
        canShoot = true;
    }

    public void Shoot()
    {
        if (dudBombPrefab == null)
        {
            Debug.LogWarning("DudBombPrefab が設定されていません");
            return;
        }

        if (firePoints == null || firePoints.Length == 0)
        {
            Debug.LogWarning("FirePoints が設定されていません");
            return;
        }

        if (shootAllFirePoints)
        {
            for (int i = 0; i < firePoints.Length; i++)
            {
                ShootFromFirePoint(firePoints[i]);
            }
        }
        else
        {
            Transform firePoint = GetNextActiveFirePoint();

            if (firePoint == null)
            {
                Debug.LogWarning("発射可能なFirePointがありません");
                return;
            }

            ShootFromFirePoint(firePoint);
        }
    }

    private Transform GetNextActiveFirePoint()
    {
        if (firePoints == null || firePoints.Length == 0)
        {
            return null;
        }

        for (int i = 0; i < firePoints.Length; i++)
        {
            int index = firePointIndex;
            firePointIndex = GetNextFirePointIndex();

            Transform point = firePoints[index];

            if (point == null)
            {
                continue;
            }

            if (!point.gameObject.activeInHierarchy)
            {
                continue;
            }

            return point;
        }

        return null;
    }

    private void ShootFromFirePoint(Transform firePoint)
    {
        if (firePoint == null)
        {
            return;
        }

        if (!firePoint.gameObject.activeInHierarchy)
        {
            return;
        }

        Vector3 shootDirection = firePoint.forward.normalized;

        if (shootDirection.sqrMagnitude <= 0.01f)
        {
            shootDirection = transform.forward.normalized;
        }

        Vector3 spawnPosition = firePoint.position + shootDirection * forwardSpawnOffset;

        GameObject bomb = Instantiate(
            dudBombPrefab,
            spawnPosition,
            firePoint.rotation
        );

        Rigidbody rb = bomb.GetComponent<Rigidbody>();

        if (rb == null)
        {
            rb = bomb.GetComponentInChildren<Rigidbody>();
        }

        if (rb == null)
        {
            Debug.LogWarning("発射した不発弾に Rigidbody がありません");
            return;
        }

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Vector3 velocity = shootDirection * shootPower + Vector3.up * upwardPower;
        rb.linearVelocity = velocity;

        if (showDebugLog)
        {
            Debug.Log("不発弾を発射: " + firePoint.name + " / Direction: " + shootDirection);
        }
    }

    private int GetNextFirePointIndex()
    {
        return (firePointIndex + 1) % firePoints.Length;
    }
}