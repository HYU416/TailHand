using UnityEngine;

public class SoundCollider : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.name == "Fin_Bone_Player_Hips")
        {
            MySoundManeger.Play(gameObject, SEList.SE_ROLL);
        }
    }
}
