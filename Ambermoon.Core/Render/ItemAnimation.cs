using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using Ambermoon.UI;
using System;

namespace Ambermoon.Render
{
    internal static class ItemAnimation
    {
        public enum Type
        {
            Enchant,
            Consume,
            Destroy,
            Move,
            Shake
        }

        static readonly Layer[] Layers = new Layer[]
        {
            Layer.UI, Layer.UI, Layer.Items, Layer.Items, Layer.Items
        };

        static readonly int[][] DestroyAnimationPositions = new int[][]
        {
            new [] { 0, -1, 1, -1, 1, -1, 1, 1, 0, 1, 0, 1, 0, 0, 0, 0 },
            new [] { 0, -1, 1, -1, 1, 0, 0, 1, 1, 1, 0, 1, 0, 1, 0, 0 },
            new [] { 1, -1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0 },
            new [] { 1, -1, 1, -1, 1, 0, 1, 1, 0, 1, 0, 1, 0, 1, 0, 0 },
            new [] { 1, 0, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 0, 1, 0 },
            new [] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 0, 1 },
            new [] { 1, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0 },
            new [] { 0, -1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 0, 0, 0 }
        };

        static Item GetItem(Game game, uint? itemIndex) => itemIndex == null ? null : game.ItemManager.GetItem(itemIndex.Value);

        static uint GetGraphicIndex(Game game, Type type, uint? itemIndex) => type switch
        {
            Type.Enchant => Graphics.GetCustomUIGraphicIndex(UICustomGraphic.ItemMagicAnimation),
            Type.Consume => Graphics.GetUIGraphicIndex(UIGraphic.ItemConsume),
            _ => GetItem(game, itemIndex)?.GraphicIndex ?? throw new AmbermoonException(ExceptionScope.Application, $"No item was given for item animtion '{type}'")
        };

        static void PlayItemDestroyAnimation(Game game, IRenderView renderView, Position position, uint graphicIndex, Action finishAction)
        {
            var sprites = new ISprite[128];
            var animationPositionIndices = new int[128];
            var offset = TextureAtlasManager.Instance.GetOrCreate(Layer.Items).GetOffset(graphicIndex);

            for (int y = 0; y < 8; ++y)
            {
                for (int x = 0; x < 8; ++x)
                {
                    for (int i = 0; i < 2; ++i)
                    {
                        int index = i * 64 + x + y * 8;
                        var sprite = sprites[index] = renderView.SpriteFactory.Create(1, 1, true, 255);
                        sprite.TextureAtlasOffset = offset + new Position(x * 2 + y % 2, y * 2 + x % 2);
                        sprite.Layer = renderView.GetLayer(Layer.Items);
                        sprite.PaletteIndex = 49;
                        sprite.X = position.X + x * 2 + y % 2;
                        sprite.Y = position.Y + y * 2 + x % 2;
                        sprite.Visible = true;
                        animationPositionIndices[index] = game.RandomInt(0, DestroyAnimationPositions.Length - 1);
                    }
                }
            }

            int numAnimationFrames = 8;

            void Animate()
            {
                if (--numAnimationFrames < 0)
                {
                    for (int i = 0; i < 128; ++i)
                        sprites[i]?.Delete();

                    game.EndSequence();
                    finishAction?.Invoke();
                }
                else
                {
                    int frame = 7 - numAnimationFrames;

                    for (int i = 0; i < 128; ++i)
                    {
                        int amplitude = i < 64 ? 1 : 2;
                        var animationPositionIndex = animationPositionIndices[i];
                        int centerX = position.X + 8;
                        int xFactor = sprites[i].X < centerX ? -1 : 1;
                        int x = DestroyAnimationPositions[animationPositionIndex][frame * 2] * amplitude;
                        int y = DestroyAnimationPositions[animationPositionIndex][frame * 2 + 1] * amplitude;
                        if (x == 0 && y == 0)
                        {
                            sprites[i].Visible = false;
                        }
                        else
                        {
                            sprites[i].X += 3 * xFactor * x;
                            sprites[i].Y += 4 * y;
                        }
                    }

                    game.AddTimedEvent(TimeSpan.FromMilliseconds(65), Animate);
                }
            }

            Animate();
        }

        public static void Play(Game game, IRenderView renderView, Type type, Position startPosition,
            Action finishAction = null, TimeSpan? initialDelay = null)
        {
            Play(game, renderView, type, startPosition, finishAction, initialDelay, null,
                GetGraphicIndex(game, type, null), null);
        }

        public static void Play(Game game, IRenderView renderView, Type type, Position startPosition,
            Action finishAction, TimeSpan? initialDelay, Position targetPosition, Item item)
        {
            Play(game, renderView, type, startPosition, finishAction, initialDelay, targetPosition,
                GetGraphicIndex(game, type, item?.Index), null);
        }

