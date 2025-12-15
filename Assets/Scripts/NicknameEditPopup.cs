using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NicknameEditPopup : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private ProfileUI profileUI;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button cancelButton;

    private void Awake()
    {
        saveButton.onClick.AddListener(OnClickSave);
        cancelButton.onClick.AddListener(OnClickCancel);
    }

    private void OnEnable()
    {
        inputField.text = PlayData.Nickname;
    }

    private async void OnClickSave()
    {
        string newName = inputField.text.Trim();

        if (string.IsNullOrEmpty(newName))
        {
            Debug.LogWarning("닉네임이 비어있음");
            return;
        }

        await SaveNicknameAsync(newName);
        gameObject.SetActive(false);
    }

    private void OnClickCancel()
    {
        gameObject.SetActive(false);
    }

    private async UniTask SaveNicknameAsync(string nickName)
    {
        var db = DatabaseManager.Instance;
        db.CurrentUser.profile.nickname = nickName;

        await db.SaveProfileAsync();

        PlayData.SetNicknameImmediate(nickName);
        profileUI.Refresh();
    }
}
