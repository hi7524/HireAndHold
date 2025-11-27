using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

[Serializable]
public class DeckPreset
{
    public UnitData[] units = new UnitData[5]; 
}

public class DeckControl : MonoBehaviour
{
    // ui 
    public GameObject highlightOverlay;
    public List<DeckSlot> slots;                 
    public Transform unitListParent;           
    public Button completeButton;
    public GameObject detailedPanel;

    //프리셋 버튼들
    public List<Button> presetButtons;       

    //프리셋 스토리지 ? 나중을 위해서
    public DeckPreset[] presets = new DeckPreset[5]; 
    private int activePresetIndex = 0;

    // 맵
    private List<UnitCard> unitCards = new();
    private Dictionary<UnitData, UnitCard> unitMap = new();
    private bool isEditing = false;

    private const string PREFS_KEY = "deck_presets_v1";

    void Awake()
    {
        foreach (var slot in slots)
        {
            slot.SetDeckControl(this);
        }

        if (completeButton != null)
        {
            completeButton.onClick.AddListener(OnCompleteClicked);
            completeButton.interactable = false;
        }

        for (int i = 0; i < presetButtons.Count; i++)
        {
            int idx = i;
            if (presetButtons[idx] != null)
            {
                presetButtons[idx].onClick.AddListener(() => {
                    OnClickPresetButton(idx);
                    OnAnyButtonClicked(presetButtons[idx]);
                });
            }
        }


        for (int i = 0; i < presets.Length; i++)
        {
            if (presets[i] == null)
            {
                presets[i] = new DeckPreset();
            }
            if (presets[i].units == null || presets[i].units.Length != slots.Count)
            {
                presets[i].units = new UnitData[slots.Count];
            }
        }
    }

    void Start()
    {
        detailedPanel.SetActive(false);
        highlightOverlay.SetActive(false);

        foreach (Transform t in unitListParent)
        {
            var card = t.GetComponent<UnitCard>();
            var unitData = t.GetComponent<UnitData>();

            if (card != null && unitData != null)
            {
                card.Init(unitData);
                card.Setup(OnUnitCardClicked);
                unitCards.Add(card);
                unitMap[unitData] = card;
            }
        }


        LoadPresetsFromPrefs();

        LoadPreset(activePresetIndex);
    }

    public void OnClickPresetButton(int index)
    {
        if (index < 0 || index >= presets.Length)
        {
            return;
        }

        if (isEditing)
        {
            ExitEditMode();
        }

        activePresetIndex = index;
        LoadPreset(activePresetIndex);
    }

    public void OnAnyButtonClicked(Button clicked)
    {
        if (clicked == completeButton)
        {
            return;
        }

        if (isEditing)
        {
            ExitEditMode();
        }
    }

    public void LoadPreset(int index)
    {
        if (index < 0 || index >= presets.Length)
        {
            return;
        }

        foreach (var kv in unitMap)
        {
            kv.Value.SetVisible(true);
        }

        var preset = presets[index];

        for (int i = 0; i < slots.Count; i++)
        {
            UnitData unit = null;
            if (preset != null && preset.units != null && i < preset.units.Length)
            {
                unit = preset.units[i];
            }

            slots[i].SetCommittedExternal(unit);

            if (unit != null)
            {
                NotifyUnitAssigned(unit);
            }
        }

        highlightOverlay.SetActive(false);
        UpdateCompleteButtonState();
    }

    public void OnSlotClicked(DeckSlot slot)
    {
        if (!isEditing)
        {
            EnterEditMode();
            return;
        }

        if (slot.HasPending)
        {
            slot.ClearPending();
            UpdateCompleteButtonState();
            return;
        }
    }

    public void EnterEditMode()
    {
        isEditing = true;
        highlightOverlay.SetActive(true);

        foreach (var slot in slots)
        {
            slot.BeginEdit();
        }

        UpdateCompleteButtonState();
    }

    public void ExitEditMode()
    {
        isEditing = false;
        highlightOverlay.SetActive(false);

        foreach (var slot in slots)
        {
            UnitData pendingUnit = slot.GetPending();
            if (pendingUnit != null)
            {
                NotifyUnitCleared(pendingUnit);
            }

            slot.CancelPending();
        }

        UpdateCompleteButtonState();
    }

