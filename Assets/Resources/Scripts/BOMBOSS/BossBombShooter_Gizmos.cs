/*
 * ==========================================================
 * 制作責任者：小林大悟
 *
 * BossBombShooter のSceneビュー表示を管理する分割スクリプトです。
 *
 * 【担当】
 * ・ランダム空爆範囲の表示
 * ・十字空爆位置の表示
 * ・追尾ミサイルの爆発範囲表示
 *
 * ※このスクリプトはアタッチしません。
 * ※BossBombShooter.cs と同じクラスとして自動で合体します。
 * ==========================================================
 */

using UnityEngine;

public partial class BossBombShooter
{
    void OnDrawGizmosSelected()
    {
        if (attackNodes == null) return;

        Gizmos.color = Color.red;

        foreach (AttackNode node in attackNodes)
        {
            if (node == null) continue;

            if (node.attackKind == AttackKind.攻撃3_ランダム空爆)
            {
                Transform centerTransform = GetRandomAirStrikeCenter(node);
                if (centerTransform == null) continue;

                Vector3 center = centerTransform.position;

                Gizmos.DrawWireSphere(center, node.airStrikeMinDistance);
                Gizmos.DrawWireSphere(center, node.airStrikeMaxDistance);
            }

            if (node.attackKind == AttackKind.攻撃4_十字空爆)
            {
                Transform centerTransform = GetCrossAirStrikeCenter(node);
                if (centerTransform == null) continue;

                Vector3 center = centerTransform.position;

                for (int i = 0; i < node.crossAirStrikeBombCount; i++)
                {
                    float angle =
                        node.crossAirStrikeAngleOffset + 90f * i;

                    Vector3 pos =
                        center + AngleToDirection(angle) * node.crossAirStrikeDistance;

                    Gizmos.DrawWireSphere(pos, node.explosionRadius);
                }
            }

            if (node.attackKind == AttackKind.攻撃5_追尾ミサイル)
            {
                Transform targetTransform = node.missileTargetOverride;

                if (targetTransform == null)
                {
                    GameObject player = GameObject.FindGameObjectWithTag("Player");

                    if (player != null)
                    {
                        targetTransform = player.transform;
                    }
                }

                if (targetTransform != null)
                {
                    Gizmos.DrawWireSphere(targetTransform.position, node.missileExplosionRadius);
                }
            }
        }
    }
}