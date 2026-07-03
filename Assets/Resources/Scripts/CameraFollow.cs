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



    [SerializeField] float introDistance = 4f;
    [SerializeField] float introHeight = 2f;
    [SerializeField] float stageDistance = 25f;
    [SerializeField] float stageHeight = 12f;

    [SerializeField] BossPhaseAttackController bossAttack;

    private float introTime = 0f;

    private Vector3 bossLandPos;
    private Vector3 bossStartPos;
    private bool bossIntroInit = false;
    private Renderer[] bossRenderers;

    public bool Gamestart;

    public bool IsGameStart()
    {
        return Gamestart;
    }

    private void Start()
    {
        Gamestart = false;

        bossIntroInit = false;

        bossLandPos = Boss.transform.position;
        bossStartPos = bossLandPos + Vector3.up * 35f;
        bossRenderers = Boss.GetComponentsInChildren<Renderer>();
        // Bossを非表示
        bossRenderers = Boss.GetComponentsInChildren<Renderer>();

        foreach (Renderer r in bossRenderers)
        {
            r.enabled = false;
        }

        var intro = MySoundManeger.Play(gameObject, BGMList.BGM_GAME);
        //intro.time += 70.0f;
        var loop = MySoundManeger.Play(gameObject, BGMList.BGM_GAME_LOOP);
        loop.Stop();
        loop.PlayScheduled(AudioSettings.dspTime + intro.clip.length - intro.time);
        //player = GameObject.FindGameObjectWithTag("Player");
        //Boss = GameObject.FindGameObjectWithTag("Boss");

        //if (player == null) Debug.LogError("Playerタグが見つかりません。");
        //if (Boss == null) Debug.LogError("Bossタグが見つかりません。");
    }

    private void LateUpdate()
    {
        if (!Gamestart)
        {
            if (player == null) return;

            introTime += Time.deltaTime;

            Vector3 center = player.transform.position + Vector3.up * introHeight;
            Vector3 forward = player.transform.forward;

            // Phase1
            if (introTime < 2.0f)
            {
                Vector3 pos = center + forward * introDistance;

                transform.position = Vector3.Lerp(
                    transform.position,
                    pos,
                    Time.deltaTime * 5f);

                transform.LookAt(center);
            }
            // Phase2
            else if (introTime < 5.0f)
            {
                float t = (introTime - 2.0f) / 3.0f;

                float angle2 = Mathf.Lerp(0f, 180f, t);

                Vector3 dir = Quaternion.Euler(0, angle2, 0) * forward;

                Vector3 pos = center + dir * introDistance;

                transform.position = pos;
                transform.LookAt(center);
            }
            // Phase3
            // Phase3
            else if (introTime < 9.0f)
            {
                float t = (introTime - 5.0f) / 4.0f;

                float dist = Mathf.Lerp(introDistance, stageDistance, t);
                float height = Mathf.Lerp(introHeight, stageHeight, t);

                Vector3 pos =
                    player.transform.position
                    - forward * dist
                    + Vector3.up * height;

                transform.position = pos;
                transform.LookAt(player.transform.position + Vector3.up * 2f);
            }
            // Phase4 Boss登場
            else if (introTime < 13.0f)
            {
                if (!bossIntroInit)
                {
                    bossIntroInit = true;

                    // Bossを表示
                    foreach (Renderer r in bossRenderers)
                    {
                        r.enabled = true;
                    }

                    Boss.transform.position = bossStartPos;
                }

                float t = (introTime - 9.0f) / 4.0f;
                t = Mathf.SmoothStep(0f, 1f, t);

                // Boss降下
                Boss.transform.position =
                    Vector3.Lerp(bossStartPos, bossLandPos, t);

                // プレイヤー斜め後ろ
                Vector3 offset =
                    -forward * 6f +
                    Vector3.left * 3f +
                    Vector3.up * 2.5f;

                transform.position =
                    player.transform.position + offset;

                // Bossを見る
                transform.LookAt(Boss.transform.position + Vector3.up * 2f);
            }
            // Phase5 Boss名表示
            // Phase5 Bossを見上げる
            // Phase5 Bossを下から上へ舐める
            // Phase5
            else if (introTime < 17.0f)
            {
                float t = (introTime - 13.0f) / 4.0f;
                t = Mathf.SmoothStep(0f, 1f, t);

                Vector3 basePos = Boss.transform.position - player.transform.forward * 10f;
                basePos.x -= 4.2f;
                basePos.z -= 2.0f;

                Vector3 startPos = basePos + Vector3.up * 1.5f;
                Vector3 endPos = basePos + Vector3.up * 12f;

                // 位置だけ移動
                transform.position = Vector3.Lerp(startPos, endPos, t);

                // LookAtはしない
            }
            // Phase6 Boss名表示
            else if (introTime < 19.0f)
            {

                // Boss名表示
                //bossNameUI.SetActive(true);
            }
            else
            {
                //bossNameUI.SetActive(false);

                bossAttack.StartBattle();
                Gamestart = true;
            }
            return;
        }


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