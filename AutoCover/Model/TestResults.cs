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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.Common;

namespace AutoCover
{
    public class TestResults
    {
        private readonly Dictionary<Guid, UnitTest> _testResults = new Dictionary<Guid, UnitTest>();

        public void ProcessUnitTestResult(Guid testId, UnitTest result)
        {
            _testResults[testId] = result;
        }

        public Dictionary<Guid, UnitTest> GetTestResults()
        {
            return _testResults;
        }

        public void Reset()
        {
            _testResults.Clear();
        }

        public void RemoveDeletedTests(List<UnitTest> suggestedTests)
        {
            var newTests = new HashSet<Guid>(suggestedTests.Select(x => x.Id));
            foreach (var key in _testResults.Keys.ToList())
            {
                if (!newTests.Contains(key))
                    _testResults.Remove(key);
            }
        }
    }
}
