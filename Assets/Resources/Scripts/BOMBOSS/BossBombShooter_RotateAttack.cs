/*
 * ==========================================================
 * 制作責任者：小林大悟
 *
 * BossBombShooter の攻撃1・攻撃2を管理する分割スクリプトです。
 *
 * 【攻撃1】
 * ・ランダム角度へ回転して止まってから発射
 *
 * 【攻撃2】
 * ・回転しながら発射
 *
 * ※このスクリプトはアタッチしません。
 * ※BossBombShooter.cs と同じクラスとして自動で合体します。
 * ==========================================================
 */

using System.Collections;
using UnityEngine;

public partial class BossBombShooter
{
    IEnumerator Attack1_RandomRotateShoot(AttackNode node)
    {
        if (node.randomAngles == null || node.randomAngles.Length == 0)
        {
            Debug.LogWarning("ランダム角度一覧が空です");
            yield break;
        }

        float angle = node.randomAngles[Random.Range(0, node.randomAngles.Length)];

        Quaternion targetRotation =
            baseRotation * Quaternion.Euler(0f, angle, 0f);

        yield return StartCoroutine(RotateTo(targetRotation, node.randomRotateTime));

        ShootSelectedGuns(node);
    }

    IEnumerator Attack2_SpinShoot(AttackNode node)
    {
        float timer = 0f;
        float fireTimer = 0f;

        float direction = 1.0f;

        if (node.spinRotateDirection == RotateDirection.反時計回り)
        {
            direction = -1.0f;
        }

        while (timer < node.spinAttackTime)
        {
            timer += Time.deltaTime;
            fireTimer += Time.deltaTime;

            if (rotateRoot != null)
            {
                rotateRoot.Rotate(
                    0f,
                    node.spinRotateSpeed * direction * Time.deltaTime,
                    0f
                );
            }

            if (fireTimer >= node.spinFireInterval)
            {
                fireTimer = 0f;
                ShootSelectedGuns(node);
            }

            yield return null;
        }
    }

    IEnumerator RotateTo(Quaternion targetRotation, float rotateTime)
    {
        if (rotateRoot == null)
        {
            yield break;
        }

        if (rotateTime <= 0f)
        {
            rotateRoot.rotation = targetRotation;
            yield break;
        }

        Quaternion startRotation = rotateRoot.rotation;
        float timer = 0f;

        while (timer < rotateTime)
        {
            timer += Time.deltaTime;

            float t = timer / rotateTime;

            rotateRoot.rotation = Quaternion.Slerp(
                startRotation,
                targetRotation,
                t
            );

            yield return null;
        }

        rotateRoot.rotation = targetRotation;
    }
}