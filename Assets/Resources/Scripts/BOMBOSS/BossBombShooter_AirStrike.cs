/*
 * ==========================================================
 * 制作責任者：小林大悟
 *
 * BossBombShooter の空爆攻撃を管理する分割スクリプトです。
 *
 * 【担当】
 * ・攻撃3：ランダム空爆
 * ・攻撃4：十字空爆
 * ・空爆用爆弾の生成
 *
 * ※このスクリプトはアタッチしません。
 * ※BossBombShooter.cs と同じクラスとして自動で合体します。
 * ==========================================================
 */

using System.Collections;
using UnityEngine;

public partial class BossBombShooter
{
    IEnumerator Attack3_RandomAirStrike(AttackNode node)
    {
        Transform centerTransform = GetRandomAirStrikeCenter(node);

        for (int i = 0; i < node.airStrikeBombCount; i++)
        {
            SpawnRandomAirStrikeBomb(node, centerTransform);

            yield return new WaitForSeconds(node.airStrikeInterval);
        }
    }

    IEnumerator Attack4_CrossAirStrike(AttackNode node)
    {
        Transform centerTransform = GetCrossAirStrikeCenter(node);

        for (int i = 0; i < node.crossAirStrikeBombCount; i++)
        {
            SpawnCrossAirStrikeBomb(node, centerTransform, i);

            yield return new WaitForSeconds(node.crossAirStrikeInterval);
        }
    }

    void SpawnRandomAirStrikeBomb(AttackNode node, Transform centerTransform)
    {
        if (centerTransform == null) return;

        Vector3 center = centerTransform.position;

        float randomAngle = Random.Range(0f, 360f);

        float randomDistance = Random.Range(
            node.airStrikeMinDistance,
            node.airStrikeMaxDistance
        );

        Vector3 direction = AngleToDirection(randomAngle);
        Vector3 groundPosition = center + direction * randomDistance;

        Vector3 spawnPosition = new Vector3(
            groundPosition.x,
            center.y + node.airStrikeHeight,
            groundPosition.z
        );

        bool isDud = false;

        if (node.useDudBomb)
        {
            isDud = Random.Range(0f, 100f) < node.dudChance;
        }

        SpawnAirStrikeBombObject(
            node,
            spawnPosition,
            isDud,
            node.airStrikeFallSpeed
        );
    }

    void SpawnCrossAirStrikeBomb(AttackNode node, Transform centerTransform, int index)
    {
        if (centerTransform == null) return;

        Vector3 center = centerTransform.position;

        float angle =
            node.crossAirStrikeAngleOffset + 90f * index;

        Vector3 direction = AngleToDirection(angle);

        Vector3 groundPosition =
            center + direction * node.crossAirStrikeDistance;

        Vector3 spawnPosition = new Vector3(
            groundPosition.x,
            center.y + node.crossAirStrikeHeight,
            groundPosition.z
        );

        bool isDud = false;

        if (node.useDudBomb)
        {
            isDud = Random.Range(0f, 100f) < node.dudChance;
        }

        SpawnAirStrikeBombObject(
            node,
            spawnPosition,
            isDud,
            node.crossAirStrikeFallSpeed
        );
    }

    void SpawnAirStrikeBombObject(
        AttackNode node,
        Vector3 spawnPosition,
        bool isDud,
        float fallSpeed
    )
    {
        GameObject prefab = GetBombPrefab(node, isDud);

        if (prefab == null)
        {
            Debug.LogWarning("空爆用の爆弾Prefabが設定されていません");
            return;
        }

        GameObject bomb = Instantiate(
            prefab,
            spawnPosition,
            Quaternion.identity
        );

        ApplyBombSetting(bomb, node, isDud);

        Rigidbody rb = GetBombRigidbody(bomb);

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = false;

            rb.linearDamping = Random.Range(
                node.minLinearDamping,
                node.maxLinearDamping
            );

            rb.linearVelocity = Vector3.down * fallSpeed;
        }
        else
        {
            Debug.LogWarning("空爆用BOMにRigidbodyがありません");
        }
    }

    Transform GetRandomAirStrikeCenter(AttackNode node)
    {
        if (node.airStrikeCenterOverride != null)
        {
            return node.airStrikeCenterOverride;
        }

        if (airStrikeCenter != null)
        {
            return airStrikeCenter;
        }

        return transform;
    }

    Transform GetCrossAirStrikeCenter(AttackNode node)
    {
        if (node.crossAirStrikeCenterOverride != null)
        {
            return node.crossAirStrikeCenterOverride;
        }

        if (airStrikeCenter != null)
        {
            return airStrikeCenter;
        }

        return transform;
    }
}