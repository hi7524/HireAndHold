using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class SkillEffectTestEditor : EditorWindow
{
    private List<UnitDataEntry> unitDataList = new List<UnitDataEntry>();
    private List<SkillDataEntry> skillDataList = new List<SkillDataEntry>();
    private Dictionary<int, string> stringTable = new Dictionary<int, string>();

    private int selectedUnitIndex = 0;
    private int selectedSkillIndex = 0;
    private string[] unitNames = new string[0];
    private string[] skillNames = new string[0];
    private List<SkillDataEntry> currentUnitSkills = new List<SkillDataEntry>();

    private GameObject loadedPrefab;
    private GameObject previewInstance;
    private Vector3 effectScale = Vector3.one;
    private Vector3 originalScale = Vector3.one;
    private float uniformScale = 1f;
    private bool useUniformScale = true;

    private bool scaleChildParticles = true;
    private List<ChildParticleInfo> childParticles = new List<ChildParticleInfo>();
    private bool showChildParticlesFoldout = true;

    private class ChildParticleInfo
    {
        public string name;
        public string path;
        public Vector3 originalScale;
        public Vector3 currentScale;
        public Vector3 originalPosition;
        public Vector3 currentPosition;
        public Vector3 originalRotation;
        public Vector3 currentRotation;
        public ParticleSystem particleSystem;
        public bool isActiveInPrefab;
    }

    // 스크롤
    private Vector2 scrollPosition;

    private bool showRangeGizmo = true;
    private float currentSkillRange = 1f;

    private float editedSkillRange = 1f;
    private bool skillRangeModified = false;
    private List<string[]> skillTableRawData = new List<string[]>();
    private const int SKILL_RANGE_COLUMN_INDEX = 16;

    private Vector3 testSpawnPosition = new Vector3(0, 0, 0);

    [MenuItem("Tools/Skill Effect Test Editor")]
    public static void ShowWindow()
    {
        var window = GetWindow<SkillEffectTestEditor>("Skill Effect Test");
        window.minSize = new Vector2(400, 600);
    }

    private void OnEnable()
    {
        LoadAllTables();
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        CleanupPreview();
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        DrawHeader();
        EditorGUILayout.Space(10);

        DrawTableReloadSection();
        EditorGUILayout.Space(10);

        DrawUnitSelection();
        EditorGUILayout.Space(10);

        DrawSkillSelection();
        EditorGUILayout.Space(10);

        DrawSkillInfo();
        EditorGUILayout.Space(10);

        DrawEffectScaleControls();
        EditorGUILayout.Space(10);

        DrawPreviewControls();
        EditorGUILayout.Space(10);

        DrawSaveControls();
        EditorGUILayout.Space(10);

        DrawPlayModeTestSection();
        EditorGUILayout.Space(10);

        DrawAllSkillsOverview();

        EditorGUILayout.EndScrollView();
    }

    #region UI Drawing

    private void DrawHeader()
    {
        EditorGUILayout.LabelField("Skill Effect Size Test Editor", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "유닛의 스킬 이펙트 크기를 SKILL_RANGE에 맞춰 조정하는 에디터입니다.\n" +
            "1. 유닛 선택 → 2. 스킬 선택 → 3. 스케일 조정 → 4. 저장",
            MessageType.Info);
    }

    private void DrawTableReloadSection()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Data Tables", EditorStyles.boldLabel);
        if (GUILayout.Button("Reload Tables", GUILayout.Width(100)))
        {
            LoadAllTables();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField($"Units: {unitDataList.Count}, Skills: {skillDataList.Count}, Strings: {stringTable.Count}");
    }

    private void DrawUnitSelection()
    {
        EditorGUILayout.LabelField("1. Unit Selection", EditorStyles.boldLabel);

        if (unitNames.Length == 0)
        {
            EditorGUILayout.HelpBox("No units loaded. Click 'Reload Tables'.", MessageType.Warning);
            return;
        }

        int newIndex = EditorGUILayout.Popup("Select Unit", selectedUnitIndex, unitNames);
        if (newIndex != selectedUnitIndex)
        {
            selectedUnitIndex = newIndex;
            OnUnitSelected();
        }

        if (selectedUnitIndex >= 0 && selectedUnitIndex < unitDataList.Count)
        {
            var unit = unitDataList[selectedUnitIndex];
            EditorGUILayout.LabelField($"Unit ID: {unit.UNIT_ID}");
            EditorGUILayout.LabelField($"Skills: {unit.UNIT_SKILL1}, {unit.UNIT_SKILL2}");
        }
    }

    private void DrawSkillSelection()
    {
        EditorGUILayout.LabelField("2. Skill Selection", EditorStyles.boldLabel);

        if (currentUnitSkills.Count == 0)
        {
            EditorGUILayout.HelpBox("Select a unit first or unit has no skills.", MessageType.Info);
            return;
        }

        int newIndex = EditorGUILayout.Popup("Select Skill", selectedSkillIndex, skillNames);
        if (newIndex != selectedSkillIndex)
        {
            selectedSkillIndex = newIndex;
            OnSkillSelected();
        }
    }

    private void DrawSkillInfo()
    {
        if (selectedSkillIndex < 0 || selectedSkillIndex >= currentUnitSkills.Count)
            return;

        var skill = currentUnitSkills[selectedSkillIndex];

        EditorGUILayout.LabelField("Skill Info", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField($"Skill ID: {skill.SKILL_ID}");
        EditorGUILayout.LabelField($"Name: {GetString(skill.SKILL_NAME)}");
        EditorGUILayout.LabelField($"Effect Prefab: {skill.SKILL_EFFECT}");

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Range Settings", EditorStyles.boldLabel);

        // SKILL_RANGE 편집 UI
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("SKILL_RANGE:", GUILayout.Width(100));

        float newRange = EditorGUILayout.FloatField(editedSkillRange, GUILayout.Width(60));
        if (!Mathf.Approximately(newRange, editedSkillRange))
        {
            editedSkillRange = newRange;
            skillRangeModified = !Mathf.Approximately(editedSkillRange, skill.SKILL_RANGE);
        }

        // 빠른 버튼들
        if (GUILayout.Button("1", GUILayout.Width(25))) { editedSkillRange = 1f; skillRangeModified = !Mathf.Approximately(editedSkillRange, skill.SKILL_RANGE); }
        if (GUILayout.Button("2", GUILayout.Width(25))) { editedSkillRange = 2f; skillRangeModified = !Mathf.Approximately(editedSkillRange, skill.SKILL_RANGE); }
        if (GUILayout.Button("3", GUILayout.Width(25))) { editedSkillRange = 3f; skillRangeModified = !Mathf.Approximately(editedSkillRange, skill.SKILL_RANGE); }
        if (GUILayout.Button("5", GUILayout.Width(25))) { editedSkillRange = 5f; skillRangeModified = !Mathf.Approximately(editedSkillRange, skill.SKILL_RANGE); }

        EditorGUILayout.EndHorizontal();

        // 변경 상태 표시 및 저장 버튼
        if (skillRangeModified)
        {
            EditorGUILayout.BeginHorizontal();
            GUI.color = Color.yellow;
            EditorGUILayout.LabelField($"Changed: {skill.SKILL_RANGE} → {editedSkillRange}", EditorStyles.miniLabel);
            GUI.color = Color.white;

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Save to CSV", GUILayout.Width(100)))
            {
                SaveSkillRangeToCSV(skill, editedSkillRange);
            }
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("Reset", GUILayout.Width(50)))
            {
                editedSkillRange = skill.SKILL_RANGE;
                skillRangeModified = false;
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.LabelField($"SKILL_ATKRANGE: {skill.SKILL_ATKRANGE}");

        currentSkillRange = editedSkillRange;

        EditorGUILayout.EndVertical();

        // 자식 파티클 정보 표시
        DrawChildParticlesInfo();
    }

    private void DrawChildParticlesInfo()
    {
        if (loadedPrefab == null) return;

        showChildParticlesFoldout = EditorGUILayout.Foldout(showChildParticlesFoldout, $"Child Particles ({childParticles.Count})", true);

        if (!showChildParticlesFoldout) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        if (childParticles.Count == 0)
        {
            EditorGUILayout.LabelField("No child particles found.", EditorStyles.miniLabel);
        }
        else
        {
            foreach (var child in childParticles)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // 자식 이름 헤더 + 활성화 상태 표시
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(child.path, EditorStyles.boldLabel);
                if (!child.isActiveInPrefab)
                {
                    GUI.color = Color.yellow;
                    EditorGUILayout.LabelField("[Inactive]", EditorStyles.miniLabel, GUILayout.Width(60));
                    GUI.color = Color.white;
                }
                if (child.particleSystem != null)
                {
                    GUI.color = Color.cyan;
                    EditorGUILayout.LabelField("[PS]", EditorStyles.miniLabel, GUILayout.Width(30));
                    GUI.color = Color.white;
                }
                EditorGUILayout.EndHorizontal();

                // Scale 행
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Scale", GUILayout.Width(50));
                EditorGUILayout.LabelField($"({child.originalScale.x:F2})", EditorStyles.miniLabel, GUILayout.Width(45));

                float newScale = EditorGUILayout.Slider(child.currentScale.x, 0.1f, 10f, GUILayout.Width(120));
                if (!Mathf.Approximately(newScale, child.currentScale.x))
                {
                    child.currentScale = Vector3.one * newScale;
                    UpdateChildPreviewTransform(child);
                }

                if (GUILayout.Button("1x", GUILayout.Width(25)))
                {
                    child.currentScale = child.originalScale;
                    UpdateChildPreviewTransform(child);
                }
                if (GUILayout.Button("R", GUILayout.Width(25)))
                {
                    child.currentScale = Vector3.one * currentSkillRange;
                    UpdateChildPreviewTransform(child);
                }
                EditorGUILayout.EndHorizontal();

                // Position 행
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Pos", GUILayout.Width(50));

                Vector3 newPos = child.currentPosition;
                newPos.x = EditorGUILayout.FloatField(newPos.x, GUILayout.Width(50));
                newPos.y = EditorGUILayout.FloatField(newPos.y, GUILayout.Width(50));
                newPos.z = EditorGUILayout.FloatField(newPos.z, GUILayout.Width(50));

                if (newPos != child.currentPosition)
                {
                    child.currentPosition = newPos;
                    UpdateChildPreviewTransform(child);
                }

                if (GUILayout.Button("Reset", GUILayout.Width(45)))
                {
                    child.currentPosition = child.originalPosition;
                    UpdateChildPreviewTransform(child);
                }
                if (GUILayout.Button("0", GUILayout.Width(20)))
                {
                    child.currentPosition = Vector3.zero;
                    UpdateChildPreviewTransform(child);
                }
                EditorGUILayout.EndHorizontal();

                // Rotation 행
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Rot", GUILayout.Width(50));

                Vector3 newRot = child.currentRotation;
                newRot.x = EditorGUILayout.FloatField(newRot.x, GUILayout.Width(50));
                newRot.y = EditorGUILayout.FloatField(newRot.y, GUILayout.Width(50));
                newRot.z = EditorGUILayout.FloatField(newRot.z, GUILayout.Width(50));

                if (newRot != child.currentRotation)
                {
                    child.currentRotation = newRot;
                    UpdateChildPreviewTransform(child);
                }

                if (GUILayout.Button("Reset", GUILayout.Width(45)))
                {
                    child.currentRotation = child.originalRotation;
                    UpdateChildPreviewTransform(child);
                }
                if (GUILayout.Button("0", GUILayout.Width(20)))
                {
                    child.currentRotation = Vector3.zero;
                    UpdateChildPreviewTransform(child);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(5);

            // 전체 자식 일괄 조정
            EditorGUILayout.LabelField("Batch All Children:", EditorStyles.boldLabel);

            // Scale 일괄
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Scale:", GUILayout.Width(45));
            if (GUILayout.Button("All = 1x"))
            {
                foreach (var child in childParticles)
                {
                    child.currentScale = child.originalScale;
                    UpdateChildPreviewTransform(child);
                }
            }
            if (GUILayout.Button($"All = R({currentSkillRange})"))
            {
                foreach (var child in childParticles)
                {
                    child.currentScale = Vector3.one * currentSkillRange;
                    UpdateChildPreviewTransform(child);
                }
            }
            if (GUILayout.Button("x2"))
            {
                foreach (var child in childParticles)
                {
                    child.currentScale = child.currentScale * 2f;
                    UpdateChildPreviewTransform(child);
                }
            }
            if (GUILayout.Button("/2"))
            {
                foreach (var child in childParticles)
                {
                    child.currentScale = child.currentScale * 0.5f;
                    UpdateChildPreviewTransform(child);
                }
            }
            EditorGUILayout.EndHorizontal();

            // Position 일괄
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Pos:", GUILayout.Width(45));
            if (GUILayout.Button("All Reset"))
            {
                foreach (var child in childParticles)
                {
                    child.currentPosition = child.originalPosition;
                    UpdateChildPreviewTransform(child);
                }
            }
            if (GUILayout.Button("All = 0"))
            {
                foreach (var child in childParticles)
                {
                    child.currentPosition = Vector3.zero;
                    UpdateChildPreviewTransform(child);
                }
            }
            EditorGUILayout.EndHorizontal();

            // Rotation 일괄
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Rot:", GUILayout.Width(45));
            if (GUILayout.Button("All Reset"))
            {
                foreach (var child in childParticles)
                {
                    child.currentRotation = child.originalRotation;
                    UpdateChildPreviewTransform(child);
                }
            }
            if (GUILayout.Button("All = 0"))
            {
                foreach (var child in childParticles)
                {
                    child.currentRotation = Vector3.zero;
                    UpdateChildPreviewTransform(child);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(5);

        // 루트 스케일 변경 시 자식도 함께 변경할지 옵션
        scaleChildParticles = EditorGUILayout.Toggle("Link Children to Root Scale", scaleChildParticles);

        if (scaleChildParticles)
        {
            EditorGUILayout.HelpBox("When enabled, changing root scale will also multiply child scales proportionally.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("Children can be scaled independently. Use sliders above to adjust each child.", MessageType.Info);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawEffectScaleControls()
    {
        EditorGUILayout.LabelField("3. Effect Scale", EditorStyles.boldLabel);

        if (loadedPrefab == null)
        {
            EditorGUILayout.HelpBox("Select a skill to load its effect prefab.", MessageType.Info);
            return;
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // 원본 스케일 표시
        EditorGUILayout.LabelField($"Original Scale: {originalScale}");
        EditorGUILayout.LabelField($"Current Prefab: {loadedPrefab.name}");

        EditorGUILayout.Space(5);

        // Uniform Scale 토글
        useUniformScale = EditorGUILayout.Toggle("Uniform Scale", useUniformScale);

        if (useUniformScale)
        {
            float newUniformScale = EditorGUILayout.Slider("Scale", uniformScale, 0.1f, 10f);
            if (!Mathf.Approximately(newUniformScale, uniformScale))
            {
                uniformScale = newUniformScale;
                effectScale = Vector3.one * uniformScale;
                UpdatePreviewScale();
            }

            // 빠른 스케일 버튼
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("0.5x")) { uniformScale = 0.5f; effectScale = Vector3.one * uniformScale; UpdatePreviewScale(); }
            if (GUILayout.Button("1x")) { uniformScale = 1f; effectScale = Vector3.one * uniformScale; UpdatePreviewScale(); }
            if (GUILayout.Button("2x")) { uniformScale = 2f; effectScale = Vector3.one * uniformScale; UpdatePreviewScale(); }
            if (GUILayout.Button("3x")) { uniformScale = 3f; effectScale = Vector3.one * uniformScale; UpdatePreviewScale(); }
            EditorGUILayout.EndHorizontal();

            // Range 기반 자동 스케일
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Auto Scale from SKILL_RANGE:", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button($"= Range ({currentSkillRange})"))
            {
                uniformScale = currentSkillRange;
                effectScale = Vector3.one * uniformScale;
                UpdatePreviewScale();
            }
            if (GUILayout.Button($"= Range/2 ({currentSkillRange / 2f:F1})"))
            {
                uniformScale = currentSkillRange / 2f;
                effectScale = Vector3.one * uniformScale;
                UpdatePreviewScale();
            }
            if (GUILayout.Button($"= Range*2 ({currentSkillRange * 2f:F1})"))
            {
                uniformScale = currentSkillRange * 2f;
                effectScale = Vector3.one * uniformScale;
                UpdatePreviewScale();
            }
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            effectScale = EditorGUILayout.Vector3Field("Scale", effectScale);
            UpdatePreviewScale();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawPreviewControls()
    {
        EditorGUILayout.LabelField("4. Preview", EditorStyles.boldLabel);

        if (loadedPrefab == null)
        {
            EditorGUILayout.HelpBox("Select a skill first to preview.", MessageType.Info);
            return;
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        showRangeGizmo = EditorGUILayout.Toggle("Show Range Gizmo", showRangeGizmo);
        testSpawnPosition = EditorGUILayout.Vector3Field("Preview Position", testSpawnPosition);

        EditorGUILayout.Space(5);

        // 프리뷰 생성/제거 버튼
        EditorGUILayout.BeginHorizontal();
        if (previewInstance == null)
        {
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Create Preview in Scene", GUILayout.Height(25)))
            {
                CreatePreviewInstance();
            }
            GUI.backgroundColor = Color.white;
        }
        else
        {
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Remove Preview", GUILayout.Height(25)))
            {
                CleanupPreview();
            }
            GUI.backgroundColor = Color.white;
        }
        EditorGUILayout.EndHorizontal();

        // 프리뷰 상태 표시 및 파티클 컨트롤
        if (previewInstance != null)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"Preview Active: {previewInstance.name}", EditorStyles.boldLabel);

            // 파티클 재생 컨트롤 - 항상 표시
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("Play Particles", GUILayout.Height(30)))
            {
                PlayPreviewParticles();
            }
            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("Stop Particles", GUILayout.Height(30)))
            {
                StopPreviewParticles();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("Click 'Create Preview' to spawn effect in scene and test particles.", MessageType.Info);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawSaveControls()
    {
        EditorGUILayout.LabelField("5. Save", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        if (loadedPrefab == null)
        {
            EditorGUILayout.HelpBox("Load a prefab first.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.LabelField($"Target Prefab: {loadedPrefab.name}");
            EditorGUILayout.LabelField($"New Scale: {effectScale}");

            EditorGUILayout.Space(5);

            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("Save Scale to Prefab", GUILayout.Height(30)))
            {
                SaveScaleToPrefab();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.HelpBox("This will modify the prefab's root transform scale.", MessageType.Warning);
        }

        EditorGUILayout.EndVertical();
    }

    #endregion

    #region Data Loading

    private void LoadAllTables()
    {
        LoadStringTable();
        LoadUnitTable();
        LoadSkillTable();
        BuildUnitNames();
    }

    private void LoadStringTable()
    {
        stringTable.Clear();
        string path = "Assets/DataTables/StringTable.csv";

        if (!File.Exists(path))
        {
            Debug.LogWarning($"StringTable not found at {path}");
            return;
        }

        var lines = File.ReadAllLines(path);
        for (int i = 1; i < lines.Length; i++) // Skip header
        {
            var parts = ParseCSVLine(lines[i]);
            if (parts.Length >= 2 && int.TryParse(parts[0], out int id))
            {
                stringTable[id] = parts[1];
            }
        }
    }

    private void LoadUnitTable()
    {
        unitDataList.Clear();
        string path = "Assets/DataTables/UnitTable.csv";

        if (!File.Exists(path))
        {
            Debug.LogWarning($"UnitTable not found at {path}");
            return;
        }

        var lines = File.ReadAllLines(path);
        var headers = ParseCSVLine(lines[0]);

        for (int i = 1; i < lines.Length; i++)
        {
            var parts = ParseCSVLine(lines[i]);
            if (parts.Length < 18) continue;

            var entry = new UnitDataEntry
            {
                UNIT_ID = ParseInt(parts, 0),
                NAME = ParseInt(parts, 1),
                RANK = ParseInt(parts, 2),
                LEVEL = ParseInt(parts, 3),
                UNIT_SKILL1 = ParseInt(parts, 9),
                UNIT_SKILL2 = ParseInt(parts, 10),
            };

            unitDataList.Add(entry);
        }
    }

    private void LoadSkillTable()
    {
        skillDataList.Clear();
        skillTableRawData.Clear();
        string path = "Assets/DataTables/SkillTable.csv";

        if (!File.Exists(path))
        {
            Debug.LogWarning($"SkillTable not found at {path}");
            return;
        }

        var lines = File.ReadAllLines(path);

        // 원본 CSV 데이터 저장 (헤더 포함)
        for (int i = 0; i < lines.Length; i++)
        {
            skillTableRawData.Add(ParseCSVLine(lines[i]));
        }

        for (int i = 1; i < lines.Length; i++)
        {
            var parts = skillTableRawData[i];
            if (parts.Length < 20) continue;

            var entry = new SkillDataEntry
            {
                SKILL_ID = ParseInt(parts, 0),
                SKILL_NAME = ParseInt(parts, 1),
                SKILL_COOLTIME = ParseFloat(parts, 11),
                SKILL_ATKRANGE = ParseFloat(parts, 15),
                SKILL_RANGE = ParseFloat(parts, 16),
                SKILL_ICON = parts.Length > 17 ? parts[17] : "",
                SKILL_EFFECT = parts.Length > 19 ? parts[19] : "",
                csvRowIndex = i,
            };

            skillDataList.Add(entry);
        }
    }

    private void BuildUnitNames()
    {
        unitNames = unitDataList.Select(u =>
        {
            string name = GetString(u.NAME);
            return $"[{u.UNIT_ID}] {name} (Rank:{u.RANK} Lv:{u.LEVEL})";
        }).ToArray();
    }

    #endregion

    #region Selection Handlers

    private void OnUnitSelected()
    {
        if (selectedUnitIndex < 0 || selectedUnitIndex >= unitDataList.Count)
            return;

        var unit = unitDataList[selectedUnitIndex];
        currentUnitSkills.Clear();

        // 유닛의 스킬 찾기
        if (unit.UNIT_SKILL1 > 0)
        {
            var skill = skillDataList.FirstOrDefault(s => s.SKILL_ID == unit.UNIT_SKILL1);
            if (skill != null) currentUnitSkills.Add(skill);
        }
        if (unit.UNIT_SKILL2 > 0)
        {
            var skill = skillDataList.FirstOrDefault(s => s.SKILL_ID == unit.UNIT_SKILL2);
            if (skill != null) currentUnitSkills.Add(skill);
        }

        // 스킬 이름 목록 생성
        skillNames = currentUnitSkills.Select(s =>
        {
            string name = GetString(s.SKILL_NAME);
            return $"[{s.SKILL_ID}] {name} ({s.SKILL_EFFECT})";
        }).ToArray();

        selectedSkillIndex = 0;
        OnSkillSelected();
    }

    private void OnSkillSelected()
    {
        if (selectedSkillIndex < 0 || selectedSkillIndex >= currentUnitSkills.Count)
            return;

        var skill = currentUnitSkills[selectedSkillIndex];
        LoadEffectPrefab(skill.SKILL_EFFECT);

        // SKILL_RANGE 편집 초기화
        editedSkillRange = skill.SKILL_RANGE;
        skillRangeModified = false;
    }

    private void LoadEffectPrefab(string effectName)
    {
        childParticles.Clear();

        if (string.IsNullOrEmpty(effectName))
        {
            loadedPrefab = null;
            return;
        }

        // 1. 직접 경로 탐색 (UnitSkillProjectile용)
        string[] searchPaths = new[]
        {
            $"Assets/Prefabs/UnitSkillProjectiles/{effectName}.prefab",
            $"Assets/Prefabs/{effectName}.prefab",
            $"Assets/Prefabs/Effects/{effectName}.prefab",
        };

        foreach (var path in searchPaths)
        {
            loadedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (loadedPrefab != null)
            {
                SetLoadedPrefab(loadedPrefab, path);
                return;
            }
        }

        // 2. 정확한 이름으로 전체 프로젝트 검색
        var guids = AssetDatabase.FindAssets($"t:Prefab");
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);

            // 정확히 일치하거나 (Opt) 접두어 포함
            if (fileName == effectName || fileName == $"(Opt){effectName}")
            {
                loadedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (loadedPrefab != null)
                {
                    SetLoadedPrefab(loadedPrefab, path);
                    return;
                }
            }
        }

        // 3. 부분 이름 검색 (괄호 제거 후)
        string cleanName = effectName.Replace("(", "").Replace(")", "");
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            string cleanFileName = fileName.Replace("(", "").Replace(")", "").Replace("(Opt)", "");

            if (cleanFileName.Contains(cleanName) || cleanName.Contains(cleanFileName))
            {
                loadedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (loadedPrefab != null)
                {
                    SetLoadedPrefab(loadedPrefab, path);
                    return;
                }
            }
        }

        Debug.LogWarning($"Prefab not found: {effectName}");
        loadedPrefab = null;
    }

    private void SetLoadedPrefab(GameObject prefab, string path)
    {
        originalScale = prefab.transform.localScale;
        effectScale = originalScale;
        uniformScale = originalScale.x;
        CollectChildParticles(prefab);
        Debug.Log($"Loaded prefab: {path}");
    }

    private void CollectChildParticles(GameObject prefab)
    {
        childParticles.Clear();

        // 모든 자식의 ParticleSystem과 Transform 정보 수집
        var allTransforms = prefab.GetComponentsInChildren<Transform>(true);

        foreach (var t in allTransforms)
        {
            if (t == prefab.transform) continue; // 루트 제외

            var ps = t.GetComponent<ParticleSystem>();
            string path = GetTransformPath(t, prefab.transform);

            childParticles.Add(new ChildParticleInfo
            {
                name = t.name,
                path = path,
                originalScale = t.localScale,
                currentScale = t.localScale,
                originalPosition = t.localPosition,
                currentPosition = t.localPosition,
                originalRotation = t.localEulerAngles,
                currentRotation = t.localEulerAngles,
                particleSystem = ps,
                isActiveInPrefab = t.gameObject.activeSelf
            });
        }
    }

    private string GetTransformPath(Transform target, Transform root)
    {
        string path = target.name;
        Transform current = target.parent;

        while (current != null && current != root)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }

    #endregion

    #region Preview

    private void CreatePreviewInstance()
    {
        CleanupPreview();

        if (loadedPrefab == null) return;

        previewInstance = (GameObject)PrefabUtility.InstantiatePrefab(loadedPrefab);
        previewInstance.name = $"[PREVIEW] {loadedPrefab.name}";
        previewInstance.transform.position = testSpawnPosition;
        previewInstance.transform.localScale = effectScale;

        // 자식 파티클 스케일 적용 (개별 설정값 사용)
        ApplyAllChildScalesToPreview();

        Selection.activeGameObject = previewInstance;
        SceneView.lastActiveSceneView?.Frame(new Bounds(testSpawnPosition, Vector3.one * currentSkillRange * 2), false);
    }

    private void CleanupPreview()
    {
        if (previewInstance != null)
        {
            DestroyImmediate(previewInstance);
            previewInstance = null;
        }
    }

    private void UpdatePreviewScale()
    {
        if (previewInstance != null)
        {
            previewInstance.transform.localScale = effectScale;

            // Link 모드일 때만 자식도 비례 업데이트
            if (scaleChildParticles)
            {
                ApplyChildParticleScalesProportional();
            }
        }
    }

    // 개별 자식 Transform 업데이트 (Scale + Position + Rotation)
    private void UpdateChildPreviewTransform(ChildParticleInfo childInfo)
    {
        if (previewInstance == null) return;

        var childTransform = previewInstance.transform.Find(childInfo.path);
        if (childTransform != null)
        {
            childTransform.localScale = childInfo.currentScale;
            childTransform.localPosition = childInfo.currentPosition;
            childTransform.localEulerAngles = childInfo.currentRotation;
        }
    }

    // 모든 자식에 개별 설정된 currentScale/currentPosition/currentRotation 적용
    private void ApplyAllChildScalesToPreview()
    {
        if (previewInstance == null) return;

        foreach (var childInfo in childParticles)
        {
            var childTransform = previewInstance.transform.Find(childInfo.path);
            if (childTransform != null)
            {
                childTransform.localScale = childInfo.currentScale;
                childTransform.localPosition = childInfo.currentPosition;
                childTransform.localEulerAngles = childInfo.currentRotation;
            }
        }
    }

    // 루트 스케일 변경에 비례하여 자식 스케일 업데이트 (Link 모드)
    private void ApplyChildParticleScalesProportional()
    {
        if (previewInstance == null) return;

        float scaleMultiplier = uniformScale / (originalScale.x > 0 ? originalScale.x : 1f);

        foreach (var childInfo in childParticles)
        {
            var childTransform = previewInstance.transform.Find(childInfo.path);
            if (childTransform != null)
            {
                // currentScale도 업데이트
                childInfo.currentScale = childInfo.originalScale * scaleMultiplier;
                childTransform.localScale = childInfo.currentScale;
                childTransform.localPosition = childInfo.currentPosition;
                childTransform.localEulerAngles = childInfo.currentRotation;
            }
        }
    }

    private void PlayPreviewParticles()
    {
        if (previewInstance == null) return;

        // PlayerUnitProjectile 등에서 비활성화된 파티클 오브젝트들도 활성화
        var particles = previewInstance.GetComponentsInChildren<ParticleSystem>(true); // includeInactive = true
        foreach (var ps in particles)
        {
            // 비활성화된 GameObject 활성화
            if (!ps.gameObject.activeInHierarchy)
            {
                ps.gameObject.SetActive(true);
            }
            ps.Clear();
            ps.Play();
        }

        Debug.Log($"Playing {particles.Length} particle systems");
    }

    private void StopPreviewParticles()
    {
        if (previewInstance == null) return;

        var particles = previewInstance.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in particles)
        {
            ps.Stop();
            ps.Clear();
        }
    }

    #endregion

    #region All Skills Overview

    private bool showAllSkillsOverview = false;
    private Vector2 overviewScrollPosition;

    private void DrawAllSkillsOverview()
    {
        showAllSkillsOverview = EditorGUILayout.Foldout(showAllSkillsOverview, "All Skills Overview (Range & Effect)", true);

        if (!showAllSkillsOverview) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // 헤더
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Skill ID", GUILayout.Width(60));
        EditorGUILayout.LabelField("Name", GUILayout.Width(150));
        EditorGUILayout.LabelField("Range", GUILayout.Width(50));
        EditorGUILayout.LabelField("AtkRange", GUILayout.Width(60));
        EditorGUILayout.LabelField("Effect Prefab", GUILayout.Width(180));
        EditorGUILayout.LabelField("Prefab Scale", GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(2);

        overviewScrollPosition = EditorGUILayout.BeginScrollView(overviewScrollPosition, GUILayout.MaxHeight(300));

        // 유닛 스킬만 (21xxx)
        var unitSkills = skillDataList.Where(s => s.SKILL_ID >= 21000 && s.SKILL_ID < 22000).OrderBy(s => s.SKILL_ID);

        foreach (var skill in unitSkills)
        {
            DrawSkillRow(skill);
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(5);

        // 일괄 적용 버튼
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Apply Range as Scale to ALL Prefabs"))
        {
            if (EditorUtility.DisplayDialog("Confirm",
                "This will set each prefab's scale to match its SKILL_RANGE value.\nAre you sure?",
                "Yes, Apply All", "Cancel"))
            {
                ApplyRangeAsScaleToAll();
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawSkillRow(SkillDataEntry skill)
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(skill.SKILL_ID.ToString(), GUILayout.Width(60));
        EditorGUILayout.LabelField(GetString(skill.SKILL_NAME), GUILayout.Width(150));
        EditorGUILayout.LabelField(skill.SKILL_RANGE.ToString("F1"), GUILayout.Width(50));
        EditorGUILayout.LabelField(skill.SKILL_ATKRANGE.ToString("F1"), GUILayout.Width(60));
        EditorGUILayout.LabelField(skill.SKILL_EFFECT, GUILayout.Width(180));

        // 프리팹 스케일 확인
        var prefab = FindEffectPrefab(skill.SKILL_EFFECT);
        if (prefab != null)
        {
            var scale = prefab.transform.localScale;
            bool scaleMatchesRange = Mathf.Approximately(scale.x, skill.SKILL_RANGE);

            GUI.color = scaleMatchesRange ? Color.green : Color.yellow;
            EditorGUILayout.LabelField($"{scale.x:F1}", GUILayout.Width(50));
            GUI.color = Color.white;

            if (GUILayout.Button("Select", GUILayout.Width(45)))
            {
                // 해당 스킬 선택
                SelectSkillById(skill.SKILL_ID);
            }
        }
        else
        {
            GUI.color = Color.red;
            EditorGUILayout.LabelField("Not Found", GUILayout.Width(100));
            GUI.color = Color.white;
        }

        EditorGUILayout.EndHorizontal();
    }

    private GameObject FindEffectPrefab(string effectName)
    {
        if (string.IsNullOrEmpty(effectName)) return null;

        // 1. 직접 경로 탐색
        string[] searchPaths = new[]
        {
            $"Assets/Prefabs/UnitSkillProjectiles/{effectName}.prefab",
            $"Assets/Prefabs/{effectName}.prefab",
            $"Assets/Prefabs/Effects/{effectName}.prefab",
        };

        foreach (var path in searchPaths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null) return prefab;
        }

        // 2. 전체 프로젝트에서 정확한 이름 검색
        var guids = AssetDatabase.FindAssets($"t:Prefab");
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);

            if (fileName == effectName || fileName == $"(Opt){effectName}")
            {
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
        }

        // 3. 부분 이름 검색
        string cleanName = effectName.Replace("(", "").Replace(")", "");
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            string cleanFileName = fileName.Replace("(", "").Replace(")", "").Replace("(Opt)", "");

            if (cleanFileName.Contains(cleanName) || cleanName.Contains(cleanFileName))
            {
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
        }

        return null;
    }

    private void SelectSkillById(int skillId)
    {
        // 스킬을 가진 유닛 찾기
        for (int i = 0; i < unitDataList.Count; i++)
        {
            var unit = unitDataList[i];
            if (unit.UNIT_SKILL1 == skillId || unit.UNIT_SKILL2 == skillId)
            {
                selectedUnitIndex = i;
                OnUnitSelected();

                // 해당 스킬 선택
                for (int j = 0; j < currentUnitSkills.Count; j++)
                {
                    if (currentUnitSkills[j].SKILL_ID == skillId)
                    {
                        selectedSkillIndex = j;
                        OnSkillSelected();
                        break;
                    }
                }
                break;
            }
        }
    }

    private void ApplyRangeAsScaleToAll()
    {
        int modified = 0;
        var unitSkills = skillDataList.Where(s => s.SKILL_ID >= 21000 && s.SKILL_ID < 22000);

        foreach (var skill in unitSkills)
        {
            var prefab = FindEffectPrefab(skill.SKILL_EFFECT);
            if (prefab == null) continue;

            string path = AssetDatabase.GetAssetPath(prefab);
            float targetScale = skill.SKILL_RANGE;
            float originalRootScale = prefab.transform.localScale.x;
            float scaleMultiplier = targetScale / (originalRootScale > 0 ? originalRootScale : 1f);

            using (var editScope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                var root = editScope.prefabContentsRoot;
                if (!Mathf.Approximately(root.transform.localScale.x, targetScale))
                {
                    // 루트 스케일 적용
                    root.transform.localScale = Vector3.one * targetScale;

                    // 자식 파티클 스케일도 적용
                    if (scaleChildParticles)
                    {
                        var allChildren = root.GetComponentsInChildren<Transform>(true);
                        foreach (var child in allChildren)
                        {
                            if (child == root.transform) continue;
                            child.localScale = child.localScale * scaleMultiplier;
                        }
                    }

                    modified++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string childInfo = scaleChildParticles ? " (including children)" : "";
        EditorUtility.DisplayDialog("Complete", $"Modified {modified} prefabs{childInfo}.", "OK");
    }

    #endregion

    #region Play Mode Test

    private void DrawPlayModeTestSection()
    {
        EditorGUILayout.LabelField("Play Mode Test", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to test skills in action.", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Enter Play Mode", GUILayout.Height(25)))
            {
                EditorApplication.isPlaying = true;
            }
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox("Play Mode Active - You can test skills now!", MessageType.None);

            if (GUILayout.Button("Exit Play Mode", GUILayout.Height(25)))
            {
                EditorApplication.isPlaying = false;
            }
        }

        EditorGUILayout.EndVertical();
    }

    #endregion

    #region Save

    private void SaveScaleToPrefab()
    {
        if (loadedPrefab == null) return;

        string path = AssetDatabase.GetAssetPath(loadedPrefab);
        int childCount = 0;
        int posChangedCount = 0;
        int rotChangedCount = 0;

        // 프리팹 편집 모드로 열기
        using (var editScope = new PrefabUtility.EditPrefabContentsScope(path))
        {
            var root = editScope.prefabContentsRoot;
            root.transform.localScale = effectScale;

            // 자식 파티클 Transform 저장 (개별 설정된 currentScale/currentPosition/currentRotation 사용)
            foreach (var childInfo in childParticles)
            {
                var childTransform = root.transform.Find(childInfo.path);
                if (childTransform != null)
                {
                    childTransform.localScale = childInfo.currentScale;
                    childTransform.localPosition = childInfo.currentPosition;
                    childTransform.localEulerAngles = childInfo.currentRotation;
                    childCount++;

                    // 위치가 변경되었는지 확인
                    if (childInfo.currentPosition != childInfo.originalPosition)
                        posChangedCount++;
                    // 회전이 변경되었는지 확인
                    if (childInfo.currentRotation != childInfo.originalRotation)
                        rotChangedCount++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Saved scale {effectScale} to {path} (+ {childCount} children, {posChangedCount} pos, {rotChangedCount} rot)");
        EditorUtility.DisplayDialog("Success",
            $"Prefab saved!\n{loadedPrefab.name}\nRoot Scale: {effectScale}\nChild objects: {childCount}\nPosition changes: {posChangedCount}\nRotation changes: {rotChangedCount}", "OK");

        // 원본 Transform 업데이트 및 자식 정보 갱신
        originalScale = effectScale;
        CollectChildParticles(loadedPrefab);
    }

    private void SaveSkillRangeToCSV(SkillDataEntry skill, float newRange)
    {
        if (skill.csvRowIndex <= 0 || skill.csvRowIndex >= skillTableRawData.Count)
        {
            EditorUtility.DisplayDialog("Error", "Invalid CSV row index.", "OK");
            return;
        }

        // CSV 데이터 업데이트
        var row = skillTableRawData[skill.csvRowIndex];
        if (SKILL_RANGE_COLUMN_INDEX < row.Length)
        {
            row[SKILL_RANGE_COLUMN_INDEX] = newRange.ToString("F1");
        }

        // CSV 파일 저장
        string path = "Assets/DataTables/SkillTable.csv";
        SaveCSVFile(path, skillTableRawData);

        // 메모리 내 데이터도 업데이트
        skill.SKILL_RANGE = newRange;
        skillRangeModified = false;

        AssetDatabase.Refresh();
        Debug.Log($"Saved SKILL_RANGE {newRange} to {path} for skill {skill.SKILL_ID}");
        EditorUtility.DisplayDialog("Success", $"SKILL_RANGE updated to {newRange}\nSkill ID: {skill.SKILL_ID}", "OK");
    }

    private void SaveCSVFile(string path, List<string[]> data)
    {
        var lines = new List<string>();
        foreach (var row in data)
        {
            // 각 필드를 CSV 형식으로 변환 (쉼표가 포함된 경우 따옴표로 감싸기)
            var formattedRow = row.Select(field =>
            {
                if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
                {
                    return "\"" + field.Replace("\"", "\"\"") + "\"";
                }
                return field;
            });
            lines.Add(string.Join(",", formattedRow));
        }
        File.WriteAllLines(path, lines);
    }

    #endregion

    #region Scene GUI

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!showRangeGizmo) return;

        Vector3 center = previewInstance != null ? previewInstance.transform.position : testSpawnPosition;

        // SKILL_RANGE 원 그리기
        Handles.color = new Color(1f, 0.5f, 0f, 0.5f);
        Handles.DrawWireDisc(center, Vector3.forward, currentSkillRange);

        // 현재 이펙트 스케일 표시 (X 기준)
        Handles.color = new Color(0f, 1f, 0f, 0.5f);
        Handles.DrawWireDisc(center, Vector3.forward, effectScale.x);

        // 라벨
        Handles.Label(center + Vector3.up * (currentSkillRange + 0.5f),
            $"SKILL_RANGE: {currentSkillRange}\nEffect Scale: {effectScale.x:F2}");
    }

    #endregion

    #region Utilities

    private string[] ParseCSVLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        string current = "";

        foreach (char c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current);
                current = "";
            }
            else
            {
                current += c;
            }
        }
        result.Add(current);

        return result.ToArray();
    }

    private int ParseInt(string[] parts, int index)
    {
        if (index >= parts.Length) return 0;
        int.TryParse(parts[index], out int result);
        return result;
    }

    private float ParseFloat(string[] parts, int index)
    {
        if (index >= parts.Length) return 0f;
        float.TryParse(parts[index], out float result);
        return result;
    }

    private string GetString(int id)
    {
        if (stringTable.TryGetValue(id, out string value))
            return value;
        return $"#{id}";
    }

    #endregion

    #region Data Classes

    private class UnitDataEntry
    {
        public int UNIT_ID;
        public int NAME;
        public int RANK;
        public int LEVEL;
        public int UNIT_SKILL1;
        public int UNIT_SKILL2;
    }

    private class SkillDataEntry
    {
        public int SKILL_ID;
        public int SKILL_NAME;
        public float SKILL_COOLTIME;
        public float SKILL_ATKRANGE;
        public float SKILL_RANGE;
        public string SKILL_ICON;
        public string SKILL_EFFECT;
        public int csvRowIndex; // CSV 원본 행 인덱스
    }

    #endregion
}
