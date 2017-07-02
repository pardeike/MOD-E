using Harmony;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Steam;

namespace MOD_E
{
	public class MissingModsDialog : Window
	{
		private const float ModRowHeight = 35f;
		private const float ButtonHeight = 35f;
		private const float buttonCount = 3f;
		private const float defaultSpacing = 20f;

		private Action defaultAction;

		public List<ModIdAndName> mods;
		public String title;
		public String header;

		private Vector2 scrollPosition = Vector2.zero;
		private float creationRealTime = -1f;

		public override Vector2 InitialSize { get { return new Vector2(640f, 460f); } }

		public MissingModsDialog(List<ModIdAndName> mods, Action defaultAction)
		{
			this.defaultAction = defaultAction;

			this.mods = mods;
			title = "ModsMissingTitle".Translate();
			header = "ModsMissingText".Translate();

			forcePause = true;
			absorbInputAroundWindow = true;
			closeOnEscapeKey = true;
			creationRealTime = RealTime.LastRealTime;
			onlyOneOfTypeAllowed = false;
		}

		private void AddButton(Rect inRect, int index, String label, Action action, bool dangerous)
		{
			GUI.color = dangerous ? new Color(1f, 0.3f, 0.35f) : Color.white;
			var buttonWidth = (inRect.width - (buttonCount - 1) * defaultSpacing) / buttonCount;
			var rect = new Rect((index - 1) * (buttonWidth + defaultSpacing), inRect.height - ButtonHeight, buttonWidth, ButtonHeight);
			if (Widgets.ButtonText(rect, label.Translate(), true, false, true))
				action();
		}

