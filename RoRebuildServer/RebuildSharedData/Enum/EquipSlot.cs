﻿using System;
using System.Collections.Generic;
using System.Text;

namespace RebuildSharedData.Enum;

[Flags]
public enum HeadSlots
{
    Top = 1,
    Mid = 2,
    Bottom = 4
}

public enum EquipSlot
{
    HeadTop,
    HeadMid,
    HeadBottom,
    Body,
    LeftHand,
    RightHand,
    Garment,
    Footgear,
    Accessory1,
    Accessory2,
    CostumeTop,
    CostumeMid,
    CostumeBottom
}