using Verse;
using System;
using System.Reflection;
using Harmony;
using System.Collections.Generic;
using UnityEngine;

namespace MOD_E
{
	public class MOD_E_Main : Mod
	{
		public static string MyOwnIdentifier;
		public static MOD_E_Settings Settings;

		public MOD_E_Main(ModContentPack content) : base(content)
		{
			MyOwnIdentifier = content.Identifier;
			Settings = GetSettings<MOD_E_Settings>();

			var harmony = HarmonyInstance.Create("net.pardeike.MOD-E");
			harmony.PatchAll(Assembly.GetExecutingAssembly());
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
			Settings.DoWindowContents(inRect);
		}

		public override string SettingsCategory()
		{
			return "MOD-E";
		}
	}

	[HarmonyPatch(typeof(ScribeMetaHeaderUtility))]
	[HarmonyPatch("ModListsMatch")]
	static class ScribeMetaHeaderUtility_ModListsMatch_Patch
	{
		static void Prefix(ref List<string> a, ref List<string> b)
		{
			var a2 = new List<string>(a);
			a2.RemoveAll(modID => modID == MOD_E_Main.MyOwnIdentifier);
			a2.RemoveAll(modID => MOD_E_Main.Settings.IsIgnored(modID, null));
			a = a2;

			var b2 = new List<string>(b);
			b2.RemoveAll(modID => modID == MOD_E_Main.MyOwnIdentifier);
			a2.RemoveAll(modID => MOD_E_Main.Settings.IsIgnored(modID, null));
			b = b2;
		}
	}

	[HarmonyPatch(typeof(ModsConfig))]
	[HarmonyPatch("Reset")]
	static class ModsConfig_Reset_Patch
	{
		static void Postfix()
		{
			var modIDs = Traverse.Create(typeof(ModsConfig)).Field("data").Field("activeMods").GetValue<List<string>>();
			modIDs.RemoveAll(modID => modID == MOD_E_Main.MyOwnIdentifier);
			modIDs.Insert(0, MOD_E_Main.MyOwnIdentifier);
			ModsConfig.Save();
		}
	}

	[HarmonyPatch(typeof(ScribeMetaHeaderUtility))]
	[HarmonyPatch("TryCreateDialogsForVersionMismatchWarnings")]
	static class ScribeMetaHeaderUtility_TryCreateDialogsForVersionMismatchWarnings_Patch
	{
		static bool Prefix(Action confirmedAction, ref bool __result)
		{
			var versionMatch = Traverse.Create(typeof(ScribeMetaHeaderUtility)).Method("VersionsMatch").GetValue<bool>();
			if (BackCompatibility.IsSaveCompatibleWith(ScribeMetaHeaderUtility.loadedGameVersion) || versionMatch)
			{
				string loadedModsSummary;
				string currentModsSummary;
				if (!ScribeMetaHeaderUtility.LoadedModsMatchesActiveMods(out loadedModsSummary, out currentModsSummary))
				{
					var missing = ModFixer.MissingModsInfo();

					if (missing.Count == 0)
					{
						var modInfoText = "ModsMismatchWarningText".Translate(new object[] { loadedModsSummary, currentModsSummary });
						Find.WindowStack.Add(new MismatchDialog(modInfoText, confirmedAction));
					}
					else
					{
						Find.WindowStack.Add(new MissingModsDialog(missing, delegate
						{
							missing = ModFixer.MissingModsInfo();
							if (missing.Count == 0)
								confirmedAction();
							else
							{
								var modInfoText = "ModsMismatchWarningText".Translate(new object[] { loadedModsSummary, currentModsSummary });
								Find.WindowStack.Add(new MismatchDialog(modInfoText, confirmedAction));
							}
						}));
					}

					__result = true;
					return false;
				}
			}

			return true;
		}
	}
}