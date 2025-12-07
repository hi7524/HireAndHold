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

    public bool IsInitialized => skill != null;

    public void Initialize(PlayerSkillBase skill, Vector3 spawnPosition)
    {
        skill.Init();
        this.skill = skill;
        this.spawnPosition = spawnPosition;

        button.onClick.AddListener(OnClick);

        skill.OnCooldownProgress += UpdateCooldown;
        skill.OnCooldownEnd += ResetCooldown;

        // 쿨타임 마스크 설정 (12시부터 시계방향으로 줄어듦)
        SetupCooldownMask();

        // 스킬 아이콘 로드
        LoadSkillIconAsync().Forget();
    }

    private void SetupCooldownMask()
    {
        if (cooldownMask == null) return;

        cooldownMask.fillAmount = 0f;  // 초기에는 쿨타임 없음 (스킬 사용 가능)
    }

    private async UniTaskVoid LoadSkillIconAsync()
    {
        if (skill == null)
        {
            Debug.LogError("[SkillUI] skill이 null입니다!");
            return;
        }

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

        iconAddress = iconAddress.Trim();

        // 기존 핸들 해제
        if (iconHandle.IsValid())
        {
            Addressables.Release(iconHandle);
        }

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
        if (cooldownMask == null) return;

        cooldownMask.fillAmount = 1f - progress;
    }

    private void ResetCooldown()
    {
        if (cooldownMask == null) return;

        // 쿨타임 완료: 마스크 없음 (스킬 사용 가능)
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
