using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.Common;

namespace AutoCover
{
    public class TestAssembly
    {
        public string Name { get; set; }
        public string DllPath { get; set; }
        public List<UnitTest> Tests { get; set; }
    }
}
