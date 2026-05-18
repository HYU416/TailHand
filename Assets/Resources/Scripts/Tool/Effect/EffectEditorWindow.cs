using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

public class EffectEditorWindow : EditorWindow
{
    //イベントのBOXのサイズや配置に関する定数
    private const float EVENT_HEIGHT = 20f;
    private const float EVENT_SPACING = 28f;
    private const float EVENT_Y_OFFSET = 35f;
    private const float EVENT_MIN_WIDTH = 12f;

    //====エディタ上で使用する変数======
    private EffectDatabase database;
    private Vector2 rightScroll;      // Effect Info、Player Settings、Events セクション用
    private Vector2 timelineScroll;   // タイムラインビュー用
    private Vector2 eventScroll;      // イベントリスト用
    private float topPanelHeight = 500f;// 上部のEffect Infoなどが表示されるパネルの高さ
    private bool resizingPanels;// パネルのリサイズ中かどうか
    private int selectedIndex = -1; // 現在選択されているエフェクトのインデックス
    private EffectData currentData; // 現在選択されているエフェクトのデータ
    private EffectPlayer currentPlayer; // 現在選択されているエフェクトのプレイヤーコンポーネント
    private int previewFrame;// タイムラインのプレビューで表示しているフレーム
    private GameObject previewInstance;// タイムラインのプレビューで生成しているエフェクトのインスタンス
    private EffectEvent editHitEvent;// 現在編集している当たり判定イベントのデータ
    private int editingEventIndex = -1;// 現在編集しているイベントのインデックス
    private int selectedEventIndex = -1;// イベントリストで選択されているイベントのインデックス
    private List<bool> eventFoldouts = new();//イベントリストの各イベントの折りたたみ状態を管理するリスト
    private bool isPlaying;// タイムラインの再生中かどうか
    private bool loopPlayback = true;// タイムラインの再生がループするかどうか

    private double lastTime;// 最後に更新された時間
    private float playbackFrame;// タイムラインの再生中のフレーム
    private float playbackSpeed = 1f;// タイムラインの再生速度
    private Vector3 previewPosition;// タイムラインのプレビューでエフェクトを表示する位置

    //当たり判定イベントのカードの色を決めるための配列
    private readonly Color[] hitColors =
    {
       new Color(0.32f, 0.18f, 0.18f),        //濃赤
       new Color(0.18f, 0.18f, 0.32f),        //濃青
       new Color(0.18f, 0.32f, 0.18f),        //濃緑
       new Color(0.32f, 0.32f, 0.18f),        //濃黄
       new Color(0.32f, 0.18f, 0.32f),        //濃紫
       new Color(0.18f, 0.32f, 0.32f),        //濃水色
       new Color(0.35f, 0.25f, 0.18f),        //茶色
       new Color(0.25f, 0.35f, 0.18f),        //黄緑
       new Color(0.18f, 0.25f, 0.35f),        //青灰
       new Color(0.35f, 0.18f, 0.25f),        //ワインレッド
       new Color(0.25f, 0.18f, 0.35f),        //藍紫
       new Color(0.18f, 0.35f, 0.25f),        //エメラルド
    };

    //エフェクトエディタウィンドウを開くためのメニューアイテム
    [MenuItem("Tools/Effect Editor")]
    public static void Open()
    {
        GetWindow<EffectEditorWindow>("Effect Editor");
    }

