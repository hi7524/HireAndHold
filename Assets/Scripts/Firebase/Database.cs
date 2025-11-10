using Firebase.Database;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class Database
{
    private FirebaseDatabase firebaseDatabase;
    private DatabaseReference root;

    public void Initialize()
    {
        firebaseDatabase = FirebaseDatabase.DefaultInstance;
        root = firebaseDatabase.RootReference;
    }

    public async UniTask<(T data, bool success)> GetDataAsync<T>(string path)
    {
        try
        {
            var snapshot = await root.Child(path).GetValueAsync();
            if (!snapshot.Exists) return (default, false);

            string json = snapshot.GetRawJsonValue();
            T data = JsonConvert.DeserializeObject<T>(json);
            return (data, true);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"GetDataAsync Error: {ex}");
            return (default, false);
        }
    }

    public async UniTask<(List<T> data, bool success)> GetAllDataAsync<T>(string path)
    {
        try
        {
            var snapshot = await root.Child(path).GetValueAsync();
            if (!snapshot.Exists) return (default, false);

            var list = new List<T>();
            foreach (var child in snapshot.Children)
            {
                string json = child.GetRawJsonValue();
                T item = JsonConvert.DeserializeObject<T>(json);
                list.Add(item);
            }
            return (list, true);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"GetAllDataAsync Error: {ex}");
            return (default, false);
        }
    }

    public async UniTask<bool> CreateDataAsync<T>(string path, T data)
    {
        try
        {
            string json = JsonConvert.SerializeObject(data);
            await root.Child(path).SetRawJsonValueAsync(json).AsUniTask();
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"CreateDataAsync Error: {ex}");
            return false;
        }
    }

    public async UniTask<bool> UpdateDataAsync<T>(string path, T data)
    {
        return await CreateDataAsync(path, data); // 덮어쓰기 방식
    }

    public async UniTask<bool> DeleteDataAsync(string path)
    {
        try
        {
            await root.Child(path).RemoveValueAsync().AsUniTask();
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"DeleteDataAsync Error: {ex}");
            return false;
        }
    }
}