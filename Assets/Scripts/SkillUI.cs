using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using TMPro;

public class SkillUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Image cooldownMask;
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI cooldownText;

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

        // 쿨타임 텍스트 초기화
        if (cooldownText != null)
        {
            cooldownText.text = "";
        }
    }

    private async UniTaskVoid LoadSkillIconAsync()
    {
        if (skill == null)
        {
            Debug.LogError("[SkillUI] skill이 null입니다!");
            return;
        }

        // DataTableManager 초기화 대기
        while (!DataTableManager.IsInitialized)
        {
            await UniTask.Yield();
        }

        SkillData skillData = DataTableManager.SkillTable?.Get(skill.SkillID);
        if (skillData == null)
        {
            Debug.LogWarning($"[SkillUI] SkillData를 찾을 수 없습니다. SkillID: {skill.SkillID}");
            return;
        }

        string iconAddress = skillData.SKILL_ICON;
        if (string.IsNullOrEmpty(iconAddress))
        {
            Debug.LogWarning($"[SkillUI] SKILL_ICON이 비어있습니다. SkillID: {skill.SkillID}");
            return;
        }

        iconAddress = iconAddress.Trim();

        // 캐시에서 먼저 확인
        var cachedSprite = AddressablePreloader.Instance?.GetCachedSprite(iconAddress);
        if (cachedSprite != null)
        {
            if (icon != null)
            {
                icon.sprite = cachedSprite;
            }
            return;
        }

        // 기존 핸들 해제
        if (iconHandle.IsValid())
        {
            Addressables.Release(iconHandle);
        }

        try
        {
            // Addressables로 Sprite 로드
            iconHandle = Addressables.LoadAssetAsync<Sprite>(iconAddress);
            var sprite = await iconHandle.ToUniTask();

            if (iconHandle.Status == AsyncOperationStatus.Succeeded && icon != null)
            {
                icon.sprite = sprite;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SkillUI] 아이콘 로드 실패: {iconAddress}, {e.Message}");
        }
    }

    private void OnClick()
    {
        skill.TryUse(spawnPosition);
    }

    private void UpdateCooldown(float progress)
    {
        if (cooldownMask != null)
        {
            cooldownMask.fillAmount = 1f - progress;
        }

        // 쿨타임 텍스트 업데이트 (남은 시간 표시)
        if (cooldownText != null && skill != null)
        {
            float remainingTime = skill.CoolDown * (1f - progress);
            if (remainingTime > 0f)
            {
                cooldownText.text = Mathf.CeilToInt(remainingTime).ToString();
            }
        }
    }

    private void ResetCooldown()
    {
        // 쿨타임 완료: 마스크 없음 (스킬 사용 가능)
        if (cooldownMask != null)
        {
            cooldownMask.fillAmount = 0f;
        }

        // 쿨타임 텍스트 숨김
        if (cooldownText != null)
        {
            cooldownText.text = "";
        }
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
