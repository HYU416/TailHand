using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class BossPhaseAttackController
{
    [Header("壁が全破壊された後は特殊攻撃パターンにする")]
    [SerializeField] private bool useSpecialAttackAfterAllWallsBroken = true;

    [Header("空爆後、撃った爆弾が全部消えるまで待つ")]
    [SerializeField] private bool waitUntilAirStrikeBombsDestroyed = true;

    [Header("空爆爆弾を待つ最大時間")]
    [SerializeField] private float maxAirStrikeBombWaitTime = 20.0f;

    [Header("空爆待機ログを出す")]
    [SerializeField] private bool showAirStrikeWaitLog = true;

    [Header("空爆待機ログの間隔")]
    [SerializeField] private float airStrikeWaitLogInterval = 1.0f;

    private bool allWallsBroken;
    private bool airStrikeDudBombAlreadySpawnedThisAttack;
    private int afterAllWallsAttackIndex;

    public void NotifyAllWallsBroken()
    {
        allWallsBroken = true;
        currentAttackIndex = 0;
        afterAllWallsAttackIndex = 0;

        if (showDebugLog)
        {
            Debug.Log("壁がすべて破壊されたため、空爆と移動の交互攻撃に変更します");
        }
    }

    public void ResetAllWallsBrokenState()
    {
        allWallsBroken = false;
        airStrikeDudBombAlreadySpawnedThisAttack = false;
        afterAllWallsAttackIndex = 0;

        if (showDebugLog)
        {
            Debug.Log("壁破壊状態をリセットしました");
        }
    }

    private bool ShouldUseAfterAllWallsAttackPattern()
    {
        return useSpecialAttackAfterAllWallsBroken && allWallsBroken;
    }

    private AttackKind GetAfterAllWallsAttackKind()
    {
        if (afterAllWallsAttackIndex % 2 == 0)
        {
            return AttackKind.空爆;
        }

        return AttackKind.移動;
    }

    private void AdvanceAfterAllWallsAttackIndex()
    {
        afterAllWallsAttackIndex++;
    }

    private IEnumerator Attack_AirStrike(PhaseAttackSetting setting)
    {
        if (setting == null)
        {
            yield break;
        }

        Transform centerTransform = airStrikeCenter;

        if (centerTransform == null)
        {
            centerTransform = transform;
        }

        airStrikeDudBombAlreadySpawnedThisAttack = false;

        int dudBombIndex = -1;

        if (allWallsBroken && dudBombPrefab != null && setting.airStrikeCount > 0)
        {
            dudBombIndex = Random.Range(0, setting.airStrikeCount);
        }

        List<GameObject> spawnedBombs = new List<GameObject>();

        for (int i = 0; i < setting.airStrikeCount; i++)
        {
            bool spawnDudBomb = allWallsBroken && i == dudBombIndex;

            GameObject bomb = SpawnAirStrikeBomb(setting, centerTransform, spawnDudBomb);

            if (bomb != null)
            {
                spawnedBombs.Add(bomb);
            }
        }

        if (showAirStrikeWaitLog)
        {
            Debug.Log("空爆爆弾を生成しました。待機対象: " + spawnedBombs.Count + "個");
        }

        if (waitUntilAirStrikeBombsDestroyed)
        {
            yield return StartCoroutine(WaitUntilAirStrikeBombsDestroyed(spawnedBombs));
        }
    }

    private IEnumerator WaitUntilAirStrikeBombsDestroyed(List<GameObject> bombs)
    {
        if (bombs == null || bombs.Count == 0)
        {
            if (showAirStrikeWaitLog)
            {
                Debug.LogWarning("空爆爆弾の待機対象が0個なので、次の行動へ進みます");
            }

            yield break;
        }

        float timer = 0.0f;
        float logTimer = 0.0f;

        if (showAirStrikeWaitLog)
        {
            Debug.Log("空爆爆弾がすべて爆発して消えるまで待機開始");
        }

        while (timer < maxAirStrikeBombWaitTime)
        {
            timer += Time.deltaTime;
            logTimer += Time.deltaTime;

            for (int i = bombs.Count - 1; i >= 0; i--)
            {
                GameObject bomb = bombs[i];

                if (bomb == null)
                {
                    bombs.RemoveAt(i);
                    continue;
                }

                if (!bomb.activeInHierarchy)
                {
                    bombs.RemoveAt(i);
                    continue;
                }
            }

            if (bombs.Count <= 0)
            {
                if (showAirStrikeWaitLog)
                {
                    Debug.Log("空爆で撃った爆弾がすべて消えました。次の行動へ進みます");
                }

                yield break;
            }

            if (showAirStrikeWaitLog && logTimer >= airStrikeWaitLogInterval)
            {
                logTimer = 0.0f;
                Debug.Log("空爆爆弾待機中。残り: " + bombs.Count + "個");
            }

            yield return null;
        }

        if (showAirStrikeWaitLog)
        {
            Debug.LogWarning("空爆爆弾の待機が最大時間を超えました。残り: " + bombs.Count + "個。次の行動へ進みます");
        }
    }

    private GameObject SpawnAirStrikeBomb(PhaseAttackSetting setting, Transform centerTransform, bool spawnDudBomb)
    {
        GameObject prefab = GetAirStrikeBombPrefab(spawnDudBomb);

        if (prefab == null)
        {
            Debug.LogWarning("空爆用の爆弾Prefabが設定されていません");
            return null;
        }

        Vector3 center = centerTransform.position;

        float randomAngle = Random.Range(0f, 360f);

        float randomDistance = Random.Range(
            setting.airStrikeMinDistance,
            setting.airStrikeMaxDistance
        );

        Vector3 direction = AngleToDirection(randomAngle);
        Vector3 groundPosition = center + direction * randomDistance;

        Vector3 spawnPosition = new Vector3(
            groundPosition.x,
            center.y + setting.airStrikeHeight,
            groundPosition.z
        );

        GameObject bomb = Instantiate(
            prefab,
            spawnPosition,
            Quaternion.identity
        );

        DudBombState dudBombState = bomb.GetComponent<DudBombState>();

        if (dudBombState == null)
        {
            dudBombState = bomb.GetComponentInChildren<DudBombState>();
        }

        if (dudBombState != null)
        {
            dudBombState.ClearThrownByPlayer();
        }

        Rigidbody rb = bomb.GetComponent<Rigidbody>();

        if (rb == null)
        {
            rb = bomb.GetComponentInChildren<Rigidbody>();
        }

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.down * setting.airStrikeFallSpeed;
        }
        else
        {
            Debug.LogWarning("空爆爆弾にRigidbodyがありません: " + bomb.name);
        }

        return bomb;
    }

    private GameObject GetAirStrikeBombPrefab(bool spawnDudBomb)
    {
        if (spawnDudBomb &&
            allWallsBroken &&
            dudBombPrefab != null &&
            !airStrikeDudBombAlreadySpawnedThisAttack)
        {
            airStrikeDudBombAlreadySpawnedThisAttack = true;
            return dudBombPrefab;
        }

        return bombPrefab;
    }
}