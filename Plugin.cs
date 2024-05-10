﻿using BepInEx;
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

namespace OGAT_modding_API
{

    [BepInPlugin("OGAT_modding_API", MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class OGAT_Plugin : BaseUnityPlugin
    {
        private void Awake()    //patches all of the methods
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
            var harmony = new Harmony("com.technomainiac.Ogat_modding_Api");
            harmony.PatchAll();
        }

        public void Start()
        {
            API_Methods.commands["list"] = API_Methods.ListPlayers;
        }

        public void Update()    //updates plugin, will be used eventually to open mod menu
        {
            if(Input.GetKeyDown(KeyCode.Comma))
            {
                StartCoroutine(API_Methods.PlayerTest("technomainiac39"));
            }
        }
    }

    ///////////////////Methods for API//////////////

    public class API_Methods    
    {
        //public static List<string> commands = new List<string>();
        public static IDictionary<int, string> UsersAndIds = new Dictionary<int, string>();
        public static IDictionary<string, Func<List<string>,bool>> commands = new Dictionary<string, Func<List<string>, bool>>();

        public SG.OGAT.Chat chat;

        public API_Methods()    //adds all commands and thier methods to command dict
        {
            commands.Add("list", ListPlayers);
        }

        public void LogToOgatChat()
        {
        }
        static public void CheckForCommand(string command, string username)  //takes text from chat and searches for command
        {
            var myLogSource = new ManualLogSource("OGAT_MODDING_API");
            BepInEx.Logging.Logger.Sources.Add(myLogSource);
            myLogSource.LogInfo(command);

            foreach (KeyValuePair<string, Func<List<string>, bool>> pair in commands)
            {
                myLogSource.LogInfo($"Command: {pair.Key}");
            }


            string[] message = command.Split(' ');
            myLogSource.LogInfo(message[1]);
            if (message[1].StartsWith("#"))
            {
                string comm = message[1].Trim('#');
                foreach(KeyValuePair<string, Func<List<string>, bool>> kvp in commands)
                {
                    if (comm == kvp.Key)
                    {
                        //

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

        static public IEnumerator PlayerTest(string username)   //the coroutine of player tests, called in update
        {
            if (UsersAndIds == null)
            {
                List<string> param = new List<string>();
                param.Add("");
                param.Add(username);
                ListPlayers(param);
            }
            var player = FindPlayer(username);
            if (player == null)
            {
                yield break;
            }
            //
            var myLogSource = new ManualLogSource("OGAT_MODDING_API");
            BepInEx.Logging.Logger.Sources.Add(myLogSource);
            myLogSource.LogInfo($"PlayerTest mags left: {player.magazinesLeftForCurrentWeapon}");
            BepInEx.Logging.Logger.Sources.Remove(myLogSource);

            //
            yield return null;
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
    public class ChatPatches
    {
        public static int Count = 0;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Lobby), "AddChatLine")]    //AddChatLine is called in Lobby class and handles all messages being sent
        public static void alter_addChatLine(Lobby __instance, string GKPAOFHOPJJ, string LKPHAKGDPGL, bool LCLEPHOBIFO)
        {
            var myLogSource = new ManualLogSource("OGAT_MODDING_API");
            BepInEx.Logging.Logger.Sources.Add(myLogSource);
            myLogSource.LogInfo("running");

            string prop1 = $"{GKPAOFHOPJJ}";
            if (Count != 0)
            {
                prop1 = __instance.FCABKFCOOAG.Last().text;
                API_Methods.CheckForCommand(prop1, GKPAOFHOPJJ);
            }
            if (GKPAOFHOPJJ == "technomainiac39")
            {
               // Text text;
               // text = SGExtensions.FindChildRecursiveHelper<Text>(__instance.AGLMLJACABD, "CHAT_LINE_PREFAB");
               // Text text2 = UnityEngine.Object.Instantiate<Text>(text, text.transform.parent);
               // text2.gameObject.SetActive(true);
               // text2.name = "_line_";

                //text2.text = "<color=orange>[God]</color>:<color=purple> Technomainiac Has Spoken !!</color>";

            }

            //logging
            myLogSource.LogInfo($"Lobby Properties: {prop1}");
            BepInEx.Logging.Logger.Sources.Remove(myLogSource);
            //
            Count ++;
        }
        /*
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SG.OGAT.Chat), "addLine")]     //add chatline is what the little chatbox calls when a msg has been entered will call AddChatLine
        public static bool alter_addLine(IDJNNEJNMMO GGOBEEOMBGG, string BEGNOMMHMHA, string BNOEGGNKMFM, bool LCLEPHOBIFO, SG.OGAT.Chat __instance)
        {

            BNOEGGNKMFM = FCDLDGDCNML.CPOEAFKLBKE(BNOEGGNKMFM).Trim();
            string[] array = BNOEGGNKMFM.Split(new char[]
            {
                '\n'
            });
            foreach (string text in array)
            {
                if (text.Trim() != string.Empty)
                {
                    //
                    var myLogSource = new ManualLogSource("OGAT_MODDING_API");
                    BepInEx.Logging.Logger.Sources.Add(myLogSource);
                    myLogSource.LogInfo($"Chat Params Passed: {GGOBEEOMBGG}, {BEGNOMMHMHA}, {BNOEGGNKMFM}, {LCLEPHOBIFO}");
                    BepInEx.Logging.Logger.Sources.Remove(myLogSource);
                    //
                    __instance.AIJICJEBIMD(GGOBEEOMBGG, BEGNOMMHMHA, text, false, LCLEPHOBIFO);
                    Singleton<Lobby>.I.AddChatLine(BEGNOMMHMHA, text, LCLEPHOBIFO);
                }
            }

            return false;
        }
        */
        
    }

    ///////////////////GAME CLASS Patches///////////////////////
    [HarmonyPatch]
    class GamePatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(NetPlayer), "FindNetPlayerByUserID")]      //finds player based on the username thing, no longer used by Modding_API
        public static void alter_findNetPlayerByUserID(string PEOLJGCMGPP, NetPlayer __result)
        {
            var myLogSource = new ManualLogSource("OGAT_MODDING_API");
            BepInEx.Logging.Logger.Sources.Add(myLogSource);
            //myLogSource.LogInfo($"FindNetPlayer results: {PEOLJGCMGPP}, {__result}");
            BepInEx.Logging.Logger.Sources.Remove(myLogSource);
        }


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

        [HarmonyPatch]
        [HarmonyPatch(typeof(Game), "Server_start")]        //havent been able to figure out how it works yet
        public static void alter_serverStart(Game __instance, string DMMPKHIECJK, string EDCCGAHKIJB, bool AAMKAMCPAFO)
        {
            ///
            var myLogSource = new ManualLogSource("OGAT_MODDING_API");
            BepInEx.Logging.Logger.Sources.Add(myLogSource);
            myLogSource.LogInfo($"Creating server with params: {DMMPKHIECJK}, {EDCCGAHKIJB}, {AAMKAMCPAFO}");
            BepInEx.Logging.Logger.Sources.Remove(myLogSource);
            ///

            //return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SGNet), "send_to_team")]   //sending messages to team, (not chnaging teams :( too bad I guess)
        public static void alter_sendToTeam(SGNet __instance, IDJNNEJNMMO GGOBEEOMBGG, KLEPJCJNCIJ BAPAMAPHEPA, params KLEPJCJNCIJ[] JDGFANBJEGB)
        {
            ///
            var myLogSource = new ManualLogSource("OGAT_MODDING_API");
            BepInEx.Logging.Logger.Sources.Add(myLogSource);
            myLogSource.LogInfo($"Send to team params Passed: {GGOBEEOMBGG}, {BAPAMAPHEPA}, {JDGFANBJEGB}");
            BepInEx.Logging.Logger.Sources.Remove(myLogSource);
            ///
           // return true;
        }

    }

}