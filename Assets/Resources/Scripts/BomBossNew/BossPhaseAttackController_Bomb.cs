using System.Collections;
using UnityEngine;

public partial class BossPhaseAttackController
{
    private IEnumerator Attack_DudBombShot(PhaseAttackSetting setting)
    {
        for (int i = 0; i < setting.bombShotCount; i++)
        {
            if (setting.bombShootAllGuns)
            {
                GunSetting[] guns = GetUsableGuns();

                if (guns != null)
                {
                    for (int j = 0; j < guns.Length; j++)
                    {
                        ShootBombOrDudBomb(guns[j], setting);
                    }
                }
            }
            else
            {
                GunSetting gun = GetRandomUsableGun();

                if (gun != null)
                {
                    ShootBombOrDudBomb(gun, setting);
                }
            }

            yield return SpinWait(setting.bombShotInterval, setting.bombSpinSpeed);
        }
    }

    private IEnumerator SpinWait(float waitTime, float spinSpeed)
    {
        float timer = 0f;

        while (timer < waitTime)
        {
            timer += Time.deltaTime;

            if (spinSpeed != 0f)
            {
                transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
            }

            yield return null;
        }
    }

    private GunSetting GetRandomUsableGun()
    {
        GunSetting[] guns = GetUsableGuns();

        if (guns == null || guns.Length == 0)
        {
            return null;
        }

        int index = Random.Range(0, guns.Length);
        return guns[index];
    }

    private void ShootBombOrDudBomb(GunSetting gunSetting, PhaseAttackSetting setting)
    {
        bool useDudBomb = Random.Range(0f, 100f) < setting.dudBombMixRate;

        if (useDudBomb)
        {
            ShootBombPrefab(
                dudBombPrefab,
                gunSetting,
                setting.dudShootPower,
                setting.dudUpwardPower,
                "•s”­’e"
            );
        }
        else
        {
            ShootBombPrefab(
                bombPrefab,
                gunSetting,
                setting.bombShootPower,
                setting.bombUpwardPower,
                "”š’e"
            );
        }
    }

    private void ShootBombPrefab(
        GameObject prefab,
        GunSetting gunSetting,
        float shootPower,
        float upwardPower,
        string debugName
    )
    {
        if (prefab == null)
        {
            Debug.LogWarning(debugName + "Prefab‚ªÝ’è‚³‚ê‚Ä‚¢‚Ü‚¹‚ñ");
            return;
        }

        if (gunSetting == null || gunSetting.gun == null)
        {
            return;
        }

        Vector3 shootDirection = GetShootDirection(gunSetting.gun, gunSetting.shootAxis).normalized;

        Vector3 spawnPosition =
            gunSetting.gun.position + shootDirection * gunSetting.muzzleOffset;

        GameObject bomb = Instantiate(
            prefab,
            spawnPosition,
            Quaternion.LookRotation(shootDirection)
        );

        Rigidbody rb = bomb.GetComponent<Rigidbody>();

        if (rb == null)
        {
            rb = bomb.GetComponentInChildren<Rigidbody>();
        }

        if (rb == null)
        {
            Debug.LogWarning(debugName + "‚ÉRigidbody‚ª‚ ‚è‚Ü‚¹‚ñ");
            return;
        }

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Vector3 finalDirection =
            shootDirection + Vector3.up * upwardPower;

        finalDirection.Normalize();

        rb.linearVelocity = finalDirection * shootPower;
    }
}