using Ambermoon.Data;
using Ambermoon.Render;

namespace Ambermoon.UI
{
    public class UIItem
    {
        public ItemSlot Item { get; }
        ILayerSprite sprite;
        IRenderText amountDisplay;
        readonly IRenderView renderView;
        readonly IItemManager itemManager;

        public bool Dragged
        {
            get => sprite?.DisplayLayer == 100;
            set
            {
                sprite.DisplayLayer = (byte)(value ? 100 : 0);

                if (amountDisplay != null)
                    amountDisplay.DisplayLayer = sprite.DisplayLayer;
            }
        }

        public UIItem(IRenderView renderView, IItemManager itemManager, ItemSlot item)
        {
            this.renderView = renderView;
            this.itemManager = itemManager;
            Item = item;
            sprite = renderView.SpriteFactory.Create(16, 16, false, true) as ILayerSprite;
            sprite.Layer = renderView.GetLayer(Layer.Items);
            sprite.PaletteIndex = 49;

            Update(true);
        }

        public void Update(bool itemTypeChanged)
        {
            if (itemTypeChanged)
            {
                if (Item.ItemIndex == 0 && Item.Amount != 0) // second hand slot
                {
                    sprite.TextureAtlasOffset = TextureAtlasManager.Instance.GetOrCreate(Layer.Items).GetOffset(0);
                    amountDisplay?.Delete();
                    amountDisplay = null;
                }
                else
                {
                    var itemInfo = itemManager.GetItem(Item.ItemIndex);

                    sprite.TextureAtlasOffset = TextureAtlasManager.Instance.GetOrCreate(Layer.Items).GetOffset(itemInfo.GraphicIndex);
                    bool stackable = itemInfo.Flags.HasFlag(ItemFlags.Stackable);

                    if (amountDisplay == null && stackable)
                    {
                        amountDisplay = renderView.RenderTextFactory.Create();
                        amountDisplay.Layer = renderView.GetLayer(Layer.Text);
                        amountDisplay.TextColor = TextColor.White;
                        amountDisplay.Shadow = true;
                        amountDisplay.Text = renderView.TextProcessor.CreateText(Item.Amount > 99 ? "**" : Item.Amount.ToString());
                        amountDisplay.Visible = true;
                    }
                    else if (amountDisplay != null && !stackable)
                    {
                        amountDisplay.Delete();
                        amountDisplay = null;
                    }
                }
            }
            else if (amountDisplay != null)
            {
                if (Item.Stacked)
                    amountDisplay.Text = renderView.TextProcessor.CreateText(Item.Amount > 99 ? "**" : Item.Amount.ToString());
                amountDisplay.Visible = Item.Stacked;
            }
        }

        public void Destroy()
        {
            sprite?.Delete();
            sprite = null;

            amountDisplay?.Delete();
            amountDisplay = null;
        }

        public bool Visible
        {
            get => sprite?.Visible ?? false;
            set
            {
                if (sprite == null)
                    return;

                sprite.Visible = value;

                if (amountDisplay != null)
                    amountDisplay.Visible = value && Item.Stacked;
            }
        }

        public Position Position
        {
            get => sprite == null ? null : new Position(sprite.X, sprite.Y);
            set
            {
                if (sprite == null)
                    return;

                sprite.X = value.X;
                sprite.Y = value.Y;

                if (amountDisplay != null)
                {
                    amountDisplay.X = Item.Amount < 10 ? sprite.X + 5 : sprite.X + 2;
                    amountDisplay.Y = sprite.Y + 17;
                }
            }
        }
    }
}
