using Verse;
using System;
using System.Reflection;
using Harmony;
using System.Collections.Generic;
using System.Threading;

namespace MOD_E
{
	public class MOD_E_Main : Mod
	{
		public static String MyOwnIdentifier;

		public MOD_E_Main(ModContentPack content) : base(content)
		{
			MyOwnIdentifier = content.Identifier;

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
							var modInfoText = "ModsMismatchWarningText".Translate(new object[] { loadedModsSummary, currentModsSummary });
							Find.WindowStack.Add(new MismatchDialog(modInfoText, confirmedAction));
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