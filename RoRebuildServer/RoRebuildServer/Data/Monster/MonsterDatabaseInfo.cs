﻿using RebuildSharedData.Enum.EntityStats;

namespace RoRebuildServer.Data.Monster;

public class MonsterDatabaseInfo
{
    public int Id { get; set; }
    public int Level { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public int ScanDist { get; set; }
    public int ChaseDist { get; set; }
    public int HP { get; set; }
    public int Exp { get; set; }
    public int AtkMin { get; set; }
    public int AtkMax { get; set; }
    public int Str { get; set; }
    public int Agi { get; set; }
    public int Vit { get; set; }
    public int Int { get; set; }
    public int Dex { get; set; }
    public int Luk { get; set; }
    public int Def { get; set; }
    public int MDef { get; set; }
    public float RechargeTime { get; set; }
    public float AttackLockTime { get; set; }
    public float HitTime { get; set; }
    public float AttackDamageTiming { get; set; }
    public int Range { get; set; }
    public CharacterElement Element { get; set; }
    public CharacterRace Race { get; set; }
    public MonsterAiType AiType { get; set; }
    public List<MonsterSpawnMinions>? Minions { get; set; }
    public float MoveSpeed { get; set; }

    public bool IsElementBaseType(CharacterElement targetType)
    {
        return Element >= targetType && Element <= targetType + 3; //checks if element 1-4 is equal to targetType (assuming we pass element 1)
    }
}