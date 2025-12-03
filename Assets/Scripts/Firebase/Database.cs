using UnityEngine;
using Firebase.Database;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

public class Database
{
    private FirebaseDatabase firebaseDatabase;
    private DatabaseReference root;

    public DatabaseReference Root => root;

    public void Initialize()
    {
        firebaseDatabase = FirebaseDatabase.DefaultInstance;
        root = firebaseDatabase.RootReference;
        Debug.Log("[Database] 초기화 완료");
    }

    #region Read

    public async UniTask<(T data, bool success)> GetDataAsync<T>(string path)
    {
        try
        {
            var snapshot = await root.Child(path).GetValueAsync().AsUniTask();

            if (!snapshot.Exists)
            {
                return (default, false);
            }

            string json = snapshot.GetRawJsonValue();
            T data = JsonConvert.DeserializeObject<T>(json);
            return (data, true);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Database] GetData 오류 ({path}): {ex.Message}");
            return (default, false);
        }
    }

    public async UniTask<(List<T> data, bool success)> GetAllDataAsync<T>(string path)
    {
        try
        {
            var snapshot = await root.Child(path).GetValueAsync().AsUniTask();

            if (!snapshot.Exists)
            {
                return (new List<T>(), false);
            }

            var list = new List<T>();
            foreach (var child in snapshot.Children)
            {
                string json = child.GetRawJsonValue();
                T item = JsonConvert.DeserializeObject<T>(json);
                list.Add(item);
            }

            return (list, true);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Database] GetAllData 오류 ({path}): {ex.Message}");
            return (new List<T>(), false);
        }
    }

    #endregion

    #region Write

    public async UniTask<bool> SetDataAsync<T>(string path, T data)
    {
        try
        {
            string json = JsonConvert.SerializeObject(data);
            await root.Child(path).SetRawJsonValueAsync(json).AsUniTask();
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Database] SetData 오류 ({path}): {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Delete

    public async UniTask<bool> DeleteDataAsync(string path)
    {
        try
        {
            await root.Child(path).RemoveValueAsync().AsUniTask();
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Database] DeleteData 오류 ({path}): {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Transaction (동시성 안전)

    public async UniTask<bool> TransactionAsync<T>(string path, Func<T, T> updateFunc) where T : new()
    {
        var transactionResult = await root.Child(path).RunTransaction(mutableData =>
        {
            try
            {
                T currentData;

                if (mutableData.Value == null)
                {
                    currentData = new T();
                }
                else
                {
                    string json = JsonConvert.SerializeObject(mutableData.Value);
                    currentData = JsonConvert.DeserializeObject<T>(json);
                }

                T newData = updateFunc(currentData);
                mutableData.Value = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                    JsonConvert.SerializeObject(newData)
                );

                return TransactionResult.Success(mutableData);
            }
            catch
            {
                return TransactionResult.Abort();
            }
        }).AsUniTask();

        return transactionResult != null;
    }

    public async UniTask<bool> IncrementValueAsync(string path, long amount)
    {
        var transactionResult = await root.Child(path).RunTransaction(mutableData =>
        {
            long currentValue = mutableData.Value != null ? Convert.ToInt64(mutableData.Value) : 0;
            long newValue = currentValue + amount;

            if (newValue < 0)
            {
                return TransactionResult.Abort();
            }

            mutableData.Value = newValue;
            return TransactionResult.Success(mutableData);
        }).AsUniTask();

        return transactionResult != null;
    }

    #endregion

    #region Query

    public async UniTask<List<T>> QueryOrderByChildAsync<T>(string path, string orderByChild, int limitToLast)
    {
        try
        {
            var snapshot = await root.Child(path)
                .OrderByChild(orderByChild)
                .LimitToLast(limitToLast)
                .GetValueAsync().AsUniTask();

            var list = new List<T>();

            foreach (var child in snapshot.Children)
            {
                string json = child.GetRawJsonValue();
                T item = JsonConvert.DeserializeObject<T>(json);
                list.Add(item);
            }

            // 내림차순 정렬
            list.Reverse();
            return list;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Database] Query 오류 ({path}): {ex.Message}");
            return new List<T>();
        }
    }

    #endregion
}
