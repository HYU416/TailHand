using System.Collections;
using UnityEngine;

public partial class BossPhaseAttackController
{
    [Header("•Ç‚ª‘S”j‰ó‚³‚ê‚½Œã‚Í“ÁŽêUŒ‚ƒpƒ^[ƒ“‚É‚·‚é")]
    [SerializeField] private bool useSpecialAttackAfterAllWallsBroken = true;

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
            Debug.Log("•Ç‚ª‚·‚×‚Ä”j‰ó‚³‚ê‚½‚½‚ßA‹ó”š‚ÆƒXƒsƒ“‚ÌŒðŒÝUŒ‚‚É•ÏX‚µ‚Ü‚·");
        }
    }

    public void ResetAllWallsBrokenState()
    {
        allWallsBroken = false;
        airStrikeDudBombAlreadySpawnedThisAttack = false;
        afterAllWallsAttackIndex = 0;

        if (showDebugLog)
        {
            Debug.Log("•Ç”j‰óó‘Ô‚ðƒŠƒZƒbƒg‚µ‚Ü‚µ‚½");
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
            return AttackKind.‹ó”š;
        }

        return AttackKind.ˆÚ“®;
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

        for (int i = 0; i < setting.airStrikeCount; i++)
        {
            bool spawnDudBomb = allWallsBroken && i == dudBombIndex;
            SpawnAirStrikeBomb(setting, centerTransform, spawnDudBomb);
        }

        yield return null;
    }

    private void SpawnAirStrikeBomb(PhaseAttackSetting setting, Transform centerTransform, bool spawnDudBomb)
    {
        GameObject prefab = GetAirStrikeBombPrefab(spawnDudBomb);

        if (prefab == null)
        {
            Debug.LogWarning("‹ó”š—p‚Ì”š’ePrefab‚ªÝ’è‚³‚ê‚Ä‚¢‚Ü‚¹‚ñ");
            return;
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
            Debug.LogWarning("‹ó”š”š’e‚ÉRigidbody‚ª‚ ‚è‚Ü‚¹‚ñ");
        }
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