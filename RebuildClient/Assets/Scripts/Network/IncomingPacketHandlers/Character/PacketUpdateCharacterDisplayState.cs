﻿using Assets.Scripts.Network.HandlerBase;
using Assets.Scripts.Sprites;
using RebuildSharedData.Enum;
using RebuildSharedData.Networking;
using UnityEngine;

namespace Assets.Scripts.Network.IncomingPacketHandlers.Character
{
    [ClientPacketHandler(PacketType.UpdateCharacterDisplayState)]
    public class PacketUpdateCharacterDisplayState : ClientPacketHandlerBase
    {
        public override void ReceivePacket(ClientInboundMessage msg)
        {
            var id = msg.ReadInt32();
            
            if (!Network.EntityList.TryGetValue(id, out var controllable))
                return;

            var headUpper = msg.ReadInt32();
            var headMid = msg.ReadInt32();
            var headLower = msg.ReadInt32();
            var weapon = msg.ReadInt32();
            var shield = msg.ReadInt32();
            controllable.WeaponClass = msg.ReadInt32();
            
            ClientDataLoader.Instance.LoadAndAttachEquipmentSprite(controllable, headUpper, EquipPosition.HeadUpper, 4);
            ClientDataLoader.Instance.LoadAndAttachEquipmentSprite(controllable, headMid, EquipPosition.HeadMid, 3);
            ClientDataLoader.Instance.LoadAndAttachEquipmentSprite(controllable, headLower, EquipPosition.HeadLower, 2);
            ClientDataLoader.Instance.LoadAndAttachWeapon(controllable, weapon);
            ClientDataLoader.Instance.LoadAndAttachEquipmentSprite(controllable, shield, EquipPosition.Shield, 4);
            
            Debug.Log($"Updating appearance data. New weapon class: {controllable.WeaponClass}");
        }
    }
}