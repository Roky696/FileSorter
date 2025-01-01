using System.Collections.Concurrent;

namespace FileSorter
{
    public class FileParser : IFileParser
    {
        private readonly ApplicationOptions _options;

        private long _maxUnsortedRows = 128 * 1024;
        private ConcurrentBag<string> _sortedFiles = [];
        private readonly Random _Random = new();

        public FileParser()
        {
            _options = new ApplicationOptions();
        }

        /// <summary>
        /// First we create temporary directory to store all files that will be used in sorting/merging.
        /// When sorting large files its often impossible to load it to ram, so we are dividing that huge file to many smaller.
        /// Then we sort each file and slowly merging it again to one file.
        /// Algorith that will be used is k-way merge.
        /// </summary>
        /// <param name="source">Initial stream with supplied unsorted file.</param>
        /// <param name="target">Final stream to write sorted file.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task Sort(Stream source, Stream target, CancellationToken cancellationToken)
        {
            CreateDirIfNotExist();

            var files = await SplitToSeparateFiles(source, cancellationToken);

            if (files.Count == 1)
            {
                var unsortedFilePath = GetTempFolder(files.First());
                await SortFile(File.OpenRead(unsortedFilePath), target);
                return;
            }
            var sortedFiles = await SortFiles(files);
            var done = false;
            var size = _options.Merge.FilesPerRun;
            var result = sortedFiles.Count / size;

            while (!done)
            {
                if (result <= 0)
                {
                    done = true;
                }
                result /= size;
            }

            await MergeFiles(sortedFiles, target, cancellationToken);
            DeleteDir();
        }

        /// <summary>
        /// Method divides input file to maxsize, then returns readonly list with unsorted file names.
        /// </summary>
        /// <param name="sourceStream">Input file stream to be divided to smaller files.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of divided file names.</returns>
        internal async Task<IReadOnlyCollection<string>> SplitToSeparateFiles(Stream sourceStream, CancellationToken cancellationToken)
        {
            var singleFileSize = _options.Split.FileSize;
            var buffer = new byte[singleFileSize];
            var extraBuffer = new List<byte>();
            var fileNames = new List<string>();

            await using (sourceStream)
            {
                var currentFile = 0L;
                while (sourceStream.Position < sourceStream.Length)
                {
                    long totalRows = 0;
                    var runBytesRead = 0;
                    while (runBytesRead < singleFileSize)
                    {
                        var value = sourceStream.ReadByte();
                        if (value == -1)
                        {
                            break;
                        }

                        var @byte = (byte)value;
                        buffer[runBytesRead] = @byte;
                        runBytesRead++;
                        if (@byte == _options.Split.NewLineSeparator)
                        {
                            totalRows++;
                        }
                    }

                    var extraByte = buffer[singleFileSize - 1];

                    while (extraByte != _options.Split.NewLineSeparator)
                    {
                        var flag = sourceStream.ReadByte();
                        if (flag == -1)
                        {
                            break;
                        }
                        extraByte = (byte)flag;
                        extraBuffer.Add(extraByte);
                    }

                    var filename = $"{++currentFile}.unsorted";

                    await using var unsortedFile = File.Create(GetTempFolder(filename));
                    await unsortedFile.WriteAsync(buffer, 0, runBytesRead, cancellationToken);
                    if (extraBuffer.Count > 0)
                    {
                        await unsortedFile.WriteAsync([.. extraBuffer], 0, extraBuffer.Count, cancellationToken);
                    }

                    if (totalRows > _maxUnsortedRows)
                    {
                        _maxUnsortedRows = totalRows;
                    }

                    fileNames.Add(filename);
                    extraBuffer.Clear();
                }
            }
            return fileNames;
        }

        /// <summary>
        /// Initialize concurrent sorters for all supplied unsorted files.
        /// </summary>
        /// <param name="unsortedFiles">Unsorted file names.</param>
        /// <returns>Sorted file name list.</returns>
        internal async Task<IReadOnlyList<string>> SortFiles(
                IReadOnlyCollection<string> unsortedFiles)
        {
            _sortedFiles = [];
            List<Task> TaskList = [];
            foreach (var unsorted in unsortedFiles)
            {
                TaskList.Add(SortFileSpecific(unsorted));
            }

            await Task.WhenAll([.. TaskList]);
            return [.. _sortedFiles];
        }

