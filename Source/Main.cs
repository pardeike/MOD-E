using Verse;
using System;
using System.Reflection;
using Harmony;
using System.Collections.Generic;

namespace MOD_E
{
	public class MOD_E_Main : Mod
	{
		public static String MyOwnIdentifier;

		public MOD_E_Main(ModContentPack content) : base(content)
		{
			MyOwnIdentifier = content.Identifier;

			HarmonyInstance.DEBUG = true;
			var harmony = HarmonyInstance.Create("net.pardeike.MOD-E");
			harmony.PatchAll(Assembly.GetExecutingAssembly());
		}
	}

	[HarmonyPatch(typeof(ScribeMetaHeaderUtility))]
	[HarmonyPatch("ModListsMatch")]
	static class _Patch
	{
		static void Prefix(ref List<string> a, ref List<string> b)
		{
			var a2 = new List<string>(a);
			a2.RemoveAll(modID => modID == MOD_E_Main.MyOwnIdentifier);
			a = a2;

			var b2 = new List<string>(b);
			b2.RemoveAll(modID => modID == MOD_E_Main.MyOwnIdentifier);
			b = b2;
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
				String loadedModsSummary;
				String currentModsSummary;
				if (!ScribeMetaHeaderUtility.LoadedModsMatchesActiveMods(out loadedModsSummary, out currentModsSummary))
				{
					String error;
					if (ModFixer.CanFixMods(out error))
					{
						var modInfoText = "ModsMismatchWarningText".Translate(new object[] { loadedModsSummary, currentModsSummary });
						ShowDialog(modInfoText, confirmedAction);
						__result = true;
						return false;
					}
					else
					{
						Log.Error(error);
						// TODO handle or display error
					}
				}
			}
			return true;
		}

		static void ShowDialog(String text, Action confirmedAction)
		{
			var dialog = new MismatchDialog(text, confirmedAction);
			Find.WindowStack.Add(dialog);
		}
	}
}