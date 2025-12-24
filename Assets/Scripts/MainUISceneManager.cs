using Cysharp.Threading.Tasks;
using GameData;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering;

public class MainUISceneManager : MonoBehaviour
{
    public Transform worldUnitRoot;

    private async void Start()
    {
        // DatabaseManager가 초기화되지 않았으면 리턴
        if (DatabaseManager.Instance == null || !DatabaseManager.Instance.IsInitialized)
        {
            Debug.LogWarning("[MainUISceneManager] DatabaseManager가 초기화되지 않았습니다.");
            return;
        }

        var owned = DatabaseManager.Instance.GetAllCharacters();
        if (owned.Count < 2)
        {
            return;
            }

        List<int> selectedUnits = GetTwoUniqueUnitIds(owned);

        for (int i = 0; i < selectedUnits.Count; i++)
        {
            int unitId = selectedUnits[i];
            Vector3 spawnPos = GetSpawnPosition(i);

            await SpawnUnit(unitId, spawnPos);
        }
    }

    private List<int> GetTwoUniqueUnitIds(List<OwnedCharacter> owned)
    {
        List<int> result = new List<int>();

        int r1 = Random.Range(0, owned.Count);
        result.Add(int.Parse(owned[r1].id));

        int r2;
        do
        {
            r2 = Random.Range(0, owned.Count);
        }
        while (r2 == r1);

        result.Add(int.Parse(owned[r2].id));

        return result;
    }

    private Vector3 GetSpawnPosition(int index)
    {
        if (index == 0)
        {
            return new Vector3(-1f, 1.5f, -1f);
        }

        return new Vector3(1f, -1f, -1f);
    }

    private async UniTask SpawnUnit(int unitId, Vector3 position)
    {
        GameObject wrapper = new GameObject($"Unit_{unitId}");
        wrapper.transform.SetParent(worldUnitRoot);
        wrapper.transform.localPosition = position;

        UnitData data = DataTableManager.UnitTable.Get(unitId);
        GameObject combatPrefab =
            await Addressables.LoadAssetAsync<GameObject>(data.PREFAB_NAME);

        GameObject visual = Instantiate(combatPrefab, wrapper.transform);
        visual.transform.localScale = Vector3.one * 0.4f;

        var wander = wrapper.AddComponent<LobbyUnitWander>();
        wander.min = new Vector2(-0.8f, -0.4f);
        wander.max = new Vector2(0.8f, 0.4f);
        wander.moveSpeed = Random.Range(0.3f, 0.6f);

        foreach (var r in visual.GetComponentsInChildren<SpriteRenderer>())
            r.sortingLayerName = "LobbyWorld";

        var group = visual.GetComponentInChildren<SortingGroup>();
        if (group != null)
            group.sortingLayerName = "LobbyWorld";

        await UniTask.DelayFrame(1);

        var character = visual.GetComponentInChildren<
            Assets.HeroEditor.Common.Scripts.CharacterScripts.Character>();

        if (character?.LayerManager != null)
        {
            character.LayerManager.GetSpritesBySortingOrder();
            character.LayerManager.SetSpritesBySortingOrder();
        }
    }


}
