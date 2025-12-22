using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHPBar : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject bossHealthPanel;
    [SerializeField] private Slider healthBarSlider;
    [SerializeField] private TextMeshProUGUI bossNameText;
    
    
    private Enemy currentBoss;
    private float maxHp;

    private void Awake()
    {
        HideBossHealthBar();
    }

    private void Update()
    {
        if (currentBoss != null && currentBoss.gameObject.activeSelf)
        {
            UpdateHealthBar();
        }
    }

    // 보스 체력바 표시
    public void ShowBossHealthBar(Enemy boss, string bossName)
    {
        currentBoss = boss;
        maxHp = boss.CurrentHp; // 스폰 시점의 최대 HP
        
        bossNameText.text = bossName;
        bossHealthPanel.SetActive(true);
    }

    // 보스 체력바 숨김
    public void HideBossHealthBar()
    {
        currentBoss = null;
        bossHealthPanel.SetActive(false);
    }

    // 체력바 업데이트
    private void UpdateHealthBar()
    {
        if (currentBoss == null) return;

        float currentHp = Mathf.Max(0, currentBoss.CurrentHp);
        float value = currentHp / maxHp;
        
        healthBarSlider.value = value;
        
        // 보스가 죽으면 체력바 숨김
        if (currentBoss.IsDead || !currentBoss.gameObject.activeSelf)
        {
            HideBossHealthBar();
        }
    }
}
