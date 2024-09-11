﻿using RebuildSharedData.Data;
using System;

namespace RoRebuildServer.Simulation.Util
{
    public static class MathHelper
    {
        private static readonly float[] ResistTable;
        private static readonly float[] BoostTable;

        static MathHelper()
        {
            ResistTable = new float[1000];
            BoostTable = new float[1000];

            for (var i = 0; i < 1000; i++)
            {
                ResistTable[i] = MathF.Pow(0.99f, i);
                BoostTable[i] = MathF.Pow(0.01f, i);
            }
        }
        
        public static float ResistCalc(int value)
        {
            if(value < ResistTable.Length)
                return ResistTable[value];
            return MathF.Pow(0.99f, value);
        }

        public static float BoostCalc(int value)
        {
            if (value < BoostTable.Length)
                return BoostTable[value];
            return MathF.Pow(0.01f, value);
        }

        public static int Clamp(this int val, int min, int max)
        {
            if (val < min)
                val = min;
            else if (val > max)
                val = max;
            return val;
        }


        public static float Clamp(this float val, float min, float max)
        {
            if (val < min)
                val = min;
            else if (val > max)
                val = max;
            return val;
        }

        public static float Clamp01(this float val)
        {
            if (val < 0f)
                val = 0f;
            else if (val > 1f)
                val = 1f;
            return val;
        }

        public static float Lerp(float firstFloat, float secondFloat, float by)
        {
            return firstFloat * (1 - by) + secondFloat * by;
        }
    }
}
