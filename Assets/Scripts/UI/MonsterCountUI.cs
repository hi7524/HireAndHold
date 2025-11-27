using UnityEngine;
using TMPro;

public class MonsterCountUI : MonoBehaviour
{
    [SerializeField] private StageManager stageManager;
    [SerializeField] private TextMeshProUGUI monsterCountText;
    
    private void OnEnable()
    {
        if (stageManager == null)
        {
            GameObject managerObj = GameObject.FindGameObjectWithTag("StageManager");
            
            if (managerObj != null)
            {
                stageManager = managerObj.GetComponent<StageManager>();
            }
            else
            {
               
                return;
            }
        }

        stageManager.OnMonsterCountChanged += UpdateMonsterCount;

    }
    
    private void OnDisable()
    {
        if (stageManager != null)
        {
            stageManager.OnMonsterCountChanged -= UpdateMonsterCount;
        }
    }
    
    private void UpdateMonsterCount(int remaining)
    {
        if (monsterCountText != null)
        {
            monsterCountText.text = $"{remaining}";
            
        }
       
    }
}
