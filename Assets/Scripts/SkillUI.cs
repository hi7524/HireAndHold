using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class SkillUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Image cooldownMask;
    [SerializeField] private Button button;

    private PlayerSkillBase skill;
    private Vector3 spawnPosition;
    private AsyncOperationHandle<Sprite> iconHandle;

    public async void Initialize(PlayerSkillBase skill, Vector3 spawnPosition)
    {
        skill.Init();
        this.skill = skill;
        this.spawnPosition = spawnPosition;

        button.onClick.AddListener(OnClick);

        skill.OnCooldownProgress += UpdateCooldown;
        skill.OnCooldownEnd += ResetCooldown;

        // 스킬 아이콘 로드
        await LoadSkillIcon();
    }

    private async UniTask LoadSkillIcon()
    {
        if (skill == null)
        {
            Debug.LogError("[SkillUI] skill이 null입니다!");
            return;
        }

        // SkillTable에서 스킬 데이터 가져오기
        await DataTableManager.InitAsync();
        
        SkillData skillData = DataTableManager.SkillTable?.Get(skill.SkillID);
        if (skillData == null)
        {
            return;
        }

        string iconAddress = skillData.SKILL_ICON;
        if (string.IsNullOrEmpty(iconAddress))
        {
            return;
        }

        // 공백 제거
        iconAddress = iconAddress.Trim();

        // Addressables로 Sprite 로드
        iconHandle = Addressables.LoadAssetAsync<Sprite>(iconAddress);
        var sprite = await iconHandle.ToUniTask();

        if (iconHandle.Status == AsyncOperationStatus.Succeeded && icon != null)
        {
            icon.sprite = sprite;
        }

    }

    private void OnClick()
    {
        skill.TryUse(spawnPosition);
    }

    private void UpdateCooldown(float progress)
    {
        cooldownMask.fillAmount = 1f - progress;
    }

    private void ResetCooldown()
    {
        cooldownMask.fillAmount = 0f;
    }

    private void OnDestroy()
    {
        if (skill != null)
        {
            skill.OnCooldownProgress -= UpdateCooldown;
            skill.OnCooldownEnd -= ResetCooldown;
        }

        // Addressables 리소스 해제
        if (iconHandle.IsValid())
        {
            Addressables.Release(iconHandle);
        }
    }
}