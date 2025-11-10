using CsvHelper;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.ResourceManagement.AsyncOperations;

public abstract class DataTable
{
    public static readonly string FormatPath = "DataTables/{0}";

    public abstract UniTask LoadAsync(string filename);
    public static List<T> LoadCSV<T>(string csvText)
    {
        using (var reader = new StringReader(csvText))
        using (var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            var records = csvReader.GetRecords<T>();
            return records.ToList();
        }
    }
     public static async UniTask<List<T>> LoadCSVFromAddressablesAsync<T>(string addressableKey)
    {
        // TextAsset을 Addressables로 로드
        AsyncOperationHandle<TextAsset> handle = Addressables.LoadAssetAsync<TextAsset>(addressableKey);
        TextAsset csvAsset = await handle.ToUniTask();

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            List<T> records = LoadCSV<T>(csvAsset.text);
            
            // 사용 후 리소스 해제
            Addressables.Release(handle);
            
            return records;
        }
        else
        {
            Debug.LogError($"Failed to load CSV from Addressables: {addressableKey}");
            Addressables.Release(handle);
            return new List<T>();
        }
    }
}