		private float AddMods(Rect contentRect, bool render, out bool hasSubscribeAll, out bool allModsResolved)
		{
			hasSubscribeAll = false;
			allModsResolved = true;

			var verticalPos = contentRect.y;
			var rowSpacing = 5f;

			var buttonWidth = 0f;
			Text.Font = GameFont.Small;
			var labelSubscribe = "SubscribeSteamButtonLabel".Translate();
			var labelInstalled = "Installed".Translate();
			var labelDownloading = "Downloading".Translate();
			var labelOpenSteam = "OpenSteamButtonLabel".Translate();
			var labelSearchForum = "SearchForumButtonLabel".Translate();
			buttonWidth = Math.Max(buttonWidth, Text.CalcSize(labelSubscribe).x);
			buttonWidth = Math.Max(buttonWidth, Text.CalcSize(labelInstalled).x);
			buttonWidth = Math.Max(buttonWidth, Text.CalcSize(labelDownloading).x);
			buttonWidth = Math.Max(buttonWidth, Text.CalcSize(labelOpenSteam).x);
			buttonWidth = Math.Max(buttonWidth, Text.CalcSize(labelSearchForum).x);
			buttonWidth += 4 * rowSpacing;

			var steamIsAvailable = ModFixer.SteamIsAvailable();

			var modsInstalled = new List<ulong>();
			var modsNotInstalled = new List<ulong>();
			if (steamIsAvailable)
			{
				WorkshopItems.AllSubscribedItems.Do(item =>
				{
					if (item is WorkshopItem_NotInstalled)
						modsNotInstalled.Add(item.PublishedFileId.m_PublishedFileId);
					else
						modsInstalled.Add(item.PublishedFileId.m_PublishedFileId);
				});
			}

			var descColumnWidth = contentRect.width - buttonWidth - defaultSpacing - buttonWidth - defaultSpacing;
			for (int i = 0; i < mods.Count; i++)
			{
				var mod = mods[i];

				ulong steamID;
				if (ulong.TryParse(mod.id, out steamID) == false)
					steamID = 0;

				var description = mod.name;
				if (mod.name != mod.id)
				{
					if (steamID > 0)
						description += " (Steam ID " + steamID + ")";
					else
						description += " (ID " + mod.id + ")";
				}
				Text.Font = GameFont.Small;
				var descriptionHeight = Text.CalcHeight(description, descColumnWidth);
				var rowHeight = Math.Max(descriptionHeight, ButtonHeight);

				if (render)
				{
					Rect rect = new Rect(0f, verticalPos, descColumnWidth, rowHeight);
					var vpos = verticalPos + (rowHeight - ButtonHeight) / 2;

					var anchor = Text.Anchor;
					Text.Anchor = TextAnchor.MiddleLeft;
					Widgets.Label(rect, description);
					Text.Anchor = anchor;

					if (steamID > 0)
					{
						if (steamIsAvailable)
						{
							if (modsInstalled.Contains(steamID))
							{
								rect = new Rect(descColumnWidth + defaultSpacing, verticalPos, buttonWidth, rowHeight);
								Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.1f, 0.1f));
								anchor = Text.Anchor;
								Text.Anchor = TextAnchor.MiddleCenter;
								Widgets.Label(rect, labelInstalled);
								Text.Anchor = anchor;
							}
							else if (steamIsAvailable && modsNotInstalled.Contains(steamID))
							{
								rect = new Rect(descColumnWidth + defaultSpacing, verticalPos, buttonWidth, rowHeight);
								Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.2f, 0.1f));
								anchor = Text.Anchor;
								Text.Anchor = TextAnchor.MiddleCenter;
								Widgets.Label(rect, labelDownloading);
								Text.Anchor = anchor;

								allModsResolved = false;
							}
							else
							{
								rect = new Rect(descColumnWidth + defaultSpacing, vpos, buttonWidth, ButtonHeight);
								var oldColor = GUI.color;
								GUI.color = new Color(1f, 0.3f, 0.35f);
								if (Widgets.ButtonText(rect, labelSubscribe, true, false, steamIsAvailable))
									ModFixer.SubscribeMod(steamID);
								GUI.color = oldColor;

								hasSubscribeAll = true;
								allModsResolved = false;
							}

							rect = new Rect(descColumnWidth + defaultSpacing + buttonWidth + defaultSpacing, vpos, buttonWidth, ButtonHeight);
							if (Widgets.ButtonText(rect, labelOpenSteam, true, false, steamIsAvailable))
								SteamUtility.OpenWorkshopPage(new PublishedFileId_t(steamID));
						}
					}
					else
					{
						rect = new Rect(descColumnWidth + defaultSpacing + buttonWidth + defaultSpacing, vpos, buttonWidth, ButtonHeight);
						if (Widgets.ButtonText(rect, labelSearchForum, true, false, true))
						{
							var term = WWW.EscapeURL(mod.name);
							var url = "https://ludeon.com/forums/index.php?action=search2&advanced=1&searchtype=1&sort=relevance|desc&brd[15]=15&brd[16]=16&search=" + term;
							Application.OpenURL(url);
							//SteamUtility.OpenUrl(url);
						}
						allModsResolved = false;
					}
				}

				verticalPos += rowHeight + rowSpacing;
			}
			return verticalPos - contentRect.y;
		}

		public override void DoWindowContents(Rect inRect)
		{
			var height = 0f;
			var verticalPos = inRect.y;

			Text.Font = GameFont.Medium;
			height = Text.CalcHeight(title, inRect.width);
			Widgets.Label(new Rect(0f, verticalPos, inRect.width, height), title);
			verticalPos += height + defaultSpacing;

			Text.Font = GameFont.Small;
			height = Text.CalcHeight(header, inRect.width);
			Widgets.Label(new Rect(0f, verticalPos, inRect.width, height), header);
			verticalPos += height + defaultSpacing;

			bool hasSubscribeAll, allModsResolved;

			Text.Font = GameFont.Small;
			Rect outRect = new Rect(inRect.x, verticalPos, inRect.width, inRect.height - ButtonHeight - defaultSpacing - 5f - verticalPos);
			float width = outRect.width - 16f;
			Rect viewRect = new Rect(0f, 0f, width, 0f);
			viewRect.height = AddMods(viewRect, false, out hasSubscribeAll, out allModsResolved);
			Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
			AddMods(viewRect, true, out hasSubscribeAll, out allModsResolved);
			Widgets.EndScrollView();

			if (hasSubscribeAll)
				AddButton(inRect, 1, "SubscribeToAllButtonLabel", delegate { ModFixer.SubscribeAllMods(); }, true);

			if (allModsResolved)
				AddButton(inRect, 3, "OK", delegate { Close(true); defaultAction(); }, false);
			else
				AddButton(inRect, 3, "GoBack", delegate { Close(true); }, false);
		}
	}
}