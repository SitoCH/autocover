// Guids.cs
// MUST match guids.h
using System;

namespace SimoneGrignola.AutoCover
{
    static class GuidList
    {
        public const string guidAutoCoverPkgString = "1b158639-badf-49be-8825-c3aabb5fb6f8";
        public const string guidAutoCoverCmdSetString = "06c20781-f834-4d04-931a-1ecc8eb5a6a5";
        public const string guidToolWindowPersistanceString = "45ccb3f8-6556-4283-a34a-74b0bdfd370a";

        public static readonly Guid guidAutoCoverCmdSet = new Guid(guidAutoCoverCmdSetString);
    };
}