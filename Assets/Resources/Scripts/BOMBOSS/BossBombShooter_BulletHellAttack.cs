/*
 * ==========================================================
 * §ìÓ”CŽÒF¬—Ñ‘åŒå
 *
 * BossBombShooter ‚Ì’e–‹UŒ‚‚ðŠÇ—‚·‚é•ªŠ„ƒXƒNƒŠƒvƒg‚Å‚·B
 *
 * yUŒ‚6F‰ñ“]’e–‹z
 * E“G–{‘Ì‚ð‰ñ“]‚³‚¹‚È‚ª‚ç’e‚ð˜A‘±”­ŽË
 * E’e‚ðo‚·–C‘ä‚ðTransform”z—ñ‚Å§ŒÀ‰Â”\
 * EUŒ‚ŠJŽn‘O‚É–C‘ä‚ðŽw’è•b”‚¾‚¯“_–Å
 * E’e‚Ì”­ŽËˆÊ’u‚ðã‰º‚É•â³‰Â”\
 * E’e‚Ìis•ûŒü‚ð…•½‚É•â³‰Â”\
 * E’e‚Í BulletHellBullet.cs ‘¤‚Åˆê’èŽžŠÔŒã‚É¬‚³‚­‚È‚è‚È‚ª‚çÁ‚¦‚é
 *
 * ¦‚±‚ÌƒXƒNƒŠƒvƒg‚ÍƒAƒ^ƒbƒ`‚µ‚Ü‚¹‚ñB
 * ¦BossBombShooter.cs ‚Æ“¯‚¶ƒNƒ‰ƒX‚Æ‚µ‚ÄŽ©“®‚Å‡‘Ì‚µ‚Ü‚·B
 * ==========================================================
 */

using System.Collections;
using UnityEngine;

public partial class BossBombShooter
{
    IEnumerator Attack6_BulletHell(AttackNode node)
    {
        if (node.bulletHellBulletPrefab == null)
        {
            Debug.LogWarning("’e–‹’ePrefab‚ªÝ’è‚³‚ê‚Ä‚¢‚Ü‚¹‚ñ");
            yield break;
        }

        Transform[] fireGuns = GetBulletHellFireGuns(node);

        if (fireGuns == null || fireGuns.Length == 0)
        {
            Debug.LogWarning("’e–‹—p‚Ì–C‘ä‚ªÝ’è‚³‚ê‚Ä‚¢‚Ü‚¹‚ñ");
            yield break;
        }

        if (node.bulletHellBlinkBeforeFire)
        {
            yield return StartCoroutine(BlinkBulletHellGuns(node, fireGuns));
        }

        float attackTimer = 0f;
        float fireTimer = 0f;
        int firedCount = 0;

        float rotateDirection = 1.0f;

        if (node.bulletHellRotateDirection == RotateDirection.”½ŽžŒv‰ñ‚è)
        {
            rotateDirection = -1.0f;
        }

        bool useShotLimit = node.bulletHellShotCount > 0;

        while (attackTimer < node.bulletHellAttackTime)
        {
            attackTimer += Time.deltaTime;
            fireTimer += Time.deltaTime;

            RotateBulletHellBody(node, rotateDirection);

            if (fireTimer >= node.bulletHellFireInterval)
            {
                fireTimer = 0f;

                FireBulletHellBullets(node, fireGuns);

                firedCount++;

                if (useShotLimit && firedCount >= node.bulletHellShotCount)
                {
                    yield break;
                }
            }

            yield return null;
        }
    }

    void RotateBulletHellBody(AttackNode node, float direction)
    {
        if (rotateRoot == null) return;

        rotateRoot.Rotate(
            0f,
            node.bulletHellRotateSpeed * direction * Time.deltaTime,
            0f
        );
    }

    Transform[] GetBulletHellFireGuns(AttackNode node)
    {
        if (node.bulletHellFireGuns != null &&
            node.bulletHellFireGuns.Length > 0)
        {
            return node.bulletHellFireGuns;
        }

        if (gunSettings == null || gunSettings.Length == 0)
        {
            return null;
        }

        int count = 0;

        for (int i = 0; i < gunSettings.Length; i++)
        {
            if (gunSettings[i] != null &&
                gunSettings[i].gun != null &&
                gunSettings[i].useThisGun)
            {
                count++;
            }
        }

        if (count <= 0) return null;

        Transform[] result = new Transform[count];
        int index = 0;

        for (int i = 0; i < gunSettings.Length; i++)
        {
            if (gunSettings[i] != null &&
                gunSettings[i].gun != null &&
                gunSettings[i].useThisGun)
            {
                result[index] = gunSettings[i].gun;
                index++;
            }
        }

        return result;
    }

