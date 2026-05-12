using System.Collections.Generic;
using UnityEngine;

public class EffectManager : MonoBehaviour
{
    /// シングルトンインスタンス
    private static EffectManager instance;
    /// シングルトンインスタンスへのアクセスプロパティ
    public static EffectManager Instance
    {
        /// インスタンスが存在しない場合は新規作成
        get
        {
            if (instance == null)
            {
                CreateManager();
            }
            return instance;
        }
    }

    /// エフェクトデータベースへの参照
    [SerializeField]
    private EffectDatabase database;

    /// エフェクトタイプとプレハブの対応表
    private Dictionary<EffectType, GameObject> prefabTable = new();

    /// エフェクトタイプとエフェクトプレイヤーのプール
    private Dictionary<EffectType, Queue<EffectPlayer>> pool = new();

    /// エフェクトマネージャーの作成
    private static void CreateManager()
    {
        GameObject obj = new GameObject("EffectManager");
        instance = obj.AddComponent<EffectManager>();
        DontDestroyOnLoad(obj);
    }

    private void Awake()
    {
        // すでにインスタンスが存在する場合は、重複を避けるためにこのオブジェクトを破棄
        if (instance != null && instance != this )
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        
        DontDestroyOnLoad(gameObject);
        //初期化
        Initialize();
    }

    // 初期化
    private void Initialize()
    {
        // データベースがアサインされていない場合は、Resourcesからロード
        if (database == null)
        {
            database = Resources.Load<EffectDatabase> ("Data/EffectData/EffectDatabase");
        }
        // データベースのロード失敗のチェック
        if (database == null)
        {
            Debug.LogError( "EffectDatabase Not Found");
            return;
        }
        //prefabTableの初期化
        prefabTable.Clear();

        // データベースのエフェクトデータからprefabTableを構築
        foreach (EffectData data in database.effects)
        {
            if (data.prefab == null)
                continue;
            // 同じEffectTypeが複数登録されている場合は、最初のものだけを使用
            if (!prefabTable.ContainsKey( data.type ) )
            {
                prefabTable.Add( data.type, data.prefab);
            }
        }
    }

    /// <summary>
    /// 指定したエフェクトタイプのエフェクトを、指定した位置で再生します。
    /// </summary>
    /// <param name="type">再生するエフェクトのタイプ</param>
    /// <param name="position">エフェクトを再生する位置</param>
    public EffectPlayer Play( EffectType type,Vector3 position )
    {
        //再生
        return Play(type, position,Quaternion.identity);
    }

    /// <summary>
    /// 指定したエフェクトタイプのエフェクトを、指定した位置と回転で再生します。
    /// <param name="type">再生するエフェクトのタイプ</param>
    /// <param name="position">エフェクトを再生する位置</param>
    /// <param name="rotation">エフェクトを再生する回転</param>
    public EffectPlayer Play( EffectType type,Vector3 position,Quaternion rotation)
    {
        //エフェクトプレイヤーを取得
        EffectPlayer player =GetEffect(type);

        if (player == null)
            return null;

        //エフェクトプレイヤーの位置と回転を設定
        Transform t = player.transform;
        //指定した位置と回転をエフェクトプレイヤーに適用
        t.position = position;
        t.rotation = rotation;
        //エフェクトプレイヤーをアクティブにして再生開始
        player.gameObject.SetActive(true);
        player.EffectStart();

        return player;
    }

    // 指定したエフェクトタイプのエフェクトプレイヤーをプールから取得するか、新規に作成して返す
    private EffectPlayer GetEffect(EffectType type )
    {
        //プールに指定したタイプのエフェクトプレイヤーが存在しない場合は、新しいキューを作成
        if (!pool.ContainsKey(type))
        {
            pool.Add( type,new Queue<EffectPlayer>());
        }
        //プールから指定したタイプのエフェクトプレイヤーを取得
        if (pool[type].Count > 0)
        {
            return pool[type].Dequeue();
        }
        //プールに指定したタイプのエフェクトプレイヤーが存在しない場合は、新規に作成
        if (!prefabTable.ContainsKey(type))
        {
            Debug.LogError( $"Effect Not Found : {type}");
            return null;
        }
        //プレハブから新しいエフェクトプレイヤーを作成
        GameObject obj =Instantiate( prefabTable[type]);
        //作成したエフェクトプレイヤーを子オブジェクトとして管理
        EffectPlayer player =obj.GetComponent< EffectPlayer>();

        if (player == null)
        {
            Debug.LogError( "EffectPlayer Missing"  );
            return null;
        }

        return player;
    }

    /// <summary>
    /// 指定したエフェクトプレイヤーをプールに返却して再利用可能
    /// </summary>
    /// <param name="type">エフェクトのタイプ</param>
    /// <param name="player">返却するエフェクトプレイヤー</param>
    public void Release(EffectType type, EffectPlayer player )
    {
        //プールに指定したタイプのエフェクトプレイヤーが存在しない場合は、新しいキューを作成
        if (!pool.ContainsKey(type))
        {
            pool.Add(type,new Queue<EffectPlayer>() );
        }
        //エフェクトプレイヤーを非アクティブにしてからプールに返却
        player.gameObject.SetActive(false);
        pool[type].Enqueue(player);
    }
}