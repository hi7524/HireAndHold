using UnityEngine;
using UnityEngine.EventSystems;

public class GenericWindow : MonoBehaviour
{
    public GameObject firstSelected;
    protected WindowManager manager;
    public void Init(WindowManager mgr)
    {
        manager = mgr;
    }
    public void OnFocus()
    {
        EventSystem.current.SetSelectedGameObject(firstSelected);
    }
    public virtual void Open()
    {
        if (gameObject.activeSelf)
        {
            return;
        }

        gameObject.SetActive(true);
        GetComponent<PanelOpenEffect>()?.PlayEffect();
        OnFocus();
    }
    public virtual void Close()
    {
        gameObject.SetActive(false);
    }
}
