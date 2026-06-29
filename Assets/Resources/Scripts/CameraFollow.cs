using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    private Vector3 pos;
    private Vector3 target;

    public Vector3 shakeOffset;

    [SerializeField] GameObject player;
    [SerializeField] GameObject Boss;
    [SerializeField] GameOver gameOver;

    [SerializeField] float CameraDistance = 5.0f;
    [SerializeField] float CameraHeight = 3.0f;

    [SerializeField] float angleLimit = 30.0f;
    [SerializeField] float moveSmooth = 8.0f;
    [SerializeField] float lookSmooth = 10.0f;

    [SerializeField] float bossHeight = 10.0f;

    private void Start()
    {
        MySoundManeger.Play(gameObject, BGMList.BGM_GAME);

        //player = GameObject.FindGameObjectWithTag("Player");
        //Boss = GameObject.FindGameObjectWithTag("Boss");

        //if (player == null) Debug.LogError("Playerタグが見つかりません。");
        //if (Boss == null) Debug.LogError("Bossタグが見つかりません。");
    }

    private void LateUpdate()
    {
        if (player == null || Boss == null || gameOver.IsStart()) return;

        Vector3 bossPos = Boss.transform.position;
        bossPos.y = bossHeight;
        Vector3 playerPos = player.transform.position;

        // ボス→プレイヤー方向
        Vector3 bossToPlayer = playerPos - bossPos;
        bossToPlayer.y = 0f;
        bossToPlayer.Normalize();

        // ボス→カメラ方向
        Vector3 bossToCamera = transform.position - bossPos;
        bossToCamera.y = 0f;
        bossToCamera.Normalize();

        // 角度差
        float angle = Vector3.SignedAngle(bossToPlayer, bossToCamera, Vector3.up);

        Vector3 targetDir = bossToCamera;

        // 30度より右にズレていたら、30度の位置まで戻す
        if (angle > angleLimit)
        {
            targetDir = Quaternion.Euler(0f, angleLimit, 0f) * bossToPlayer;
        }
        // 30度より左にズレていたら、-30度の位置まで戻す
        else if (angle < -angleLimit)
        {
            targetDir = Quaternion.Euler(0f, -angleLimit, 0f) * bossToPlayer;
        }

        float distance = Vector3.Distance(bossPos, playerPos) + CameraDistance;

        Vector3 targetPos = bossPos + targetDir * distance;
        targetPos.y = bossPos.y + CameraHeight;

        target = (playerPos + bossPos) / 2.0f;

        // 位置を滑らかに移動
        transform.position = Vector3.Lerp(
            transform.position,
            targetPos + shakeOffset,
            moveSmooth * Time.deltaTime
        );

        // 向きを滑らかに変更
        Quaternion targetRot = Quaternion.LookRotation(target - transform.position);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            lookSmooth * Time.deltaTime
        );
    }
}