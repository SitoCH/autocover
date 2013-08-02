using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Coverage;
using System.IO;
using Coverage.Report;

namespace AutoCover_UnitTests
{
    [TestClass]
    public class CounterTest
    {
        [TestInitialize]
        public void Initialize()
        {
            if (File.Exists(Counter.CoverageFilePath))
                File.Delete(Counter.CoverageFilePath);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (File.Exists(Counter.CoverageFilePath))
                File.Delete(Counter.CoverageFilePath);
            if (File.Exists(Counter.CoverageFilePathResults))
                File.Delete(Counter.CoverageFilePathResults);
            if (File.Exists(Counter.CoverageFilePathResults + ".log"))
                File.Delete(Counter.CoverageFilePathResults + ".log");
        }

        private void CreateBaseCoverageFile()
        {
            var builder = new CoverageReportBuilder();
            builder.AddModule(new ModuleEntry("module1", "MyModule", "MyAssembly", "XYZ.MyAssembly"));
            builder.AddMethod(new MethodEntry("Method1", "DummyClass", true));
            builder.AddPoint(new PointEntry(1, 5, 1, 10, "C:\\DummyClass.cs", true));
            builder.AddPoint(new PointEntry(2, 5, 6, 5, "C:\\DummyClass.cs", true));
            builder.AddMethod(new MethodEntry("Method2", "DummyClass", true));
            builder.AddPoint(new PointEntry(1, 0, 20, 0, "C:\\DummyClass.cs", true));
            File.WriteAllText(Counter.CoverageFilePath, builder.GetXml());
        }

        [TestMethod]
        public void TestEmptySave()
        {
            Counter.FlushCounter();
            Assert.IsFalse(File.Exists(Counter.CoverageFilePath));
        }

        [TestMethod]
        public void TestSaveWith1HitAndNoCoverageFileAndNoTtest()
        {
            Counter.Hit("module1", 1);
            Counter.FlushCounter();
            Assert.IsFalse(File.Exists(Counter.CoverageFilePath));
        }

        [TestMethod]
        public void TestSaveWith1HitAndNoCoverageFile()
        {
            Counter.SetCurrentTest("MyTest");
            Counter.Hit("module1", 1);
            Counter.FlushCounter();
            Assert.IsFalse(File.Exists(Counter.CoverageFilePath));
            Assert.IsTrue(File.Exists(Counter.CoverageFilePathResults + ".log"));
        }

        [TestMethod]
        public void TestCreateBaseCoverageFile()
        {
            CreateBaseCoverageFile();
            Assert.IsTrue(File.Exists(Counter.CoverageFilePath));
        }

        [TestMethod]
        public void TestSaveWith1Hit()
        {
            CreateBaseCoverageFile();

            Counter.Hit("module1", 1);
            Counter.FlushCounter();
            Assert.IsTrue(File.Exists(Counter.CoverageFilePathResults));
        }

        [TestMethod]
        public void TestSaveWith1HitOnCurrentTest()
        {
            CreateBaseCoverageFile();

            Counter.SetCurrentTest("MyTest");
            Counter.Hit("module1", 1);
            Counter.FlushCounter();
            Assert.IsTrue(File.Exists(Counter.CoverageFilePathResults));
        }
    }
}
