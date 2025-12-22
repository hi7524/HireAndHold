using Cysharp.Threading.Tasks;
using GameData;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class UnitInfoDisplay : MonoBehaviour
{
    [Header("Unit Basic Info")]
    [SerializeField] private Image unitImage;
    [SerializeField] private TextMeshProUGUI unitNameText;
    [SerializeField] private TextMeshProUGUI classText;
    [SerializeField] private TextMeshProUGUI powerText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI heroStarText;
    [SerializeField] private TextMeshProUGUI heroProgressText;

    [Header("Owned Resource UI")]
    [SerializeField] private TextMeshProUGUI playerStoneText;
    [SerializeField] private TextMeshProUGUI playerPieceText;

    [Header("Hero Effect List")]
    [SerializeField] private Transform heroEffectListParent;
    [SerializeField] private GameObject heroEffectItemPrefab;
    [SerializeField] private Color heroEffectUnlockedColor = Color.white;
    [SerializeField] private Color heroEffectLockedColor = Color.gray;

    private DataTable_Unit unitTable;
    private DataTable_HeroEnforce heroTable;
    private DataTable_HeroEnforceEffect effectTable;

    private const int NORMAL_MAX = 20;
    private const int HERO_MAX = 4;

    private bool isTablesLoaded = false;

    private void Start()
    {
        InitializeTables().Forget();
        RefreshResources();
    }

    private async UniTaskVoid InitializeTables()
    {
        unitTable = new DataTable_Unit();
        heroTable = new DataTable_HeroEnforce();
        effectTable = new DataTable_HeroEnforceEffect();

        await unitTable.LoadAsync("UnitTable");
        await heroTable.LoadAsync("HeroEnforceTable");
        await effectTable.LoadAsync("HeroEnforceEffectTable");

        isTablesLoaded = true;
    }

    public async UniTask UpdateDisplay(int unitId, UnitData data, OwnedCharacter character, Unit previewUnit)
    {
        // 테이블 로드 대기
        if (!isTablesLoaded)
        {
            await UniTask.WaitUntil(() => isTablesLoaded);
        }

        bool owned = character != null;

        Debug.Log($"[UnitInfoDisplay] UpdateDisplay - unitId: {unitId}, owned: {owned}");
        if (owned)
        {
            Debug.Log($"[UnitInfoDisplay] Character info - enforceLevel: {character.enforceLevel}, heroEnforceLevel: {character.heroEnforceLevel}");
        }

        unitNameText.text = data.StringName;
        classText.text = $"등급: {data.RANK}";

        float attack = previewUnit.GetAttackDamageStat().Value;
        powerText.text = attack.ToString();

        levelText.text = owned
           ? $"{character.enforceLevel}/{NORMAL_MAX}"
           : $"-/{NORMAL_MAX}";

        await LoadSprite(data.UNIT_ICON);

        RefreshResources();

        int heroLv = owned ? character.heroEnforceLevel : 0;
        heroStarText.text = $"영웅강화 등급: ★{heroLv}";
        heroProgressText.text = $"{heroLv}/{HERO_MAX}";

        RefreshHeroEffectList(unitId, heroLv);
    }

    private async UniTask LoadSprite(string key)
    {
        try
        {
            var sprite = await Addressables.LoadAssetAsync<Sprite>(key).Task;
            unitImage.sprite = sprite;
        }
        catch { }
    }

    private void RefreshHeroEffectList(int unitId, int heroLv)
    {
        if (!isTablesLoaded || heroTable == null || effectTable == null)
        {
            Debug.LogWarning("[UnitInfoDisplay] 테이블이 아직 로드되지 않았습니다.");
            return;
        }

        foreach (Transform t in heroEffectListParent)
        {
            Destroy(t.gameObject);
        }

        for (int lv = 1; lv <= HERO_MAX; lv++)
        {
            var enforce = heroTable.Get(unitId, lv);
            if (enforce == null) continue;

            var eff = effectTable.Get(enforce.Hero_Enforce_EffectID);
            if (eff == null) continue;

            string desc = effectTable.FormatEffect(eff);

            var go = Instantiate(heroEffectItemPrefab, heroEffectListParent);
            var txt = go.GetComponentInChildren<TextMeshProUGUI>();

            if (txt == null)
            {
                Debug.LogError("heroEffectItemPrefab 안에 TMP Text 없음!");
                continue;
            }

            txt.text = $"LV {lv}: {desc}";
            txt.color = lv <= heroLv ? heroEffectUnlockedColor : heroEffectLockedColor;
        }
    }

    private void RefreshResources()
    {
        playerStoneText.text = PlayData.EnhanceStone.ToString();
    }

    private void OnEnable()
    {
        PlayData.OnCurrencyChanged += RefreshResources;
    }

    private void OnDisable()
    {
        PlayData.OnCurrencyChanged -= RefreshResources;
    }
}
