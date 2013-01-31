using System;
using System.IO;

namespace Parser
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                LogParser logParser = new LogParser(args[0], args[1]);
                logParser.Process();
            }
            catch (Exception eh)
            {
                Console.WriteLine(eh.Message);
            }
        }
    }


}
