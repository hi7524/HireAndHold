using UnityEngine;

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

    public async void CreatePlayerData(string userId, PlayerData data)
    {
        string path = $"users/{userId}";
        bool success = await database.CreateDataAsync(path, data);
        Debug.Log($"Create success: {success}");
    }

    public async void ReadPlayerData(string userId)
    {
        string path = $"users/{userId}";
        var (data, success) = await database.GetDataAsync<PlayerData>(path);
        if (success)
        {
            Debug.Log($"Player name: {data.name}, score: {data.score}");
        }
    }

    public async void UpdatePlayerScore(string userId, int newScore)
    {
        string path = $"users/{userId}";
        var (data, success) = await database.GetDataAsync<PlayerData>(path);
        if (success)
        {
            data.score = newScore;
            await database.UpdateDataAsync(path, data);
        }
    }

    public async void DeletePlayerData(string userId)
    {
        string path = $"users/{userId}";
        bool success = await database.DeleteDataAsync(path);
        Debug.Log($"Delete success: {success}");
    }
}

