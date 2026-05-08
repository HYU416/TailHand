/*
 * ==========================================================
 * 制作責任者：小林大悟
 *
 * BossBombShooter の爆弾発射処理を管理する分割スクリプトです。
 *
 * 【担当】
 * ・通常爆弾の発射
 * ・不発弾の抽選
 * ・爆弾Prefabの取得
 * ・BombExplosionへの設定反映
 *
 * ※このスクリプトはアタッチしません。
 * ※BossBombShooter.cs と同じクラスとして自動で合体します。
 * ==========================================================
 */

using UnityEngine;

public partial class BossBombShooter
{
    void ShootSelectedGuns(AttackNode node)
    {
        if (gunSettings == null || gunSettings.Length == 0)
        {
            Debug.LogWarning("砲台設定が空です");
            return;
        }

        int dudIndex = -1;

        if (node.useDudBomb && node.dudOnlyOne)
        {
            bool spawnDud = Random.Range(0f, 100f) < node.dudChance;

            if (spawnDud)
            {
                dudIndex = GetRandomUsableGunIndex(node);
            }
        }

        if (node.useGunIndexes != null && node.useGunIndexes.Length > 0)
        {
            for (int i = 0; i < node.useGunIndexes.Length; i++)
            {
                int gunIndex = node.useGunIndexes[i];

                if (gunIndex < 0 || gunIndex >= gunSettings.Length)
                {
                    Debug.LogWarning("存在しない砲台番号が指定されています：" + gunIndex);
                    continue;
                }

                bool isDud = GetDudResult(node, gunIndex, dudIndex);
                ShootBomb(gunSettings[gunIndex], node, isDud);
            }
        }
        else
        {
            for (int i = 0; i < gunSettings.Length; i++)
            {
                bool isDud = GetDudResult(node, i, dudIndex);
                ShootBomb(gunSettings[i], node, isDud);
            }
        }
    }

    bool GetDudResult(AttackNode node, int currentGunIndex, int dudIndex)
    {
        if (!node.useDudBomb) return false;

        if (node.dudOnlyOne)
        {
            return currentGunIndex == dudIndex;
        }

        return Random.Range(0f, 100f) < node.dudChance;
    }

    void ShootBomb(GunSetting gunSetting, AttackNode node, bool isDud)
    {
        if (gunSetting == null) return;
        if (!gunSetting.useThisGun) return;

        if (gunSetting.gun == null)
        {
            Debug.LogWarning("GUNが設定されていない砲台があります");
            return;
        }

        GameObject prefab = GetBombPrefab(node, isDud);

        if (prefab == null)
        {
            Debug.LogWarning("爆弾Prefab、または不発弾Prefabが設定されていません");
            return;
        }

        Vector3 shootDirection =
            GetShootDirection(gunSetting.gun, gunSetting.shootAxis);

        float muzzleOffset = gunSetting.muzzleOffset;

        if (node.muzzleOffsetOverride > 0f)
        {
            muzzleOffset = node.muzzleOffsetOverride;
        }

        Vector3 spawnPosition =
            gunSetting.gun.position + shootDirection * muzzleOffset;

        GameObject bomb = Instantiate(
            prefab,
            spawnPosition,
            Quaternion.LookRotation(shootDirection)
        );

        ApplyBombSetting(bomb, node, isDud);

        Rigidbody rb = GetBombRigidbody(bomb);

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;

            rb.linearDamping = Random.Range(
                node.minLinearDamping,
                node.maxLinearDamping
            );

            float speed = gunSetting.bombSpeed;

            if (node.bombSpeed > 0f)
            {
                speed = node.bombSpeed;
            }

            Vector3 finalDirection =
                shootDirection + Vector3.up * (gunSetting.upwardPower + node.upwardPower);

            finalDirection.Normalize();

            rb.linearVelocity = finalDirection * speed;
        }
        else
        {
            Debug.LogWarning("発射したBOMにRigidbodyがありません");
        }
    }

    void ApplyBombSetting(GameObject bomb, AttackNode node, bool isDud)
    {
        if (bomb == null) return;

        if (!isDud)
        {
            bomb.transform.localScale *= node.bombScale;
        }

        BombExplosion bombExplosion = bomb.GetComponent<BombExplosion>();

        if (bombExplosion == null)
        {
            bombExplosion = bomb.GetComponentInChildren<BombExplosion>();
        }

        if (bombExplosion != null)
        {
            bombExplosion.explosionTime = node.explosionTime;
            bombExplosion.explosionRadius = node.explosionRadius;
            bombExplosion.damage = node.damage;
            bombExplosion.explosionEffectScaleMultiplier = node.explosionEffectScaleMultiplier;

            bombExplosion.useBlinkBeforeExplosion = node.useBlinkBeforeExplosion;
            bombExplosion.blinkBeforeExplosionTime = node.blinkBeforeExplosionTime;
            bombExplosion.blinkInterval = node.blinkInterval;
            bombExplosion.blinkColor = node.blinkColor;
        }
    }

    GameObject GetBombPrefab(AttackNode node, bool isDud)
    {
        if (isDud)
        {
            if (node.dudBombPrefabOverride != null)
            {
                return node.dudBombPrefabOverride;
            }

            return dudBombPrefab;
        }

        if (node.bombPrefabOverride != null)
        {
            return node.bombPrefabOverride;
        }

        return bombPrefab;
    }

    Rigidbody GetBombRigidbody(GameObject bomb)
    {
        Rigidbody rb = bomb.GetComponent<Rigidbody>();

        if (rb == null)
        {
            rb = bomb.GetComponentInChildren<Rigidbody>();
        }

        return rb;
    }

    int GetRandomUsableGunIndex(AttackNode node)
    {
        if (node.useGunIndexes != null && node.useGunIndexes.Length > 0)
        {
            int randomArrayIndex = Random.Range(0, node.useGunIndexes.Length);
            return node.useGunIndexes[randomArrayIndex];
        }

        return Random.Range(0, gunSettings.Length);
    }
}