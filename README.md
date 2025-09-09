## SimpleDiscordRelay

A Counter-Strike 2 plugin that provides seamless integration between your CS2 server and Discord using CounterStrikeSharp.

## Features

### Chat Relay
- **Bidirectional Chat**: Real-time synchronization between in-game chat and Discord channels
- **Team Chat Support**: Team chat messages are also relayed to Discord
- **Discord to Game**: Discord messages appear in the game chat
- **Custom Emojis & Stickers**: Display Discord emojis and stickers in-game via HUD

### Player Notifications
- **Join/Leave Alerts**: Automatic Discord notifications when players connect/disconnect
- **GeoIP Integration**: Shows player country with flag emojis
- **Steam Profile Links**: Clickable player names that link to Steam profiles
- **In-game Notifications**: Optional in-game announcements for player connections

### Game Events
- **Map Change Alerts**: Automatic Discord notifications when the map changes
- **Bot Status Updates**: Discord bot status shows current map and player count
- **Map Blacklist**: Configure which maps to exclude from notifications

### Special Features
- **HUD Messages**: Display Discord messages with emojis directly on player HUDs
- **Discord Invite Announcer**: Periodic in-game announcements of your Discord server
- **Message Queue**: Rate-limited message sending to respect Discord API limits
- **Reconnection Handling**: Smart handling of player reconnections to avoid spam

## ⚙️ Installation & Setup

### Prerequisites
- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) installed and configured
- Discord bot with proper permissions (Send Messages, Embed Links, Read Message History)

### Installation
1. Download the latest release zip file from the [Releases](../../releases) page
2. Extract the zip file to your CS2 server root directory
3. If GeoLite2-Country.mmdb is not included, download it from [MaxMind](https://www.maxmind.com/en/open-source-data-and-api-for-ip-geolocation) and place it in:
   ```
   addons/counterstrikesharp/plugins/SimpleDiscordRelay/GeoIP/GeoLite2-Country.mmdb
   ```
I highly recommend installing this plugin: [CS2FlashingHtmlHudFix](https://github.com/girlglock/CS2FlashingHtmlHudFix)
### Configuration
The plugin will automatically generate a configuration file on first run at:
```
addons/counterstrikesharp/configs/plugins/SimpleDiscordRelay/simplediscordrelay.json
```

#### Essential Settings
```json
{
  "Bot Token": "YOUR_DISCORD_BOT_TOKEN",
  "Chat Relay Channel ID": 1234567890123456789,
  "Notification Channel ID": 1234567890123456789,
  "Enable Chat Relay": true,
  "Enable Discord To Game Relay": true
}
```

### Discord Bot Setup
1. Create a Discord application at [Discord Developer Portal](https://discord.com/developers/applications)
2. Create a bot and copy the token
3. Invite the bot to your server with these permissions:
   - Send Messages
   - Embed Links
   - Read Message History
   - Use External Emojis

## Customization

### Message Formats
Customize how messages appear in both Discord and in-game:

```json
{
  "Discord To Game Format": " [Discord] {user} ({username}): {message}",
  "Discord To Game Hud Format": "<center><font color='white'>{user}</font><br>{emojis}<br>{message}</center>",
  "Join Message": "has connected.",
  "Leave Message": "has disconnected."
}
```

### Bot Status
Configure your bot's Discord status:

```json
{
  "Bot Status Settings": {
    "Enable Bot Status": true,
    "Update Interval (Seconds)": 60.0,
    "Activity Type": 3,
    "Status Text Format": "{map} | {players}/{max_players} players"
  }
}
```

### Colors
Customize embed colors for different message types:
- `Join Embed Color`: Player join messages (default: green)
- `Leave Embed Color`: Player leave messages (default: red)
- `Chat Embed Color`: Chat messages (default: light blue)
- `Map Change Embed Color`: Map change notifications (default: blue)

## Commands

The plugin automatically hooks into CS2 chat commands:
- `say` - Public chat (relayed to Discord)
- `say_team` - Team chat (relayed to Discord with team prefix)

## Technical Details

- **Framework**: .NET 8.0
- **Dependencies**: 
  - Discord.Net 3.15.3
  - MaxMind.GeoIP2 5.2.0
  - CounterStrikeSharp API
- **Rate Limiting**: Built-in message queue with 300ms delays
- **Error Handling**: Comprehensive exception handling and logging

## Troubleshooting

### Common Issues
1. **Bot not responding**: Check if the bot token is correct and the bot is online
2. **Messages not relaying**: Verify channel IDs and bot permissions
3. **GeoIP not working**: Ensure GeoLite2-Country.mmdb is in the correct location
4. **HUD messages not showing**: Check if `Enable Hud Message For Emoji` is enabled

---

**Note**: Make sure to keep your Discord bot token secure and never commit it to public repositories.
