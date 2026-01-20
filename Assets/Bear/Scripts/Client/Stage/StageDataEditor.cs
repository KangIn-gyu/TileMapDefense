#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[CustomEditor(typeof(StageData))]
public class StageDataEditor : Editor
{
    private TileType GetNextType(TileType current)
    {
        var values = (TileType[])System.Enum.GetValues(typeof(TileType));
        int index = System.Array.IndexOf(values, current);
        return values[(index + 1) % values.Length];
    }

    private string GetShortLabel(TileType type)
    {
        switch (type)
        {
            case TileType.None: return "None";
            case TileType.MeleeCharacterSpawnPoint: return "근접";
            case TileType.RangedCharacterSpawnPoint: return "원거리";
            case TileType.MonsterSpawnPoint: return "출발";
            case TileType.MonsterArrivalPoint: return "도착";
            default: return "?";
        }
    }

    private Color GetColor(TileType t)
    {
        return t switch
        {
            TileType.None => Color.gray,
            TileType.MeleeCharacterSpawnPoint => new Color(0.2f, 1f, 0.5f), 
            TileType.RangedCharacterSpawnPoint => new Color(0.2f, 1f, 0.5f),      
            TileType.MonsterSpawnPoint => new Color(1f, 0.4f, 0.4f),       
            TileType.MonsterArrivalPoint => new Color(0.4f, 0.7f, 1f),      
            _ => Color.white
        };
    }

    private void DrawBaseTilemapCells(StageData _stageData)
    {
        EditorGUILayout.LabelField("타일맵 정보", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_BaseTilemapWidth"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_BaseTilemapHeight"));
        EditorGUILayout.Space(10);

        int width = _stageData.m_BaseTilemapWidth;
        int height = _stageData.m_BaseTilemapHeight;

        for (int y = 0; y < height; y++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < width; x++)
            {
                int dataY = _stageData.m_BaseTilemapHeight - 1 - y;
                var cell = _stageData.GetBaseCellRaw(x, dataY);

                string label = cell.ToString();
                string shortLabel = GetShortLabel(cell);
                string fullLabel = cell.ToString();
                var prev = GUI.backgroundColor;
                GUI.backgroundColor = GetColor(cell);
                if (GUILayout.Button(new GUIContent(shortLabel, fullLabel), GUILayout.Width(40), GUILayout.Height(40)))
                {
                    Undo.RecordObject(_stageData, "Change Cell");
                    _stageData.SetBaseCellRaw(x, dataY, GetNextType(cell));
                    _stageData.RebuildMonsterSpawnPoints();
                    EditorUtility.SetDirty(_stageData);
                }
                GUI.backgroundColor = prev;
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(10);
    //    이거 나중에 빼야 됨.
    //    EditorGUILayout.LabelField("타입 디버그용", EditorStyles.boldLabel);
    //    SerializedProperty cellsProp = serializedObject.FindProperty("m_BaseCells");
    //    EditorGUILayout.PropertyField(cellsProp, true);

    }

    private void StageInformationUI()
    {
        EditorGUILayout.LabelField("스테이지 정보", EditorStyles.boldLabel);
        SerializedProperty PlayerLife = serializedObject.FindProperty("m_PlayerLife");
        EditorGUILayout.PropertyField(PlayerLife, true);

        SerializedProperty maxDeploy = serializedObject.FindProperty("m_MaxDeployableCharacters");
        maxDeploy.intValue = EditorGUILayout.IntSlider("Max Deployable Characters", maxDeploy.intValue, 1, 12);

        SerializedProperty operators = serializedObject.FindProperty("m_StageOperators");
        // 리스트 크기 강제 동기화
        int targetSize = maxDeploy.intValue;

        while (operators.arraySize > targetSize)
            operators.DeleteArrayElementAtIndex(operators.arraySize - 1);

        while (operators.arraySize < targetSize)
            operators.InsertArrayElementAtIndex(operators.arraySize);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Stage Operators", EditorStyles.boldLabel);

        for (int i = 0; i < operators.arraySize; i++)
        {
            EditorGUILayout.PropertyField(
                operators.GetArrayElementAtIndex(i),
                new GUIContent($"Operator {i + 1}")
            );
        }
    }

