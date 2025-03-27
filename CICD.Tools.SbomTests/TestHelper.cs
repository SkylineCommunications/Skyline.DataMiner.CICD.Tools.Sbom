namespace CICD.Tools.SbomTests
{
    using System.Diagnostics;
    using System.Reflection;

    using FluentAssertions;

    using Skyline.DataMiner.CICD.FileSystem;

    internal static class TestHelper
    {
        public static void BuildSolution(string solutionPath)
        {
            // Build the solution
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"build \"{solutionPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            process.ExitCode.Should().Be(0, because: $" build should succeed. Output: {output}\nError: {error}");
        }

        public static string GetTestFilesDirectory()
        {
            var baseDir = FileSystem.Instance.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return FileSystem.Instance.Path.Combine(baseDir, "Test Files");
        }
    }
}