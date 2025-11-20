using UnityEngine;

public class MainUISceneManager : MonoBehaviour
{
    public GameObject[] unitPrefabs;
    public RectTransform moveArea;
    public int spawnCount = 4;

    private void Start()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            var prefab = unitPrefabs[Random.Range(0, unitPrefabs.Length)];
            var unit = Instantiate(prefab, transform);
            var mover = unit.GetComponent<WindowUnitMovement>();
            mover.moveArea = moveArea;
        }
    }
}