        /// <summary>
        /// Task to sort single file.
        /// </summary>
        /// <param name="unsortedFile">Single file name to be sorted</param>
        /// <returns>Task.</returns>
        internal async Task SortFileSpecific(string unsortedFile)
        {
            var sortedFilename = unsortedFile.Replace(_options.UnsortedFileExtension, _options.SortedFileExtension);
            var unsortedFilePath = GetTempFolder(unsortedFile);
            var sortedFilePath = GetTempFolder(sortedFilename);
            await SortFile(File.OpenRead(unsortedFilePath), File.OpenWrite(sortedFilePath));
            _sortedFiles.Add(sortedFilename);
            File.Delete(unsortedFilePath);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="unsortedFile">Stream with open input unsorted file.</param>
        /// <param name="target">Stream to write final sorted file.</param>
        /// <returns>Task.</returns>
        internal async Task SortFile(Stream unsortedFile, Stream target)
        {
            using var streamReader = new StreamReader(unsortedFile, bufferSize: _options.Sort.InputBufferSize);
            string[] _unsortedRows = new string[_maxUnsortedRows];
            var counter = 0;
            while (!streamReader.EndOfStream)
            {
                _unsortedRows[counter++] = (await streamReader.ReadLineAsync())!;
            }

            Array.Sort(_unsortedRows, _options.Sort.Comparer);
            await using var streamWriter = new StreamWriter(target, bufferSize: _options.Sort.OutputBufferSize);

            foreach (var row in _unsortedRows.Where(x => x is not null))
            {
                await streamWriter.WriteLineAsync(row);
            }
        }

        /// <summary>
        /// After sorting files should be merged in chunks. Each run all chunks merging are run in concurrent.
        /// If there are less files than single chunk limit, only single merging task is created.
        /// </summary>
        /// <param name="sortedFiles">List of all files to merge</param>
        /// <param name="target">Stream to write final output file.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task.</returns>
        internal async Task MergeFiles(
            IReadOnlyList<string> sortedFiles, Stream target, CancellationToken cancellationToken)
        {
            var done = false;
            while (!done)
            {
                var finalRun = sortedFiles.Count <= _options.Merge.FilesPerRun;

                if (finalRun)
                {
                    await Merge(sortedFiles, target, cancellationToken);
                    return;
                }

                var runs = sortedFiles.Chunk(_options.Merge.FilesPerRun);

                List<Task> TaskList = [];
                foreach (var singleChunk in runs)
                {
                    TaskList.Add(MergeSeveralFilesToOne(singleChunk));
                }
                await Task.WhenAll([.. TaskList]);

                sortedFiles = [.. Directory.GetFiles(GetTempFolderPath(), $"*{_options.SortedFileExtension}")
                    .OrderBy(x =>
                    {
                        var filename = Path.GetFileNameWithoutExtension(x);
                        return int.Parse(filename);
                    })];

                if (sortedFiles.Count > 1)
                {
                    continue;
                }

                done = true;
            }
        }

        /// <summary>
        /// Initialize task with mergin several files to one, generate random file prefix.
        /// </summary>
        /// <param name="files">File path to merge.</param>
        /// <returns>Task.</returns>
        internal async Task MergeSeveralFilesToOne(string[] files)
        {
            var outputFilename = $"{_Random.Next((int)_maxUnsortedRows)}{_options.SortedFileExtension}";

            if (files.Length == 1)
            {
                return;
            }

            var outputStream = File.OpenWrite(GetTempFolder(outputFilename));
            await Merge(files, outputStream, CancellationToken.None);
        }

        /// <summary>
        /// Merge specific files to one, call methods to remove those after merge.
        /// </summary>
        /// <param name="filesToMerge">Specified files to merge.</param>
        /// <param name="outputStream">Stream to write output to specific file.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Task.</returns>
        internal async Task Merge(
            IReadOnlyList<string> filesToMerge,
            Stream outputStream,
            CancellationToken cancellationToken)
        {
            var (streamReaders, rows) = await InitializeStreamReaders(filesToMerge);
            var finishedStreamReaders = new List<int>(streamReaders.Length);
            var done = false;

            await using (var outputWriter = new StreamWriter(outputStream, bufferSize: _options.Merge.OutputBufferSize))
            {
                while (!done)
                {
                    rows.Sort((row1, row2) => _options.Sort.Comparer.Compare(row1.Value, row2.Value));
                    var valueToWrite = rows[0].Value;
                    var streamReaderIndex = rows[0].StreamReader;
                    await outputWriter.WriteLineAsync(valueToWrite.AsMemory(), cancellationToken);

                    if (streamReaders[streamReaderIndex].EndOfStream)
                    {
                        var indexToRemove = rows.FindIndex(x => x.StreamReader == streamReaderIndex);
                        rows.RemoveAt(indexToRemove);
                        finishedStreamReaders.Add(streamReaderIndex);
                        done = finishedStreamReaders.Count == streamReaders.Length;
                        continue;
                    }

                    var value = await streamReaders[streamReaderIndex].ReadLineAsync(cancellationToken);
                    rows[0] = new Row { Value = value!, StreamReader = streamReaderIndex };
                }
            }

            CleanupRun(streamReaders, filesToMerge);
        }

        /// <summary>
        /// Creates a StreamReader for each sorted sourceStream.
        /// Reads one line per StreamReader to initialize the rows list.
        /// </summary>
        /// <param name="sortedFiles">Files to be open.</param>
        /// <returns>Tuple containing and each row.</returns>
        internal async Task<(StreamReader[] StreamReaders, List<Row> rows)> InitializeStreamReaders(
            IReadOnlyList<string> sortedFiles)
        {
            var streamReaders = new StreamReader[sortedFiles.Count];
            var rows = new List<Row>(sortedFiles.Count);
            for (var i = 0; i < sortedFiles.Count; i++)
            {
                var sortedFilePath = GetTempFolder(sortedFiles[i]);
                var sortedFileStream = File.OpenRead(sortedFilePath);
                streamReaders[i] = new StreamReader(sortedFileStream, bufferSize: _options.Merge.InputBufferSize);
                rows.Add(new Row
                {
                    Value = (await streamReaders[i].ReadLineAsync())!,
                    StreamReader = i
                });
            }

            return (streamReaders, rows);
        }

        /// <summary>
        /// Disposes all StreamReaders
        /// Renames old files to a temporary name and then deletes them.
        /// Reason for renaming first is that large files can take quite some time to remove
        /// and the .Delete call returns immediately.
        /// </summary>
        /// <param name="streamReaders">Open stream readers that are open in specific run.</param>
        /// <param name="filesToMerge">Files to be removed.</param>
        internal void CleanupRun(StreamReader[] streamReaders, IReadOnlyList<string> filesToMerge)
        {
            foreach (var reader in streamReaders)
            {
                reader.Dispose();
            }

            foreach (var file in filesToMerge)
            {
                var temporaryFilename = $"{file}.removal";
                File.Move(GetTempFolder(file), GetTempFolder(temporaryFilename));
                File.Delete(GetTempFolder(temporaryFilename));
            }
        }

        /// <summary>
        /// Create temporary directory, directory is defined in options
        /// </summary>
        internal void CreateDirIfNotExist()
        {
            var path = GetTempFolderPath();

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        /// <summary>
        /// Delete directory defined in options and its content.
        /// </summary>
        internal void DeleteDir()
        {
            var path = GetTempFolderPath();

            bool exists = Directory.Exists(path);
            if (exists)
            {
                Directory.Delete(path, true);
            }
        }

        /// <summary>
        /// Create absolute path to specific directory defined in options.
        /// </summary>
        /// <returns>Path to directory.</returns>
        internal string GetTempFolderPath()
        {
            return AppDomain.CurrentDomain.BaseDirectory + _options.TempDirectoryName;
        }

        /// <summary>
        /// Create absolute path to specific file.
        /// </summary>
        /// <param name="filename">File name to be included in path.</param>
        /// <returns>Path.</returns>
        internal string GetTempFolder(string filename)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.Combine(_options.TempDirectoryName, filename));
        }
    }
}