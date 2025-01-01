namespace FileSorter
{
    internal interface IFileParser
    {
        Task Sort(Stream source, Stream target, CancellationToken cancellationToken);
    }
}