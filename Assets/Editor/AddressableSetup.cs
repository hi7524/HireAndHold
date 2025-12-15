using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

public static class AddressableSetup
{
    [MenuItem("Tools/Setup State Effect Addressables")]
    public static void SetupStateEffectAddressables()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("Addressable Asset Settings not found!");
            return;
        }

        // Default Local Group 찾기
        AddressableAssetGroup group = settings.FindGroup("Default Local Group");
        if (group == null)
        {
            Debug.LogError("Default Local Group not found!");
            return;
        }

        // 등록할 이펙트 프리팹 목록
        string[] effectPrefabs = new string[]
        {
            "Assets/StateEffect/EffectPrefabs/effect_state_fear.prefab",
            "Assets/StateEffect/EffectPrefabs/effect_state_stuned.prefab",
            "Assets/StateEffect/EffectPrefabs/effect_state_slowDown.prefab",
            "Assets/StateEffect/EffectPrefabs/effect_state_confusion.prefab",
            "Assets/StateEffect/EffectPrefabs/effect_state_powerUp.prefab",
        };

        int addedCount = 0;
        foreach (string prefabPath in effectPrefabs)
        {
            string guid = AssetDatabase.AssetPathToGUID(prefabPath);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogWarning($"Asset not found: {prefabPath}");
                continue;
            }

            // 이미 등록되어 있는지 확인
            var entry = settings.FindAssetEntry(guid);
            if (entry != null)
            {
                Debug.Log($"Already registered: {prefabPath}");
                continue;
            }

            // Addressable에 추가
            entry = settings.CreateOrMoveEntry(guid, group, false, false);
            if (entry != null)
            {
                // Address를 파일 이름으로 설정 (확장자 제외)
                string fileName = System.IO.Path.GetFileNameWithoutExtension(prefabPath);
                entry.address = fileName;
                addedCount++;
                Debug.Log($"Added to Addressables: {fileName}");
            }
        }

        // 변경사항 저장
        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
        AssetDatabase.SaveAssets();

        Debug.Log($"State Effect Addressables setup complete! Added {addedCount} new prefabs.");
    }
}
