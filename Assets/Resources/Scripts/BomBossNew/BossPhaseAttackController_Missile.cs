using System.Collections;
using UnityEngine;

public partial class BossPhaseAttackController
{
    private IEnumerator Attack_HomingMissile(PhaseAttackSetting setting)
    {
        for (int i = 0; i < setting.missileCount; i++)
        {
            GunSetting gun = GetRandomUsableGun();

            if (gun != null)
            {
                ShootMissile(gun, setting);
            }

            yield return new WaitForSeconds(setting.missileInterval);
        }
    }

    private void ShootMissile(GunSetting gunSetting, PhaseAttackSetting setting)
    {
        if (missilePrefab == null)
        {
            Debug.LogWarning("ミサイルPrefabが設定されていません");
            return;
        }

        if (gunSetting == null || gunSetting.gun == null)
        {
            return;
        }

        Vector3 shootDirection = GetShootDirection(gunSetting.gun, gunSetting.shootAxis).normalized;

        Vector3 spawnPosition =
            gunSetting.gun.position + shootDirection * gunSetting.muzzleOffset;

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

        if (missile == null)
        {
            Debug.LogWarning("ミサイルPrefabにMissile.csが付いていません");
            return;
        }

        Transform target = player;

        missile.SetMissileSetting(
            target,
            setting.missileScale,
            setting.missileSpeed,
            setting.missileRotateSpeed,
            setting.missileExplosionTime,
            setting.missileExplodeOnHit,
            setting.missileExplodeOnlyPlayerHit,
            setting.missileExplosionRadius,
            setting.missileDamage,
            missileExplosionEffectPrefab,
            setting.missileExplosionEffectScaleMultiplier,
            setting.missileHoming,
            setting.missileUseGravity
        );
    }
}