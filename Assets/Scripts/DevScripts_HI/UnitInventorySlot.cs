using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitInventorySlot : MonoBehaviour
{
    [SerializeField] private Image unitImg;
    [SerializeField] private TextMeshProUGUI nameText;

    private int unitId;


    public void SetUnit(int unitId)
    {
        this.unitId = unitId;
    }

    public void UpdateUi()
    {
        // 데이터 테이블과 연결 및 수정 필요 **
        // unitImg.sprite = unitSprite;
        nameText.text = unitId.ToString();
    }
}