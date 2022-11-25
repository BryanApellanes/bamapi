using Bam.Net.CommandLine;
using Bam.Net.Testing;
using System;
using System.Threading;

namespace Bam.Application
{
    [Serializable]
    class Program : CommandLineTool
    {
        static void Main(string[] args)
        {
            TryWritePid();

            IsolateMethodCalls = false;
            AddSwitches(typeof(ConsoleActions));
            AddConfigurationSwitches();
            ArgumentAdder.AddArguments(args);

            Initialize(args, (a) =>
            {
                Message.PrintLine("Error parsing arguments: {0}", ConsoleColor.Red, a.Message);
                Thread.Sleep(1000);
                Exit(1);
            });
            if (Arguments.Contains("singleProcess"))
            {
                KillExistingProcess();
            }
            if (Arguments.Contains("i"))
            {
                Interactive();
            }
            else if (!ExecuteSwitches(Arguments, new ConsoleActions()))
            {
                Interactive();
            }
        }    
    }
}
