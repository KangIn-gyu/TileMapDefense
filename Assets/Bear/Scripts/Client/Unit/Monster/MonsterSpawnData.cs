using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct MonsterSpawnEntry
{
    public string MonsterID;  // 어떤 몬스터인가
    public float Delay;       // 이전 소환 후 대기 시간(초)
}

[CreateAssetMenu(fileName = "MonsterSpawnData", menuName = "Scriptable Objects/MonsterSpawnData")]
public class MonsterSpawnData : ScriptableObject
{
    [Tooltip("소환 순서 정의")]
    public List<MonsterSpawnEntry> SpawnList = new();

    public float DelayTime = 0f;
}
