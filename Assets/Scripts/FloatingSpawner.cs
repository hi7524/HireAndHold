using UnityEngine;

public class FloatingTextSpawner : MonoBehaviour
{
    public FloatingText floatingTextPrefab;
    public Canvas canvas;  

    public FloatingText SpawnText(Vector3 worldPosition, string value)
    {
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition);

        FloatingText newText = Instantiate(floatingTextPrefab, canvas.transform);

        newText.transform.position = screenPos;
        newText.SetText(value);

        return newText;
    }
}
