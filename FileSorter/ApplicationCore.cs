using System.Diagnostics;

namespace FileSorter
{
    internal class ApplicationCore : IApplicationCore
    {
        private readonly IFileParser _fileParser;
        private readonly IFileCreator _fileCreator;

        public ApplicationCore()
        {
            _fileParser = new FileParser();
            _fileCreator = new FileCreator();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="args"></param>
        public void CreateFile(string[] args)
        {
            Console.WriteLine($"Creating file with name {args[1]} and size in bytes {args[2]}");
            Stopwatch sw = Stopwatch.StartNew();
            var fileSize = long.Parse(args[2]);
            _fileCreator.CreateFile(args[1], fileSize);
            sw.Stop();
            Console.WriteLine($"File created in {sw.ElapsedMilliseconds} ms");
            Console.WriteLine($"Stats: {GbPerMin(fileSize, sw.ElapsedMilliseconds)} Gb/min");
        }

        /// <summary>
        /// Start sorting file
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public async Task SortFile(string[] args)
        {
            Console.WriteLine($"Sorting file with name {args[1]}");
            Stopwatch sw = Stopwatch.StartNew();
            var unsortedSource = File.OpenRead(args[1]);
            var fileSize = unsortedSource.Length;
            var sortedTarget = File.Create(args[2]);

            await _fileParser.Sort(unsortedSource, sortedTarget, CancellationToken.None);
            sw.Stop();
            Console.WriteLine($"Done sorting in {sw.ElapsedMilliseconds} ms file size {fileSize} in bytes");
            Console.WriteLine("Stats: " + GbPerMin(fileSize, sw.ElapsedMilliseconds) + " Gb/min");
        }

        /// <summary>
        /// Conversion from bytes to gigabits
        /// </summary>
        /// <param name="num">File size in bytes</param>
        /// <returns>File size in gigabits</returns>
        internal double ToGb(long num)
        {
            return num / 125000000.0;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="num"></param>
        /// <param name="miliseconds"></param>
        /// <returns></returns>
        internal double GbPerMin(long num, double miliseconds)
        {
            Console.WriteLine($"File size: {ToGb(num)} Gb");
            Console.WriteLine($"Completed in: {Math.Round((miliseconds / 60000), 2)} mins");
            return Math.Round(ToGb(num) / (miliseconds / 60000), 2);
        }
    }
}