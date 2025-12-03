using UnityEngine;

public class WindowUnitMovement : MonoBehaviour
{
    public RectTransform moveArea;
    public float speed = 150f;

    private RectTransform rectTransform;
    private Vector2 target;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        rectTransform.anchoredPosition = GetRandomPosition();
        PickNewTarget();
    }

    private void Update()
    {
        rectTransform.anchoredPosition = Vector2.MoveTowards(
            rectTransform.anchoredPosition,
            target,
            speed * Time.deltaTime
        );

        if (Vector2.Distance(rectTransform.anchoredPosition, target) < 5f)
            PickNewTarget();
    }

    private Vector2 GetRandomPosition()
    {
        Vector2 size = moveArea.rect.size;
        float x = Random.Range(-size.x * 0.5f, size.x * 0.5f);
        float y = Random.Range(-size.y * 0.5f, size.y * 0.5f);
        return new Vector2(x, y);
    }

    private void PickNewTarget()
    {
        target = GetRandomPosition();
    }
}
