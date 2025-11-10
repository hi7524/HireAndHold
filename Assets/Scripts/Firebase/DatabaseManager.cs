using UnityEngine;
using Cysharp.Threading.Tasks;

public class DatabaseManager : MonoBehaviour
{
    public static DatabaseManager Instance { get; private set; }

    private Database database;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            database = new Database();
            database.Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Example methods for CRUD operations on PlayerData
    public async UniTask<bool> CreatePlayerDataAsync(string userId, PlayerData data)
    {
        try
        {
            string path = $"users/{userId}";
            bool success = await database.CreateDataAsync(path, data);
            Debug.Log($"Create success: {success}");
            return success;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"CreatePlayerData Error: {ex.Message}");
            return false;
        }
    }

    public async UniTask<(PlayerData data, bool success)> ReadPlayerDataAsync(string userId)
    {
        try
        {
            string path = $"users/{userId}";
            var (data, success) = await database.GetDataAsync<PlayerData>(path);
            if (success)
            {
                Debug.Log($"Player name: {data.name}, score: {data.score}");
            }
            return (data, success);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"ReadPlayerData Error: {ex.Message}");
            return (default(PlayerData), false);
        }
    }

    public async UniTask<bool> UpdatePlayerScoreAsync(string userId, int newScore)
    {
        try
        {
            string path = $"users/{userId}";
            var (data, success) = await database.GetDataAsync<PlayerData>(path);
            if (success)
            {
                data.score = newScore;
                bool updateSuccess = await database.UpdateDataAsync(path, data);
                Debug.Log($"Update success: {updateSuccess}");
                return updateSuccess;
            }
            return false;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"UpdatePlayerScore Error: {ex.Message}");
            return false;
        }
    }

    public async UniTask<bool> DeletePlayerDataAsync(string userId)
    {
        try
        {
            string path = $"users/{userId}";
            bool success = await database.DeleteDataAsync(path);
            Debug.Log($"Delete success: {success}");
            return success;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"DeletePlayerData Error: {ex.Message}");
            return false;
        }
    }
}

