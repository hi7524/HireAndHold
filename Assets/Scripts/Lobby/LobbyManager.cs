using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using GameData;
using Tutorial;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private WindowManager windowManager;

    private void Start()
    {
        Time.timeScale = 1f;

        // 우편함 테스트 로그
        TestMailSystem();

        // 업적 시스템 테스트 로그
        TestAchievementSystem();

        // 튜토리얼 체크 및 시작
        CheckTutorialAsync().Forget();
    }

    private async UniTaskVoid CheckTutorialAsync()
    {
        // TutorialManager가 있으면 로비 진입 튜토리얼 체크
        if (TutorialManager.Instance != null)
        {
            await TutorialManager.Instance.CheckAndStartTutorialAsync(TutorialTriggerType.OnLobbyEnter);
        }
    }

    private void TestMailSystem()
    {
        Debug.Log("========== [Mail Test] 우편함 시스템 테스트 시작 ==========");

        if (DatabaseManager.Instance == null)
        {
            Debug.LogError("[Mail Test] DatabaseManager.Instance가 null입니다!");
            return;
        }

        if (DatabaseManager.Instance.CurrentUser == null)
        {
            Debug.LogError("[Mail Test] CurrentUser가 null입니다!");
            return;
        }

        var user = DatabaseManager.Instance.CurrentUser;

        // mails 필드 확인
        if (user.mails == null)
        {
            Debug.LogWarning("[Mail Test] mails가 null입니다. 초기화합니다.");
            user.mails = new System.Collections.Generic.Dictionary<string, MailData>();
        }

        // 개인 메일 정보
        Debug.Log($"[Mail Test] ===== 개인 메일 =====");
        Debug.Log($"[Mail Test] 개인 메일 수: {user.mails.Count}");
        Debug.Log($"[Mail Test] 읽지 않은 개인 메일: {DatabaseManager.Instance.GetUnreadMailCount()}개");
        Debug.Log($"[Mail Test] 수령 가능한 개인 메일: {DatabaseManager.Instance.GetClaimableMailCount()}개");

        var personalMails = DatabaseManager.Instance.GetValidMails();
        foreach (var mail in personalMails)
        {
            Debug.Log($"[Mail Test] ─────────────────────");
            Debug.Log($"[Mail Test] [개인] ID: {mail.mailId}");
            Debug.Log($"[Mail Test] 제목: {mail.title}");
            Debug.Log($"[Mail Test] 내용: {mail.content}");
            Debug.Log($"[Mail Test] 읽음: {mail.isRead}, 수령: {mail.isClaimed}");
            LogMailReward(mail.reward);
        }

        // 전역 메일 정보
        Debug.Log($"[Mail Test] ===== 전역 메일 =====");
        var globalMails = DatabaseManager.Instance.GetValidGlobalMails();
        Debug.Log($"[Mail Test] 전역 메일 수: {globalMails.Count}");
        Debug.Log($"[Mail Test] 수령하지 않은 전역 메일: {DatabaseManager.Instance.GetUnclaimedGlobalMailCount()}개");

        foreach (var mail in globalMails)
        {
            bool isClaimed = DatabaseManager.Instance.IsGlobalMailClaimed(mail.mailId);
            Debug.Log($"[Mail Test] ─────────────────────");
            Debug.Log($"[Mail Test] [전역] ID: {mail.mailId}");
            Debug.Log($"[Mail Test] 제목: {mail.title}");
            Debug.Log($"[Mail Test] 내용: {mail.content}");
            Debug.Log($"[Mail Test] 수령: {isClaimed}");
            LogMailReward(mail.reward);
        }

        // 통합 정보
        Debug.Log($"[Mail Test] ===== 통합 =====");
        Debug.Log($"[Mail Test] 전체 읽지않은 메일: {DatabaseManager.Instance.GetTotalUnreadMailCount()}개");
        Debug.Log($"[Mail Test] 전체 수령 가능: {DatabaseManager.Instance.GetTotalClaimableMailCount()}개");

        if (personalMails.Count == 0 && globalMails.Count == 0)
        {
            Debug.LogWarning("[Mail Test] 메일이 없습니다. Firebase에서 테스트 메일을 추가해주세요.");
            Debug.Log("[Mail Test] 개인 메일 경로: users/{유저ID}/mails/");
            Debug.Log("[Mail Test] 전역 메일 경로: globalMails/");
        }

        Debug.Log("========== [Mail Test] 우편함 시스템 테스트 완료 ==========");
    }

    private void LogMailReward(MailReward reward)
    {
        if (reward == null) return;

        Debug.Log($"[Mail Test] 보상 - 골드:{reward.gold}, 다이아:{reward.diamond}, 스태미나:{reward.stamina}, 강화석:{reward.enhanceStone}");
        if (reward.items != null && reward.items.Count > 0)
        {
            foreach (var item in reward.items)
            {
                Debug.Log($"[Mail Test]   아이템 {item.Key}: {item.Value}개");
            }
        }
    }

    private void TestAchievementSystem()
    {
        Debug.Log("========== [Achievement Test] 업적 시스템 테스트 시작 ==========");

        // 전체 업적 상태 출력
        AchievementManager.DebugPrintAllAchievements();

        // 테스트: 튜토리얼 완료 업적 진행 (조건키로 테스트)
        // TestAchievementProgressAsync().Forget();

        // Debug.Log("========== [Achievement Test] 업적 시스템 테스트 완료 ==========");
    }

    private async UniTaskVoid TestAchievementProgressAsync()
    {
        // 몬스터 킬 업적 테스트 (누적형)
        Debug.Log("[Achievement Test] 몬스터 킬 +10 테스트...");
        await AchievementManager.AddMonsterKillAsync(10);

        // 로그인 일수 업적 테스트 (절대값)
        Debug.Log("[Achievement Test] 로그인 1일차 테스트...");
        await AchievementManager.UpdateLoginDaysAsync(1);

        // 진행 후 상태 재출력
        Debug.Log("[Achievement Test] ===== 진행 후 상태 =====");
        AchievementManager.DebugPrintAllAchievements();

        // 수령 가능한 업적 확인
        var claimable = AchievementManager.GetClaimableAchievements();
        if (claimable.Count > 0)
        {
            Debug.Log($"[Achievement Test] 수령 가능한 업적 {claimable.Count}개:");
            foreach (var achievement in claimable)
            {
                Debug.Log($"[Achievement Test] - ID:{achievement.Achievements_ID}, 조건:{achievement.Condition_Key}");
            }
        }
    }

    public void OnClickedStoreButton()
    {
        windowManager.Open(Windows.Store);
    }
    public void OnClickedMainButton()
    {
        windowManager.Open(Windows.Main);
    }

    public void OnClickedDungeonButton()
    {
        windowManager.Open(Windows.Dungeon);
    }
    public void OnClickedUnitButton()
    {
        windowManager.Open(Windows.Unit);
    }
    public void OnClickedStageButton()
    {
        windowManager.Open(Windows.Stage);
    }

    public void OnClickedEnforceButton()
    {
        windowManager.Open(Windows.Enforce);
    }
    public async void OnClickedLogOutButton()
    {
        await AuthManager.Instance.SignOutAsync();
        SceneManager.LoadScene("01_Title");
    }


}
