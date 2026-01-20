#if UNITY_EDITOR
using System.IO;
using UnityEditor;

public static class FolderUtility
{
    public static void EnsureFolder(string fullPath)
    {
        if (AssetDatabase.IsValidFolder(fullPath))
            return;

        string parent = Path.GetDirectoryName(fullPath);
        string folderName = Path.GetFileName(fullPath);

        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, folderName);
    }
}
#endif