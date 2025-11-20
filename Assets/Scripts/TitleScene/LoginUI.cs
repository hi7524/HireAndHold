using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using TMPro;

public class LoginUI : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject signupPanel;

    [Header("Login UI")]
    [SerializeField] private TMP_InputField loginEmailInput;
    [SerializeField] private TMP_InputField loginPasswordInput;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button goToSignupButton;
    [SerializeField] private Button guestLoginButton;

    [Header("Signup UI")]
    [SerializeField] private TMP_InputField signupEmailInput;
    [SerializeField] private TMP_InputField signupPasswordInput;
    [SerializeField] private TMP_InputField signupPasswordConfirmInput;
    [SerializeField] private Button signupButton;
    [SerializeField] private Button backToLoginButton;

    [Header("Feedback")]
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private GameObject loadingIndicator;


    [SerializeField]private GameInitializer gameInitializer;

    private void Start()
    {
        if (loginButton != null)
            loginButton.onClick.AddListener(() => OnLoginButtonClick().Forget());
        
        if (goToSignupButton != null)
            goToSignupButton.onClick.AddListener(ShowSignupPanel);
        
        if (guestLoginButton != null)
            guestLoginButton.onClick.AddListener(() => OnGuestLoginButtonClick().Forget());
        
        if (signupButton != null)
            signupButton.onClick.AddListener(() => OnSignupButtonClick().Forget());
        
        if (backToLoginButton != null)
            backToLoginButton.onClick.AddListener(ShowLoginPanel);

        ShowLoginPanel();
    }

   
    private async UniTaskVoid OnLoginButtonClick()
    {
        string email = loginEmailInput.text.Trim();
        string password = loginPasswordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowFeedback("이메일과 비밀번호를 입력해주세요.", false);
            return;
        }

        SetUIInteractable(false);
        ShowLoading(true);

        var (success, error) = await AuthManager.Instance.SighInWithEmailAsync(email, password);

        SetUIInteractable(true);
        ShowLoading(false);

        if (success)
        {
            ShowFeedback("로그인 성공!", true);
            
            // 로그인 성공 - GameInitializer에 알림
            if (gameInitializer != null)
            {
                gameInitializer.OnLoginSuccess();
            }
        }
        else
        {
            ShowFeedback(GetFriendlyErrorMessage(error), false);
        }
    }

    /// <summary>
    /// 회원가입 버튼 클릭
    /// </summary>
    private async UniTaskVoid OnSignupButtonClick()
    {
        string email = signupEmailInput.text.Trim();
        string password = signupPasswordInput.text;
        string passwordConfirm = signupPasswordConfirmInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowFeedback("이메일과 비밀번호를 입력해주세요.", false);
            return;
        }

        if (password != passwordConfirm)
        {
            ShowFeedback("비밀번호가 일치하지 않습니다.", false);
            return;
        }

        if (password.Length < 6)
        {
            ShowFeedback("비밀번호는 6자 이상이어야 합니다.", false);
            return;
        }

        SetUIInteractable(false);
        ShowLoading(true);

        var (success, error) = await AuthManager.Instance.CreateUserWithEmailAsync(email, password);

        SetUIInteractable(true);
        ShowLoading(false);

        if (success)
        {
            ShowFeedback("회원가입 성공!", true);
            
            // 회원가입 성공 후 자동 로그인
            if (gameInitializer != null)
            {
                gameInitializer.OnLoginSuccess();
            }
        }
        else
        {
            ShowFeedback(GetFriendlyErrorMessage(error), false);
        }
    }

   
    private async UniTaskVoid OnGuestLoginButtonClick()
    {
        SetUIInteractable(false);
        ShowLoading(true);

        var (success, error) = await AuthManager.Instance.SingInAnonymouslyAsync();

        SetUIInteractable(true);
        ShowLoading(false);

        if (success)
        {
            ShowFeedback("게스트 로그인 성공!", true);
            Debug.Log("[LoginUI] 게스트 로그인 성공 - GameInitializer 호출 시도");
            
            if (gameInitializer != null)
            {
                Debug.Log("[LoginUI] GameInitializer.OnLoginSuccess() 호출");
                gameInitializer.OnLoginSuccess();
            }
            else
            {
                Debug.LogError("[LoginUI] GameInitializer가 null입니다! Inspector에서 할당하세요.");
            }
        }
        else
        {
            ShowFeedback(GetFriendlyErrorMessage(error), false);
        }
    }

    private void ShowLoginPanel()
    {
        if (loginPanel != null)
            loginPanel.SetActive(true);
        
        if (signupPanel != null)
            signupPanel.SetActive(false);

        ClearInputFields();
        ShowFeedback("", true);
    }

    /// <summary>
    /// 회원가입 패널 표시
    /// </summary>
    private void ShowSignupPanel()
    {
        if (loginPanel != null)
            loginPanel.SetActive(false);
        
        if (signupPanel != null)
            signupPanel.SetActive(true);

        ClearInputFields();
        ShowFeedback("", true);
    }

    /// <summary>
    /// 입력 필드 초기화
    /// </summary>
    private void ClearInputFields()
    {
        if (loginEmailInput != null) loginEmailInput.text = "";
        if (loginPasswordInput != null) loginPasswordInput.text = "";
        if (signupEmailInput != null) signupEmailInput.text = "";
        if (signupPasswordInput != null) signupPasswordInput.text = "";
        if (signupPasswordConfirmInput != null) signupPasswordConfirmInput.text = "";
    }

    /// <summary>
    /// 피드백 메시지 표시
    /// </summary>
    private void ShowFeedback(string message, bool isSuccess)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
            feedbackText.color = isSuccess ? Color.green : Color.red;
        }
    }

    /// <summary>
    /// 로딩 인디케이터 표시/숨김
    /// </summary>
    private void ShowLoading(bool show)
    {
        if (loadingIndicator != null)
        {
            loadingIndicator.SetActive(show);
        }
    }

    /// <summary>
    /// UI 상호작용 가능 여부 설정
    /// </summary>
    private void SetUIInteractable(bool interactable)
    {
        if (loginButton != null) loginButton.interactable = interactable;
        if (goToSignupButton != null) goToSignupButton.interactable = interactable;
        if (guestLoginButton != null) guestLoginButton.interactable = interactable;
        if (signupButton != null) signupButton.interactable = interactable;
        if (backToLoginButton != null) backToLoginButton.interactable = interactable;

        if (loginEmailInput != null) loginEmailInput.interactable = interactable;
        if (loginPasswordInput != null) loginPasswordInput.interactable = interactable;
        if (signupEmailInput != null) signupEmailInput.interactable = interactable;
        if (signupPasswordInput != null) signupPasswordInput.interactable = interactable;
        if (signupPasswordConfirmInput != null) signupPasswordConfirmInput.interactable = interactable;
    }

    /// <summary>
    /// Firebase 에러 메시지를 사용자 친화적인 메시지로 변환
    /// </summary>
    private string GetFriendlyErrorMessage(string error)
    {
        if (string.IsNullOrEmpty(error))
            return "알 수 없는 오류가 발생했습니다.";

        string lowerError = error.ToLower();

        if (lowerError.Contains("email") && lowerError.Contains("already"))
            return "이미 사용 중인 이메일입니다.";
        
        if (lowerError.Contains("invalid") && lowerError.Contains("email"))
            return "유효하지 않은 이메일 형식입니다.";
        
        if (lowerError.Contains("weak") && lowerError.Contains("password"))
            return "비밀번호가 너무 약합니다. (최소 6자 이상)";
        
        if (lowerError.Contains("wrong") && lowerError.Contains("password"))
            return "잘못된 비밀번호입니다.";
        
        if (lowerError.Contains("user") && lowerError.Contains("not") && lowerError.Contains("found"))
            return "등록되지 않은 사용자입니다.";
        
        if (lowerError.Contains("network"))
            return "네트워크 연결을 확인해주세요.";

        // 기본 에러 메시지
        return $"오류: {error}";
    }
}