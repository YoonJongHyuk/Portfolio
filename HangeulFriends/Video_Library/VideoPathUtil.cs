using System.IO;
using UnityEngine;
using UnityEngine.Video;

public static class VideoPathUtil
{
    public static string GetStreamingVideoUrl(string fileName)
    {
        var path = Path.Combine(Application.streamingAssetsPath, "Video", fileName);

        // URL엔 슬래시가 / 여야 안정적
        path = path.Replace("\\", "/");

        // Android는 Application.streamingAssetsPath 자체가 jar:file://... 로 시작함
        if (path.StartsWith("jar:") || path.StartsWith("http") || path.StartsWith("https"))
            return path;

        // Windows/Editor/Standalone 로컬 파일은 file:/// 스킴 권장
        return "file:///" + path;
    }
}
