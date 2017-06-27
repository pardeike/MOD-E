using Harmony;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MOD_E
{
	public class MismatchDialog : Window
	{
		private const float TitleHeight = 42f;
		private const float ButtonHeight = 35f;

		private const float buttonCount = 3f;
		private const float buttonSpacing = 20f;

		public String text;
		public Action defaultAction;

		private Vector2 scrollPosition = Vector2.zero;
		private float creationRealTime = -1f;

		public override Vector2 InitialSize { get { return new Vector2(640f, 460f); } }
		private float TimeUntilInteractive { get { return creationRealTime - Time.realtimeSinceStartup; } }

		public MismatchDialog(String text, Action defaultAction)
		{
			this.text = text;
			this.defaultAction = defaultAction;

			forcePause = true;
			absorbInputAroundWindow = true;
			closeOnEscapeKey = false;
			creationRealTime = RealTime.LastRealTime;
			onlyOneOfTypeAllowed = false;
		}

		private void AddButton(Rect inRect, int index, String label, Action action, bool dangerous)
		{
			GUI.color = dangerous ? new Color(1f, 0.3f, 0.35f) : Color.white;
			var buttonWidth = (inRect.width - (buttonCount - 1) * buttonSpacing) / buttonCount;
			var rect = new Rect((index - 1) * (buttonWidth + buttonSpacing), inRect.height - ButtonHeight, buttonWidth, ButtonHeight);
			if (Widgets.ButtonText(rect, label.Translate(), true, false, true))
			{
				action();
				Close(true);
			}
		}

		public override void DoWindowContents(Rect inRect)
		{
			float verticalPos = inRect.y;

			Text.Font = GameFont.Medium;
			Widgets.Label(new Rect(0f, verticalPos, inRect.width, TitleHeight), "ModsMismatchWarningTitle".Translate());
			verticalPos += TitleHeight;

			Text.Font = GameFont.Small;
			Rect outRect = new Rect(inRect.x, verticalPos, inRect.width, inRect.height - ButtonHeight - 5f - verticalPos);
			float width = outRect.width - 16f;
			Rect viewRect = new Rect(0f, 0f, width, Text.CalcHeight(text, width));
			Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect, true);
			Widgets.Label(new Rect(0f, 0f, viewRect.width, viewRect.height), this.text);
			Widgets.EndScrollView();

			AddButton(inRect, 1, "GoBack", delegate { Close(true); }, false);
			AddButton(inRect, 2, "FixThoseMods", delegate { ModFixer.FixMods(); }, true);
			AddButton(inRect, 3, "LoadAnyway", defaultAction, false);
		}
	}
}