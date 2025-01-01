using FileSorter;
using System.IO;

namespace FileSorterTests
{
    public class FileParserTests
    {
        private FileParser _parser;
        private ApplicationOptions _options;

        [SetUp]
        public void Setup()
        {
            _parser = new FileParser();
            _options = new ApplicationOptions();
        }

        #region Sort

        [Test]
        public async Task SortShouldSortFile()
        {
            //Arrange
            var inputFilePath = "TestFiles/Testfile1.txt";
            var outputFilePath = "testoutputfile.txt";

            var unsortedSource = File.OpenRead(inputFilePath);
            var unsortedSourceLength = unsortedSource.Length;

            var sortedTarget = File.Create(outputFilePath);

            //Act
            await _parser.SortFile(unsortedSource, sortedTarget);

            //Assert
            Assert.That(File.Exists(inputFilePath), Is.True);

            var outputFile = File.OpenRead(outputFilePath);

            Assert.That(outputFile.Length, Is.EqualTo(unsortedSourceLength - 1));

            outputFile.Dispose();

            File.Delete(outputFilePath);
        }

        #endregion Sort

        #region SplitToSeparateFiles

        [Test]
        public async Task SplitToSeparateFiles()
        {
            //Arrange
            var inputFilePath = "TestFiles/Testfile1.txt";
            var directory = AppDomain.CurrentDomain.BaseDirectory + _options.TempDirectoryName;
            Directory.CreateDirectory(directory);
            var unsortedSource = File.OpenRead(inputFilePath);

            //Act
            var data = await _parser.SplitToSeparateFiles(unsortedSource, CancellationToken.None);

            //Assert
            foreach (var entry in data)
            {
                Assert.That(File.Exists(Path.Combine(directory, entry)), Is.True);
            }

            Directory.Delete(directory, true);
        }

        #endregion SplitToSeparateFiles

        #region Sortfiles

        [Test]
        public async Task SortFilesShouldSortFiles()
        {
            //Arrange
            _options.TempDirectoryName = "testSortFiles";
            var inputFilePath = "TestFiles/Testfile1.txt";

            var directory = AppDomain.CurrentDomain.BaseDirectory + _options.TempDirectoryName;
            var fileList = new List<string>();

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            for (int i = 1; i < 11; i++)
            {
                var unsortedFilePath = Path.Combine(directory, $"{i}.unsorted");
                fileList.Add(unsortedFilePath);
                File.Copy(inputFilePath, unsortedFilePath);
            }

            //Act
            var sortedFiles = await _parser.SortFiles(fileList);

            //Assert
            Assert.That(sortedFiles, Is.Not.Null);
            Assert.That(sortedFiles, Has.Count.EqualTo(10));

            foreach (var file in sortedFiles)
            {
                Assert.That(File.Exists(file), Is.True);
            }

            Directory.Delete(directory, true);
        }

        #endregion Sortfiles

        #region SortFileSpecific

        [Test]
        public async Task SortSpecificFileShouldSortFile()
        {
            //Arrange
            _options.TempDirectoryName = "testSortSpecificFile";
            var inputFilePath = "TestFiles/Testfile1.txt";
            var inputFileName = "1.unsorted";
            var directory = AppDomain.CurrentDomain.BaseDirectory + _options.TempDirectoryName;
            var unsortedFilePath = Path.Combine(directory, inputFileName);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.Copy(inputFilePath, unsortedFilePath);

            //Act
            await _parser.SortFileSpecific(unsortedFilePath);

            //Assert
            var filePath = Path.Combine(directory, "1.sorted");
            Assert.That(File.Exists(filePath), Is.True);

            Directory.Delete(directory, true);
        }

        #endregion SortFileSpecific

        #region SortFile

