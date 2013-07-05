// Guids.cs
// MUST match guids.h
using System;

namespace AutoCover
{
    static class GuidList
    {
        public const string guidAutoCoverPkgString = "90257b84-827f-4bd2-bfdc-b39a67c5d5b1";
        public const string guidAutoCoverCmdSetString = "fc22dab0-e985-419b-a3c5-71e82d823f0f";

        public static readonly Guid guidAutoCoverCmdSet = new Guid(guidAutoCoverCmdSetString);
    };
}