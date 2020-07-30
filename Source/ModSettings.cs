using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MOD_E
{
	[StaticConstructorOnStartup]
	public static class SettingAssets
	{
		public static readonly Texture2D DeleteX = ContentFinder<Texture2D>.Get("UI/Buttons/Delete", true);
	}

	public class MOD_E_Settings : ModSettings
	{
		List<ModIdAndName> IgnoredMods = new List<ModIdAndName>();
		public bool ShowIgnoreConfirmation = true;

		public bool IsIgnored(string id, string name)
		{
			return IgnoredMods.Any(mod => mod.id == id || (name != null && mod.name == name));
		}

		public bool IsIgnored(ModIdAndName mod)
		{
			return IgnoredMods.Any(mod2 =>
			{
				if (ulong.TryParse(mod.id, out var steamID) == false)
					return mod2.name == mod.name;
				return mod2.id == mod.id;
			});
		}

		public void AddIgnoredMod(ModIdAndName mod)
		{
			IgnoredMods.Add(mod);
			Write();
		}

		public void RemoveIgnoredMod(ModIdAndName mod)
		{
			_ = IgnoredMods.Remove(mod);
			Write();
		}

		public void RemoveAllIgnoredMods()
		{
			IgnoredMods.Clear();
			Write();
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Collections.Look(ref IgnoredMods, "IgnoredMods");
			Scribe_Values.Look(ref ShowIgnoreConfirmation, "ShowIgnoreConfirmation", true);
		}

		private static string GetDescription(ModIdAndName mod)
		{
			if (ulong.TryParse(mod.id, out var steamID) == false)
				steamID = 0;

			var description = mod.name;
			if (mod.name != mod.id)
			{
				if (steamID > 0)
					description += " (Steam ID " + steamID + ")";
				else
					description += " (ID " + mod.id + ")";
			}

			return description;
		}

		static Vector2 scrollPosition = Vector2.zero;
		public void DoWindowContents(Rect inRect)
		{
			var list = new Listing_Standard();
			var rowSpacing = 5f;
			var ButtonHeight = 24f;
			var verticalPos = 0f;

			var totalWidth = Math.Max(320f, inRect.width);

			var titleRect = inRect;
			titleRect.height = 48f;

			Text.Font = GameFont.Medium;
			var anchor = Text.Anchor;
			Text.Anchor = TextAnchor.UpperLeft;
			Widgets.Label(titleRect, "PermanentlyIgnoredModsList".Translate());
			Text.Anchor = anchor;

			var scrollHeigth = 0f;
			for (var i = 0; i < IgnoredMods.Count; i++)
			{
				var mod = IgnoredMods[i];
				var description = GetDescription(mod);

				var buttonColumnWidth = ButtonHeight + 16f;
				var descriptionHeight = Text.CalcHeight(description, totalWidth - 16f - buttonColumnWidth);
				var rowHeight = Math.Max(descriptionHeight, ButtonHeight);

				scrollHeigth += rowHeight;
			}

			inRect.yMin += titleRect.height;
			var outRect = new Rect(inRect.x, inRect.y, totalWidth, inRect.height);
			var scrollRect = new Rect(0f, 0f, totalWidth - 16f, scrollHeigth);
			Widgets.BeginScrollView(outRect, ref scrollPosition, scrollRect, true);

			list.Begin(scrollRect);
			for (var i = 0; i < IgnoredMods.Count; i++)
			{
				var mod = IgnoredMods[i];
				var description = GetDescription(mod);

				var rowWidth = scrollRect.width;
				var buttonColumnWidth = ButtonHeight + 16f;

				Text.Font = GameFont.Small;
				var descriptionHeight = Text.CalcHeight(description, rowWidth - buttonColumnWidth);
				var rowHeight = Math.Max(descriptionHeight, ButtonHeight);

				var rect = new Rect(0f, verticalPos, rowWidth, rowHeight);

				var y = rect.y + (rect.height - ButtonHeight) / 2f;
				var butRect = new Rect(rect.xMin, y, ButtonHeight, ButtonHeight);
				if (Widgets.ButtonImage(butRect, SettingAssets.DeleteX))
					RemoveIgnoredMod(mod);

				rect.xMin += buttonColumnWidth;

				anchor = Text.Anchor;
				Text.Anchor = TextAnchor.MiddleLeft;
				Widgets.Label(rect, description);
				Text.Anchor = anchor;

				verticalPos += rowHeight + rowSpacing;
			}
			list.End();

			Widgets.EndScrollView();
		}
	}
}