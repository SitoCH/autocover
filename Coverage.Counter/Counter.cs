//
// Counter.cs
//
// Author:
//   Sergiy Sakharov (sakharov@gmail.com)
//
// (C) 2010 Sergiy Sakharov
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Linq;
using System.Xml.Linq;
using System.Xml;
using System.Text;

namespace Coverage
{
    /// <summary>
    /// This class collects and stores
    /// history of hits of sequence points
    /// </summary>
    public static class Counter
    {
        private static DateTime _startTime;
        private static DateTime _measureTime;
        private static string _currentTest;
        private static readonly Dictionary<string, Dictionary<int, HashSet<string>>> Hits = new Dictionary<string, Dictionary<int, HashSet<string>>>();
        private static readonly Mutex Mutex = new Mutex(false, "CoverageReportUpdate");

        static Counter()
        {
            _startTime = DateTime.Now;
            //These handlers execute flushing all hit counts to the xml file
            AppDomain.CurrentDomain.DomainUnload += delegate { FlushCounter(); };
            AppDomain.CurrentDomain.ProcessExit += delegate { FlushCounter(); };
            AppDomain.CurrentDomain.UnhandledException += delegate { UnhandledException(); };
        }

        private static void UnhandledException()
        {
            Console.WriteLine("UnhandledException");
        }

        /// <summary>
        /// Location of coverage xml file
        /// This property's IL code is modified to store actual file location
        /// </summary>
        public static string CoverageFilePath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cReport.xml"); }
        }

        public static string CoverageFilePathResults
        {
            get { return CoverageFilePath.Replace(".xml", ".results.xml"); }
        }

        /// <summary>
        /// This method flushes hit count buffers.
        /// </summary>
        public static void FlushCounter()
        {
            try
            {
                if (Hits.Count == 0)
                    return;

                KeyValuePair<string, Dictionary<int, HashSet<string>>>[] hitCounts;
                lock (Hits)
                {
                    if (Hits.Count == 0)
                        return;

                    hitCounts = Hits.ToArray();
                    Hits.Clear();
                }

                _measureTime = DateTime.Now;
                UpdateFileReport(hitCounts);
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                var ie = ex;
                while (ie != null)
                {
                    sb.AppendLine(ex.Message);
                    sb.AppendLine(ex.StackTrace);
                    ie = ie.InnerException;
                }
                File.WriteAllText(CoverageFilePathResults + ".log", sb.ToString());
            }

        }

        /// <summary>
        /// Save sequence point hit counts to xml report file
        /// </summary>
        static void UpdateFileReport(IEnumerable<KeyValuePair<string, Dictionary<int, HashSet<string>>>> hitCounts)
        {
            Mutex.WaitOne(10000);
            try
            {
                using (var coverageFile = new FileStream(CoverageFilePath, FileMode.Open))
                {
                    using (var writer = XmlWriter.Create(CoverageFilePathResults))
                    {
                        writer.WriteStartDocument();
                        writer.WriteStartElement("results");

                        //Edit xml report to store new hits
                        var xDoc = XDocument.Load(new XmlTextReader(coverageFile));

                        /*var startTimeAttr = xDoc.Root.Attribute("startTime");
                        var measureTimeAttr = xDoc.Root.Attribute("measureTime");
                        var oldStartTime = DateTime.ParseExact(startTimeAttr.Value, "o", null);
                        var oldMeasureTime = DateTime.ParseExact(measureTimeAttr.Value, "o", null);

                        _startTime = _startTime < oldStartTime ? _startTime : oldStartTime; //Min
                        _measureTime = _measureTime > oldMeasureTime ? _measureTime : oldMeasureTime; //Max

                        startTimeAttr.SetValue(_startTime.ToString("o"));
                        measureTimeAttr.SetValue(_measureTime.ToString("o"));*/

                        foreach (var pair in hitCounts)
                        {
                            var moduleId = pair.Key;
                            var moduleHits = pair.Value;
                            var xModule = xDoc.Descendants("module").First(el => el.Attribute("moduleId").Value == moduleId);

                            var counter = 0;
                            foreach (var pt in xModule.Descendants("seqpnt"))
                            {
                                counter++;
                                if (!moduleHits.ContainsKey(counter))
                                    continue;
                                if (moduleHits[counter].Count > 0)
                                {
                                    var line = int.Parse(pt.Attribute("line").Value);
                                    var column = int.Parse(pt.Attribute("column").Value);
                                    var endLine = int.Parse(pt.Attribute("endline").Value);
                                    var endColumn = int.Parse(pt.Attribute("endcolumn").Value);
                                    var document = pt.Attribute("document").Value;

                                    writer.WriteStartElement("seqpnt");
                                    writer.WriteAttributeString("line", line.ToString());
                                    writer.WriteAttributeString("column", column.ToString());
                                    writer.WriteAttributeString("endline", endLine.ToString());
                                    writer.WriteAttributeString("endcolumn", endColumn.ToString());
                                    writer.WriteAttributeString("document", document);
                                    foreach (var test in moduleHits[counter])
                                    {
                                        writer.WriteStartElement("test");
                                        writer.WriteAttributeString("name", test);
                                        writer.WriteEndElement();
                                    }
                                    writer.WriteEndElement();
                                }
                            }
                        }
                        writer.WriteEndElement();
                        writer.WriteEndDocument();
                    }
                }
            }
            finally
            {
                Mutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// This method is executed from instrumented assemblies.
        /// </summary>
        public static void Hit(string moduleId, int hitPointId)
        {
            lock (Hits)
            {
                if (string.IsNullOrWhiteSpace(_currentTest))
                    return;
                if (!Hits.ContainsKey(moduleId))
                    Hits[moduleId] = new Dictionary<int, HashSet<string>>();
                if (!Hits[moduleId].ContainsKey(hitPointId))
                    Hits[moduleId][hitPointId] = new HashSet<string>();
                Hits[moduleId][hitPointId].Add(_currentTest);
            }
        }

        public static void SetCurrentTest(string test)
        {
            lock (Hits)
            {
                _currentTest = test;
            }
        }
    }
}