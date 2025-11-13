using UnityEngine;

[CreateAssetMenu(fileName = "SampleItems", menuName = "Scriptable Objects/SampleItems")]
public class SampleItems : ScriptableObject
{
    public int unitId;
    public float probability;
    public int weight;
}
