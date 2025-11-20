using System;
using Cysharp.Threading.Tasks;
using Firebase.Auth;
using UnityEngine;


public class AuthManager : MonoBehaviour
{
    private static AuthManager instance;
    public static AuthManager Instance => instance;

    private FirebaseAuth auth;
    private FirebaseUser currentUser;
    private bool isInitialized = false;

    public FirebaseUser CurrentUser => currentUser;

    public bool IsLoggedIn => currentUser != null;

    public string UserId => currentUser?.UserId ?? string.Empty;
    public bool IsInitialized => isInitialized;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private async UniTaskVoid Start()
    {
        await FirebaseInitializer.Instance.WaitForInitializationAsync();

        auth = FirebaseAuth.DefaultInstance;
        auth.StateChanged += OnAuthStateChanged;

        currentUser = auth.CurrentUser;

        if (currentUser != null)
        {
            Debug.Log($"[Auth] 이미 로그인됨: {UserId}");
        }
        else
        {
            Debug.Log($"[Auth] 로그인 필요");

        }

        isInitialized = true;
    }

    private void OnDestroy()
    {
        if (auth != null)
        {
            auth.StateChanged -= OnAuthStateChanged;
        }
    }


    private void OnAuthStateChanged(object sender, System.EventArgs eventArgrs)
    {
        if (auth.CurrentUser != currentUser)
        {
            bool signedIn = auth.CurrentUser != currentUser && auth.CurrentUser != null;
            if (!signedIn && currentUser != null)
            {
                // Debug.Log("[Auth] 로그 아웃 됨");
            }

            currentUser = auth.CurrentUser;

            if (signedIn)
            {
                // Debug.Log("[Auth] 로그 인 됨");
            }
        }
    }


    public async UniTask<(bool success, string error)> SingInAnonymouslyAsync()
    {
        try
        {
            Debug.Log("[Auth] 익명 로그인 시도...");

            AuthResult result = await auth.SignInAnonymouslyAsync().AsUniTask();
            currentUser = result.User;

            Debug.Log($"[Auth] 익명 로그인 성공: {UserId}");

            return (true, null);
        }
        catch (System.Exception ex)
        {
            Debug.Log($"[Auth] 익명 로그인 실패: {ex.Message}");
            return (false, ex.Message);
        }

    }

    public async UniTask<(bool success, string error)> CreateUserWithEmailAsync(string email, string passwd)
    {
        try
        {
            // Debug.Log("[Auth] 회원 가입 시도...");

            AuthResult result = await auth.CreateUserWithEmailAndPasswordAsync(email, passwd).AsUniTask();
            currentUser = result.User;

            // Debug.Log($"[Auth] 회원 가입 성공: {UserId}");

            return (true, null);
        }
        catch (System.Exception ex)
        {
            // Debug.Log($"[Auth] 회원 가입 실패: {ex.Message}");
            return (false, ex.Message);
        }
    }

    public async UniTask<(bool success, string error)> SighInWithEmailAsync(string email, string passwd)
    {
        try
        {
            // Debug.Log("[Auth] 로그인 시도...");

            AuthResult result = await auth.SignInWithEmailAndPasswordAsync(email, passwd).AsUniTask();
            currentUser = result.User;

            // Debug.Log($"[Auth] 로그인 성공: {UserId}");

            return (true, null);
        }
        catch (System.Exception ex)
        {
            // Debug.Log($"[Auth] 로그인 실패: {ex.Message}");
            return (false, ex.Message);
        }

    }

    public async UniTask SignOutAsync()
    {
        if (auth != null && currentUser != null)
        {
            auth.SignOut();
            currentUser = null;

            // 타임아웃과 함께 대기
            var waitTask = UniTask.WaitUntil(() => auth.CurrentUser == null);
            var timeoutTask = UniTask.Delay(TimeSpan.FromSeconds(3f));

            int result = await UniTask.WhenAny(waitTask, timeoutTask);

            if (result == 0)
            {
                Debug.Log("[Auth] 로그아웃 완료");
            }
            else
            {
                Debug.LogWarning("[Auth] 로그아웃 타임아웃 (강제 진행)");
            }
        }
    }

    private string ParseFirebaseError(string error)
    {
        return error;
    }
}
