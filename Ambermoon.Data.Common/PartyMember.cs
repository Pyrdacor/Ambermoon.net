namespace Ambermoon.Data
{
    public class PartyMember : Character
    {
        public ushort MarkOfReturnMapIndex { get; set; }
        public ushort MarkOfReturnX { get; set; }
        public ushort MarkOfReturnY { get; set; }

        private PartyMember()
            : base(CharacterType.PartyMember)
        {

        }

        public static PartyMember Create(string name, ushort portraitIndex, Gender gender)
        {
            var hero = new PartyMember
            {
                Name = name,
                Gender = gender,
                PortraitIndex = portraitIndex,
                Race = Race.Human,
                Class = Class.Adventurer,
                Level = 1,
                ExperiencePoints = 75,
                SpellLearningPoints = 5,
                TrainingPoints = 6,
                Gold = 16,
                SpokenLanguages = Language.Human,
                AttacksPerRound = 1,
                SpellMastery = SpellTypeMastery.Alchemistic
            };

            hero.HitPoints.CurrentValue = hero.HitPoints.MaxValue = 10;
            hero.SpellPoints.CurrentValue = hero.SpellPoints.MaxValue = 8;
            
            // TODO: per level values

            void SetAttribute(Attribute attribute, uint current, uint max)
            {
                hero.Attributes[attribute].CurrentValue = current;
                hero.Attributes[attribute].MaxValue = max;
            }

            SetAttribute(Attribute.Age, 17, 99); // TODO: is 99 correct?
            SetAttribute(Attribute.Strength, 33, 50);
            SetAttribute(Attribute.Intelligence, 27, 50);
            SetAttribute(Attribute.Dexterity, 34, 50);
            SetAttribute(Attribute.Speed, 21, 50);
            SetAttribute(Attribute.Stamina, 48, 50);
            SetAttribute(Attribute.Charisma, 41, 50);
            SetAttribute(Attribute.Luck, 16, 50);
            SetAttribute(Attribute.AntiMagic, 0, 0);

            void SetAbility(Ability ability, uint current, uint max)
            {
                hero.Abilities[ability].CurrentValue = current;
                hero.Abilities[ability].MaxValue = max;
            }

            SetAbility(Ability.Attack, 42, 80);
            SetAbility(Ability.Parry, 19, 75);
            SetAbility(Ability.Swim, 1, 95);
            SetAbility(Ability.CriticalHit, 0, 0);
            SetAbility(Ability.FindTraps, 0, 25);
            SetAbility(Ability.DisarmTraps, 0, 25);
            SetAbility(Ability.LockPicking, 0, 25);
            SetAbility(Ability.Searching, 0, 25);
            SetAbility(Ability.ReadMagic, 23, 50);
            SetAbility(Ability.UseMagic, 27, 50);

            return hero;
        }

        public static PartyMember Load(IPartyMemberReader partyMemberReader, IDataReader dataReader)
        {
            var partyMember = new PartyMember();

            partyMemberReader.ReadPartyMember(partyMember, dataReader);

            return partyMember;
        }
    }
}
