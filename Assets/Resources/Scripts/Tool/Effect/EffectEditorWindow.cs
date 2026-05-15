using System.Collections.Generic;
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
    private float playbackFrame;
    private float playbackSpeed = 1f;

    private readonly Color[] hitColors =
    {
        new Color(0.32f, 0.18f, 0.18f),
        new Color(0.18f, 0.18f, 0.32f),
        new Color(0.18f, 0.32f, 0.18f),
        new Color(0.32f, 0.32f, 0.18f),
        new Color(0.32f, 0.18f, 0.32f),
        new Color(0.18f, 0.32f, 0.32f),
        new Color(0.35f, 0.25f, 0.18f),
        new Color(0.25f, 0.35f, 0.18f),
        new Color(0.18f, 0.25f, 0.35f),
        new Color(0.35f, 0.18f, 0.25f),
        new Color(0.25f, 0.18f, 0.35f),
        new Color(0.18f, 0.35f, 0.25f),
    };

    [MenuItem("Tools/Effect Editor")]
    public static void Open()
    {
        GetWindow<EffectEditorWindow>("Effect Editor");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        EditorApplication.update += UpdatePlayback;

        string[] guids = AssetDatabase.FindAssets("t:EffectDatabase");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            database = AssetDatabase.LoadAssetAtPath<EffectDatabase>(path);
        }
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        EditorApplication.update -= UpdatePlayback;

        if (previewInstance != null)
        {
            DestroyImmediate(previewInstance);
        }
    }

    private void OnGUI()
    {
        DrawDatabaseField();

        if (database == null)
        {
            EditorGUILayout.HelpBox("EffectDatabase Missing", MessageType.Warning);
            return;
        }

        DrawEffectSelect();

        if (currentData == null)
            return;

        rightScroll = EditorGUILayout.BeginScrollView(rightScroll);
        DrawEffectInfo();

        if (currentPlayer != null)
        {
            DrawPlayerSettings();
            DrawEventList();
        }

        EditorGUILayout.EndScrollView();
        DrawTimelinePanel();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (editHitEvent == null || previewInstance == null)
            return;

        Handles.color = editHitEvent.previewColor;
        Matrix4x4 matrix = previewInstance.transform.localToWorldMatrix;

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

    private void DrawDatabaseField()
    {
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Database", EditorStyles.boldLabel);
        database = (EffectDatabase)EditorGUILayout.ObjectField(database, typeof(EffectDatabase), false);
        EditorGUILayout.EndVertical();
        GUILayout.Space(5);
    }

    private void DrawSpherePreview()
    {
        Vector3 center = editHitEvent.hitOffset;
        DrawPositionHandle(ref center, "Edit Sphere Position");

        float radius = Handles.RadiusHandle(Quaternion.identity, center, editHitEvent.hitRadius);

        if (radius != editHitEvent.hitRadius)
        {
            RecordUndo("Edit Sphere Radius");
            editHitEvent.hitRadius = radius;
            Repaint();
        }

        editHitEvent.hitOffset = center;

        Handles.DrawWireDisc(center, Vector3.up, radius);
        Handles.DrawWireDisc(center, Vector3.right, radius);
        Handles.DrawWireDisc(center, Vector3.forward, radius);
    }

    private void DrawBoxPreview()
    {
        Color oldColor = Handles.color;
        Handles.color = editHitEvent.previewColor;

        EditorGUI.BeginChangeCheck();

        Vector3 center = Handles.PositionHandle(editHitEvent.hitOffset, Quaternion.identity);
        Vector3 size = Handles.ScaleHandle(editHitEvent.hitBoxSize, center, Quaternion.identity, HandleUtility.GetHandleSize(center));

        if (EditorGUI.EndChangeCheck())
        {
            editHitEvent.hitOffset = center;
            editHitEvent.hitBoxSize = size;
            Repaint();
        }

        Handles.DrawWireCube(center, size);
        Handles.color = oldColor;
    }

    private void DrawCapsulePreview()
    {
        Color oldColor = Handles.color;
        Handles.color = editHitEvent.previewColor;

        Vector3 center = editHitEvent.hitOffset;
        float radius = editHitEvent.capsuleRadius;
        float height = editHitEvent.capsuleHeight;

        Vector3 axis = GetCapsuleAxis(editHitEvent.capsuleDirection);
        (Vector3 crossA, Vector3 crossB) = GetCapsuleCrossAxes(editHitEvent.capsuleDirection);

        EditorGUI.BeginChangeCheck();

        center = Handles.PositionHandle(center, Quaternion.identity);
        radius = Handles.RadiusHandle(Quaternion.identity, center, radius);

        float bodyHalf = Mathf.Max(0, (height * 0.5f) - radius);
        Vector3 top = center + axis * bodyHalf;
        Vector3 bottom = center - axis * bodyHalf;

        top = Handles.Slider(top, axis);
        bottom = Handles.Slider(bottom, -axis);

        if (EditorGUI.EndChangeCheck())
        {
            editHitEvent.hitOffset = center;
            editHitEvent.capsuleRadius = radius;
            editHitEvent.capsuleHeight = Vector3.Distance(top, bottom) + radius * 2f;
            Repaint();
        }

        DrawCapsuleWireframe(center, radius, axis, crossA, crossB);
        Handles.color = oldColor;
    }

    private Vector3 GetCapsuleAxis(CapsuleDirection direction)
    {
        return direction switch
        {
            CapsuleDirection.X => Vector3.right,
            CapsuleDirection.Y => Vector3.up,
            CapsuleDirection.Z => Vector3.forward,
            _ => Vector3.up
        };
    }

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

    private void DrawCapsuleWireframe(Vector3 center, float radius, Vector3 axis, Vector3 crossA, Vector3 crossB)
    {
        float bodyHalf = Mathf.Max(0, (editHitEvent.capsuleHeight * 0.5f) - radius);
        Vector3 top = center + axis * bodyHalf;
        Vector3 bottom = center - axis * bodyHalf;

        Handles.DrawWireDisc(top, axis, radius);
        Handles.DrawWireDisc(top, crossA, radius);
        Handles.DrawWireDisc(top, crossB, radius);
        Handles.DrawWireDisc(bottom, axis, radius);
        Handles.DrawWireDisc(bottom, crossA, radius);
        Handles.DrawWireDisc(bottom, crossB, radius);

        Handles.DrawLine(top + crossA * radius, bottom + crossA * radius);
        Handles.DrawLine(top - crossA * radius, bottom - crossA * radius);
        Handles.DrawLine(top + crossB * radius, bottom + crossB * radius);
        Handles.DrawLine(top - crossB * radius, bottom - crossB * radius);
    }

    private void DrawEffectSelect()
    {
        EnsureAllEffectTypesExist();

        string[] names = new string[database.effects.Count];
        for (int i = 0; i < database.effects.Count; i++)
        {
            names[i] = database.effects[i].type.ToString();
        }

        selectedIndex = EditorGUILayout.Popup("Effect", selectedIndex, names);

        if (selectedIndex >= 0 && selectedIndex < database.effects.Count)
        {
            currentData = database.effects[selectedIndex];
            if (currentData.prefab != null)
            {
                currentPlayer = currentData.prefab.GetComponent<EffectPlayer>();
            }
        }
    }

    private void EnsureAllEffectTypesExist()
    {
        EffectType[] allTypes = (EffectType[])System.Enum.GetValues(typeof(EffectType));
        foreach (EffectType type in allTypes)
        {
            bool exists = false;
            foreach (EffectData data in database.effects)
            {
                if (data.type == type)
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                EffectData data = new EffectData { type = type };
                database.effects.Add(data);
            }
        }
    }

    private void DrawEffectInfo()
    {
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Effect Info", EditorStyles.boldLabel);

        currentData.prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", currentData.prefab, typeof(GameObject), false);

        if (GUILayout.Button("Setup Effect", GUILayout.Height(40)))
        {
            SetupEffect();
        }

        EditorGUILayout.EndVertical();
    }

    private void SetupEffect()
    {
        if (currentData.prefab == null)
            return;

        string path = AssetDatabase.GetAssetPath(currentData.prefab);
        GameObject root = PrefabUtility.LoadPrefabContents(path);

        EffectPlayer player = root.GetComponent<EffectPlayer>();
        if (player == null)
        {
            player = root.AddComponent<EffectPlayer>();
        }

        Rigidbody rb = root.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = root.AddComponent<Rigidbody>();
        }

        rb.isKinematic = true;
        rb.useGravity = false;

        ParticleSystem particle = root.GetComponentInChildren<ParticleSystem>();
        SerializedObject so = new SerializedObject(player);
        so.FindProperty("mainParticle").objectReferenceValue = particle;
        so.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        currentData.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        currentPlayer = currentData.prefab.GetComponent<EffectPlayer>();
    }

    private void DrawPlayerSettings()
    {
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Player Settings", EditorStyles.boldLabel);

        SerializedObject so = new SerializedObject(currentPlayer);
        so.Update();
        EditorGUILayout.PropertyField(so.FindProperty("frameRate"));
        EditorGUILayout.PropertyField(so.FindProperty("mainParticle"));
        so.ApplyModifiedProperties();

        EditorGUILayout.EndVertical();
    }

    private void DrawEventList()
    {
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Events", EditorStyles.boldLabel);

        eventScroll = EditorGUILayout.BeginScrollView(eventScroll, GUILayout.Height(position.height * 0.45f));

        SerializedObject so = new SerializedObject(currentPlayer);
        SerializedProperty events = so.FindProperty("events");

        while (eventFoldouts.Count < events.arraySize)
        {
            eventFoldouts.Add(true);
        }

        DrawEventListButtons(events);
        DrawEventItems(events, so);

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawEventListButtons(SerializedProperty events)
    {
        if (GUILayout.Button("Add"))
        {
            RecordUndo("Add Event");
            events.InsertArrayElementAtIndex(events.arraySize);
            eventFoldouts.Add(true);
        }

        if (GUILayout.Button("Sort"))
        {
            RecordUndo("Sort Events");
            currentPlayer.Events.Sort((a, b) => a.frame.CompareTo(b.frame));
            EditorUtility.SetDirty(currentPlayer);
        }
    }

    private void DrawEventItems(SerializedProperty events, SerializedObject so)
    {
        for (int i = 0; i < events.arraySize; i++)
        {
            SerializedProperty e = events.GetArrayElementAtIndex(i);
            SerializedProperty typeProp = e.FindPropertyRelative("type");
            EffectEventType eventType = (EffectEventType)typeProp.enumValueIndex;

            int hitId = eventType == EffectEventType.Hit ? e.FindPropertyRelative("hitId").intValue : 0;
            Color cardColor = GetEventColor(eventType, hitId);

            if (eventType == EffectEventType.Hit)
            {
                e.FindPropertyRelative("previewColor").colorValue = cardColor;
            }

            if (selectedEventIndex == i)
            {
                cardColor *= 1.35f;
            }

            bool remove = DrawEventCard(e, eventType, cardColor, i);

            if (remove)
            {
                RecordUndo("Delete Event");
                events.DeleteArrayElementAtIndex(i);
                eventFoldouts.RemoveAt(i);
                break;
            }
        }

        if (so.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(currentPlayer);
        }
    }

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

        SerializedProperty frameProp = e.FindPropertyRelative("frame");
        SerializedProperty endFrameProp = e.FindPropertyRelative("endFrame");

        int startFrame = frameProp.intValue;
        int endFrame = endFrameProp.intValue;
        int hitId = eventType == EffectEventType.Hit ? e.FindPropertyRelative("hitId").intValue : 0;

        string title = eventType == EffectEventType.Hit
            ? $"Hit {hitId} [{startFrame} - {endFrame}]"
            : $"Frame {startFrame}";

        Rect foldRect = GUILayoutUtility.GetRect(20, 22);
        EditorGUI.DrawRect(foldRect, cardColor);
        eventFoldouts[index] = EditorGUI.Foldout(foldRect, eventFoldouts[index], title, true);

        if (!eventFoldouts[index])
        {
            GUILayout.EndVertical();
            return false;
        }

        EditorGUILayout.Space(4);

        if (eventType == EffectEventType.Hit)
        {
            int newFrame = EditorGUILayout.IntField("Start", startFrame);
            if (newFrame != startFrame)
            {
                RecordUndo("Edit Start Frame");
                frameProp.intValue = newFrame;
            }
            endFrameProp.intValue = EditorGUILayout.IntField("End", endFrameProp.intValue);
        }
        else
        {
            EditorGUILayout.PropertyField(frameProp);
        }

        EditorGUILayout.PropertyField(e.FindPropertyRelative("type"));
        DrawEventFields(e, eventType, index);

        bool remove = GUILayout.Button("Delete");

        EditorGUILayout.EndVertical();

        return remove;
    }

    private void DrawTimelinePanel()
    {
        if (currentPlayer == null)
            return;

        GUILayout.Space(10);
        EditorGUILayout.BeginVertical("box");

        DrawPlaybackControls();
        DrawTimelineControls();
        DrawTimelineView();

        EditorGUILayout.EndVertical();
    }

    private void DrawPlaybackControls()
    {
        EditorGUILayout.BeginHorizontal();

        if (!isPlaying)
        {
            if (GUILayout.Button("Play", GUILayout.Width(70), GUILayout.Height(24)))
            {
                StartPlayback();
            }
        }
        else
        {
            if (GUILayout.Button("Stop", GUILayout.Width(70), GUILayout.Height(24)))
            {
                isPlaying = false;
            }
        }

        GUILayout.Space(10);
        GUILayout.Label($"Frame : {previewFrame}", GUILayout.Width(100));
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Preview", GUILayout.Width(80)))
        {
            PreviewFrame(previewFrame);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void StartPlayback()
    {
        isPlaying = true;

        int playbackMaxFrame = GetMaxFrame();
        if (previewFrame >= playbackMaxFrame)
        {
            previewFrame = 0;
            playbackFrame = 0;
        }

        lastTime = EditorApplication.timeSinceStartup;

        if (previewInstance != null)
        {
            DestroyImmediate(previewInstance);
        }

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

    private void DrawTimelineControls()
    {
        playbackSpeed = EditorGUILayout.Slider("Speed", playbackSpeed, 0.1f, 3f);

        int maxFrame = GetMaxFrame();
        previewFrame = EditorGUILayout.IntSlider("Preview Frame", previewFrame, 0, maxFrame);
    }

    private void DrawTimelineView()
    {
        timelineScroll = EditorGUILayout.BeginScrollView(timelineScroll, GUILayout.Height(320));
        DrawTimeline();
        EditorGUILayout.EndScrollView();
    }

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

    private int GetMaxFrame()
    {
        int maxFrame = 60;

        foreach (EffectEvent e in currentPlayer.Events)
        {
            maxFrame = Mathf.Max(maxFrame, e.frame, e.endFrame);
        }

        if (currentPlayer.MainParticle != null)
        {
            ParticleSystem.MainModule main = currentPlayer.MainParticle.main;
            float duration = main.duration;

            if (main.loop)
            {
                duration = 0;
            }

            int particleFrame = Mathf.CeilToInt(duration * currentPlayer.FrameRate);
            maxFrame = Mathf.Max(maxFrame, particleFrame);
        }

        return maxFrame;
    }

    private void RecordUndo(string name)
    {
        Undo.RecordObject(currentPlayer, name);
        EditorUtility.SetDirty(currentPlayer);
    }
}