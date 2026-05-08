/*
 * ==========================================================
 * 制作責任者：小林大悟
 *
 * BossBombShooter.cs のInspector表示を見やすくするための
 * カスタムインスペクターです。
 *
 * ※このスクリプトはアタッチしません。
 * ※Project内に1個だけ置いてください。
 * ==========================================================
 */

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BossBombShooter))]
public class BossBombShooterEditor : Editor
{
    SerializedProperty gunSettings;
    SerializedProperty attackNodes;

    SerializedProperty bombPrefab;
    SerializedProperty dudBombPrefab;
    SerializedProperty missilePrefab;
    SerializedProperty rotateRoot;
    SerializedProperty airStrikeCenter;
    SerializedProperty playOnStart;
    SerializedProperty loopAttackNodes;
    SerializedProperty startWaitTime;

    bool showOverallSettings = true;
    bool showGunSettings = true;
    bool showAttackNodes = true;

    bool[] gunFoldouts = new bool[0];
    bool[] attackNodeFoldouts = new bool[0];

    const int InspectorFontSize = 12;

    GUIStyle normalLabelStyle;
    GUIStyle wordWrapLabelStyle;
    GUIStyle foldoutStyle;
    GUIStyle buttonStyle;
    GUIStyle boxStyle;

    void OnEnable()
    {
        gunSettings = serializedObject.FindProperty("gunSettings");
        attackNodes = serializedObject.FindProperty("attackNodes");

        bombPrefab = serializedObject.FindProperty("bombPrefab");
        dudBombPrefab = serializedObject.FindProperty("dudBombPrefab");
        missilePrefab = serializedObject.FindProperty("missilePrefab");
        rotateRoot = serializedObject.FindProperty("rotateRoot");
        airStrikeCenter = serializedObject.FindProperty("airStrikeCenter");
        playOnStart = serializedObject.FindProperty("playOnStart");
        loopAttackNodes = serializedObject.FindProperty("loopAttackNodes");
        startWaitTime = serializedObject.FindProperty("startWaitTime");

        SyncFoldoutSize();
    }

    public override void OnInspectorGUI()
    {
        CreateStyles();

        serializedObject.Update();

        SyncFoldoutSize();

        DrawSampleButton();

        Space();

        DrawOverallSettings();

        Space();

        DrawGunSettings();

        Space();

        DrawAttackNodes();

        serializedObject.ApplyModifiedProperties();
    }

    void CreateStyles()
    {
        normalLabelStyle = new GUIStyle(EditorStyles.label);
        normalLabelStyle.fontSize = InspectorFontSize;
        normalLabelStyle.fontStyle = FontStyle.Normal;
        normalLabelStyle.wordWrap = false;

        wordWrapLabelStyle = new GUIStyle(EditorStyles.label);
        wordWrapLabelStyle.fontSize = InspectorFontSize;
        wordWrapLabelStyle.fontStyle = FontStyle.Normal;
        wordWrapLabelStyle.wordWrap = true;

        foldoutStyle = new GUIStyle(EditorStyles.foldout);
        foldoutStyle.fontSize = InspectorFontSize;
        foldoutStyle.fontStyle = FontStyle.Normal;

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = InspectorFontSize;
        buttonStyle.fontStyle = FontStyle.Normal;

        boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.fontSize = InspectorFontSize;
        boxStyle.fontStyle = FontStyle.Normal;
        boxStyle.padding = new RectOffset(8, 8, 8, 8);
    }

    void DrawSampleButton()
    {
        EditorGUILayout.BeginVertical(boxStyle);

        DrawNormalText("初期サンプルを作成すると、攻撃ノードが自動で入ります。砲台設定は消さずに残します。");

        SpaceSmall();

        if (GUILayout.Button("初期サンプルを作成", buttonStyle))
        {
            BossBombShooter shooter = (BossBombShooter)target;

            Undo.RecordObject(shooter, "初期サンプルを作成");

            shooter.CreateDefaultInspectorData();

            EditorUtility.SetDirty(shooter);

            serializedObject.Update();
            SyncFoldoutSize();
        }

        EditorGUILayout.EndVertical();
    }