        [Test]
        public async Task SortFileShouldSortFile()
        {
            //Arrange
            _options.TempDirectoryName = "sortFilesTemp";
            var inputFilePath = "TestFiles/TestFile1.txt";

            var directory = AppDomain.CurrentDomain.BaseDirectory + _options.TempDirectoryName;

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            var unsortedFilePath = Path.Combine(directory, $"1.unsorted");
            var sortedFilePath = Path.Combine(directory, $"1.sorted");

            File.Copy(inputFilePath, unsortedFilePath);

            var unsortedStream = File.OpenRead(unsortedFilePath);
            var sortedStream = File.OpenWrite(sortedFilePath);

            //Act
            await _parser.SortFile(unsortedStream, sortedStream);

            //Assert

            Assert.That(File.Exists(sortedFilePath), Is.True);

            var testUnsortedStream = File.OpenRead(unsortedFilePath);
            var testSortedStream = File.OpenRead(sortedFilePath);
            Assert.That(testSortedStream.Length, Is.EqualTo(testUnsortedStream.Length - 1));

            testUnsortedStream.Dispose();
            testSortedStream.Dispose();

            Directory.Delete(directory, true);
        }

        #endregion SortFile

        #region MergeFiles

        [Test]
        public async Task MergeFilesShouldMergeSuppliedFiles()
        {
            //Arrange
            var inputFilePath = "TestFiles/SortedTestFile.txt";

            var directory = AppDomain.CurrentDomain.BaseDirectory + _options.TempDirectoryName;
            var outputFile = Path.Combine(directory, "output.txt");
            var fileList = new List<string>();

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            for (int i = 1; i < 11; i++)
            {
                var unsortedFilePath = Path.Combine(directory, $"{i}.sorted");
                fileList.Add(unsortedFilePath);
                File.Copy(inputFilePath, unsortedFilePath);
            }
            var outputStream = File.OpenWrite(outputFile);

            //Act
            await _parser.MergeFiles(fileList, outputStream, CancellationToken.None);

            //Assert
            outputStream.Dispose();

            Assert.That(File.Exists(outputFile), Is.True);

            foreach (var file in fileList)
            {
                Assert.That(File.Exists(Path.Combine(directory, file)), Is.False);
            }

            Directory.Delete(directory, true);
        }

        #endregion MergeFiles

        #region MergeSeveralFilesToOne

        [Test]
        public async Task MergeSeveralFilesToOne()
        {
            //Arrange
            var inputFilePath = "TestFiles/SortedTestFile.txt";

            var directory = AppDomain.CurrentDomain.BaseDirectory + _options.TempDirectoryName;
            var fileList = new List<string>();

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            for (int i = 1; i < 11; i++)
            {
                var unsortedFilePath = Path.Combine(directory, $"{i}.sorted");
                fileList.Add(unsortedFilePath);
                File.Copy(inputFilePath, unsortedFilePath);
            }

            //Act
            await _parser.MergeSeveralFilesToOne(fileList.ToArray());

            //Assert
            IReadOnlyList<string> files = [.. Directory.GetFiles(directory, $"*{_options.SortedFileExtension}")
                .OrderBy(x =>
            {
                var filename = Path.GetFileNameWithoutExtension(x);
                return int.Parse(filename);
            })];

            Assert.That(files, Has.Count.EqualTo(1));

            foreach (var file in fileList)
            {
                Assert.That(File.Exists(Path.Combine(directory, file)), Is.False);
            }

            Directory.Delete(directory, true);
        }

        #endregion MergeSeveralFilesToOne

        #region Merge

        [Test]
        public async Task MergeShouldMergeFiles()
        {
            //Arrange
            var inputFilePath = "TestFiles/SortedTestFile.txt";

            var directory = AppDomain.CurrentDomain.BaseDirectory + _options.TempDirectoryName;
            var outputFile = Path.Combine(directory, "output.txt");
            var fileList = new List<string>();

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            for (int i = 1; i < 11; i++)
            {
                var unsortedFilePath = Path.Combine(directory, $"{i}.sorted");
                fileList.Add(unsortedFilePath);
                File.Copy(inputFilePath, unsortedFilePath);
            }
            var outputStream = File.OpenWrite(outputFile);

            //Act
            await _parser.Merge(fileList, outputStream, CancellationToken.None);

            //Assert
            Assert.That(File.Exists(outputFile), Is.True);

            foreach (var file in fileList)
            {
                Assert.That(File.Exists(Path.Combine(directory, file)), Is.False);
            }

            Directory.Delete(directory, true);
        }

