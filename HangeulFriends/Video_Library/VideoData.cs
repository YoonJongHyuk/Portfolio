using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class VideoData
{
    [Header("Video")]
    public string videoTitle;
    public string fileName;
    public float videoLength; // 영상 길이 (초 단위)

    [Header("Quiz Timing")]
    public QuizTiming quizTiming; // ✅ 영상별 퀴즈 타이밍
}
