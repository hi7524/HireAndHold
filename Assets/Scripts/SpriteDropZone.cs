using UnityEngine;
using UnityEngine.UI;

public class SpriteDropZone : MonoBehaviour, IDroppable
{
    public Unit testUnitPrf;

    [SerializeField] private bool acceptAllDraggables = true;

    private SpriteRenderer sr;
    private Color normalColor = Color.white;
    private Color validDropColor = Color.green;
    private Color invalidDropColor = Color.red;

    public void Awake()
    {
        sr = GetComponent<SpriteRenderer>();  
    }

    public bool CanDrop(IDraggable draggable)
    {
        return acceptAllDraggables;
    }

    public void OnDrop(IDraggable draggable)
    {
        sr.color = normalColor;
        draggable.GameObject.SetActive(false);

        // CreateUnitCardUI인 경우 유닛 ID를 받아서 생성
        if (draggable is CreateUnitCardUI cardUI)
        {
            CreateUnit();
        }
    }

    private void CreateUnit()
    {
        // 테스트용 유닛 생성
        var testUnitObj = Instantiate(testUnitPrf);
        testUnitObj.transform.position = transform.position;
    }

    public void OnDragEnter(IDraggable draggable)
    {
        if (CanDrop(draggable))
            sr.color = validDropColor;
        else
            sr.color = invalidDropColor;
    }

    public void OnDragExit(IDraggable draggable)
    {
        sr.color = normalColor;
    }
}