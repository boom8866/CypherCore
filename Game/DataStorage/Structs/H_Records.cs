﻿/*
 * Copyright (C) 2012-2017 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Framework.Constants;

namespace Game.DataStorage
{
    public sealed class HeirloomRecord
    {
        public uint ItemID;
        public LocalizedString SourceText;
        public uint[] OldItem = new uint[2];
        public uint NextDifficultyItemID;
        public uint[] UpgradeItemID = new uint[3];
        public ushort[] ItemBonusListID = new ushort[3];
        public byte Flags;
        public byte Source;
        public uint Id;
    }

    public sealed class HolidaysRecord
    {
        public uint Id;
        public uint[] Date = new uint[SharedConst.MaxHolidayDates];                                 // dates in unix time starting at January, 1, 2000
        public ushort[] Duration = new ushort[SharedConst.MaxHolidayDurations];
        public ushort Region;
        public byte Looping;
        public byte[] CalendarFlags = new byte[SharedConst.MaxHolidayFlags];
        public byte Priority;
        public sbyte CalendarFilterType;
        public byte Flags;
        public uint HolidayNameID;
        public uint HolidayDescriptionID;
        public int[] TextureFileDataID = new int[3];
    }
}
