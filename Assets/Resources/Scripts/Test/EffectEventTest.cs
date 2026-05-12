using UnityEngine;

public class EffectEventTest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //エフェクトの再生
        EffectManager.Instance.Play(EffectType.Explosion, transform.position);
    }
}