        #endregion Merge

        #region InitializeStreamReaders

        [Test]
        public async Task InitializeStreamReadersShouldInitializeStreamReaders()
        {
            //Arrange
            var inputFilePath = "TestFiles/Testfile1.txt";
            var directory = AppDomain.CurrentDomain.BaseDirectory + _options.TempDirectoryName;
            var fileList = new List<string>();

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            for (int i = 1; i < 6; i++)
            {
                var unsortedFilePath = Path.Combine(directory, $"{i}.sorted");
                fileList.Add(unsortedFilePath);
                File.Copy(inputFilePath, unsortedFilePath);
            }
            //Act
            var (StreamReaders, rows) = await _parser.InitializeStreamReaders(fileList);

            //Assert
            Assert.That(rows, Is.Not.Null);
            Assert.That(rows, Is.Not.Empty);

            foreach (var reader in StreamReaders)
            {
                var line = reader.ReadLine();
                Assert.That(line, Is.Not.Null);
                Assert.That(line, Is.Not.Empty);
            }
            foreach (var reader in StreamReaders)
            {
                reader.Dispose();
            }
            Directory.Delete(directory, true);
        }

        #endregion InitializeStreamReaders

        #region CleanupRun

        [Test]
        public void CleanupRunShouldCleanupAllFilesInTempDirectory()
        {
            //Arrange
            var inputFilePath = "TestFiles/Testfile1.txt";
            var directory = AppDomain.CurrentDomain.BaseDirectory + _options.TempDirectoryName;

            var fileList = new List<string>();

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            for (int i = 1; i < 6; i++)
            {
                var unsortedFilePath = Path.Combine(directory, $"{i}.sorted");
                fileList.Add(unsortedFilePath);
                File.Copy(inputFilePath, unsortedFilePath);
            }

            var streamReaders = new StreamReader[fileList.Count];
            for (int i = 0; i < streamReaders.Length; i++)
            {
                var sortedFilePath = Path.Combine(directory, fileList[i]);
                var sortedFileStream = File.OpenRead(sortedFilePath);
                streamReaders[i] = new StreamReader(sortedFileStream, bufferSize: _options.Merge.InputBufferSize);
            }

            //Act
            _parser.CleanupRun(streamReaders, fileList);

            //Assert
            foreach (var file in fileList)
            {
                Assert.That(File.Exists(Path.Combine(directory, file)), Is.False);
            }
            Directory.Delete(directory, true);
        }

        #endregion CleanupRun

        #region CreateDirIfNotExist

        [Test]
        public void CreateDirIfNotExistShouldCreateDir()
        {
            //Arrange
            var directory = AppDomain.CurrentDomain.BaseDirectory + _options.TempDirectoryName;
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }

            //Act
            _parser.CreateDirIfNotExist();

            //Assert
            Assert.That(Directory.Exists(directory), Is.True);
            Directory.Delete(directory, true);
        }

        #endregion CreateDirIfNotExist

        #region DeleteDir

        [Test]
        public void DeletDirShouldDeleteDir()
        {
            //Arrange
            var dirName = _parser.GetTempFolderPath();
            Directory.CreateDirectory(dirName);

            //Act
            _parser.DeleteDir();

            //Assert
            Assert.That(Directory.Exists(dirName), Is.False);
        }

        #endregion DeleteDir

        #region GetTempFolderPath

        [Test]
        public void GetTempFolderPathShouldReturnPath()
        {
            //Arrange
            //Act
            var path = _parser.GetTempFolderPath();

            //Assert
            Assert.That(path, Is.Not.Null);
            Assert.That(path, Is.Not.Empty);
        }

        #endregion GetTempFolderPath

        #region GetTempFolder

        [Test]
        public void GetTempFolderShouldReturnProperPath()
        {
            //Arrange
            var fileName = "test";

            //Act
            var path = _parser.GetTempFolder(fileName);

            //Assert
            Assert.That(path, Is.Not.Null);
            Assert.That(path, Is.Not.Empty);
            Assert.That(path, Does.Contain(fileName));
        }

        #endregion GetTempFolder
    }
}