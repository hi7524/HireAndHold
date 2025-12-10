using TMPro;
using UnityEngine;

public class NotificationUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform rect;
    [SerializeField] private TextMeshProUGUI notificationText;

    public float fadeInDuration = 0.5f;
    public float displayDuration = 1.5f;
    public float fadeOutDuration = 0.5f;
    public float floatDistance = 50f;

    public float fadeOutExtraDistance = 20f;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private Vector3 fadeOutStartPosition;
    private Vector3 fadeOutTargetPosition;
}