    void SyncFoldoutSize()
    {
        if (gunSettings != null && gunFoldouts.Length != gunSettings.arraySize)
        {
            bool[] newFoldouts = new bool[gunSettings.arraySize];

            for (int i = 0; i < newFoldouts.Length; i++)
            {
                if (i < gunFoldouts.Length)
                {
                    newFoldouts[i] = gunFoldouts[i];
                }
                else
                {
                    newFoldouts[i] = false;
                }
            }

            gunFoldouts = newFoldouts;
        }

        if (attackNodes != null && attackNodeFoldouts.Length != attackNodes.arraySize)
        {
            bool[] newFoldouts = new bool[attackNodes.arraySize];

            for (int i = 0; i < newFoldouts.Length; i++)
            {
                if (i < attackNodeFoldouts.Length)
                {
                    newFoldouts[i] = attackNodeFoldouts[i];
                }
                else
                {
                    newFoldouts[i] = false;
                }
            }

            attackNodeFoldouts = newFoldouts;
        }
    }

    void DrawOverallSettings()
    {
        showOverallSettings = EditorGUILayout.Foldout(
            showOverallSettings,
            "全体設定",
            true,
            foldoutStyle
        );

        if (!showOverallSettings) return;

        EditorGUILayout.BeginVertical(boxStyle);

        DrawProperty(bombPrefab, "通常爆弾Prefab");
        DrawProperty(dudBombPrefab, "不発弾Prefab");
        DrawProperty(missilePrefab, "追尾ミサイルPrefab");
        DrawProperty(rotateRoot, "回転させるボス本体");
        DrawProperty(airStrikeCenter, "空爆中心位置");
        DrawProperty(playOnStart, "ゲーム開始時に攻撃開始");
        DrawProperty(loopAttackNodes, "最後まで行ったら最初に戻る");
        DrawProperty(startWaitTime, "攻撃開始までの待ち時間");

        EditorGUILayout.EndVertical();
    }

    void DrawGunSettings()
    {
        showGunSettings = EditorGUILayout.Foldout(
            showGunSettings,
            "砲台設定",
            true,
            foldoutStyle
        );

        if (!showGunSettings) return;

        EditorGUILayout.BeginVertical(boxStyle);

        DrawNormalText("ここは敵の動きではなく、GUNだけを登録する場所です。必要な砲台数だけ追加してください。");

        SpaceSmall();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("砲台を追加", buttonStyle))
        {
            gunSettings.arraySize++;
            serializedObject.ApplyModifiedProperties();
            SyncFoldoutSize();
        }

        if (GUILayout.Button("最後の砲台を削除", buttonStyle))
        {
            if (gunSettings.arraySize > 0)
            {
                gunSettings.arraySize--;
                serializedObject.ApplyModifiedProperties();
                SyncFoldoutSize();
            }
        }

        EditorGUILayout.EndHorizontal();

        Space();

        for (int i = 0; i < gunSettings.arraySize; i++)
        {
            DrawOneGunSetting(i);
        }

