# DiscordMusicBot 🎵

**DiscordMusicBot** is a simple and powerful music bot for Discord, built using **C#**. This bot allows users to enjoy their favorite tunes directly in their Discord servers with commands to play, pause, skip, and more.

---

## Features

- 🎶 **Play music** from YouTube or other sources.
- ⏸️ **Pause, Resume, and Stop** music playback.
- ⏭️ **Skip tracks** to play the next song in the queue.
- 📜 **Queue management** for seamless playback.
- 📢 **Dynamic voice channel support**.
- ⚡ **Fast and lightweight** with intuitive commands.

---

## Installation

### Prerequisites

- **.NET 8 SDK** installed on your system.
- A Discord bot account with a valid **Bot Token**. If you don’t have one, create a bot here: (https://discord.com/developers/applications).

---

### Setup

1. **Clone the repository**:
   ```bash
   git clone https://github.com/yourusername/DiscordMusicBot.git
   cd DiscordMusicBot
   ```
2. **Create a .env file**
	The bot requires a **.env** file to securely store the bot token. Create a .env file in the root of the project directory and add your bot token:
	```env
	BOT_TOKEN=discord_bot_token
	```
	Replace **discord_bot_token** with the token from your Discord Developer Portal.
3. **Install dependencies:**
	Run the following command to restore dependencies:
	```bash
	dotnet restore
	```
4. **Run the bot:**
	Start the bot by running:
	```bash
	dotnet run
	```

---

## Usage

- Once the bot is running:

1.	Invite the bot to your Discord server using the OAuth2 URL from the Discord Developer Portal.
2.  Use the provided commands to interact with the bot. (e.g., **!play <song name or URL>**)

---

| Command            | Category         | Description                                    |
|--------------------|------------------|------------------------------------------------|
| `!join`            | Bot Controls     | Joining the voice channel.                      |
| `!play <song>`     | Music Controls   | Plays the specified song or URL.              |
| `!stop`            | Music Controls   | Stops the playback and clears the queue.      |


## Contributing

- Contributions are welcome! Feel free to open issues or submit pull requests to improve the bot.

## License

- This project is licensed under the MIT License. See the LICENSE file for details.

- Happy listening! 🎧