    //エディタウィンドウが有効になったときの処理
    private void OnEnable()
    {
        //SceneビューのGUIイベントとエディタの更新イベントにコールバックを登録
        SceneView.duringSceneGui += OnSceneGUI;
        EditorApplication.update += UpdatePlayback;

        //プロジェクト内からEffectDatabaseアセットを検索してロード
        string[] guids = AssetDatabase.FindAssets("t:EffectDatabase");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            database = AssetDatabase.LoadAssetAtPath<EffectDatabase>(path);
        }
    }

    //エディタウィンドウが無効になったときの処理
    private void OnDisable()
    {
        //SceneビューのGUIイベントとエディタの更新イベントからコールバックを解除
        SceneView.duringSceneGui -= OnSceneGUI;
        EditorApplication.update -= UpdatePlayback;

        //プレビューインスタンスが存在する場合は破棄
        if (previewInstance != null)
        {
            DestroyImmediate(previewInstance);
        }
    }

    //GUIの描画処理
    private void OnGUI()
    {
        //efectDatabaseの描画
        DrawDatabaseField();

        //データベースがない場合は警告を表示して処理を中断
        if (database == null)
        {
            EditorGUILayout.HelpBox("EffectDatabase Missing", MessageType.Warning);
            return;
        }

        //エフェクト選択のドロップダウンを描画
        DrawEffectSelect();

        if (currentData == null)
            return;

        //右側のスクロールビューを開始
        rightScroll = EditorGUILayout.BeginScrollView(rightScroll, GUILayout.Height(topPanelHeight));

        //エフェクトの情報を描画
        DrawEffectInfo();

        //現在のエフェクトプレイヤーが存在する場合は、プレイヤー設定とイベントリストを描画
        if (currentPlayer != null)
        {
            DrawPlayerSettings();
            DrawEventList();
        }

        EditorGUILayout.EndScrollView();

        //ドラッグでパネルの高さを調整するためのバーを描画
        DrawResizeBar();

        //タイムラインパネルを描画
        DrawTimelinePanel();
    }

    //シーンに当たり判定のプレビューを描画するための処理
    private void OnSceneGUI(SceneView sceneView)
    {
        if (editHitEvent == null || previewInstance == null)
            return;

        //色を設定して、プレビューインスタンスのローカルからワールドへの変換行列を取得
        Handles.color = editHitEvent.previewColor;
        Matrix4x4 matrix = previewInstance.transform.localToWorldMatrix;

        //当たり判定イベントのコライダーの種類に応じて、対応するプレビュー描画関数を呼び出す
        using (new Handles.DrawingScope(matrix))
        {
            switch (editHitEvent.colliderType)
            {
                case HitColliderType.Sphere:
                    DrawSpherePreview();
                    break;
                case HitColliderType.Box:
                    DrawBoxPreview();
                    break;
                case HitColliderType.Capsule:
                    DrawCapsulePreview();
                    break;
            }
        }

        SceneView.RepaintAll();
    }

    //EffectDatabaseのフィールドを描画する関数
    private void DrawDatabaseField()
    {
        //データベースのラベルを表示
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Database", EditorStyles.boldLabel);
        database = (EffectDatabase)EditorGUILayout.ObjectField(database, typeof(EffectDatabase), false);
        EditorGUILayout.EndVertical();
        GUILayout.Space(5);
    }

    //当たり判定イベントのスフィアコライダーのプレビューを描画する関数
    private void DrawSpherePreview()
    {
        //スフィアの中心位置を編集するためのハンドルを描画
        Vector3 center = editHitEvent.hitOffset;
        DrawPositionHandle(ref center, "Edit Sphere Position");
        //スフィアの半径を編集するためのハンドルを描画
        float radius = Handles.RadiusHandle(Quaternion.identity, center, editHitEvent.hitRadius);

        //半径が変更された場合は、Undoを記録して値を更新
        if (radius != editHitEvent.hitRadius)
        {
            RecordUndo("Edit Sphere Radius");
            editHitEvent.hitRadius = radius;
            Repaint();
        }

        editHitEvent.hitOffset = center;

        //スフィアのワイヤーフレームを描画
        Handles.DrawWireDisc(center, Vector3.up, radius);
        Handles.DrawWireDisc(center, Vector3.right, radius);
        Handles.DrawWireDisc(center, Vector3.forward, radius);
    }

    //当たり判定イベントのボックスコライダーのプレビューを描画する関数
    private void DrawBoxPreview()
    {
        //色を保存して、プレビューの色を設定
        Color oldColor = Handles.color;
        Handles.color = editHitEvent.previewColor;

        EditorGUI.BeginChangeCheck();

        //ボックス中心位置の移動ハンドル
        Vector3 center = Handles.PositionHandle(editHitEvent.hitOffset, Quaternion.identity);
        //サイズ変更ハンドル
        Vector3 size = Handles.ScaleHandle(editHitEvent.hitBoxSize, center, Quaternion.identity, HandleUtility.GetHandleSize(center));
        //変更時、位置、サイズを保存
        if (EditorGUI.EndChangeCheck())
        {

            editHitEvent.hitOffset = center;
            editHitEvent.hitBoxSize = size;
            Repaint();
        }

        //ボックスのワイヤーフレームを描画
        Handles.DrawWireCube(center, size);
        Handles.color = oldColor;
    }

    //当たり判定イベントのカプセルコライダーのプレビューを描画する関数
    private void DrawCapsulePreview()
    {
        //色を保存して、プレビューの色を設定
        Color oldColor = Handles.color;
        Handles.color = editHitEvent.previewColor;

        //カプセルの中心位置、半径、高さを計算
        Vector3 center = editHitEvent.hitOffset;
        float radius = editHitEvent.capsuleRadius;
        float height = editHitEvent.capsuleHeight;
        //カプセルの向きを決める軸と、その軸に垂直な2つの軸を取得
        Vector3 axis = GetCapsuleAxis(editHitEvent.capsuleDirection);
        (Vector3 crossA, Vector3 crossB) = GetCapsuleCrossAxes(editHitEvent.capsuleDirection);

        EditorGUI.BeginChangeCheck();
        //カプセル全体を移動するハンドル
        center = Handles.PositionHandle(center, Quaternion.identity);
        //radius = Handles.RadiusHandle(Quaternion.identity, center, radius);

        //円柱部分の半分の長さ
        float bodyHalf = Mathf.Max(0, (height * 0.5f) - radius);
        //上端位置
        Vector3 top = center + axis * bodyHalf;
        //下端位置
        Vector3 bottom = center - axis * bodyHalf;

        //上下を引っ張って高さ変更
        top = Handles.Slider(top, axis);
        bottom = Handles.Slider(bottom, -axis);

        //編集結果を保存
        if (EditorGUI.EndChangeCheck())
        {
            editHitEvent.hitOffset = center;
            editHitEvent.capsuleRadius = radius;
            editHitEvent.capsuleHeight = Vector3.Distance(top, bottom) + radius * 2f;
            Repaint();
        }

        //カプセルのワイヤーフレームを描画
        DrawCapsuleWireframe(center, radius, axis, crossA, crossB);
        Handles.color = oldColor;
    }

    //カプセルの向きを決める軸を取得する関数
    private Vector3 GetCapsuleAxis(CapsuleDirection direction)
    {
        //カプセルの向きを決める軸を、指定された方向に応じて返す
        switch (direction)
        {
            case CapsuleDirection.X:
                return Vector3.right;

            case CapsuleDirection.Y:
                return Vector3.up;

            case CapsuleDirection.Z:
                return Vector3.forward;

            default:
                return Vector3.up;
        }
    }

    // カプセル断面用の2軸を取得する関数
    private (Vector3, Vector3) GetCapsuleCrossAxes(CapsuleDirection direction)
    {
        return direction switch
        {
            CapsuleDirection.X => (Vector3.up, Vector3.forward),
            CapsuleDirection.Y => (Vector3.right, Vector3.forward),
            CapsuleDirection.Z => (Vector3.right, Vector3.up),
            _ => (Vector3.right, Vector3.forward)
        };
    }

    //カプセル全体のワイヤーフレームを描画する関数
    private void DrawCapsuleWireframe(
     Vector3 center,
     float radius,
     Vector3 axis,
     Vector3 crossA,
     Vector3 crossB
 )
    {
        //円柱部分の半分の長さを計算
        float bodyHalf = Mathf.Max(0,(editHitEvent.capsuleHeight * 0.5f) - radius);
        //円柱の上端と下端の位置を計算
        Vector3 top =center + axis * bodyHalf;
        Vector3 bottom =center - axis * bodyHalf;
        //カプセルの断面を描画するための分割数
        int segment = 24;

        
        // 側面の縦ライン
        for (int i = 0; i < segment; i++)
        {
            //断面上の点を計算するための角度を計算
            float angle =(Mathf.PI * 2f / segment) * i;
            //断面上の点を計算するための方向ベクトルを、crossAとcrossBを使って計算
            Vector3 dir =Mathf.Cos(angle) * crossA +Mathf.Sin(angle) * crossB;
            //円柱の側面のラインを描画するための、上端と下端の点を計算
            Vector3 topPoint =top + dir * radius;
            Vector3 bottomPoint = bottom + dir * radius;
            //円柱の側面のラインを描画
            Handles.DrawLine(topPoint,bottomPoint);
        }

        // 上半球
        DrawHemisphere( top,radius, axis, crossA,crossB,true);

       // 下半球
        DrawHemisphere( bottom,radius, axis,crossA,crossB, false);
    }

    private void DrawHemisphere(
        Vector3 center,
        float radius,
        Vector3 axis,
        Vector3 crossA,
        Vector3 crossB,
        bool upper
    )
    {
        //カプセルの半球部分を描画するための分割数
        int horizontal = 24;
        int vertical = 6;

        // 横リング

        //上半球は赤道から極に向かって、下半球は赤道から底面に向かって描画するため、開始Yを2にして少し上から描き始める
        int startY = 2;
        //赤道から極/底面に向かって、verticalの分割数に応じてループしてリングを描画
        for (int y = startY; y <= vertical; y++)
        {
            //赤道からの割合を計算
            float v = y / (float)vertical;
            //赤道からの割合をもとに、リングの半径と高さを計算
            float theta = v * Mathf.PI * 0.5f;
            float ringRadius = Mathf.Cos(theta) * radius;
            float height = Mathf.Sin(theta) * radius;
            //下半球の場合は高さを反転
            if (!upper) height = -height;
            //リングの中心位置を、カプセルの中心から軸方向に高さ分移動した位置に設定
            Vector3 ringCenter = center + axis * height;
            //リング上の点を計算するための前の点を保存する変数と、最初の点かどうかを判定するフラグ
            Vector3 prev = Vector3.zero;
            bool first = true;
            //リング上の点を、horizontalの分割数に応じてループして計算し、ラインを描画                                         
            for (int x = 0; x <= horizontal; x++)
            {
                //断面上の点を計算するための角度を計算
                float angle =(x / (float)horizontal) *Mathf.PI * 2f;
                //断面上の点を計算するための方向ベクトルを、crossAとcrossBを使って計算
                Vector3 dir = Mathf.Cos(angle) * crossA + Mathf.Sin(angle) * crossB;
                //リング上の点を、リングの中心から方向ベクトルにリングの半径を掛けた位置に設定
                Vector3 point = ringCenter +dir * ringRadius;
                //最初の点でなければ、前の点から現在の点にラインを描画
                if (!first)
                {
                    Handles.DrawLine( prev,point);
                }
                //前の点を現在の点に更新して、最初の点フラグをfalseに設定
                prev = point;
                first = false;
            }
        }

       // 縦ライン
        for (int x = 0; x < horizontal; x++)
        {
            //断面上の点を計算するための角度を計算
            float angle = (x / (float)horizontal) * Mathf.PI * 2f;
            //断面上の点を計算するための方向ベクトルを、crossAとcrossBを使って計算
            Vector3 dir = Mathf.Cos(angle) * crossA + Mathf.Sin(angle) * crossB;
            //縦ラインを描画するための前の点を保存する変数と、最初の点かどうかを判定するフラグ
            Vector3 prev = Vector3.zero;
            bool first = true;
            //赤道から極/底面に向かって、verticalの分割数に応じてループして縦ラインを描画
            for (int y = 0; y <= vertical; y++)
            {
                //赤道からの割合を計算
                float v =  y / (float)vertical;
                //赤道からの割合をもとに、リングの半径と高さを計算
                float theta =v * Mathf.PI * 0.5f;
                //リングの半径は、赤道からの割合に応じて、カプセルの半径を掛けた値に設定
                float ringRadius =Mathf.Cos(theta) * radius;
                //リングの高さは、赤道からの割合に応じて、カプセルの半径を掛けた値に設定
                float height =Mathf.Sin(theta) * radius;
                //下半球の場合は高さを反転
                if (!upper)height = -height;
                //リングの中心位置を、カプセルの中心から軸方向に高さ分移動した位置に設定
                Vector3 ringCenter =center + axis * height;
                //リング上の点を、リングの中心から方向ベクトルにリングの半径を掛けた位置に設定
                Vector3 point = ringCenter +dir * ringRadius;
                //最初の点でなければ、前の点から現在の点にラインを描画
                if (!first)
                {
                    Handles.DrawLine(prev, point);
                }
                //前の点を現在の点に更新して、最初の点フラグをfalseに設定
                prev = point;
                first = false;
            }
        }
    }

    // エフェクト選択UIを描画する関数
    private void DrawEffectSelect()
    {
        //存在しないEffectTypeを追加
        EnsureAllEffectTypesExist();

        //Popup表示用の名前配列
        string[] names = new string[database.effects.Count];
        //EffectType名を配列へ格納
        for (int i = 0; i < database.effects.Count; i++)
        {
            names[i] = database.effects[i].type.ToString();
        }

        //エフェクト選択Popup
        selectedIndex = EditorGUILayout.Popup("Effect", selectedIndex, names);

        //有効なインデックス時
        if (selectedIndex >= 0 && selectedIndex < database.effects.Count)
        {
            //現在選択中のEffectData取得
            currentData = database.effects[selectedIndex];
            //EffectPlayer取得
            if (currentData.prefab != null)
            {
                currentPlayer = currentData.prefab.GetComponent<EffectPlayer>();
            }
        }
    }

    // EffectType列挙体の全要素がDatabaseに存在するようにする関数
    private void EnsureAllEffectTypesExist()
    {
        //EffectType列挙体の全値取得
        EffectType[] allTypes = (EffectType[])System.Enum.GetValues(typeof(EffectType));
        //全EffectTypeを走査
        foreach (EffectType type in allTypes)
        {
            bool exists = false;
            //既存データ確認
            foreach (EffectData data in database.effects)
            {
                if (data.type == type)
                {
                    exists = true;
                    break;
                }
            }
            //存在しない場合
            if (!exists)
            {
                //新規EffectData作成
                EffectData data = new EffectData { type = type };
                //Databaseへ追加
                database.effects.Add(data);
            }
        }
    }

    // Effect情報UIを描画する関数
    private void DrawEffectInfo()
    {
        EditorGUILayout.BeginVertical("box");
        //セクションのタイトルを表示
        GUILayout.Label("Effect Info", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        //Prefabフィールドを表示して、ユーザーがエフェクトのPrefabを割り当てられるようにする
        currentData.prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", currentData.prefab, typeof(GameObject), false);

        //エフェクトのターゲットタグを設定するためのタグフィールドを表示
        currentData.targetTag = EditorGUILayout.TagField("Target Tag", currentData.targetTag);

        //エフェクトの再生速度を設定するためのフロートフィールドを表示
        currentData.playSpeed = EditorGUILayout.FloatField( "Play Speed", currentData.playSpeed);

        //変更があった場合は、データベースを保存してアセットを更新
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(database);

            AssetDatabase.SaveAssets();
        }

        //Setupボタン
        if (GUILayout.Button("Setup Effect", GUILayout.Height(40)))
        {
            SetupEffect();
        }

        EditorGUILayout.EndVertical();
    }

    // EffectPrefabを初期設定する関数
    private void SetupEffect()
    {
        if (currentData.prefab == null)
            return;

        //Prefabパス取得
        string path = AssetDatabase.GetAssetPath(currentData.prefab);
        //Prefab編集用ロード
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        //EffectPlayer取得
        EffectPlayer player = root.GetComponent<EffectPlayer>();
        //存在しない場合追加
        if (player == null)
        {
            player = root.AddComponent<EffectPlayer>();
        }
        //子階層からParticleSystem取得
        ParticleSystem particle = root.GetComponentInChildren<ParticleSystem>();
        //SerializedObject作成
        SerializedObject so = new SerializedObject(player);
        //mainParticle設定
        so.FindProperty("mainParticle").objectReferenceValue = particle;
        //変更適用
        so.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        //Prefab,EffectPlayer再取得
        currentData.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        currentPlayer = currentData.prefab.GetComponent<EffectPlayer>();
    }

    // EffectPlayer設定UIを描画する関数
    private void DrawPlayerSettings()
    {
        EditorGUILayout.BeginVertical("box");
        //タイトル表示
        GUILayout.Label("Player Settings", EditorStyles.boldLabel);
        //EffectPlayerをSerializedObject化
        SerializedObject so = new SerializedObject(currentPlayer);
        so.Update();
        //FrameRate表示
        EditorGUILayout.PropertyField(so.FindProperty("frameRate"));
        //mainParticle表示
        EditorGUILayout.PropertyField(so.FindProperty("mainParticle"));
        //変更適用
        so.ApplyModifiedProperties();
        EditorGUILayout.EndVertical();
    }

    // イベント一覧UIを描画する関数
    private void DrawEventList()
    {
        EditorGUILayout.BeginVertical("box");
        //タイトル表示
        GUILayout.Label("Events", EditorStyles.boldLabel);
        //イベント一覧スクロール開始
        eventScroll = EditorGUILayout.BeginScrollView(eventScroll, GUILayout.Height(position.height * 0.45f));
        //EffectPlayerをSerializedObject化
        SerializedObject so = new SerializedObject(currentPlayer);
        //events配列取得
        SerializedProperty events = so.FindProperty("events");
        //Foldout数不足時追加
        while (eventFoldouts.Count < events.arraySize)
        {
            eventFoldouts.Add(true);
        }
        //イベント操作ボタン,イベント一覧の描画
        DrawEventListButtons(events);
        DrawEventItems(events, so);

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    // イベント一覧操作ボタンを描画する関数
    private void DrawEventListButtons(SerializedProperty events)
    {
        // Addボタン
        if (GUILayout.Button("Add"))
        {
            //イベント追加のUndoを記録
            RecordUndo("Add Event");
            events.InsertArrayElementAtIndex(events.arraySize);
            eventFoldouts.Add(true);
        }
        // Sortボタン
        if (GUILayout.Button("Sort"))
        {
            //イベントソートのUndoを記録
            RecordUndo("Sort Events");
            currentPlayer.Events.Sort((a, b) => a.frame.CompareTo(b.frame));
            EditorUtility.SetDirty(currentPlayer);
        }
    }

    // イベント一覧を描画する関数
    private void DrawEventItems(SerializedProperty events, SerializedObject so)
    {
        //全イベント走査
        for (int i = 0; i < events.arraySize; i++)
        {
            //現在イベント取得
            SerializedProperty e = events.GetArrayElementAtIndex(i);
            //イベントの種類を取得
            SerializedProperty typeProp = e.FindPropertyRelative("type");
            EffectEventType eventType = (EffectEventType)typeProp.enumValueIndex;

            //イベントの種類に応じてカードの色を決定
            int hitId = eventType == EffectEventType.Hit ? e.FindPropertyRelative("hitId").intValue : 0;
            Color cardColor = GetEventColor(eventType, hitId);

            // // Hitイベント
            if (eventType == EffectEventType.Hit)
            {
                //イベントの当たり判定IDに応じて、カードの色をhitColors配列から取得
                e.FindPropertyRelative("previewColor").colorValue = cardColor;
                e.FindPropertyRelative("hitTag").stringValue = currentData.targetTag;
            }
            // 選択中イベント強調
            if (selectedEventIndex == i)
            {
                cardColor *= 1.35f;
            }
            //イベントカード描画
            bool remove = DrawEventCard(e, eventType, cardColor, i);
            // 削除処理
            if (remove)
            {
                //イベント削除のUndoを記録
                RecordUndo("Delete Event");
                events.DeleteArrayElementAtIndex(i);
                eventFoldouts.RemoveAt(i);
                break;
            }
           
        }

        // 変更適用
        if (so.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(currentPlayer);
        }
    }

    // イベントカードを描画する関数
    private bool DrawEventCard(SerializedProperty e, EffectEventType eventType, Color cardColor, int index)
    {
        GUI.backgroundColor = cardColor;

        if (eventFoldouts[index])
        {
            EditorGUILayout.BeginVertical("box");
        }
        else
        {
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Height(24));
        }

        GUI.backgroundColor = Color.white;
        //frame取得
        SerializedProperty frameProp = e.FindPropertyRelative("frame");
        //endFrame取得
        SerializedProperty endFrameProp = e.FindPropertyRelative("endFrame");

        //開始フレーム
        int startFrame = frameProp.intValue;
        //終了フレーム
        int endFrame = endFrameProp.intValue;
        //Hitイベント時のみhitId取得
        int hitId = eventType == EffectEventType.Hit ? e.FindPropertyRelative("hitId").intValue : 0;
        //タイトル作成
        string title = eventType == EffectEventType.Hit
            ? $"Hit {hitId} [{startFrame} - {endFrame}]"
            : $"Frame {startFrame}";

        //描画
        Rect foldRect = GUILayoutUtility.GetRect(20, 22);
        EditorGUI.DrawRect(foldRect, cardColor);
        eventFoldouts[index] = EditorGUI.Foldout(foldRect, eventFoldouts[index], title, true);

        if (!eventFoldouts[index])
        {
            GUILayout.EndVertical();
            return false;
        }

        EditorGUILayout.Space(4);

        // Hitイベント
        if (eventType == EffectEventType.Hit)
        {
            //開始フレーム編集
            int newFrame = EditorGUILayout.IntField("Start", startFrame);
            //変更時Undo記録して値更新
            if (newFrame != startFrame)
            {
                RecordUndo("Edit Start Frame");
                frameProp.intValue = newFrame;
            }
            endFrameProp.intValue = EditorGUILayout.IntField("End", endFrameProp.intValue);
        }
        else
        {
            //通常イベントframe表示
            EditorGUILayout.PropertyField(frameProp);
        }

        //イベントType表示
        EditorGUILayout.PropertyField(e.FindPropertyRelative("type"));
        //イベント詳細描画
        DrawEventFields(e, eventType, index);
        //削除ボタン
        bool remove = GUILayout.Button("Delete");

        EditorGUILayout.EndVertical();

        return remove;
    }

    // タイムライン全体を描画する関数
    private void DrawTimelinePanel()
    {
        if (currentPlayer == null)
            return;

        GUILayout.Space(10);
        EditorGUILayout.BeginVertical("box");
        //再生UI描画
        DrawPlaybackControls();
        //タイムライン設定UI描画
        DrawTimelineControls();
        //タイムラインビュー描画
        DrawTimelineView();

        EditorGUILayout.EndVertical();
    }

    // 再生UIを描画する関数
    private void DrawPlaybackControls()
    {
        EditorGUILayout.BeginHorizontal();


        if (!isPlaying)
        {
            //再生ボタン
            if (GUILayout.Button("Play", GUILayout.Width(70), GUILayout.Height(24)))
            {
                StartPlayback();
            }
        }
        else
        {
            //停止ボタン
            if (GUILayout.Button("Stop", GUILayout.Width(70), GUILayout.Height(24)))
            {
                isPlaying = false;
            }
        }

        GUILayout.Space(10);
        //現在フレーム表示
        GUILayout.Label($"Frame : {previewFrame}", GUILayout.Width(100));
        
        
        GUILayout.FlexibleSpace();
        //プレビューボタン
        if (GUILayout.Button("Preview", GUILayout.Width(80)))
        {
            PreviewFrame(previewFrame);
        }

        EditorGUILayout.EndHorizontal();
    }

    // タイムライン再生開始処理
    private void StartPlayback()
    {
        isPlaying = true;
        //最大フレーム取得
        int playbackMaxFrame = GetMaxFrame();
        if (previewFrame >= playbackMaxFrame)
        {
            previewFrame = 0;
            playbackFrame = 0;
        }
        //現在時間保存
        lastTime = EditorApplication.timeSinceStartup;
        //既存プレビュー削除
        if (previewInstance != null)
        {
            DestroyImmediate(previewInstance);
        }
        //Prefab生成
        previewInstance = Instantiate(currentData.prefab);
        previewInstance.hideFlags = HideFlags.HideAndDontSave;

        // 子オブジェクトを含むすべてのパーティクルシステムを取得
        ParticleSystem[] allParticles = previewInstance.GetComponentsInChildren<ParticleSystem>();
        
        if (allParticles != null && allParticles.Length > 0)
        {
            // メインパーティクルからループ設定を取得（最初のパーティクル）
            loopPlayback = allParticles[0].main.loop;
            
            // すべてのパーティクルシステムを初期化して再生開始
            foreach (ParticleSystem particle in allParticles)
            {
                if (particle != null)
                {
                    particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    particle.Simulate(previewFrame / (float)currentPlayer.FrameRate, true, true, true);
                }
            }
        }
    }

    // タイムライン設定UIを描画する関数
    private void DrawTimelineControls()
    {
        //再生速度
        playbackSpeed = EditorGUILayout.Slider("Speed", playbackSpeed, 0.1f, 100f);
        //再生速度保存
        currentPlayer.PlaySpeed = playbackSpeed;

        EditorUtility.SetDirty(currentPlayer);

        int maxFrame = GetMaxFrame();
        //プレビューフレーム
        previewFrame = EditorGUILayout.IntSlider("Preview Frame", previewFrame, 0, maxFrame);
        //プレビュー位置
        previewPosition = EditorGUILayout.Vector3Field("Preview Position",previewPosition );
        if (previewInstance != null)
        {
            //位置更新
            previewInstance.transform.position = previewPosition;
        }
    }

    // タイムラインスクロールビュー描画
    private void DrawTimelineView()
    {
        //スクロール開始
        timelineScroll = EditorGUILayout.BeginScrollView(timelineScroll, GUILayout.Height(320));
        //タイムライン本体描画
        DrawTimeline();
        EditorGUILayout.EndScrollView();
    }

    // イベント種類ごとの色を取得する関数
    private Color GetEventColor(EffectEventType type, int hitId)
    {
        return type switch
        {
            EffectEventType.Hit => hitColors[Mathf.Abs(hitId) % hitColors.Length],
            EffectEventType.Sound => new Color(0.8f, 0.8f, 0.2f),
            EffectEventType.CameraShake => new Color(0.2f, 0.8f, 0.8f),
            EffectEventType.Function => new Color(0.8f, 0.2f, 0.8f),
            _ => Color.gray
        };
    }

    private void DrawResizeBar()
    {
        Rect resizeRect =
            GUILayoutUtility.GetRect(
                position.width,
                6f
            );

        EditorGUI.DrawRect(
            resizeRect,
            new Color(0.15f, 0.15f, 0.15f)
        );

        EditorGUIUtility.AddCursorRect(
            resizeRect,
            MouseCursor.ResizeVertical
        );

        Event e = Event.current;

        switch (e.type)
        {
            case EventType.MouseDown:

                if (resizeRect.Contains(e.mousePosition))
                {
                    resizingPanels = true;
                }

                break;

            case EventType.MouseDrag:

                if (resizingPanels)
                {
                    topPanelHeight += e.delta.y;

                    topPanelHeight =
                        Mathf.Clamp(
                            topPanelHeight,
                            200f,
                            position.height - 200f
                        );

                    Repaint();
                }

                break;

            case EventType.MouseUp:

                resizingPanels = false;

                break;
        }
    }

    private void DrawTimeline()
    {
        GUILayout.Label("Timeline", EditorStyles.boldLabel);

        SerializedObject so = new SerializedObject(currentPlayer);
        SerializedProperty events = so.FindProperty("events");

        int maxFrame = CalculateMaxFrame(events);
        float extraWidth = Mathf.Max(0, GetMaxFrame() - 60) * 30;
        Rect rect = GUILayoutUtility.GetRect(1200 + extraWidth, 60 + events.arraySize * 28);

        EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f));

        float frameWidth = rect.width / Mathf.Max(1, maxFrame);

        if (isPlaying)
        {
            AutoScrollTimeline(rect, frameWidth);
        }

        bool clickedEvent = HandleEventClicks(events, rect, frameWidth);
        DrawPlayhead(rect, frameWidth);
        DrawTimelineGrid(rect, frameWidth, maxFrame);
        HandleTimelineClick(rect, frameWidth, maxFrame, clickedEvent);
        DrawTimelineEvents(events, rect, frameWidth);
    }

    private int CalculateMaxFrame(SerializedProperty events)
    {
        int maxFrame = 60;
        for (int i = 0; i < events.arraySize; i++)
        {
            SerializedProperty e = events.GetArrayElementAtIndex(i);
            int startFrame = e.FindPropertyRelative("frame").intValue;
            int endFrame = e.FindPropertyRelative("endFrame").intValue;
            maxFrame = Mathf.Max(maxFrame, startFrame, endFrame, GetMaxFrame());
        }
        return maxFrame;
    }

    private void AutoScrollTimeline(Rect rect, float frameWidth)
    {
        float autoScrollPlayheadX = rect.x + previewFrame * frameWidth;
        float viewWidth = position.width - 40;
        timelineScroll.x = Mathf.Max(0, autoScrollPlayheadX - viewWidth * 0.5f);
    }

    private bool HandleEventClicks(SerializedProperty events, Rect rect, float frameWidth)
    {
        bool clickedEvent = false;

        for (int i = 0; i < events.arraySize; i++)
        {
            SerializedProperty e = events.GetArrayElementAtIndex(i);
            Rect eventRect = GetEventRect(e, rect, frameWidth, i);
            Event current = Event.current;

            if (current.type == EventType.MouseDown && eventRect.Contains(current.mousePosition))
            {
                clickedEvent = true;
                selectedEventIndex = i;

                if (current.clickCount == 2)
                {
                    editingEventIndex = i;
                    editHitEvent = currentPlayer.Events[i];
                    previewFrame = e.FindPropertyRelative("frame").intValue;
                    PreviewFrame(previewFrame);
                }

                Repaint();
                current.Use();
            }
        }

        return clickedEvent;
    }

    private void DrawPlayhead(Rect rect, float frameWidth)
    {
        float playheadX = rect.x + previewFrame * frameWidth;

        EditorGUI.DrawRect(new Rect(playheadX, rect.y, 2, rect.height), Color.red);

        Handles.BeginGUI();
        Handles.color = Color.red;

        Vector3[] triangle =
        {
            new Vector3(playheadX - 6, rect.y),
            new Vector3(playheadX + 6, rect.y),
            new Vector3(playheadX, rect.y + 10)
        };

        Handles.DrawAAConvexPolygon(triangle);
        Handles.EndGUI();

        GUI.Label(new Rect(playheadX + 4, rect.y + 10, 40, 20), previewFrame.ToString());
    }

    private void DrawTimelineGrid(Rect rect, float frameWidth, int maxFrame)
    {
        for (int i = 0; i <= maxFrame; i++)
        {
            float x = rect.x + frameWidth * i;
            EditorGUI.DrawRect(new Rect(x, rect.y, 1, rect.height), new Color(0.25f, 0.25f, 0.25f));
            GUI.Label(new Rect(x + 2, rect.y + 2, 30, 20), i.ToString());
        }
    }

    private void HandleTimelineClick(Rect rect, float frameWidth, int maxFrame, bool clickedEvent)
    {
        if (!clickedEvent && Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
        {
            float localX = Event.current.mousePosition.x - rect.x;
            previewFrame = Mathf.Clamp(Mathf.RoundToInt(localX / frameWidth), 0, maxFrame);
            PreviewFrame(previewFrame);
            Repaint();
            Event.current.Use();
        }
    }

    private void DrawTimelineEvents(SerializedProperty events, Rect rect, float frameWidth)
    {
        for (int i = 0; i < events.arraySize; i++)
        {
            SerializedProperty e = events.GetArrayElementAtIndex(i);
            EffectEventType eventType = (EffectEventType)e.FindPropertyRelative("type").enumValueIndex;
            int hitId = eventType == EffectEventType.Hit ? e.FindPropertyRelative("hitId").intValue : 0;
            int frame = e.FindPropertyRelative("frame").intValue;

            Color color = GetEventColor(eventType, hitId);
            Rect eventRect = GetEventRect(e, rect, frameWidth, i);

            bool selected = selectedEventIndex == i;
            bool editing = editingEventIndex == i;

            EditorGUI.DrawRect(eventRect, color);

            if (selected || editing)
            {
                DrawEventOutline(eventRect, editing);
            }

            float y = rect.y + EVENT_Y_OFFSET + i * EVENT_SPACING;
            GUI.Label(new Rect(5, y - 2, 80, 20), eventType == EffectEventType.Hit ? $"Hit{hitId}" : eventType.ToString());
        }
    }

    private void DrawEventOutline(Rect eventRect, bool editing)
    {
        Handles.BeginGUI();

        Color old = Handles.color;
        Handles.color = editing ? Color.green : Color.yellow;

        Vector3[] lines =
        {
            new Vector3(eventRect.x, eventRect.y),
            new Vector3(eventRect.xMax, eventRect.y),
            new Vector3(eventRect.xMax, eventRect.yMax),
            new Vector3(eventRect.x, eventRect.yMax),
            new Vector3(eventRect.x, eventRect.y)
        };

        Handles.DrawAAPolyLine(3f, lines);
        Handles.color = old;
        Handles.EndGUI();
    }

    private void DrawEventFields(SerializedProperty e, EffectEventType type, int index)
    {
        switch (type)
        {
            case EffectEventType.Hit:
                DrawHitEvent(e, index);
                break;
            case EffectEventType.Sound:
                DrawSoundEvent(e);
                break;
            case EffectEventType.CameraShake:
                DrawCameraShakeEvent(e);
                break;
            case EffectEventType.Function:
                DrawFunctionEvent(e);
                break;
        }
    }

    private void DrawHitEvent(SerializedProperty e, int index)
    {
        EditorGUILayout.PropertyField(e.FindPropertyRelative("hitId"));
        EditorGUILayout.PropertyField(e.FindPropertyRelative("colliderType"));
        EditorGUILayout.PropertyField(e.FindPropertyRelative("hitOffset"));
        EditorGUILayout.PropertyField(e.FindPropertyRelative("previewColor"));

        HitColliderType type = (HitColliderType)e.FindPropertyRelative("colliderType").enumValueIndex;

        switch (type)
        {
            case HitColliderType.Sphere:
                EditorGUILayout.PropertyField(e.FindPropertyRelative("hitRadius"));
                break;
            case HitColliderType.Box:
                EditorGUILayout.PropertyField(e.FindPropertyRelative("hitBoxSize"));
                break;
            case HitColliderType.Capsule:
                EditorGUILayout.PropertyField(e.FindPropertyRelative("capsuleRadius"));
                EditorGUILayout.PropertyField(e.FindPropertyRelative("capsuleHeight"));
                EditorGUILayout.PropertyField(e.FindPropertyRelative("capsuleDirection"));
                break;
        }

        if (GUILayout.Button("Edit"))
        {
            selectedEventIndex = index;
            editingEventIndex = index;
            editHitEvent = (EffectEvent)e.boxedValue;
            PreviewFrame(e.FindPropertyRelative("frame").intValue);
        }
    }

    private void DrawSoundEvent(SerializedProperty e)
    {
        SerializedProperty useBGM = e.FindPropertyRelative("useBGM");
        EditorGUILayout.PropertyField(useBGM, new GUIContent("Use BGM"));

        if (useBGM.boolValue)
        {
            EditorGUILayout.PropertyField(e.FindPropertyRelative("bgm"));
        }
        else
        {
            EditorGUILayout.PropertyField(e.FindPropertyRelative("se"));
        }
    }

    private void DrawCameraShakeEvent(SerializedProperty e)
    {
        EditorGUILayout.PropertyField(e.FindPropertyRelative("shakePower"));
        EditorGUILayout.PropertyField(e.FindPropertyRelative("shakeTime"));
        EditorGUILayout.PropertyField(e.FindPropertyRelative("shakeAxis"));
        EditorGUILayout.PropertyField(e.FindPropertyRelative("shakeCurve"));
    }

    private void DrawFunctionEvent(SerializedProperty e)
    {
        EditorGUILayout.PropertyField(e.FindPropertyRelative("onEvent"));
    }

    private bool DrawPositionHandle(ref Vector3 position, string undoName)
    {
        EditorGUI.BeginChangeCheck();
        Vector3 newPos = Handles.PositionHandle(position, Quaternion.identity);

        if (!EditorGUI.EndChangeCheck())
            return false;

        RecordUndo(undoName);
        position = newPos;
        Repaint();

        return true;
    }

    private void PreviewFrame(int frame)
    {
        if (currentData == null)
            return;

        if (previewInstance != null)
        {
            DestroyImmediate(previewInstance);
        }

        previewInstance = Instantiate(currentData.prefab);
        previewInstance.hideFlags = HideFlags.HideAndDontSave;

        // 子オブジェクトを含むすべてのパーティクルシステムを取得
        ParticleSystem[] allParticles = previewInstance.GetComponentsInChildren<ParticleSystem>();
        
        if (allParticles == null || allParticles.Length == 0)
            return;

        EffectPlayer player = previewInstance.GetComponent<EffectPlayer>();
        SerializedObject so = new SerializedObject(player);
        int frameRate = so.FindProperty("frameRate").intValue;
        float time = frame / (float)Mathf.Max(1, frameRate);

        // すべてのパーティクルシステムを指定フレームまでシミュレート
        foreach (ParticleSystem particle in allParticles)
        {
            if (particle != null)
            {
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                particle.Simulate(time, true, true, true);
            }
        }

        SceneView.RepaintAll();
    }

    private void UpdatePlayback()
    {
        if (!isPlaying || currentPlayer == null)
            return;

        double currentTime = EditorApplication.timeSinceStartup;
        double delta = currentTime - lastTime;
        lastTime = currentTime;

        playbackFrame += (float)(delta * currentPlayer.FrameRate * playbackSpeed);
        previewFrame = Mathf.FloorToInt(playbackFrame);

        int maxFrame = GetMaxFrame();

        if (previewFrame > maxFrame)
        {
            if (loopPlayback)
            {
                previewFrame = 0;
                playbackFrame = 0;
            }
            else
            {
                previewFrame = maxFrame;
                playbackFrame = maxFrame;
                isPlaying = false;
            }
        }

        UpdateParticlePlayback((float)delta);
        Repaint();
    }

    private void UpdateParticlePlayback(float delta)
    {
        if (previewInstance == null)
        {
            previewInstance = Instantiate(currentData.prefab);
            previewInstance.hideFlags = HideFlags.HideAndDontSave;
        }

        // 子オブジェクトを含むすべてのパーティクルシステムを取得
        ParticleSystem[] allParticles = previewInstance.GetComponentsInChildren<ParticleSystem>();
        
        if (allParticles == null || allParticles.Length == 0)
            return;

        // すべてのパーティクルシステムを再生速度に応じてシミュレート
        foreach (ParticleSystem particle in allParticles)
        {
            if (particle != null)
            {
                particle.Simulate(delta * playbackSpeed, true, false, true);
            }
        }

        SceneView.RepaintAll();
    }

    private Rect GetEventRect(SerializedProperty e, Rect timelineRect, float frameWidth, int stack)
    {
        int frame = e.FindPropertyRelative("frame").intValue;
        EffectEventType type = (EffectEventType)e.FindPropertyRelative("type").enumValueIndex;

        float x = timelineRect.x + frame * frameWidth;
        float y = timelineRect.y + EVENT_Y_OFFSET + stack * EVENT_SPACING;

        if (type == EffectEventType.Hit)
        {
            int endFrame = e.FindPropertyRelative("endFrame").intValue;
            float endX = timelineRect.x + endFrame * frameWidth;
            return new Rect(x, y, Mathf.Max(EVENT_MIN_WIDTH, endX - x), EVENT_HEIGHT);
        }

        return new Rect(x, y, EVENT_MIN_WIDTH, EVENT_HEIGHT);
    }

    // EffectPlayer内のイベントとパーティクルから最大フレームを計算する関数
    private int GetMaxFrame()
    {
        //フレームの初期値
        int maxFrame = 0;

        //イベントのフレームを走査して、終了フレームも考慮して最大値を更新
        foreach (EffectEvent e in currentPlayer.Events)
        {
            maxFrame = Mathf.Max(maxFrame, e.frame, e.endFrame);
        }

        //全パーティクルシステムを取得して、ループ設定を考慮せずに最大フレームを計算
        ParticleSystem[] particles =currentPlayer.GetComponentsInChildren<ParticleSystem>(true);
        //ループするパーティクルシステムは無限に続く可能性があるため、ループ設定がないものだけを考慮
        foreach (ParticleSystem particle in particles)
        {
            if (particle == null)
                continue;
            //メインモジュールを取得
            ParticleSystem.MainModule main = particle.main;

            if (main.loop)
                continue;
            //パーティクルの最大フレームは、durationにフレームレートを掛けた値に切り上げたもの
            int particleFrame = Mathf.CeilToInt(main.duration *currentPlayer.FrameRate);
            //最大フレームを更新
            maxFrame = Mathf.Max(maxFrame, particleFrame);
        }

        return maxFrame;
    }

    //Undoを記録する関数
    private void RecordUndo(string name)
    {
        //Undoを記録して、currentPlayerを変更したことをUnityに通知
        Undo.RecordObject(currentPlayer, name);
        EditorUtility.SetDirty(currentPlayer);
    }
}