using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SP1Assets.MonsterPack2D
{
    public class Main : MonoBehaviour
    {
        [SerializeField] UI_Main _uiMain;

        void Awake()
        {
            if (_uiMain == null)
            {
                Debug.LogError("UI Main is null.");
                return;
            }
        }

        void Start()
        {
            _uiMain.Init();
        }
    }
}


