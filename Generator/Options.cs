using CommandLine;
using System.Text;

namespace Generator
{
    class Options
    {
        [Option('d', "database", Required = true, HelpText = "Which database.")]
        public string database { get; set; }

        [Option('g', null, HelpText = "Generate test data.")]
        public bool generate { get; set; }

        [Option('a', null, HelpText = "Aggregation test.")]
        public bool aggregate { get; set; }

        [Option('s', null, HelpText = "Search text test.")]
        public string searchText { get; set; }

        [Option('i', null, HelpText = "Insensitive search.", DefaultValue = false)]
        public bool insensitive { get; set; }

        [HelpOption(HelpText = "Display this help screen.")]
        public string GetUsage()
        {
            var usage = new StringBuilder();
            usage.AppendLine("Generator");
            usage.AppendLine("usage: generator.exe -d [cache|postgres] [-g] [-a] [-s text] [-i]");
            usage.AppendLine("  -d: which database");
            usage.AppendLine("  -g: generate test data");
            usage.AppendLine("  -a: aggregation test");
            usage.AppendLine("  -s: search text");
            usage.AppendLine("  -i: insensitive search. Use only with -s");
            return usage.ToString();
        }

    }
}
