using System;
using Cysharp.Threading.Tasks;
using Tutorial;
using UnityEngine;
using UnityEngine.EventSystems;

public class GenericWindow : MonoBehaviour
{
    public GameObject firstSelected;
    protected WindowManager manager;
    public void Init(WindowManager mgr)
    {
        manager = mgr;
    }
    public void OnFocus()
    {
        EventSystem.current.SetSelectedGameObject(firstSelected);
    }
    public virtual void Open()
    {
        if (gameObject.activeSelf)
        {
            return;
        }

        gameObject.SetActive(true);
        GetComponent<PanelOpenEffect>()?.PlayEffect();
        OnFocus();

        // 던전 윈도우가 열릴 때 튜토리얼 체크
        if (gameObject.name == "DunGeonWindow")
        {
            CheckDungeonTutorialAsync().Forget();
        }
    }

    private async UniTaskVoid CheckDungeonTutorialAsync()
    {
        // 던전 튜토리얼이 아직 완료되지 않았으면 튜토리얼 시작
        if (!DatabaseManager.Instance.IsTutorialSequenceCompleted("dungeon_tutorial"))
        {
            // 윈도우 열리는 애니메이션 대기
            await UniTask.Delay(TimeSpan.FromSeconds(0.3f), ignoreTimeScale: true);

            Debug.Log("[GenericWindow] 던전 튜토리얼 시작 - DUNGEON_TAB_FIRST_ENTER 조건 알림");
            TutorialManager.Instance?.NotifyConditionMet("DUNGEON_TAB_FIRST_ENTER");
        }
    }

    public virtual void Close()
    {
        gameObject.SetActive(false);
    }
}
