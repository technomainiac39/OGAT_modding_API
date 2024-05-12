using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using HarmonyLib.Tools;
using CodeStage.AntiCheat.Detectors;
using SG.OGAT;
using SG.OGAT.State;
using SG.Util;
using System.Reflection;
using UnityEngine.UI;
using System.Linq;
using UnityEngine;
using UnityEngine.Windows.Speech;
using System.Collections.Generic;
using System.Collections;
using System.Drawing;
using System;
using SG.Transport;
using static System.Net.Mime.MediaTypeNames;
using System.Runtime.CompilerServices;
using Steamworks;

namespace OGAT_modding_API      //look to server info class to get admin name for commands (found through server list do not know how to get from a lobby yet)
{

    [BepInPlugin("OGAT_modding_API", MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class OGAT_Plugin : BaseUnityPlugin
    {
        private void Awake()    //patches all of the methods
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
            var harmony = new Harmony("com.technomainiac.OGAT_modding_API");
            harmony.PatchAll();
        }

        public void Start()
        {
            API_Methods.commands["list"] = API_Methods.ListPlayers;
            API_Methods.commands["host"] = API_Methods.SendHostName;
        }

        public void Update()    //updates plugin, will be used eventually to open mod menu
        {
        }
    }

    ///////////////////Methods for API//////////////

    public class API_Methods    
    {
        //public static List<string> commands = new List<string>();
        public static IDictionary<int, string> UsersAndIds = new Dictionary<int, string>();
        public static IDictionary<string, Func<List<string>,bool>> commands = new Dictionary<string, Func<List<string>, bool>>();

        public SG.OGAT.Chat chat;

        public API_Methods()    //adds all commands and their methods to command dict
        {
            commands.Add("list", ListPlayers);
            commands.Add("host", SendHostName);
        }

        public void LogToOgatChat()
        {
        }

        static public void CheckForCommand(string command, string username)  //takes text from chat and searches for command
        {
            var myLogSource = new ManualLogSource("OGAT_MODDING_API");
            BepInEx.Logging.Logger.Sources.Add(myLogSource);

            string[] message = command.Split(' ');

            myLogSource.LogInfo(message[1]);
            if (message[1].StartsWith("#"))
            {
                string comm = message[1].Trim('#');
                foreach(KeyValuePair<string, Func<List<string>, bool>> kvp in commands)
                {
                    if (comm == kvp.Key)
                    {
                        myLogSource.LogInfo($"Running Command: {comm}");
                        BepInEx.Logging.Logger.Sources.Remove(myLogSource);

                        List<string> param = new List<string>();
                        param.Add(command);
                        param.Add(username);
                        
                        kvp.Value(param);
                    }
                }
            }
        }

        static public bool ListPlayers(List<string> comm_and_user)    //finds all players in lobby and stores netID and username
        {   
            UsersAndIds.Clear();
            var myLogSource = new ManualLogSource("OGAT_MODDING_API");
            BepInEx.Logging.Logger.Sources.Add(myLogSource);

            for (int i = 0; i < NetPlayer.All.Count; i++)
            {
                string user = NetPlayer.All[i].profile.username;
                int ID = NetPlayer.All[i].NetId;

                UsersAndIds.Add(ID, user);
                myLogSource.LogInfo($"ListCommand Player{i}: {ID}, {user}");
            }

            BepInEx.Logging.Logger.Sources.Remove(myLogSource);
            return true;
        }

        static public NetPlayer FindPlayer(string username) //finds a player by username, by using the netId stored in player list
        {
            foreach(KeyValuePair<int, string> kvp in UsersAndIds)
            {
                if (kvp.Value == username)
                {
                    NetPlayer player = NetPlayer.find_by_net_id(kvp.Key, false);
                    return player;
                }
            }
            return null;
        }

        static public string GetHostName()      //might not need this should be able to use netcode.ISHost to use as bool to check insead of check a users username
        {
            return ServerPatches.Hostname;
        }


        static public bool SendHostName(List<string> comm_and_user)     //sends the hostname to ogat chat, command is #host
        {
            string text = $"The Host is {ServerPatches.Hostname}";

            SendLobbyMessage(string.Empty, text, true);
            return true;
        }

        static public void SendLobbyMessage(string username, string message, bool setTrue)  //sends message to lobby chat set username to string.Empty for system message
        {
            Singleton<Lobby>.I.AddChatLine(username, message, setTrue);
        }

    }

    ///////////////////Server Patches for Hostname and shit///////////////////////
    public delegate void ServerConnect();                                                   //delegate for the event

    [HarmonyPatch]
    public class ServerPatches
    {
        public static event ServerConnect ConnectedToNewServer;     //the event that tracks when joining a server

        public static string Hostname;
        
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Game), "Server_start")]
        public static void GetHostName(Game __instance)
        {
            var myLogSource = new ManualLogSource("OGAT_MODDING_API");
            BepInEx.Logging.Logger.Sources.Add(myLogSource);

            Hostname = NetPlayer.Mine.profile.username;

            myLogSource.LogInfo($"You are the server host");
            BepInEx.Logging.Logger.Sources.Remove(myLogSource);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ConnectToServer), "OnConnectedToServer", MethodType.Normal)]
        public static bool OnCOnnectedToServerPatch(ConnectToServer __instance)
        {
            var myLogSource = new ManualLogSource("OGAT_MODDING_API");
            BepInEx.Logging.Logger.Sources.Add(myLogSource);

            Hostname = __instance.GBJJJFLIFNE.adminName;

            myLogSource.LogInfo($"Server hostname is: {Hostname}");
            BepInEx.Logging.Logger.Sources.Remove(myLogSource);

            ConnectedToNewServer?.Invoke(); //invokes the connected to server event

            return true;
        }
        
    }

    ///////////////////Anticheat Patches///////////////////////

    [HarmonyPatch(typeof(CodeStage.AntiCheat.Detectors.InjectionDetector), "OnCheatingDetected")]
    public class InjectionPatch0
    {

        [HarmonyPrefix, HarmonyDebug]
        public static bool DisableInjectionCheat(CodeStage.AntiCheat.Detectors.InjectionDetector __instance)
        {
            var myLogSource = new ManualLogSource("OGAT_MODDING_API");
            BepInEx.Logging.Logger.Sources.Add(myLogSource);
            myLogSource.LogInfo("Disabled Injection cheat detector !!");
            BepInEx.Logging.Logger.Sources.Remove(myLogSource);

            __instance.StopDetectionInternal();

            return false;
        }
    }

    [HarmonyPatch(typeof(CodeStage.AntiCheat.Detectors.InjectionDetector), "StartDetectionInternal")]
    public class InjectionPatch1
    {

        [HarmonyPrefix, HarmonyDebug]
        public static bool DisableInjectionCheatStart(CodeStage.AntiCheat.Detectors.InjectionDetector __instance)
        {
            var myLogSource = new ManualLogSource("OGAT_MODDING_API");
            BepInEx.Logging.Logger.Sources.Add(myLogSource);
            myLogSource.LogInfo("Disabled Injection cheat detector !!");
            BepInEx.Logging.Logger.Sources.Remove(myLogSource);

            __instance.StopDetectionInternal();

            return false;
        }
    }

    ///////////////////Chat Patches///////////////////////

    [HarmonyPatch]
    public class ChatPatches        //will probably want to do this patch that dictates when to check for command thing different
    {
        public static int Count = 0;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Lobby), "AddChatLine")]    //AddChatLine is called in Lobby class and handles all messages being sent
        public static void alter_addChatLine(Lobby __instance, string GKPAOFHOPJJ, string LKPHAKGDPGL, bool LCLEPHOBIFO)
        {
            string prop1 = $"{GKPAOFHOPJJ}";
            if (Count != 0)
            {
                prop1 = __instance.FCABKFCOOAG.Last().text;
                API_Methods.CheckForCommand(prop1, GKPAOFHOPJJ);
            }

            Count ++;
        }
        
    }

    ///////////////////GAME CLASS Patches///////////////////////
    [HarmonyPatch]
    class GamePatches
    {

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Game), "LoadGameModes")]   //called by game to load all of the game modes
        public static bool alter_loadGameModes(Game __instance)
        {
            var myLogSource = new ManualLogSource("OGAT_MODDING_API");
            foreach (GameMode gameMode in __instance.gameModes)
            {
                if (!(gameMode == null))
                {
                    gameMode.game = __instance;
                    gameMode.gm_LoadConfig();

                    //
                    BepInEx.Logging.Logger.Sources.Add(myLogSource);
                    myLogSource.LogInfo($"Gamemode Loaded: {gameMode.name} - {gameMode.config.FDLIEBOMBAK}");
                    //
                }
            }
            BepInEx.Logging.Logger.Sources.Remove(myLogSource);
            return false;
        }

    }

}