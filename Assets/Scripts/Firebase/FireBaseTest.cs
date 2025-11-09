using UnityEngine;

public class FirebaseTest : MonoBehaviour
{
    private void Start()
    {
        
        string userId = AuthManager.Instance.UserId;

        PlayerData newPlayer = new PlayerData
        {
            name = "Alice",
            score = 100
        };

        // Create
        DatabaseManager.Instance.CreatePlayerData(userId, newPlayer);

        // Read
        DatabaseManager.Instance.ReadPlayerData(userId);

        // Update
        DatabaseManager.Instance.UpdatePlayerScore(userId, 200);

        // Delete
        // DatabaseManager.Instance.DeletePlayerData(userId);
    }
}

[System.Serializable]
public class PlayerData
{
    public string name;
    public int score;
}
