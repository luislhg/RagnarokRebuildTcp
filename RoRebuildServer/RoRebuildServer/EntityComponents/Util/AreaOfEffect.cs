﻿using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.ObjectPool;
using RebuildSharedData.Data;
using RebuildSharedData.Enum;
using RebuildZoneServer.Networking;
using RoRebuildServer.EntitySystem;
using RoRebuildServer.Simulation;
using RoRebuildServer.Simulation.Util;

namespace RoRebuildServer.EntityComponents.Util;

public class AreaOfEffectPoolPolicy : IPooledObjectPolicy<AreaOfEffect>
{
    public AreaOfEffect Create()
    {
        return new AreaOfEffect();
    }

    public bool Return(AreaOfEffect obj)
    {
        obj.Reset();
        return true;
    }
}

public class AreaOfEffect
{
    public Entity SourceEntity = Entity.Null;
    public Area Area = Area.Zero;
    public Map CurrentMap = null!;
    public EntityList? TouchingEntities;

    public TargetingInfo TargetingInfo = new();
    public AoeType Type = AoeType.Inactive;
    public CharacterSkill SkillSource;

    public float NextTick = float.MaxValue;
    public float Expiration = float.MaxValue;

    public float TickRate = 99999;

    public int Value1 = 0;
    public int Value2 = 0;

    public bool IsActive = false;
    public bool CheckStayTouching = false;
    public bool TriggerOnFirstTouch = true;

    public void Init(WorldObject sourceCharacter, Area area, AoeType type, TargetingInfo targetingInfo, float duration, float tickRate, int value1, int value2)
    {
        Debug.Assert(sourceCharacter.Map != null);

        SourceEntity = sourceCharacter.Entity;
        CurrentMap = sourceCharacter.Map;
        Area = area;
        Type = type;
        TickRate = tickRate;
        Value1 = value1;
        Value2 = value2;
        TargetingInfo = targetingInfo;
        Expiration = Time.ElapsedTimeFloat + duration;
        NextTick = Time.ElapsedTimeFloat + tickRate;
        IsActive = true;
        CheckStayTouching = false;
        SkillSource = CharacterSkill.None;
        TriggerOnFirstTouch = type == AoeType.NpcTouch;
    }

    public void Reset()
    {
        SourceEntity = Entity.Null;
        IsActive = false;
        if(TouchingEntities != null)
            EntityListPool.Return(TouchingEntities);
        TouchingEntities = null;
    }

    //HasTouchedAoE checks if we are entering an aoe we were not previously in. If we are already in the aoe nothing happens.
    public bool HasTouchedAoE(Position initial, Position newPos)
    {
        if (Area.Contains(initial))
            return false;
        
        return Area.Contains(newPos);
    }

    public void OnAoETouch(WorldObject character)
    {
        if (character.Type == CharacterType.Player && Type == AoeType.NpcTouch)
        {
            if (SourceEntity.IsAlive() && SourceEntity.Type == EntityType.Npc)
            {
                SourceEntity.Get<Npc>().OnTouch(character.Entity.Get<Player>());
            }
        }

        if (Type == AoeType.DamageAoE && character.Type != CharacterType.NPC)
        {
            if (SourceEntity == character.Entity)
                return;
            if (!SourceEntity.TryGet<Npc>(out var npc))
                return;
            if (!TargetingInfo.SourceEntity.TryGet<CombatEntity>(out var attacker))
                return;
            if (!character.CombatEntity.IsValidTarget(attacker))
                return;
            if(TriggerOnFirstTouch)
                npc.Behavior.OnAoEInteraction(npc, character.CombatEntity, this);
            if (IsActive && CheckStayTouching && Area.Contains(character.Position)) //it might have moved so we check position again
            {
                if (TouchingEntities == null)
                    TouchingEntities = EntityListPool.Get();
                TouchingEntities.Add(character.Entity);
            }
        }
    }

    public void TouchEntitiesRemainingInAoE()
    {
        if (TouchingEntities == null)
            return;

        if (!SourceEntity.TryGet<Npc>(out var npc))
            return;
        
        TouchingEntities.ClearInactive();

        for (var i = 0; i < TouchingEntities.Count; i++)
        {
            var e = TouchingEntities[i];
            if (!e.IsAlive() || !e.TryGet<WorldObject>(out var ch))
                continue;
            if (CurrentMap != ch.Map || ch.Type == CharacterType.NPC)
                continue;
            if (!Area.Contains(ch.Position))
            {
                TouchingEntities.SwapFromBack(i);
                i--;
                continue;
            }
            
            npc.Behavior.OnAoEInteraction(npc, ch.CombatEntity, this);
            if (!IsActive)
                return; //if the aoe ends during our interaction event we should be ready
        }

        if (TouchingEntities.Count == 0)
        {
            EntityListPool.Return(TouchingEntities);
            TouchingEntities = null;
        }
    }
    
    public void Update()
    {
        if (Expiration < 0 || NextTick < 0)
            return;

        if (Time.ElapsedTimeFloat < NextTick)
            return;
        
        NextTick += TickRate;

        if (!SourceEntity.IsAlive())
            return;

        if (CheckStayTouching && TouchingEntities?.Count > 0)
            TouchEntitiesRemainingInAoE();
    }
}