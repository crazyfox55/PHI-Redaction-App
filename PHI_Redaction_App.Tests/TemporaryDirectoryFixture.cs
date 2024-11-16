using System;
using System.IO;
using Xunit;

namespace PHI_Redaction_App.Tests
{
    public class TemporaryDirectoryFixture : IDisposable
    {
        public string TempDirectory { get; }
        public string SampleFilePath { get; }

        public TemporaryDirectoryFixture()
        {
            // Set up a temporary directory
            TempDirectory = Path.Combine(Path.GetTempPath(), "PHIRedactionAppTesting");
            Directory.CreateDirectory(TempDirectory);

            // Create a sample file for testing
            SampleFilePath = Path.Combine(TempDirectory, "sampleFile.txt");
            File.WriteAllText(SampleFilePath, """
                Patient Name: John Doe
                Date of Birth: 01/23/1980
                Social Security Number: 123-45-6789
                Address: 123 Main St, Anytown, USA
                Phone Number: (555) 123-4567
                Email: john.doe@example.com
                Medical Record Number: MRN-0012345
                Order Details:
                - Complete Blood Count (CBC)
                - Comprehensive Metabolic Panel (CMP)
                """);
        }

        public void Dispose()
        {
            // Clean up temporary directory and file after tests complete
            if (Directory.Exists(TempDirectory))
            {
                Directory.Delete(TempDirectory, true);
            }
        }
    }
}
