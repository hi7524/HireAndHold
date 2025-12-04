using UnityEngine;

public class WorldUnitMovement : MonoBehaviour
{
    public RectTransform moveArea;
    public Camera cam;
    public float speed = 0.5f;

    private Vector3 target;

    private void Start()
    {
        PickNewTarget();
    }

    private void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) < 0.1f)
        {
            PickNewTarget();
        }
    }

    private void PickNewTarget()
    {
        target = GetRandomWorldPoint(moveArea, cam);
        target.z = 0f;
    }

    private Vector3 GetRandomWorldPoint(RectTransform area, Camera cam)
    {
        Vector3[] corners = new Vector3[4];
        area.GetWorldCorners(corners);

        float x = Random.Range(corners[0].x, corners[2].x);
        float y = Random.Range(corners[0].y, corners[2].y);

        Vector3 screen = new Vector3(x, y, 10f);
        return cam.ScreenToWorldPoint(screen);
    }
}
