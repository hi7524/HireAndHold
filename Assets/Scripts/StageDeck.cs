using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

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

    void OnEnable()
    {
        if (isInitialized)
        {
            Init();
        }
    }

    public async void Init()
    {
        if (!isInitialized)
        {
            return;
        }
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
            slotImages[i].sprite = await Addressables.LoadAssetAsync<Sprite>(data.UNIT_ICON);
        }
    }

    public void Refresh()
    {
        if (!isInitialized)
        {
            return;
        }
        Init();
    }
}
