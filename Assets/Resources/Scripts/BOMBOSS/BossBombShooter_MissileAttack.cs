/*
 * ==========================================================
 * 制作責任者：小林大悟
 *
 * BossBombShooter の追尾ミサイル攻撃を管理する分割スクリプトです。
 *
 * 【担当】
 * ・攻撃5：追尾ミサイル
 * ・ミサイルPrefabの生成
 * ・Missile.csへの設定反映
 *
 * ※このスクリプトはアタッチしません。
 * ※BossBombShooter.cs と同じクラスとして自動で合体します。
 *
 * ※ミサイル本体の動き、向き、爆発処理は Missile.cs 側に書きます。
 * ==========================================================
 */

using System.Collections;
using UnityEngine;

public partial class BossBombShooter
{
    IEnumerator Attack5_HomingMissile(AttackNode node)
    {
        /*
         * 今回は既存仕様のままです。
         *
         * Missile Count は「発射回数」として扱います。
         * 使用する砲台番号が空の場合、1回の発射で全砲台から撃ちます。
         *
         * 例：
         * Missile Count 4
         * 砲台4個
         * 使用する砲台番号 空
         * ↓
         * 4回 × 4砲台 = 16発
         */
        for (int i = 0; i < node.missileCount; i++)
        {
            ShootSelectedMissileGuns(node);

            yield return new WaitForSeconds(node.missileFireInterval);
        }
    }

    void ShootSelectedMissileGuns(AttackNode node)
    {
        if (gunSettings == null || gunSettings.Length == 0)
        {
            Debug.LogWarning("砲台設定が空です");
            return;
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

                ShootMissile(gunSettings[gunIndex], node);
            }
        }
        else
        {
            for (int i = 0; i < gunSettings.Length; i++)
            {
                ShootMissile(gunSettings[i], node);
            }
        }
    }

    void ShootMissile(GunSetting gunSetting, AttackNode node)
    {
        if (gunSetting == null) return;
        if (!gunSetting.useThisGun) return;

        if (gunSetting.gun == null)
        {
            Debug.LogWarning("GUNが設定されていない砲台があります");
            return;
        }

        if (missilePrefab == null)
        {
            Debug.LogWarning("追尾ミサイルPrefabが設定されていません");
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

        GameObject missileObject = Instantiate(
            missilePrefab,
            spawnPosition,
            Quaternion.LookRotation(shootDirection)
        );

        Missile missile = missileObject.GetComponent<Missile>();

        if (missile == null)
        {
            missile = missileObject.GetComponentInChildren<Missile>();
        }

        if (missile != null)
        {
            Transform target = node.missileTargetOverride;

            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");

                if (player != null)
                {
                    target = player.transform;
                }
            }

            missile.SetMissileSetting(
                target,
                node.missileScale,
                node.missileSpeed,
                node.missileRotateSpeed,
                node.missileExplosionTime,
                node.missileExplodeOnHit,
                node.missileExplodeOnlyPlayerHit,
                node.missileExplosionRadius,
                node.missileDamage,
                node.missileExplosionEffectPrefab,
                node.missileExplosionEffectScaleMultiplier,
                node.missileHoming,
                node.missileUseGravity
            );
        }
        else
        {
            Debug.LogWarning("追尾ミサイルPrefabにMissile.csが付いていません");
        }
    }
}