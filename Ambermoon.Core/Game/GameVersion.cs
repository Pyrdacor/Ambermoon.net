/*
 * GameVersion.cs - Game version and language
 *
 * Copyright (C) 2020-2026  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
using Ambermoon.Data;
using Ambermoon.Data.Enumerations;

namespace Ambermoon;

public class GameVersion
{
    public required string Version;
    public GameLanguage Language;
    public required string Info;
    public Features Features;
    public bool MergeWithPrevious;
    public bool ExternalData;
    public required Func<IGameData> DataProvider;

    internal const string RemakeReleaseDate = "24-11-2025";
}

