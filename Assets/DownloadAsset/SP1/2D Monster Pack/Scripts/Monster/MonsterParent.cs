using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SP1Assets.MonsterPack2D
{
    public class MonsterParent : MonoBehaviour
    {
        List<MonsterPrefabController> _monsters = new List<MonsterPrefabController>();

        public void Init(float monsterWidth)
        {
            _monsters = GetComponentsInChildren<MonsterPrefabController>().ToList();
            foreach (var monster in _monsters)
                monster.Init();

            int monsterCount = _monsters.Count;

            if (monsterCount == 0)
            {
                return;
            }

            if (monsterCount == 1)
            {
                _monsters[0].transform.localPosition = Vector3.zero;
                return;
            }

            _monsters.Sort((a, b) => a.name.CompareTo(b.name));

            float childGap = monsterWidth / (monsterCount - 1);
            Vector3 leftEnd = Vector3.left * monsterWidth * 0.5f;

            for (int i = 0; i < monsterCount; i++)
                _monsters[i].transform.localPosition = leftEnd + Vector3.right * childGap * i;
        }

        public void ResetMonsters()
        {
            foreach (var monster in _monsters)
                monster.ResetPose();
        }

        public List<string> GetAnimationNames()
        {
            return _monsters.Count > 0 ? _monsters[0].GetAnimationNames() : null;
        }

        public void PlayAnimation(string animName)
        {
            foreach (var monster in _monsters)
                monster.PlayAnimation(animName, 0, 0);
        }
    }
}