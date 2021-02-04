using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using dnlib.DotNet;
using Optional.Collections;
using Optional.Unsafe;
using SampleTestProject;
using Skipper.Core;
using Xunit;

namespace Tests
{
    public class SkipperTests
    {
        [Fact(DisplayName = @"The skipper should modify a unit test with the FactAttribute so that it is ignored")]
        public async Task ShouldSkipTestsWithFactAttributes()
        {
            var testsToSkip = new[] {"Test1", "Test2", "Test3"};

            TestSkipper(testsToSkip, "FactAttribute");
        }

        [Fact(DisplayName = @"The skipper should modify a unit test with the TheoryAttribute so that it is ignored")]
        public async Task ShouldSkipTestsWithTheoryAttribute()
        {
            var testsToSkip = new[] {"Test6"};

            TestSkipper(testsToSkip, "TheoryAttribute");
        }

        private static void TestSkipper(string[] testsToSkip, string attributeTypeName)
        {
            var targetAssemblyPath = typeof(SampleTests).Assembly.Location;
            var assemblyDef = AssemblyDef.Load(targetAssemblyPath);

            Func<string, string, string, string, bool> testFilter = (assemblyName, typeName, methodName, fullyQualifiedMethodName) =>
                testsToSkip.Contains(methodName);

            SkipWeaver.InsertSkips(assemblyDef, "This is a test", testFilter);

            var modifiedAssembly = assemblyDef.ToLoadedAssembly();
            var modifiedTestFixture = modifiedAssembly.GetTypes().First(t => t.Name == nameof(SampleTests));

            var testMethods = modifiedTestFixture.GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(m =>
                m.GetCustomAttributes(false).Any(ca => ca.GetType().Name == attributeTypeName)).ToArray();

            var actualSkippedMethods = new List<MethodInfo>();
            foreach (var testMethod in testMethods)
            {
                var factAttribute = testMethod.GetCustomAttributes(false)
                    .Where(ca => ca.GetType().Name == attributeTypeName).FirstOrNone();

                if (!factAttribute.HasValue)
                    continue;

                dynamic currentAttribute = factAttribute.ValueOrFailure();

                string skipValue = currentAttribute.Skip;
                if (string.IsNullOrEmpty(skipValue))
                    continue;

                actualSkippedMethods.Add(testMethod);
            }

            var actualTestsSkipped = actualSkippedMethods.Select(m => m.Name).ToHashSet();
            var expectedTestsSkipped = testsToSkip.ToHashSet();

            foreach (var test in expectedTestsSkipped)
            {
                Assert.Contains(test, actualTestsSkipped);
            }
        }
    }
}