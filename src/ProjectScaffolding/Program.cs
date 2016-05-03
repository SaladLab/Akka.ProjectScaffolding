using System;
using CommandLine;

namespace ProjectScaffolding
{
    internal class Program
    {
        internal class Options
        {
            [Value(0, Required = true, HelpText = "Project Name")]
            public string ProjectFile { get; set; }

            [Option('o', "output", HelpText = "Specifies the directory for generate project. If not specified, uses the current directory.")]
            public string OutputDirectory { get; set; }

            [Option('t', "template", HelpText = "Specifies the template file name. If not specified, uses the embeded one.")]
            public string TemplatePath { get; set; }
        }

        private static int Main(string[] args)
        {
            var parser = new Parser(config => config.HelpWriter = Console.Out);
            if (args.Length == 0)
            {
                parser.ParseArguments<Options>(new[] { "--help" });
                return 1;
            }

            Options options = null;
            var result = parser.ParseArguments<Options>(args)
                               .WithParsed(r => { options = r; });

            // Run process !

            if (options != null)
                return Process(options);
            else
                return 1;
        }

        internal static int Process(Options options)
        {
            return 0;
        }
    }
}
