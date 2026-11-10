using System.Collections;
using UnityEngine;

public static class JsonDataLoader
{
    public static TextAsset[] LoadAll(string filePath)
    {
        return Resources.LoadAll<TextAsset>(filePath);
    }

    public static IEnumerator LoadText(string fileName, System.Action<string> onLoaded)
    {
        TextAsset textAsset = Resources.Load<TextAsset>($"JSON/{fileName}");
        if (textAsset == null) Debug.LogError($"JSON 파일을 찾을 수 없습니다: JSON/{fileName}");
        onLoaded?.Invoke(textAsset != null ? textAsset.text : null);
        yield break;
    }
}
