using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CommandLine;
using dnlib.DotNet;
using Optional.Collections;
using Optional.Unsafe;
using Serilog;
using Skipper.Core;

namespace Skipper
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .MinimumLevel
                .Information()
                .CreateLogger();

            Parser.Default.ParseArguments<SkipperOptions>(args)
                .WithParsed(options =>
                {
                    var assemblyLocation = options.TargetAssemblyLocation;
                    var outputFile = options.OutputFile ?? assemblyLocation;
                    var skipFilePath = options.SkipListFilePath;
                    var skipReason = options.SkipReason ??
                                     "This method has been skipped since it has been green in past runs";

                    Log.Information($"Unit Test AssemblyLocation = {assemblyLocation} ");
                    Log.Information($"Output Test Assembly Path = {outputFile}");
                    Log.Information($"Skip File Path = {skipFilePath}");
                    Log.Information($"Skip Reason = '{skipReason}'");

                    var skipFileLines = File.ReadAllLines(skipFilePath);
                    var skipFileEntries = new HashSet<string>(skipFileLines);

                    Log.Debug($"{skipFileEntries.Count} Skip File Lines Loaded");

                    bool ShouldSkipUnitTest(string assemblyName, string typeName, string methodName,
                        string fullyQualifiedMethodName)
                    {
                        var combinedMethodName = $"{typeName}.{methodName}";

                        // Match either the combined method name or just the method name
                        var shouldBeSkipped = skipFileEntries.Contains(combinedMethodName) ||
                                              skipFileEntries.Contains(methodName)
                                              || skipFileEntries.Any(entry =>
                                                  entry.EndsWith(methodName));

                        // Fall back to regex if we can't find a match
                        if (!shouldBeSkipped)
                            shouldBeSkipped |= skipFileEntries.Any(expression =>
                                Regex.IsMatch(fullyQualifiedMethodName, expression));

                        return shouldBeSkipped;
                    }

                    Log.Information($"Reading unit test assembly file '{assemblyLocation}'");
                    Skipper.Core.Skipper.AddSkips(assemblyLocation, outputFile, skipReason, ShouldSkipUnitTest);
                });
        }
    }
}