using Cysharp.Threading.Tasks;
using GameData;
using UnityEngine;

public class UnitUIWarmup : MonoBehaviour
{
    public static UnitUIWarmup Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    public async UniTask<Unit> PreparePreviewUnitAsync(int unitId)
    {
        var unitTable = DataTableManager.UnitTable;
        if (unitTable == null) return null;

        var data = unitTable.Get(unitId);
        if (data == null) return null;

        if (!string.IsNullOrEmpty(data.UNIT_ICON))
        {
            await SpriteCache.Instance.LoadSpriteAsync(data.UNIT_ICON);
        }

        var go = new GameObject($"PreviewUnit_{unitId}");
        var unit = go.AddComponent<Unit>();
        unit.IsPreview = true;
        unit.SetUnitID(unitId);

        await UniTask.WaitUntil(() => unit != null && unit.IsInitialized);
        return unit;

        var skillTable = DataTableManager.SkillTable;
        if (skillTable != null)
        {
            if (data.UNIT_SKILL1 > 0)
            {
                var s1 = skillTable.Get(data.UNIT_SKILL1);
                if (s1 != null && !string.IsNullOrEmpty(s1.SKILL_ICON))
                    await SpriteCache.Instance.LoadSpriteAsync(s1.SKILL_ICON);
            }

            if (data.UNIT_SKILL2 > 0)
            {
                var s2 = skillTable.Get(data.UNIT_SKILL2);
                if (s2 != null && !string.IsNullOrEmpty(s2.SKILL_ICON))
                    await SpriteCache.Instance.LoadSpriteAsync(s2.SKILL_ICON);
            }
        }

    }
}
