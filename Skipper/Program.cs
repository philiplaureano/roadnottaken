using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using dnlib.DotNet;
using Optional.Collections;
using Optional.Unsafe;
using Serilog;

namespace Skipper
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .MinimumLevel
                .Warning()
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

                    bool ShouldSkipUnitTest(string assemblyName, string typeName, string methodName)
                    {
                        var combinedMethodName = $"{typeName}.{methodName}";
                        return skipFileEntries.Contains(combinedMethodName);
                    }

                    Log.Information($"Reading unit test assembly file '{assemblyLocation}'");
                    AddSkips(assemblyLocation, outputFile, skipReason, ShouldSkipUnitTest);
                });
        }

        private static void AddSkips(string assemblyLocation, string outputFile, string skipReason,
            Func<string, string, string, bool> testFilter)
        {
            var assemblyDef = AssemblyDef.Load(assemblyLocation);
            var module = assemblyDef.Modules.First();
            var types = module.Types.ToArray();
            foreach (var type in types)
            {
                foreach (var method in type.Methods)
                {
                    if (!method.HasCustomAttributes)
                        continue;

                    var customAttributes = method.CustomAttributes.ToArray();

                    // Search for the [Fact] or [Theory] attributes
                    var factOrTheoryAttribute =
                        customAttributes.FirstOrNone(c =>
                            c?.AttributeType.Name == "FactAttribute" ||
                            c?.AttributeType.Name == "TheoryAttribute");

                    if (!factOrTheoryAttribute.HasValue)
                    {
                        Log.Verbose(
                            $"Ignoring method '{method.FullName}' since it does not appear to be an xUnit test case");
                        continue;
                    }

                    var targetAttribute = factOrTheoryAttribute.ValueOrFailure();

                    // Ignore the method if it is already going to be skipped
                    if (targetAttribute.HasNamedArguments && targetAttribute.NamedArguments.Any(c => c.Name == "Skip"))
                    {
                        Log.Warning(
                            $"The test method '{method.FullName}' is already being skipped and won't be modified.");
                        continue;
                    }

                    var assemblyName = type.DefinitionAssembly.Name;
                    var typeName = type.Name;
                    var methodName = method.Name;

                    if (!testFilter(assemblyName, typeName, methodName))
                    {
                        Log.Warning(
                            $"The test method '{method.FullName}' has not been marked for skipping and won't be modified.");
                        continue;
                    }

                    targetAttribute.NamedArguments.Add(new CANamedArgument(false, module.CorLibTypes.String, "Skip",
                        new CAArgument(module.CorLibTypes.String,
                            skipReason)));
                }
            }

            assemblyDef.Write(outputFile);
        }
    }
}