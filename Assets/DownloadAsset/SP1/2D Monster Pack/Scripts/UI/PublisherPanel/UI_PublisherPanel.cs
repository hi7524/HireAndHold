using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SP1Assets.MonsterPack2D
{
    public class UI_PublisherPanel : MonoBehaviour
    {
        [SerializeField] Button _button_AssetStore;
        [SerializeField] Button _button_Youtube;
        [SerializeField] Button _button_Homepage;

        readonly string _url_AssetStore = "https://assetstore.unity.com/publishers/116487";
        readonly string _url_Youtube = "https://www.youtube.com/@SP1-k6q";
        readonly string _url_Homepage = "http://idiocracygames.com/sp1";

        void Awake()
        {
            _button_AssetStore.onClick.RemoveAllListeners();
            _button_Youtube.onClick.RemoveAllListeners();
            _button_Homepage.onClick.RemoveAllListeners();

            _button_AssetStore.onClick.AddListener(() => OpenUrl(_url_AssetStore));
            _button_Youtube.onClick.AddListener(() => OpenUrl(_url_Youtube));
            _button_Homepage.onClick.AddListener(() => OpenUrl(_url_Homepage));
        }

        void OpenUrl(string url)
        {
            Application.OpenURL(url);
        }
    }
}