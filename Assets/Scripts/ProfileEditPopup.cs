using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using GameData;

public class ProfileEditPopup : MonoBehaviour
{
    [Header("Preview")]
    [SerializeField] private Image currentIconImage;

    [Header("Grid")]
    [SerializeField] private Transform iconGridRoot;
    [SerializeField] private ProfileIconItem itemPrefab;

    [Header("Buttons")]
    [SerializeField] private Button saveButton;
    [SerializeField] private Button cancelButton;

    private string selectedIconAddress;

    private void Awake()
    {
        saveButton.onClick.AddListener(OnClickSave);
        cancelButton.onClick.AddListener(() => gameObject.SetActive(false));
    }

    private void OnEnable()
    {
        selectedIconAddress = PlayData.ProfileIconAddress;
        RefreshPreview();
        BuildIconGrid();
    }

    private void RefreshPreview()
    {
        if (string.IsNullOrEmpty(selectedIconAddress))
            return;

        var sprite = AddressablePreloader.Instance
            .GetCachedSprite(selectedIconAddress);

        if (sprite != null)
            currentIconImage.sprite = sprite;
    }

    private void BuildIconGrid()
    {
        foreach (Transform child in iconGridRoot)
            Destroy(child.gameObject);

        var ownedCharacters = DatabaseManager.Instance.GetAllCharacters();

        foreach (var character in ownedCharacters)
        {
            int unitId = int.Parse(character.id);
            var unitData = DataTableManager.UnitTable.Get(unitId);

            if (unitData == null || string.IsNullOrEmpty(unitData.UNIT_ICON))
                continue;

            var sprite = AddressablePreloader.Instance
                .GetCachedSprite(unitData.UNIT_ICON);

            if (sprite == null)
                continue;

            var item = Instantiate(itemPrefab, iconGridRoot);
            item.Init(sprite, unitData.UNIT_ICON, OnIconSelected);
        }
    }

    private void OnIconSelected(string iconAddress)
    {
        selectedIconAddress = iconAddress;
        RefreshPreview();
    }

    private async void OnClickSave()
    {
        var db = DatabaseManager.Instance;

        db.CurrentUser.profile.profileIconAddress = selectedIconAddress;
        await db.SaveProfileAsync();

        PlayData.SetProfileIconImmediate(selectedIconAddress);
        gameObject.SetActive(false);
    }
}
