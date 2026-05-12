using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public class EffectEditorWindow : EditorWindow
{
    private const float EVENT_HEIGHT = 20f;
    private const float EVENT_SPACING = 28f;
    private const float EVENT_Y_OFFSET = 35f;
    private const float EVENT_MIN_WIDTH = 12f;

    private EffectDatabase database;
    private Vector2 rightScroll;
    private Vector2 timelineScroll;
    private Vector2 eventScroll;
    private int selectedIndex = -1;
    private EffectData currentData;
    private EffectPlayer currentPlayer;
    private int previewFrame;
    private GameObject previewInstance;
    private EffectEvent editHitEvent;
    private int editingEventIndex = -1;
    private int selectedEventIndex = -1;
    private List<bool>eventFoldouts =new();
    private bool isPlaying;
    private bool loopPlayback = true;

    private double lastTime;
    private float playbackFrame;
    private float playbackSpeed = 1f;

    [MenuItem("Tools/Effect Editor")]
    public static void Open()
    {
        GetWindow<EffectEditorWindow>(
            "Effect Editor"
        );
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;

        EditorApplication.update += UpdatePlayback;

        string[] guids =
            AssetDatabase.FindAssets(
                "t:EffectDatabase"
            );

        if (guids.Length > 0)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guids[0]
                );

            database =
                AssetDatabase.LoadAssetAtPath
                <
                    EffectDatabase
                >
                (
                    path
                );
        }
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;

        EditorApplication.update -= UpdatePlayback;

        if (previewInstance != null)
        {
            DestroyImmediate(
                previewInstance
            );
        }
    }

    private void OnSceneGUI(
       SceneView sceneView
   )
    {
        if (editHitEvent == null)
            return;

        if (previewInstance == null)
            return;

        Handles.color =
    editHitEvent.previewColor;

        Matrix4x4 matrix =
            previewInstance.transform
            .localToWorldMatrix;

        using (
            new Handles.DrawingScope(matrix)
        )
        {
            switch (
                editHitEvent.colliderType
            )
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

    private void DrawSpherePreview()
    {
        EditorGUI.BeginChangeCheck();

        Vector3 center =
            Handles.PositionHandle(
                editHitEvent.hitOffset,
                Quaternion.identity
            );

        float radius =
            Handles.RadiusHandle(
                Quaternion.identity,
                center,
                editHitEvent.hitRadius
            );

        if (EditorGUI.EndChangeCheck())
        {
            RecordUndo(
       "Edit Sphere Hit"
   );

            editHitEvent.hitOffset =
                center;

            editHitEvent.hitRadius =
                radius;

            Repaint();
        }

        Handles.DrawWireDisc(
            center,
            Vector3.up,
            radius
        );

        Handles.DrawWireDisc(
            center,
            Vector3.right,
            radius
        );

        Handles.DrawWireDisc(
            center,
            Vector3.forward,
            radius
        );
    }

    private void DrawBoxPreview()
    {
        Color oldColor =
            Handles.color;

        Handles.color =
            editHitEvent.previewColor;

        EditorGUI.BeginChangeCheck();

        Vector3 center =
            Handles.PositionHandle(
                editHitEvent.hitOffset,
                Quaternion.identity
            );

        Vector3 size =
            Handles.ScaleHandle(
                editHitEvent.hitBoxSize,
                center,
                Quaternion.identity,
                HandleUtility.GetHandleSize(
                    center
                )
            );

        if (EditorGUI.EndChangeCheck())
        {
            editHitEvent.hitOffset =
                center;

            editHitEvent.hitBoxSize =
                size;

            Repaint();
        }

        Handles.color =
            editHitEvent.previewColor;

        Handles.DrawWireCube(
            center,
            size
        );

        Handles.color =
            oldColor;
    }

    private void DrawCapsulePreview()
    {
        Color oldColor =
            Handles.color;

        Handles.color =
            editHitEvent.previewColor;

        Vector3 center =
            editHitEvent.hitOffset;

        float radius =
            editHitEvent.capsuleRadius;

        float height =
            editHitEvent.capsuleHeight;

        Vector3 axis =
            Vector3.up;

        switch (
            editHitEvent.capsuleDirection
        )
        {
            case CapsuleDirection.X:

                axis =
                    Vector3.right;

                break;

            case CapsuleDirection.Y:

                axis =
                    Vector3.up;

                break;

            case CapsuleDirection.Z:

                axis =
                    Vector3.forward;

                break;
        }

        Vector3 crossA =
            Vector3.right;

        Vector3 crossB =
            Vector3.forward;

        switch (
            editHitEvent.capsuleDirection
        )
        {
            case CapsuleDirection.X:

                crossA =
                    Vector3.up;

                crossB =
                    Vector3.forward;

                break;

            case CapsuleDirection.Y:

                crossA =
                    Vector3.right;

                crossB =
                    Vector3.forward;

                break;

            case CapsuleDirection.Z:

                crossA =
                    Vector3.right;

                crossB =
                    Vector3.up;

                break;
        }

        EditorGUI.BeginChangeCheck();

        center =
            Handles.PositionHandle(
                center,
                Quaternion.identity
            );

        radius =
            Handles.RadiusHandle(
                Quaternion.identity,
                center,
                radius
            );

        float bodyHalf =
            Mathf.Max(
                0,
                (
                    height * 0.5f
                ) - radius
            );

        Vector3 top =
            center +
            axis * bodyHalf;

        Vector3 bottom =
            center -
            axis * bodyHalf;

        top =
            Handles.Slider(
                top,
                axis
            );

        bottom =
            Handles.Slider(
                bottom,
                -axis
            );

        if (EditorGUI.EndChangeCheck())
        {
            editHitEvent.hitOffset =
                center;

            editHitEvent.capsuleRadius =
                radius;

            editHitEvent.capsuleHeight =
                Vector3.Distance(
                    top,
                    bottom
                ) +
                radius * 2f;

            Repaint();
        }

        bodyHalf =
            Mathf.Max(
                0,
                (
                    editHitEvent
                    .capsuleHeight
                    * 0.5f
                ) - radius
            );

        top =
            center +
            axis * bodyHalf;

        bottom =
            center -
            axis * bodyHalf;

        Handles.DrawWireDisc(
            top,
            axis,
            radius
        );

        Handles.DrawWireDisc(
            top,
            crossA,
            radius
        );

        Handles.DrawWireDisc(
            top,
            crossB,
            radius
        );

        Handles.DrawWireDisc(
            bottom,
            axis,
            radius
        );

        Handles.DrawWireDisc(
            bottom,
            crossA,
            radius
        );

        Handles.DrawWireDisc(
            bottom,
            crossB,
            radius
        );

        Handles.DrawLine(
            top + crossA * radius,
            bottom + crossA * radius
        );

        Handles.DrawLine(
            top - crossA * radius,
            bottom - crossA * radius
        );

        Handles.DrawLine(
            top + crossB * radius,
            bottom + crossB * radius
        );

        Handles.DrawLine(
            top - crossB * radius,
            bottom - crossB * radius
        );

        Handles.color =
            oldColor;
    }



    private void OnGUI()
    {
        EditorGUILayout.BeginVertical(
            "box"
        );

        GUILayout.Label(
            "Database",
            EditorStyles.boldLabel
        );

        database =
            (EffectDatabase)
            EditorGUILayout.ObjectField(
                database,
                typeof(EffectDatabase),
                false
            );

        EditorGUILayout.EndVertical();

        GUILayout.Space(5);

        if (database == null)
        {
            EditorGUILayout.HelpBox(
                "EffectDatabase Missing",
                MessageType.Warning
            );

            return;
        }

        DrawEffectSelect();

        if (currentData == null)
            return;

        rightScroll =
            EditorGUILayout.BeginScrollView(
                rightScroll
            );

        DrawEffectInfo();

        if (currentPlayer != null)
        {
            DrawPlayerSettings();
            DrawEventList();
        }

        EditorGUILayout.EndScrollView();

        DrawTimelinePanel();
    }

    private void DrawEffectSelect()
    {
        EffectType[] allTypes =
            (EffectType[])
            System.Enum.GetValues(
                typeof(EffectType)
            );

        foreach (EffectType type in allTypes)
        {
            bool exists = false;

            foreach (
                EffectData data
                in database.effects
            )
            {
                if (data.type == type)
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                EffectData data =
                    new EffectData();

                data.type = type;

                database.effects.Add(
                    data
                );
            }
        }

        string[] names =
            new string[
                database.effects.Count
            ];

        for (
            int i = 0;
            i < database.effects.Count;
            i++
        )
        {
            names[i] =
                database.effects[i]
                .type
                .ToString();
        }

        selectedIndex =
            EditorGUILayout.Popup(
                "Effect",
                selectedIndex,
                names
            );

        if (
            selectedIndex >= 0 &&
            selectedIndex <
            database.effects.Count
        )
        {
            currentData =
                database.effects[
                    selectedIndex
                ];

            if (
                currentData.prefab != null
            )
            {
                currentPlayer =
                    currentData.prefab
                    .GetComponent
                    <
                        EffectPlayer
                    >();
            }
        }
    }

    private void DrawEffectInfo()
    {
        EditorGUILayout.BeginVertical(
            "box"
        );

        GUILayout.Label(
            "Effect Info",
            EditorStyles.boldLabel
        );

        currentData.prefab =
            (GameObject)
            EditorGUILayout.ObjectField(
                "Prefab",
                currentData.prefab,
                typeof(GameObject),
                false
            );

        if (
            GUILayout.Button(
                "Setup Effect",
                GUILayout.Height(40)
            )
        )
        {
            SetupEffect();
        }

        EditorGUILayout.EndVertical();
    }

    private void SetupEffect()
    {
        if (currentData.prefab == null)
            return;

        string path =
            AssetDatabase.GetAssetPath(
                currentData.prefab
            );

        GameObject root =
            PrefabUtility
            .LoadPrefabContents(path);

        EffectPlayer player =
            root.GetComponent
            <
                EffectPlayer
            >();

        if (player == null)
        {
            player =
                root.AddComponent
                <
                    EffectPlayer
                >();
        }

        Rigidbody rb =
            root.GetComponent<Rigidbody>();

        if (rb == null)
        {
            rb =
                root.AddComponent
                <
                    Rigidbody
                >();
        }

        rb.isKinematic = true;
        rb.useGravity = false;

        ParticleSystem particle =
            root.GetComponentInChildren
            <
                ParticleSystem
            >();

        SerializedObject so =
            new SerializedObject(player);

        so.FindProperty(
            "mainParticle"
        ).objectReferenceValue =
            particle;

        so.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(
            root,
            path
        );

        PrefabUtility
            .UnloadPrefabContents(
                root
            );

        AssetDatabase.SaveAssets();

        AssetDatabase.Refresh();

        currentData.prefab =
            AssetDatabase.LoadAssetAtPath
            <
                GameObject
            >
            (
                path
            );

        currentPlayer =
            currentData.prefab
            .GetComponent
            <
                EffectPlayer
            >();
    }

    private void DrawPlayerSettings()
    {
        EditorGUILayout.BeginVertical(
            "box"
        );

        GUILayout.Label(
            "Player Settings",
            EditorStyles.boldLabel
        );

        SerializedObject so =
            new SerializedObject(
                currentPlayer
            );

        so.Update();

        EditorGUILayout.PropertyField(
            so.FindProperty(
                "frameRate"
            )
        );

        EditorGUILayout.PropertyField(
            so.FindProperty(
                "mainParticle"
            )
        );

        so.ApplyModifiedProperties();

        EditorGUILayout.EndVertical();
    }

    private void DrawEventList()
    {
        EditorGUILayout.BeginVertical(
            "box"
        );

        GUILayout.Label(
            "Events",
            EditorStyles.boldLabel
        );

        eventScroll =
    EditorGUILayout.BeginScrollView(
        eventScroll,
       GUILayout.Height(
    position.height * 0.45f
)
    );

        SerializedObject so =
            new SerializedObject(
                currentPlayer
            );

        SerializedProperty events =
            so.FindProperty(
                "events"
            );

        while (
            eventFoldouts.Count <
            events.arraySize
        )
        {
            eventFoldouts.Add(true);
        }

        if (
            GUILayout.Button("Add")
        )
        {
            RecordUndo("Add Event");
            events.InsertArrayElementAtIndex(
                events.arraySize
            );

            eventFoldouts.Add(true);

            
        }

        if (
            GUILayout.Button("Sort")
        )
        {
            RecordUndo("Sort Events");

            currentPlayer.Events.Sort(
                (a, b) =>
                a.frame.CompareTo(
                    b.frame
                )
            );

            EditorUtility.SetDirty(
                currentPlayer
            );
        }

        for (
            int i = 0;
            i < events.arraySize;
            i++
        )
        {
            SerializedProperty e =
                events
                .GetArrayElementAtIndex(i);

            SerializedProperty typeProp =
                e.FindPropertyRelative(
                    "type"
                );

            EffectEventType eventType =
                (EffectEventType)
                typeProp.enumValueIndex;


            bool remove = false;

            int hitId = 0;

            if (
                eventType ==
                EffectEventType.Hit
            )
            {
                hitId =
                    e.FindPropertyRelative(
                        "hitId"
                    ).intValue;
            }

            //イベントタイプとヒットIDに基づいてカードの色を決定
            Color cardColor =GetEventColor(eventType, hitId);

            if (eventType == EffectEventType.Hit)
            {
                e.FindPropertyRelative(
                    "previewColor"
                ).colorValue =
                    cardColor;
            }

            if (
                selectedEventIndex == i
            )
            {
                cardColor *= 1.35f;
            }

            GUI.backgroundColor =
    cardColor;

            if (eventFoldouts[i])
            {
                EditorGUILayout.BeginVertical(
                    "box" 
                );
            }
            else
            {
                GUILayout.BeginVertical(
                    GUI.skin.box,
                    GUILayout.Height(24)
                );
            }

            GUI.backgroundColor =
                Color.white;

            SerializedProperty frameProp =
                e.FindPropertyRelative(
                    "frame"
                );



            SerializedProperty endFrameProp =
     e.FindPropertyRelative(
         "endFrame"
     );

            int startFrame =
                frameProp.intValue;

            int endFrame =
                endFrameProp.intValue;

            string title =
                eventType ==
                EffectEventType.Hit
                ? $"Hit {hitId} [{startFrame} - {endFrame}]"
                : $"Frame {startFrame}";

            Rect foldRect =
                GUILayoutUtility.GetRect(
                    20,
                    22
                );

            EditorGUI.DrawRect(
                foldRect,
                cardColor
            );

            eventFoldouts[i] =
                EditorGUI.Foldout(
                    foldRect,
                    eventFoldouts[i],
                    title,
                    true
                );

            if (!eventFoldouts[i])
            {
                GUILayout.EndVertical();

                continue;
            }

            EditorGUILayout.Space(4);

            if (
                eventType ==
                EffectEventType.Hit
            )
            {
                int newFrame =
     EditorGUILayout.IntField(
         "Start",
         startFrame
     );


                if (newFrame != startFrame)
                {
                    RecordUndo(
                        "Edit Start Frame"
                    );

                    frameProp.intValue =
                        newFrame;
                }



                endFrameProp.intValue =
                    EditorGUILayout.IntField(
                        "End",
                        endFrameProp.intValue
                    );
            }
            else
            {
                EditorGUILayout.PropertyField(
                    frameProp
                );
            }

            EditorGUILayout.PropertyField(
                typeProp
            );

            DrawEventFields(e,eventType, i);

            if (
                GUILayout.Button("Delete")
            )
            {
                remove = true;
            }

            EditorGUILayout.EndVertical();

            if (remove)
            {

                RecordUndo("Delete Event");

                events
                .DeleteArrayElementAtIndex(
                    i
                );

                eventFoldouts.RemoveAt(i);

                break;
            }
        }

        if (
            so.ApplyModifiedProperties()
        )
        {
            EditorUtility.SetDirty(
                currentPlayer
            );
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.EndVertical();
    }

    private void DrawTimelinePanel()
    {
        if (currentPlayer == null)
            return;

        GUILayout.Space(10);

        EditorGUILayout.BeginVertical("box");

        //========================
        // 上段
        //========================

        EditorGUILayout.BeginHorizontal();

        if (!isPlaying)
        {
            if (
                GUILayout.Button(
                    "Play",
                    GUILayout.Width(70),
                    GUILayout.Height(24)
                )
            )
            {
                isPlaying = true;

                int playbackMaxFrame =
       GetMaxFrame();

                if (previewFrame >= playbackMaxFrame)
                {
                    previewFrame = 0;
                    playbackFrame = 0;
                }




                lastTime =
                    EditorApplication
                    .timeSinceStartup;

                if (previewInstance != null)
                {
                    DestroyImmediate(
                        previewInstance
                    );
                }

                previewInstance =
                    Instantiate(
                        currentData.prefab
                    );

                previewInstance.hideFlags =
                    HideFlags.HideAndDontSave;

                ParticleSystem particle =
                    previewInstance
                    .GetComponentInChildren
                    <
                        ParticleSystem
                    >();

                if (particle != null)
                {
                    loopPlayback =
        particle.main.loop;

                    particle.Stop(
                        true,
                        ParticleSystemStopBehavior
                        .StopEmittingAndClear
                    );

                    particle.Simulate(
                        previewFrame /
                        (float)currentPlayer.FrameRate,
                        true,
                        true,
                        true
                    );
                }
            }
        }
        else
        {
            if (
                GUILayout.Button(
                    "Stop",
                    GUILayout.Width(70),
                    GUILayout.Height(24)
                )
            )
            {
                isPlaying = false;
            }
        }

       

        GUILayout.Space(10);

        GUILayout.Label(
            $"Frame : {previewFrame}",
            GUILayout.Width(100)
        );

        GUILayout.FlexibleSpace();

        if (
            GUILayout.Button(
                "Preview",
                GUILayout.Width(80)
            )
        )
        {
            PreviewFrame(previewFrame);
        }

        EditorGUILayout.EndHorizontal();

        //========================
        // 下段
        //========================

       

        playbackSpeed =
            EditorGUILayout.Slider(
                "Speed",
                playbackSpeed,
                0.1f,
                3f
            );

        int maxFrame =
            GetMaxFrame();

        previewFrame =
            EditorGUILayout.IntSlider(
                "Preview Frame",
                previewFrame,
                0,
                maxFrame
            );

        //========================
        // Timeline
        //========================

        timelineScroll =
            EditorGUILayout.BeginScrollView(
                timelineScroll,
                GUILayout.Height(320)
            );

        DrawTimeline();

        EditorGUILayout.EndScrollView();

        EditorGUILayout.EndVertical();
    }

    private readonly Color[] hitColors =
{
    new Color(0.32f,0.18f,0.18f),
    new Color(0.18f,0.18f,0.32f),
    new Color(0.18f,0.32f,0.18f),
    new Color(0.32f,0.32f,0.18f),
    new Color(0.32f,0.18f,0.32f),
    new Color(0.18f,0.32f,0.32f),
    new Color(0.35f,0.25f,0.18f),
    new Color(0.25f,0.35f,0.18f),
    new Color(0.18f,0.25f,0.35f),
    new Color(0.35f,0.18f,0.25f),
    new Color(0.25f,0.18f,0.35f),
    new Color(0.18f,0.35f,0.25f),
};

    private Color GetEventColor( EffectEventType type,int hitId)
    {
        switch (type)
        {
            case EffectEventType.Hit:
                return hitColors[
                    Mathf.Abs(hitId) %
                    hitColors.Length
                ];

            case EffectEventType.Sound:
                return new Color(
                    0.8f,
                    0.8f,
                    0.2f
                );

            case EffectEventType.CameraShake:
                return new Color(
                    0.2f,
                    0.8f,
                    0.8f
                );

            case EffectEventType.Function:
                return new Color(
                    0.8f,
                    0.2f,
                    0.8f
                );

            default:
                return Color.gray;
        }
    }

    private void DrawTimeline()
    {
        GUILayout.Label(
            "Timeline",
            EditorStyles.boldLabel
        );

       

        SerializedObject so =
            new SerializedObject(
                currentPlayer
            );

        SerializedProperty events =
            so.FindProperty(
                "events"
            );
        float contentHeight =
            60 + events.arraySize * 28;

        int maxFrame = 60;

        for (
            int i = 0;
            i < events.arraySize;
            i++
        )
        {
            SerializedProperty e =
                events
                .GetArrayElementAtIndex(i);

            int startFrame =
                e.FindPropertyRelative(
                    "frame"
                ).intValue;

            int endFrame =
                e.FindPropertyRelative(
                    "endFrame"
                ).intValue;

            maxFrame =
                Mathf.Max(
                    maxFrame,
                    startFrame,
                    endFrame,
                    GetMaxFrame()
                );
        }

        float extraWidth =
            Mathf.Max(
                0,
                GetMaxFrame() - 60
            ) * 30;

        Rect rect =
     GUILayoutUtility.GetRect(
         1200 + extraWidth,
         contentHeight
     );

        EditorGUI.DrawRect(
            rect,
            new Color(
                0.12f,
                0.12f,
                0.12f
            )
        );

        float frameWidth =
            rect.width /
            Mathf.Max(
                1,
                maxFrame
            );

        if (isPlaying)
        {
            float autoScrollPlayheadX =
                rect.x +
                previewFrame *
                frameWidth;

            float viewWidth =
                position.width - 40;

            timelineScroll.x =
                Mathf.Max(
                    0,
                    autoScrollPlayheadX -
                    viewWidth * 0.5f
                );
        }

        bool clickedEvent =
            false;

        //================================
        // Event Click
        //================================

        for (
            int i = 0;
            i < events.arraySize;
            i++
        )
        {
            SerializedProperty e =
                events
                .GetArrayElementAtIndex(i);

            int frame =
                e.FindPropertyRelative(
                    "frame"
                ).intValue;

            int typeIndex =
                e.FindPropertyRelative(
                    "type"
                ).enumValueIndex;

            EffectEventType eventType =
                (EffectEventType)
                typeIndex;

            int stack = i;

            float x =
                rect.x +
                frame * frameWidth;

            float y =
                rect.y + 35 +
                stack * 28;

            Rect eventRect =GetEventRect(e, rect,frameWidth,stack);

            Event current =
                Event.current;

            if (
                current.type ==
                EventType.MouseDown &&
                eventRect.Contains(
                    current.mousePosition
                )
            )
            {
                clickedEvent =
                    true;

                selectedEventIndex =
                    i;

                if (
                    current.clickCount == 2
                )
                {
                    editingEventIndex =
                        i;

                    editHitEvent =
                        currentPlayer
                        .Events[i];

                    previewFrame =
                        frame;

                    PreviewFrame(
                        frame
                    );
                }

                Repaint();

                current.Use();
            }
        }

        //================================
        // Playhead
        //================================

        float playheadX =
            rect.x +
            previewFrame *
            frameWidth;

        EditorGUI.DrawRect(
            new Rect(
                playheadX,
                rect.y,
                2,
                rect.height
            ),
            Color.red
        );

        Handles.BeginGUI();

        Handles.color =
            Color.red;

        Vector3[] triangle =
        {
        new Vector3(
            playheadX - 6,
            rect.y
        ),

        new Vector3(
            playheadX + 6,
            rect.y
        ),

        new Vector3(
            playheadX,
            rect.y + 10
        )
    };

        Handles.DrawAAConvexPolygon(
            triangle
        );

        Handles.EndGUI();

        GUI.Label(
            new Rect(
                playheadX + 4,
                rect.y + 10,
                40,
                20
            ),
            previewFrame.ToString()
        );

        //================================
        // Grid
        //================================

        for (
            int i = 0;
            i <= maxFrame;
            i++
        )
        {
            float x =
                rect.x +
                frameWidth * i;

            EditorGUI.DrawRect(
                new Rect(
                    x,
                    rect.y,
                    1,
                    rect.height
                ),
                new Color(
                    0.25f,
                    0.25f,
                    0.25f
                )
            );

            GUI.Label(
                new Rect(
                    x + 2,
                    rect.y + 2,
                    30,
                    20
                ),
                i.ToString()
            );
        }

        //================================
        // Timeline Click
        //================================

        if (
            !clickedEvent &&
            Event.current.type ==
            EventType.MouseDown &&
            rect.Contains(
                Event.current.mousePosition
            )
        )
        {
            float localX =
                Event.current
                .mousePosition.x
                - rect.x;

            previewFrame =
                Mathf.Clamp(
                    Mathf.RoundToInt(
                        localX / frameWidth
                    ),
                    0,
                    maxFrame
                );

            PreviewFrame(
                previewFrame
            );

            Repaint();

            Event.current.Use();
        }

        //================================
        // Event Draw
        //================================

        for (
            int i = 0;
            i < events.arraySize;
            i++
        )
        {
            SerializedProperty e =
                events
                .GetArrayElementAtIndex(i);

            int frame =
                e.FindPropertyRelative(
                    "frame"
                ).intValue;

            int typeIndex =
                e.FindPropertyRelative(
                    "type"
                ).enumValueIndex;

            EffectEventType eventType =
                (EffectEventType)
                typeIndex;

            int hitId = 0;

            if (
                eventType ==
                EffectEventType.Hit
            )
            {
                hitId =
                    e.FindPropertyRelative(
                        "hitId"
                    ).intValue;
            }

            int stack = i;

            float x =
                rect.x +
                frame * frameWidth;

            float y = rect.y + EVENT_Y_OFFSET + stack * EVENT_SPACING;


            //イベントタイプとヒットIDに基づいてカードの色を決定
            Color color = GetEventColor(eventType,hitId);



            Rect eventRect = GetEventRect(e,rect,frameWidth,stack);

            bool selected =
                selectedEventIndex == i;

            bool editing =
                editingEventIndex == i;

            EditorGUI.DrawRect(
                eventRect,
                color
            );

            if (
                selected ||
                editing
            )
            {
                Handles.BeginGUI();

                Color old =
                    Handles.color;

                Handles.color =
                    editing
                    ? Color.green
                    : Color.yellow;

                Vector3[] lines =
                {
                new Vector3(
                    eventRect.x,
                    eventRect.y
                ),

                new Vector3(
                    eventRect.xMax,
                    eventRect.y
                ),

                new Vector3(
                    eventRect.xMax,
                    eventRect.yMax
                ),

                new Vector3(
                    eventRect.x,
                    eventRect.yMax
                ),

                new Vector3(
                    eventRect.x,
                    eventRect.y
                )
            };

                Handles.DrawAAPolyLine(
                    3f,
                    lines
                );

                Handles.color =
                    old;

                Handles.EndGUI();
            }

            GUI.Label(
                new Rect(
                    5,
                    y - 2,
                    80,
                    20
                ),
                eventType ==
                EffectEventType.Hit
                ? $"Hit{hitId}"
                : eventType.ToString()
            );
        }
    }

    private void DrawEventFields( SerializedProperty e, EffectEventType type,int index)
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

    private void DrawHitEvent(
    SerializedProperty e,
    int index
)
    {
        EditorGUILayout.PropertyField(
            e.FindPropertyRelative(
                "hitId"
            )
        );

        EditorGUILayout.PropertyField(
            e.FindPropertyRelative(
                "colliderType"
            )
        );

        EditorGUILayout.PropertyField(
            e.FindPropertyRelative(
                "hitOffset"
            )
        );

        EditorGUILayout.PropertyField(
            e.FindPropertyRelative(
                "previewColor"
            )
        );

        HitColliderType type =
            (HitColliderType)
            e.FindPropertyRelative(
                "colliderType"
            ).enumValueIndex;

        switch (type)
        {
            case HitColliderType.Sphere:

                EditorGUILayout.PropertyField(
                    e.FindPropertyRelative(
                        "hitRadius"
                    )
                );

                break;

            case HitColliderType.Box:

                EditorGUILayout.PropertyField(
                    e.FindPropertyRelative(
                        "hitBoxSize"
                    )
                );

                break;

            case HitColliderType.Capsule:

                EditorGUILayout.PropertyField(
                    e.FindPropertyRelative(
                        "capsuleRadius"
                    )
                );

                EditorGUILayout.PropertyField(
                    e.FindPropertyRelative(
                        "capsuleHeight"
                    )
                );

                EditorGUILayout.PropertyField(
                    e.FindPropertyRelative(
                        "capsuleDirection"
                    )
                );

                break;
        }

        if (
            GUILayout.Button(
                "Edit"
            )
        )
        {
            selectedEventIndex =
                index;

            editingEventIndex =
                index;

            editHitEvent =
                (EffectEvent)
                e.boxedValue;

            PreviewFrame(
                e.FindPropertyRelative(
                    "frame"
                ).intValue
            );
        }
    }

    private void DrawSoundEvent(
    SerializedProperty e
)
    {
        SerializedProperty useBGM =
            e.FindPropertyRelative(
                "useBGM"
            );

        EditorGUILayout.PropertyField(
            useBGM,
            new GUIContent(
                "Use BGM"
            )
        );

        if (useBGM.boolValue)
        {
            EditorGUILayout.PropertyField(
                e.FindPropertyRelative(
                    "bgm"
                )
            );
        }
        else
        {
            EditorGUILayout.PropertyField(
                e.FindPropertyRelative(
                    "se"
                )
            );
        }
    }

    private void DrawCameraShakeEvent(
    SerializedProperty e
)
    {
        EditorGUILayout.PropertyField(
            e.FindPropertyRelative(
                "shakePower"
            )
        );

        EditorGUILayout.PropertyField(
            e.FindPropertyRelative(
                "shakeTime"
            )
        );

        EditorGUILayout.PropertyField(
            e.FindPropertyRelative(
                "shakeAxis"
            )
        );

        EditorGUILayout.PropertyField(
            e.FindPropertyRelative(
                "shakeCurve"
            )
        );
    }

    private void DrawFunctionEvent(
    SerializedProperty e
)
    {
        EditorGUILayout.PropertyField(
            e.FindPropertyRelative(
                "onEvent"
            )
        );
    }

    private void PreviewFrame(
        int frame
    )
    {
        if (currentData == null)
            return;

        if (previewInstance != null)
        {
            DestroyImmediate(
                previewInstance
            );
        }

        previewInstance =
            Instantiate(
                currentData.prefab
            );

        previewInstance.hideFlags =
            HideFlags.HideAndDontSave;

        ParticleSystem particle =
            previewInstance
            .GetComponentInChildren
            <
                ParticleSystem
            >();

        if (particle == null)
            return;

        EffectPlayer player =
            previewInstance
            .GetComponent
            <
                EffectPlayer
            >();

        SerializedObject so =
            new SerializedObject(player);

        int frameRate =
            so.FindProperty(
                "frameRate"
            ).intValue;

        float time =
      frame /
      (float)Mathf.Max(
          1,
          frameRate
      );


        particle.Stop(
            true,
            ParticleSystemStopBehavior
            .StopEmittingAndClear
        );

        particle.Simulate(
            time,
            true,
            true,
            true
        );

        SceneView.RepaintAll();

    }

    private void UpdatePlayback()
    {
        if (!isPlaying)
            return;

        if (currentPlayer == null)
            return;

        double currentTime =
            EditorApplication.timeSinceStartup;

        double delta =
            currentTime - lastTime;

        lastTime = currentTime;

        playbackFrame +=
            (float)(
               delta *
                currentPlayer.FrameRate *
                playbackSpeed
            );

        previewFrame =
            Mathf.FloorToInt(
                playbackFrame
            );

        int maxFrame =
            GetMaxFrame();

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

    private void UpdateParticlePlayback(
    float delta
)
    {
        if (previewInstance == null)
        {
            previewInstance =
                Instantiate(
                    currentData.prefab
                );

            previewInstance.hideFlags =
                HideFlags.HideAndDontSave;
        }

        ParticleSystem particle =
            previewInstance
            .GetComponentInChildren
            <
                ParticleSystem
            >();

        if (particle == null)
            return;

        particle.Simulate(
     delta * playbackSpeed,
     true,
     false,
     true
 );

        SceneView.RepaintAll();
    }

    private Rect GetEventRect(
    SerializedProperty e,
    Rect timelineRect,
    float frameWidth,
    int stack
)
    {
        int frame =
            e.FindPropertyRelative(
                "frame"
            ).intValue;

        EffectEventType type =
            (EffectEventType)
            e.FindPropertyRelative(
                "type"
            ).enumValueIndex;

        float x =
            timelineRect.x +
            frame * frameWidth;

        float y =
            timelineRect.y + 35 +
            stack * 28;

        if (type == EffectEventType.Hit)
        {
            int endFrame =
                e.FindPropertyRelative(
                    "endFrame"
                ).intValue;

            float endX =
                timelineRect.x +
                endFrame * frameWidth;

            return new Rect(
                x,
                y,
                Mathf.Max(
                    EVENT_MIN_WIDTH,
                    endX - x
                ),
                EVENT_HEIGHT
            );
        }

        return new Rect(
            x,
            y,
            EVENT_MIN_WIDTH,
            EVENT_HEIGHT
        );
    }

    private int GetMaxFrame()
    {
        int maxFrame = 60;

        //========================
        // Event側
        //========================

        foreach (
            EffectEvent e
            in currentPlayer.Events
        )
        {
            maxFrame =
                Mathf.Max(
                    maxFrame,
                    e.frame,
                    e.endFrame
                );
        }

        //========================
        // Effect長さ側
        //========================
        if (
            currentPlayer.MainParticle != null
        )
        {
            ParticleSystem.MainModule main =
                currentPlayer.MainParticle.main;

            float duration =
                main.duration;

            if (main.loop)
            {
                duration = 0;
            }

            int particleFrame =
                Mathf.CeilToInt(
                    duration *
                    currentPlayer.FrameRate
                );

            maxFrame =
                Mathf.Max(
                    maxFrame,
                    particleFrame
                );
        }

        return maxFrame;
    }

    private void RecordUndo(
    string name
)
    {
        Undo.RecordObject(
            currentPlayer,
            name
        );

        EditorUtility.SetDirty(
            currentPlayer
        );
    }
}