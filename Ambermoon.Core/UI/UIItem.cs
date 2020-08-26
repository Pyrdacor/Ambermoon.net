using Ambermoon.Data;
using Ambermoon.Render;

namespace Ambermoon.UI
{
    public class UIItem
    {
        readonly ItemSlot item;
        ISprite sprite;
        IRenderText amountDisplay;

        public UIItem(IRenderView renderView, IItemManager itemManager, ItemSlot item)
        {
            this.item = item;
            var itemInfo = itemManager.GetItem(item.ItemIndex);
            sprite = renderView.SpriteFactory.Create(16, 16, 0, 0, false, true);
            sprite.Layer = renderView.GetLayer(Layer.Items);
            sprite.PaletteIndex = 49;
            sprite.TextureAtlasOffset = TextureAtlasManager.Instance.GetOrCreate(Layer.Items).GetOffset(itemInfo.GraphicIndex);

            if (itemInfo.Flags.HasFlag(ItemFlags.Stackable))
            {
                amountDisplay = renderView.RenderTextFactory.Create();
                amountDisplay.Layer = renderView.GetLayer(Layer.Text);
                amountDisplay.TextColor = TextColor.White;
                amountDisplay.Shadow = true;
                amountDisplay.Text = renderView.TextProcessor.CreateText(item.Amount > 99 ? "**" : item.Amount.ToString());
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
                    amountDisplay.Visible = item.Amount > 1;
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
                    amountDisplay.X = item.Amount < 10 ? sprite.X + 5 : sprite.X + 2;
                    amountDisplay.Y = sprite.Y + 17;
                }
            }
        }
    }
}
