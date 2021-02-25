using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using System;

namespace Ambermoon
{
    public enum DayTime
    {
        Night,
        Dusk,
        Day,
        Dawn
    }

    internal interface ITime
    {
        uint Year { get; }
        uint Month { get; }
        uint DayOfMonth { get; }
        uint Hour { get; }
        uint Minute { get; }
        uint TimeSlot { get; }

        public static Time operator +(ITime time, uint minutes)
        {
            var newTime = new Time(time);
            newTime.AddMinutes(minutes);
            return newTime;
        }
    }

    internal class Time : ITime
    {
        public uint Year { get; private set; }
        public uint Month { get; private set; }
        public uint DayOfMonth { get; private set; }
        public uint Hour { get; private set; }
        public uint Minute { get; private set; }
        public uint TimeSlot => (Hour * 60 + Minute) / 5;

        public Time()
        {

        }

        public Time(ITime time)
        {
            Year = time.Year;
            Month = time.Month;
            DayOfMonth = time.DayOfMonth;
            Hour = time.Hour;
            Minute = time.Minute;
        }

        public int GetDifferenceInHours(ITime other)
        {
            int years = (int)Year - (int)other.Year;
            int month = (int)Month - (int)other.Month;
            int days = (int)DayOfMonth - (int)other.DayOfMonth;
            int hours = (int)Hour - (int)other.Hour;

            return years * 12 * 31 * 24 + month * 31 * 24 + days * 24 + hours;
        }

        public void AddHours(uint hours)
        {
            if (hours > 24)
                throw new AmbermoonException(ExceptionScope.Application, "Max 24 hours can be added at once.");

            Hour += hours;

            if (Hour >= 24)
            {
                Hour -= 24;

                if (++DayOfMonth == 31)
                {
                    DayOfMonth = 0;

                    if (Month == 13)
                    {
                        Month = 0;
                        ++Year;
                    }
                }
            }
        }

        public void AddMinutes(uint minutes)
        {
            if (minutes % 5 != 0)
                throw new AmbermoonException(ExceptionScope.Application, "Only 5-minute intervals can be used.");

            if (minutes >= 60)
            {
                AddHours(minutes / 60);
                minutes %= 60;
            }

            Minute += minutes;

            if (Minute >= 60)
            {
                Minute -= 60;

                if (++Hour == 24)
                {
                    Hour = 0;

                    if (++DayOfMonth == 31)
                    {
                        DayOfMonth = 0;

                        if (Month == 13)
                        {
                            Month = 0;
                            ++Year;
                        }
                    }
                }
            }
        }
    }

    internal class SavegameTime : ITime
    {
        readonly Savegame savegame;
        DateTime lastTickTime = DateTime.Now;
        uint currentMoveTicks = 0;

        public uint Year => savegame.Year;
        public uint Month => savegame.Month;
        public uint DayOfMonth => savegame.DayOfMonth;
        public uint Hour => savegame.Hour;
        public uint Minute => savegame.Minute;
        public uint TimeSlot => (savegame.Hour * 60 + savegame.Minute) / 5;
        public uint HoursWithoutSleep
        {
            get => savegame.HoursWithoutSleep;
            set => savegame.HoursWithoutSleep = value;
        }

        public SavegameTime(Savegame savegame)
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
                ++savegame.HoursWithoutSleep;
                PostIncreaseUpdate();
            }

            currentMoveTicks = 0;
            ResetTickTimer();
            HandleTimePassed(0, 5);
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

        public event Action GotTired;
        public event Action GotExhausted;
        public event Action NewDay;
        public event Action NewYear;

        public void Wait(uint hours)
        {
            savegame.HoursWithoutSleep += hours;
            savegame.Hour += hours;
            PostIncreaseUpdate();
            ResetTickTimer();
            HandleTimePassed(hours, 0);
        }

        void PostIncreaseUpdate()
        {
            while (savegame.Hour >= 24)
            {
                savegame.Hour -= 24;
                ++savegame.DayOfMonth;
                NewDay?.Invoke();

                if (savegame.DayOfMonth > 31)
                {
                    savegame.DayOfMonth = 1;
                    ++savegame.Month;

                    if (savegame.Month > 12)
                    {
                        savegame.Month = 1;
                        ++savegame.Year;
                        NewYear?.Invoke();

                        if (savegame.Year > 0xffff)
                            savegame.Year = 0;
                    }
                }
            }

            if (savegame.HoursWithoutSleep >= 36)
                GotExhausted?.Invoke();
            else if (savegame.HoursWithoutSleep >=24)
                GotTired?.Invoke();
        }

        void HandleTimePassed(uint passedHours, uint passedMinutes)
        {
            uint passed5MinuteChunks = passedHours / 12 + passedMinutes / 5;

            foreach (var activeSpellType in Enum.GetValues<ActiveSpellType>())
            {
                var activeSpell = savegame.ActiveSpells[(int)activeSpellType];

                if (activeSpell != null)
                {
                    if (activeSpell.Duration <= passed5MinuteChunks)
                        savegame.ActiveSpells[(int)activeSpellType] = null;
                    else
                        activeSpell.Duration -= passed5MinuteChunks;
                }
            }
        }
    }

    internal static class TimeExtensions
    {
        public static DayTime GetDayTime(this ITime time)
        {
            // 6-8 -> Dusk
            // 8-18 -> Day
            // 18-20 -> Dawn
            // 20-6 -> Night

            if (time.Hour < 6 || time.Hour >= 20)
                return DayTime.Night;
            else if (time.Hour < 8)
                return DayTime.Dusk;
            else if (time.Hour < 18)
                return DayTime.Day;
            else
                return DayTime.Dawn;
        }

        public static int CombatBackgroundPaletteIndex(this ITime time)
        {
            /// 3 palettes for daylight (07:00-18:59),
            /// twilight (05:00-06:59, 19:00-20:59)
            /// and night (21:00-04:59) in that order.
            if (time.Hour >= 7 && time.Hour < 19)
                return 0;
            if (time.Hour >= 21 || time.Hour < 5)
                return 2;
            return 1;
        }
    }
}
