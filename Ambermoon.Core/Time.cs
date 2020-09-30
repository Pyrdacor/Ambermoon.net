using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using System;

namespace Ambermoon
{
    internal class Time
    {
        readonly Savegame savegame;
        DateTime lastTickTime = DateTime.Now;
        uint currentMoveTicks = 0;

        public uint Year => savegame.Year;
        public uint Month => savegame.Month;
        public uint DayOfMonth => savegame.DayOfMonth;
        public uint Hour => savegame.Hour;
        public uint Minute => savegame.Minute;
        public uint TimeSlot => savegame.Minute / 5;

        public Time(Savegame savegame)
        {
            this.savegame = savegame;
        }

        public void Update()
        {
            if (DateTime.Now - lastTickTime > TimeSpan.FromSeconds(10))
                Tick();
        }

        public void ResetTickTimer()
        {
            lastTickTime = DateTime.Now;
        }

        public void Tick()
        {
            savegame.Minute += 5;

            if (savegame.Minute >= 60)
            {
                savegame.Minute = 0;
                ++savegame.Hour;
                PostIncreaseUpdate();
            }

            currentMoveTicks = 0;
            ResetTickTimer();
        }

        public void MoveTick(Map map, TravelType travelType)
        {
            ++currentMoveTicks;

            if (map.Type == MapType.Map3D)
            {
                if (currentMoveTicks == 5)
                    Tick();
            }
            else if (map.Flags.HasFlag(MapFlags.Indoor))
            {
                if (currentMoveTicks == 5)
                    Tick();
            }
            else
            {
                uint stepsPerTick = travelType switch
                {
                    TravelType.Walk => 2,
                    TravelType.Horse => 3,
                    TravelType.Raft => 2,
                    TravelType.Ship => 4,
                    TravelType.MagicalDisc => 2,
                    TravelType.Eagle => 6,
                    TravelType.Fly => 256,
                    TravelType.Swim => 2,
                    TravelType.WitchBroom => 4,
                    TravelType.SandLizard => 3,
                    TravelType.SandShip => 4,
                    _ => 2
                };

                if (currentMoveTicks == stepsPerTick)
                    Tick();
            }
        }

        public void Wait(uint hours)
        {
            savegame.Hour += hours;
            PostIncreaseUpdate();
            ResetTickTimer();
        }

        public int GetDifferenceInHours(Time other)
        {
            int years = (int)Year - (int)other.Year;
            int month = (int)Month - (int)other.Month;
            int days = (int)DayOfMonth - (int)other.DayOfMonth;
            int hours = (int)Hour - (int)other.Hour;

            return years * 12 * 31 * 24 + month * 31 * 24 + days * 24 + hours;
        }

        void PostIncreaseUpdate()
        {
            while (savegame.Hour >= 24)
            {
                savegame.Hour -= 24;
                ++savegame.DayOfMonth;

                if (savegame.DayOfMonth > 31)
                {
                    savegame.DayOfMonth = 1;
                    ++savegame.Month;

                    if (savegame.Month > 12)
                    {
                        savegame.Month = 1;
                        ++savegame.Year;

                        if (savegame.Year > 0xffff)
                            savegame.Year = 0;
                    }
                }
            }
        }
    }
}