    IEnumerator BlinkBulletHellGuns(AttackNode node, Transform[] fireGuns)
    {
        if (node.bulletHellBlinkTime <= 0f) yield break;
        if (fireGuns == null || fireGuns.Length == 0) yield break;

        Renderer[] renderers = GetRenderersFromTransforms(fireGuns);

        if (renderers == null || renderers.Length == 0) yield break;

        float timer = 0f;
        float blinkTimer = 0f;
        bool blinkOn = false;

        MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

        while (timer < node.bulletHellBlinkTime)
        {
            timer += Time.deltaTime;
            blinkTimer += Time.deltaTime;

            if (blinkTimer >= node.bulletHellBlinkInterval)
            {
                blinkTimer = 0f;
                blinkOn = !blinkOn;

                if (blinkOn)
                {
                    SetRendererColor(renderers, propertyBlock, node.bulletHellBlinkColor);
                }
                else
                {
                    ResetRendererColor(renderers);
                }
            }

            yield return null;
        }

        ResetRendererColor(renderers);
    }

    Renderer[] GetRenderersFromTransforms(Transform[] targets)
    {
        int count = 0;

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null) continue;

            Renderer[] foundRenderers =
                targets[i].GetComponentsInChildren<Renderer>();

            count += foundRenderers.Length;
        }

        if (count <= 0) return null;

        Renderer[] result = new Renderer[count];
        int index = 0;

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null) continue;

            Renderer[] foundRenderers =
                targets[i].GetComponentsInChildren<Renderer>();

            for (int j = 0; j < foundRenderers.Length; j++)
            {
                result[index] = foundRenderers[j];
                index++;
            }
        }

        return result;
    }

    void SetRendererColor(
        Renderer[] renderers,
        MaterialPropertyBlock propertyBlock,
        Color color
    )
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;

            renderers[i].GetPropertyBlock(propertyBlock);

            propertyBlock.SetColor("_Color", color);
            propertyBlock.SetColor("_BaseColor", color);

            renderers[i].SetPropertyBlock(propertyBlock);
        }
    }

    void ResetRendererColor(Renderer[] renderers)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;

            renderers[i].SetPropertyBlock(null);
        }
    }

    void FireBulletHellBullets(AttackNode node, Transform[] fireGuns)
    {
        for (int i = 0; i < fireGuns.Length; i++)
        {
            if (fireGuns[i] == null) continue;

            FireOneBulletHellBullet(node, fireGuns[i]);
        }
    }

    void FireOneBulletHellBullet(AttackNode node, Transform fireGun)
    {
        Vector3 shootDirection =
            GetShootDirection(fireGun, node.bulletHellShootAxis);

        if (node.bulletHellForceHorizontalDirection)
        {
            shootDirection.y = 0f;

            if (shootDirection.sqrMagnitude <= 0.001f)
            {
                shootDirection = fireGun.forward;
                shootDirection.y = 0f;
            }

            if (shootDirection.sqrMagnitude <= 0.001f)
            {
                shootDirection = fireGun.right;
                shootDirection.y = 0f;
            }
        }

        shootDirection += Vector3.up * node.bulletHellDirectionHeightOffset;

        if (shootDirection.sqrMagnitude <= 0.001f)
        {
            shootDirection = fireGun.forward;
        }

        shootDirection.Normalize();

        float muzzleOffset = node.bulletHellMuzzleOffset;

        Vector3 spawnPosition =
            fireGun.position + shootDirection * muzzleOffset;

        spawnPosition += Vector3.up * node.bulletHellSpawnHeightOffset;

        GameObject bulletObject = Instantiate(
            node.bulletHellBulletPrefab,
            spawnPosition,
            Quaternion.LookRotation(shootDirection)
        );

        bulletObject.transform.localScale =
            Vector3.one * node.bulletHellBulletScale;

        BulletHellBullet bullet =
            bulletObject.GetComponent<BulletHellBullet>();

        if (bullet == null)
        {
            bullet = bulletObject.GetComponentInChildren<BulletHellBullet>();
        }

        if (bullet != null)
        {
            bullet.SetBulletData(
                shootDirection,
                node.bulletHellBulletSpeed,
                node.bulletHellBulletScale,
                node.bulletHellBulletLifeTime,
                node.bulletHellBulletShrinkTime,
                node.bulletHellDamage,
                node.bulletHellDestroyOnPlayerHit
            );
        }
        else
        {
            Rigidbody rb = bulletObject.GetComponent<Rigidbody>();

            if (rb == null)
            {
                rb = bulletObject.GetComponentInChildren<Rigidbody>();
            }

            if (rb != null)
            {
                rb.useGravity = false;
                rb.linearVelocity = shootDirection * node.bulletHellBulletSpeed;
            }

            Destroy(
                bulletObject,
                node.bulletHellBulletLifeTime + node.bulletHellBulletShrinkTime
            );
        }
    }
}