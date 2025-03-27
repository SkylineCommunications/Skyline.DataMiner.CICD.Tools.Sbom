namespace Skyline.DataMiner.CICD.Tools.Sbom.SystemCommandLine
{
    using System.CommandLine;
    using System.CommandLine.Parsing;

    using Skyline.DataMiner.CICD.FileSystem;
    using Skyline.DataMiner.CICD.FileSystem.DirectoryInfoWrapper;
    using Skyline.DataMiner.CICD.FileSystem.FileInfoWrapper;

    internal static class OptionExtensions
    {
        /// <summary>
        /// Configures an option to accept only values corresponding to an existing file.
        /// </summary>
        /// <param name="option">The option to configure.</param>
        /// <returns>The option being extended.</returns>
        public static Option<FileInfo?> ExistingOnly(this Option<FileInfo?> option)
        {
            option.AddValidator(FileExists);
            return option;
        }

        /// <summary>
        /// Configures an option to accept only values corresponding to an existing directory.
        /// </summary>
        /// <param name="option">The option to configure.</param>
        /// <returns>The option being extended.</returns>
        public static Option<DirectoryInfo?> ExistingOnly(this Option<DirectoryInfo?> option)
        {
            option.AddValidator(DirectoryExists);
            return option;
        }

        private static void FileExists(OptionResult result)
        {
            foreach (Token token in result.Tokens)
            {
                if (!FileSystem.Instance.File.Exists(token.Value))
                {
                    result.ErrorMessage = result.LocalizationResources.FileDoesNotExist(token.Value);
                    return;
                }
            }
        }

        private static void DirectoryExists(OptionResult result)
        {
            foreach (Token token in result.Tokens)
            {
                if (!FileSystem.Instance.Directory.Exists(token.Value))
                {
                    result.ErrorMessage = result.LocalizationResources.DirectoryDoesNotExist(token.Value);
                    return;
                }
            }
        }
    }
}