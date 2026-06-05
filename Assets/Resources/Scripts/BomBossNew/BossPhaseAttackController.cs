using System.Collections;
using UnityEngine;

public partial class BossPhaseAttackController : MonoBehaviour
{
    public enum ShootAxis
    {
        Forward,
        Back,
        Right,
        Left,
        Up,
        Down
    }

    public enum AttackKind
    {
        ”š’eUŒ‚,
        ‹ó”š,
        ’Ç”öƒ~ƒTƒCƒ‹,
        ‰ñ“]’e–‹,
        ˆÚ“®,
        ˆÚ“®‚µ‚È‚ª‚ç‹ó”š
    }

    public enum RotateDirection
    {
        Œv‰ñ‚è,
        ”½Œv‰ñ‚è
    }

    [System.Serializable]
    public class GunSetting
    {
        [Header("–C‘äTransform")]
        public Transform gun;

        [Header("”­Ë•ûŒü")]
        public ShootAxis shootAxis = ShootAxis.Forward;

        [Header("–CŒû‚©‚ç­‚µ‘O‚Éo‚·‹——£")]
        public float muzzleOffset = 0.8f;

        [Header("‚±‚Ì–C‘ä‚ğg‚¤")]
        public bool useThisGun = true;
    }

    [System.Serializable]
    public class PhaseAttackSetting
    {
        [Header("‚±‚Ì’iŠK‚ÌUŒ‚‚ğg‚¤")]
        public bool useThisPhase = true;

        [Header("UŒ‚‡")]
        public AttackKind[] attackOrder;

        [Header("UŒ‚‘O‚Ì‘Ò‚¿ŠÔ")]
        public float waitBeforeAttack = 1.0f;

        [Header("UŒ‚Œã‚Ì‘Ò‚¿ŠÔ")]
        public float waitAfterAttack = 1.5f;

        [Header("”š’eUŒ‚")]
        public int bombShotCount = 10;
        public float bombShotInterval = 0.25f;
        public float bombShootPower = 12.0f;
        public float bombUpwardPower = 0.15f;
        public float bombSpinSpeed = 180.0f;
        public bool bombShootAllGuns = false;

        [Header("•s”­’e¬“ü")]
        [Range(0f, 100f)]
        public float dudBombMixRate = 20.0f;
        public float dudShootPower = 6.0f;
        public float dudUpwardPower = 1.5f;

        [Header("‹ó”š")]
        public int airStrikeCount = 8;
        public float airStrikeInterval = 0.25f;
        public float airStrikeHeight = 15.0f;
        public float airStrikeFallSpeed = 12.0f;
        public float airStrikeMinDistance = 5.0f;
        public float airStrikeMaxDistance = 15.0f;

        [Header("’Ç”öƒ~ƒTƒCƒ‹")]
        public int missileCount = 4;
        public float missileInterval = 0.4f;
        public float missileSpeed = 12.0f;
        public float missileRotateSpeed = 180.0f;
        public float missileScale = 1.0f;
        public float missileExplosionTime = 5.0f;
        public bool missileHoming = true;
        public bool missileUseGravity = false;
        public bool missileExplodeOnHit = true;
        public bool missileExplodeOnlyPlayerHit = false;
        public float missileExplosionRadius = 3.0f;
        public int missileDamage = 20;
        public float missileExplosionEffectScaleMultiplier = 1.0f;

        [Header("‰ñ“]’e–‹")]
        public float bulletHellTime = 4.0f;
        public float bulletHellRotateSpeed = 120.0f;
        public RotateDirection bulletHellRotateDirection = RotateDirection.Œv‰ñ‚è;
        public float bulletHellFireInterval = 0.12f;
        public float bulletHellBulletSpeed = 10.0f;
        public float bulletHellBulletScale = 1.0f;
        public float bulletHellBulletLifeTime = 3.0f;
        public float bulletHellBulletShrinkTime = 0.5f;
        public int bulletHellDamage = 10;
        public bool bulletHellDestroyOnPlayerHit = true;

        [Header("ˆÚ“®")]
        public float moveSpeed = 3.0f;
        public float moveStopDistance = 0.1f;
    }

    [Header("’iŠKŠÇ—")]
    [SerializeField] private BossPhaseController phaseController;

    [Header("’Êí”š’ePrefab")]
    [SerializeField] private GameObject bombPrefab;

    [Header("•s”­’ePrefab")]
    [SerializeField] private GameObject dudBombPrefab;

    [Header("ƒ~ƒTƒCƒ‹Prefab")]
    [SerializeField] private GameObject missilePrefab;

    [Header("’e–‹’ePrefab")]
    [SerializeField] private GameObject bulletHellBulletPrefab;

    [Header("ƒ~ƒTƒCƒ‹”š”­ƒGƒtƒFƒNƒg")]
    [SerializeField] private GameObject missileExplosionEffectPrefab;

    [Header("‰ñ“]‚³‚¹‚é–{‘Ì")]
    [SerializeField] private Transform rotateRoot;

    [Header("3’iŠK–Ú‚Ì‰ñ“]’†S")]
    [SerializeField] private Transform phase3RotateCenter;

    [Header("3’iŠK–Ú‚Í•K‚¸‰ñ“]’†S‚ğg‚¤")]
    [SerializeField] private bool forcePhase3RotateAroundCenter = true;

    [Header("‹ó”š’†S")]
    [SerializeField] private Transform airStrikeCenter;

    [Header("ƒvƒŒƒCƒ„[")]
    [SerializeField] private Transform player;

    [Header("–C‘äİ’è")]
    [SerializeField] private GunSetting[] gunSettings;

    [Header("1’iŠK‚ ‚½‚è‚Ì–C‘ä”")]
    [SerializeField] private int gunsPerPhase = 4;

    [Header("ˆÚ“®’n“_")]
    [SerializeField] private Transform[] movePoints;

    [Header("1’iŠK–Ú‚ÌUŒ‚İ’è")]
    [SerializeField] private PhaseAttackSetting phase1Setting;

    [Header("2’iŠK–Ú‚ÌUŒ‚İ’è")]
    [SerializeField] private PhaseAttackSetting phase2Setting;

    [Header("3’iŠK–Ú‚ÌUŒ‚İ’è")]
    [SerializeField] private PhaseAttackSetting phase3Setting;

    [Header("ƒQ[ƒ€ŠJn‚ÉUŒ‚ŠJn")]
    [SerializeField] private bool playOnStart = true;

    [Header("UŒ‚ŠJn‚Ü‚Å‚Ì‘Ò‚¿ŠÔ")]
    [SerializeField] private float startDelay = 2.0f;

    [Header("ƒXƒsƒ“UŒ‚")]
    [SerializeField] private bool useSpinAttack = true;
    [SerializeField] private float spinDetectRange = 4.0f;
    [SerializeField] private float spinAttackTime = 1.2f;
    [SerializeField] private float spinRotateSpeed = 720.0f;
    [SerializeField] private float spinCooldown = 4.0f;
    [SerializeField] private float spinKnockbackPower = 8.0f;
    [SerializeField] private float spinKnockbackUpPower = 2.0f;

    [Header("ƒfƒoƒbƒO")]
    [SerializeField] private bool showDebugLog = true;

    private int currentAttackIndex;
    private int lastPhase = -1;
    private int nextMovePointIndex;
    private bool isMainAttackRunning;
    private bool isSpinRunning;
    private float spinCooldownTimer;
    private int nextGunIndexInPhase;

    private void Reset()
    {
        CreateDefaultSettings();
    }

    private void Awake()
    {
        if (rotateRoot == null)
        {
            rotateRoot = transform;
        }

        if (airStrikeCenter == null)
        {
            airStrikeCenter = transform;
        }

        if (phaseController == null)
        {
            phaseController = GetComponent<BossPhaseController>();
        }

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }
    }

    private void Start()
    {
        if (playOnStart)
        {
            StartCoroutine(MainAttackLoop());
        }
    }

    private void Update()
    {
        UpdateSpinAttack();
    }

    private IEnumerator MainAttackLoop()
    {
        yield return new WaitForSeconds(startDelay);

        isMainAttackRunning = true;

        while (true)
        {
            if (phaseController != null)
            {
                if (phaseController.IsBossDefeated)
                {
                    yield break;
                }

                if (phaseController.IsChangingPhase)
                {
                    yield return null;
                    continue;
                }
            }

            int phase = GetCurrentPhase();

            if (phase != lastPhase)
            {
                lastPhase = phase;
                currentAttackIndex = 0;
                nextGunIndexInPhase = 0;
            }

            PhaseAttackSetting setting = GetCurrentSetting();

            if (setting == null || !setting.useThisPhase)
            {
                yield return null;
                continue;
            }

            yield return new WaitForSeconds(setting.waitBeforeAttack);

            if (ShouldUseAfterAllWallsAttackPattern())
            {
                AttackKind specialAttack = GetAfterAllWallsAttackKind();

                if (specialAttack == AttackKind.‹ó”š)
                {
                    if (showDebugLog)
                    {
                        Debug.Log("•Ç‘S”j‰óŒãUŒ‚: ‹ó”š");
                    }

                    yield return StartCoroutine(Attack_AirStrike(setting));
                }
                else
                {
                    if (showDebugLog)
                    {
                        Debug.Log("•Ç‘S”j‰óŒãUŒ‚: ƒXƒsƒ“");
                    }

                    yield return StartCoroutine(SpinAttackRoutine());
                }

                AdvanceAfterAllWallsAttackIndex();

                yield return new WaitForSeconds(setting.waitAfterAttack);

                continue;
            }

            if (setting.attackOrder == null || setting.attackOrder.Length == 0)
            {
                yield return null;
                continue;
            }

            if (currentAttackIndex >= setting.attackOrder.Length)
            {
                currentAttackIndex = 0;
            }

            AttackKind attack = setting.attackOrder[currentAttackIndex];

            yield return StartCoroutine(ExecuteAttack(attack, setting));

            yield return new WaitForSeconds(setting.waitAfterAttack);

            currentAttackIndex++;
        }
    }

    private IEnumerator ExecuteAttack(AttackKind attack, PhaseAttackSetting setting)
    {
        if (ShouldUseAfterAllWallsAttackPattern())
        {
            attack = AttackKind.‹ó”š;
        }

        if (showDebugLog)
        {
            Debug.Log("ƒ{ƒXUŒ‚ŠJn: " + attack + " / Phase " + GetCurrentPhase());
        }

        if (attack == AttackKind.”š’eUŒ‚)
        {
            yield return StartCoroutine(Attack_DudBombShot(setting));
        }
        else if (attack == AttackKind.‹ó”š)
        {
            yield return StartCoroutine(Attack_AirStrike(setting));
        }
        else if (attack == AttackKind.’Ç”öƒ~ƒTƒCƒ‹)
        {
            yield return StartCoroutine(Attack_HomingMissile(setting));
        }
        else if (attack == AttackKind.‰ñ“]’e–‹)
        {
            yield return StartCoroutine(Attack_BulletHell(setting));
        }
        else if (attack == AttackKind.ˆÚ“®)
        {
            yield return StartCoroutine(Attack_Move(setting));
        }
        else if (attack == AttackKind.ˆÚ“®‚µ‚È‚ª‚ç‹ó”š)
        {
            yield return StartCoroutine(Attack_MoveAndAirStrike(setting));
        }
    }

    private int GetCurrentPhase()
    {
        if (phaseController == null)
        {
            return 1;
        }

        return phaseController.CurrentPhase;
    }

    private PhaseAttackSetting GetCurrentSetting()
    {
        int phase = GetCurrentPhase();

        if (phase == 1)
        {
            return phase1Setting;
        }

        if (phase == 2)
        {
            return phase2Setting;
        }

        if (phase == 3)
        {
            return phase3Setting;
        }

        return null;
    }

    private Transform GetPhase3RotateCenter()
    {
        if (phaseController != null && phaseController.Phase3CoreTransform != null)
        {
            return phaseController.Phase3CoreTransform;
        }

        if (phase3RotateCenter != null)
        {
            return phase3RotateCenter;
        }

        return null;
    }

    private void RotateBossForAttack(float rotateSpeed)
    {
        if (rotateRoot == null)
        {
            rotateRoot = transform;
        }

        float rotateAmount = rotateSpeed * Time.deltaTime;

        if (forcePhase3RotateAroundCenter && GetCurrentPhase() == 3)
        {
            Transform center = GetPhase3RotateCenter();

            if (center != null)
            {
                rotateRoot.RotateAround(
                    center.position,
                    Vector3.up,
                    rotateAmount
                );

                return;
            }

            if (showDebugLog)
            {
                Debug.LogWarning("3’iŠK–Ú‚Ì‰ñ“]’†S‚ªİ’è‚³‚ê‚Ä‚¢‚Ü‚¹‚ñB’Êí‰ñ“]‚É–ß‚µ‚Ü‚·B");
            }
        }

        rotateRoot.Rotate(
            Vector3.up,
            rotateAmount,
            Space.World
        );
    }

    private void RotateBossForAttack(float rotateSpeed, RotateDirection rotateDirection)
    {
        float direction = 1f;

        if (rotateDirection == RotateDirection.”½Œv‰ñ‚è)
        {
            direction = -1f;
        }

        RotateBossForAttack(rotateSpeed * direction);
    }

    private Vector3 GetShootDirection(Transform gun, ShootAxis axis)
    {
        if (gun == null)
        {
            return transform.forward;
        }

        if (axis == ShootAxis.Forward) return gun.forward;
        if (axis == ShootAxis.Back) return -gun.forward;
        if (axis == ShootAxis.Right) return gun.right;
        if (axis == ShootAxis.Left) return -gun.right;
        if (axis == ShootAxis.Up) return gun.up;
        if (axis == ShootAxis.Down) return -gun.up;

        return gun.forward;
    }

    private int GetCurrentPhaseGunStartIndex()
    {
        int phase = GetCurrentPhase();

        if (phase < 1)
        {
            phase = 1;
        }

        if (gunsPerPhase <= 0)
        {
            gunsPerPhase = 1;
        }

        return (phase - 1) * gunsPerPhase;
    }

    private int GetCurrentPhaseGunEndIndex()
    {
        return GetCurrentPhaseGunStartIndex() + gunsPerPhase;
    }

    private bool IsGunIndexInCurrentPhase(int index)
    {
        int startIndex = GetCurrentPhaseGunStartIndex();
        int endIndex = GetCurrentPhaseGunEndIndex();

        return index >= startIndex && index < endIndex;
    }

    private GunSetting GetNextUsableGun()
    {
        GunSetting[] usableGuns = GetUsableGuns();

        if (usableGuns == null || usableGuns.Length == 0)
        {
            return null;
        }

        if (nextGunIndexInPhase >= usableGuns.Length)
        {
            nextGunIndexInPhase = 0;
        }

        GunSetting gun = usableGuns[nextGunIndexInPhase];
        nextGunIndexInPhase++;

        if (nextGunIndexInPhase >= usableGuns.Length)
        {
            nextGunIndexInPhase = 0;
        }

        return gun;
    }

    private GunSetting[] GetUsableGuns()
    {
        if (gunSettings == null || gunSettings.Length == 0)
        {
            return null;
        }

        int count = 0;

        for (int i = 0; i < gunSettings.Length; i++)
        {
            if (!IsGunIndexInCurrentPhase(i))
            {
                continue;
            }

            if (gunSettings[i] != null &&
                gunSettings[i].useThisGun &&
                gunSettings[i].gun != null &&
                gunSettings[i].gun.gameObject.activeInHierarchy)
            {
                count++;
            }
        }

        if (count == 0)
        {
            return null;
        }

        GunSetting[] result = new GunSetting[count];
        int resultIndex = 0;

        for (int i = 0; i < gunSettings.Length; i++)
        {
            if (!IsGunIndexInCurrentPhase(i))
            {
                continue;
            }

            if (gunSettings[i] != null &&
                gunSettings[i].useThisGun &&
                gunSettings[i].gun != null &&
                gunSettings[i].gun.gameObject.activeInHierarchy)
            {
                result[resultIndex] = gunSettings[i];
                resultIndex++;
            }
        }

        return result;
    }

    private Vector3 AngleToDirection(float angle)
    {
        float rad = angle * Mathf.Deg2Rad;

        return new Vector3(
            Mathf.Cos(rad),
            0f,
            Mathf.Sin(rad)
        ).normalized;
    }

    [ContextMenu("‰Šúİ’è‚ğì¬")]
    public void CreateDefaultSettings()
    {
        phase1Setting = new PhaseAttackSetting();
        phase1Setting.attackOrder = new AttackKind[]
        {
            AttackKind.‹ó”š,
            AttackKind.’Ç”öƒ~ƒTƒCƒ‹,
            AttackKind.‰ñ“]’e–‹,
            AttackKind.”š’eUŒ‚
        };
        phase1Setting.bombShotCount = 10;
        phase1Setting.bombShotInterval = 0.25f;
        phase1Setting.bombShootPower = 12.0f;
        phase1Setting.bombUpwardPower = 0.15f;
        phase1Setting.bombSpinSpeed = 180.0f;
        phase1Setting.dudBombMixRate = 20.0f;

        phase2Setting = new PhaseAttackSetting();
        phase2Setting.attackOrder = new AttackKind[]
        {
            AttackKind.ˆÚ“®,
            AttackKind.’Ç”öƒ~ƒTƒCƒ‹,
            AttackKind.‰ñ“]’e–‹,
            AttackKind.”š’eUŒ‚
        };
        phase2Setting.bombShotCount = 12;
        phase2Setting.bombShotInterval = 0.22f;
        phase2Setting.bombShootPower = 13.0f;
        phase2Setting.bombUpwardPower = 0.15f;
        phase2Setting.bombSpinSpeed = 220.0f;
        phase2Setting.dudBombMixRate = 25.0f;
        phase2Setting.missileCount = 5;
        phase2Setting.bulletHellFireInterval = 0.1f;

        phase3Setting = new PhaseAttackSetting();
        phase3Setting.attackOrder = new AttackKind[]
        {
            AttackKind.ˆÚ“®‚µ‚È‚ª‚ç‹ó”š,
            AttackKind.’Ç”öƒ~ƒTƒCƒ‹,
            AttackKind.‰ñ“]’e–‹,
            AttackKind.”š’eUŒ‚
        };
        phase3Setting.bombShotCount = 15;
        phase3Setting.bombShotInterval = 0.18f;
        phase3Setting.bombShootPower = 14.0f;
        phase3Setting.bombUpwardPower = 0.15f;
        phase3Setting.bombSpinSpeed = 260.0f;
        phase3Setting.dudBombMixRate = 30.0f;
        phase3Setting.missileCount = 6;
        phase3Setting.bulletHellFireInterval = 0.08f;
        phase3Setting.bulletHellRotateSpeed = 180.0f;
        phase3Setting.airStrikeCount = 12;
    }
}