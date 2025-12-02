using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class StageDeck : MonoBehaviour
{
    public Image[] slotImages;   
    public Sprite emptySprite;      
    private DataTable_Unit unitTable;

    async void Start()
    {
        unitTable = new DataTable_Unit();
        await unitTable.LoadAsync("UnitTable");

        Init();
    }

    public void Init()
    {
        for (int i = 0; i < slotImages.Length; i++)
        {
            int index = i;

            string address = PlayData.selectedDeckUnitIconAddresses[index];


            if (string.IsNullOrEmpty(address))
            {
                slotImages[index].sprite = emptySprite;
                continue;
            }

            Addressables.LoadAssetAsync<Sprite>(address).Completed += (handle) =>
            {

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    slotImages[index].sprite = handle.Result;
                }
                else
                {
                    slotImages[index].sprite = emptySprite;
                }
            };
        }
    }


}
