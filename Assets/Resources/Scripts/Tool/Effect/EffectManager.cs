// EffectManager.cs

using System.Collections.Generic;
using UnityEngine;

public class EffectManager :
    MonoBehaviour
{
    private static EffectManager instance;

    public static EffectManager Instance
    {
        get
        {
            if (instance == null)
            {
                CreateManager();
            }

            return instance;
        }
    }

    [SerializeField]
    private EffectDatabase database;

    private Dictionary
    <
        EffectType,
        GameObject
    >
    prefabTable =
        new();

    private Dictionary
    <
        EffectType,
        Queue<EffectPlayer>
    >
    pool =
        new();

    private static void CreateManager()
    {
        GameObject obj =
            new GameObject(
                "EffectManager"
            );

        instance =
            obj.AddComponent<EffectManager>();

        DontDestroyOnLoad(obj);
    }

    private void Awake()
    {
        if (
            instance != null &&
            instance != this
        )
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        DontDestroyOnLoad(gameObject);

        Initialize();
    }

    private void Initialize()
    {
        if (database == null)
        {
            database =
                Resources.Load
                <
                    EffectDatabase
                >
                (
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

        foreach (
            EffectData data
            in database.effects
        )
        {
            if (data.prefab == null)
                continue;

            if (
                !prefabTable.ContainsKey(
                    data.type
                )
            )
            {
                prefabTable.Add(
                    data.type,
                    data.prefab
                );
            }
        }
    }

    public EffectPlayer Play(
        EffectType type,
        Vector3 position
    )
    {
        return Play(
            type,
            position,
            Quaternion.identity
        );
    }

    public EffectPlayer Play(
        EffectType type,
        Vector3 position,
        Quaternion rotation
    )
    {
        EffectPlayer player =
            GetEffect(type);

        if (player == null)
            return null;

        Transform t =
            player.transform;

        t.position = position;

        t.rotation = rotation;

        player.gameObject.SetActive(true);

        player.EffectStart();

        return player;
    }

    private EffectPlayer GetEffect(
        EffectType type
    )
    {
        if (!pool.ContainsKey(type))
        {
            pool.Add(
                type,
                new Queue<EffectPlayer>()
            );
        }

        if (pool[type].Count > 0)
        {
            return pool[type]
                .Dequeue();
        }

        if (
            !prefabTable.ContainsKey(
                type
            )
        )
        {
            Debug.LogError(
                $"Effect Not Found : {type}"
            );

            return null;
        }

        GameObject obj =
            Instantiate(
                prefabTable[type]
            );

        EffectPlayer player =
            obj.GetComponent
            <
                EffectPlayer
            >();

        if (player == null)
        {
            Debug.LogError(
                "EffectPlayer Missing"
            );

            return null;
        }

        return player;
    }

    public void Release(
        EffectType type,
        EffectPlayer player
    )
    {
        if (!pool.ContainsKey(type))
        {
            pool.Add(
                type,
                new Queue<EffectPlayer>()
            );
        }

        player.gameObject
            .SetActive(false);

        pool[type]
            .Enqueue(player);
    }
}