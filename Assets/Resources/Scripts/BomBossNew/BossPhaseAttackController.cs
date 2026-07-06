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
        [InspectorName("爆弾攻撃")]
        BombAttack,

        [InspectorName("空爆")]
        AirStrike,

        [InspectorName("追尾ミサイル")]
        HomingMissile,

        [InspectorName("回転弾幕")]
        BulletHell,

        [InspectorName("移動")]
        Move,

        [InspectorName("移動しながら空爆")]
        MoveAndAirStrike
    }

    public enum RotateDirection
    {
        [InspectorName("時計回り")]
        Clockwise,

        [InspectorName("反時計回り")]
        CounterClockwise
    }

    [System.Serializable]
    public class GunSetting
    {
        [Header("砲台Transform")]
        public Transform gun;

        [Header("発射方向")]
        public ShootAxis shootAxis = ShootAxis.Forward;

        [Header("砲口から少し前に出す距離")]
        public float muzzleOffset = 0.8f;

        [Header("この砲台を使う")]
        public bool useThisGun = true;
    }

    [System.Serializable]
    public class LaserDeviceSetting
    {
        [Header("このレーザー装置を使う")]
        public bool useThisDevice = true;

        [Header("動かす先端部分（Back_3）")]
        [Tooltip("レーザー発射時に出し、通常時に収納するBack_3を設定します")]
        public Transform tipPart;

        [Header("先端部分の収納位置補正")]
        [Tooltip(
            "Scene上の展開位置から、収納時にどれだけ移動させるかをローカル座標で指定します"
        )]
        public Vector3 tipRetractedLocalOffset = Vector3.zero;

        [System.NonSerialized]
        public Vector3 tipDeployedLocalPosition;

        [System.NonSerialized]
        public bool initialized;
    }

    [System.Serializable]
    public class PhaseAttackSetting
    {
        [Header("この段階の攻撃を使う")]
        public bool useThisPhase = true;

        [Header("攻撃順")]
        public AttackKind[] attackOrder;

        [Header("攻撃前の待ち時間")]
        public float waitBeforeAttack = 1.0f;

        [Header("攻撃後の待ち時間")]
        public float waitAfterAttack = 1.5f;

        [Header("爆弾攻撃")]
        public int bombShotCount = 10;
        public float bombShotInterval = 0.25f;
        public float bombShootPower = 12.0f;
        public float bombUpwardPower = 0.15f;
        public float bombSpinSpeed = 180.0f;
        public bool bombShootAllGuns = false;

        [Header("不発弾混入")]
        [Range(0f, 100f)]
        public float dudBombMixRate = 20.0f;
        public float dudShootPower = 6.0f;
        public float dudUpwardPower = 1.5f;

        [Header("空爆")]
        public int airStrikeCount = 8;
        public float airStrikeInterval = 0.25f;
        public float airStrikeHeight = 15.0f;
        public float airStrikeFallSpeed = 12.0f;
        public float airStrikeMinDistance = 5.0f;
        public float airStrikeMaxDistance = 15.0f;

        [Header("追尾ミサイル")]
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

        [Header("回転弾幕")]
        public float bulletHellTime = 4.0f;
        public float bulletHellRotateSpeed = 120.0f;
        public RotateDirection bulletHellRotateDirection =
            RotateDirection.Clockwise;
        public float bulletHellFireInterval = 0.12f;
        public float bulletHellBulletSpeed = 10.0f;
        public float bulletHellBulletScale = 1.0f;
        public float bulletHellBulletLifeTime = 3.0f;
        public float bulletHellBulletShrinkTime = 0.5f;
        public int bulletHellDamage = 10;
        public bool bulletHellDestroyOnPlayerHit = true;

        [Header("移動")]
        public float moveSpeed = 3.0f;
        public float moveStopDistance = 0.1f;
    }

    [Header("段階管理")]
    [SerializeField]
    private BossPhaseController phaseController;

    [Header("通常爆弾Prefab")]
    [SerializeField]
    private GameObject bombPrefab;

    [Header("不発弾Prefab")]
    [SerializeField]
    private GameObject dudBombPrefab;

    [Header("ミサイルPrefab")]
    [SerializeField]
    private GameObject missilePrefab;

    [Header("弾幕弾Prefab")]
    [SerializeField]
    private GameObject bulletHellBulletPrefab;

    [Header("ミサイル爆発エフェクト")]
    [SerializeField]
    private GameObject missileExplosionEffectPrefab;

    [Header("回転させる本体")]
    [SerializeField]
    private Transform rotateRoot;

    [Header("3段階目の回転中心")]
    [SerializeField]
    private Transform phase3RotateCenter;

    [Header("3段階目は必ず回転中心を使う")]
    [SerializeField]
    private bool forcePhase3RotateAroundCenter = true;

    [Header("空爆中心")]
    [SerializeField]
    private Transform airStrikeCenter;

    [Header("プレイヤー")]
    [SerializeField]
    private Transform player;

    [Header("砲台設定")]
    [SerializeField]
    private GunSetting[] gunSettings;

    [Header("1段階あたりの砲台数")]
    [SerializeField]
    private int gunsPerPhase = 4;

    [Header("レーザー装置設定")]
    [Tooltip(
        "Gun Settingsと同じ順番で設定してください。0～3が1段階目、4～7が2段階目、8～11が3段階目です"
    )]
    [SerializeField]
    private LaserDeviceSetting[] laserDeviceSettings;

    [Header("レーザー装置を展開する攻撃")]
    [Tooltip("現在は回転弾幕をレーザー攻撃として扱います")]
    [SerializeField]
    private AttackKind laserDeviceAttackKind = AttackKind.BulletHell;

    [Header("レーザー先端の移動時間")]
    [Min(0.01f)]
    [SerializeField]
    private float laserTipMoveTime = 0.25f;

    [Header("ゲーム開始時にレーザー先端を収納")]
    [SerializeField]
    private bool retractLaserDevicesOnStart = true;

    [Header("移動地点")]
    [SerializeField]
    private Transform[] movePoints;

    [Header("1段階目の攻撃設定")]
    [SerializeField]
    private PhaseAttackSetting phase1Setting;

    [Header("2段階目の攻撃設定")]
    [SerializeField]
    private PhaseAttackSetting phase2Setting;

    [Header("3段階目の攻撃設定")]
    [SerializeField]
    private PhaseAttackSetting phase3Setting;

    [Header("ゲーム開始時に攻撃開始")]
    [SerializeField]
    private bool playOnStart = true;

    [Header("攻撃開始までの待ち時間")]
    [SerializeField]
    private float startDelay = 0.0f;

    [Header("スピン攻撃")]
    [SerializeField]
    private bool useSpinAttack = true;

    [SerializeField]
    private float spinDetectRange = 4.0f;

    [SerializeField]
    private float spinAttackTime = 1.2f;

    [SerializeField]
    private float spinRotateSpeed = 720.0f;

    [SerializeField]
    private float spinCooldown = 4.0f;

    [SerializeField]
    private float spinKnockbackPower = 8.0f;

    [SerializeField]
    private float spinKnockbackUpPower = 2.0f;

    [Header("デバッグ")]
    [SerializeField]
    private bool showDebugLog = true;

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

    private void OnValidate()
    {
        SetAllLaserDeviceUsageEnabled();
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
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        SetAllLaserDeviceUsageEnabled();
    }
    public void StartBattle()
    {
        if (isMainAttackRunning)
            return;
        Debug.Log("あかかかかかかかかあかあっかあっかか");

        StartCoroutine(MainAttackLoop());
    }

    private void Start()
    {
        InitializeLaserDevices();

        if (retractLaserDevicesOnStart)
        {
            SetAllLaserDevicesImmediate(false);
        }

        //if (playOnStart)
        //{
        //    StartCoroutine(MainAttackLoop());
        //}
    }

    private void Update()
    {
        UpdateSpinAttack();
    }

    private void SetAllLaserDeviceUsageEnabled()
    {
        if (laserDeviceSettings == null)
        {
            return;
        }

        for (int i = 0; i < laserDeviceSettings.Length; i++)
        {
            LaserDeviceSetting device = laserDeviceSettings[i];

            if (device == null)
            {
                continue;
            }

            device.useThisDevice = true;
        }
    }

    private IEnumerator MainAttackLoop()
    {
        //競合回避のためむりやりここで秒数変更してます(せいら
        startDelay = 2.0f;
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
                AttackKind specialAttack =
                    GetAfterAllWallsAttackKind();

                if (specialAttack == AttackKind.AirStrike)
                {
                    if (showDebugLog)
                    {
                        Debug.Log("壁全破壊後攻撃: 空爆");
                    }

                    yield return StartCoroutine(
                        Attack_AirStrike(setting)
                    );
                }
                else
                {
                    if (showDebugLog)
                    {
                        Debug.Log("壁全破壊後攻撃: スピン");
                    }

                    yield return StartCoroutine(
                        SpinAttackRoutine()
                    );
                }

                AdvanceAfterAllWallsAttackIndex();

                yield return new WaitForSeconds(
                    setting.waitAfterAttack
                );

                continue;
            }

            if (setting.attackOrder == null ||
                setting.attackOrder.Length == 0)
            {
                yield return null;
                continue;
            }

            if (currentAttackIndex >= setting.attackOrder.Length)
            {
                currentAttackIndex = 0;
            }

            AttackKind attack =
                setting.attackOrder[currentAttackIndex];

            yield return StartCoroutine(
                ExecuteAttack(attack, setting)
            );

            yield return new WaitForSeconds(
                setting.waitAfterAttack
            );

            currentAttackIndex++;
        }
    }

    private IEnumerator ExecuteAttack(
        AttackKind attack,
        PhaseAttackSetting setting
    )
    {
        if (ShouldUseAfterAllWallsAttackPattern())
        {
            attack = AttackKind.AirStrike;
        }

        if (showDebugLog)
        {
            Debug.Log(
                "ボス攻撃開始: " +
                attack +
                " / Phase " +
                GetCurrentPhase()
            );
        }

        bool usesLaserDevice =
            attack == laserDeviceAttackKind;

        int attackPhase = GetCurrentPhase();

        if (usesLaserDevice)
        {
            if (showDebugLog)
            {
                Debug.Log(
                    "レーザー先端を展開します / Phase " +
                    attackPhase
                );
            }

            yield return StartCoroutine(
                SetLaserDevicesForPhase(
                    attackPhase,
                    true
                )
            );
        }

        if (attack == AttackKind.BombAttack)
        {
            yield return StartCoroutine(
                Attack_DudBombShot(setting)
            );
        }
        else if (attack == AttackKind.AirStrike)
        {
            yield return StartCoroutine(
                Attack_AirStrike(setting)
            );
        }
        else if (attack == AttackKind.HomingMissile)
        {
            yield return StartCoroutine(
                Attack_HomingMissile(setting)
            );
        }
        else if (attack == AttackKind.BulletHell)
        {
            yield return StartCoroutine(
                Attack_BulletHell(setting)
            );
        }
        else if (attack == AttackKind.Move)
        {
            yield return StartCoroutine(
                Attack_Move(setting)
            );
        }
        else if (attack == AttackKind.MoveAndAirStrike)
        {
            yield return StartCoroutine(
                Attack_MoveAndAirStrike(setting)
            );
        }

        if (usesLaserDevice)
        {
            if (showDebugLog)
            {
                Debug.Log(
                    "レーザー先端を収納します / Phase " +
                    attackPhase
                );
            }

            yield return StartCoroutine(
                SetLaserDevicesForPhase(
                    attackPhase,
                    false
                )
            );
        }
    }

    private void InitializeLaserDevices()
    {
        if (laserDeviceSettings == null)
        {
            return;
        }

        for (int i = 0; i < laserDeviceSettings.Length; i++)
        {
            LaserDeviceSetting device =
                laserDeviceSettings[i];

            if (device == null)
            {
                continue;
            }

            device.useThisDevice = true;

            if (device.tipPart != null)
            {
                device.tipDeployedLocalPosition =
                    device.tipPart.localPosition;

                device.initialized = true;
            }
            else
            {
                device.initialized = false;
            }
        }
    }

    private void SetAllLaserDevicesImmediate(bool deploy)
    {
        if (laserDeviceSettings == null)
        {
            return;
        }

        for (int i = 0; i < laserDeviceSettings.Length; i++)
        {
            SetLaserDeviceImmediate(
                laserDeviceSettings[i],
                deploy
            );
        }
    }

    private void SetLaserDeviceImmediate(
        LaserDeviceSetting device,
        bool deploy
    )
    {
        if (!IsValidLaserDevice(device))
        {
            return;
        }

        if (deploy)
        {
            device.tipPart.localPosition =
                device.tipDeployedLocalPosition;
        }
        else
        {
            device.tipPart.localPosition =
                device.tipDeployedLocalPosition +
                device.tipRetractedLocalOffset;
        }
    }

    private IEnumerator SetLaserDevicesForPhase(
        int phase,
        bool deploy
    )
    {
        yield return StartCoroutine(
            MoveLaserTips(
                phase,
                deploy,
                laserTipMoveTime
            )
        );
    }

    private IEnumerator MoveLaserTips(
        int phase,
        bool deploy,
        float moveTime
    )
    {
        if (laserDeviceSettings == null ||
            laserDeviceSettings.Length == 0)
        {
            yield break;
        }

        int startIndex =
            GetPhaseLaserDeviceStartIndex(phase);

        int endIndex =
            GetPhaseLaserDeviceEndIndex(phase);

        if (startIndex >= laserDeviceSettings.Length)
        {
            yield break;
        }

        if (endIndex > laserDeviceSettings.Length)
        {
            endIndex = laserDeviceSettings.Length;
        }

        Vector3[] startPositions =
            new Vector3[laserDeviceSettings.Length];

        bool hasValidPart = false;

        for (int i = startIndex; i < endIndex; i++)
        {
            LaserDeviceSetting device =
                laserDeviceSettings[i];

            if (!IsValidLaserDevice(device))
            {
                continue;
            }

            startPositions[i] =
                device.tipPart.localPosition;

            hasValidPart = true;
        }

        if (!hasValidPart)
        {
            yield break;
        }

        if (moveTime <= 0f)
        {
            moveTime = 0.01f;
        }

        float elapsedTime = 0f;

        while (elapsedTime < moveTime)
        {
            elapsedTime += Time.deltaTime;

            float rate = Mathf.Clamp01(
                elapsedTime / moveTime
            );

            rate = Mathf.SmoothStep(
                0f,
                1f,
                rate
            );

            for (int i = startIndex; i < endIndex; i++)
            {
                LaserDeviceSetting device =
                    laserDeviceSettings[i];

                if (!IsValidLaserDevice(device))
                {
                    continue;
                }

                Vector3 targetPosition =
                    GetLaserTipTargetPosition(
                        device,
                        deploy
                    );

                device.tipPart.localPosition =
                    Vector3.Lerp(
                        startPositions[i],
                        targetPosition,
                        rate
                    );
            }

            yield return null;
        }

        for (int i = startIndex; i < endIndex; i++)
        {
            LaserDeviceSetting device =
                laserDeviceSettings[i];

            if (!IsValidLaserDevice(device))
            {
                continue;
            }

            device.tipPart.localPosition =
                GetLaserTipTargetPosition(
                    device,
                    deploy
                );
        }
    }

    private Vector3 GetLaserTipTargetPosition(
        LaserDeviceSetting device,
        bool deploy
    )
    {
        if (deploy)
        {
            return device.tipDeployedLocalPosition;
        }

        return device.tipDeployedLocalPosition +
               device.tipRetractedLocalOffset;
    }

    private bool IsValidLaserDevice(
        LaserDeviceSetting device
    )
    {
        return device != null &&
               device.useThisDevice &&
               device.initialized &&
               device.tipPart != null;
    }

    private int GetPhaseLaserDeviceStartIndex(int phase)
    {
        if (phase < 1)
        {
            phase = 1;
        }

        int devicesPerPhase = gunsPerPhase;

        if (devicesPerPhase <= 0)
        {
            devicesPerPhase = 1;
        }

        return (phase - 1) * devicesPerPhase;
    }

    private int GetPhaseLaserDeviceEndIndex(int phase)
    {
        int devicesPerPhase = gunsPerPhase;

        if (devicesPerPhase <= 0)
        {
            devicesPerPhase = 1;
        }

        return GetPhaseLaserDeviceStartIndex(phase) +
               devicesPerPhase;
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
        if (phaseController != null &&
            phaseController.Phase3CoreTransform != null)
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

        float rotateAmount =
            rotateSpeed * Time.deltaTime;

        if (forcePhase3RotateAroundCenter &&
            GetCurrentPhase() == 3)
        {
            Transform center =
                GetPhase3RotateCenter();

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
                Debug.LogWarning(
                    "3段階目の回転中心が設定されていません。" +
                    "通常回転に戻します。"
                );
            }
        }

        rotateRoot.Rotate(
            Vector3.up,
            rotateAmount,
            Space.World
        );
    }

    private void RotateBossForAttack(
        float rotateSpeed,
        RotateDirection rotateDirection
    )
    {
        float direction = 1f;

        if (rotateDirection ==
            RotateDirection.CounterClockwise)
        {
            direction = -1f;
        }

        RotateBossForAttack(
            rotateSpeed * direction
        );
    }

    private Vector3 GetShootDirection(
        Transform gun,
        ShootAxis axis
    )
    {
        if (gun == null)
        {
            return transform.forward;
        }

        if (axis == ShootAxis.Forward)
        {
            return gun.forward;
        }

        if (axis == ShootAxis.Back)
        {
            return -gun.forward;
        }

        if (axis == ShootAxis.Right)
        {
            return gun.right;
        }

        if (axis == ShootAxis.Left)
        {
            return -gun.right;
        }

        if (axis == ShootAxis.Up)
        {
            return gun.up;
        }

        if (axis == ShootAxis.Down)
        {
            return -gun.up;
        }

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
        return GetCurrentPhaseGunStartIndex() +
               gunsPerPhase;
    }

    private bool IsGunIndexInCurrentPhase(int index)
    {
        int startIndex =
            GetCurrentPhaseGunStartIndex();

        int endIndex =
            GetCurrentPhaseGunEndIndex();

        return index >= startIndex &&
               index < endIndex;
    }

    private GunSetting GetNextUsableGun()
    {
        GunSetting[] usableGuns =
            GetUsableGuns();

        if (usableGuns == null ||
            usableGuns.Length == 0)
        {
            return null;
        }

        if (nextGunIndexInPhase >=
            usableGuns.Length)
        {
            nextGunIndexInPhase = 0;
        }

        GunSetting gun =
            usableGuns[nextGunIndexInPhase];

        nextGunIndexInPhase++;

        if (nextGunIndexInPhase >=
            usableGuns.Length)
        {
            nextGunIndexInPhase = 0;
        }

        return gun;
    }

    private GunSetting[] GetUsableGuns()
    {
        if (gunSettings == null ||
            gunSettings.Length == 0)
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

        GunSetting[] result =
            new GunSetting[count];

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
                result[resultIndex] =
                    gunSettings[i];

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

    [ContextMenu("初期設定を作成")]
    public void CreateDefaultSettings()
    {
        phase1Setting = new PhaseAttackSetting();

        phase1Setting.attackOrder =
            new AttackKind[]
            {
                AttackKind.AirStrike,
                AttackKind.HomingMissile,
                AttackKind.BulletHell,
                AttackKind.BombAttack
            };

        phase1Setting.bombShotCount = 10;
        phase1Setting.bombShotInterval = 0.25f;
        phase1Setting.bombShootPower = 12.0f;
        phase1Setting.bombUpwardPower = 0.15f;
        phase1Setting.bombSpinSpeed = 180.0f;
        phase1Setting.dudBombMixRate = 20.0f;

        phase2Setting = new PhaseAttackSetting();

        phase2Setting.attackOrder =
            new AttackKind[]
            {
                AttackKind.Move,
                AttackKind.HomingMissile,
                AttackKind.BulletHell,
                AttackKind.BombAttack
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

        phase3Setting.attackOrder =
            new AttackKind[]
            {
                AttackKind.MoveAndAirStrike,
                AttackKind.HomingMissile,
                AttackKind.BulletHell,
                AttackKind.BombAttack
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