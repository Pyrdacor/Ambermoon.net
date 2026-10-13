using System.Linq;
using Ambermoon.Data;
using Ambermoon.Render;
using Ambermoon.UI;

namespace Ambermoon;

partial class GameCore
{
    readonly ILayerSprite questLogIcon;

    internal QuestLog? QuestLog { get; private set; }

    Rect QuestLogIconArea => new(questLogIcon.X, questLogIcon.Y, questLogIcon.Width, questLogIcon.Height + 2);

    protected bool QuestLogIconVisible
    {
        get => questLogIcon.Visible;
        set => questLogIcon.Visible = value;
    }

    public bool IsQuestLogEnabled
    {
        get
        {
            if (QuestLog == null || BattleActive || PopupActive || !InputEnable || allInputDisabled)
                return false;

            if (!WindowActive)
                return true;

            return CurrentWindow.Window switch
            {
                Window.Merchant or Window.Event or Window.Battle or Window.BattlePositions or
                Window.Trainer or Window.FoodDealer or Window.Healer or Window.HorseSalesman or
                Window.RaftSalesman or Window.ShipSalesman or Window.Sage or Window.Blacksmith or
                Window.Enchanter or Window.Automap => false,
                _ => true,
            };
        }
    }

    ILayerSprite CreateQuestLogIcon()
    {
        var icon = (renderView.SpriteFactory.Create(16, 16, true) as ILayerSprite)!;
        icon.Layer = renderView.GetLayer(Layer.Misc);
        icon.PaletteIndex = PrimaryUIPaletteIndex;
        icon.TextureAtlasOffset = TextureAtlasManager.Instance.GetOrCreate(Layer.Misc)!.GetOffset(QuestLog.IconGraphicIndex);
        icon.X = 0;
        icon.Y = 8;
        icon.Visible = true;

        return icon;
    }

    public void ToggleQuestLog()
    {
        if (QuestLog != null)
        {
            if (QuestLog.Open)
                ClosePopup();
            else if (IsQuestLogEnabled)
                QuestLog.Show();
        }
    }

    public uint GetTotalItemCount(uint itemIndex)
    {
        uint foundCount = 0;

        foreach (var partyMember in PartyMembers)
            foundCount += (uint)partyMember.Inventory.Slots.Where(s => s.ItemIndex == itemIndex).Sum(s => s.Amount);

        return foundCount;
    }

    // TODO: Update for new episodes!
    protected static int GetAdvancedEpisode(Savegame? savegame)
    {
        if (savegame == null)
            return 3;

        if (!savegame.PartyMembers.ContainsKey(16)) // If Kasimir is not there, it is episode 1
            return 1;
        else if (!savegame.Chests.ContainsKey(280)) // If chest 280 is not there it is episode 2
            return 2;
        else // If both are there, it is episode 3
            return 3;
    }

    int GetCurrentAdvancedEpisode() => GetAdvancedEpisode(CurrentSavegame);

    void CreateQuestLog()
    {
        QuestLog = new(this, renderView, renderView.GameData.Advanced,
            renderView.GameData.Advanced ? GetCurrentAdvancedEpisode() : 0);
    }
}
