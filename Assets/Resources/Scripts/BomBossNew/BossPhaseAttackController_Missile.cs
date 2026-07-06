using System.Collections;
using UnityEngine;

public partial class BossPhaseAttackController
{
    [Header("ミサイル生成直後の当たり判定無効時間")]
    [SerializeField]
    private float missileColliderDisableTime = 0.5f;

    [Header("ミサイルがボス自身に当たらないようにする")]
    [SerializeField]
    private bool ignoreBossCollidersForMissile = true;

    [Header("ミサイルが無視する床タグ")]
    [SerializeField]
    private string missileIgnoreGroundTag = "Ground";

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

        Vector3 shootDirection =
            GetShootDirection(gunSetting.gun, gunSetting.shootAxis).normalized;

        Vector3 spawnPosition =
            gunSetting.gun.position + shootDirection * gunSetting.muzzleOffset;

        GameObject missileObject = Instantiate(
            missilePrefab,
            spawnPosition,
            Quaternion.LookRotation(shootDirection)
        );

        if (ignoreBossCollidersForMissile)
        {
            IgnoreBossCollidersForSpawnedMissile(missileObject, gunSetting.gun);
        }

        IgnoreTaggedObjectCollidersForSpawnedMissile(
            missileObject,
            missileIgnoreGroundTag
        );

        StartCoroutine(DisableMissileCollidersAtSpawn(missileObject));

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

    private void IgnoreBossCollidersForSpawnedMissile(GameObject missileObject, Transform gunTransform)
    {
        if (missileObject == null)
        {
            return;
        }

        Collider[] missileColliders =
            missileObject.GetComponentsInChildren<Collider>(true);

        if (missileColliders == null || missileColliders.Length == 0)
        {
            return;
        }

        IgnoreCollisionWithTargetColliders(missileColliders, transform);

        if (rotateRoot != null && rotateRoot != transform)
        {
            IgnoreCollisionWithTargetColliders(missileColliders, rotateRoot);
        }

        if (gunTransform != null)
        {
            Transform current = gunTransform;

            while (current != null)
            {
                IgnoreCollisionWithTargetColliders(missileColliders, current);
                current = current.parent;
            }
        }
    }

    private void IgnoreTaggedObjectCollidersForSpawnedMissile(GameObject missileObject, string targetTag)
    {
        if (missileObject == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(targetTag))
        {
            return;
        }

        Collider[] missileColliders =
            missileObject.GetComponentsInChildren<Collider>(true);

        if (missileColliders == null || missileColliders.Length == 0)
        {
            return;
        }

        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(targetTag);

        if (taggedObjects == null || taggedObjects.Length == 0)
        {
            return;
        }

        for (int i = 0; i < taggedObjects.Length; i++)
        {
            GameObject taggedObject = taggedObjects[i];

            if (taggedObject == null)
            {
                continue;
            }

            Collider[] targetColliders =
                taggedObject.GetComponentsInChildren<Collider>(true);

            if (targetColliders == null || targetColliders.Length == 0)
            {
                continue;
            }

            IgnoreCollisionWithColliderArray(missileColliders, targetColliders);
        }
    }

    private void IgnoreCollisionWithTargetColliders(Collider[] missileColliders, Transform targetRoot)
    {
        if (missileColliders == null || targetRoot == null)
        {
            return;
        }

        Collider[] targetColliders =
            targetRoot.GetComponentsInChildren<Collider>(true);

        if (targetColliders == null || targetColliders.Length == 0)
        {
            return;
        }

        IgnoreCollisionWithColliderArray(missileColliders, targetColliders);
    }

    private void IgnoreCollisionWithColliderArray(Collider[] missileColliders, Collider[] targetColliders)
    {
        if (missileColliders == null || targetColliders == null)
        {
            return;
        }

        for (int i = 0; i < missileColliders.Length; i++)
        {
            Collider missileCollider = missileColliders[i];

            if (missileCollider == null)
            {
                continue;
            }

            for (int j = 0; j < targetColliders.Length; j++)
            {
                Collider targetCollider = targetColliders[j];

                if (targetCollider == null)
                {
                    continue;
                }

                if (missileCollider == targetCollider)
                {
                    continue;
                }

                Physics.IgnoreCollision(missileCollider, targetCollider, true);
            }
        }
    }

    private IEnumerator DisableMissileCollidersAtSpawn(GameObject missileObject)
    {
        if (missileObject == null)
        {
            yield break;
        }

        Collider[] colliders =
            missileObject.GetComponentsInChildren<Collider>(true);

        if (colliders == null || colliders.Length == 0)
        {
            yield break;
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = false;
            }
        }

        if (missileColliderDisableTime > 0f)
        {
            yield return new WaitForSeconds(missileColliderDisableTime);
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = true;
            }
        }
    }
}