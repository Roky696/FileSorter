using System.Diagnostics;

namespace FileSorter
{
    internal static class Program
    {
        private static async Task<int> Main(string[] args)
        {
            var applicationCore = new ApplicationCore();

            if (args.Length == 0)
            {
                Console.WriteLine("no params supplied exiting");
                return 1;
            }

            switch (args[0])
            {
                case "c":
                case "-c":
                case "--create":
                case "create":
                    {
                        applicationCore.CreateFile(args);
                        break;
                    }
                case "s":
                case "-s":
                case "--sort":
                case "sort":
                    {
                        await applicationCore.SortFile(args);
                        break;
                    }
                case "?":
                case "-h":
                case "h":
                case "--help":
                case "help":
                    {
                        ShowHelp();
                        break;
                    }
                default:
                    {
                        Console.WriteLine("Mode not specified, use -h for help, exiting...");
                        break;
                    }
            }
            return 0;
        }

        /// <summary>
        ///
        /// </summary>
        private static void ShowHelp()
        {
            Console.WriteLine("For file creation use -c or --create, specifie file name and size in bytes");
            Console.WriteLine("Example: FileSorter.exe -c test.txt 5000");
            Console.WriteLine("For file sorting use -s or --sort, specifie source file name and target file name");
            Console.WriteLine("Example: FileSorter.exe -s test.txt testsorted.txt");
        }
    }
}