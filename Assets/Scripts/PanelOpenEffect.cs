using UnityEngine;
using DG.Tweening;

public class PanelOpenEffect : MonoBehaviour
{
    private void OnEnable()
    {
        transform.localScale = Vector3.one * 0.8f;
        transform.DOScale(1.0f, 0.15f);
    }
}