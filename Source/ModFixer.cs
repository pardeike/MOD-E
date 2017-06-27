using Harmony;
using System;
using System.Collections.Generic;
using Verse;

namespace MOD_E
{
	public static class ModFixer
	{
		static Traverse GetModWithIdentifier = Traverse.Create(typeof(ModLister)).Method("GetModWithIdentifier", new Type[] { typeof(String) });
		public static ModMetaData GetModMetaData(String modID) { return GetModWithIdentifier.GetValue<ModMetaData>(modID); }

		public static bool CanFixMods(out String error)
		{
			if (ScribeMetaHeaderUtility.loadedModIdsList == null)
			{
				error = "Cannot get mod list";
				return false;
			}
			var modIDs = new List<String>(ScribeMetaHeaderUtility.loadedModIdsList);
			foreach (var modID in modIDs)
			{
				Log.Warning("#" + modID + "#");

				var metaData = GetModMetaData(modID);
				if (metaData == null)
				{
					error = "Cannot get meta data for mod " + modID;
					return false;
				}
				// Log.Error("MOD " + metaData.Name + "(" + metaData.Identifier + ") active = " 
				//           + (metaData.Active ? "yes" : "no") + " core = " + (metaData.IsCoreMod ? "yes" : "no"));
			}

			error = null;
			return true;
		}

		public static void FixMods()
		{
			var modIDs = new List<String>(ScribeMetaHeaderUtility.loadedModIdsList);
			modIDs.RemoveAll(modID => modID == MOD_E_Main.MyOwnIdentifier);
			modIDs.Insert(0, MOD_E_Main.MyOwnIdentifier);
			Traverse.Create(typeof(ModsConfig)).Field("data").Field("activeMods").SetValue(modIDs);
			ModsConfig.Save();
			GenCommandLine.Restart();
		}
	}
}