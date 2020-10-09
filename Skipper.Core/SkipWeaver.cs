using System;
using System.Linq;
using dnlib.DotNet;
using Optional.Collections;
using Optional.Unsafe;
using Serilog;

namespace Skipper.Core
{
    public class SkipWeaver
    {
        public static void InsertSkips(AssemblyDef assemblyDef, string skipReason, 
            Func<string, string, string, bool> testFilter)
        {
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
        }
    }
}