// -----------------------------------------------------------------------
// <copyright file="BinKind.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

namespace ThaumielMapEditor.API.Enums
{
    internal enum BinKind : byte
    {
        Null,
        Bool,
        Byte,
        SByte,
        Int16,
        UInt16,
        Int32,
        UInt32,
        Int64,
        UInt64,
        Float,
        Double,
        Decimal,
        Char,
        String,
        DateTime,
        TimeSpan,
        Guid,
        ByteArray,
        DateTimeOffset,
        Uri,
        List,
        Dictionary,
        Object,
        RuntimeTyped
    }
}
