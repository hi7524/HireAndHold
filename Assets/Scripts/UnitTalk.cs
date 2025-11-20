using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class UnitTalk : MonoBehaviour, IPointerClickHandler
{
    public GameObject bubbleObj;
    public TextMeshProUGUI bubbleText;

    public string[] lines;
    public float hideDelay = 2f;

    private int index = 0;

    private float hideTimer = -1f;

    public void OnPointerClick(PointerEventData eventData)
    {
        bubbleObj.SetActive(true);

        bubbleText.gameObject.SetActive(true);
        bubbleText.text = lines[index];

        index = (index + 1) % lines.Length;

        hideTimer = hideDelay;
    }

    private void Update()
    {
        if (hideTimer >= 0f)
        {
            hideTimer -= Time.deltaTime;

            if (hideTimer <= 0f)
            {
                bubbleObj.SetActive(false);

                bubbleText.text = "";
                bubbleText.gameObject.SetActive(false);

                hideTimer = -1f;
            }
        }
    }
}
