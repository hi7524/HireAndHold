using UnityEngine;
using TMPro;

public class MonsterCountUI : MonoBehaviour
{
    [SerializeField] private StageManager stageManager;
    [SerializeField] private TextMeshProUGUI monsterCountText;
    
    private void OnEnable()
    {
       // StageManager 참조가 끊어졌으면 다시 찾기
        if (stageManager == null)
        {
            GameObject managerObj = GameObject.FindGameObjectWithTag("StageManager");
            
            if (managerObj != null)
            {
                stageManager = managerObj.GetComponent<StageManager>();
                Debug.Log("[MonsterCountUI] StageManager 재탐색 성공 (Tag)");
            }
            else
            {
                Debug.LogError("[MonsterCountUI] StageManager 태그를 찾을 수 없습니다!");
                return;
            }
        }

        stageManager.OnMonsterCountChanged += UpdateMonsterCount;
        Debug.Log("[MonsterCountUI] 이벤트 구독 성공");
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
            Debug.Log($"[MonsterCountUI] UI 업데이트: {remaining}");
        }
        else
        {
            Debug.LogWarning("[MonsterCountUI] monsterCountText가 null입니다!");
        }
    }
}
