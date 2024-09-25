/*
 * MapAnimation.cs - Manages map animations
 *
 * Copyright (C) 2024  Robert Schneckenhaus <robert.schneckenhaus@web.de>
 *
 * This file is part of Ambermoon.net.
 *
 * Ambermoon.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Ambermoon.net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Ambermoon.net. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

namespace Ambermoon.Render
{
    internal class MapAnimation
    {
        // This is similar to the original code.
        private class AnimationInfo
        {
            public AnimationInfo()
            {
				CurrentFrame = 0;
				Randomization = 0;
				RandomFlagBits = 0;
			}

			public int CurrentFrame { get; set; }
			public int Randomization { get; private set; }
			public int RandomFlagBits { get; private set; }

			public void Randomize(Func<int, int, int> random)
            {
				int flags = 0;

				for (int i = 0; i < 6; i++)
				{
					int randomValue = (random(0, ushort.MaxValue) >> 1) & 0xf;
					flags |= (1 << randomValue);
				}

				RandomFlagBits = flags;
                Randomization = (random(0, ushort.MaxValue) >> 1) & 0xff;
			}
        }

        readonly Game game;
        // Key: Number of frames
        readonly Dictionary<int, AnimationInfo> forwardAnimationInfos = new();
		readonly Dictionary<int, AnimationInfo> waveAnimationInfos = new();
		readonly Dictionary<int, bool> waveAnimationsRunningBackwards = new();

		public MapAnimation(Game game)
        {
            this.game = game;

            // In original only frame counts 2 to 8 are handled. We support more on the fly
            // but will init those already.
            for (int n = 2; n <= 8; n++)
            {
                var forward = new AnimationInfo();
                var wave = new AnimationInfo();

                forward.Randomize(game.RandomInt);
				wave.Randomize(game.RandomInt);

				forwardAnimationInfos.Add(n, forward);
				waveAnimationInfos.Add(n, wave);
				waveAnimationsRunningBackwards.Add(n, false);
			}
        }

        private void CreateAnimationInfos(int n)
        {
            var forward = new AnimationInfo();
            var wave = new AnimationInfo();

            forward.Randomize(game.RandomInt);
            wave.Randomize(game.RandomInt);

			forwardAnimationInfos.Add(n, forward);
			waveAnimationInfos.Add(n, wave);
			waveAnimationsRunningBackwards.Add(n, false);
		}

		public void Tick()
        {
			foreach (var info in forwardAnimationInfos)
			{
                if (info.Value.CurrentFrame == info.Key - 1)
                {
                    info.Value.CurrentFrame = 0;
                    info.Value.Randomize(game.RandomInt);
                }
                else
                {
                    info.Value.CurrentFrame++;
                }
			}

			foreach (var info in waveAnimationInfos)
			{
				bool backwards = waveAnimationsRunningBackwards[info.Key];

				if (!backwards && info.Value.CurrentFrame == info.Key - 1)
				{
					waveAnimationsRunningBackwards[info.Key] = true;
					info.Value.CurrentFrame = info.Key - 2;
				}
				else if (backwards && info.Value.CurrentFrame == 1)
				{
					waveAnimationsRunningBackwards[info.Key] = false;
					info.Value.CurrentFrame = 0;
					info.Value.Randomize(game.RandomInt);
				}
				else if (backwards)
				{
					info.Value.CurrentFrame--;
				}
				else
				{
					info.Value.CurrentFrame++;
				}
			}
		}

        public int UpdateFrameIndex(int currentIndex, int frameCount, int tileIndex, bool wave, bool randomAnimationStart)
		{
			if (frameCount < 2)
				return 0;

			if (frameCount > 8 && !forwardAnimationInfos.ContainsKey(frameCount))
				CreateAnimationInfos(frameCount);

			var infos = wave ? waveAnimationInfos : forwardAnimationInfos;
			var info = infos[frameCount];
			int frame = info.CurrentFrame;

			if (frame != 0 && randomAnimationStart)
			{
				int index = (tileIndex + info.Randomization) & 0xf;

				if ((info.RandomFlagBits & (1 << index)) == 0)
					return currentIndex; // no change in this case
			}

			return frame;
		}
	}
}
