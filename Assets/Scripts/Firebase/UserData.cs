using System;
using System.Security;
using UnityEngine;

[Serializable]
public class UserData
{
    public string nickname;
    public string email;
    public long createdAt; 

    public UserData()
    {

    }

    public UserData(string nickname, string email)
    {
        this.nickname = nickname;
        this.email = email;
        this.createdAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(); 
    }

    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }

    public static UserData FromJson(string json)
    {
        return JsonUtility.FromJson<UserData>(json);
    }
}
