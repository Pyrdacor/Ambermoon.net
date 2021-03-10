using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using Ambermoon.Render;

namespace Ambermoon.UI
{
    internal class UIItem
    {
        public ItemSlot Item { get; private set; }
        ILayerSprite sprite;
        ILayerSprite brokenOverlay;
        IRenderText amountDisplay;
        readonly IRenderView renderView;
        readonly IItemManager itemManager;
        readonly bool merchantItem;
        bool showItemAmount = true;

        public bool Dragged
        {
            get => sprite?.DisplayLayer == 100;
            set
            {
                sprite.DisplayLayer = (byte)(value ? 100 : 0);

                if (brokenOverlay != null)
                    brokenOverlay.DisplayLayer = (byte)(sprite.DisplayLayer + 1);

                if (amountDisplay != null)
                    amountDisplay.DisplayLayer = (byte)(sprite.DisplayLayer + 2);
            }
        }

        public bool ShowItemAmount
        {
            get => showItemAmount;
            set
            {
                if (showItemAmount == value)
                    return;

                showItemAmount = value;
                Update(false);
            }
        }

        public UIItem(IRenderView renderView, IItemManager itemManager, ItemSlot item, bool merchantItem)
        {
            this.renderView = renderView;
            this.itemManager = itemManager;
            Item = item;
            this.merchantItem = merchantItem;
            sprite = renderView.SpriteFactory.Create(16, 16, true) as ILayerSprite;
            sprite.Layer = renderView.GetLayer(Layer.Items);
            sprite.PaletteIndex = 49;

            Update(true);
        }

        public UIItem Clone()
        {
            if (brokenOverlay != null)
            {
                brokenOverlay?.Delete();
                brokenOverlay = null;
            }

            return new UIItem(renderView, itemManager, Item.Copy(), merchantItem);
        }

        public void SetItem(ItemSlot item)
        {
            bool itemTypeChanged = Item.ItemIndex != item.ItemIndex;
            Item = item;
            Update(itemTypeChanged);
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
                    bool stackable = merchantItem || Item.Stacked;

                    if (amountDisplay == null && stackable)
                    {
                        amountDisplay = renderView.RenderTextFactory.Create();
                        amountDisplay.Layer = renderView.GetLayer(Layer.Text);
                        amountDisplay.TextColor = TextColor.White;
                        amountDisplay.Shadow = true;
                        amountDisplay.X = Item.Amount < 10 ? sprite.X + 5 : sprite.X + 2;
                        amountDisplay.Y = sprite.Y + 17;
                        amountDisplay.Text = renderView.TextProcessor.CreateText(Item.Amount > 99 ? "**" : Item.Amount.ToString());
                        amountDisplay.Visible = ShowItemAmount;
                    }
                    else if (amountDisplay != null)
                    {
                        if (!stackable)
                        {
                            amountDisplay.Delete();
                            amountDisplay = null;
                        }
                        else
                        {
                            amountDisplay.Text = renderView.TextProcessor.CreateText(Item.Amount > 99 ? "**" : Item.Amount.ToString());
                            amountDisplay.Visible = ShowItemAmount;
                        }
                    }
                }
            }
            else if (amountDisplay != null)
            {
                if (Item.Stacked)
                    amountDisplay.Text = renderView.TextProcessor.CreateText(Item.Amount > 99 ? "**" : Item.Amount.ToString());
                amountDisplay.Visible = Item.Stacked && ShowItemAmount;
            }

            if (Item.ItemIndex != 0 && Item.Amount != 0 && Item.Flags.HasFlag(ItemSlotFlags.Broken))
            {
                if (brokenOverlay == null)
                {
                    brokenOverlay = renderView.SpriteFactory.Create(16, 16, true, (byte)(sprite?.DisplayLayer ?? 0 + 1)) as ILayerSprite;
                    brokenOverlay.Layer = renderView.GetLayer(Layer.UI);
                    brokenOverlay.PaletteIndex = 49;
                    brokenOverlay.TextureAtlasOffset = TextureAtlasManager.Instance.GetOrCreate(Layer.UI)
                        .GetOffset(Graphics.GetCustomUIGraphicIndex(UICustomGraphic.BrokenItemOverlay)); ;
                }

                brokenOverlay.X = sprite.X;
                brokenOverlay.Y = sprite.Y;
                brokenOverlay.DisplayLayer = (byte)(sprite.DisplayLayer + 1);
                brokenOverlay.Visible = true;
            }
            else
            {
                brokenOverlay?.Delete();
                brokenOverlay = null;
            }
        }

        public void Destroy()
        {
            sprite?.Delete();
            sprite = null;

            brokenOverlay?.Delete();
            brokenOverlay = null;

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

                if (brokenOverlay != null)
                    brokenOverlay.Visible = value;

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

                if (brokenOverlay != null)
                {
                    brokenOverlay.X = sprite.X;
                    brokenOverlay.Y = sprite.Y;
                }

                if (amountDisplay != null)
                {
                    amountDisplay.X = Item.Amount < 10 ? sprite.X + 5 : sprite.X + 2;
                    amountDisplay.Y = sprite.Y + 17;
                }
            }
        }
    }
}
