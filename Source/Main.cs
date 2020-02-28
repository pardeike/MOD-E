using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace MOD_E
{
	public class MOD_E_Main : Mod
	{
		public static string MyOwnIdentifier;
		public static MOD_E_Settings Settings;

		public MOD_E_Main(ModContentPack content) : base(content)
		{
			MyOwnIdentifier = content.PackageId;
			Settings = GetSettings<MOD_E_Settings>();

			var harmony = new Harmony("net.pardeike.MOD-E");
			harmony.PatchAll();
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

	[HarmonyPatch(typeof(Game))]
	[HarmonyPatch("FinalizeInit")]
	static class Game_FinalizeInit_Patch
	{
		public static void Postfix()
		{
			ModCounter.Trigger();
		}
	}

	[HarmonyPatch(typeof(ScribeMetaHeaderUtility))]
	[HarmonyPatch("ModListsMatch")]
	static class ScribeMetaHeaderUtility_ModListsMatch_Patch
	{
		public static void Prefix(ref List<string> a, ref List<string> b)
		{
			var a2 = new List<string>(a);
			_ = a2.RemoveAll(modID => modID == MOD_E_Main.MyOwnIdentifier);
			_ = a2.RemoveAll(modID => MOD_E_Main.Settings.IsIgnored(modID, null));
			a = a2;

			var b2 = new List<string>(b);
			_ = b2.RemoveAll(modID => modID == MOD_E_Main.MyOwnIdentifier);
			_ = a2.RemoveAll(modID => MOD_E_Main.Settings.IsIgnored(modID, null));
			b = b2;
		}
	}

	[HarmonyPatch(typeof(ModsConfig))]
	[HarmonyPatch("Reset")]
	static class ModsConfig_Reset_Patch
	{
		public static void Postfix()
		{
			var modIDs = Traverse.Create(typeof(ModsConfig)).Field("data").Field("activeMods").GetValue<List<string>>();
			_ = modIDs.RemoveAll(modID => modID == MOD_E_Main.MyOwnIdentifier);
			modIDs.Insert(0, MOD_E_Main.MyOwnIdentifier);
			ModsConfig.Save();
		}
	}

	[HarmonyPatch(typeof(ScribeMetaHeaderUtility))]
	[HarmonyPatch("TryCreateDialogsForVersionMismatchWarnings")]
	static class ScribeMetaHeaderUtility_TryCreateDialogsForVersionMismatchWarnings_Patch
	{
		public static bool Prefix(Action confirmedAction, ref bool __result)
		{
			var versionMatch = Traverse.Create(typeof(ScribeMetaHeaderUtility)).Method("VersionsMatch").GetValue<bool>();
			if (BackCompatibility.IsSaveCompatibleWith(ScribeMetaHeaderUtility.loadedGameVersion) || versionMatch)
			{
				if (!ScribeMetaHeaderUtility.LoadedModsMatchesActiveMods(out var loadedModsSummary, out var currentModsSummary))
				{
					var missing = ModFixer.MissingModsInfo();

					if (missing.Count == 0)
					{
						var modInfoText = "ModsMismatchWarningText".Translate(loadedModsSummary, currentModsSummary);
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
								var modInfoText = "ModsMismatchWarningText".Translate(loadedModsSummary, currentModsSummary);
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