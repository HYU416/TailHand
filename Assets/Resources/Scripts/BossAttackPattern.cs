using System.Collections;
using UnityEngine;

public class BossRandomAttack : MonoBehaviour
{
    [System.Serializable]
    public class GunData
    {
        [Header("’e‚ğo‚·GUN–{‘Ì")]
        public Transform gun;

        [Header("’e‚ğo‚·•ûŒü")]
        public ShootAxis shootAxis = ShootAxis.Up;

        [Header("GUN’†S‚©‚ç–Cgæ’[‚Ü‚Å‚Ì‹——£")]
        public float muzzleOffset = 1.2f;

        [Header("’e‚Ì‘¬“x")]
        public float bombSpeed = 10.0f;
    }

    public enum ShootAxis
    {
        Forward,
        Back,
        Right,
        Left,
        Up,
        Down
    }

    [Header("UŒ‚‚Ég‚¤GUN‚½‚¿")]
    public GunData[] guns;

    [Header("”š”­‚·‚é”š’ePrefab")]
    public GameObject bombPrefab;

    [Header("•s”­’ePrefab")]
    public GameObject dudBombPrefab;

    [Header("UŒ‚2‚¾‚¯‚Å•s”­’e‚ªo‚éŠm—¦")]
    [Range(0f, 100f)]
    public float dudChance = 25f;

    [Header("”š’e‚²‚Æ‚ÌLinear Dampingƒ‰ƒ“ƒ_ƒ€”ÍˆÍ")]
    public float minLinearDamping = 0.05f;
    public float maxLinearDamping = 0.2f;

    [Header("UŒ‚1‚¾‚¯”š’eƒTƒCƒY‚ğ‘å‚«‚­‚·‚é")]
    public float attack1BombScale = 2.0f;

    [Header("‰ñ“]‚³‚¹‚éƒ{ƒX–{‘Ì")]
    public Transform rotateRoot;

    [Header("UŒ‚‚ÆUŒ‚‚ÌŠÔŠu")]
    public float attackWaitTime = 2.5f;

    [Header("UŒ‚1Fƒ‰ƒ“ƒ_ƒ€‰ñ“]‚µ‚Ä~‚Ü‚Á‚Ä‚©‚çUŒ‚")]
    public float randomRotateTime = 1.0f;

    public float[] randomAngles =
    {
        0f,
        45f,
        90f,
        135f,
        180f,
        225f,
        270f,
        315f
    };

    [Header("UŒ‚2F‰ñ“]‚µ‚È‚ª‚çUŒ‚")]
    public float spinAttackTime = 4.0f;
    public float spinRotateSpeed = 180.0f;
    public float spinFireInterval = 0.6f;

    [Header("UŒ‚3F‹ó”š‚ğ‰½‰ñ‚É1‰ñ“ü‚ê‚é‚©")]
    public int airStrikeEvery = 5;

    [Header("‹ó”š‚Å—‚Æ‚·”š’e‚Ì”")]
    public int airStrikeBombCount = 8;

    [Header("‹ó”š‚Ì”š’e‚ğ—‚Æ‚·ŠÔŠu")]
    public float airStrikeInterval = 0.25f;

    [Header("‹ó”š‚Ì‚‚³")]
    public float airStrikeHeight = 15.0f;

    [Header("‹ó”š‚Ì—‰º‘¬“x")]
    public float airStrikeFallSpeed = 12.0f;

    [Header("‹ó”š‚ÌÅ¬‹——£")]
    public float airStrikeMinDistance = 8.0f;

    [Header("‹ó”š‚ÌÅ‘å‹——£")]
    public float airStrikeMaxDistance = 20.0f;

    [Header("‹ó”š’†SˆÊ’u")]
    public Transform airStrikeCenter;

    private Quaternion baseRotation;
    private bool isAttacking = false;
    private int attackCount = 0;

    void Start()
    {
        if (rotateRoot == null)
        {
            rotateRoot = transform;
        }

        if (airStrikeCenter == null)
        {
            airStrikeCenter = transform;
        }

        baseRotation = rotateRoot.rotation;

        StartCoroutine(AttackLoop());
    }

