using Cysharp.Threading.Tasks;
using UnityEngine;

public class LoadTest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    async UniTaskVoid Start()
    {
        await DataTableManager.InitAsync();
        Debug.Log(DataTableManager.Get<DataTable_Stage>(DataTableIds.Stage).Get(701).STAGE_NAME);
        int stage = 701;
        int wave = int.Parse("8" + stage + 1 + 1);
        Debug.Log(DataTableManager.Get<DataTable_Wave>(DataTableIds.Wave).Get(wave).SPAWN_MON1_ID);
        int monsterId = DataTableManager.Get<DataTable_Wave>(DataTableIds.Wave).Get(wave).SPAWN_MON1_ID;
        Debug.Log(DataTableManager.Get<DataTable_Monster>(DataTableIds.Monster).Get(monsterId).MON_NAME);
    }

   
}
