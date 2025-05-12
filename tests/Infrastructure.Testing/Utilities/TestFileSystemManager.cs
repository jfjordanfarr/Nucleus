using System;
using System.IO;
using System.Threading.Tasks;

namespace Nucleus.Infrastructure.Testing.Utilities
{
    /// <summary>
    /// Manages the creation and cleanup of a conceptual local file system structure for testing purposes.
    /// This helps simulate the environment where LocalFileArtifactProvider might operate.
    /// </summary>
    public class TestFileSystemManager : IDisposable
    {
        /// <summary>
        /// Gets the root path for all test data managed by this instance.
        /// </summary>
        public string BaseTestPath { get; }

        private bool _disposed = false; // To detect redundant calls

        /// <summary>
        /// Initializes a new instance of the <see cref="TestFileSystemManager"/> class,
        /// creating a unique base directory for test files under the system's temporary path.
        /// </summary>
        public TestFileSystemManager() : this(Path.Combine(Path.GetTempPath(), "NucleusTestEnv", Guid.NewGuid().ToString()))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestFileSystemManager"/> class
        /// with a specific base path for test files.
        /// </summary>
        /// <param name="baseTestPath">The root directory to use for test files and structures.</param>
        public TestFileSystemManager(string baseTestPath)
        {
            BaseTestPath = baseTestPath;
            Directory.CreateDirectory(BaseTestPath); // Ensure the base path itself exists
        }

        /// <summary>
        /// Ensures that a specific relative path exists within the BaseTestPath.
        /// Creates all directories in the path if they don't exist.
        /// </summary>
        /// <param name="relativePath">The relative path to ensure (e.g., "TenantA/Persona1/conversations/ConvX/files").</param>
        /// <returns>The full absolute path to the ensured directory.</returns>
        public string EnsurePathExists(string relativePath)
        {
            var fullPath = Path.Combine(BaseTestPath, relativePath);
            Directory.CreateDirectory(fullPath);
            return fullPath;
        }

        /// <summary>
        /// Creates a test artifact file with specified content at a given relative path and filename.
        /// Ensures the directory structure for the file exists.
        /// </summary>
        /// <param name="relativePath">The relative directory path for the artifact (e.g., "TenantA/Persona1/conversations/ConvX/files").</param>
        /// <param name="fileName">The name of the artifact file (e.g., "test_doc.txt").</param>
        /// <param name="content">The content to write to the artifact file.</param>
        /// <returns>The full absolute path to the created artifact file.</returns>
        public async Task<string> CreateTestArtifactAsync(string relativePath, string fileName, string content)
        {
            var directoryPath = EnsurePathExists(relativePath);
            var filePath = Path.Combine(directoryPath, fileName);
            await File.WriteAllTextAsync(filePath, content);
            return filePath;
        }

        /// <summary>
        /// Deletes a specific file within the test environment.
        /// </summary>
        /// <param name="relativePath">The relative directory path for the file.</param>
        /// <param name="fileName">The name of the file to delete.</param>
        public void DeleteTestArtifact(string relativePath, string fileName)
        {
            var filePath = Path.Combine(BaseTestPath, relativePath, fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        /// <summary>
        /// Cleans up the entire test environment by recursively deleting the BaseTestPath directory.
        /// This should be called after tests are complete (e.g., in a test dispose method).
        /// </summary>
        public void CleanupTestEnvironment()
        {
            if (Directory.Exists(BaseTestPath))
            {
                Directory.Delete(BaseTestPath, recursive: true);
            }
        }

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // Free any other managed objects here.
                // In this class, there are no explicit managed resources to dispose
                // that aren't handled by CleanupTestEnvironment.
            }

            // Free unmanaged resources (unmanaged objects) and override a finalizer below.
            CleanupTestEnvironment(); // This is the main cleanup logic

            _disposed = true;
        }

        /// <summary>
        /// Finalizer to attempt cleanup if Dispose was not called.
        /// </summary>
        ~TestFileSystemManager()
        {
            Dispose(false);
        }
    }
}
