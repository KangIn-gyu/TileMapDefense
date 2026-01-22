#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class OperatorDatabaseProvider
{
   private static OperatorDatabase _cached;

   private const string DatabaseFolder = "Assets/Bear/ScriptableObjects/Database";
   private const string AssetPath = DatabaseFolder + "/OperatorDatabase.asset";
   public static OperatorDatabase Get()
   {
       if (null != _cached)
           return _cached;

       string[] guids = AssetDatabase.FindAssets("t:OperatorDatabase");
       if (guids.Length > 0)
       {
           string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            _cached = AssetDatabase.LoadAssetAtPath<OperatorDatabase>(path);
           return _cached;
       }

       return CreateDatabase();
   }

   public static void Register(OperatorData data)
   {
       var db = Get();
       if (null == db)
           return;

       db.Register(data);
       EditorUtility.SetDirty(db);
   }

   public static void Unregister(OperatorData data)
   {
       var db = Get();
       if (null == db || null == data)
           return;

       db.Unregister(data);
       EditorUtility.SetDirty(db);
   }

   private static OperatorDatabase CreateDatabase()
   {
       FolderUtility.EnsureFolder(DatabaseFolder);

       var db = ScriptableObject.CreateInstance<OperatorDatabase>();
       AssetDatabase.CreateAsset(db, AssetPath);

       AssetDatabase.SaveAssets();
       AssetDatabase.Refresh();

        _cached = db;
       return db;
   }
}
#endif