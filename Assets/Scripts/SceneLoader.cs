using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public async UniTaskVoid LoadSceneAsync(string sceneName)
    {
        await SceneManager.LoadSceneAsync(sceneName);
    }
}
