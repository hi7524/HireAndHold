using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.Universal;
#endif

namespace SP1Assets.MonsterPack2D
{
    public class UI_Main : MonoBehaviour
    {
        [SerializeField] Camera _uiCam;
        int _screenWidth_Prev = 1;
        int _screenHeight_Prev = 1;
        float _orthoSize_Default = 1f;
        float _aspect_Default = 1920f / 1080f;

        const string _assetFolderPath = "Assets/DownloadAsset/SP1/2D Monster Pack/MonsterAssets/MonsterParents";

        [SerializeField] Transform _transform_Monsters_Parent;
        List<MonsterParent> _monsterParent_List = new List<MonsterParent>();
        MonsterParent _selectedMonsterParent = null;

        [SerializeField] GameObject _prefab_MonsterButton;
        [SerializeField] RectTransform _rectTran_MonsterButtonsParent;
        List<UI_MonsterButton> _monsterButtons = new List<UI_MonsterButton>();

        [SerializeField] GameObject _prefab_AnimationButton;
        [SerializeField] RectTransform _rectTran_AnimationButtonsParent;
        List<UI_AnimationButton> _animationButtons = new List<UI_AnimationButton>();

        public void Init()
        {
#if UNITY_6000_0_OR_NEWER
        Camera uiCam = FindAnyObjectByType<Canvas>().worldCamera;
        uiCam.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
        Camera.main.GetUniversalAdditionalCameraData().cameraStack.Add(uiCam);
#endif

            _screenWidth_Prev = Screen.width;
            _screenHeight_Prev = Screen.height;
            _orthoSize_Default = Camera.main.orthographicSize;

            Init_Monsters();
            Init_MonsterButtons();
        }

        void Init_Monsters()
        {
            _monsterParent_List.Clear();

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { _assetFolderPath });
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                if (prefab != null && prefab.GetComponent<MonsterParent>())
                {
                    MonsterParent monsterParent = Instantiate(prefab, _transform_Monsters_Parent).GetComponent<MonsterParent>();
                    monsterParent.transform.localPosition = Vector3.zero;
                    monsterParent.name = prefab.name;
                    monsterParent.Init(12);

                    _monsterParent_List.Add(monsterParent);
                }
            }

            _monsterParent_List.Sort((a, b) => b.name.CompareTo(a.name));
        }

        void Init_MonsterButtons()
        {
            foreach (var button in _monsterButtons)
                Destroy(button.gameObject);

            _monsterButtons.Clear();

            foreach (var monsterParent in _monsterParent_List)
            {
                UI_MonsterButton monsterButton = Instantiate(_prefab_MonsterButton, _rectTran_MonsterButtonsParent).GetComponent<UI_MonsterButton>();
                monsterButton.Init();
                monsterButton.AddListener_OnClick(OnClick_MonsterButton);
                monsterButton.SetMonsterName(monsterParent.name);
                _monsterButtons.Add(monsterButton);
            }

            ActivateMonsters(_monsterButtons[_monsterButtons.Count - 1].MonsterName);
        }

        void OnClick_MonsterButton(UI_MonsterButton monsterButton)
        {
            ActivateMonsters(monsterButton.MonsterName);
        }

        void ActivateMonsters(string monsterName)
        {
            foreach (var monsterParent in _monsterParent_List)
            {
                monsterParent.gameObject.SetActive(monsterParent.name == monsterName);
                if (monsterParent.name == monsterName)
                    _selectedMonsterParent = monsterParent;
            }

            if (_selectedMonsterParent == null)
                return;

            foreach (var button in _monsterButtons)
                button.SetSelected(button.MonsterName == monsterName);

            _selectedMonsterParent.ResetMonsters();

            SetAnimButtons(_selectedMonsterParent.GetAnimationNames());
            PlayAnimation("idle");
        }

        void SetAnimButtons(List<string> animNames)
        {
            if (animNames == null)
                return;

            foreach (var button in _animationButtons)
                Destroy(button.gameObject);

            _animationButtons.Clear();

            animNames.Sort((a, b) => a.CompareTo(b));

            foreach (var animName in animNames)
            {
                UI_AnimationButton animButton = Instantiate(_prefab_AnimationButton, _rectTran_AnimationButtonsParent).GetComponent<UI_AnimationButton>();
                animButton.Init();
                animButton.AddListener_OnClick(OnClick_AnimButton);
                animButton.SetAnimationName(animName);
                _animationButtons.Add(animButton);
            }
        }

        void OnClick_AnimButton(UI_AnimationButton animButton)
        {
            PlayAnimation(animButton.AnimationName);
        }

        void PlayAnimation(string animationName)
        {
            _selectedMonsterParent.PlayAnimation(animationName);

            foreach (var button in _animationButtons)
                button.SetSelected(button.AnimationName == animationName);
        }

        void Update()
        {
            if (_screenWidth_Prev != Screen.width || _screenHeight_Prev != Screen.height)
            {
                float orthoSizeModifier = Camera.main.aspect >= _aspect_Default ? 1 : _aspect_Default / Camera.main.aspect;
                Camera.main.orthographicSize = _orthoSize_Default * orthoSizeModifier;
            }

            DebugControl();
        }

        void DebugControl()
        {
            // new input system
            if(Keyboard.current != null && Keyboard.current.dKey.wasPressedThisFrame)
            
            {
                _selectedMonsterParent.ResetMonsters();
            }
        }
    }
}
