using Cysharp.Threading.Tasks;
using Firebase.Database;
using UnityEditor.Analytics;
using UnityEngine;

public class UserDataManager : MonoBehaviour
{
    private static UserDataManager instance;
    public static UserDataManager Instance => instance;

    private DatabaseReference databaseRef;
    private DatabaseReference usersRef;

    private UserData cachedProfile; //메모리에 올라갈 프로파일

    public UserData CachedProfile => cachedProfile;

    private void Awake()
    {
        {
            if(instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    private async UniTaskVoid Start()
    {
        await FirebaseInitializer.Instance.WaitForInitializationAsync();

        databaseRef = FirebaseDatabase.DefaultInstance.RootReference;
        usersRef = databaseRef.Child("users"); //하위에 입력되어있는 데이터가 리턴되는 함수다. / root : json 통째로, users : users라고 적혀있는 내부 데이터만

        Debug.Log("[Profile] ProfileManager 초기화 완료");
    }
    
}
