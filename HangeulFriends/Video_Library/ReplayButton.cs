using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ReplayButton : MonoBehaviour
{
    [SerializeField] private AudioPlayer audioPlayer;
    [SerializeField] private VideoPlay videoPlay;

    [SerializeField] private bool isReplay = false;

    [SerializeField] private List<GameObject> replayPanels;
    [SerializeField] private int currentReplayIndex = 0;

    public void OnReplayButtonClicked()
    {
        isReplay = !isReplay;
        currentReplayIndex = isReplay ? currentReplayIndex + 1 : 0;
        if(audioPlayer != null)
        {
            audioPlayer.LoopAudio(isReplay);
            replayPanels[0].SetActive(!isReplay);
            replayPanels[1].SetActive(isReplay);
            TMP_Text replayIndexText = replayPanels[2].GetComponent<TMP_Text>();
            replayIndexText.text = currentReplayIndex.ToString();
        }
    }
}
