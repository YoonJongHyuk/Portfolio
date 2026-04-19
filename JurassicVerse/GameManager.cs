using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        // 이미 인스턴스가 존재하면 중복 파괴
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // 씬 이동해도 유지
    }

    public int currentdinosaurnum = 0;

    private DinosaurFactory dinosaurFactory;


    public void SetDinosaurFactory()
    {
        DinosaurFactory factory = GameObject.FindAnyObjectByType<DinosaurFactory>();

        if (factory == null)
        {
            Debug.LogWarning("factory 찾지 못했습니다. 무시하고 넘어갑니다.");
            return; // 아무것도 하지 않음
        }

        dinosaurFactory = factory;
        // 찾았을 경우 추가 작업 가능
        Debug.Log("factory 연결 완료!");

        dinosaurFactory.InstanteDinosaurEvent(currentdinosaurnum);
    }


    public void MoveScene(string scene_name)
    {
        StartCoroutine(LoadSceneAsyncExample(scene_name));
    }

    IEnumerator LoadSceneAsyncExample(string scene_name)
    {

        AsyncOperation op = SceneManager.LoadSceneAsync(scene_name);

        while (!op.isDone)
        {
            Debug.Log($"로딩 중... {op.progress}");

            // 로딩 완료
            if (op.progress >= 0.9f)
            {
                Debug.Log("씬 로딩 완료 직전!");
                // 자동 씬 활성화 (기본값 true)
            }

            yield return null;
        }

        Debug.Log("씬 로딩 완료!");

        SetDinosaurFactory();
    }

}
