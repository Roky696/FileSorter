namespace FileSorter
{
    public class ApplicationOptions
    {
        public ApplicationOptions()
        {
            Split = new ExternalMergeSortSplitOptions();
            Sort = new ExternalMergeSortSortOptions();
            Merge = new ExternalMergeSortMergeOptions();
        }

        public string TempDirectoryName { get; set; } = "temp";
        public string UnsortedFileExtension { get; init; } = ".unsorted";
        public string SortedFileExtension { get; init; } = ".sorted";

        public ExternalMergeSortSplitOptions Split { get; init; }
        public ExternalMergeSortSortOptions Sort { get; init; }
        public ExternalMergeSortMergeOptions Merge { get; init; }
    }

    public class ExternalMergeSortSplitOptions
    {
        /// <summary>
        /// File to be sorted will be divided to this size (in bytes)
        /// </summary>
        public int FileSize { get; init; } = 2 * 1024 * 1024;

        public char NewLineSeparator { get; init; } = '\n';
    }

    public class ExternalMergeSortSortOptions
    {
        public IComparer<string> Comparer { get; init; } = new CustomStringComparer();
        public int InputBufferSize { get; init; } = 65536;
        public int OutputBufferSize { get; init; } = 65536;
    }

    public class ExternalMergeSortMergeOptions
    {
        /// <summary>
        /// How many files we will process per run
        /// </summary>
        public int FilesPerRun { get; init; } = 10;

        /// <summary>
        /// Buffer size (in bytes) for input StreamReaders
        /// </summary>
        public int InputBufferSize { get; init; } = 65536;

        /// <summary>
        /// Buffer size (in bytes) for output StreamWriter
        /// </summary>
        public int OutputBufferSize { get; init; } = 65536;
    }
}