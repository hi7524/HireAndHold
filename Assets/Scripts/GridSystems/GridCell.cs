using UnityEngine;

public enum CellState
{
    Normal,          // 기본 상태 (비어있음)
    Occupied,        // 아이템이 배치됨
    ValidPlace,      // 드래그 중 - 배치 가능
    InvalidPlace,    // 드래그 중 - 배치 불가
    Hover            // 마우스 호버
}

public class GridCell : MonoBehaviour
{
    public Vector2Int gridPosition;
    private bool isOccupied;
    private SpriteRenderer spriteRenderer;
}
