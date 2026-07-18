using System.Collections;
using UnityEngine;

public partial class BossPhaseAttackController
{
    private IEnumerator Attack_BulletHell(PhaseAttackSetting setting)
    {
        if (bulletHellBulletPrefab == null)
        {
            Debug.LogWarning("íeñãíePrefabÇ™ê›íËÇ≥ÇÍÇƒÇ¢Ç‹ÇπÇÒ");
            yield break;
        }

        float timer = 0f;
        float fireTimer = 0f;

        float rotateDirection = 1.0f;

        if (setting.bulletHellRotateDirection == RotateDirection.CounterClockwise)
        {
            rotateDirection = -1.0f;
        }

        while (timer < setting.bulletHellTime)
        {
            timer += Time.deltaTime;
            fireTimer += Time.deltaTime;

            if (rotateRoot != null)
            {
                rotateRoot.Rotate(
                    0f,
                    setting.bulletHellRotateSpeed * rotateDirection * Time.deltaTime,
                    0f
                );
            }

            if (fireTimer >= setting.bulletHellFireInterval)
            {
                fireTimer = 0f;
                FireBulletHell(setting);
            }

            yield return null;
        }
    }

    private void FireBulletHell(PhaseAttackSetting setting)
    {
        GunSetting[] guns = GetUsableGuns();

        if (guns == null)
        {
            return;
        }

        for (int i = 0; i < guns.Length; i++)
        {
            FireOneBulletHell(guns[i], setting);
            if(i== 0) MySoundManeger.Play(gameObject, SEList.SE_BEAM);

        }
    }

    private void FireOneBulletHell(GunSetting gunSetting, PhaseAttackSetting setting)
    {
        if (gunSetting == null || gunSetting.gun == null)
        {
            return;
        }

        Vector3 shootDirection =
            GetShootDirection(gunSetting.gun, gunSetting.shootAxis);

        shootDirection.y = 0f;

        if (shootDirection.sqrMagnitude <= 0.001f)
        {
            shootDirection = gunSetting.gun.forward;
            shootDirection.y = 0f;
        }

        if (shootDirection.sqrMagnitude <= 0.001f)
        {
            shootDirection = transform.forward;
            shootDirection.y = 0f;
        }

        shootDirection.Normalize();

        Vector3 spawnPosition =
            gunSetting.gun.position + shootDirection * gunSetting.muzzleOffset;

        GameObject bulletObject = EffectManager.Instance.Play(EffectType.Beam, spawnPosition, Quaternion.LookRotation(shootDirection));
        //MySoundManeger.Play(gameObject, SEList.SE_BEAM);


        BulletHellBullet bullet = bulletObject.GetComponent<BulletHellBullet>();

        if (bullet == null)
        {
            bullet = bulletObject.GetComponentInChildren<BulletHellBullet>();
        }

        if (bullet != null)
        {
            bullet.SetBulletData(
                shootDirection,
                setting.bulletHellBulletSpeed,
                setting.bulletHellBulletScale,
                setting.bulletHellBulletLifeTime,
                setting.bulletHellBulletShrinkTime,
                setting.bulletHellDamage,
                setting.bulletHellDestroyOnPlayerHit
            );
        }
        else
        {
            Rigidbody rb = bulletObject.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.useGravity = false;
                rb.linearVelocity = shootDirection * setting.bulletHellBulletSpeed;
            }

            Destroy(
                bulletObject,
                setting.bulletHellBulletLifeTime + setting.bulletHellBulletShrinkTime
            );
        }
    }
}