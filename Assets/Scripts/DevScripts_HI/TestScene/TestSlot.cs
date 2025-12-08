using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TestSlot : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private TextMeshProUGUI imgText;
    [SerializeField] private TextMeshProUGUI idText;
    [SerializeField]private TextMeshProUGUI nameText;
    private int ID;

    public void SetID(int id)
    {
        ID = id;
        idText.text = $"ID: {ID}";
    }

    public void SetSprite(Sprite sprite)
    {
        imgText.gameObject.SetActive(false);
        image.sprite = sprite;
    }

    public void SetNameText(string name)
    {
        nameText.text = name;
    }
}