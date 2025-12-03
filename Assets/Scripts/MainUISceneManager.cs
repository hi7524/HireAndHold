using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering;
using UnityEngine.ResourceManagement.AsyncOperations;

public class MainUISceneManager : MonoBehaviour
{
    public GameObject uiUnitWrapperPrefab;   
    public RectTransform moveArea;
    public int spawnCount = 4;

    private int uiUnitLayer;

    private void Awake()
    {
        uiUnitLayer = LayerMask.NameToLayer("UIUnitLayer");
    }

    private async void Start()
    {
        var owned = DatabaseManager.Instance.GetAllCharacters();
        if (owned.Count == 0) return;

        for (int i = 0; i < spawnCount; i++)
        {
            int r = Random.Range(0, owned.Count);
            int unitId = int.Parse(owned[r].id);

            await SpawnUIUnit(unitId);
        }
    }

    private async UniTask SpawnUIUnit(int unitId)
    {

        GameObject wrapper = Instantiate(uiUnitWrapperPrefab, moveArea);
        WindowUnitMovement mover = wrapper.GetComponent<WindowUnitMovement>();
        mover.moveArea = moveArea;

        Transform visualRoot = wrapper.transform.Find("VisualRoot");

        UnitData data = DataTableManager.UnitTable.Get(unitId);
        var handle = Addressables.LoadAssetAsync<GameObject>(data.PREFAB_NAME);
        GameObject combatPrefab = await handle.ToUniTask();


        GameObject visual = Instantiate(combatPrefab, visualRoot);

        visual.transform.localScale = Vector3.one * 3f;
        visual.transform.localPosition = Vector3.zero;

        SetLayerRecursively(visual, uiUnitLayer);

    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
