using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using System.IO;
using System.Xml.Linq;
using HarmonyLib;

namespace MOD_E
{
    [HarmonyLib.HarmonyPatch(typeof(ScribeMetaHeaderUtility), "WriteMetaHeader")]
    [StaticConstructorOnStartup]
    public class ModFinderIO
    {

        static List<string> aboutList = new List<string>();
        static List<long> longSteamIDs = new List<long>();
        static ModFinderIO()
        {

            Dictionary<string, long> pairs = new Dictionary<string, long>();


            string path = Directory.GetCurrentDirectory();
            string newPath = Path.GetFullPath(Path.Combine(path, @"..\..\"));
            newPath += "workshop\\content\\294100";

            findAllAboutFiles(newPath);

            foreach (string entry in aboutList)
            {
                XDocument xml = XDocument.Load(entry);

                //Log.Message("path: " + entry);
                //Log.Message("root " + xml.Root);

                string steamID = entry.Substring(entry.IndexOf("294100") + 7);
                steamID = steamID.Substring(0, 10);


                foreach (XElement elm in xml.Root.Elements("packageId"))
                {
                    if (elm.Parent != new XElement("li"))
                    {
                        //Log.Message("Found a ID");
                        //Log.Message("SteamID: "+steamID);
                        //Log.Message("Element: "+elm.Value);
                        try
                        {
                            pairs[elm.Value.ToLower()] = long.Parse(GetNumbers(steamID));
                        }
                        catch
                        {
                            Log.Message("trouble parsing ID for " + steamID+ " | "+ GetNumbers(steamID));
                        }

                    }
                }

                //Log.Message("====================");

            }

            List<string> list = LoadedModManager.RunningMods.Select((ModContentPack mod) => mod.PackageId).ToList();
            longSteamIDs = Enumerable.Repeat(0L, list.Count).ToList();

            for (int i = 0; i < list.Count; i++)
            {
                if (pairs.ContainsKey(list[i]))
                {
                    Log.Message("Found Steam ID for: " + list[i] + " == " + pairs.TryGetValue(list[i]));
                    longSteamIDs[i] = pairs.TryGetValue(list[i]);
                }
                else
                {
                    Log.Message("Steam ID not found for: " + list[i]);
                }
            }


            Harmony harmony = new Harmony("net.pardeike.MOD-E.helper");

            harmony.PatchAll();
        }

        static bool Prefix()
        {
            if (!Scribe.EnterNode("meta"))
            {
                return true;
            }
            try
            {
                string value = VersionControl.CurrentVersionStringWithRev;
                Scribe_Values.Look(ref value, "gameVersion");
                List<string> list = LoadedModManager.RunningMods.Select((ModContentPack mod) => mod.PackageId).ToList();
                Scribe_Collections.Look(ref list, "modIds", LookMode.Undefined);

                Log.Message("Use new IDs... ()");
                Scribe_Collections.Look(ref longSteamIDs, "modSteamIds", LookMode.Undefined);

                List<string> list3 = LoadedModManager.RunningMods.Select((ModContentPack mod) => mod.Name).ToList();
                Scribe_Collections.Look(ref list3, "modNames", LookMode.Undefined);
            }
            finally
            {
                Scribe.ExitNode();
            }
            return false;
        }

        private static string GetNumbers(string input)
        {
            return new string(input.Where(c => char.IsDigit(c)).ToArray());
        }

        static void findAllAboutFiles(string sDir)
        {
            try
            {

                foreach (string f in Directory.GetFiles(sDir))
                {
                    if (f.Contains("About.xml") || f.Contains("about.xml"))
                    {
                        aboutList.Add(f);
                    }

                }

                foreach (string d in Directory.GetDirectories(sDir))
                {
                    findAllAboutFiles(d);
                }
            }
            catch (System.Exception excpt)
            {
                Log.Message(excpt.Message);
            }
        }
    }
}
