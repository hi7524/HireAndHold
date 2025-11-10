using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

public class AuthManagerTester : MonoBehaviour
{
    [Header("Dummy Credentials")]
    [SerializeField] private string email = "test_user@example.com";
    [SerializeField] private string password = "test1234";

    private string log = "";
    private bool ready = false;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        // FirebaseInitializer & AuthManager가 준비될 때까지 대기
        await UniTask.WaitUntil(() => FirebaseInitializer.Instance != null && FirebaseInitializer.Instance.IsInitialized,
            cancellationToken: this.GetCancellationTokenOnDestroy());
        await UniTask.WaitUntil(() => AuthManager.Instance != null && AuthManager.Instance.IsInitialized,
            cancellationToken: this.GetCancellationTokenOnDestroy());

        ready = true;
        Append("[Tester] Ready");
        DumpState();
    }

    private void Append(string msg)
    {
        Debug.Log(msg);
        log = $"{DateTime.Now:HH:mm:ss} {msg}\n" + log;
    }

    private void DumpState()
    {
        var am = AuthManager.Instance;
        if (am == null)
        {
            Append("[Tester] AuthManager.Instance == null");
            return;
        }

        Append($"[State] IsInitialized={am.IsInitialized} | IsLoggedIn={am.IsLoggedIn} | UserId={(string.IsNullOrEmpty(am.UserId) ? "null" : am.UserId)}");
    }

    private void OnGUI()
    {
        const int w = 380;
        const int h = 26;
        int x = 20;
        int y = 20;

        GUI.Box(new Rect(x - 10, y - 10, w + 20, 420), "AuthManager Tester");

        GUI.Label(new Rect(x, y, w, h), ready ? "Status: READY" : "Status: WAITING (Init...)");
        y += h + 6;

        GUI.Label(new Rect(x, y, 80, h), "Email");
        email = GUI.TextField(new Rect(x + 90, y, w - 90, h), email);
        y += h + 4;

        GUI.Label(new Rect(x, y, 80, h), "Password");
        password = GUI.PasswordField(new Rect(x + 90, y, w - 90, h), password, '*');
        y += h + 10;

        GUI.enabled = ready;

        if (GUI.Button(new Rect(x, y, w, h), "① Sign In Anonymously"))
        {
            DoSignInAnonymously().Forget();
        }
        y += h + 4;

        if (GUI.Button(new Rect(x, y, w, h), "② Create User With Email"))
        {
            DoCreateUser().Forget();
        }
        y += h + 4;

        if (GUI.Button(new Rect(x, y, w, h), "③ Sign In With Email"))
        {
            DoSignInWithEmail().Forget();
        }
        y += h + 4;

        if (GUI.Button(new Rect(x, y, w, h), "④ Sign Out"))
        {
            DoSignOut();
        }
        y += h + 10;

        GUI.enabled = true;

        // 현재 상태 표시
        var am = AuthManager.Instance;
        string info = (am == null)
            ? "AuthManager.Instance == null"
            : $"IsLoggedIn={am.IsLoggedIn} | UserId={(string.IsNullOrEmpty(am.UserId) ? "null" : am.UserId)}";
        GUI.Label(new Rect(x, y, w, h), info);
        y += h + 6;

        // 로그 창
        GUI.Label(new Rect(x, y, w, 18), "Log (latest first):");
        GUI.TextArea(new Rect(x, y + 18, w, 180), log);
    }

    private async UniTaskVoid DoSignInAnonymously()
    {
        if (AuthManager.Instance == null) { Append("[Tester] AuthManager null"); return; }
        Append("[Tester] Try: Anonymous SignIn");
        var (ok, err) = await AuthManager.Instance.SingInAnonymouslyAsync(); // 네 메서드명 그대로 사용
        Append(ok ? "[Tester] Anonymous SignIn OK" : $"[Tester] Anonymous SignIn FAIL: {err}");
        DumpState();
    }

    private async UniTaskVoid DoCreateUser()
    {
        if (AuthManager.Instance == null) { Append("[Tester] AuthManager null"); return; }
        Append($"[Tester] Try: CreateUser {email}");
        var (ok, err) = await AuthManager.Instance.CreateUserWithEmailAsync(email, password);
        Append(ok ? "[Tester] CreateUser OK" : $"[Tester] CreateUser FAIL: {err}");
        DumpState();
    }

    private async UniTaskVoid DoSignInWithEmail()
    {
        if (AuthManager.Instance == null) { Append("[Tester] AuthManager null"); return; }
        Append($"[Tester] Try: SignIn {email}");
        var (ok, err) = await AuthManager.Instance.SighInWithEmailAsync(email, password); // 네 메서드명 그대로 사용
        Append(ok ? "[Tester] Email SignIn OK" : $"[Tester] Email SignIn FAIL: {err}");
        DumpState();
    }

    private void DoSignOut()
    {
        if (AuthManager.Instance == null) { Append("[Tester] AuthManager null"); return; }
        Append("[Tester] Try: SignOut");
        AuthManager.Instance.SignOut();
        Append("[Tester] SignOut called");
        DumpState();
    }
}
