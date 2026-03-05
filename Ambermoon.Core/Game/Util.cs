using System;
using Ambermoon.Data;

namespace Ambermoon;

partial class GameCore
{
    readonly Random random = new();

    internal int RollDice100()
    {
        return RandomInt(0, 99);
    }

    public int RandomInt(int min, int max)
    {
        uint range = (uint)(max + 1 - min);
        if (range == 0) // this avoid a possible division by zero crash
            return min;
        return min + (int)(random.Next() % range);
    }

    public void StartSequence()
    {
        allInputWasDisabled = allInputDisabled;
        layout.ReleaseButtons();
        allInputDisabled = true;
        clickMoveActive = false;
        CurrentMobileAction = MobileAction.None;
        trappedAfterClickMoveActivation = false;
    }

    public void EndSequence(bool force = true)
    {
        if (force || !allInputWasDisabled)
            allInputDisabled = false;
        allInputWasDisabled = false;
    }

    static float FadeAlphaToLight(float alpha)
    {
        // 3D shaders use: outColor = vec4(pixelColor.rgb + vec3(light) - vec3(1), pixelColor.a);
        // Or in short: result = color + light - 1.0f
        // This means that darker colors are faster darkened and lighter colors are almost linear.
        // A linear light factor change will darken too quickly and lighten too slowly.
        // We want fade alpha which is: result = color * alpha

        // But light must increase faster than alpha. As both values are smaller than 1, we can
        // just use the square root here.
        return (float)Math.Sqrt(alpha);
    }

    public void KillAllMapMonsters()
    {
        if (Map == null || Map.CharacterReferences == null)
            return;

        for (uint characterIndex = 0; characterIndex < Map.CharacterReferences.Length; ++characterIndex)
        {
            var characterReference = Map.CharacterReferences[characterIndex];

            if (characterReference == null)
                break;

            if (characterReference.Type == CharacterType.Monster)
                SetMapCharacterBit(Map.Index, characterIndex, true);
        }
    }

    MonsterGroup CloneMonsterGroup(MonsterGroup monsterGroup)
    {
        Monster? CloneMonster(Monster? monster)
        {
            if (monster == null)
                return null;

            return CharacterManager.CloneMonster(monster);
        }

        var clone = new MonsterGroup();

        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 6; x++)
            {
                clone.Monsters[x, y] = CloneMonster(monsterGroup.Monsters[x, y]);
            }
        }

        return clone;
    }

    void PlayTimedSequence(int steps, Action? stepAction, int stepTimeInMs, Action? followUpAction = null)
    {
        if (steps == 0)
            return;

        StartSequence();
        for (int i = 0; i < steps - 1; ++i)
            AddTimedEvent(TimeSpan.FromMilliseconds(i * stepTimeInMs), stepAction);
        AddTimedEvent(TimeSpan.FromMilliseconds((steps - 1) * stepTimeInMs), () =>
        {
            stepAction?.Invoke();
            EndSequence();
            ResetMoveKeys();
            followUpAction?.Invoke();
        });
    }

    public class GameSequence : IDisposable
    {
        private readonly GameCore game;

        public GameSequence(GameCore game)
        {
            this.game = game;
            game.StartSequence();
        }

        public void Dispose()
        {
            game.EndSequence();
        }
    }
}
