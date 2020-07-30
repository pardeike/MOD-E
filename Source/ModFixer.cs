using HarmonyLib;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Threading;
using Verse;
using Verse.Steam;

namespace MOD_E
{
	public class ModIdAndName : IExposable
	{
		public string id;
		public string name;

		public ModIdAndName()
		{
			id = "";
			name = "";
		}

		public ModIdAndName(string id, string name)
		{
			this.id = id;
			this.name = name;
		}

		public void ExposeData()
		{
			Scribe_Values.Look(ref id, "id");
			Scribe_Values.Look(ref name, "name");
		}
	}

	public static class ModFixer
	{
		public static ModMetaData GetModMetaData(string modID) { return ModLister.GetModWithIdentifier(modID, false); }

		static List<ModIdAndName> missingMods;

		public static bool SteamIsAvailable()
		{
			var steamManagerType = AccessTools.TypeByName("Verse.Steam.SteamManager");
			if (steamManagerType == null) return false;
			var initializedProp = AccessTools.Property(steamManagerType, "Initialized");
			if (initializedProp == null) return false;
			return SteamManager.Initialized;
		}

		public static List<ModIdAndName> MissingModsInfo()
		{
			//if (SteamIsAvailable())
			//{
			//	var numSub = SteamUGC.GetNumSubscribedItems();
			//	var array = new PublishedFileId_t[numSub];
			//	var count = (int)SteamUGC.GetSubscribedItems(array, numSub);
			//	var subscribedMods = array.ToList()
			//		.GetRange(0, count)
			//		.Select(pfid => pfid.m_PublishedFileId)
			//		.ToList();

			//	var changed = false;
			//	foreach (var modID in ScribeMetaHeaderUtility.loadedModIdsList)
			//	{
			//		var metaData = GetModMetaData(modID);
			//		if (metaData == null)
			//		{
			//			ulong steamID;
			//			if (ulong.TryParse(modID, out steamID) && subscribedMods.Contains(steamID) == false)
			//			{
			//				Log.Warning("Auto subscribing to steam workshop mod " + steamID);
			//				var pfid = new PublishedFileId_t(steamID);
			//				SteamUGC.SubscribeItem(pfid);
			//				changed = true;
			//			}
			//		}
			//	}
			//	if (changed)
			//	{
			//		Log.Warning("Rebuilding workshop items");
			//		Traverse.Create(typeof(WorkshopItems)).Method("RebuildItemsList").GetValue();
			//		Log.Warning("Done");
			//	}
			//}

			missingMods = new List<ModIdAndName>();
			var modIDs = new List<string>(ScribeMetaHeaderUtility.loadedModIdsList);
			var modNames = new List<string>(ScribeMetaHeaderUtility.loadedModNamesList);
			for (var i = 0; i < Math.Min(modIDs.Count, modNames.Count); i++)
			{
				if (MOD_E_Main.Settings.IsIgnored(modIDs[i], modNames[i]))
					continue;

				var mod = new ModIdAndName(modIDs[i], modNames[i]);

				var metaData = GetModMetaData(mod.id);
				if (metaData == null)
					missingMods.Add(mod);
			}
			return missingMods;
		}

		public static void FixMods()
		{
			var modIDs = new List<string>(ScribeMetaHeaderUtility.loadedModIdsList);
			_ = modIDs.RemoveAll(modID => modID == MOD_E_Main.MyOwnIdentifier);
			modIDs.Insert(0, MOD_E_Main.MyOwnIdentifier);
			_ = Traverse.Create(typeof(ModsConfig)).Field("data").Field("activeMods").SetValue(modIDs);
			ModsConfig.Save();
			GenCommandLine.Restart();
		}

		public static void SubscribeMod(ulong steamID)
		{
			new Thread(() =>
			{
				_ = SteamUGC.SubscribeItem(new PublishedFileId_t(steamID));
				MissingModsDialog.rebuildList = true;
			}).Start();
		}

		public static void SubscribeAllMods()
		{
			var localMissingMods = missingMods.ToArray();
			var currentThread = Thread.CurrentThread;
			new Thread(() =>
			{
				var changed = false;
				foreach (var mod in localMissingMods)
				{
					if (ulong.TryParse(mod.id, out var steamID))
					{
						_ = SteamUGC.SubscribeItem(new PublishedFileId_t(steamID));
						changed = true;
					}
				}
				if (changed)
					MissingModsDialog.rebuildList = true;
			}).Start();
		}
	}
}