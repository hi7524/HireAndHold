using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class StageDeck : MonoBehaviour
{
    public Image[] slotImages;
    public Sprite emptySprite;

    private DataTable_Unit unitTable;
    private bool isInitialized = false;

    async void Start()
    {
        await DatabaseManager.Instance.WaitForInitializationAsync();

        unitTable = new DataTable_Unit();
        await unitTable.LoadAsync("UnitTable");

        isInitialized = true;
        Init();
    }


    public async void Init()
    {
        if (!isInitialized) return;

        int preset = PlayData.currentSelectedPreset;
        Debug.Log("[StageDeck] Using Preset = " + preset);

        for (int i = 0; i < slotImages.Length; i++)
        {
            int unitId = PlayData.selectedDeckUnitIds[preset, i];
            Debug.Log("[StageDeck] slot " + i + " = " + unitId);

            if (unitId == 0)
            {
                slotImages[i].sprite = emptySprite;
                continue;
            }

            UnitData data = unitTable.Get(unitId);
            if (data == null || string.IsNullOrEmpty(data.UNIT_ICON))
            {
                slotImages[i].sprite = emptySprite;
                continue;
            }

            string address = data.UNIT_ICON;

            // 🟩 Addressables 로딩을 await로 기다림
            var handle = Addressables.LoadAssetAsync<Sprite>(address);
            Sprite icon = await handle.Task;

            slotImages[i].sprite = icon != null ? icon : emptySprite;
        }
    }


    public void Refresh()
    {
        if (!isInitialized) return;
        Init();
    }
}