    public void ToggleEditMode()
    {
        if (isEditing)
        {
            ExitEditMode();
        }
        else
        {
            EnterEditMode();
        }
    }

    void OnUnitCardClicked(UnitData data)
    {
        if (isEditing)
        {
            foreach (var slot in slots)
            {
                if (!slot.HasPending)
                {
                    slot.SetPending(data);
                    NotifyUnitAssigned(data);
                    UpdateCompleteButtonState();
                    return;
                }
            }
        }
        else
        {
            ShowUnitDetailPanel(data);
        }
    }

    void ShowUnitDetailPanel(UnitData data)
    {
        detailedPanel.SetActive(true);
    }

    public void NotifyUnitAssigned(UnitData data)
    {
        if (data == null) return;
        if (unitMap.TryGetValue(data, out var card))
        {
            card.SetVisible(false);
        }
    }

    public void NotifyUnitCleared(UnitData data)
    {
        if (data == null)
        {
            return;
        }

        if (unitMap.TryGetValue(data, out var card))
        {
            card.SetVisible(true);
        }
    }
    void UpdateCompleteButtonState()
    {
        bool allFilled = true;

        foreach (var slot in slots)
        {
            if (!slot.HasPending)
            {
                allFilled = false;
                break;
            }
        }

        completeButton.interactable = allFilled;
    }
    void OnCompleteClicked()
    {
        if (!isEditing) return;

        foreach (var slot in slots)
        {
            if (!slot.HasPending)
            {
                return;
            }
        }

        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].CommitPending();
            presets[activePresetIndex].units[i] = slots[i].GetCommitted();
        }

        isEditing = false;
        highlightOverlay.SetActive(false);
        UpdateCompleteButtonState();
        SavePresetsToPrefs();
    }

    [Serializable]
    private class PresetContainer
    {
        public string[] unitIds; 
    }

    private void SavePresetsToPrefs()
    {
        try
        {
            var wrapper = new List<int?>();


            for (int p = 0; p < presets.Length; p++)
            {
                for (int s = 0; s < slots.Count; s++)
                {
                    var u = presets[p].units[s];
                    if (u == null)
                    {
                        wrapper.Add(null);
                    }
                    else
                    {
                        wrapper.Add(u.unitId);
                    }
                }
            }

            string json = JsonUtility.ToJson(new SerializationIntNullable { items = wrapper.ToArray() });
            PlayerPrefs.SetString(PREFS_KEY, json);
            PlayerPrefs.Save();
            Debug.Log("[덱 컨트롤 프리셋 저장 완료");
        }
        catch (Exception e)
        {
            Debug.LogWarning("프리셋 저장 실패 " + e);
        }
    }

    private void LoadPresetsFromPrefs()
    {
        if (!PlayerPrefs.HasKey(PREFS_KEY))
        {
            return;
        }

        try
        {
            string json = PlayerPrefs.GetString(PREFS_KEY);
            var container = JsonUtility.FromJson<SerializationIntNullable>(json);
            if (container == null || container.items == null)
            {
                return;
            }

            int idx = 0;
            for (int p = 0; p < presets.Length; p++)
            {
                for (int s = 0; s < slots.Count; s++)
                {
                    if (idx >= container.items.Length)
                    {
                        break;
                    }

                    int? maybeId = container.items[idx++];
                    if (maybeId.HasValue)
                    {
                        UnitData found = FindUnitDataById(maybeId.Value);
                        presets[p].units[s] = found;
                    }
                    else
                    {
                        presets[p].units[s] = null;
                    }
                }
            }

            Debug.Log("덱컨트롤에서 프리셋 로드 됨");
        }
        catch (Exception e)
        {
            Debug.Log("프리셋 로드 실패" + e);
        }
    }

    [Serializable]
    private class SerializationIntNullable
    {
        public int?[] items;
    }

    private UnitData FindUnitDataById(int id)
    {
        foreach (var kv in unitMap)
        {
            if (kv.Key.unitId == id)
                return kv.Key;
        }
        return null;
    }
}
