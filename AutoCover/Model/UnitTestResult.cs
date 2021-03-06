﻿// Copyright (c) 2013
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoCover
{
    public enum CodeCoverageResult
    {
        Passed, Failed, NotCovered
    }

    public enum UnitTestResult
    {
        Passed, Failed
    }

    [Serializable]
    public class ACUnitTest : IEquatable<ACUnitTest>
    {
        public Guid Id { get; set; }

        public string Name { get; set; }
        
        public string ProjectName { get; set; }

        public string HumanReadableId { get; set; }

        public UnitTestResult Result { get; set; }

        public string Message { get; set; }

        public bool Equals(ACUnitTest other)
        {
            return Id == other.Id;
        }
    }
}
