using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Grid
{
    public int unitId;
    public UnitGridData gridData;
}

// 테스트 스크립트
public class GridDatasForTesting : MonoBehaviour
{
    public Grid[] grids;

    public  Dictionary<int, UnitGridData> GridDatas => gridDatas;

    private Dictionary<int, UnitGridData> gridDatas = new Dictionary<int, UnitGridData>();


    private void Awake()
    {
        // 딕셔너리에 정보 담아두기 [id - unitGridData]
        for (int i = 0; i < grids.Length; i++)
        {
            gridDatas[grids[i].unitId] = grids[i].gridData;
        }
    }
}