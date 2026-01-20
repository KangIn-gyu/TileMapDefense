#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(OperatorData))]
public class OperatorDataEditor : Editor
{
    private const int GridSize = 7;
    private const int Center = GridSize / 2;
    private const float ButtonSize = 32f;

    private void SortAttackOffsets(List<Vector2Int> list)
    {
        list.Sort((a, b) =>
        {
            int distA = Mathf.Abs(a.x) + Mathf.Abs(a.y);
            int distB = Mathf.Abs(b.x) + Mathf.Abs(b.y);

            if (distA != distB)
                return distA.CompareTo(distB);

            if (a.y != b.y)
                return b.y.CompareTo(a.y); // 위쪽 먼저 (원하면 반대로)

            return a.x.CompareTo(b.x);     // 왼쪽 → 오른쪽
        });
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GUILayout.Space(10);

        OperatorData data = (OperatorData)target;
        if (null == data.m_AttackOffsets)
            data.m_AttackOffsets = new List<Vector2Int>();

        GUILayout.Label("공격 범위 설정 (7x7)", EditorStyles.boldLabel);

        Color defaultColor = GUI.backgroundColor;

        for (int y = 0; y < GridSize; y++)
        {
            GUILayout.BeginHorizontal();
            for (int x = 0; x < GridSize; x++)
            {
                Vector2Int offset = new Vector2Int(x - Center, Center - y);

                bool isCenter = offset == Vector2Int.zero;
                bool isSelected = data.m_AttackOffsets.Contains(offset);

                if (true == isCenter)
                {
                    GUI.backgroundColor = Color.green;
                }
                else if (true == isSelected)
                {
                    GUI.backgroundColor = Color.red;
                }
                else
                {
                    GUI.backgroundColor = defaultColor;
                }

                if (GUILayout.Button("", GUILayout.Width(ButtonSize), GUILayout.Height(ButtonSize)))
                {
                    if (false == isCenter)
                    {
                        if (true == isSelected)
                        {
                            data.m_AttackOffsets.Remove(offset);
                        }
                        else
                        {
                            data.m_AttackOffsets.Add(offset);
                        }

                        SortAttackOffsets(data.m_AttackOffsets);
                        EditorUtility.SetDirty(data);
                    }
                }
            }
            GUILayout.EndHorizontal();
        }

        GUI.backgroundColor = defaultColor;
    }
}
#endif
