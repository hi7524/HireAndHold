using System;
using System.Collections.Generic;

[Serializable]
public class PresetSlotUnlockData
{
    public Dictionary<string, bool> unlockedSlots = new Dictionary<string, bool>();

    /// <summary>
    /// 특정 프리셋의 특정 슬롯이 해제되었는지 확인
    /// </summary>
    public bool IsSlotUnlocked(int presetIndex, int slotIndex)
    {
        // 0, 1번 슬롯은 항상 해제
        if (slotIndex < 2)
            return true;

        string key = $"preset_{presetIndex}_slot_{slotIndex}";
        return unlockedSlots.TryGetValue(key, out bool unlocked) && unlocked;
    }

    /// <summary>
    /// 슬롯 해제
    /// </summary>
    public void UnlockSlot(int presetIndex, int slotIndex)
    {
        string key = $"preset_{presetIndex}_slot_{slotIndex}";
        unlockedSlots[key] = true;
    }

    /// <summary>
    /// 슬롯 잠금 (디버그용)
    /// </summary>
    public void LockSlot(int presetIndex, int slotIndex)
    {
        string key = $"preset_{presetIndex}_slot_{slotIndex}";
        unlockedSlots[key] = false;
    }
}
