using System;
using System.IO;

namespace Parser
{
    class Program
    {
        static void Main(string[] args)
        {
            Program program = new Program();
            if (!program.ParseCmdLine(args))
            {
                program.PrintHelp();
                return;
            }

            try
            {
                bool simpleMode = String.IsNullOrEmpty(program.Mode) || "0" == program.Mode;
                LogParser logParser = new LogParser(program.InputFile, program.OutputFile, simpleMode);
                logParser.Process();
            }
            catch (Exception eh)
            {
                Console.WriteLine(eh.Message);
            }
        }

        bool ParseCmdLine(string[] args)
        {
            foreach (string arg in args)
            {
                if ('/' != arg[0] && '-' != arg[0])
                {
                    Console.WriteLine("Command parameter doesn't start with '/' or '-'");
                    return false;
                }

                string argString = arg.Substring(1);
                int idx = argString.IndexOf(':');
                if (-1 == idx)
                {
                    Console.WriteLine("Command format wrong '{0}'", arg);
                    return false;
                }

                string[] key_value = new string[] { argString.Substring(0, idx), argString.Substring(idx + 1) };
                switch (key_value[0])
                {
                    case "in":
                        inFile_ = key_value[1];
                        break;

                    case "out":
                        outFile_ = key_value[1];
                        break;

                    case "mode":
                        mode_ = key_value[1];
                        break;

                    default:
                        Console.WriteLine("Unknown argument '{0}', ignored.", arg);
                        break;
                }
            }

            if (String.IsNullOrEmpty(inFile_) || String.IsNullOrEmpty(outFile_))
            {
                Console.WriteLine("-in:filepath1 and -out:filepath2 is required.");
                return false;
            }

            return true;
        }

        void PrintHelp()
        {
            Console.WriteLine();
            Console.WriteLine("{0} -in:filePath1 -out:filePath2 [-mode:0|1]", System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName);
            Console.WriteLine("    -mode        0: using simple mode.  0 will be stripped from the result.");
            Console.WriteLine("                 1: using complete mode.  All result will be presented in the -out file.");
        }

        string InputFile
        {
            get
            {
                return inFile_;
            }
        }

        string OutputFile
        {
            get
            {
                return outFile_;
            }
        }

        string Mode
        {
            get
            {
                return mode_;
            }
        }

        private string inFile_;
        private string outFile_;
        private string mode_;
    }


}
