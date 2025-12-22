using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using System.Collections.Generic;

/// <summary>
/// 플레이어 스킬 프리팹을 Addressable로 일괄 등록하는 에디터 유틸리티
/// </summary>
public static class SkillAddressableRegistrar
{
    public const string PlayerSkillLabel = "PlayerSkillPrefab";

    // 스킬 프리팹 경로 → Addressable Key 매핑
    private static readonly Dictionary<string, string> SkillPrefabMappings = new Dictionary<string, string>
    {
        { "Assets/Prefabs/Skills/EarthQuake.prefab", "EarthQuake" },
        { "Assets/Prefabs/Skills/EternalBlizzard.prefab", "EternalBlizzard" },
        { "Assets/Prefabs/Skills/BlackHoleSkill.prefab", "BlackHoleSkill" },
        { "Assets/Prefabs/Skills/AirForce.prefab", "AirForce" },
        { "Assets/Prefabs/Skills/ChaosWave.prefab", "ChaosWave" },
        { "Assets/Prefabs/Skills/AnkleCatch.prefab", "AnkleCatch" },
        { "Assets/Prefabs/Skills/SuperNova.prefab", "SuperNova" },
        { "Assets/Prefabs/Skills/FlagOfVictory.prefab", "FlagOfVictory" },
        { "Assets/Prefabs/Skills/FlagOfCourage.prefab", "FlagOfCourage" },
        { "Assets/Prefabs/Skills/FlagOfSpeed.prefab", "FlagOfSpeed" },
        { "Assets/Prefabs/Skills/GreatSlow.prefab", "GreatSlow" },
    };

    [MenuItem("Tools/Addressable/Register Player Skill Prefabs")]
    public static void RegisterSkillPrefabs()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("[SkillAddressableRegistrar] Addressable Settings가 없습니다. Window > Asset Management > Addressables > Groups에서 생성해주세요.");
            return;
        }

        // PlayerSkillPrefab Label 추가
        if (!settings.GetLabels().Contains(PlayerSkillLabel))
        {
            settings.AddLabel(PlayerSkillLabel);
        }

        // PlayerSkillPrefab 그룹 찾기 또는 생성
        var group = settings.FindGroup("PlayerSkillPrefab");
        if (group == null)
        {
            group = settings.CreateGroup("PlayerSkillPrefab", false, false, false, null, typeof(UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema));
        }

        int registeredCount = 0;
        foreach (var kvp in SkillPrefabMappings)
        {
            string assetPath = kvp.Key;
            string addressableKey = kvp.Value;

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogWarning($"[SkillAddressableRegistrar] 프리팹을 찾을 수 없음: {assetPath}");
                continue;
            }

            // 이미 등록되어 있는지 확인
            var existingEntry = settings.FindAssetEntry(guid);
            if (existingEntry != null)
            {
                // 키 업데이트 및 Label 추가
                existingEntry.address = addressableKey;
                if (!existingEntry.labels.Contains(PlayerSkillLabel))
                {
                    existingEntry.SetLabel(PlayerSkillLabel, true);
                }
            }
            else
            {
                // 새로 등록
                var entry = settings.CreateOrMoveEntry(guid, group, false, false);
                entry.address = addressableKey;
                entry.SetLabel(PlayerSkillLabel, true);
            }
            registeredCount++;
        }

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
        AssetDatabase.SaveAssets();
    }
}
