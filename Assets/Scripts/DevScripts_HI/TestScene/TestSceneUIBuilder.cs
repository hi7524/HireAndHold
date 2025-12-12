using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;

/// <summary>
/// 테스트 씬 UI를 런타임에 동적으로 생성
/// </summary>
public class TestSceneUIBuilder : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform wallTransform;
    [SerializeField] private ExperienceCollector expCollector;
    [SerializeField] private ObjectPoolManager poolManager;
    [SerializeField] private PassiveSkillManager passiveSkillManager;
    [SerializeField] private Transform skillSpawnPoint;
    
    [Header("UI Settings")]
    [SerializeField] private Color panelColor = new Color(0.9f, 0.9f, 0.9f, 0.95f);
    [SerializeField] private Color buttonColor = new Color(0.2f, 0.5f, 0.8f, 1f);
    [SerializeField] private Color textColor = Color.black;
    [SerializeField] private int fontSize = 28;
    [SerializeField] private TMP_FontAsset defaultFont;
    
    // 생성된 컨트롤러 참조
    private TestMonsterSpawnController monsterSpawnController;
    private TestWaveSettingsController waveSettingsController;
    private TestPassiveSkillController passiveSkillController;
    private TestPlayerSkillController playerSkillController;
    private TestUnitStatEditor unitStatEditor;
    private TestMonsterStatEditor monsterStatEditor;
    private TestStatusEffectController statusEffectController;

    // UI 패널들
    private GameObject settingsPanel;
    private GameObject monsterPanel;
    private GameObject wavePanel;
    private GameObject passivePanel;
    private GameObject skillPanel;
    private GameObject unitStatPanel;
    private GameObject monsterStatPanel;
    private GameObject statusEffectPanel;
    
    private async void Start()
    {
        // DataTable 초기화 대기
        await DataTableManager.InitAsync();

        // 한글 폰트 로드 시도 (Addressables)
        await LoadKoreanFontAsync();

        // 자동 참조 찾기
        AutoFindReferences();

        // 씬 참조 설정
        SetupSceneReferences();

        // UI 생성
        BuildUI();

        Debug.Log("[TestSceneUIBuilder] UI 생성 완료");
    }

    private void AutoFindReferences()
    {
        // Inspector에서 설정되지 않은 참조들을 자동으로 찾기
        if (rootCanvas == null)
            rootCanvas = FindObjectOfType<Canvas>();

        if (poolManager == null)
            poolManager = FindObjectOfType<ObjectPoolManager>();

        if (passiveSkillManager == null)
            passiveSkillManager = FindObjectOfType<PassiveSkillManager>();

        if (expCollector == null)
            expCollector = FindObjectOfType<ExperienceCollector>();

        if (wallTransform == null)
        {
            var wall = GameObject.FindGameObjectWithTag("Wall");
            if (wall != null) wallTransform = wall.transform;
        }

        if (spawnPoint == null)
        {
            var spawn = GameObject.Find("MonsterSpawnPoint");
            if (spawn != null) spawnPoint = spawn.transform;
        }

        if (skillSpawnPoint == null)
        {
            var skillSpawn = GameObject.Find("SkillSpawnpoint");
            if (skillSpawn != null) skillSpawnPoint = skillSpawn.transform;
        }

        // TMP 기본 폰트 찾기 (한글 지원 폰트 우선)
        if (defaultFont == null)
        {
            // 씬에서 기존 TMP 텍스트에서 폰트 가져오기 (한글 폰트일 가능성 높음)
            var existingTmp = FindObjectOfType<TMP_Text>();
            if (existingTmp != null && existingTmp.font != null)
            {
                defaultFont = existingTmp.font;
                Debug.Log($"[TestSceneUIBuilder] 씬에서 폰트 발견: {defaultFont.name}");
            }

            // TMP 기본 폰트 사용
            if (defaultFont == null)
            {
                defaultFont = TMP_Settings.defaultFontAsset;
            }

            if (defaultFont != null)
            {
                Debug.Log($"[TestSceneUIBuilder] 폰트 로드 완료: {defaultFont.name}");
            }
            else
            {
                Debug.LogWarning("[TestSceneUIBuilder] TMP 폰트를 찾을 수 없습니다!");
            }
        }
    }

    // Addressables로 한글 폰트 로드 (Start에서 호출)
    private async UniTask LoadKoreanFontAsync()
    {
        if (defaultFont != null) return;

        // 한글 폰트 이름 패턴
        string[] koreanFontNames = { "Noto", "ONE MOBILE", "Pyeojin", "Gothic", "KR" };

        // 1. 씬에서 한글 폰트를 사용하는 TMP 찾기
        var allTmpTexts = FindObjectsOfType<TMP_Text>(true);
        foreach (var tmp in allTmpTexts)
        {
            if (tmp.font != null)
            {
                string fontName = tmp.font.name;
                foreach (var pattern in koreanFontNames)
                {
                    if (fontName.Contains(pattern))
                    {
                        defaultFont = tmp.font;
                        Debug.Log($"[TestSceneUIBuilder] 씬에서 한글 폰트 발견: {defaultFont.name}");
                        return;
                    }
                }
            }
        }

        // 2. Addressables로 로드 시도
        string[] koreanFontPaths = {
            "Assets/Fonts/NotoSansKR-Regular SDF.asset",
            "Assets/Fonts/ONE MOBILE POP SDF.asset",
            "Assets/Fonts/PyeojinGothic-Regular SDF.asset"
        };

        foreach (var fontPath in koreanFontPaths)
        {
            try
            {
                var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<TMP_FontAsset>(fontPath);
                defaultFont = await handle.ToUniTask();
                if (defaultFont != null)
                {
                    Debug.Log($"[TestSceneUIBuilder] Addressables 폰트 로드 성공: {defaultFont.name}");
                    return;
                }
            }
            catch
            {
                // 다음 폰트 시도
            }
        }

        // 3. 아무 TMP에서 폰트 가져오기 (마지막 수단)
        if (defaultFont == null && allTmpTexts.Length > 0)
        {
            foreach (var tmp in allTmpTexts)
            {
                if (tmp.font != null)
                {
                    defaultFont = tmp.font;
                    Debug.Log($"[TestSceneUIBuilder] 대체 폰트 사용: {defaultFont.name}");
                    return;
                }
            }
        }
    }

    private void SetupSceneReferences()
    {
        if (wallTransform != null && expCollector != null)
        {
            Enemy.SetSceneReferences(wallTransform, expCollector);
        }

        if (poolManager != null && passiveSkillManager != null)
        {
            Unit.SetSceneReferences(poolManager, passiveSkillManager);
        }
    }
    
    private void BuildUI()
    {
        if (rootCanvas == null)
        {
            rootCanvas = FindObjectOfType<Canvas>();
        }

        // 메인 설정 패널 (왼쪽 상단 앵커)
        settingsPanel = CreatePanel("SettingsPanel", new Vector2(20, -20), new Vector2(380, 800),
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));

        // 몬스터 스폰 섹션
        CreateMonsterSpawnSection(settingsPanel.transform);

        // 웨이브 설정 섹션
        CreateWaveSettingsSection(settingsPanel.transform);

        // 패시브 스킬 섹션
        CreatePassiveSkillSection(settingsPanel.transform);

        // 스킬 패널 (오른쪽 상단 앵커) - 높이 증가 (스킬 정보 표시 추가)
        skillPanel = CreatePanel("SkillPanel", new Vector2(-20, -20), new Vector2(350, 550),
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1));
        CreatePlayerSkillSection(skillPanel.transform);

        // 유닛 스탯 패널 (하단 왼쪽 앵커) - 높이 증가 (카드 배치 섹션 추가)
        unitStatPanel = CreatePanel("UnitStatPanel", new Vector2(20, 20), new Vector2(400, 700),
            new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0));
        CreateUnitStatSection(unitStatPanel.transform);

        // 몬스터 스탯 패널 (하단 오른쪽 앵커)
        monsterStatPanel = CreatePanel("MonsterStatPanel", new Vector2(-20, 20), new Vector2(400, 350),
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0));
        CreateMonsterStatSection(monsterStatPanel.transform);

        // 상태 효과 테스트 패널 (중앙 오른쪽)
        statusEffectPanel = CreatePanel("StatusEffectPanel", new Vector2(-20, -450), new Vector2(350, 380),
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1));
        CreateStatusEffectSection(statusEffectPanel.transform);

        // 토글 버튼 생성
        CreateToggleButtons();

        // 기본적으로 패널들 숨기기 (토글 버튼으로 켜고 끔)
        settingsPanel.SetActive(false);
        skillPanel.SetActive(false);
        unitStatPanel.SetActive(false);
        monsterStatPanel.SetActive(false);
        statusEffectPanel.SetActive(false);
    }
    
    private GameObject CreatePanel(string name, Vector2 anchoredPos, Vector2 size,
        Vector2? anchorMin = null, Vector2? anchorMax = null, Vector2? pivot = null)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(rootCanvas.transform, false);
        panel.layer = LayerMask.NameToLayer("UI");

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin ?? new Vector2(0.5f, 0.5f);
        rect.anchorMax = anchorMax ?? new Vector2(0.5f, 0.5f);
        rect.pivot = pivot ?? new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
        
        Image img = panel.AddComponent<Image>();
        img.color = panelColor;
        
        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        
        ContentSizeFitter fitter = panel.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        return panel;
    }
    
    private void CreateMonsterSpawnSection(Transform parent)
    {
        // 섹션 헤더
        CreateLabel(parent, "=== 몬스터 스폰 ===");
        
        // 컨트롤러 오브젝트 생성
        GameObject controllerObj = new GameObject("MonsterSpawnController");
        controllerObj.transform.SetParent(parent, false);
        monsterSpawnController = controllerObj.AddComponent<TestMonsterSpawnController>();
        
        // 드롭다운
        GameObject dropdownObj = CreateDropdown(parent, "MonsterDropdown");
        var dropdown = dropdownObj.GetComponent<TMP_Dropdown>();
        
        // 보스 토글
        GameObject toggleObj = CreateToggle(parent, "보스로 스폰");
        var toggle = toggleObj.GetComponent<Toggle>();
        
        // 스폰 개수 입력
        GameObject countObj = CreateInputField(parent, "스폰 개수", "1");
        var countInput = countObj.GetComponentInChildren<TMP_InputField>();
        
        // 스폰 버튼
        GameObject spawnBtnObj = CreateButton(parent, "스폰", buttonColor);
        var spawnBtn = spawnBtnObj.GetComponent<Button>();
        
        // Kill All 버튼
        GameObject killBtnObj = CreateButton(parent, "모두 제거", new Color(0.8f, 0.2f, 0.2f, 1f));
        var killBtn = killBtnObj.GetComponent<Button>();
        
        // 리플렉션으로 필드 설정
        SetPrivateField(monsterSpawnController, "monsterDropdown", dropdown);
        SetPrivateField(monsterSpawnController, "bossToggle", toggle);
        SetPrivateField(monsterSpawnController, "spawnCountInput", countInput);
        SetPrivateField(monsterSpawnController, "spawnButton", spawnBtn);
        SetPrivateField(monsterSpawnController, "killAllButton", killBtn);
        SetPrivateField(monsterSpawnController, "spawnPoint", spawnPoint);
        
        monsterSpawnController.Initialize(poolManager);
    }
    
    private void CreateWaveSettingsSection(Transform parent)
    {
        CreateLabel(parent, "=== 웨이브 설정 ===");

        GameObject controllerObj = new GameObject("WaveSettingsController");
        controllerObj.transform.SetParent(parent, false);
        waveSettingsController = controllerObj.AddComponent<TestWaveSettingsController>();

        // 스테이지 드롭다운
        GameObject stageDropdownObj = CreateDropdown(parent, "StageDropdown");
        var stageDropdown = stageDropdownObj.GetComponent<TMP_Dropdown>();

        // 웨이브 슬라이더
        GameObject sliderObj = CreateSlider(parent, "Wave", 1, 10, 1);
        var slider = sliderObj.GetComponentInChildren<Slider>();
        var valueText = sliderObj.transform.Find("LabelContainer/ValueText")?.GetComponent<TMP_Text>();

        // HP 배율 입력
        GameObject hpObj = CreateInputField(parent, "HP 배율", "1.0");
        var hpInput = hpObj.GetComponentInChildren<TMP_InputField>();

        // EXP 배율 입력
        GameObject expObj = CreateInputField(parent, "EXP 배율", "1.0");
        var expInput = expObj.GetComponentInChildren<TMP_InputField>();

        SetPrivateField(waveSettingsController, "stageDropdown", stageDropdown);
        SetPrivateField(waveSettingsController, "waveSlider", slider);
        SetPrivateField(waveSettingsController, "waveValueText", valueText);
        SetPrivateField(waveSettingsController, "hpMultiplierInput", hpInput);
        SetPrivateField(waveSettingsController, "expMultiplierInput", expInput);

        waveSettingsController.Initialize();
    }
    
    private void CreatePassiveSkillSection(Transform parent)
    {
        CreateLabel(parent, "=== 패시브 스킬 ===");

        GameObject controllerObj = new GameObject("PassiveSkillController");
        controllerObj.transform.SetParent(parent, false);
        passiveSkillController = controllerObj.AddComponent<TestPassiveSkillController>();

        // 상태 텍스트 (흰색 배경 패널에 표시)
        GameObject statusPanelObj = new GameObject("StatusPanel");
        statusPanelObj.transform.SetParent(parent, false);
        statusPanelObj.layer = LayerMask.NameToLayer("UI");

        RectTransform statusPanelRect = statusPanelObj.AddComponent<RectTransform>();
        statusPanelRect.sizeDelta = new Vector2(360, 120);

        Image statusPanelImg = statusPanelObj.AddComponent<Image>();
        statusPanelImg.color = new Color(0.15f, 0.15f, 0.15f, 0.95f); // 어두운 배경

        GameObject statusObj = CreateLabel(statusPanelObj.transform, "패시브 효과:");
        var statusText = statusObj.GetComponent<TMP_Text>();
        statusText.color = Color.white; // 흰색 텍스트
        statusText.alignment = TextAlignmentOptions.TopLeft;

        RectTransform statusRect = statusObj.GetComponent<RectTransform>();
        statusRect.anchorMin = Vector2.zero;
        statusRect.anchorMax = Vector2.one;
        statusRect.offsetMin = new Vector2(10, 10);
        statusRect.offsetMax = new Vector2(-10, -10);

        // 리셋 버튼
        GameObject resetBtnObj = CreateButton(parent, "패시브 리셋", new Color(0.6f, 0.3f, 0.3f, 1f));
        var resetBtn = resetBtnObj.GetComponent<Button>();

        SetPrivateField(passiveSkillController, "statusText", statusText);
        SetPrivateField(passiveSkillController, "resetAllButton", resetBtn);

        passiveSkillController.Initialize(passiveSkillManager);
    }
    
    private void CreatePlayerSkillSection(Transform parent)
    {
        CreateLabel(parent, "=== 플레이어 스킬 ===");

        GameObject controllerObj = new GameObject("PlayerSkillController");
        controllerObj.transform.SetParent(parent, false);
        playerSkillController = controllerObj.AddComponent<TestPlayerSkillController>();

        // 스킬 드롭다운
        GameObject dropdownObj = CreateDropdown(parent, "SkillDropdown");
        var dropdown = dropdownObj.GetComponent<TMP_Dropdown>();

        // 스킬 정보 표시 패널 (어두운 배경)
        GameObject infoPanelObj = new GameObject("SkillInfoPanel");
        infoPanelObj.transform.SetParent(parent, false);
        infoPanelObj.layer = LayerMask.NameToLayer("UI");

        RectTransform infoPanelRect = infoPanelObj.AddComponent<RectTransform>();
        infoPanelRect.sizeDelta = new Vector2(330, 180);

        Image infoPanelImg = infoPanelObj.AddComponent<Image>();
        infoPanelImg.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

        GameObject infoObj = CreateLabel(infoPanelObj.transform, "스킬 정보");
        var infoText = infoObj.GetComponent<TMP_Text>();
        infoText.color = Color.white;
        infoText.alignment = TextAlignmentOptions.TopLeft;
        infoText.fontSize = fontSize - 4;
        infoText.richText = true;

        RectTransform infoRect = infoObj.GetComponent<RectTransform>();
        infoRect.anchorMin = Vector2.zero;
        infoRect.anchorMax = Vector2.one;
        infoRect.offsetMin = new Vector2(10, 10);
        infoRect.offsetMax = new Vector2(-10, -10);

        // 쿨다운 무시 토글
        GameObject noCdToggle = CreateToggle(parent, "쿨다운 무시");
        var toggle = noCdToggle.GetComponent<Toggle>();

        // 스킬 사용 버튼
        GameObject useBtnObj = CreateButton(parent, "스킬 사용", new Color(0.3f, 0.7f, 0.3f, 1f));
        var useBtn = useBtnObj.GetComponent<Button>();

        // 쿨다운 리셋 버튼
        GameObject resetCdBtnObj = CreateButton(parent, "쿨다운 리셋", new Color(0.5f, 0.5f, 0.5f, 1f));
        var resetCdBtn = resetCdBtnObj.GetComponent<Button>();
        resetCdBtn.onClick.AddListener(() => playerSkillController.ResetAllCooldowns());

        SetPrivateField(playerSkillController, "skillDropdown", dropdown);
        SetPrivateField(playerSkillController, "skillInfoText", infoText);
        SetPrivateField(playerSkillController, "noCooldownToggle", toggle);
        SetPrivateField(playerSkillController, "useSkillButton", useBtn);
        SetPrivateField(playerSkillController, "skillSpawnPoint", skillSpawnPoint);

        playerSkillController.Initialize();
    }
    
    private void CreateUnitStatSection(Transform parent)
    {
        CreateLabel(parent, "=== 유닛 스탯 ===");

        GameObject controllerObj = new GameObject("UnitStatEditor");
        controllerObj.transform.SetParent(parent, false);
        unitStatEditor = controllerObj.AddComponent<TestUnitStatEditor>();

        // 유닛 드롭다운
        GameObject dropdownObj = CreateDropdown(parent, "UnitDropdown");
        var dropdown = dropdownObj.GetComponent<TMP_Dropdown>();

        // 리프레시 버튼
        GameObject refreshBtnObj = CreateButton(parent, "유닛 갱신", new Color(0.5f, 0.5f, 0.5f, 1f));
        var refreshBtn = refreshBtnObj.GetComponent<Button>();

        // 유닛 정보 텍스트 (어두운 배경)
        GameObject infoPanelObj = new GameObject("InfoPanel");
        infoPanelObj.transform.SetParent(parent, false);
        infoPanelObj.layer = LayerMask.NameToLayer("UI");

        RectTransform infoPanelRect = infoPanelObj.AddComponent<RectTransform>();
        infoPanelRect.sizeDelta = new Vector2(380, 130);

        Image infoPanelImg = infoPanelObj.AddComponent<Image>();
        infoPanelImg.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

        GameObject infoObj = CreateLabel(infoPanelObj.transform, "유닛 정보");
        var infoText = infoObj.GetComponent<TMP_Text>();
        infoText.color = Color.white;
        infoText.alignment = TextAlignmentOptions.TopLeft;
        infoText.fontSize = fontSize - 4;

        RectTransform infoRect = infoObj.GetComponent<RectTransform>();
        infoRect.anchorMin = Vector2.zero;
        infoRect.anchorMax = Vector2.one;
        infoRect.offsetMin = new Vector2(10, 5);
        infoRect.offsetMax = new Vector2(-10, -5);

        // 스탯 입력 필드들
        GameObject atkObj = CreateInputField(parent, "공격력", "100");
        var atkInput = atkObj.GetComponentInChildren<TMP_InputField>();

        GameObject cooltimeObj = CreateInputField(parent, "쿨타임", "1.0");
        var cooltimeInput = cooltimeObj.GetComponentInChildren<TMP_InputField>();

        GameObject critRateObj = CreateInputField(parent, "치명타율", "10");
        var critRateInput = critRateObj.GetComponentInChildren<TMP_InputField>();

        GameObject critDmgObj = CreateInputField(parent, "치명타뎀", "1.5");
        var critDmgInput = critDmgObj.GetComponentInChildren<TMP_InputField>();

        // 버튼 컨테이너 (선택 적용)
        GameObject btnContainer = new GameObject("ButtonContainer");
        btnContainer.transform.SetParent(parent, false);
        btnContainer.layer = LayerMask.NameToLayer("UI");

        RectTransform btnContainerRect = btnContainer.AddComponent<RectTransform>();
        btnContainerRect.sizeDelta = new Vector2(380, 50);

        HorizontalLayoutGroup btnLayout = btnContainer.AddComponent<HorizontalLayoutGroup>();
        btnLayout.spacing = 10;
        btnLayout.childControlWidth = true;
        btnLayout.childForceExpandWidth = true;

        // 씬 유닛 적용 버튼
        GameObject applyBtnObj = CreateButton(btnContainer.transform, "씬 적용", buttonColor);
        var applyBtn = applyBtnObj.GetComponent<Button>();

        // DataTable/CSV 버튼 컨테이너
        GameObject dataTableBtnContainer = new GameObject("DataTableButtonContainer");
        dataTableBtnContainer.transform.SetParent(parent, false);
        dataTableBtnContainer.layer = LayerMask.NameToLayer("UI");

        RectTransform dataTableBtnContainerRect = dataTableBtnContainer.AddComponent<RectTransform>();
        dataTableBtnContainerRect.sizeDelta = new Vector2(380, 50);

        HorizontalLayoutGroup dataTableBtnLayout = dataTableBtnContainer.AddComponent<HorizontalLayoutGroup>();
        dataTableBtnLayout.spacing = 10;
        dataTableBtnLayout.childControlWidth = true;
        dataTableBtnLayout.childForceExpandWidth = true;

        // DataTable 적용 버튼
        GameObject applyToDataTableBtnObj = CreateButton(dataTableBtnContainer.transform, "테이블 적용", new Color(0.5f, 0.3f, 0.7f, 1f));
        var applyToDataTableBtn = applyToDataTableBtnObj.GetComponent<Button>();

        // DataTable 리셋 버튼
        GameObject resetDataTableBtnObj = CreateButton(dataTableBtnContainer.transform, "테이블 리셋", new Color(0.6f, 0.3f, 0.3f, 1f));
        var resetDataTableBtn = resetDataTableBtnObj.GetComponent<Button>();

        // CSV 저장 버튼
        GameObject saveToCsvBtnObj = CreateButton(parent, "CSV 파일 저장", new Color(0.3f, 0.6f, 0.3f, 1f));
        var saveToCsvBtn = saveToCsvBtnObj.GetComponent<Button>();

        // === 유닛 슬롯 배치 섹션 ===
        // 유닛 슬롯 생성 버튼
        GameObject spawnSlotBtnObj = CreateButton(parent, "유닛 슬롯 생성", new Color(0.4f, 0.6f, 0.8f, 1f));
        var spawnSlotBtn = spawnSlotBtnObj.GetComponent<Button>();

        // 유닛 슬롯 컨테이너 (생성된 슬롯이 여기에 표시됨)
        GameObject slotContainerObj = new GameObject("UnitSlotContainer");
        slotContainerObj.transform.SetParent(parent, false);
        slotContainerObj.layer = LayerMask.NameToLayer("UI");

        RectTransform slotContainerRect = slotContainerObj.AddComponent<RectTransform>();
        slotContainerRect.sizeDelta = new Vector2(380, 120);

        Image slotContainerBg = slotContainerObj.AddComponent<Image>();
        slotContainerBg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        HorizontalLayoutGroup slotLayout = slotContainerObj.AddComponent<HorizontalLayoutGroup>();
        slotLayout.padding = new RectOffset(10, 10, 10, 10);
        slotLayout.spacing = 10;
        slotLayout.childAlignment = TextAnchor.MiddleCenter;
        slotLayout.childControlWidth = false;
        slotLayout.childControlHeight = false;

        SetPrivateField(unitStatEditor, "unitDropdown", dropdown);
        SetPrivateField(unitStatEditor, "refreshButton", refreshBtn);
        SetPrivateField(unitStatEditor, "selectedUnitInfoText", infoText);
        SetPrivateField(unitStatEditor, "attackDamageInput", atkInput);
        SetPrivateField(unitStatEditor, "attackCooltimeInput", cooltimeInput);
        SetPrivateField(unitStatEditor, "critRateInput", critRateInput);
        SetPrivateField(unitStatEditor, "critDamageInput", critDmgInput);
        SetPrivateField(unitStatEditor, "applyButton", applyBtn);
        SetPrivateField(unitStatEditor, "applyToDataTableButton", applyToDataTableBtn);
        SetPrivateField(unitStatEditor, "resetDataTableButton", resetDataTableBtn);
        SetPrivateField(unitStatEditor, "saveToCsvButton", saveToCsvBtn);
        SetPrivateField(unitStatEditor, "spawnUnitSlotButton", spawnSlotBtn);
        SetPrivateField(unitStatEditor, "unitSlotContainer", slotContainerObj.transform);

        // TestSlot 프리팹은 TestUnitCreator.Instance를 통해 사용하므로 별도 로드 불필요

        unitStatEditor.Initialize();
    }
    
    private void CreateMonsterStatSection(Transform parent)
    {
        CreateLabel(parent, "=== 몬스터 스탯 ===");

        GameObject controllerObj = new GameObject("MonsterStatEditor");
        controllerObj.transform.SetParent(parent, false);
        monsterStatEditor = controllerObj.AddComponent<TestMonsterStatEditor>();

        // 몬스터 드롭다운
        GameObject dropdownObj = CreateDropdown(parent, "MonsterDropdown");
        var dropdown = dropdownObj.GetComponent<TMP_Dropdown>();

        // 리프레시 버튼
        GameObject refreshBtnObj = CreateButton(parent, "몬스터 갱신", new Color(0.5f, 0.5f, 0.5f, 1f));
        var refreshBtn = refreshBtnObj.GetComponent<Button>();

        // 몬스터 정보 텍스트 (어두운 배경)
        GameObject infoPanelObj = new GameObject("InfoPanel");
        infoPanelObj.transform.SetParent(parent, false);
        infoPanelObj.layer = LayerMask.NameToLayer("UI");

        RectTransform infoPanelRect = infoPanelObj.AddComponent<RectTransform>();
        infoPanelRect.sizeDelta = new Vector2(380, 100);

        Image infoPanelImg = infoPanelObj.AddComponent<Image>();
        infoPanelImg.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

        GameObject infoObj = CreateLabel(infoPanelObj.transform, "몬스터 정보");
        var infoText = infoObj.GetComponent<TMP_Text>();
        infoText.color = Color.white;
        infoText.alignment = TextAlignmentOptions.TopLeft;
        infoText.fontSize = fontSize - 4;

        RectTransform infoRect = infoObj.GetComponent<RectTransform>();
        infoRect.anchorMin = Vector2.zero;
        infoRect.anchorMax = Vector2.one;
        infoRect.offsetMin = new Vector2(10, 5);
        infoRect.offsetMax = new Vector2(-10, -5);

        // 스탯 입력 필드들
        GameObject hpObj = CreateInputField(parent, "HP", "100");
        var hpInput = hpObj.GetComponentInChildren<TMP_InputField>();

        GameObject speedObj = CreateInputField(parent, "이동속도", "1.0");
        var speedInput = speedObj.GetComponentInChildren<TMP_InputField>();

        GameObject expObj = CreateInputField(parent, "경험치", "10");
        var expInput = expObj.GetComponentInChildren<TMP_InputField>();

        // 버튼 컨테이너
        GameObject btnContainer = new GameObject("ButtonContainer");
        btnContainer.transform.SetParent(parent, false);
        btnContainer.layer = LayerMask.NameToLayer("UI");

        RectTransform btnContainerRect = btnContainer.AddComponent<RectTransform>();
        btnContainerRect.sizeDelta = new Vector2(380, 50);

        HorizontalLayoutGroup btnLayout = btnContainer.AddComponent<HorizontalLayoutGroup>();
        btnLayout.spacing = 10;
        btnLayout.childControlWidth = true;
        btnLayout.childForceExpandWidth = true;

        // 선택 적용 버튼
        GameObject applyBtnObj = CreateButton(btnContainer.transform, "선택 적용", buttonColor);
        var applyBtn = applyBtnObj.GetComponent<Button>();

        // 전체 적용 버튼
        GameObject applyAllBtnObj = CreateButton(btnContainer.transform, "전체 적용", new Color(0.7f, 0.4f, 0.2f, 1f));
        var applyAllBtn = applyAllBtnObj.GetComponent<Button>();

        // DataTable/CSV 버튼 컨테이너
        GameObject dataTableBtnContainer = new GameObject("DataTableButtonContainer");
        dataTableBtnContainer.transform.SetParent(parent, false);
        dataTableBtnContainer.layer = LayerMask.NameToLayer("UI");

        RectTransform dataTableBtnContainerRect = dataTableBtnContainer.AddComponent<RectTransform>();
        dataTableBtnContainerRect.sizeDelta = new Vector2(380, 50);

        HorizontalLayoutGroup dataTableBtnLayout = dataTableBtnContainer.AddComponent<HorizontalLayoutGroup>();
        dataTableBtnLayout.spacing = 10;
        dataTableBtnLayout.childControlWidth = true;
        dataTableBtnLayout.childForceExpandWidth = true;

        // DataTable 적용 버튼
        GameObject applyToDataTableBtnObj = CreateButton(dataTableBtnContainer.transform, "테이블 적용", new Color(0.5f, 0.3f, 0.7f, 1f));
        var applyToDataTableBtn = applyToDataTableBtnObj.GetComponent<Button>();

        // DataTable 리셋 버튼
        GameObject resetDataTableBtnObj = CreateButton(dataTableBtnContainer.transform, "테이블 리셋", new Color(0.6f, 0.3f, 0.3f, 1f));
        var resetDataTableBtn = resetDataTableBtnObj.GetComponent<Button>();

        // CSV 저장 버튼
        GameObject saveToCsvBtnObj = CreateButton(parent, "CSV 파일 저장", new Color(0.3f, 0.6f, 0.3f, 1f));
        var saveToCsvBtn = saveToCsvBtnObj.GetComponent<Button>();

        SetPrivateField(monsterStatEditor, "monsterDropdown", dropdown);
        SetPrivateField(monsterStatEditor, "refreshButton", refreshBtn);
        SetPrivateField(monsterStatEditor, "selectedMonsterInfoText", infoText);
        SetPrivateField(monsterStatEditor, "hpInput", hpInput);
        SetPrivateField(monsterStatEditor, "speedInput", speedInput);
        SetPrivateField(monsterStatEditor, "expInput", expInput);
        SetPrivateField(monsterStatEditor, "applyButton", applyBtn);
        SetPrivateField(monsterStatEditor, "applyToAllButton", applyAllBtn);
        SetPrivateField(monsterStatEditor, "applyToDataTableButton", applyToDataTableBtn);
        SetPrivateField(monsterStatEditor, "resetDataTableButton", resetDataTableBtn);
        SetPrivateField(monsterStatEditor, "saveToCsvButton", saveToCsvBtn);

        monsterStatEditor.Initialize();
    }

    private void CreateStatusEffectSection(Transform parent)
    {
        CreateLabel(parent, "=== 상태 효과 테스트 ===");

        GameObject controllerObj = new GameObject("StatusEffectController");
        controllerObj.transform.SetParent(parent, false);
        statusEffectController = controllerObj.AddComponent<TestStatusEffectController>();

        // 효과 타입 드롭다운
        GameObject dropdownObj = CreateDropdown(parent, "EffectDropdown");
        var dropdown = dropdownObj.GetComponent<TMP_Dropdown>();

        // 지속 시간 입력
        GameObject durationObj = CreateInputField(parent, "지속 시간", "5.0");
        var durationInput = durationObj.GetComponentInChildren<TMP_InputField>();

        // 효과 값 입력
        GameObject valueObj = CreateInputField(parent, "효과 값", "50");
        var valueInput = valueObj.GetComponentInChildren<TMP_InputField>();

        // 전체 적용 토글
        GameObject applyAllToggle = CreateToggle(parent, "모든 몬스터에 적용");
        var toggle = applyAllToggle.GetComponent<Toggle>();

        // 버튼 컨테이너
        GameObject btnContainer = new GameObject("ButtonContainer");
        btnContainer.transform.SetParent(parent, false);
        btnContainer.layer = LayerMask.NameToLayer("UI");

        RectTransform btnContainerRect = btnContainer.AddComponent<RectTransform>();
        btnContainerRect.sizeDelta = new Vector2(330, 50);

        HorizontalLayoutGroup btnLayout = btnContainer.AddComponent<HorizontalLayoutGroup>();
        btnLayout.spacing = 10;
        btnLayout.childControlWidth = true;
        btnLayout.childForceExpandWidth = true;

        // 효과 적용 버튼
        GameObject applyBtnObj = CreateButton(btnContainer.transform, "효과 적용", new Color(0.3f, 0.6f, 0.3f, 1f));
        var applyBtn = applyBtnObj.GetComponent<Button>();

        // 효과 제거 버튼
        GameObject clearBtnObj = CreateButton(btnContainer.transform, "효과 제거", new Color(0.6f, 0.3f, 0.3f, 1f));
        var clearBtn = clearBtnObj.GetComponent<Button>();

        // 단축키 안내
        CreateLabel(parent, "단축키: 1~6 효과적용, C 제거");

        SetPrivateField(statusEffectController, "effectDropdown", dropdown);
        SetPrivateField(statusEffectController, "durationInput", durationInput);
        SetPrivateField(statusEffectController, "valueInput", valueInput);
        SetPrivateField(statusEffectController, "applyToAllToggle", toggle);
        SetPrivateField(statusEffectController, "applyEffectButton", applyBtn);
        SetPrivateField(statusEffectController, "clearEffectsButton", clearBtn);

        statusEffectController.Initialize();
    }

    private void CreateToggleButtons()
    {
        // 설정 패널 토글
        GameObject toggleBtnsPanel = new GameObject("ToggleButtons");
        toggleBtnsPanel.transform.SetParent(rootCanvas.transform, false);
        toggleBtnsPanel.layer = LayerMask.NameToLayer("UI");

        RectTransform rect = toggleBtnsPanel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(-20, -20);
        rect.sizeDelta = new Vector2(150, 280);

        VerticalLayoutGroup layout = toggleBtnsPanel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 5;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        CreateToggleButton(toggleBtnsPanel.transform, "설정", settingsPanel);
        CreateToggleButton(toggleBtnsPanel.transform, "스킬", skillPanel);
        CreateToggleButton(toggleBtnsPanel.transform, "유닛", unitStatPanel);
        CreateToggleButton(toggleBtnsPanel.transform, "몬스터", monsterStatPanel);
        CreateToggleButton(toggleBtnsPanel.transform, "상태효과", statusEffectPanel);
    }
    
    private void CreateToggleButton(Transform parent, string label, GameObject targetPanel)
    {
        GameObject btnObj = CreateButton(parent, label, new Color(0.3f, 0.3f, 0.3f, 0.9f));
        btnObj.GetComponent<Button>().onClick.AddListener(() => {
            targetPanel.SetActive(!targetPanel.activeSelf);
        });

        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(130, 50);
    }
    
    // UI 요소 생성 헬퍼 메서드들
    private GameObject CreateLabel(Transform parent, string text)
    {
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(parent, false);
        labelObj.layer = LayerMask.NameToLayer("UI");

        RectTransform rect = labelObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(380, 45);

        TextMeshProUGUI tmp = labelObj.AddComponent<TextMeshProUGUI>();
        if (defaultFont != null) tmp.font = defaultFont;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white; // 어두운 패널 배경에서 보이도록 흰색

        return labelObj;
    }
    
    private GameObject CreateButton(Transform parent, string text, Color color)
    {
        GameObject btnObj = new GameObject("Button_" + text);
        btnObj.transform.SetParent(parent, false);
        btnObj.layer = LayerMask.NameToLayer("UI");

        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(380, 50);

        Image img = btnObj.AddComponent<Image>();
        img.color = color;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;

        // 버튼 텍스트
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        textObj.layer = LayerMask.NameToLayer("UI");

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        if (defaultFont != null) tmp.font = defaultFont;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;  // 버튼 텍스트는 흰색 유지 (배경색과 대비)

        return btnObj;
    }

    private GameObject CreateDropdown(Transform parent, string name)
    {
        GameObject dropdownObj = new GameObject(name);
        dropdownObj.transform.SetParent(parent, false);
        dropdownObj.layer = LayerMask.NameToLayer("UI");

        RectTransform rect = dropdownObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(380, 50);

        Image img = dropdownObj.AddComponent<Image>();
        img.color = new Color(0.85f, 0.85f, 0.85f, 1f);

        TMP_Dropdown dropdown = dropdownObj.AddComponent<TMP_Dropdown>();

        // 캡션 텍스트
        GameObject captionObj = new GameObject("Label");
        captionObj.transform.SetParent(dropdownObj.transform, false);
        captionObj.layer = LayerMask.NameToLayer("UI");

        RectTransform captionRect = captionObj.AddComponent<RectTransform>();
        captionRect.anchorMin = Vector2.zero;
        captionRect.anchorMax = Vector2.one;
        captionRect.offsetMin = new Vector2(10, 0);
        captionRect.offsetMax = new Vector2(-30, 0);

        TextMeshProUGUI captionText = captionObj.AddComponent<TextMeshProUGUI>();
        if (defaultFont != null) captionText.font = defaultFont;
        captionText.fontSize = fontSize - 2;
        captionText.alignment = TextAlignmentOptions.MidlineLeft;
        captionText.color = textColor;

        dropdown.captionText = captionText;
        
        // 템플릿 (간소화)
        GameObject templateObj = new GameObject("Template");
        templateObj.transform.SetParent(dropdownObj.transform, false);
        templateObj.layer = LayerMask.NameToLayer("UI");
        templateObj.SetActive(false);
        
        RectTransform templateRect = templateObj.AddComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0, 0);
        templateRect.anchorMax = new Vector2(1, 0);
        templateRect.pivot = new Vector2(0.5f, 1);
        templateRect.anchoredPosition = Vector2.zero;
        templateRect.sizeDelta = new Vector2(0, 200);
        
        Image templateImg = templateObj.AddComponent<Image>();
        templateImg.color = new Color(0.9f, 0.9f, 0.9f, 1f); // 밝은 배경
        
        ScrollRect scrollRect = templateObj.AddComponent<ScrollRect>();
        
        // Viewport
        GameObject viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(templateObj.transform, false);
        viewportObj.layer = LayerMask.NameToLayer("UI");
        
        RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        
        viewportObj.AddComponent<Mask>();
        Image viewportImg = viewportObj.AddComponent<Image>();
        viewportImg.color = Color.white;
        
        // Content
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewportObj.transform, false);
        contentObj.layer = LayerMask.NameToLayer("UI");

        RectTransform contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0, 35);

        // Content에 VerticalLayoutGroup 추가 (아이템들 수직 정렬)
        VerticalLayoutGroup contentLayout = contentObj.AddComponent<VerticalLayoutGroup>();
        contentLayout.childAlignment = TextAnchor.UpperCenter;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.spacing = 2;
        contentLayout.padding = new RectOffset(2, 2, 2, 2);

        // ContentSizeFitter 추가 (내용에 맞게 크기 조절)
        ContentSizeFitter contentFitter = contentObj.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Item
        GameObject itemObj = new GameObject("Item");
        itemObj.transform.SetParent(contentObj.transform, false);
        itemObj.layer = LayerMask.NameToLayer("UI");

        RectTransform itemRect = itemObj.AddComponent<RectTransform>();
        itemRect.anchorMin = new Vector2(0, 0.5f);
        itemRect.anchorMax = new Vector2(1, 0.5f);
        itemRect.sizeDelta = new Vector2(0, 40);

        // LayoutElement 추가 (아이템 높이 고정)
        LayoutElement itemLayoutElement = itemObj.AddComponent<LayoutElement>();
        itemLayoutElement.minHeight = 40;
        itemLayoutElement.preferredHeight = 40;

        // 아이템 배경 이미지
        Image itemBg = itemObj.AddComponent<Image>();
        itemBg.color = new Color(0.95f, 0.95f, 0.95f, 1f);

        Toggle itemToggle = itemObj.AddComponent<Toggle>();
        itemToggle.targetGraphic = itemBg;

        // 체크마크 (선택 표시)
        GameObject checkmarkObj = new GameObject("Item Checkmark");
        checkmarkObj.transform.SetParent(itemObj.transform, false);
        checkmarkObj.layer = LayerMask.NameToLayer("UI");

        RectTransform checkmarkRect = checkmarkObj.AddComponent<RectTransform>();
        checkmarkRect.anchorMin = Vector2.zero;
        checkmarkRect.anchorMax = Vector2.one;
        checkmarkRect.offsetMin = Vector2.zero;
        checkmarkRect.offsetMax = Vector2.zero;

        Image checkmarkImg = checkmarkObj.AddComponent<Image>();
        checkmarkImg.color = new Color(0.3f, 0.6f, 0.9f, 0.3f); // 선택 시 파란색 하이라이트

        itemToggle.graphic = checkmarkImg;

        // Item Label
        GameObject itemLabelObj = new GameObject("Item Label");
        itemLabelObj.transform.SetParent(itemObj.transform, false);
        itemLabelObj.layer = LayerMask.NameToLayer("UI");

        RectTransform itemLabelRect = itemLabelObj.AddComponent<RectTransform>();
        itemLabelRect.anchorMin = Vector2.zero;
        itemLabelRect.anchorMax = Vector2.one;
        itemLabelRect.offsetMin = new Vector2(10, 0);
        itemLabelRect.offsetMax = new Vector2(-10, 0);

        TextMeshProUGUI itemLabelText = itemLabelObj.AddComponent<TextMeshProUGUI>();
        if (defaultFont != null) itemLabelText.font = defaultFont;
        itemLabelText.fontSize = fontSize - 2;
        itemLabelText.alignment = TextAlignmentOptions.MidlineLeft;
        itemLabelText.color = Color.black; // 드롭다운 아이템은 밝은 배경이므로 검정색

        dropdown.template = templateRect;
        dropdown.itemText = itemLabelText;
        
        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        
        return dropdownObj;
    }
    
    private GameObject CreateToggle(Transform parent, string label)
    {
        GameObject toggleObj = new GameObject("Toggle_" + label);
        toggleObj.transform.SetParent(parent, false);
        toggleObj.layer = LayerMask.NameToLayer("UI");

        RectTransform rect = toggleObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(380, 45);

        HorizontalLayoutGroup layout = toggleObj.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 15;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;

        Toggle toggle = toggleObj.AddComponent<Toggle>();

        // 체크박스 배경
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(toggleObj.transform, false);
        bgObj.layer = LayerMask.NameToLayer("UI");

        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(35, 35);

        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.7f, 0.7f, 0.7f, 1f);

        // 체크마크
        GameObject checkObj = new GameObject("Checkmark");
        checkObj.transform.SetParent(bgObj.transform, false);
        checkObj.layer = LayerMask.NameToLayer("UI");

        RectTransform checkRect = checkObj.AddComponent<RectTransform>();
        checkRect.anchorMin = Vector2.zero;
        checkRect.anchorMax = Vector2.one;
        checkRect.offsetMin = new Vector2(4, 4);
        checkRect.offsetMax = new Vector2(-4, -4);

        Image checkImg = checkObj.AddComponent<Image>();
        checkImg.color = buttonColor;

        toggle.targetGraphic = bgImg;
        toggle.graphic = checkImg;
        toggle.isOn = false;

        // 라벨
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(toggleObj.transform, false);
        labelObj.layer = LayerMask.NameToLayer("UI");

        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(300, 40);

        TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
        if (defaultFont != null) labelText.font = defaultFont;
        labelText.text = label;
        labelText.fontSize = fontSize;
        labelText.alignment = TextAlignmentOptions.MidlineLeft;
        labelText.color = Color.white; // 어두운 배경에서 보이도록 흰색

        return toggleObj;
    }

    private GameObject CreateInputField(Transform parent, string label, string defaultValue)
    {
        GameObject containerObj = new GameObject("InputField_" + label);
        containerObj.transform.SetParent(parent, false);
        containerObj.layer = LayerMask.NameToLayer("UI");

        RectTransform containerRect = containerObj.AddComponent<RectTransform>();
        containerRect.sizeDelta = new Vector2(380, 50);

        HorizontalLayoutGroup layout = containerObj.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 15;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;

        // 라벨
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(containerObj.transform, false);
        labelObj.layer = LayerMask.NameToLayer("UI");

        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(140, 40);

        TextMeshProUGUI labelText2 = labelObj.AddComponent<TextMeshProUGUI>();
        if (defaultFont != null) labelText2.font = defaultFont;
        labelText2.text = label;
        labelText2.fontSize = fontSize - 2;
        labelText2.alignment = TextAlignmentOptions.MidlineLeft;
        labelText2.color = Color.white; // 어두운 배경에서 보이도록 흰색

        // 입력 필드
        GameObject inputObj = new GameObject("InputField");
        inputObj.transform.SetParent(containerObj.transform, false);
        inputObj.layer = LayerMask.NameToLayer("UI");

        RectTransform inputRect = inputObj.AddComponent<RectTransform>();
        inputRect.sizeDelta = new Vector2(200, 40);

        Image inputImg = inputObj.AddComponent<Image>();
        inputImg.color = new Color(0.85f, 0.85f, 0.85f, 1f);

        TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();

        // 텍스트 영역
        GameObject textAreaObj = new GameObject("Text Area");
        textAreaObj.transform.SetParent(inputObj.transform, false);
        textAreaObj.layer = LayerMask.NameToLayer("UI");

        RectTransform textAreaRect = textAreaObj.AddComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.offsetMin = new Vector2(8, 0);
        textAreaRect.offsetMax = new Vector2(-8, 0);

        // 텍스트
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(textAreaObj.transform, false);
        textObj.layer = LayerMask.NameToLayer("UI");

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp2 = textObj.AddComponent<TextMeshProUGUI>();
        if (defaultFont != null) tmp2.font = defaultFont;
        tmp2.fontSize = fontSize - 2;
        tmp2.alignment = TextAlignmentOptions.MidlineLeft;
        tmp2.color = textColor;

        inputField.textViewport = textAreaRect;
        inputField.textComponent = tmp2;
        inputField.text = defaultValue;

        return containerObj;
    }
    
    private GameObject CreateSlider(Transform parent, string label, float min, float max, float defaultVal)
    {
        GameObject containerObj = new GameObject("Slider_" + label);
        containerObj.transform.SetParent(parent, false);
        containerObj.layer = LayerMask.NameToLayer("UI");

        RectTransform containerRect = containerObj.AddComponent<RectTransform>();
        containerRect.sizeDelta = new Vector2(380, 65);

        VerticalLayoutGroup layout = containerObj.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        // 라벨 + 값 표시
        GameObject labelContainerObj = new GameObject("LabelContainer");
        labelContainerObj.transform.SetParent(containerObj.transform, false);
        labelContainerObj.layer = LayerMask.NameToLayer("UI");

        RectTransform labelContainerRect = labelContainerObj.AddComponent<RectTransform>();
        labelContainerRect.sizeDelta = new Vector2(380, 30);

        HorizontalLayoutGroup labelLayout = labelContainerObj.AddComponent<HorizontalLayoutGroup>();
        labelLayout.childControlWidth = true;
        labelLayout.childForceExpandWidth = true;

        // 라벨
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(labelContainerObj.transform, false);
        labelObj.layer = LayerMask.NameToLayer("UI");

        labelObj.AddComponent<RectTransform>();
        TextMeshProUGUI labelText3 = labelObj.AddComponent<TextMeshProUGUI>();
        if (defaultFont != null) labelText3.font = defaultFont;
        labelText3.text = label;
        labelText3.fontSize = fontSize - 2;
        labelText3.alignment = TextAlignmentOptions.MidlineLeft;
        labelText3.color = Color.white; // 어두운 배경에서 보이도록 흰색

        // 값 텍스트
        GameObject valueObj = new GameObject("ValueText");
        valueObj.transform.SetParent(labelContainerObj.transform, false);
        valueObj.layer = LayerMask.NameToLayer("UI");

        valueObj.AddComponent<RectTransform>();
        TextMeshProUGUI valueText = valueObj.AddComponent<TextMeshProUGUI>();
        if (defaultFont != null) valueText.font = defaultFont;
        valueText.text = defaultVal.ToString();
        valueText.fontSize = fontSize - 2;
        valueText.alignment = TextAlignmentOptions.MidlineRight;
        valueText.color = Color.white; // 어두운 배경에서 보이도록 흰색

        // 슬라이더
        GameObject sliderObj = new GameObject("Slider");
        sliderObj.transform.SetParent(containerObj.transform, false);
        sliderObj.layer = LayerMask.NameToLayer("UI");

        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(380, 25);
        
        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = defaultVal;
        slider.wholeNumbers = true;
        
        // 배경
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);
        bgObj.layer = LayerMask.NameToLayer("UI");
        
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 0.25f);
        bgRect.anchorMax = new Vector2(1, 0.75f);
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        
        // Fill Area
        GameObject fillAreaObj = new GameObject("Fill Area");
        fillAreaObj.transform.SetParent(sliderObj.transform, false);
        fillAreaObj.layer = LayerMask.NameToLayer("UI");
        
        RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1, 0.75f);
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;
        
        // Fill
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillAreaObj.transform, false);
        fillObj.layer = LayerMask.NameToLayer("UI");
        
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = buttonColor;
        
        slider.fillRect = fillRect;
        
        // Handle
        GameObject handleAreaObj = new GameObject("Handle Slide Area");
        handleAreaObj.transform.SetParent(sliderObj.transform, false);
        handleAreaObj.layer = LayerMask.NameToLayer("UI");
        
        RectTransform handleAreaRect = handleAreaObj.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(10, 0);
        handleAreaRect.offsetMax = new Vector2(-10, 0);
        
        GameObject handleObj = new GameObject("Handle");
        handleObj.transform.SetParent(handleAreaObj.transform, false);
        handleObj.layer = LayerMask.NameToLayer("UI");
        
        RectTransform handleRect = handleObj.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20, 0);
        
        Image handleImg = handleObj.AddComponent<Image>();
        handleImg.color = Color.white;
        
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImg;
        
        // 값 변경 이벤트
        slider.onValueChanged.AddListener((val) => {
            valueText.text = val.ToString();
        });
        
        return containerObj;
    }
    
    private void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance | 
            System.Reflection.BindingFlags.Public);
        if (field != null)
        {
            field.SetValue(obj, value);
        }
    }
    
    private void OnDestroy()
    {
        Enemy.ClearSceneReferences();
        Unit.ClearSceneReferences();
    }
}
