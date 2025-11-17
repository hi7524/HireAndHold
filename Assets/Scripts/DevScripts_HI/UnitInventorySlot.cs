using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitInventorySlot : MonoBehaviour, ITestDraggable
{
    [SerializeField] private Image unitImg;
    [SerializeField] private TextMeshProUGUI nameText;

    public bool IsDraggable => true; // 수정 필요: 편집 시스템과 연동짓기 **
    public GameObject GameObject => gameObject;

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

    public void OnDrag()
    {
    }

    public void OnDragEnd()
    {
    }

    public void OnDragStart()
    {
    }

    public void OnDropFailed()
    {
    }

}