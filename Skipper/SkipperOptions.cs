using CommandLine;

namespace Skipper
{
    public class SkipperOptions
    {
        [Option('t', "targetTestAssembly", Required = true,
            HelpText = "The path of the test assembly that will be modified for skipping.")]
        public string TargetAssemblyLocation { get; set; }

        [Option('o', "outputFile", Required = false,
            HelpText =
                "The output path of the modified test assembly. If blank, then the Skipper will use the same path as the target test assembly path.")]
        public string OutputFile { get; set; }

        [Option('s', "skipListFile", Required = false,
            HelpText =
                "The location of the skip list file that contains the list of single line test method entries that will be marked for skipping (in the form of Project.Namespace.TestType.TestMethodName, one entry per line).")]
        public string SkipListFilePath { get; set; }

        [Option('r', "skipReason", Required = false,
            HelpText =
                "The reason why each one of the matching tests will be skipped. This text will become part of the text output when the tests are run and the matching tests are skipped.")]
        public string SkipReason { get; set; }

        [Option('i', "includeListFile", Required = false,
            HelpText =
                "The location of the inclusion file that contains the list of single line test method entries that must be marked for inclusion. If the file is present, all tests not in the inclusion file will be skipped")]
        public string IncludeListFilePath { get; set; }
    }
}