        public static void Play(Game game, IRenderView renderView, Type type, Position startPosition,
            Action finishAction, TimeSpan? initialDelay, Position targetPosition, UIItem item)
        {
            Play(game, renderView, type, startPosition, finishAction, initialDelay, targetPosition,
                GetGraphicIndex(game, type, item?.Item?.ItemIndex), item);
        }

        static void Play(Game game, IRenderView renderView, Type type, Position startPosition,
            Action finishAction, TimeSpan? initialDelay, Position targetPosition, uint graphicIndex,
            UIItem item)
        {
            void Start()
            {
                game.StartSequence();
                int typeIndex = (int)type;
                var layer = Layers[typeIndex];
                var textureAtlas = TextureAtlasManager.Instance.GetOrCreate(layer);
                var offset = textureAtlas.GetOffset(graphicIndex);

                switch (type)
                {
                    case Type.Destroy:
                    {
                        PlayItemDestroyAnimation(game, renderView, startPosition, graphicIndex, finishAction);
                        break;
                    }
                    case Type.Enchant:
                    case Type.Consume:
                    {
                        int remainingFrames = type == Type.Enchant ? 8 : 11;
                        uint timePerFrame = type == Type.Enchant ? 110u : 60u;
                        var sprite = renderView.SpriteFactory.Create(16, 16, true, 255);
                        sprite.TextureAtlasOffset = offset;
                        sprite.Layer = renderView.GetLayer(layer);
                        sprite.PaletteIndex = 49;
                        sprite.X = startPosition.X;
                        sprite.Y = startPosition.Y;
                        sprite.Visible = true;
                        void Animate()
                        {
                            if (--remainingFrames == 0)
                            {
                                sprite?.Delete();
                                game.EndSequence(false);
                                finishAction?.Invoke();
                            }
                            else
                            {
                                sprite.TextureAtlasOffset = new Position(sprite.TextureAtlasOffset.X + 16, sprite.TextureAtlasOffset.Y);                                
                                game.AddTimedEvent(TimeSpan.FromMilliseconds(timePerFrame), Animate);                                
                            }
                        }
                        game.AddTimedEvent(TimeSpan.FromMilliseconds(timePerFrame), Animate);
                        break;
                    }
                    case Type.Move:
                    {
                        PlayMoveAnimation(game, startPosition, targetPosition, item, finishAction);
                        break;
                    }
                    case Type.Shake:
                    {
                        PlayShakeAnimation(game, item, finishAction);
                        break;
                    }
                    default:
                    {
                        throw new AmbermoonException(ExceptionScope.Application, $"Invalid item animation type '{type}'");
                    }
                }
            }

            if (initialDelay != null)
                game.AddTimedEvent(initialDelay.Value, Start);
            else
                Start();
        }

        static void PlayMoveAnimation(Game game, Position startPosition, Position targetPosition, UIItem item, Action finishAction)
        {
            const int pixelsPerSecond = 300;
            const int timePerFrame = 10;
            int distPerFrame = pixelsPerSecond * timePerFrame / 1000;
            var dist = targetPosition - startPosition;
            int moved = 0;
            float length = (float)Math.Sqrt(dist.X * dist.X + dist.Y * dist.Y);
            int maxMove = Util.Ceiling(length);

            void Move()
            {
                moved += distPerFrame;
                if (moved > maxMove)
                    moved = maxMove;
                float factor = moved / length;
                item.Position = startPosition + new Position(Util.Round(factor * dist.X), Util.Round(factor * dist.Y));

                if (moved == maxMove)
                {
                    item.Dragged = false;
                    finishAction?.Invoke();
                }
                else
                {
                    game.AddTimedEvent(TimeSpan.FromMilliseconds(timePerFrame), Move);
                }
            }

            item.Dragged = true;
            item.ShowItemAmount = false;

            Move();
        }

        static void PlayShakeAnimation(Game game, UIItem item, Action finishAction)
        {
            int baseX = item.Position.X;
            int minX = item.Position.X - 1;
            int maxX = item.Position.X + 1;
            bool right = true;
            int runs = 7;

            void Shake()
            {
                if (right)
                {
                    item.Position = new Position(item.Position.X + 1, item.Position.Y);

                    if (item.Position.X == maxX)
                        right = false;
                }
                else
                {
                    item.Position = new Position(item.Position.X - 1, item.Position.Y);

                    if (item.Position.X == minX || (runs == 0 && item.Position.X == baseX))
                    {
                        right = true;

                        if (--runs < 0)
                        {
                            finishAction?.Invoke();
                            return;
                        }
                    }
                }

                game.AddTimedEvent(TimeSpan.FromMilliseconds(6), Shake);
            }

            Shake();
        }
    }
}
