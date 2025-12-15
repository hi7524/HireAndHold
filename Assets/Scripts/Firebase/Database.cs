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
        try
        {
            bool transactionAborted = false;

            var transactionResult = await root.Child(path).RunTransaction(mutableData =>
            {
                long currentValue = mutableData.Value != null ? Convert.ToInt64(mutableData.Value) : 0;
                long newValue = currentValue + amount;

                // 차감인데 현재값이 null(0)이면 → 서버에서 실제 값을 가져오도록 Success 반환
                // Firebase가 서버 값과 비교 후 불일치하면 자동으로 재시도함
                if (mutableData.Value == null && amount < 0)
                {
                    // 값을 변경하지 않고 Success 반환 → Firebase가 서버 값으로 재시도
                    return TransactionResult.Success(mutableData);
                }

                // 음수 결과 방지 (실제 값이 있는 상태에서 부족한 경우)
                if (newValue < 0)
                {
                    Debug.LogWarning($"[Database] IncrementValue 실패 - 잔액 부족 ({path}): {currentValue} + {amount} = {newValue}");
                    transactionAborted = true;
                    // 값을 변경하지 않고 Success 반환 (Abort는 예외를 발생시킴)
                    return TransactionResult.Success(mutableData);
                }

                mutableData.Value = newValue;
                return TransactionResult.Success(mutableData);
            }).AsUniTask();

            // Transaction은 성공했지만 잔액 부족으로 중단된 경우
            if (transactionAborted)
            {
                return false;
            }

            return transactionResult != null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Database] IncrementValue 오류 ({path}): {ex.Message}");
            if (ex.InnerException != null)
            {
                Debug.LogError($"[Database] InnerException: {ex.InnerException.Message}");
            }
            return false;
        }
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
