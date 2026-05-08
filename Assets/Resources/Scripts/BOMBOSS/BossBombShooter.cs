/*
 * ==========================================================
 * 制作責任者：小林大悟
 *
 * ボスの砲台設定と攻撃ノード設定を管理するメインスクリプトです。
 *
 * このファイルには、
 * ・Inspectorに表示する設定
 * ・攻撃ノードのループ処理
 * ・初期サンプル作成
 * ・共通で使う補助関数
 * だけを残しています。
 *
 * 攻撃ごとの細かい処理は、別ファイルに分割しています。
 *
 * 【分割ファイル】
 * ・BossBombShooter_RotateAttack.cs
 * ・BossBombShooter_Bomb.cs
 * ・BossBombShooter_AirStrike.cs
 * ・BossBombShooter_MissileAttack.cs
 * ・BossBombShooter_BulletHellAttack.cs
 * ・BossBombShooter_Gizmos.cs
 *
 * ※BOSSにアタッチするのは、この BossBombShooter.cs だけです。
 * ※他の分割ファイルはアタッチしません。
 * ==========================================================
 */

using System.Collections;
using UnityEngine;

public partial class BossBombShooter : MonoBehaviour
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
        攻撃1_ランダム回転して止まってから発射,
        攻撃2_回転しながら発射,
        攻撃3_ランダム空爆,
        攻撃4_十字空爆,
        攻撃5_追尾ミサイル,
        攻撃6_回転弾幕
    }

    public enum RotateDirection
    {
        時計回り,
        反時計回り
    }

    [System.Serializable]
    public class GunSetting
    {
        [Header("【弾を出すGUN本体】")]
        [Tooltip("弾を出す位置と向きになるTransformです")]
        public Transform gun;

        [Header("【弾を出す方向】")]
        [Tooltip("GUNのどの方向へ弾を飛ばすかです")]
        public ShootAxis shootAxis = ShootAxis.Up;

        [Header("【GUN中心から砲身先端までの距離】")]
        [Tooltip("GUNの中心から、弾をどれくらい前に出して生成するかです")]
        public float muzzleOffset = 1.2f;

        [Header("【この砲台の基本弾速】")]
        [Tooltip("この砲台の基本弾速です。攻撃ノード側で上書きできます")]
        public float bombSpeed = 10.0f;

        [Header("【少し上へ飛ばす力】")]
        [Tooltip("弾を少し山なりに飛ばしたい場合に使います")]
        public float upwardPower = 0.0f;

        [Header("【この砲台を使う】")]
        [Tooltip("OFFにすると、この砲台からは発射しません")]
        public bool useThisGun = true;
    }

    [System.Serializable]
    public class AttackNode
    {
        [Header("【ノード基本設定】")]
        [Tooltip("この攻撃ノードの名前です")]
        public string nodeName = "攻撃ノード";

        [Tooltip("このノードで使う攻撃を選びます")]
        public AttackKind attackKind = AttackKind.攻撃1_ランダム回転して止まってから発射;

        [Tooltip("OFFにすると、この攻撃ノードは飛ばされます")]
        public bool useThisNode = true;

        [Tooltip("この攻撃が始まる前に待つ時間です")]
        public float waitBeforeAttack = 1.0f;

        [Tooltip("この攻撃が終わった後に待つ時間です")]
        public float waitAfterAttack = 2.5f;

        [Header("【使用する砲台番号】")]
        [Tooltip("特定の砲台だけ使いたい場合に指定します。空なら全部の砲台を使います")]
        public int[] useGunIndexes;

        [Header("【Prefab上書き設定】")]
        [Tooltip("この攻撃だけ別の爆弾Prefabを使いたい場合に設定します")]
        public GameObject bombPrefabOverride;

        [Tooltip("この攻撃だけ別の不発弾Prefabを使いたい場合に設定します")]
        public GameObject dudBombPrefabOverride;

        [Header("【弾の共通設定】")]
        [Tooltip("この攻撃で出す爆弾の大きさです")]
        public float bombScale = 1.0f;

        [Tooltip("この攻撃で出す爆弾の速度です。0以下なら砲台側の速度を使います")]
        public float bombSpeed = 0.0f;

        [Tooltip("この攻撃だけ砲口距離を変えたい場合に使います")]
        public float muzzleOffsetOverride = -1.0f;

        [Tooltip("この攻撃だけ上方向の力を変えたい場合に使います")]
        public float upwardPower = 0.0f;

        [Header("【爆弾の物理設定】")]
        [Tooltip("爆弾のLinear Damping最小値です")]
        public float minLinearDamping = 0.05f;

        [Tooltip("爆弾のLinear Damping最大値です")]
        public float maxLinearDamping = 0.2f;

        [Header("【爆発設定】")]
        [Tooltip("爆弾が爆発するまでの時間です")]
        public float explosionTime = 7.0f;

        [Tooltip("爆発の当たり判定範囲です")]
        public float explosionRadius = 3.0f;

        [Tooltip("プレイヤーに与えるダメージです")]
        public int damage = 20;

        [Tooltip("爆発エフェクトの見た目の大きさ倍率です")]
        public float explosionEffectScaleMultiplier = 1.0f;

        [Tooltip("ONにすると、爆発前に爆弾を点滅させます")]
        public bool useBlinkBeforeExplosion = true;

        [Tooltip("爆発する何秒前から点滅を始めるかです")]
        public float blinkBeforeExplosionTime = 3.0f;

        [Tooltip("点滅の間隔です")]
        public float blinkInterval = 0.15f;

        [Tooltip("点滅時の色です")]
        public Color blinkColor = Color.red;

        [Header("【不発弾設定】")]
        [Tooltip("ONにすると、この攻撃で不発弾が出る可能性があります")]
        public bool useDudBomb = false;

        [Tooltip("不発弾が出る確率です")]
        [Range(0f, 100f)]
        public float dudChance = 25f;

        [Tooltip("ONにすると、複数砲台から撃つ場合でも不発弾は最大1個になります")]
        public bool dudOnlyOne = true;

        [Header("【攻撃1：ランダム回転して止まってから発射】")]
        [Tooltip("ランダム角度へ回転する時間です")]
        public float randomRotateTime = 1.0f;

        [Tooltip("この中からランダムに選ばれて回転します")]
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

        [Header("【攻撃2：回転しながら発射】")]
        [Tooltip("回転しながら弾を撃つ時間です")]
        public float spinAttackTime = 4.0f;

        [Tooltip("回転しながら攻撃するときの回転速度です")]
        public float spinRotateSpeed = 180.0f;

        [Tooltip("時計回りか反時計回りを選びます")]
        public RotateDirection spinRotateDirection = RotateDirection.時計回り;

        [Tooltip("回転攻撃中に何秒ごとに弾を撃つかです")]
        public float spinFireInterval = 0.6f;

        [Header("【攻撃3：ランダム空爆】")]
        [Tooltip("この攻撃だけ空爆中心位置を変えたい場合に設定します")]
        public Transform airStrikeCenterOverride;

        [Tooltip("ランダム空爆で落とす爆弾の数です")]
        public int airStrikeBombCount = 8;

        [Tooltip("空爆弾を何秒ごとに落とすかです")]
        public float airStrikeInterval = 0.25f;

        [Tooltip("空爆弾をどの高さから落とすかです")]
        public float airStrikeHeight = 15.0f;

        [Tooltip("空爆弾の落下速度です")]
        public float airStrikeFallSpeed = 12.0f;

        [Tooltip("ランダム空爆で中心からどれくらい離すかの最小値です")]
        public float airStrikeMinDistance = 8.0f;

        [Tooltip("ランダム空爆で中心からどれくらい離すかの最大値です")]
        public float airStrikeMaxDistance = 20.0f;

        [Header("【攻撃4：十字空爆】")]
        [Tooltip("十字空爆で使う中心位置です")]
        public Transform crossAirStrikeCenterOverride;

        [Tooltip("十字方向に出す爆弾の数です")]
        public int crossAirStrikeBombCount = 4;

        [Tooltip("十字方向に爆弾を出す距離です")]
        public float crossAirStrikeDistance = 8.0f;

        [Tooltip("十字方向を回転させたい場合に使います")]
        public float crossAirStrikeAngleOffset = 0.0f;

        [Tooltip("十字空爆で爆弾を何秒ごとに出すかです")]
        public float crossAirStrikeInterval = 0.25f;

        [Tooltip("十字空爆の高さです")]
        public float crossAirStrikeHeight = 15.0f;

        [Tooltip("十字空爆の落下速度です")]
        public float crossAirStrikeFallSpeed = 12.0f;

        [Header("【攻撃5：追尾ミサイル】")]
        [Tooltip("ミサイルが向かっていく対象です。未設定ならPlayerタグを探します")]
        public Transform missileTargetOverride;

        [Tooltip("追尾ミサイルを何発撃つかです")]
        public int missileCount = 4;

        [Tooltip("ミサイルを何秒ごとに撃つかです")]
        public float missileFireInterval = 0.4f;

        [Tooltip("ミサイルの大きさです")]
        public float missileScale = 1.0f;

        [Tooltip("ミサイルの速度です")]
        public float missileSpeed = 12.0f;

        [Tooltip("ミサイルが曲がる速さです")]
        public float missileRotateSpeed = 180.0f;

        [Tooltip("ONにするとプレイヤーを追尾します")]
        public bool missileHoming = true;

        [Tooltip("ONにするとミサイルに重力を使います")]
        public bool missileUseGravity = false;

        [Tooltip("ミサイルが爆発するまでの時間です")]
        public float missileExplosionTime = 5.0f;

        [Tooltip("ONにすると、ぶつかった時に爆発します")]
        public bool missileExplodeOnHit = true;

        [Tooltip("ONにすると、プレイヤーにぶつかった時だけ爆発します")]
        public bool missileExplodeOnlyPlayerHit = false;

        [Tooltip("ミサイルの爆発範囲です")]
        public float missileExplosionRadius = 3.0f;

        [Tooltip("ミサイルのダメージです")]
        public int missileDamage = 20;

        [Tooltip("ミサイルの爆発エフェクトです")]
        public GameObject missileExplosionEffectPrefab;

        [Tooltip("ミサイル爆発エフェクトのサイズ倍率です")]
        public float missileExplosionEffectScaleMultiplier = 1.0f;

        [Header("【攻撃6：回転弾幕】")]
        [Tooltip("弾幕用の弾Prefabです。BulletHellBullet.csを付けたPrefabを入れてください")]
        public GameObject bulletHellBulletPrefab;

        [Tooltip("弾幕を出す砲台です。ここに入れた砲台だけから弾が出ます。空なら通常の砲台設定を使います")]
        public Transform[] bulletHellFireGuns;

        [Tooltip("弾幕用の発射方向です")]
        public ShootAxis bulletHellShootAxis = ShootAxis.Up;

        [Tooltip("砲台からどれくらい前に弾を出すかです")]
        public float bulletHellMuzzleOffset = 1.2f;

        [Tooltip("弾の発射位置を上下にずらします。マイナスで下、プラスで上です")]
        public float bulletHellSpawnHeightOffset = 0.0f;

        [Tooltip("ONにすると、弾の進行方向の上下成分を消して水平に飛ばします")]
        public bool bulletHellForceHorizontalDirection = true;

        [Tooltip("弾の進行方向を上下に少し補正します。マイナスで下向き、プラスで上向きです")]
        public float bulletHellDirectionHeightOffset = 0.0f;

        [Tooltip("弾幕攻撃を続ける時間です")]
        public float bulletHellAttackTime = 5.0f;

        [Tooltip("弾幕攻撃中の本体回転速度です")]
        public float bulletHellRotateSpeed = 180.0f;

        [Tooltip("本体を回転させる方向です")]
        public RotateDirection bulletHellRotateDirection = RotateDirection.時計回り;

        [Tooltip("何秒ごとに弾を発射するかです")]
        public float bulletHellFireInterval = 0.05f;

        [Tooltip("発射回数です。0以下なら攻撃時間が終わるまで撃ち続けます")]
        public int bulletHellShotCount = 0;

        [Tooltip("弾の速度です")]
        public float bulletHellBulletSpeed = 12.0f;

        [Tooltip("弾の大きさです")]
        public float bulletHellBulletScale = 1.0f;

        [Tooltip("弾が消え始めるまでの時間です")]
        public float bulletHellBulletLifeTime = 3.0f;

        [Tooltip("弾が小さくなりながら消える時間です")]
        public float bulletHellBulletShrinkTime = 0.5f;

        [Tooltip("弾がプレイヤーに与えるダメージです")]
        public int bulletHellDamage = 10;

        [Tooltip("ONにするとプレイヤーに当たった時に弾が消えます")]
        public bool bulletHellDestroyOnPlayerHit = true;

        [Tooltip("ONにすると、弾を撃つ前に砲台が点滅します")]
        public bool bulletHellBlinkBeforeFire = true;

        [Tooltip("弾幕開始前に何秒点滅するかです")]
        public float bulletHellBlinkTime = 2.0f;

        [Tooltip("砲台の点滅間隔です")]
        public float bulletHellBlinkInterval = 0.15f;

        [Tooltip("砲台の点滅色です")]
        public Color bulletHellBlinkColor = Color.red;
    }

    [Header("【通常爆弾Prefab】")]
    [Tooltip("基本で使う爆弾Prefabです")]
    public GameObject bombPrefab;

    [Header("【不発弾Prefab】")]
    [Tooltip("基本で使う不発弾Prefabです")]
    public GameObject dudBombPrefab;

    [Header("【追尾ミサイルPrefab】")]
    [Tooltip("攻撃5で使うミサイルPrefabです。Missile.csを付けたPrefabを入れてください")]
    public GameObject missilePrefab;

    [Header("【回転させるボス本体】")]
    [Tooltip("攻撃1、攻撃2、攻撃6で回転させるボス本体です")]
    public Transform rotateRoot;

    [Header("【空爆中心位置】")]
    [Tooltip("空爆の中心位置です。未設定ならこのオブジェクトの位置を使います")]
    public Transform airStrikeCenter;

    [Header("【ゲーム開始時に攻撃開始】")]
    [Tooltip("ONにするとゲーム開始時に自動で攻撃を始めます")]
    public bool playOnStart = true;

    [Header("【最後まで行ったら最初に戻る】")]
    [Tooltip("ONにすると攻撃ノードの最後まで行ったあと、最初に戻ります")]
    public bool loopAttackNodes = true;

    [Header("【攻撃開始までの待ち時間】")]
    [Tooltip("ゲーム開始後、何秒待ってから攻撃を始めるかです")]
    public float startWaitTime = 1.0f;

    [Header("【砲台設定】")]
    [Tooltip("ここは敵の動きではなく、GUNだけを登録する場所です")]
    public GunSetting[] gunSettings;

    [Header("【攻撃ノード一覧】")]
    [Tooltip("ここで敵の攻撃順を作ります")]
    public AttackNode[] attackNodes;

    private Quaternion baseRotation;
    private int currentAttackNodeIndex = 0;
    private bool isAttacking = false;

    void Reset()
    {
        CreateDefaultInspectorData();
    }

    [ContextMenu("初期サンプルを作成")]
    public void CreateDefaultInspectorData()
    {
        if (gunSettings == null || gunSettings.Length == 0)
        {
            gunSettings = new GunSetting[4];

            for (int i = 0; i < gunSettings.Length; i++)
            {
                gunSettings[i] = new GunSetting();
                gunSettings[i].shootAxis = ShootAxis.Up;
                gunSettings[i].muzzleOffset = 1.2f;
                gunSettings[i].bombSpeed = 10.0f;
                gunSettings[i].upwardPower = 0.0f;
                gunSettings[i].useThisGun = true;
            }
        }

        attackNodes = new AttackNode[8];

        attackNodes[0] = CreateAttack1Node("ノード0：攻撃1 ランダム回転発射");
        attackNodes[1] = CreateAttack2Node("ノード1：攻撃2 回転しながら発射");
        attackNodes[2] = CreateAttack3Node("ノード2：攻撃3 ランダム空爆");
        attackNodes[3] = CreateAttack2Node("ノード3：攻撃2 回転しながら発射");
        attackNodes[4] = CreateAttack1Node("ノード4：攻撃1 ランダム回転発射");
        attackNodes[5] = CreateAttack4Node("ノード5：攻撃4 十字空爆");
        attackNodes[6] = CreateAttack5Node("ノード6：攻撃5 追尾ミサイル");
        attackNodes[7] = CreateAttack6Node("ノード7：攻撃6 回転弾幕");
    }

    AttackNode CreateAttack1Node(string name)
    {
        AttackNode node = new AttackNode();

        node.nodeName = name;
        node.attackKind = AttackKind.攻撃1_ランダム回転して止まってから発射;
        node.waitBeforeAttack = 1.0f;
        node.waitAfterAttack = 2.5f;

        node.bombScale = 2.0f;
        node.bombSpeed = 10.0f;
        node.explosionEffectScaleMultiplier = 2.0f;

        node.useBlinkBeforeExplosion = true;
        node.blinkBeforeExplosionTime = 3.0f;
        node.blinkInterval = 0.15f;
        node.blinkColor = Color.red;

        node.useDudBomb = false;
        node.randomRotateTime = 1.0f;

        return node;
    }

    AttackNode CreateAttack2Node(string name)
    {
        AttackNode node = new AttackNode();

        node.nodeName = name;
        node.attackKind = AttackKind.攻撃2_回転しながら発射;
        node.waitBeforeAttack = 1.0f;
        node.waitAfterAttack = 2.5f;

        node.bombScale = 1.0f;
        node.bombSpeed = 8.0f;

        node.useBlinkBeforeExplosion = true;
        node.blinkBeforeExplosionTime = 3.0f;
        node.blinkInterval = 0.15f;
        node.blinkColor = Color.red;

        node.spinAttackTime = 4.0f;
        node.spinRotateSpeed = 180.0f;
        node.spinRotateDirection = RotateDirection.時計回り;
        node.spinFireInterval = 0.6f;

        node.useDudBomb = true;
        node.dudChance = 25.0f;
        node.dudOnlyOne = true;

        return node;
    }

    AttackNode CreateAttack3Node(string name)
    {
        AttackNode node = new AttackNode();

        node.nodeName = name;
        node.attackKind = AttackKind.攻撃3_ランダム空爆;
        node.waitBeforeAttack = 1.0f;
        node.waitAfterAttack = 2.5f;

        node.useBlinkBeforeExplosion = true;
        node.blinkBeforeExplosionTime = 3.0f;
        node.blinkInterval = 0.15f;
        node.blinkColor = Color.red;

        node.airStrikeBombCount = 8;
        node.airStrikeInterval = 0.25f;
        node.airStrikeHeight = 15.0f;
        node.airStrikeFallSpeed = 12.0f;
        node.airStrikeMinDistance = 8.0f;
        node.airStrikeMaxDistance = 20.0f;

        node.useDudBomb = false;

        return node;
    }

    AttackNode CreateAttack4Node(string name)
    {
        AttackNode node = new AttackNode();

        node.nodeName = name;
        node.attackKind = AttackKind.攻撃4_十字空爆;
        node.waitBeforeAttack = 1.0f;
        node.waitAfterAttack = 2.5f;

        node.useBlinkBeforeExplosion = true;
        node.blinkBeforeExplosionTime = 3.0f;
        node.blinkInterval = 0.15f;
        node.blinkColor = Color.red;

        node.crossAirStrikeBombCount = 4;
        node.crossAirStrikeDistance = 8.0f;
        node.crossAirStrikeAngleOffset = 0.0f;
        node.crossAirStrikeInterval = 0.25f;
        node.crossAirStrikeHeight = 15.0f;
        node.crossAirStrikeFallSpeed = 12.0f;

        node.useDudBomb = true;
        node.dudChance = 25.0f;
        node.dudOnlyOne = true;

        return node;
    }

    AttackNode CreateAttack5Node(string name)
    {
        AttackNode node = new AttackNode();

        node.nodeName = name;
        node.attackKind = AttackKind.攻撃5_追尾ミサイル;
        node.waitBeforeAttack = 1.0f;
        node.waitAfterAttack = 2.5f;

        node.missileCount = 4;
        node.missileFireInterval = 0.4f;
        node.missileScale = 1.0f;
        node.missileSpeed = 12.0f;
        node.missileRotateSpeed = 180.0f;
        node.missileHoming = true;
        node.missileUseGravity = false;
        node.missileExplosionTime = 5.0f;
        node.missileExplodeOnHit = true;
        node.missileExplodeOnlyPlayerHit = false;
        node.missileExplosionRadius = 3.0f;
        node.missileDamage = 20;
        node.missileExplosionEffectScaleMultiplier = 1.0f;

        return node;
    }

    AttackNode CreateAttack6Node(string name)
    {
        AttackNode node = new AttackNode();

        node.nodeName = name;
        node.attackKind = AttackKind.攻撃6_回転弾幕;
        node.waitBeforeAttack = 1.0f;
        node.waitAfterAttack = 2.5f;

        node.bulletHellShootAxis = ShootAxis.Up;
        node.bulletHellMuzzleOffset = 1.2f;
        node.bulletHellSpawnHeightOffset = -1.0f;
        node.bulletHellForceHorizontalDirection = true;
        node.bulletHellDirectionHeightOffset = 0.0f;

        node.bulletHellAttackTime = 5.0f;
        node.bulletHellRotateSpeed = 180.0f;
        node.bulletHellRotateDirection = RotateDirection.時計回り;
        node.bulletHellFireInterval = 0.05f;
        node.bulletHellShotCount = 0;

        node.bulletHellBulletSpeed = 12.0f;
        node.bulletHellBulletScale = 1.0f;
        node.bulletHellBulletLifeTime = 3.0f;
        node.bulletHellBulletShrinkTime = 0.5f;
        node.bulletHellDamage = 10;
        node.bulletHellDestroyOnPlayerHit = true;

        node.bulletHellBlinkBeforeFire = true;
        node.bulletHellBlinkTime = 2.0f;
        node.bulletHellBlinkInterval = 0.15f;
        node.bulletHellBlinkColor = Color.red;

        return node;
    }

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

        if (playOnStart)
        {
            StartCoroutine(AttackLoop());
        }
    }

    IEnumerator AttackLoop()
    {
        yield return new WaitForSeconds(startWaitTime);

        while (true)
        {
            if (attackNodes == null || attackNodes.Length == 0)
            {
                Debug.LogWarning("攻撃ノード一覧が空です。Inspectorのメニューから初期サンプルを作成してください");
                yield return null;
                continue;
            }

            if (currentAttackNodeIndex >= attackNodes.Length)
            {
                if (loopAttackNodes)
                {
                    currentAttackNodeIndex = 0;
                }
                else
                {
                    yield break;
                }
            }

            AttackNode node = attackNodes[currentAttackNodeIndex];

            if (node == null || !node.useThisNode)
            {
                currentAttackNodeIndex++;
                continue;
            }

            isAttacking = true;

            Debug.Log("攻撃開始：" + node.nodeName);

            yield return new WaitForSeconds(node.waitBeforeAttack);

            if (node.attackKind == AttackKind.攻撃1_ランダム回転して止まってから発射)
            {
                yield return StartCoroutine(Attack1_RandomRotateShoot(node));
            }
            else if (node.attackKind == AttackKind.攻撃2_回転しながら発射)
            {
                yield return StartCoroutine(Attack2_SpinShoot(node));
            }
            else if (node.attackKind == AttackKind.攻撃3_ランダム空爆)
            {
                yield return StartCoroutine(Attack3_RandomAirStrike(node));
            }
            else if (node.attackKind == AttackKind.攻撃4_十字空爆)
            {
                yield return StartCoroutine(Attack4_CrossAirStrike(node));
            }
            else if (node.attackKind == AttackKind.攻撃5_追尾ミサイル)
            {
                yield return StartCoroutine(Attack5_HomingMissile(node));
            }
            else if (node.attackKind == AttackKind.攻撃6_回転弾幕)
            {
                yield return StartCoroutine(Attack6_BulletHell(node));
            }

            yield return new WaitForSeconds(node.waitAfterAttack);

            isAttacking = false;
            currentAttackNodeIndex++;
        }
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

    Vector3 AngleToDirection(float angle)
    {
        float rad = angle * Mathf.Deg2Rad;

        return new Vector3(
            Mathf.Cos(rad),
            0f,
            Mathf.Sin(rad)
        ).normalized;
    }
}