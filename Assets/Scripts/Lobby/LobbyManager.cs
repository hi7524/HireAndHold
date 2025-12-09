using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private WindowManager windowManager;

    private void Start()
    {
        Time.timeScale = 1f;
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

    public void OnClickedEnforceButton()
    {
        windowManager.Open(Windows.Enforce);
    }
    public async void OnClickedLogOutButton()
    {
        await AuthManager.Instance.SignOutAsync();
        SceneManager.LoadScene("01_Title");
    }


}
