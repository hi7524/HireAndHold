using TMPro;
using UnityEngine;

public class FloatingText : MonoBehaviour
{
    public TextMeshProUGUI text;
    public float floatSpeed = 40f;  
    public float lifetime = 1f;    
    public float stayDuration = 0.15f; 
    private Color originalColor;
    private RectTransform rect;
    private float timer = 0f;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        originalColor = text.color;
    }

    public void SetText(string value)
    {
        text.text = value;
        text.color = originalColor;
        timer = 0f;
    }

    private void OnEnable()
    {
        timer = 0f;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer > stayDuration)
        {
            rect.anchoredPosition += Vector2.up * floatSpeed * Time.deltaTime;
        }

     
        float fadeStartTime = lifetime * 0.5f;
        if (timer > fadeStartTime)
        {
            float fadeT = (timer - fadeStartTime) / (lifetime - fadeStartTime);
            Color c = text.color;
            c.a = Mathf.Lerp(originalColor.a, 0, fadeT);
            text.color = c;
        }

        if (timer >= lifetime)
        {
            gameObject.SetActive(false);
        }
    }
}
