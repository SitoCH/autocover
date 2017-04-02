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

using System.Linq;
using System.IO;

namespace AutoCover
{
    public static class FileUtils
    {
        public static bool FileEquals(string fileName1, string fileName2)
        {
            if (new FileInfo(fileName1).Length != new FileInfo(fileName2).Length)
                return false;

            using (var file1 = new FileStream(fileName1, FileMode.Open))
            using (var file2 = new FileStream(fileName2, FileMode.Open))
                return StreamEquals(file1, file2);
        }

        private static bool StreamEquals(Stream stream1, Stream stream2)
        {
            const int bufferSize = 2048;
            byte[] buffer1 = new byte[bufferSize]; //buffer size
            byte[] buffer2 = new byte[bufferSize];
            while (true)
            {
                int count1 = stream1.Read(buffer1, 0, bufferSize);
                int count2 = stream2.Read(buffer2, 0, bufferSize);

                if (count1 != count2)
                    return false;

                if (count1 == 0)
                    return true;

                // You might replace the following with an efficient "memcmp"
                if (!buffer1.Take(count1).SequenceEqual(buffer2.Take(count2)))
                    return false;
            }
        }
    }
}
