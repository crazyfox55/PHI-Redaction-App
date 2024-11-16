namespace PHI_Redaction_App.Tests
{
    public class MainViewModelTests : IClassFixture<TemporaryDirectoryFixture>
    {
        private readonly TemporaryDirectoryFixture _fixture;

        public MainViewModelTests(TemporaryDirectoryFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task ProcessFilesCommand_Snapshot()
        {
            var viewModel = new MainViewModel();

            // Set up files and output folder in the temporary directory
            viewModel.SelectedFiles.Add(_fixture.SampleFilePath);
            viewModel.OutputFolderPath = _fixture.TempDirectory;

            viewModel.ProcessCommand.Execute(null);

            var sanitized = await File.ReadAllTextAsync(Path.Combine(_fixture.TempDirectory, Path.GetFileNameWithoutExtension(_fixture.SampleFilePath) + "_sanitized.txt"));

            await Verify(sanitized);
        }
    }
}
