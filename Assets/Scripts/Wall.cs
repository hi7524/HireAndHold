using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class Wall : MonoBehaviour, IDamagable
{
    [SerializeField] private float maxHp;
    [SerializeField] private float currentHp;
    [Space]
    [SerializeField] private Slider hpSlider;
    [Header("Managers")]
    [SerializeField] GameManager gameManager;
    [SerializeField] StageManager stageManager;

    public float CurrentHp => currentHp;
    public float MaxHp => maxHp;
    private bool isDead = false; 

    private PassiveSkillManager passiveSkillManager;
    private float regenTimer = 0f;
    private const float REGEN_INTERVAL = 1f; // 1초마다 회복


    private void Start()
    {
        currentHp = maxHp;
        hpSlider.value = 1f;
        isDead = false; 

        passiveSkillManager = FindFirstObjectByType<PassiveSkillManager>();
    }

    private void Update()
    {
        if (isDead) return;

        // 최대 HP보다 낮을 때만 회복
        if (currentHp < maxHp)
        {
            RegenerateShield();
        }
    }

    private void RegenerateShield()
    {
        if (passiveSkillManager == null) return;

        regenTimer += Time.deltaTime;

        if (regenTimer >= REGEN_INTERVAL)
        {
            regenTimer = 0f;

            PassiveSkillEffects effects = passiveSkillManager.GetCurrentEffects();
            int regenAmount = (int)(effects.shieldRegenBonus / 100f * maxHp);

            if (regenAmount > 0f)
            {
                Heal(regenAmount);
            }
        }
    }

    public void Heal(int amount)
    {
        if (isDead) return;

        int previousHp = (int)currentHp;
        currentHp = Mathf.Min(currentHp + amount, maxHp);
        hpSlider.value = currentHp / maxHp;

        if (amount > 0)
        {
            Debug.Log($"[Wall] 방벽 회복: +{amount:F1} ({previousHp:F0} → {currentHp:F0})");
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHp -= damage;
        hpSlider.value = currentHp / maxHp;

        transform.DOComplete();
        transform.DOShakePosition(0.1f, strength: 0.05f, vibrato: 1, randomness: 90, snapping: false, fadeOut: true)
            .SetUpdate(false);

        if(currentHp <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        
        stageManager.FailStage();
        gameObject.SetActive(false);
    }
}