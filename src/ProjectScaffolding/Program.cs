using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using CommandLine;

namespace ProjectScaffolding
{
    internal class Program
    {
        internal class Options
        {
            [Value(0, Required = true, HelpText = "Project Name")]
            public string ProjectName { get; set; }

            [Option('o', "output", HelpText = "Specifies the directory for generate project. If not specified, uses the current directory.")]
            public string OutputDirectory { get; set; }

            [Option('t', "template", HelpText = "Specifies the template file path. If not specified, uses the embeded one.")]
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
            var targetPath = Path.Combine(options.OutputDirectory, options.ProjectName);
            if (Directory.Exists(targetPath) == false)
                Directory.CreateDirectory(targetPath);

            Console.WriteLine("* BuildGuidMappingTable");
            BuildGuidMappingTable(options);

            Console.WriteLine("* Populate");
            Populate(options, options.TemplatePath, targetPath);

            Console.WriteLine("* RestorePackage");
            RestorePackages(options, targetPath);

            return 0;
        }

        internal static string GuidPattern = "[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}";
        internal static char[] HexUpperAlphabets = { 'A', 'B', 'C', 'D', 'E', 'F' };

        internal static Dictionary<Guid, Guid> GuidMappingTable;

        internal static void BuildGuidMappingTable(Options options)
        {
            GuidMappingTable = new Dictionary<Guid, Guid>();

            foreach (var file in Directory.GetFiles(options.TemplatePath, "*.sln", SearchOption.AllDirectories))
            {
                // Project("{IGNORED_GUID}") = "~", "~", "{MATCHED_GUID}"
                var matches = Regex.Matches(File.ReadAllText(file), @",\s*""\{(" + GuidPattern + @")\}""", RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    var guid = Guid.Parse(match.Groups[1].ToString());
                    if (GuidMappingTable.ContainsKey(guid) == false)
                        GuidMappingTable[guid] = Guid.NewGuid();
                }
            }
        }

        internal static void Populate(Options options, string srcPath, string dstPath)
        {
            if (Directory.Exists(dstPath) == false)
                Directory.CreateDirectory(dstPath);

            foreach (var dir in Directory.GetDirectories(srcPath))
            {
                Populate(options, dir, Path.Combine(dstPath, Path.GetFileName(dir)));
            }

            foreach (var file in Directory.GetFiles(srcPath))
            {
                var ext = Path.GetExtension(file).ToLower();
                var srcFileName = Path.GetFileName(file).ToLower();
                var dstFileName = Path.GetFileName(file);

                if (srcFileName == "solution.sln")
                    dstFileName = options.ProjectName + ".sln";

                Stream stream = null;
                try
                {
                    stream = new FileStream(file, FileMode.Open, FileAccess.Read);

                    if (ext == ".sln" || ext == ".csproj" || ext == ".vbproj" || ext == ".fsproj" ||
                        srcFileName.StartsWith("assemblyinfo"))
                    {
                        stream = FilterGuid(stream);
                    }
                    if (srcFileName == ".gitignore")
                    {
                        stream = FilterScaffoldingComment(stream);
                    }

                    using (var dstStream = new FileStream(Path.Combine(dstPath, dstFileName), FileMode.Create, FileAccess.Write))
                    {
                        stream.CopyTo(dstStream);
                    }
                }
                finally
                {
                    stream.Dispose();
                }
            }
        }

        private static Stream FilterGuid(Stream stream)
        {
            var text = "";
            using (var textReader = new StreamReader(stream, true))
            {
                text = textReader.ReadToEnd();
            }

            text = Regex.Replace(
                text,
                GuidPattern,
                mo =>
                {
                    var guid = Guid.Parse(mo.ToString());
                    if (GuidMappingTable.ContainsKey(guid) == false)
                        return mo.ToString();

                    var newGuid = GuidMappingTable[guid];
                    var uppercase = mo.ToString().IndexOfAny(HexUpperAlphabets) != -1;
                    return uppercase ? newGuid.ToString().ToUpper() : newGuid.ToString();
                },
                RegexOptions.IgnoreCase);

            return new MemoryStream(Encoding.UTF8.GetBytes(text));
        }

        private static Stream FilterScaffoldingComment(Stream stream)
        {
            var text = "";
            using (var textReader = new StreamReader(stream, true))
            {
                text = textReader.ReadToEnd();
            }

            text = Regex.Replace(
                text,
                @"\# SCAFFOLDING \{.*\# SCAFFOLDING \}",
                "",
                RegexOptions.Singleline);

            return new MemoryStream(Encoding.UTF8.GetBytes(text));
        }

        private static void RestorePackages(Options options, string dstPath)
        {
            foreach (var file in Directory.GetFiles(dstPath, "*.sln", SearchOption.AllDirectories))
            {
                Console.WriteLine("- nuget restore: " + file);
                PackageUtil.RunNuGet("restore", file).Wait();
            }

            foreach (var file in Directory.GetFiles(dstPath, "UnityPackages.json", SearchOption.AllDirectories))
            {
                var dir = Path.GetDirectoryName(file);
                if (Directory.Exists(Path.Combine(dir, "Assets")) == false)
                    continue;

                Console.WriteLine("- uniget restore: " + file);
                PackageUtil.RunUniGet("restore", file, "-o", dir).Wait();
            }
        }
    }
}
