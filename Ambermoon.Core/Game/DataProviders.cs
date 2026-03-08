using System.Collections.Generic;
using System.Linq;
using Ambermoon.Data;
using Ambermoon.UI;

namespace Ambermoon;

partial class GameCore
{
    class NameProvider(GameCore game) : ITextNameProvider
    {
        Character? Subject => game.currentWindow.Window switch
        {
            Window.Healer => game.currentlyHealedMember,
            Window.Battle => game.BattleRoundActive ? game.CurrentPartyMember : game.CurrentSpellTarget ?? game.CurrentPartyMember,
            _ => game.CurrentSpellTarget ?? game.CurrentPartyMember
        };

        /// <inheritdoc />
        public string LeadName => game.CurrentPartyMember?.Name ?? "";
        /// <inheritdoc />
        public string SelfName => game?.PartyMembers?.FirstOrDefault()?.Name ?? LeadName;
        /// <inheritdoc />
        public string CastName => game.CurrentCaster?.Name ?? LeadName;
        /// <inheritdoc />
        public string InvnName => game.CurrentInventory?.Name ?? LeadName;
        /// <inheritdoc />
        public string SubjName => Subject?.Name ?? LeadName;
        /// <inheritdoc />
        public string Sex1Name => Subject?.Gender == Gender.Male ? game.DataNameProvider.He : game.DataNameProvider.She;
        /// <inheritdoc />
        public string Sex2Name => Subject?.Gender == Gender.Male ? game.DataNameProvider.His : game.DataNameProvider.Her;
    }

    readonly NameProvider nameProvider;
    readonly TextDictionary textDictionary;
    internal IDataNameProvider DataNameProvider { get; }
    public ICharacterManager CharacterManager { get; }

    // TODO: Optimize to not query this every time (e.g. by updating it when a word is learned)
    public List<string>? Dictionary
        => CurrentSavegame == null ? null : [.. textDictionary.Entries.Where((word, index) =>
            CurrentSavegame.IsDictionaryWordKnown((uint)index))];

    internal string GetCustomText(CustomTexts.Index index) => CustomTexts.GetText(GameLanguage, index);
}
