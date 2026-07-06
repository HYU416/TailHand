using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
            if (isQuitting)
                return null;
            if (instance == null)
            {
                CreateManager();
            }
            return instance;
        }
    }
    /// エフェクトマネージャーが初期化済みかどうかを示すプロパティ
    public static bool IsInitialized => isInitialized;

    private static bool isQuitting;

    /// エフェクトデータベースへの参照
    [SerializeField]
    private EffectDatabase database;

    /// エフェクトタイプとプレハブの対応表
    private Dictionary<EffectType, GameObject> prefabTable = new();

    /// エフェクトタイプとエフェクトプレイヤーのプール
    private Dictionary<EffectType, Queue<EffectPlayer>> pool = new();

    private Dictionary<EffectType, EffectData> dataTable = new();

    private static bool isInitialized = false;

    /// エフェクトマネージャーの作成
    private static void CreateManager()
    {
        GameObject obj = new GameObject("EffectManager");
        instance = obj.AddComponent<EffectManager>();
        DontDestroyOnLoad(obj);
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
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
        // エフェクトデータベースの初期化
        if (!isInitialized)
        {
            Initialize();
            isInitialized = true;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        pool.Clear();
    }


    private void Initialize()
    {
        if (database == null)
        {
            database =
                Resources.Load<EffectDatabase>(
                    "Data/EffectData/EffectDatabase"
                );
        }

        if (database == null)
        {
            Debug.LogError(
                "EffectDatabase Not Found"
            );

            return;
        }

        prefabTable.Clear();
        dataTable.Clear();

        foreach (EffectData data in database.effects)
        {
            if (data.prefab == null)
                continue;

            if (!prefabTable.ContainsKey(data.type))
            {
                prefabTable.Add(
                    data.type,
                    data.prefab
                );

                dataTable.Add(
                    data.type,
                    data
                );
            }
        }
    }

    /// <summary>
    /// 指定したエフェクトタイプのエフェクトを、指定した位置で再生します。
    /// </summary>
    /// <param name="type">再生するエフェクトのタイプ</param>
    /// <param name="position">エフェクトを再生する位置</param>
    public GameObject Play( EffectType type,Vector3 position )
    {
        //再生
        return Play(type, position,Quaternion.identity);
    }

    /// <summary>
    /// 指定したエフェクトタイプのエフェクトを、指定した位置と回転で再生します。
    /// <param name="type">再生するエフェクトのタイプ</param>
    /// <param name="position">エフェクトを再生する位置</param>
    /// <param name="rotation">エフェクトを再生する回転</param>
    public GameObject Play( EffectType type,Vector3 position,Quaternion rotation)
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

        return player.gameObject;
    }

    // 指定したエフェクトプレイヤーを停止
    public void Stop(EffectPlayer player)
    {
        if (player == null)
            return;

        player.EffectStop();
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
        while (pool[type].Count > 0)
        {
            EffectPlayer poolPlayer = pool[type].Dequeue();

            if (poolPlayer == null)
            {
                continue;
            }

            poolPlayer.SetEffectType(type);

            if (dataTable.ContainsKey(type))
            {
                poolPlayer.SetPlaySpeed(dataTable[type].playSpeed);
            }

            return poolPlayer;
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

        player.SetEffectType(type);
        if (dataTable.ContainsKey(type))
        {
            player.SetPlaySpeed(dataTable[type].playSpeed);
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