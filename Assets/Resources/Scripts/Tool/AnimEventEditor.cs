//==============================================================
// アニメーションイベントエディタ
// 制作者：中嶋飛賀
//
//履歴
// 2026 4/28　新規作成
//==============================================================

using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;

public class AnimEventEditor : EditorWindow
{
    //編集するデータ
    AnimEventDataAsset data;
    //現在の再生時間（秒）
    float fCurrentTime;
    //再生中かどうか
    bool isPlaying;

    //メニューからウィンドウを開く
    [MenuItem("Tools/AnimEventEditor")]
    static void Open()
    {
        GetWindow<AnimEventEditor>();
    }

    //UIの描画
    private void OnGUI()
    {
        GUILayout.BeginHorizontal();
        //新しいデータの作成ボタン
        if (GUILayout.Button("New AnimEventData"))
        {
            CreateEventData();
        }

        //消去ボタン
        if (GUILayout.Button("Delete AnimEventData"))
        {
            if (data != null)
            {
                string path = AssetDatabase.GetAssetPath(data);
                if (EditorUtility.DisplayDialog("Delete AnimEventData", "Are you sure you want to delete this AnimEventData?", "Yes", "No"))
                {
                    AssetDatabase.DeleteAsset(path);
                    data = null;
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Delete AnimEventData", "No AnimEventData selected.", "OK");
            }
            GUILayout.EndHorizontal();
            return;

        }
        GUILayout.EndHorizontal();

        EditorGUILayout.Space();

        //編集するデータを選択
        data = (AnimEventDataAsset)EditorGUILayout.ObjectField("AnimEventData", data, typeof(AnimEventDataAsset), false);

        // nullならここで止める（重要）
        if (data == null)
        {
            EditorGUILayout.HelpBox("AnimEventDataを選択または作成してください", MessageType.Info);
            return;
        }

        //アニメーションクリップ選択
        data.clip = (AnimationClip)EditorGUILayout.ObjectField(
            "Animation Clip",
            data.clip,
            typeof(AnimationClip),
            false
        );

        //以降の処理を止める
        if (data.clip == null)
        {
            EditorGUILayout.HelpBox("Animation Clipを設定してください", MessageType.Warning);
            return;
        }

        // 1秒単位のフレーム数
        //EditorGUILayout.LabelField("1秒単位のフレーム数");
        data.frameRate = EditorGUILayout.IntField("Frame Rate", data.frameRate);

        if (data.frameRate <= 0)
        {
            EditorGUILayout.HelpBox("Frame Rateは1以上の値を設定してください", MessageType.Warning);
            return;
        }

        //アニメーションの長さを取得
        float Length = data.clip.length;
        //総フレーム数を計算
        int totalFrame = Mathf.RoundToInt(Length * data.frameRate);

        //再生時間のスライダー
        fCurrentTime = EditorGUILayout.Slider("Current Time", fCurrentTime, 0, Length);

        GUILayout.BeginHorizontal();
        //再生/停止ボタン
        if (GUILayout.Button(isPlaying ? "Stop" : "Play"))
        {
            isPlaying = !isPlaying; 
        }

        //現在のフレームにイベントを追加
        if (GUILayout.Button("Add Event"))
        {
            //Ctrl+Zで元に戻せるように変更を記録
            Undo.RecordObject(data, "Add Anim Event");
            data.events.Add(new AnimEventData()
            {
                frame = Mathf.RoundToInt(fCurrentTime * data.frameRate),
                Type = AnimEventType.Attack
            });
            //データ変更をunityに通知
            EditorUtility.SetDirty(data);
        }
        GUILayout.EndHorizontal();

        EditorGUILayout.Space();

        int deleteIndex = -1;

        //イベントのリストを表示
        for (int i = 0; i < data.events.Count; i++)
        {
            EditorGUILayout.BeginVertical("box");

             //フレーム番号
            GUILayout.BeginHorizontal();
            GUILayout.Label("Frame", GUILayout.Width(50));
            data.events[i].frame = EditorGUILayout.IntField(data.events[i].frame);
            //イベントタイプ
            GUILayout.Label("Type", GUILayout.Width(40));
            data.events[i].Type = (AnimEventType)EditorGUILayout.EnumPopup(data.events[i].Type);
            GUILayout.EndHorizontal();

            //SEリスト
            GUILayout.BeginHorizontal();
            GUILayout.Label("SE", GUILayout.Width(30));
            data.events[i].list = (SEList)EditorGUILayout.EnumPopup(data.events[i].list);

            GUILayout.FlexibleSpace();

            //削除ボタン
            if (GUILayout.Button("Delete", GUILayout.Width(80)))
            {
                deleteIndex = i;
            }
            GUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        // ループ外で削除
        if (deleteIndex != -1)
        {
            Undo.RecordObject(data, "Delete Anim Event");
            data.events.RemoveAt(deleteIndex);
            EditorUtility.SetDirty(data);
        }
    }

    //エディターが有効になったときに呼ばれる
    private void OnEnable()
    {
        EditorApplication.update += Update;
    }

    //エディターが無効になったときに呼ばれる
    private void OnDisable()
    {
        EditorApplication.update -= Update;
    }

    int prevFrame = -1;

    private void Update()
    {
        if (data == null || !isPlaying) return;

        fCurrentTime += Time.deltaTime;

        if (fCurrentTime > data.clip.length)
        {
            fCurrentTime = 0;
            prevFrame = -1; 
        }

        int currentFrame = Mathf.RoundToInt(fCurrentTime * data.frameRate);

       
        if (currentFrame != prevFrame)
        {
            CheckEvents(currentFrame);
            prevFrame = currentFrame;
        }

        Repaint();
    }

    //指定したフレームにイベントがあるかチェック
    void CheckEvents(int frame)
    {
        foreach (var e in data.events)
        {
            if (e.frame == frame)
            {
                ExecuteEvent(e);
            }
        }
    }

    //イベントの実行
    void ExecuteEvent(AnimEventData data)
    {
        switch (data.Type)
        {
            case AnimEventType.Attack:
                //TODO:
                //攻撃イベントの処理
                Debug.Log("発生した攻撃イベントのフレーム: " + data.frame);
                break;
            case AnimEventType.SE:
                //TODO:
                //SEイベントの処理
                Debug.Log("発生したSEイベントのフレーム: " + data.frame);
                break;
            case AnimEventType.Effect:
                //TODO:
                //エフェクトイベントの処理
                Debug.Log("発生したEffectイベントのフレーム: " + data.frame);
                break;
        }
    }

    void CreateEventData()
    {
        //新しいAnimEventDataAssetを作成
        var newData = ScriptableObject.CreateInstance<AnimEventDataAsset>();

        // デフォルトフォルダ
        string root = "Assets/Resources";
        string folderName = "AnimationEventDatas";
        string defaultFolder = root + "/" + folderName;

        // フォルダが無ければ作成
        if (!AssetDatabase.IsValidFolder(root))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        if (!AssetDatabase.IsValidFolder(defaultFolder))
        {
            AssetDatabase.CreateFolder(root, folderName);
        }

        // 保存ダイアログ
        string path = EditorUtility.SaveFilePanelInProject(
            "Save AnimEventData",
            "NewAnimEventData",
            "asset",
            "Please enter a file name to save the AnimEventData.",
            defaultFolder 
        );

        if (!string.IsNullOrEmpty(path))
        {
            AssetDatabase.CreateAsset(newData, path);
            AssetDatabase.SaveAssets();
            data = newData;
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = newData;
        }
    }
}

