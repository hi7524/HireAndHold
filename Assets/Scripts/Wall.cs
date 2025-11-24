using UnityEngine;
using UnityEngine.UI;

public class Wall : MonoBehaviour, IDamagable
{
    [SerializeField] private float maxHp;
    [SerializeField] private float currentHp;
    [Space]
    [SerializeField] private Slider hpSlider;
    [Header("Managers")]
    [SerializeField] StageUiManager uiManager;
    [SerializeField] GameManager gameManager;

    public float CurrentHp => currentHp;
    public float MaxHp => maxHp;
    private bool isDead = false; 


    private void Start()
    {
        currentHp = maxHp;
        hpSlider.value = 1f;
        isDead = false; 
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return; 

        currentHp -= damage;
        hpSlider.value = currentHp / maxHp;

        if(currentHp <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        
        gameObject.SetActive(false);
        uiManager.ActiveGameOverPanel();
        gameManager.GameEnd();
    }
}
