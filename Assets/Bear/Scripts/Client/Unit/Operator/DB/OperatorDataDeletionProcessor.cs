#if UNITY_EDITOR
using UnityEditor;

public class OperatorDataDeletionProcessor : AssetModificationProcessor
{
    static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
    {
        var operatorData = AssetDatabase.LoadAssetAtPath<OperatorData>(assetPath);

        if (operatorData == null)
            return AssetDeleteResult.DidNotDelete;

        OperatorDatabaseProvider.Unregister(operatorData);

        return AssetDeleteResult.DidNotDelete;
    }
}
#endif