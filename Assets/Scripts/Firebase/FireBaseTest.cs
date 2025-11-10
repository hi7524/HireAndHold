using UnityEngine;
using Cysharp.Threading.Tasks;

public class FirebaseTest : MonoBehaviour
{
    private async void Start()
    {
        // AuthManager 초기화 대기
        while (!AuthManager.Instance.IsInitialized)
        {
            await UniTask.Yield();
        }
        
        string userId = AuthManager.Instance.UserId;
        
        // 익명 로그인이 안 되어 있으면 시도
        if (string.IsNullOrEmpty(userId))
        {
            var (success, error) = await AuthManager.Instance.SingInAnonymouslyAsync();
            if (success)
            {
                userId = AuthManager.Instance.UserId;
            }
            else
            {
                Debug.LogError($"Login failed: {error}");
                return;
            }
        }

        PlayerData newPlayer = new PlayerData
        {
            name = "Alice",
            score = 100
        };

        // Create
        bool createSuccess = await DatabaseManager.Instance.CreatePlayerDataAsync(userId, newPlayer);
        if (!createSuccess) return;

        // Read
        var (data, readSuccess) = await DatabaseManager.Instance.ReadPlayerDataAsync(userId);
        if (!readSuccess) return;

        // Update
        bool updateSuccess = await DatabaseManager.Instance.UpdatePlayerScoreAsync(userId, 200);
        if (!updateSuccess) return;

        // Delete
        // bool deleteSuccess = await DatabaseManager.Instance.DeletePlayerDataAsync(userId);
    }
}

[System.Serializable]
public class PlayerData
{
    public string name;
    public int score;
}
