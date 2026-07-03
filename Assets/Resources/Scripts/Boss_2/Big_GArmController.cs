using UnityEngine;

public class Big_GArmController : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform parentTransform;
    [SerializeField] private float returnDuration;                  // 腕が元の位置に戻るまでの時間
    [SerializeField] private PlayerCatchEnemy playerCatchEnemy;
    [SerializeField] private float launchForce;                     // 腕が吹き飛ぶ強さ

    private Vector3 savedLocalPosition;
    private Quaternion savedLocalRotation;
    private bool bReturned;
    private float moveSpeed = 0.0f;
    private float rotateSpeed = 0.0f;
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        savedLocalPosition = this.transform.localPosition;
        savedLocalRotation = this.parentTransform.localRotation;
        bReturned = true;

        if (playerCatchEnemy == null)
            return;
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    // Update is called once per frame
    void Update()
    {
        // 腕を元の位置に戻す処理
        if (!bReturned)
        {
            if (rb == null || playerCatchEnemy == null)
                return;

            // 等速移動
            this.transform.localPosition = Vector3.MoveTowards(this.transform.localPosition, savedLocalPosition, moveSpeed * Time.deltaTime);
            // 等速回転
            this.transform.localRotation = Quaternion.RotateTowards(this.transform.localRotation, savedLocalRotation, rotateSpeed * Time.deltaTime);

            // 目的地に到達したかチェック
            if (Vector3.Distance(this.transform.localPosition, savedLocalPosition) < 0.01f &&
                Quaternion.Angle(this.transform.localRotation, savedLocalRotation) < 0.1f)
            {
                bReturned = true;
            }
        }
        if (playerCatchEnemy.IsCatching())
        {
            this.gameObject.tag = "Projectile";
        }
    }


    private void BlowOff()
    {
        Vector3 randomDirection = new Vector3(
            Random.Range(-1.0f, 1.0f),
            Random.Range(0.3f, 0.7f), // 0.2を加えることで、真横や少し下に行きにくくする
            Random.Range(-1.0f, 1.0f)
        ).normalized;

        // 吹き飛ばす
        rb.AddForce(randomDirection * launchForce, ForceMode.Impulse);

        // 回転
        rb.AddTorque(Random.insideUnitSphere * 5.0f, ForceMode.Impulse);
    }

    public void DetachArm()
    {
        if (rb == null || playerCatchEnemy == null)
            return;

        // 親をなしにする（ワールド空間で今の位置を維持する）
        this.transform.SetParent(null, true);
        rb.isKinematic = false;
        rb.useGravity = true;
        BlowOff();
        Debug.Log(this.gameObject.layer);

    }

    public void ReattachArm()
    {
        this.transform.SetParent(parentTransform, true);
        bReturned = false;
        moveSpeed = Vector3.Distance(this.transform.localPosition, savedLocalPosition) / returnDuration;
        rotateSpeed = Quaternion.Angle(this.transform.localRotation, savedLocalRotation) / returnDuration;
        rb.isKinematic = true;
        this.gameObject.tag = "Boss";
        this.gameObject.layer = LayerMask.NameToLayer("Enemy"); ;
    }
}
