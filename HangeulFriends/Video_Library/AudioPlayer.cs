using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class AudioPlayer : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    // 0: 첫시작, 1: 일시정지, 2: 재생중
    [SerializeField] private int playState = 0;

    // AudioTimeBar에서 첫 재생 전 시작 위치를 저장해두는 변수
    public float pendingStartTime = 0f;


    public void PauseAudio()
    {
        if (playState != 2) return;
        audioSource.Pause();
        playState = 1;
    }

    public void ResumeAudio()
    {
        if (playState != 1) return;
        audioSource.UnPause();
        playState = 2;
    }

    public void LoopAudio(bool shouldLoop)
    {
        audioSource.loop = shouldLoop;
    }

    // 토글: 외부에서 버튼 하나로 재생/일시정지 제어할 때
    public void TogglePause()
    {
        if (playState == 2) PauseAudio();
        else if (playState == 1) ResumeAudio();
    }


    public IEnumerator LoadAndPlay(string folder, string fileNameNoExt)
    {
        if (playState == 1)
        {
            ResumeAudio();
            yield break;
        }
        else if (playState == 2)
        {
            PauseAudio();
            yield break;
        }
        else if (playState == 0)
        {
            playState = 1;
        }

        string fileName = fileNameNoExt.EndsWith(".mp3") ? fileNameNoExt : fileNameNoExt + ".mp3";
        string path = Path.Combine(Application.streamingAssetsPath, "Audio", folder, fileName);
        string url = ToStreamingAssetsUrl(path);

        Debug.Log($"[Audio] path={path}");
        Debug.Log($"[Audio] url ={url}");

        using (var req = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
        {
            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                Debug.LogError($"오디오 로드 실패 : {req.error}\nurl={url}");
                yield break;
            }

            var clip = DownloadHandlerAudioClip.GetContent(req);
            audioSource.clip = clip;

            // 저장된 시작 위치 적용 후 초기화 (pendingStartTime은 0~1 normalized 값)
            if (pendingStartTime > 0f)
            {
                audioSource.time = pendingStartTime * clip.length;
                pendingStartTime = 0f;
            }

            audioSource.Play();
            playState = 2;

            // 🔽 재생 종료까지 대기
            yield return new WaitUntil(() =>
                !audioSource.loop &&
                audioSource.clip != null &&
                audioSource.time >= audioSource.clip.length - 0.05f
            );
            // 🔽 재생 완료
            playState = 0;
            Debug.Log("오디오 재생 완료");

        }
    }

    private string ToStreamingAssetsUrl(string path)
    {
        if (path.Contains("://") || path.Contains("jar:"))
            return path.Replace("\\", "/");

        return new Uri(path.Replace("\\", "/")).AbsoluteUri;
    }
}