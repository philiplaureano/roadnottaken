using System;
using System.IO;
using dnlib.DotNet;

namespace Skipper.Core
{
    public class Skipper
    {
        public static void AddSkips(string assemblyLocation, string outputFile, string skipReason,
            Func<string, string, string, string, bool> testFilter)
        {
            var bytes = File.ReadAllBytes(assemblyLocation);
            var assemblyDef = AssemblyDef.Load(bytes);
            SkipWeaver.InsertSkips(assemblyDef, skipReason, testFilter);

            assemblyDef.Write(outputFile);
        }
    }
}