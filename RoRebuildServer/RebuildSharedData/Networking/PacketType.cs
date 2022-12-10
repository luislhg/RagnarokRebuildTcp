﻿namespace RebuildSharedData.Networking;

public class ServerOnlyPacketAttribute : Attribute
{

}

public enum PacketType : byte
{
    [ServerOnlyPacket] ConnectionApproved,
    [ServerOnlyPacket] ConnectionDenied,
    PlayerReady,
    EnterServer,
    Ping,
    [ServerOnlyPacket] CreateEntity,
    StartMove,
    [ServerOnlyPacket] Move,
    Attack,
    [ServerOnlyPacket] TakeDamage,
    LookTowards,
    SitStand,
    [ServerOnlyPacket] RemoveEntity,
    [ServerOnlyPacket] RemoveAllEntities,
    Disconnect,
    [ServerOnlyPacket] ChangeMaps,
    StopAction,
    StopImmediate,
    RandomTeleport,
    UnhandledPacket,
    [ServerOnlyPacket] HitTarget,
    Skill,
    ChangeTarget,
    [ServerOnlyPacket] GainExp,
    [ServerOnlyPacket] LevelUp,
    [ServerOnlyPacket] Death,
    [ServerOnlyPacket] HpRecovery,
    Respawn,
    [ServerOnlyPacket] RequestFailed,
    [ServerOnlyPacket] Targeted,
    Say,
    ChangeName,
    [ServerOnlyPacket] Resurrection,
    UseInventoryItem,
    [ServerOnlyPacket] EffectOnCharacter,
    [ServerOnlyPacket] EffectAtLocation,
    
    AdminRequestMove,
    AdminServerAction,
    AdminLevelUp,
    AdminEnterServerSpecificMap,
    AdminRandomizeAppearance,

    NpcClick,
    [ServerOnlyPacket] NpcInteraction,
    NpcAdvance,
    NpcSelectOption,
    
}

public enum MessageType : byte
{
    Local,
    MapWide,
    WorldWide,
    Server,
    Party,
    DirectMessage
}

public enum AdminAction : byte
{
    ForceGC,
    ReloadScripts
}

public enum NpcInteractionType
{
    NpcFocusNpc,
    NpcDialog,
    NpcOption,
    NpcEndInteraction,
    NpcShowSprite,
}