namespace Skyline.DataMiner.CICD.Tools.Sbom.SystemCommandLine
{
    using System.CommandLine.Parsing;

    using DirectoryInfo = FileSystem.DirectoryInfoWrapper.DirectoryInfo;
    using FileInfo = FileSystem.FileInfoWrapper.FileInfo;

    /// <summary>
    /// Helper methods so that System.CommandLine can deal with CICD.FileSystem classes.
    /// </summary>
    internal static class OptionHelper
    {
        public static DirectoryInfo? ParseDirectoryInfo(ArgumentResult result)
        {
            if (result.Tokens.Count != 1)
            {
                result.ErrorMessage = $"--{result.Argument.Name} requires exactly one argument.";
                return null;
            }

            return new DirectoryInfo(result.Tokens[0].Value);
        }

        public static FileInfo? ParseFileInfo(ArgumentResult result)
        {
            if (result.Tokens.Count != 1)
            {
                result.ErrorMessage = $"--{result.Argument.Name} requires exactly one argument.";
                return null;
            }

            return new FileInfo(result.Tokens[0].Value);
        }
    }
}