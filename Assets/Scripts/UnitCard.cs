using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Tutorial;

public class UnitCard : MonoBehaviour
{
    public Image icon;
    public TMP_Text nameText;

    private DeckUnitModel data;
    private Action<DeckUnitModel> onClick;

    private bool isAssigned = false;

    public DeckUnitModel Data => data;
    public int UnitId => data?.unitId ?? 0;

    public void Init(DeckUnitModel unit)
    {
        data = unit;
        icon.sprite = unit.icon;
        nameText.text = unit.unitName;

        // 튜토리얼 타겟 레지스트리에 유닛 ID로 등록
        TutorialTargetRegistry.Register($"UnitCard_{unit.unitId}", gameObject);
    }

    public void Setup(Action<DeckUnitModel> clickAction)
    {
        onClick = clickAction;
    }

    public void OnClick()
    {
        // 튜토리얼에 유닛 카드 클릭 알림 (유닛 ID 기반)
        if (data != null)
        {
            TutorialManager.Instance?.NotifyButtonTouched($"UnitCard_{data.unitId}");
        }

        onClick?.Invoke(data);
    }

    private void OnDestroy()
    {
        // 튜토리얼 타겟 레지스트리에서 해제
        if (data != null)
        {
            TutorialTargetRegistry.Unregister($"UnitCard_{data.unitId}");
        }
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    public void SetAssigned(bool assigned)
    {
        isAssigned = assigned;

        if (assigned)
        {
            icon.color = new Color(0.2f, 0.2f, 0.2f);
        }
        else
        {
            icon.color = Color.white;
        }
    }
}
