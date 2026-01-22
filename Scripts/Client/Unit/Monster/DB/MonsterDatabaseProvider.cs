#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class MonsterDatabaseProvider
{
    private static MonsterDatabase _cached;

    private const string DatabaseFolder = "Assets/Bear/ScriptableObjects/Database";
    private const string AssetPath = DatabaseFolder + "/MonsterDatabase.asset";
    public static MonsterDatabase Get()
    {
        if (null != _cached)
            return _cached;

        string[] guids = AssetDatabase.FindAssets("t:MonsterDatabase");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            _cached = AssetDatabase.LoadAssetAtPath<MonsterDatabase>(path);
            return _cached;
        }

        return CreateDatabase();
    }

    public static void Register(MonsterData data)
    {
        var db = Get();
        if (null == db) 
            return;

        db.Register(data);
        EditorUtility.SetDirty(db);
    }

    public static void Unregister(MonsterData data)
    {
        var db = Get();
        if (null == db || null == data) 
            return;

        db.Unregister(data);
        EditorUtility.SetDirty(db);
    }

    private static MonsterDatabase CreateDatabase()
    {
        FolderUtility.EnsureFolder(DatabaseFolder);

        var db = ScriptableObject.CreateInstance<MonsterDatabase>();
        AssetDatabase.CreateAsset(db, AssetPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        _cached = db;
        return db;
    }

}
#endif
