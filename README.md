This project has been retired as of the release of CS2

## Synopsis

BotHATTwaffle - the Source Engine Discord server's overlord. Used for announcing playtests and basic server tasks.

## Settings

The bot will read from a settings.ini next to the EXE. If the file does not exist, on first run the bot will create it and then crash. If you do not provide the Discord bot
token you will keep crashing. If you want to run without the Google Calendar, you can disable the calls to that file inside of the LevelTesting module constructor. 
You'll also need to remove any calls to the calendar object in that file. There are a few of them. LevelTesting is the only module to use the calendar.