    IEnumerator AttackLoop()
    {
        while (true)
        {
            if (!isAttacking)
            {
                isAttacking = true;

                int attackType = ChooseAttackType();

                if (attackType == 1)
                {
                    yield return StartCoroutine(RandomRotateAttack());
                }
                else if (attackType == 2)
                {
                    yield return StartCoroutine(SpinAttack());
                }
                else if (attackType == 3)
                {
                    yield return StartCoroutine(AirStrikeAttack());
                }

                isAttacking = false;
            }

            yield return new WaitForSeconds(attackWaitTime);
        }
    }

    int ChooseAttackType()
    {
        attackCount++;

        // w’è‰ñ”‚É1‰ñA•K‚¸‹ó”šUŒ‚
        if (airStrikeEvery > 0 && attackCount >= airStrikeEvery)
        {
            attackCount = 0;
            return 3;
        }

        // ‚»‚êˆÈŠO‚ÍUŒ‚1‚©UŒ‚2‚ğƒ‰ƒ“ƒ_ƒ€
        int normalAttack = Random.Range(0, 2);

        if (normalAttack == 0)
        {
            return 1;
        }

        return 2;
    }

    IEnumerator RandomRotateAttack()
    {
        Debug.Log("UŒ‚1Fƒ‰ƒ“ƒ_ƒ€Šp“x‚É‰ñ“]‚µ‚ÄA~‚Ü‚Á‚Ä‚©‚ç‘å‚«‚¢”š’e‚ğ”­Ë");

        float angle = randomAngles[Random.Range(0, randomAngles.Length)];

        Quaternion targetRotation =
            baseRotation * Quaternion.Euler(0f, angle, 0f);

        yield return StartCoroutine(RotateTo(targetRotation, randomRotateTime));

        // UŒ‚1‚Í‘å‚«‚¢”š’e‚Ì‚İB•s”­’e‚Ío‚³‚È‚¢
        ShootAllGuns(false, true);
    }

    IEnumerator SpinAttack()
    {
        Debug.Log("UŒ‚2F‰ñ“]‚µ‚È‚ª‚çUŒ‚B•s”­’e‚Í‚±‚ÌUŒ‚‚¾‚¯o‚é");

        float timer = 0f;
        float fireTimer = 0f;

        while (timer < spinAttackTime)
        {
            timer += Time.deltaTime;
            fireTimer += Time.deltaTime;

            rotateRoot.Rotate(
                0f,
                spinRotateSpeed * Time.deltaTime,
                0f
            );

            if (fireTimer >= spinFireInterval)
            {
                fireTimer = 0f;

                // UŒ‚2‚¾‚¯•s”­’e‚ ‚èB”š’eƒTƒCƒY‚Í•’Ê
                ShootAllGuns(true, false);
            }

            yield return null;
        }
    }

    IEnumerator AirStrikeAttack()
    {
        Debug.Log("UŒ‚3F‹ó”šUŒ‚");

        for (int i = 0; i < airStrikeBombCount; i++)
        {
            SpawnAirStrikeBomb();

            yield return new WaitForSeconds(airStrikeInterval);
        }
    }

