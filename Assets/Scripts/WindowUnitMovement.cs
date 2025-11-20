using UnityEngine;

public class WindowUnitMovement : MonoBehaviour
{
    public RectTransform moveArea;
    public float speed = 150f;

    private Vector3 target;

    void Start()
    {
        transform.localPosition = GetRandomLocalPosition();
        PickNewTarget();
    }

    void Update()
    {
        transform.localPosition = Vector3.MoveTowards(transform.localPosition, target, speed * Time.deltaTime);

        if (Vector3.Distance(transform.localPosition, target) < 5f)
        {
            PickNewTarget();
        }
    }

    Vector3 GetRandomLocalPosition()
    {
        Vector2 size = moveArea.rect.size;

        float x = Random.Range(-size.x / 2f, size.x / 2f);
        float y = Random.Range(-size.y / 2f, size.y / 2f);

        return moveArea.localPosition + new Vector3(x, y, 0);
    }

    void PickNewTarget()
    {
        target = GetRandomLocalPosition();
    }
}
