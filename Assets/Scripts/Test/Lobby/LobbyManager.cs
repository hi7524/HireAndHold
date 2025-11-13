using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    private WindowManager windowManager;

    [System.Obsolete]
    private void Start()
    {
        windowManager = FindObjectOfType<WindowManager>();
    }

    public void OnClickedStoreButton()
    {
        windowManager.Open(Windows.Store);
    }
    public void OnClickedMainButton()
    {
        windowManager.Open(Windows.Main);
    }

    public void OnClickedDungeonButton()
    {
        windowManager.Open(Windows.Dungeon);
    }
    public void OnClickedUnitButton()
    {
        windowManager.Open(Windows.Unit);
    }
    public void OnClickedStageButton()
    {
        windowManager.Open(Windows.Stage);
    }
    
}
