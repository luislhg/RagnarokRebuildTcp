﻿using RebuildSharedData.Data;
using RebuildSharedData.Enum;
using RebuildSharedData.Networking;
using RoRebuildServer.EntityComponents.Character;

namespace RoRebuildServer.Networking.PacketHandlers;

[ClientPacketHandler(PacketType.AdminChangeAppearance)]
public class PacketAdminChangeAppearance : IClientPacketHandler
{
    public void Process(NetworkConnection connection, InboundMessage msg)
    {
        if (connection.Character == null || !connection.Character.IsActive || connection.Character.Map == null
            || !connection.Entity.IsAlive() || connection.Character.State == CharacterState.Dead)
            return;

        var p = connection.Player;

        var id = msg.ReadInt32();
        var val = msg.ReadInt32();
        
        switch (id)
        {
            default:
            case 0:
                p.SetData(PlayerStat.Head, GameRandom.NextInclusive(0, 31));
                p.SetData(PlayerStat.Gender, GameRandom.NextInclusive(0, 1));
                break;
            case 1:
                if(val >= 0 && val <= 31)
                    p.SetData(PlayerStat.Head, val);
                else
                    p.SetData(PlayerStat.Head, GameRandom.NextInclusive(0, 31));
                break;
            case 2:
                if(val >= 0 && val <= 1)
                    p.SetData(PlayerStat.Gender, val);
                else
                    p.SetData(PlayerStat.Gender, GameRandom.NextInclusive(0, 1));
                break;
            case 3:
                if (val >= 0 && val <= 6)
                    p.ChangeJob(val);
                else
                    p.ChangeJob(GameRandom.Next(0, 6));
                return; //return as we don't want to double refresh (change job will refresh)
        }

        connection.Character.Map.RefreshEntity(p.Character);
        p.UpdateStats();
    }
}