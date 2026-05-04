//§ìF¬—Ñ‘åŒå@@‰½‚©‚ ‚ê‚Î‚²˜A—‚­‚¾‚³‚¢
using UnityEngine;

public class BossBombShooter : MonoBehaviour
{
    [System.Serializable]
    public class BossLayer
    {
        [Header("‰ñ“]‚³‚¹‚é“·‘ÌE–C‘äRoot")]
        public Transform rotateRoot;

        [Header("’e‚ğo‚·GUN–{‘Ì")]
        public Transform gun;

        [Header("‚±‚ÌŠK‘w‚Ì‰ñ“]‘¬“x")]
        public float rotateSpeed = 90f;

        [Header("‹t‰ñ“]‚É‚·‚é")]
        public bool reverseRotate = false;

        [Header("”­ËŠÔŠu")]
        public float fireInterval = 1.0f;

        [Header("’e‚Ì‘¬“x")]
        public float bombSpeed = 8.0f;

        [Header("GUN’†S‚©‚ç–Cgæ’[‚Ü‚Å‚Ì‹——£")]
        public float muzzleOffset = 1.0f;

        [Header("’e‚ğo‚·•ûŒü")]
        public ShootAxis shootAxis = ShootAxis.Forward;

        [Header("­‚µã‚Ö”ò‚Î‚·—Í")]
        public float upwardPower = 0.0f;

        [HideInInspector] public float fireTimer;
    }

    public enum ShootAxis
    {
        Forward,
        Back,
        Right,
        Left,
        Up,
        Down
    }

    [Header("ŠK‘w‚²‚Æ‚Ìİ’è")]
    public BossLayer[] layers;

    [Header("”š’ePrefab")]
    public GameObject bombPrefab;

    void Update()
    {
        for (int i = 0; i < layers.Length; i++)
        {
            RotateLayer(layers[i]);
            ShootTimer(layers[i]);
        }
    }

    void RotateLayer(BossLayer layer)
    {
        if (layer.rotateRoot == null) return;

        float direction = layer.reverseRotate ? -1f : 1f;

        layer.rotateRoot.Rotate(
            0f,
            layer.rotateSpeed * direction * Time.deltaTime,
            0f
        );
    }

    void ShootTimer(BossLayer layer)
    {
        layer.fireTimer += Time.deltaTime;

        if (layer.fireTimer >= layer.fireInterval)
        {
            layer.fireTimer = 0f;
            ShootBomb(layer);
        }
    }

    void ShootBomb(BossLayer layer)
    {
        if (bombPrefab == null) return;
        if (layer.gun == null) return;

        Vector3 shootDirection = GetShootDirection(layer.gun, layer.shootAxis);

        Vector3 spawnPosition =
            layer.gun.position + shootDirection * layer.muzzleOffset;

        GameObject bomb = Instantiate(
            bombPrefab,
            spawnPosition,
            Quaternion.LookRotation(shootDirection)
        );

        Rigidbody rb = bomb.GetComponent<Rigidbody>();

        if (rb != null)
        {
            Vector3 finalDirection = shootDirection + Vector3.up * layer.upwardPower;
            finalDirection.Normalize();

            rb.linearVelocity = finalDirection * layer.bombSpeed;
        }
    }

    Vector3 GetShootDirection(Transform gun, ShootAxis axis)
    {
        switch (axis)
        {
            case ShootAxis.Forward:
                return gun.forward;

            case ShootAxis.Back:
                return -gun.forward;

            case ShootAxis.Right:
                return gun.right;

            case ShootAxis.Left:
                return -gun.right;

            case ShootAxis.Up:
                return gun.up;

            case ShootAxis.Down:
                return -gun.up;

            default:
                return gun.forward;
        }
    }
}