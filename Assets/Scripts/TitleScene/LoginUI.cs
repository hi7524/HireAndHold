using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using TMPro;
using System.Text.RegularExpressions;

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

    [Header("Validation Messages")]
    [SerializeField] private TextMeshProUGUI signupEmailValidationText;
    [SerializeField] private TextMeshProUGUI signupPasswordValidationText;
    [SerializeField] private TextMeshProUGUI signupPasswordConfirmValidationText;

    [Header("Feedback")]
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private GameObject loadingIndicator;

    [Header("Validation Settings")]
    [SerializeField] private float messageFadeDuration = 3f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    [SerializeField] private GameInitializer gameInitializer;

    private bool isProcessing = false;

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

        // 회원가입 입력 필드에 실시간 검증 리스너 추가
        if (signupEmailInput != null)
            signupEmailInput.onValueChanged.AddListener(OnSignupEmailChanged);

        if (signupPasswordInput != null)
            signupPasswordInput.onValueChanged.AddListener(OnSignupPasswordChanged);

        if (signupPasswordConfirmInput != null)
            signupPasswordConfirmInput.onValueChanged.AddListener(OnSignupPasswordConfirmChanged);

        // 검증 메시지 초기화
        HideValidationMessage(signupEmailValidationText);
        HideValidationMessage(signupPasswordValidationText);
        HideValidationMessage(signupPasswordConfirmValidationText);

        ShowLoginPanel();
    }

    #region Validation Methods

    /// <summary>
    /// 이메일 입력 검증
    /// </summary>
    private void OnSignupEmailChanged(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            HideValidationMessage(signupEmailValidationText);
            return;
        }

        if (!IsValidEmail(email))
        {
            ShowValidationMessage(signupEmailValidationText, "이메일 형식으로 입력");
        }
        else
        {
            HideValidationMessage(signupEmailValidationText);
        }
    }

    /// <summary>
    /// 비밀번호 입력 검증
    /// </summary>
    private void OnSignupPasswordChanged(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            HideValidationMessage(signupPasswordValidationText);
            return;
        }

        var (isValid, errorMessage) = ValidatePassword(password);
        if (!isValid)
        {
            ShowValidationMessage(signupPasswordValidationText, errorMessage);
        }
        else
        {
            HideValidationMessage(signupPasswordValidationText);
        }

        // 비밀번호 확인 필드도 함께 검증
        if (signupPasswordConfirmInput != null && !string.IsNullOrEmpty(signupPasswordConfirmInput.text))
        {
            OnSignupPasswordConfirmChanged(signupPasswordConfirmInput.text);
        }
    }

    /// <summary>
    /// 비밀번호 확인 입력 검증
    /// </summary>
    private void OnSignupPasswordConfirmChanged(string passwordConfirm)
    {
        if (string.IsNullOrEmpty(passwordConfirm))
        {
            HideValidationMessage(signupPasswordConfirmValidationText);
            return;
        }

        if (signupPasswordInput != null && passwordConfirm != signupPasswordInput.text)
        {
            ShowValidationMessage(signupPasswordConfirmValidationText, "비밀번호가 일치하지 않습니다");
        }
        else
        {
            HideValidationMessage(signupPasswordConfirmValidationText);
        }
    }

    /// <summary>
    /// 이메일 형식 검증
    /// </summary>
    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            // 간단한 이메일 정규식 패턴
            string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            return Regex.IsMatch(email, pattern);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 비밀번호 검증 (영어, 숫자만 허용, 6자 이상)
    /// </summary>
    private (bool isValid, string errorMessage) ValidatePassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            return (false, "비밀번호를 입력해주세요");

        // 영어와 숫자만 허용 (특수문자 제외)
        if (!Regex.IsMatch(password, @"^[a-zA-Z0-9]+$"))
            return (false, "영어와 숫자만 입력 가능");

        if (password.Length < 6)
            return (false, "비밀번호는 6자 이상");

        return (true, "");
    }

    /// <summary>
    /// 검증 메시지 표시 및 자동 페이드아웃
    /// </summary>
    private void ShowValidationMessage(TextMeshProUGUI validationText, string message)
    {
        if (validationText == null) return;

        validationText.text = message;
        validationText.color = new Color(1f, 0f, 0f, 1f); // 빨간색
        validationText.gameObject.SetActive(true);

        // 기존 페이드 작업 취소 및 새로운 페이드 시작
        FadeOutValidationMessage(validationText).Forget();
    }

    /// <summary>
    /// 검증 메시지 즉시 숨김
    /// </summary>
    private void HideValidationMessage(TextMeshProUGUI validationText)
    {
        if (validationText == null) return;
        validationText.gameObject.SetActive(false);
    }

    /// <summary>
    /// 검증 메시지 페이드아웃 애니메이션
    /// </summary>
    private async UniTaskVoid FadeOutValidationMessage(TextMeshProUGUI validationText)
    {
        if (validationText == null) return;

        // 메시지가 표시된 상태로 대기
        await UniTask.Delay((int)(messageFadeDuration * 1000));

        // 페이드아웃
        float elapsedTime = 0f;
        Color startColor = validationText.color;

        while (elapsedTime < fadeOutDuration)
        {
            if (validationText == null) return;

            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutDuration);
            validationText.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            await UniTask.Yield();
        }

        // 완전히 숨김
        if (validationText != null)
        {
            validationText.gameObject.SetActive(false);
        }
    }

    #endregion

    #region Login/Signup Methods

    private async UniTaskVoid OnLoginButtonClick()
    {
        if (isProcessing) return;

        string email = loginEmailInput.text.Trim();
        string password = loginPasswordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowFeedback("이메일과 비밀번호를 입력해주세요.", false);
            return;
        }

        isProcessing = true;
        SetUIInteractable(false);
        ShowLoading(true);

        var (success, error) = await AuthManager.Instance.SighInWithEmailAsync(email, password);

        if (success)
        {
            ShowFeedback("로그인 성공!", true);

            if (gameInitializer != null)
            {
                gameInitializer.OnLoginSuccess();
            }
        }
        else
        {
            isProcessing = false;
            SetUIInteractable(true);
            ShowLoading(false);
            ShowFeedback(GetFriendlyErrorMessage(error), false);
        }
    }

    /// <summary>
    /// 회원가입 버튼 클릭
    /// </summary>
    private async UniTaskVoid OnSignupButtonClick()
    {
        if (isProcessing) return;

        string email = signupEmailInput.text.Trim();
        string password = signupPasswordInput.text;
        string passwordConfirm = signupPasswordConfirmInput.text;

        // 최종 검증
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowFeedback("이메일과 비밀번호를 입력해주세요.", false);
            return;
        }

        if (!IsValidEmail(email))
        {
            ShowFeedback("유효한 이메일 형식이 아닙니다.", false);
            return;
        }

        var (isValidPassword, passwordError) = ValidatePassword(password);
        if (!isValidPassword)
        {
            ShowFeedback(passwordError, false);
            return;
        }

        if (password != passwordConfirm)
        {
            ShowFeedback("비밀번호가 일치하지 않습니다.", false);
            return;
        }

        isProcessing = true;
        SetUIInteractable(false);
        ShowLoading(true);

        var (success, error) = await AuthManager.Instance.CreateUserWithEmailAsync(email, password);

        if (success)
        {
            ShowFeedback("회원가입 성공!", true);

            if (gameInitializer != null)
            {
                gameInitializer.OnLoginSuccess();
            }
        }
        else
        {
            isProcessing = false;
            SetUIInteractable(true);
            ShowLoading(false);
            ShowFeedback(GetFriendlyErrorMessage(error), false);
        }
    }

    private async UniTaskVoid OnGuestLoginButtonClick()
    {
        if (isProcessing) return;

        isProcessing = true;
        SetUIInteractable(false);
        ShowLoading(true);

        var (success, error) = await AuthManager.Instance.SingInAnonymouslyAsync();

        if (success)
        {
            ShowFeedback("게스트 로그인 성공!", true);
            if (gameInitializer != null)
            {
                gameInitializer.OnLoginSuccess();
            }
            else
            {
                Debug.LogError("[LoginUI] GameInitializer가 null입니다! Inspector에서 할당하세요.");
            }
        }
        else
        {
            isProcessing = false;
            SetUIInteractable(true);
            ShowLoading(false);
            ShowFeedback(GetFriendlyErrorMessage(error), false);
        }
    }

    #endregion

    #region UI Panel Methods

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

        // 검증 메시지 초기화
        HideValidationMessage(signupEmailValidationText);
        HideValidationMessage(signupPasswordValidationText);
        HideValidationMessage(signupPasswordConfirmValidationText);
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

    #endregion

    #region Feedback Methods

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

        return $"오류: {error}";
    }

    #endregion
}
