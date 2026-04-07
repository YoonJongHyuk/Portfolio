using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class StarCountEvent : MonoBehaviour
{

    public int starCount = 3; // 영상 결과창에서 사용할 별 개수
    [SerializeField] private string nextSceneName;

    public enum QuizCountungType
    {
        Counting, // 별점 매기는 문제
        NoCounting // 별점 매기지 않는 문제
    }

    [SerializeField] private QuizCountungType quizCountungType;

    public void StartEndEvent()
    {
        Debug.Log("StartEndEvent 시작!");
        StartCoroutine(IEndEvent());
    }
    
    public void FinishEvent()
    {
        StartCoroutine(IFinishEvent());
    }

    IEnumerator IFinishEvent()
    {
        Debug.LogError($"최종 별 갯수: {starCount}");

        if(StudyManager.Instance != null)
        {
            StudyManager.Instance.CompleteStep(StudyManager.Instance.currentStep);         
        }
        yield return new WaitForSeconds(0.1f);
        // 해창씨 코드를 여기다 두면 될듯
        CallResult.Save($"{SceneManager.GetActiveScene().name}", starCount, nextSceneName, true, true);
        // nextSceneName 은 인스펙터에서 세팅. VideoPlay 는 대체로 VideoManager 라는 오브젝트에 붙어있다.
    }
    

    IEnumerator IEndEvent()
    {
        Debug.Log($"최종 별 갯수: {starCount}");
        if(StudyManager.Instance != null)
        {
            StudyManager.Instance.CompleteStep(StudyManager.Instance.currentStep);
        }
        yield return new WaitForSeconds(0.1f);
        // 해창씨 코드를 여기다 두면 될듯
        CallResult.Save($"{SceneManager.GetActiveScene().name}", starCount, nextSceneName);
        // nextSceneName 은 인스펙터에서 세팅. VideoPlay 는 대체로 VideoManager 라는 오브젝트에 붙어있다.
    }



    public void DecrementStarCount()
    {
        if (starCount > 1)
        {
            starCount--;
        }
        else
        {
            starCount = 1; // 최소 별 개수는 1로 유지
        }
        Debug.Log($"현재 별 갯수: {starCount}");
    }
}
