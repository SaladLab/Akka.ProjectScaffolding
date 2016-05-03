using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;

namespace ProjectScaffolding
{
    internal static class PackageUtil
    {
        // Install-Package UniGet

        public static async Task RunNuGet(params string[] args)
        {
            var nugetPath = Path.Combine(PrepareCacheRoot(), "nuget.exe");
            if (File.Exists(nugetPath) == false)
                await DownloadNuGet();

            await RunProcessAsync(nugetPath, args);
        }

        public static async Task RunUniGet(params string[] args)
        {
            var unigetPath = Path.Combine(PrepareCacheRoot(), "uniget/tools/uniget.exe");
            if (File.Exists(unigetPath) == false)
                await DownloadUniGet();

            await RunProcessAsync(unigetPath, args);
        }

        private static async Task DownloadNuGet()
        {
            using (var client = new HttpClient())
            using (var response = await client.GetAsync("https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"))
            using (var content = response.Content)
            {
                var result = await content.ReadAsByteArrayAsync();
                var targetPath = PrepareCacheRoot();
                File.WriteAllBytes(Path.Combine(targetPath, "nuget.exe"), result);
            }
        }

        private static async Task DownloadUniGet()
        {
            await RunNuGet("install", "uniget", "-OutputDirectory", PrepareCacheRoot(), "-Source", "nuget.org", "-ExcludeVersion");
        }

        public static string PrepareCacheRoot()
        {
            var path = Path.Combine(Environment.GetEnvironmentVariable("APPDATA"), "project-scaffolding");
            if (Directory.Exists(path) == false)
                Directory.CreateDirectory(path);
            return path;
        }

        public static Task RunProcessAsync(string fileName, string[] args)
        {
            var tcs = new TaskCompletionSource<bool>();

            var process = new Process
            {
                StartInfo =
                {
                    FileName = fileName,
                    Arguments = string.Join(" ", args.Select(x => '"' + x + '"')),
                    UseShellExecute = false
                },
                EnableRaisingEvents = true
            };

            process.Exited += (sender, _) =>
            {
                tcs.SetResult(true);
                process.Dispose();
            };

            process.Start();

            return tcs.Task;
        }

        public static string CreateTemporaryDirectory()
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }
    }
}
