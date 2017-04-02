// Copyright (c) 2013
// Simone Grignola [http://www.grignola.ch]
//
// This file is part of AutoCover.
//
// AutoCover is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

// Guids.cs
// MUST match guids.h
using System;

namespace AutoCover
{
    static class GuidList
    {
        public const string guidAutoCoverPkgString = "1b158639-badf-49be-8825-c3aabb5fb6f8";
        public const string guidAutoCoverCmdSetString = "06c20781-f834-4d04-931a-1ecc8eb5a6a5";
        public const string guidToolWindowPersistanceString = "45ccb3f8-6556-4283-a34a-74b0bdfd370a";

        public static readonly Guid guidAutoCoverCmdSet = new Guid(guidAutoCoverCmdSetString);
    };
}