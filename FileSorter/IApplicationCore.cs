namespace FileSorter
{
    public interface IApplicationCore
    {
        void CreateFile(string[] args);

        Task SortFile(string[] args);
    }
}