    private Texture2D GetSpritePreview(Sprite _sprite)
    {
        if (_sprite == null) return null;

        // 1) Read/Write 가능 텍스처 확보
        Texture2D tex = _sprite.texture;

#if UNITY_EDITOR
        string path = UnityEditor.AssetDatabase.GetAssetPath(tex);
        tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
#endif

        // 2) Sprite 영역 잘라내기
        Rect r = _sprite.textureRect;
        Texture2D newTex = new Texture2D((int)r.width, (int)r.height);
        newTex.SetPixels(tex.GetPixels((int)r.x, (int)r.y, (int)r.width, (int)r.height));
        newTex.Apply();

        return newTex;
    }

    private void DrawDecoTilemapCells(StageData data)
    {
        EditorGUILayout.LabelField("장식용 타일맵", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_DecoTilePos"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_DecorationTilemapWidth"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_DecorationTilemapHeight"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_TilePalette"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_DecorationTileList"));

        int width = data.m_DecorationTilemapWidth;
        int height = data.m_DecorationTilemapHeight;

        //   이거 나중에 빼야 됨.
        //   EditorGUILayout.LabelField("타입 디버그용", EditorStyles.boldLabel);
        //   EditorGUILayout.PropertyField(serializedObject.FindProperty("m_DecoCells"), true);

        // 타일 팔레트에 저장된 타일 베이스 자동 저장.
        if (data.m_TilePalette != data.m_LastTilePalette)
        {
            data.m_LastTilePalette = data.m_TilePalette;

            data.m_DecorationTileList ??= new List<TileBase>();
            data.m_DecorationTileList.Clear();

            data.m_DecorationTileList.Add(null);

            if (data.m_TilePalette != null)
            {
                var tiles = TilePaletteLoader.LoadTilesFromPalette(data.m_TilePalette);
                if (tiles != null)
                {
                    data.m_DecorationTileList.AddRange(tiles);
                }
            }

            EditorUtility.SetDirty(data);
        }

        for (int y = 0; y < height; y++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < width; x++)
            {
                int flippedY = height - 1 - y;
                int idx = data.GetDecoIndex(x, flippedY);
                
                if (idx < 0 || data.m_DecoCells == null || idx >= data.m_DecoCells.Length)
                {
                    GUILayout.Box("", GUILayout.Width(40), GUILayout.Height(40));
                    continue;
                }

                int tileIndex = data.m_DecoCells[idx];
                if (data.m_DecorationTileList == null || data.m_DecorationTileList.Count == 0)
                {
                    GUILayout.Box("", GUILayout.Width(40), GUILayout.Height(40));
                    continue;
                }
                tileIndex = Mathf.Clamp(tileIndex, 0, data.m_DecorationTileList.Count - 1);
                var tile = data.m_DecorationTileList[tileIndex];
                var sprites = TilePaletteLoader.GetTileSprite(data.m_TilePalette, data.m_DecorationTileList[tileIndex]);
                var tex = GetSpritePreview(sprites);
    
                if (GUILayout.Button(tex, GUILayout.Width(40), GUILayout.Height(40)))
                {
                    Undo.RecordObject(data, "Change Deco Cell");
             
                    if (data.m_DecorationTileList.Count > 0)
                    {
                        tileIndex = (tileIndex + 1) % data.m_DecorationTileList.Count;
                        data.m_DecoCells[idx] = tileIndex;
                    }
             
                    EditorUtility.SetDirty(data);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var data = (StageData)target;
        StageInformationUI();
        EditorGUILayout.Space(10);
        DrawBaseTilemapCells(data);
        EditorGUILayout.Space(10);
        SerializedProperty NoneTile = serializedObject.FindProperty("m_NoneTile");
        SerializedProperty RangedTile = serializedObject.FindProperty("m_RangedTile");
        SerializedProperty MeleeTile = serializedObject.FindProperty("m_MeleeTile");

        EditorGUILayout.PropertyField(NoneTile);
        EditorGUILayout.PropertyField(RangedTile);
        EditorGUILayout.PropertyField(MeleeTile);

        EditorGUILayout.Space(10);
        DrawDecoTilemapCells(data);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_MonsterSpawnPoints"));

        serializedObject.ApplyModifiedProperties();
    }

}
#endif