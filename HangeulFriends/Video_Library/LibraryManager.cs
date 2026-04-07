using UnityEngine;

public class LibraryManager : MonoBehaviour
{
    private AudioSource audioSource;
    

    public AudioPlayer audioPlayer;

    public string currentFolder = "DefaultFolder";

    public string currentFileNameNoExt = "DefaultAudio";

    // 플레이 버튼용 함수
    public void PlayAudio()
    {
        StartCoroutine(audioPlayer.LoadAndPlay(currentFolder, currentFileNameNoExt));
    }

    // 다른 스크립트에서 폴더와 파일명을 설정할 때 사용할 함수
    public void SetAudioInfo(string folder, string fileNameNoExt)
    {
        currentFolder = folder;
        currentFileNameNoExt = fileNameNoExt;
    }
}
