using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

namespace SP1Assets.MonsterPack2D
{
    public class UI_MonsterButton : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI _textMesh;
        [SerializeField] Button _button;
        [SerializeField] Image _image_BG;
        [SerializeField] Color _bgColor_Active;
        [SerializeField] Color _bgColor_Passive;

        public string MonsterName { get; private set; }

        UnityAction<UI_MonsterButton> _action;
        public void AddListener_OnClick(UnityAction<UI_MonsterButton> action) => _action = action;

        public void Init()
        {
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(OnClick);
        }

        void OnClick()
        {
            _action?.Invoke(this);
        }

        public void SetMonsterName(string text)
        {
            MonsterName = text;
            _textMesh.text = text;
        }

        public void SetSelected(bool b)
        {
            _image_BG.color = b ? _bgColor_Active : _bgColor_Passive;
        }
    }
}