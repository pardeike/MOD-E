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
				error = "CannotGetModList".Translate();
				return false;
			}

			var missingMods = new List<String>();
			var modIDs = new List<String>(ScribeMetaHeaderUtility.loadedModIdsList);
			foreach (var modID in modIDs)
			{
				var metaData = GetModMetaData(modID);
				if (metaData == null)
					missingMods.Add(modID);
			}

			if (missingMods.Count > 0)
			{
				error = "TheseModsAreMissing".Translate(String.Join(", ", missingMods.ToArray()));
				return false;
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