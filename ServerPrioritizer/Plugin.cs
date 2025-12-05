using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Steamworks;
using System.Reflection;
using System.Reflection.Emit;
using Mirror;
using Nessie.ATLYSS.EasySettings;

namespace ServerPrioritizer;

[BepInPlugin(ModInfo.GUID, ModInfo.NAME, ModInfo.VERSION)]
[BepInDependency("EasySettings")]
[BepInProcess("ATLYSS.exe")]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger = null!;

    private static ConfigEntry<string> _desiredLobbyNameConfig = null!;
    private static ConfigEntry<string> _lastJoinedLobbyNameConfig = null!;
    private static string _cachedDesiredLobbyName = null!;

    private void Awake()
    {
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        _desiredLobbyNameConfig = Config.Bind(
            "General",
            "ServerName",
            "Eu 24/7 SFW",
            "Enter the name of the ATLYSS server you want to see at the top of the list here. If left empty, the last joined server will appear on top."
        );

        _lastJoinedLobbyNameConfig = Config.Bind(
            "General",
            "LastJoinedServer",
            "",
            "No need to make any changes here, this field is only used for persistence."
        );

        _cachedDesiredLobbyName = _desiredLobbyNameConfig.Value;

        Logger.LogInfo("EasySettings found – config UI enabled.");

        Settings.OnInitialized.AddListener(() =>
        {
            var tab = Settings.ModTab;
            tab.AddHeader("Server Prioritizer");
            tab.AddTextField("Server Name", _desiredLobbyNameConfig, "Server Name");
        });
        Settings.OnApplySettings.AddListener(() => 
        {
            Config.Save();
            _cachedDesiredLobbyName = _desiredLobbyNameConfig.Value;
            Logger.LogInfo($"Desired server name set to '{_cachedDesiredLobbyName}'");
        });

        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

        Logger.LogInfo($"Desired server name set to '{_cachedDesiredLobbyName}'");
    }

    [HarmonyPatch(typeof(LobbyListManager), nameof(LobbyListManager.Iterate_SteamLobbies))]
    public class IterateSteamLobbiesPatch
    {
        public static void Postfix(LobbyListManager __instance)
        {
            if (string.IsNullOrWhiteSpace(_desiredLobbyNameConfig.Value) && _cachedDesiredLobbyName != _lastJoinedLobbyNameConfig.Value)
            {
                Logger.LogInfo($"Using '{_lastJoinedLobbyNameConfig.Value}' as the desired server name since its empty.");
                _cachedDesiredLobbyName = _lastJoinedLobbyNameConfig.Value;
            }

            if (__instance._lobbyListEntries == null || __instance._lobbyListEntries.Count == 0)
            {
                Logger.LogInfo("_lobbyListEntries is empty");
                return;
            }

            LobbyDataEntry? prioritizedLobbyEntry = null;
            int prioritizedLobbyIndex = -1;

            for (int i = 0; i < __instance._lobbyListEntries.Count; i++)
            {
                LobbyDataEntry lobEntry = __instance._lobbyListEntries[i];

                if (lobEntry != null && lobEntry._lobbyName == _cachedDesiredLobbyName)
                {
                    prioritizedLobbyEntry = lobEntry;
                    prioritizedLobbyIndex = i;
                    break;
                }
            }

            if (prioritizedLobbyEntry != null && prioritizedLobbyIndex != 0)
            {
                __instance._lobbyListEntries.RemoveAt(prioritizedLobbyIndex);
                __instance._lobbyListEntries.Insert(0, prioritizedLobbyEntry);

                prioritizedLobbyEntry.transform.SetSiblingIndex(0);

                Logger.LogInfo($"Moved '{_cachedDesiredLobbyName}' to the top of the server list.");
            }
        }
    }

    [HarmonyPatch(typeof(SteamLobby), nameof(SteamLobby.GetLobbiesList))]
    public static class GetLobbiesListPatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false,
                new CodeMatch(x => x.LoadsConstant()),
                new CodeMatch(x => x.Calls(AccessTools.Method(typeof(SteamMatchmaking), nameof(SteamMatchmaking.AddRequestLobbyListResultCountFilter))))
            );

            if (!matcher.IsValid)
                throw new InvalidOperationException($"Couldn't find location to patch in {nameof(GetLobbiesListPatch)}! Please report this to the mod developer!");
            
            return matcher.InstructionEnumeration();
        }
    }

    [HarmonyPatch(typeof(ServerInfoObject), nameof(ServerInfoObject.Update))]
    public static class OnServerInfoObjectUpdatePatch
    {
        static void Postfix(ServerInfoObject __instance)
        {
            if (NetworkServer.active)
                return; // Ignore servers hosted by the player themselves
            
            if (_lastJoinedLobbyNameConfig.Value == __instance._serverName)
                return;

            Logger.LogInfo($"Storing '{__instance._serverName}' as the last entered server.");
            _lastJoinedLobbyNameConfig.Value = __instance._serverName;
        }
    }
}
