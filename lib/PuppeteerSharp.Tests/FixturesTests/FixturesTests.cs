using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using PuppeteerSharp.Tests.Attributes;
using PuppeteerSharp.Nunit;
using NUnit.Framework;
using System.Linq;

namespace PuppeteerSharp.Tests.FixturesTests
{
    public class FixturesTests : PuppeteerBaseTest
    {
        public FixturesTests(): base() { }

        [PuppeteerTest("fixtures.spec.ts", "Fixtures", "should dump browser process stderr")]
        [Skip(SkipAttribute.Targets.Firefox)]
        public void ShouldDumpBrowserProcessStderr()
        {
            var success = false;
            using var browserFetcher = new BrowserFetcher(SupportedBrowser.Chrome);
            using var process = GetTestAppProcess(
                "PuppeteerSharp.Tests.DumpIO",
                $"\"{browserFetcher.GetInstalledBrowsers().First().GetExecutablePath()}\"");

            process.ErrorDataReceived += (_, e) =>
            {
                success |= e.Data != null && e.Data.Contains("DevTools listening on ws://");
            };

            process.Start();
            process.BeginErrorReadLine();
            process.WaitForExit();
            Assert.True(success);
        }

        [Skip(SkipAttribute.Targets.Firefox)]
        public async Task ShouldCloseTheBrowserWhenTheConnectedProcessCloses()
        {
            var browserClosedTaskWrapper = new TaskCompletionSource<bool>();
            using var browserFetcher = new BrowserFetcher(SupportedBrowser.Chrome);
            var ChromiumLauncher = new ChromiumLauncher(
                browserFetcher.GetInstalledBrowsers().First().GetExecutablePath(),
                new LaunchOptions { Headless = true });

            await ChromiumLauncher.StartAsync().ConfigureAwait(false);

            var browser = await Puppeteer.ConnectAsync(new ConnectOptions
            {
                BrowserWSEndpoint = ChromiumLauncher.EndPoint
            });

            browser.Disconnected += (_, _) =>
            {
                browserClosedTaskWrapper.SetResult(true);
            };

            KillProcess(ChromiumLauncher.Process.Id);

            await browserClosedTaskWrapper.Task;
            Assert.True(browser.IsClosed);
        }

        [PuppeteerTest("fixtures.spec.ts", "Fixtures", "should close the browser when the node process closes")]
        [Skip(SkipAttribute.Targets.Firefox)]
        public async Task ShouldCloseTheBrowserWhenTheLaunchedProcessCloses()
        {
            var browserClosedTaskWrapper = new TaskCompletionSource<bool>();
            var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true }, TestConstants.LoggerFactory);

            browser.Disconnected += (_, _) =>
            {
                browserClosedTaskWrapper.SetResult(true);
            };

            KillProcess(browser.Process.Id);

            await browserClosedTaskWrapper.Task;
            Assert.True(browser.IsClosed);
        }

        private void KillProcess(int pid)
        {
            using var process = new Process();

            //We need to kill the process tree manually
            //See: https://github.com/dotnet/corefx/issues/26234
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                process.StartInfo.FileName = "taskkill";
                process.StartInfo.Arguments = $"-pid {pid} -t -f";
            }
            else
            {
                process.StartInfo.FileName = "/bin/bash";
                process.StartInfo.Arguments = $"-c \"kill -s 9 {pid}\"";
            }

            process.Start();
            process.WaitForExit();
        }

        private Process GetTestAppProcess(string appName, string arguments)
        {
            var process = new Process();

#if NETCOREAPP
            process.StartInfo.WorkingDirectory = GetSubprocessWorkingDir(appName);
            process.StartInfo.FileName = "dotnet";
            process.StartInfo.Arguments = $"{appName}.dll {arguments}";
#else
            process.StartInfo.FileName = Path.Combine(GetSubprocessWorkingDir(appName), $"{appName}.exe");
            process.StartInfo.Arguments = arguments;
#endif
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;
            return process;
        }

        private string GetSubprocessWorkingDir(string dir)
        {
#if DEBUG
            var build = "Debug";
#else

            var build = "Release";
#endif
#if NETCOREAPP
            return Path.Combine(
                TestUtils.FindParentDirectory("lib"),
                dir,
                "bin",
                build,
                "net7.0");
#else
            return Path.Combine(
                TestUtils.FindParentDirectory("lib"),
                dir,
                "bin",
                build,
                "net48");
#endif
        }
    }
}
