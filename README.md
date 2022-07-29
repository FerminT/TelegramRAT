# TelegramRAT
.NET 4.5 implementation of a Remote Access Tool (RAT) that can be controlled through a Telegram bot. Coded alongside @eezv, @GonzaloRuarte and @facubara for the Information Security course as a group project.

## How does it work?
Two configuration constants have to be specified in ```Config.cs```:
- ```TelegramBot_Token```: Telegram's bot token (obtained from the [BOTFather](https://t.me/botfather) after sending the command ```/NEWBOT```). This will act as a server, from where commands will be uploaded by the attacker and downloaded by the client.
- ```TelegramChat_ID```: ID of the Telegram account to be used to chat with the bot (i.e. *who* will be sending the commands to the client). It can be obtained through the [Chat ID echo bot](https://t.me/chatid_echo_bot), sending the command ```/START```.


Once the program is deployed and executed, a message will be sent from the bot to the Telegram account anouncing a PC is online. The attacker can then begin to send commands to control the infected PC.
Each infected PC possesses a different random ID, so several PCs can be controlled by a single user.

[![Fig-README.png](https://i.postimg.cc/3wnKV3LH/Fig-README.png)](https://postimg.cc/XZBSy6S2)

## Available commands
All commands receive as a parameter the ID of the infected PC. *0* is reserved as a broadcast ID.

     * /popup <id> <text>: a popup containing "text" is shown to the target. 
     * /ipinfo <id>: information about the target's IP and location is retrieved. 
     * /screenshot <id>: a screenshot of the target's screen at the time of execution is retrieved.
     * /download <id> <file>: the file specified by <file> is retrieved.
     * /upload <id> <path>: it indicates to the target where to download the file that will be sent to the bot.
     * /exec <id> <command>: it executes <command> on the target machine, returning its response.
     * /start <id> <appName> <parameters>: the app specified by <appName> with <parameters> parameters is run.
     * /apps <id>: a list of the installed apps is retrieved. (NOT working in Windows 11.)
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
     
### Additional features
Besides the available commands, on start-up, the tool runs a check-up to see if it is being run in a Virtual Machine or Sandboxie. This is controlled by the configuration constant ```checkForVMAndSandboxie``` in ```Config.cs```. Additionally, a different thread is constantly checking for packet analysers in parallel. If one is detected, the tool stops sending and receiving data and waits for its exit.
