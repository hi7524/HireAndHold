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
        }
        else
        {
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

    public async UniTask WaitForInitializationAsync()
    {
        await UniTask.WaitUntil(() => isInitialized);
    }


    private void OnAuthStateChanged(object sender, System.EventArgs eventArgrs)
    {
        if (auth.CurrentUser != currentUser)
        {
            bool signedIn = auth.CurrentUser != currentUser && auth.CurrentUser != null;
            if (!signedIn && currentUser != null)
            {
            }

            currentUser = auth.CurrentUser;

            if (signedIn)
            {
            }
        }
    }


    public async UniTask<(bool success, string error)> SingInAnonymouslyAsync()
    {
        try
        {
            AuthResult result = await auth.SignInAnonymouslyAsync().AsUniTask();
            currentUser = result.User;
            return (true, null);
        }
        catch (System.Exception ex)
        {
            return (false, ex.Message);
        }

    }

    public async UniTask<(bool success, string error)> CreateUserWithEmailAsync(string email, string passwd)
    {
        try
        {
            AuthResult result = await auth.CreateUserWithEmailAndPasswordAsync(email, passwd).AsUniTask();
            currentUser = result.User;
            return (true, null);
        }
        catch (System.Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async UniTask<(bool success, string error)> SighInWithEmailAsync(string email, string passwd)
    {
        try
        {
            AuthResult result = await auth.SignInWithEmailAndPasswordAsync(email, passwd).AsUniTask();
            currentUser = result.User;
            return (true, null);
        }
        catch (System.Exception ex)
        {
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
