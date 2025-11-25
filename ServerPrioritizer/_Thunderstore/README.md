# ServerPrioritizer

A BepInEx plugin for **ATLYSS** that automatically moves your favorite server to the top of the in-game server browser.

--- 

## ⚠️ Dependency 

This mod **requires [EasySettings](https://thunderstore.io/c/atlyss/p/Nessie/EasySettings/)** to function.  
Make sure you have it installed, or the plugin will not load.

---

## 📦 Features

- Prioritizes a specific server in the lobby list based on its name  
- Automatically uses your **last visited server** if no ServerName is set  
- Makes it easier to find and join your preferred server  
- Removes Steam’s 60-result limit on lobby queries

---

## 📥 Installation

1. Download the compiled `ServerPrioritizer.dll`.  
2. Place it in your `BepInEx/plugins` folder.  
3. Launch the game once to generate a config file.  
4. Edit the generated config at:  
   `BepInEx/config/com.fleetime.serverprioritizer.cfg`

---

## ⚙️ How It Works

- Scans the server list for a lobby matching your configured **ServerName**.  
- If found, it reorders the list so your server appears first.  
- If **ServerName** is left blank, the plugin automatically detects and prioritizes your **last visited server**.  
- Patches out Steam’s result count filter (`AddRequestLobbyListResultCountFilter(60)`) to show more than 60 lobbies.

---

## 📋 Config Options

| Key         | Default       | Description                                 |
| ----------- | ------------- | ------------------------------------------- |
| ServerName  | `Eu 24/7 SFW` | The exact name of the server to prioritize. Leave blank to use your last visited server. |

---

## 🆘 Troubleshooting

- Server not moving to the top? Double-check that the **ServerName** in `BepInEx/config/com.fleetime.serverprioritizer.cfg` matches exactly as it appears in-game.  
  - It’s case-sensitive (`Eu 24/7 SFW` ≠ `eu 24/7 sfw`).  
  - Check for trailing spaces or special characters.  
- If **ServerName** is blank, ensure you’ve joined a server at least once so it can record your last visited server.  
- Watch the BepInEx console/log for messages like:  
  `Moved 'Eu 24/7 SFW' to the top of the server list.`  
  If you don’t see these, the name isn’t matching or no previous server is stored.
