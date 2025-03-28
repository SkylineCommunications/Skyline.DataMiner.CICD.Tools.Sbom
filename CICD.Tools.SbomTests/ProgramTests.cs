namespace CICD.Tools.SbomTests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using FluentAssertions;

    using Skyline.DataMiner.CICD.FileSystem;
    using Skyline.DataMiner.CICD.Tools.Sbom;

    using DirectoryInfo = Skyline.DataMiner.CICD.FileSystem.DirectoryInfoWrapper.DirectoryInfo;
    using FileInfo = Skyline.DataMiner.CICD.FileSystem.FileInfoWrapper.FileInfo;

    [TestClass, TestCategory("IntegrationTest"), TestCategory("LocalOnly")]
    public class ProgramTests
    {
        private DirectoryInfo temporaryDirectory = null!;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            string solutionFile = FileSystem.Instance.Path.Combine(TestHelper.GetTestFilesDirectory(), "SbomTest", "SbomTest.sln");
            TestHelper.BuildSolution(solutionFile); // Solution needs to be built for the Sbom stuff to work
        }

        [TestInitialize]
        public void Initialize()
        {
            temporaryDirectory = new DirectoryInfo(FileSystem.Instance.Directory.CreateTemporaryDirectory());
        }

        [TestCleanup]
        public void Cleanup()
        {
            temporaryDirectory.Delete(true);
        }

        [TestMethod]
        public async Task MainTest_Generate_Valid()
        {
            // Arrange
            string solutionDirectory = FileSystem.Instance.Path.Combine(TestHelper.GetTestFilesDirectory(), "SbomTest");
            string expectedSbomFilePath = FileSystem.Instance.Path.Combine(temporaryDirectory.FullName, "sbom.json");

            string[] args =
            [
                "generate",
                "--solution-path", solutionDirectory,
                "--package-name", "MyTestPackage",
                "--package-version", "1.2.3",
                "--package-supplier", "Skyline Communications",
                "--output", temporaryDirectory.FullName
            ];

            // Act
            Func<Task<int>> task = async () => await Program.Main(args);

            // Assert
            (await task.Should().NotThrowAsync()).Which.Should().Be(0);
            FileInfo sbomFile = new FileInfo(expectedSbomFilePath);
            sbomFile.Exists.Should().BeTrue();
            sbomFile.Length.Should().BeGreaterThan(20_000);
        }

        [TestMethod]
        [TestCategory("LocalOnly")]
        public async Task MainTest_GenerateAndAdd_Valid()
        {
            // Arrange
            string solutionDirectory = FileSystem.Instance.Path.Combine(TestHelper.GetTestFilesDirectory(), "SbomTest");
            string expectedPackageFile = FileSystem.Instance.Path.Combine(temporaryDirectory.FullName, "SbomTest.1.0.0.dmapp");

            FileInfo packageFile = new FileInfo(FileSystem.Instance.Directory.GetFiles(solutionDirectory, "*.dmapp", SearchOption.AllDirectories).Single());
            long initialSize = packageFile.Length;

            string[] args =
            [
                "generate-and-add",
                "--solution-path", solutionDirectory,
                "--package-file", packageFile.FullName,
                "--package-name", "MyTestPackage",
                "--package-version", "1.2.3",
                "--package-supplier", "Skyline Communications",
                "--output", temporaryDirectory.FullName
            ];

            // Act
            Func<Task<int>> task = async () => await Program.Main(args);

            // Assert
            (await task.Should().NotThrowAsync()).Which.Should().Be(0);
            FileInfo newPackageFile = new FileInfo(expectedPackageFile);
            newPackageFile.Exists.Should().BeTrue();
            newPackageFile.Length.Should().BeGreaterThan(initialSize);
        }

        [TestMethod]
        [TestCategory("LocalOnly")]
        public async Task MainTest_Add_Valid()
        {
            // Arrange
            FileInfo packageFile = new FileInfo(FileSystem.Instance.Path.Combine(TestHelper.GetTestFilesDirectory(), "Packages", "SbomTest.1.0.0.dmapp"));
            FileInfo sbomFile = new FileInfo(FileSystem.Instance.Path.Combine(TestHelper.GetTestFilesDirectory(), "SbomFiles", "RandomSbomFile.json"));

            long initialSize = packageFile.Length;

            string[] args =
            [
                "add",
                "--sbom-file", sbomFile.FullName,
                "--package-file", packageFile.FullName,
                "--output", temporaryDirectory.FullName
            ];

            // Act
            Func<Task<int>> task = async () => await Program.Main(args);

            // Assert
            (await task.Should().NotThrowAsync()).Which.Should().Be(0);

            // Check original package
            packageFile.Refresh();
            packageFile.Length.Should().Be(initialSize, because: "the package should not be modified.");

            // Check new package
            temporaryDirectory.Refresh();
            temporaryDirectory.GetFiles().Should().ContainSingle()
                              .Which.Length.Should().BeGreaterThan(initialSize, because: "the package have the added sbom file.");
        }
    }
}