using UnityEngine;

public class CoreBarrier : MonoBehaviour
{
    [SerializeField] private CoreBarrierManager barrierManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (barrierManager == null)
            Destroy(this.gameObject);
    }

    // Update is called once per frame
    void Update()
    {
    }

    void OnTriggerEnter(Collider col)
    {
        if (barrierManager == null)
            Destroy(this.gameObject);

        if (col.gameObject.CompareTag("Projectile"))
        {
            barrierManager.TakeDamage();

            EighthNoteController eighth = col.GetComponent<EighthNoteController>();
            if (eighth != null)
            {
                eighth.Hit();
                return;
            }
            DupletNoteController duplet = col.GetComponent<DupletNoteController>();
            if (duplet != null)
            {
                duplet.Hit();
                return;
            }
        }
    }
}
