using System.Text;

namespace FileSorter
{
    public class FileCreator : IFileCreator
    {
        private readonly Random random = new();
        private const int RandomTextWordsMax = 30;
        private const int RandomTextWordsMin = 1;
        private const int RandomNumberMax = 10000000;
        private const char _wordDelimiter = ' ';
        private const string _sectionDelimiter = ". ";

        /// <summary>
        /// Create and populate specified file, file size is specified bytes plus 1 line.
        /// </summary>
        /// <param name="name">File name to be created</param>
        /// <param name="size">File size to be created</param>
        public void CreateFile(string name, long size)
        {
            var fileStream = File.Create(name);

            fileStream.Position = 0;
            do
            {
                fileStream.Write(Encoding.UTF8.GetBytes(CreateLine()));
            } while (fileStream.Length < size);
            fileStream.Close();
        }

        /// <summary>
        /// Create single line.
        /// </summary>
        /// <returns>String containg full line</returns>
        internal string CreateLine()
        {
            return random.Next(RandomNumberMax).ToString() + _sectionDelimiter + CreateText() + Environment.NewLine;
        }

        /// <summary>
        /// Create text part of line.
        /// </summary>
        /// <returns>Complete text part up to defined max words</returns>
        internal string CreateText()
        {
            var sb = new StringBuilder();

            var words = random.Next(RandomTextWordsMin, RandomTextWordsMax);

            for (int i = 0; i < words; i++)
            {
                sb.Append(Words[random.Next(Words.Length - 1)]);
                sb.Append(_wordDelimiter);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Words to be used as text part of line.
        /// </summary>
        private readonly string[] Words =
        [
            "Blue", "Yellow", "Sky", "is", "smt", "whatever", "nothing", "casual", "people", "part", "etc"
        ];
    }
}