#if UNITY_EDITOR
using UnityEditor;

public class MonsterDataDeletionProcessor : AssetModificationProcessor
{
    static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
    {
        var monsterData = AssetDatabase.LoadAssetAtPath<MonsterData>(assetPath);

        if (monsterData == null)
            return AssetDeleteResult.DidNotDelete;

        MonsterDatabaseProvider.Unregister(monsterData);

        return AssetDeleteResult.DidNotDelete;
    }
}
#endif
