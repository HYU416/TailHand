using UnityEngine;

public class ObjectFlasher : MonoBehaviour
{
    [System.Serializable]
    public struct FFlashingObject
    {
        public GameObject flashingPart;
        public Renderer renderer;
        [HideInInspector] public Material material;
        [HideInInspector] public Color originalEmissionColor;
    }

    [System.Serializable]
    public struct FFlashing
    {
        [HideInInspector] public float deltaTime;
        public float flashDuration;                                 // 点滅間隔
        public int flashCount;                                      // 点滅させる回数
        [HideInInspector] public int flashCounter;                  // 点滅したか回数
        [Range(0.0f, 1.0f)] public float maxBrightness;             // 点滅の明るさ最大値
        [HideInInspector] public float minBrightness;               // 点滅の明るさ最低値
        [HideInInspector] public bool bReverse;
    }

    [SerializeField] private FFlashingObject[] flashingObjects;
    [SerializeField] private FFlashing flashing;

    public void InitializeFlashingData()
    {
        for (int i = 0; i < flashingObjects.Length; ++i)
        {
            ref var obj = ref flashingObjects[i];
            if (obj.renderer == null)
                continue;

            // 複製したマテリアルをセット
            obj.material = obj.renderer.material;
            // エミッション機能をセット
            obj.material.EnableKeyword("_EMISSION");
            // 元の色を取得
            if (obj.material.HasProperty("_EmissionColor"))
            {
                obj.originalEmissionColor = obj.material.GetColor("_EmissionColor");

                if (obj.originalEmissionColor.r == 0.0f &&
                    obj.originalEmissionColor.g == 0.0f &&
                    obj.originalEmissionColor.b == 0.0f)
                {
                    obj.originalEmissionColor = Color.white;
                }
            }
            else
            {
                obj.originalEmissionColor = Color.white;
            }


        }
        flashing.minBrightness = 0.0f;
        ResetFlashingData();
    }

    public void ResetFlashingData()
    {
        flashing.deltaTime = 0.0f;
        flashing.bReverse = false;
        flashing.flashCounter = 0;
    }

    // 点滅処理
    public void UpdateFlashing()
    {
        // 規定回数に達したらリターン
        if (HasFinishedFlashing())
            return;

        if (flashing.bReverse)
        {
            flashing.deltaTime -= Time.deltaTime;
            if (flashing.deltaTime < 0.0f)
            {
                // カウント増加
                flashing.flashCounter++;

                flashing.deltaTime = 0.0f;
                flashing.bReverse = false;
            }
        }
        else
        {
            flashing.deltaTime += Time.deltaTime;
            if (flashing.deltaTime > flashing.flashDuration * 0.5f)
            {
                flashing.deltaTime = flashing.flashDuration * 0.5f;
                flashing.bReverse = true;
            }
        }

        float correctional = 1 / flashing.flashDuration;
        float alpha = flashing.deltaTime * correctional * 2.0f;

        for (int i = 0; i < flashingObjects.Length; ++i)
        {
            ref var obj = ref flashingObjects[i];
            if (obj.material == null)
                continue;

            // 明るさの倍率をman〜mixの間で補間
            float currentBrightness = Mathf.Lerp(flashing.minBrightness, flashing.maxBrightness, alpha);
            // マテリアルの明るさを補正
            Color calculatedColor = obj.originalEmissionColor * currentBrightness;
            // マテリアルの変更
            obj.material.SetColor("_EmissionColor", calculatedColor);
        }
    }

    // 点滅動作終了時の処理
    public void ResetFlashing()
    {
        for (int i = 0; i < flashingObjects.Length; ++i)
        {
            ref var obj = ref flashingObjects[i];
            if (obj.material == null) continue;

            // エミッションを真っ黒（発光ゼロ）にする
            obj.material.SetColor("_EmissionColor", Color.clear);
        }

    }

    // フラッシュ回数が規定回数に達したか
    public bool HasFinishedFlashing()
    {
        if (flashing.flashCounter >= flashing.flashCount)
            return true;
        return false;
    }
}
