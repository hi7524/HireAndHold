using UnityEngine;

public class FloatingTextSpawner : MonoBehaviour
{
    public FloatingText floatingTextPrefab;
    public RectTransform floatingTextRoot;

    public FloatingText SpawnText(Vector3 worldPosition, string value)
    {
        return SpawnText(worldPosition, value, false);
    }

    public FloatingText SpawnText(Vector3 worldPosition, string value, bool isCritical)
    {
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition);

        FloatingText newText = Instantiate(floatingTextPrefab, floatingTextRoot);

        newText.transform.position = screenPos;
        newText.SetText(value, isCritical);

        return newText;
    }
}
