namespace FileSorter
{
    internal interface IFileCreator
    {
        void CreateFile(string name, long size);
    }
}