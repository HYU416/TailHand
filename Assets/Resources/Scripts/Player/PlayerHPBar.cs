using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PlayerHPBar : MonoBehaviour
{
    [SerializeField] private Player player = null;

    [Header("HP設定")]
    [SerializeField] private float maxHP = 100f;
    [SerializeField] private float currentHP = 100f;

    [Header("HPバー設定")]
    [SerializeField] private Image hpFrontImage;

    [Header("ダメージ後の無敵時間")]
    [SerializeField] private float damageCooldown = 0.1f;

    [Header("死亡時イベント")]
    public UnityEvent onDead;

    private float lastDamageTime = -999f;
    private bool isDead;

    public float MaxHP => maxHP;
    public float CurrentHP => currentHP;
    public bool IsDead => isDead;

    private void Awake()
    {
        currentHP = Mathf.Clamp(currentHP, 0f, maxHP);

        SetupHPImage();
        UpdateHPBar();
    }

    private void Start()
    {
        SetupHPImage();
        UpdateHPBar();
        if (!player)
            Debug.Log("Error PlayerHPBar NoneSetPlayerScript");
    }

    private void SetupHPImage()
    {
        if (hpFrontImage == null)
        {
            Debug.LogWarning("Hp Front Image が設定されていません");
            return;
        }

        hpFrontImage.gameObject.SetActive(true);

        hpFrontImage.type = Image.Type.Filled;
        hpFrontImage.fillMethod = Image.FillMethod.Horizontal;
        hpFrontImage.fillOrigin = 0; // Left
        hpFrontImage.fillAmount = 1f;

        hpFrontImage.transform.SetAsLastSibling();
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;
        if (damage <= 0f) return;

        if (Time.time < lastDamageTime + damageCooldown)
        {
            return;
        }

        lastDamageTime = Time.time;

        currentHP -= damage;
        currentHP = Mathf.Clamp(currentHP, 0f, maxHP);

        // ノックバックアニメーションに変更
        player.SwitchAnimation(AnimeState.Knockback);
        UpdateHPBar();

        Debug.Log("プレイヤーHP: " + currentHP + " / " + maxHP);

        if (currentHP <= 0f)
        {

            Die();
        }
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        if (amount <= 0f) return;

        currentHP += amount;
        currentHP = Mathf.Clamp(currentHP, 0f, maxHP);

        UpdateHPBar();
    }

    public void SetHP(float hp)
    {
        currentHP = Mathf.Clamp(hp, 0f, maxHP);

        if (currentHP > 0f)
        {
            isDead = false;
        }

        UpdateHPBar();
    }

    private void UpdateHPBar()
    {
        if (hpFrontImage == null) return;

        float hpRate = currentHP / maxHP;
        hpRate = Mathf.Clamp01(hpRate);

        hpFrontImage.fillAmount = hpRate;
        hpFrontImage.transform.SetAsLastSibling();
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;

        Debug.Log("プレイヤー死亡");

        onDead?.Invoke();
    }

    private void OnValidate()
    {
        maxHP = Mathf.Max(1f, maxHP);
        currentHP = Mathf.Clamp(currentHP, 0f, maxHP);

        if (Application.isPlaying)
        {
            UpdateHPBar();
        }
    }
}