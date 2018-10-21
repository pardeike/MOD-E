using System;
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

		public string text;
		public Action defaultAction;

		private Vector2 scrollPosition = Vector2.zero;
		private readonly float creationRealTime = -1f;

		public override Vector2 InitialSize => new Vector2(640f, 460f);

		public MismatchDialog(string text, Action defaultAction)
		{
			this.text = text;
			this.defaultAction = defaultAction;

			forcePause = true;
			absorbInputAroundWindow = true;
			creationRealTime = RealTime.LastRealTime;
			onlyOneOfTypeAllowed = false;
		}

		private void AddButton(Rect inRect, int index, string label, Action action, bool dangerous)
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
			var verticalPos = inRect.y;

			Text.Font = GameFont.Medium;
			Widgets.Label(new Rect(0f, verticalPos, inRect.width, TitleHeight), "ModsMismatchWarningTitle".Translate());
			verticalPos += TitleHeight;

			Text.Font = GameFont.Small;
			var outRect = new Rect(inRect.x, verticalPos, inRect.width, inRect.height - ButtonHeight - 5f - verticalPos);
			var width = outRect.width - 16f;
			var viewRect = new Rect(0f, 0f, width, Text.CalcHeight(text, width));
			Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
			Widgets.Label(new Rect(0f, 0f, viewRect.width, viewRect.height), text);
			Widgets.EndScrollView();

			AddButton(inRect, 1, "GoBack", delegate { Close(true); }, false);
			AddButton(inRect, 2, "FixThoseMods", delegate { ModFixer.FixMods(); }, true);
			AddButton(inRect, 3, "LoadAnyway", defaultAction, false);
		}
	}
}