        EditorGUILayout.EndVertical();
    }

    void DrawOneGunSetting(int index)
    {
        SerializedProperty gun =
            gunSettings.GetArrayElementAtIndex(index);

        SerializedProperty gunTransform =
            gun.FindPropertyRelative("gun");

        string gunLabel = "砲台 Element " + index;

        if (gunTransform != null && gunTransform.objectReferenceValue != null)
        {
            gunLabel += "： " + gunTransform.objectReferenceValue.name;
        }

        EditorGUILayout.BeginVertical(boxStyle);

        EditorGUILayout.BeginHorizontal();

        gunFoldouts[index] = EditorGUILayout.Foldout(
            gunFoldouts[index],
            gunLabel,
            true,
            foldoutStyle
        );

        if (GUILayout.Button("削除", buttonStyle, GUILayout.Width(60)))
        {
            gunSettings.DeleteArrayElementAtIndex(index);
            serializedObject.ApplyModifiedProperties();
            SyncFoldoutSize();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUILayout.EndHorizontal();

        if (gunFoldouts[index])
        {
            Space();

            DrawProperty(gun.FindPropertyRelative("gun"), "弾を出すGUN本体");
            DrawProperty(gun.FindPropertyRelative("shootAxis"), "弾を出す方向");
            DrawProperty(gun.FindPropertyRelative("muzzleOffset"), "砲身先端までの距離");
            DrawProperty(gun.FindPropertyRelative("bombSpeed"), "この砲台の基本弾速");
            DrawProperty(gun.FindPropertyRelative("upwardPower"), "少し上へ飛ばす力");
            DrawProperty(gun.FindPropertyRelative("useThisGun"), "この砲台を使う");
        }

        EditorGUILayout.EndVertical();

        Space();
    }

    void DrawAttackNodes()
    {
        showAttackNodes = EditorGUILayout.Foldout(
            showAttackNodes,
            "攻撃ノード一覧",
            true,
            foldoutStyle
        );

        if (!showAttackNodes) return;

        EditorGUILayout.BeginVertical(boxStyle);

        DrawNormalText("ここで敵の動きを作ります。攻撃ノードを折りたたんで、必要なものだけ開いて編集できます。");

        SpaceSmall();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("攻撃ノードを追加", buttonStyle))
        {
            attackNodes.arraySize++;
            serializedObject.ApplyModifiedProperties();
            SyncFoldoutSize();
        }

        if (GUILayout.Button("最後の攻撃ノードを削除", buttonStyle))
        {
            if (attackNodes.arraySize > 0)
            {
                attackNodes.arraySize--;
                serializedObject.ApplyModifiedProperties();
                SyncFoldoutSize();
            }
        }

        EditorGUILayout.EndHorizontal();

        Space();

        for (int i = 0; i < attackNodes.arraySize; i++)
        {
            DrawOneAttackNode(i);
        }

        EditorGUILayout.EndVertical();
    }

    void DrawOneAttackNode(int index)
    {
        SerializedProperty node =
            attackNodes.GetArrayElementAtIndex(index);

        SerializedProperty nodeName =
            node.FindPropertyRelative("nodeName");

        SerializedProperty attackKind =
            node.FindPropertyRelative("attackKind");

        SerializedProperty useThisNode =
            node.FindPropertyRelative("useThisNode");

        string title = "攻撃ノード Element " + index;

        if (nodeName != null && !string.IsNullOrEmpty(nodeName.stringValue))
        {
            title += "： " + nodeName.stringValue;
        }

        EditorGUILayout.BeginVertical(boxStyle);

        attackNodeFoldouts[index] = EditorGUILayout.Foldout(
            attackNodeFoldouts[index],
            title,
            true,
            foldoutStyle
        );

        if (attackNodeFoldouts[index])
        {
            SpaceSmall();

            EditorGUILayout.BeginVertical(boxStyle);

            DrawSubTitle("ノード操作");

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("この攻撃ノードを複製", buttonStyle))
            {
                attackNodes.InsertArrayElementAtIndex(index);
                serializedObject.ApplyModifiedProperties();
                SyncFoldoutSize();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
                return;
            }

            if (GUILayout.Button("この攻撃ノードを削除", buttonStyle))
            {
                attackNodes.DeleteArrayElementAtIndex(index);
                serializedObject.ApplyModifiedProperties();
                SyncFoldoutSize();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            Space();

            DrawProperty(nodeName, "ノード名");
            DrawProperty(attackKind, "使う攻撃");
            DrawProperty(useThisNode, "このノードを使う");

            Space();

            DrawProperty(node.FindPropertyRelative("waitBeforeAttack"), "攻撃前の待ち時間");
            DrawProperty(node.FindPropertyRelative("waitAfterAttack"), "攻撃後の待ち時間");

            Space();

            BossBombShooter.AttackKind selectedAttack =
                (BossBombShooter.AttackKind)attackKind.enumValueIndex;

            DrawCommonSettings(node, selectedAttack);

            Space();

            if (selectedAttack == BossBombShooter.AttackKind.攻撃1_ランダム回転して止まってから発射)
            {
                DrawAttack1Settings(node);
            }
            else if (selectedAttack == BossBombShooter.AttackKind.攻撃2_回転しながら発射)
            {
                DrawAttack2Settings(node);
            }
            else if (selectedAttack == BossBombShooter.AttackKind.攻撃3_ランダム空爆)
            {
                DrawAttack3Settings(node);
            }
            else if (selectedAttack == BossBombShooter.AttackKind.攻撃4_十字空爆)
            {
                DrawAttack4Settings(node);
            }
            else if (selectedAttack == BossBombShooter.AttackKind.攻撃5_追尾ミサイル)
            {
                DrawAttack5Settings(node);
            }
            else if (selectedAttack == BossBombShooter.AttackKind.攻撃6_回転弾幕)
            {
                DrawAttack6Settings(node);
            }
        }

        EditorGUILayout.EndVertical();

        Space();
    }

    void DrawCommonSettings(
        SerializedProperty node,
        BossBombShooter.AttackKind selectedAttack
    )
    {
        DrawSubTitle("共通設定");

        DrawProperty(node.FindPropertyRelative("useGunIndexes"), "使用する砲台番号", true);

        if (selectedAttack == BossBombShooter.AttackKind.攻撃5_追尾ミサイル ||
            selectedAttack == BossBombShooter.AttackKind.攻撃6_回転弾幕)
        {
            return;
        }

        DrawSubTitle("Prefab上書き設定");

        DrawProperty(node.FindPropertyRelative("bombPrefabOverride"), "通常爆弾Prefab上書き");
        DrawProperty(node.FindPropertyRelative("dudBombPrefabOverride"), "不発弾Prefab上書き");

        DrawSubTitle("弾の共通設定");

        DrawProperty(node.FindPropertyRelative("bombScale"), "弾の大きさ");
        DrawProperty(node.FindPropertyRelative("bombSpeed"), "弾の速度");
        DrawProperty(node.FindPropertyRelative("muzzleOffsetOverride"), "砲身先端までの距離");
        DrawProperty(node.FindPropertyRelative("upwardPower"), "少し上へ飛ばす力");

        DrawSubTitle("爆弾の物理設定");

        DrawProperty(node.FindPropertyRelative("minLinearDamping"), "Linear Damping 最小");
        DrawProperty(node.FindPropertyRelative("maxLinearDamping"), "Linear Damping 最大");

        DrawSubTitle("爆発設定");

        DrawProperty(node.FindPropertyRelative("explosionTime"), "爆発までの時間");
        DrawProperty(node.FindPropertyRelative("explosionRadius"), "爆発範囲");
        DrawProperty(node.FindPropertyRelative("damage"), "ダメージ");
        DrawProperty(node.FindPropertyRelative("explosionEffectScaleMultiplier"), "爆発エフェクト倍率");

        DrawProperty(node.FindPropertyRelative("useBlinkBeforeExplosion"), "爆発前に点滅する");

        SerializedProperty useBlinkBeforeExplosion =
            node.FindPropertyRelative("useBlinkBeforeExplosion");

        if (useBlinkBeforeExplosion != null && useBlinkBeforeExplosion.boolValue)
        {
            DrawProperty(node.FindPropertyRelative("blinkBeforeExplosionTime"), "爆発何秒前から点滅");
            DrawProperty(node.FindPropertyRelative("blinkInterval"), "点滅間隔");
            DrawProperty(node.FindPropertyRelative("blinkColor"), "点滅色");
        }

        DrawSubTitle("不発弾設定");

        DrawProperty(node.FindPropertyRelative("useDudBomb"), "不発弾を使う");

        SerializedProperty useDudBomb =
            node.FindPropertyRelative("useDudBomb");

        if (useDudBomb != null && useDudBomb.boolValue)
        {
            DrawProperty(node.FindPropertyRelative("dudChance"), "不発弾が出る確率");
            DrawProperty(node.FindPropertyRelative("dudOnlyOne"), "不発弾は1回の発射で1個だけ");
        }
    }

    void DrawAttack1Settings(SerializedProperty node)
    {
        DrawSubTitle("攻撃1：ランダム回転して止まってから発射");

        DrawProperty(node.FindPropertyRelative("randomRotateTime"), "ランダム回転時間");
        DrawProperty(node.FindPropertyRelative("randomAngles"), "ランダム角度一覧", true);
    }

    void DrawAttack2Settings(SerializedProperty node)
    {
        DrawSubTitle("攻撃2：回転しながら発射");

        DrawProperty(node.FindPropertyRelative("spinAttackTime"), "回転しながら攻撃する時間");
        DrawProperty(node.FindPropertyRelative("spinRotateSpeed"), "回転速度");
        DrawProperty(node.FindPropertyRelative("spinRotateDirection"), "回転方向");
        DrawProperty(node.FindPropertyRelative("spinFireInterval"), "発射間隔");
    }

    void DrawAttack3Settings(SerializedProperty node)
    {
        DrawSubTitle("攻撃3：ランダム空爆");

        DrawProperty(node.FindPropertyRelative("airStrikeCenterOverride"), "空爆中心位置");
        DrawProperty(node.FindPropertyRelative("airStrikeBombCount"), "空爆で落とす爆弾の数");
        DrawProperty(node.FindPropertyRelative("airStrikeInterval"), "空爆の爆弾を落とす間隔");
        DrawProperty(node.FindPropertyRelative("airStrikeHeight"), "空爆の高さ");
        DrawProperty(node.FindPropertyRelative("airStrikeFallSpeed"), "空爆の落下速度");
        DrawProperty(node.FindPropertyRelative("airStrikeMinDistance"), "空爆の最小距離");
        DrawProperty(node.FindPropertyRelative("airStrikeMaxDistance"), "空爆の最大距離");
    }

    void DrawAttack4Settings(SerializedProperty node)
    {
        DrawSubTitle("攻撃4：十字空爆");

        DrawProperty(node.FindPropertyRelative("crossAirStrikeCenterOverride"), "十字空爆中心位置");
        DrawProperty(node.FindPropertyRelative("crossAirStrikeBombCount"), "十字空爆の弾数");
        DrawProperty(node.FindPropertyRelative("crossAirStrikeDistance"), "十字空爆の距離");
        DrawProperty(node.FindPropertyRelative("crossAirStrikeAngleOffset"), "十字空爆の角度ずらし");
        DrawProperty(node.FindPropertyRelative("crossAirStrikeInterval"), "十字空爆の爆弾を落とす間隔");
        DrawProperty(node.FindPropertyRelative("crossAirStrikeHeight"), "十字空爆の高さ");
        DrawProperty(node.FindPropertyRelative("crossAirStrikeFallSpeed"), "十字空爆の落下速度");
    }

    void DrawAttack5Settings(SerializedProperty node)
    {
        DrawSubTitle("攻撃5：追尾ミサイル");

        DrawProperty(node.FindPropertyRelative("missileTargetOverride"), "ミサイルのターゲット");
        DrawProperty(node.FindPropertyRelative("missileCount"), "追尾ミサイルを撃つ数");
        DrawProperty(node.FindPropertyRelative("missileFireInterval"), "ミサイル発射間隔");
        DrawProperty(node.FindPropertyRelative("missileScale"), "ミサイルの大きさ");
        DrawProperty(node.FindPropertyRelative("missileSpeed"), "ミサイルの速度");
        DrawProperty(node.FindPropertyRelative("missileRotateSpeed"), "ミサイルが曲がる速さ");
        DrawProperty(node.FindPropertyRelative("missileHoming"), "プレイヤーを追尾する");
        DrawProperty(node.FindPropertyRelative("missileUseGravity"), "ミサイルに重力を使う");
        DrawProperty(node.FindPropertyRelative("missileExplosionTime"), "ミサイル爆発までの時間");
        DrawProperty(node.FindPropertyRelative("missileExplodeOnHit"), "ぶつかった時に爆発する");
        DrawProperty(node.FindPropertyRelative("missileExplodeOnlyPlayerHit"), "プレイヤーに当たった時だけ爆発");
        DrawProperty(node.FindPropertyRelative("missileExplosionRadius"), "ミサイル爆発範囲");
        DrawProperty(node.FindPropertyRelative("missileDamage"), "ミサイルダメージ");
        DrawProperty(node.FindPropertyRelative("missileExplosionEffectPrefab"), "ミサイル爆発エフェクト");
        DrawProperty(node.FindPropertyRelative("missileExplosionEffectScaleMultiplier"), "ミサイル爆発エフェクト倍率");
    }

    void DrawAttack6Settings(SerializedProperty node)
    {
        DrawSubTitle("攻撃6：回転弾幕");

        DrawProperty(node.FindPropertyRelative("bulletHellBulletPrefab"), "弾幕弾Prefab");
        DrawProperty(node.FindPropertyRelative("bulletHellFireGuns"), "弾を出す砲台", true);
        DrawProperty(node.FindPropertyRelative("bulletHellShootAxis"), "弾の発射方向");
        DrawProperty(node.FindPropertyRelative("bulletHellMuzzleOffset"), "砲身先端までの距離");
        DrawProperty(node.FindPropertyRelative("bulletHellSpawnHeightOffset"), "弾の発射位置上下補正");
        DrawProperty(node.FindPropertyRelative("bulletHellForceHorizontalDirection"), "弾を水平に飛ばす");
        DrawProperty(node.FindPropertyRelative("bulletHellDirectionHeightOffset"), "弾の進行方向上下補正");

        DrawSubTitle("回転弾幕の動き");

        DrawProperty(node.FindPropertyRelative("bulletHellAttackTime"), "弾幕攻撃時間");
        DrawProperty(node.FindPropertyRelative("bulletHellRotateSpeed"), "本体回転速度");
        DrawProperty(node.FindPropertyRelative("bulletHellRotateDirection"), "本体回転方向");
        DrawProperty(node.FindPropertyRelative("bulletHellFireInterval"), "弾の発射間隔");
        DrawProperty(node.FindPropertyRelative("bulletHellShotCount"), "弾の発射回数");

        DrawSubTitle("弾幕弾の設定");

        DrawProperty(node.FindPropertyRelative("bulletHellBulletSpeed"), "弾の速度");
        DrawProperty(node.FindPropertyRelative("bulletHellBulletScale"), "弾の大きさ");
        DrawProperty(node.FindPropertyRelative("bulletHellBulletLifeTime"), "弾が消え始めるまでの時間");
        DrawProperty(node.FindPropertyRelative("bulletHellBulletShrinkTime"), "弾が小さくなって消える時間");
        DrawProperty(node.FindPropertyRelative("bulletHellDamage"), "弾のダメージ");
        DrawProperty(node.FindPropertyRelative("bulletHellDestroyOnPlayerHit"), "プレイヤーに当たったら消える");

        DrawSubTitle("発射前の砲台点滅");

        DrawProperty(node.FindPropertyRelative("bulletHellBlinkBeforeFire"), "発射前に砲台を点滅");

        SerializedProperty blink =
            node.FindPropertyRelative("bulletHellBlinkBeforeFire");

        if (blink != null && blink.boolValue)
        {
            DrawProperty(node.FindPropertyRelative("bulletHellBlinkTime"), "発射前に点滅する時間");
            DrawProperty(node.FindPropertyRelative("bulletHellBlinkInterval"), "点滅間隔");
            DrawProperty(node.FindPropertyRelative("bulletHellBlinkColor"), "点滅色");
        }
    }

    void DrawProperty(
        SerializedProperty property,
        string label,
        bool includeChildren = false
    )
    {
        if (property == null)
        {
            return;
        }

        GUIContent content = new GUIContent(label);

        EditorGUILayout.PropertyField(
            property,
            content,
            includeChildren
        );
    }

    void DrawSubTitle(string title)
    {
        Space();

        EditorGUILayout.LabelField(
            title,
            normalLabelStyle
        );
    }

    void DrawNormalText(string text)
    {
        EditorGUILayout.LabelField(
            text,
            wordWrapLabelStyle
        );
    }

    void Space()
    {
        EditorGUILayout.Space(8);
    }

    void SpaceSmall()
    {
        EditorGUILayout.Space(4);
    }
}

#endif