    void SpawnAirStrikeBomb()
    {
        if (bombPrefab == null)
        {
            Debug.LogWarning("Bomb Prefab ‚ªİ’è‚³‚ê‚Ä‚¢‚Ü‚¹‚ñ");
            return;
        }

        Vector3 center = airStrikeCenter.position;

        float randomAngle = Random.Range(0f, 360f);

        float randomDistance = Random.Range(
            airStrikeMinDistance,
            airStrikeMaxDistance
        );

        Vector3 direction = new Vector3(
            Mathf.Cos(randomAngle * Mathf.Deg2Rad),
            0f,
            Mathf.Sin(randomAngle * Mathf.Deg2Rad)
        );

        Vector3 groundPosition = center + direction * randomDistance;

        Vector3 spawnPosition = new Vector3(
            groundPosition.x,
            center.y + airStrikeHeight,
            groundPosition.z
        );

        GameObject bomb = Instantiate(
            bombPrefab,
            spawnPosition,
            Quaternion.identity
        );

        Rigidbody rb = bomb.GetComponent<Rigidbody>();

        if (rb == null)
        {
            rb = bomb.GetComponentInChildren<Rigidbody>();
        }

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = false;

            rb.linearDamping = Random.Range(minLinearDamping, maxLinearDamping);

            rb.linearVelocity = Vector3.down * airStrikeFallSpeed;
        }
        else
        {
            Debug.LogWarning("‹ó”š—pBOM‚ÉRigidbody‚ª‚ ‚è‚Ü‚¹‚ñ");
        }
    }

    void ShootAllGuns(bool allowDudBomb, bool bigBomb)
    {
        if (guns == null || guns.Length == 0)
        {
            Debug.LogWarning("Guns ‚ªİ’è‚³‚ê‚Ä‚¢‚Ü‚¹‚ñ");
            return;
        }

        int dudIndex = -1;

        // allowDudBomb ‚ª true ‚Ì‚¾‚¯•s”­’e‚ğo‚·
        if (allowDudBomb && dudBombPrefab != null)
        {
            bool spawnDud = Random.Range(0f, 100f) < dudChance;

            if (spawnDud)
            {
                dudIndex = Random.Range(0, guns.Length);
            }
        }

        for (int i = 0; i < guns.Length; i++)
        {
            bool isDud = i == dudIndex;
            Shoot(guns[i], isDud, bigBomb);
        }
    }

    void Shoot(GunData gunData, bool isDud, bool bigBomb)
    {
        if (gunData == null)
        {
            return;
        }

        if (gunData.gun == null)
        {
            Debug.LogWarning("Gun ‚ªİ’è‚³‚ê‚Ä‚¢‚È‚¢Element‚ª‚ ‚è‚Ü‚·");
            return;
        }

        GameObject prefab = isDud ? dudBombPrefab : bombPrefab;

        if (prefab == null)
        {
            Debug.LogWarning("”š’ePrefabA‚Ü‚½‚Í•s”­’ePrefab‚ªİ’è‚³‚ê‚Ä‚¢‚Ü‚¹‚ñ");
            return;
        }

        Vector3 shootDirection =
            GetShootDirection(gunData.gun, gunData.shootAxis);

        Vector3 spawnPosition =
            gunData.gun.position + shootDirection * gunData.muzzleOffset;

        GameObject bomb = Instantiate(
            prefab,
            spawnPosition,
            Quaternion.LookRotation(shootDirection)
        );

        // bigBomb ‚ª true ‚Ì‚¾‚¯ƒTƒCƒY‚ğ‘å‚«‚­‚·‚é
        // ‚½‚¾‚µ•s”­’e‚Íâ‘Î‚É‘å‚«‚­‚µ‚È‚¢
        if (bigBomb && !isDud)
        {
            bomb.transform.localScale *= attack1BombScale;

            BombExplosion bombExplosion = bomb.GetComponent<BombExplosion>();

            if (bombExplosion == null)
            {
                bombExplosion = bomb.GetComponentInChildren<BombExplosion>();
            }

            if (bombExplosion != null)
            {
                bombExplosion.explosionEffectScaleMultiplier = attack1BombScale;
            }
        }

        Rigidbody rb = bomb.GetComponent<Rigidbody>();

        if (rb == null)
        {
            rb = bomb.GetComponentInChildren<Rigidbody>();
        }

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;

            rb.linearDamping = Random.Range(minLinearDamping, maxLinearDamping);

            rb.linearVelocity = shootDirection.normalized * gunData.bombSpeed;
        }
        else
        {
            Debug.LogWarning("”­Ë‚µ‚½BOM‚ÉRigidbody‚ª‚ ‚è‚Ü‚¹‚ñ");
        }
    }

    IEnumerator RotateTo(Quaternion targetRotation, float rotateTime)
    {
        Quaternion startRotation = rotateRoot.rotation;
        float timer = 0f;

        while (timer < rotateTime)
        {
            timer += Time.deltaTime;

            float t = timer / rotateTime;

            rotateRoot.rotation = Quaternion.Slerp(
                startRotation,
                targetRotation,
                t
            );

            yield return null;
        }

        rotateRoot.rotation = targetRotation;
    }

    Vector3 GetShootDirection(Transform gun, ShootAxis axis)
    {
        switch (axis)
        {
            case ShootAxis.Forward:
                return gun.forward;

            case ShootAxis.Back:
                return -gun.forward;

            case ShootAxis.Right:
                return gun.right;

            case ShootAxis.Left:
                return -gun.right;

            case ShootAxis.Up:
                return gun.up;

            case ShootAxis.Down:
                return -gun.up;

            default:
                return gun.forward;
        }
    }
}