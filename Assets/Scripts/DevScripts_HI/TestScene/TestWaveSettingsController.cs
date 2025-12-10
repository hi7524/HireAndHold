using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 웨이브 설정 - HP/EXP 배율 조절 (WaveTable 기반)
/// DataTableManager.WaveTable을 사용하여 데이터 로드
/// </summary>
public class TestWaveSettingsController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider waveSlider;
    [SerializeField] private TMP_Text waveValueText;
    [SerializeField] private TMP_InputField hpMultiplierInput;
    [SerializeField] private TMP_InputField expMultiplierInput;
    [SerializeField] private Button applyButton;

    [Header("Stage Settings")]
    [SerializeField] private TMP_Dropdown stageDropdown;

    // 현재 설정값
    public float HpMultiplier { get; private set; } = 1f;
    public float ExpMultiplier { get; private set; } = 1f;
    public int CurrentWave { get; private set; } = 1;
    public int CurrentStageId { get; private set; } = 701;

    // WaveTable에서 읽어온 데이터
    private Dictionary<int, List<WaveInfo>> stageWaveData = new Dictionary<int, List<WaveInfo>>();
    private List<int> availableStages = new List<int>();

    private struct WaveInfo
    {
        public int waveNum;
        public int waveType; // 1: 일반, 2: 러시, 3: 중간보스, 4: 최종보스
        public float hpMultiplier;
        public float expMultiplier;
        public int monster1Id;
        public int monster2Id;
    }

    public void Initialize()
    {
        LoadWaveTableData();
        SetupUI();
        ApplyWavePreset(1);
    }

    private void LoadWaveTableData()
    {
        // DataTableManager에서 WaveTable 데이터 로드
        var waveTable = DataTableManager.WaveTable;
        if (waveTable == null)
        {
            Debug.LogWarning("[TestWaveSettings] WaveTable 데이터를 찾을 수 없습니다.");
            return;
        }

        stageWaveData.Clear();
        availableStages.Clear();

        foreach (var data in waveTable.GetAll())
        {
            int stageId = data.STAGE_ID;

            if (!stageWaveData.ContainsKey(stageId))
            {
                stageWaveData[stageId] = new List<WaveInfo>();
                availableStages.Add(stageId);
            }

            stageWaveData[stageId].Add(new WaveInfo
            {
                waveNum = data.WAVE_NUM,
                waveType = data.WAVE_TYPE,
                hpMultiplier = data.MON_HP_UP,
                expMultiplier = data.MON_EXP_UP,
                monster1Id = data.SPAWN_MON1_ID,
                monster2Id = data.SPAWN_MON2_ID
            });
        }

        availableStages.Sort();
        Debug.Log($"[TestWaveSettings] WaveTable 로드 완료 - {availableStages.Count}개 스테이지");
    }

    private void SetupUI()
    {
        // 스테이지 드롭다운 설정
        if (stageDropdown != null)
        {
            stageDropdown.ClearOptions();
            var options = new List<string>();
            foreach (var stageId in availableStages)
            {
                options.Add($"Stage {stageId}");
            }
            stageDropdown.AddOptions(options);
            stageDropdown.onValueChanged.AddListener(OnStageChanged);
        }

        // 웨이브 슬라이더 설정
        if (waveSlider != null)
        {
            waveSlider.minValue = 1;
            waveSlider.maxValue = 10;
            waveSlider.wholeNumbers = true;
            waveSlider.value = 1;
            waveSlider.onValueChanged.AddListener(OnWaveSliderChanged);
        }

        // 배율 입력 필드 초기화
        if (hpMultiplierInput != null)
        {
            hpMultiplierInput.text = "1.0";
            hpMultiplierInput.onEndEdit.AddListener(OnHpMultiplierChanged);
        }

        if (expMultiplierInput != null)
        {
            expMultiplierInput.text = "1.0";
            expMultiplierInput.onEndEdit.AddListener(OnExpMultiplierChanged);
        }

        // 적용 버튼
        if (applyButton != null)
        {
            applyButton.onClick.AddListener(ApplySettings);
        }
    }

    private void OnStageChanged(int index)
    {
        if (index >= 0 && index < availableStages.Count)
        {
            CurrentStageId = availableStages[index];
            ApplyWavePreset(CurrentWave);
            Debug.Log($"[TestWaveSettings] 스테이지 변경: {CurrentStageId}");
        }
    }

    private void OnWaveSliderChanged(float value)
    {
        int wave = Mathf.RoundToInt(value);
        ApplyWavePreset(wave);
    }

    private void ApplyWavePreset(int wave)
    {
        CurrentWave = Mathf.Clamp(wave, 1, 10);

        // WaveTable에서 해당 스테이지/웨이브 데이터 찾기
        if (stageWaveData.TryGetValue(CurrentStageId, out var waves))
        {
            var waveData = waves.Find(w => w.waveNum == CurrentWave);
            if (waveData.waveNum > 0)
            {
                // 보스 웨이브 (타입 3, 4)는 배율이 0으로 설정되어 있음 -> 이전 웨이브 배율 유지
                if (waveData.hpMultiplier > 0)
                {
                    HpMultiplier = waveData.hpMultiplier;
                    ExpMultiplier = waveData.expMultiplier;
                }

                string waveTypeStr = GetWaveTypeName(waveData.waveType);
                if (waveValueText != null)
                    waveValueText.text = $"W{CurrentWave} ({waveTypeStr})";
            }
        }

        // UI 업데이트
        if (waveSlider != null)
            waveSlider.value = CurrentWave;

        if (hpMultiplierInput != null)
            hpMultiplierInput.text = HpMultiplier.ToString("F2");

        if (expMultiplierInput != null)
            expMultiplierInput.text = ExpMultiplier.ToString("F2");

        // 자동으로 MonsterSpawnController에 배율 전달
        ApplySettings();

        Debug.Log($"[TestWaveSettings] Stage {CurrentStageId} Wave {CurrentWave} - HP: x{HpMultiplier}, EXP: x{ExpMultiplier}");
    }

    private string GetWaveTypeName(int waveType)
    {
        return waveType switch
        {
            1 => "일반",
            2 => "러시",
            3 => "중보스",
            4 => "최종보스",
            _ => "???"
        };
    }

    private void OnHpMultiplierChanged(string value)
    {
        if (float.TryParse(value, out float result))
        {
            HpMultiplier = Mathf.Max(0.1f, result);
            hpMultiplierInput.text = HpMultiplier.ToString("F2");
        }
    }

    private void OnExpMultiplierChanged(string value)
    {
        if (float.TryParse(value, out float result))
        {
            ExpMultiplier = Mathf.Max(0.1f, result);
            expMultiplierInput.text = ExpMultiplier.ToString("F2");
        }
    }

    private void ApplySettings()
    {
        // MonsterSpawnController에 배율 전달
        var spawnController = FindObjectOfType<TestMonsterSpawnController>();
        if (spawnController != null)
        {
            spawnController.SetMultipliers(HpMultiplier, ExpMultiplier);
        }
    }

    // 커스텀 배율 직접 설정
    public void SetCustomMultipliers(float hp, float exp)
    {
        HpMultiplier = hp;
        ExpMultiplier = exp;

        if (hpMultiplierInput != null)
            hpMultiplierInput.text = HpMultiplier.ToString("F2");

        if (expMultiplierInput != null)
            expMultiplierInput.text = ExpMultiplier.ToString("F2");
    }

    // 현재 웨이브의 몬스터 ID 반환
    public (int monster1Id, int monster2Id) GetCurrentWaveMonsters()
    {
        if (stageWaveData.TryGetValue(CurrentStageId, out var waves))
        {
            var waveData = waves.Find(w => w.waveNum == CurrentWave);
            if (waveData.waveNum > 0)
            {
                return (waveData.monster1Id, waveData.monster2Id);
            }
        }
        return (0, 0);
    }
}
