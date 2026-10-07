using System;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
	/// <summary>
	/// A pressable <see cref="LipBox"/>: translate-down press state (via <see cref="PressedClassManipulator"/>
	/// and <c>.is-pressed</c> in USS), a <see cref="clicked"/> event, and an optional text label. Disable it with
	/// <c>SetEnabled(false)</c>; the <c>:disabled</c> rule in <c>Theme.uss</c> dims it and it stops raising clicks.
	/// </summary>
	[UxmlElement]
	public partial class LipButton : LipBox
	{
		public const string LabelUssClassName = "lipbox__label";

		private readonly Label m_label;
		private string m_text = string.Empty;

		public event Action clicked;

		[UxmlAttribute]
		public string text
		{
			get => m_text;
			set
			{
				m_text = value ?? string.Empty;
				m_label.text = m_text;
				m_label.style.display = m_text.Length == 0 ? DisplayStyle.None : DisplayStyle.Flex;
			}
		}

		public LipButton()
		{
			m_label = new Label { pickingMode = PickingMode.Ignore };
			m_label.AddToClassList(LabelUssClassName);
			m_label.style.display = DisplayStyle.None;
			Face.Add(m_label);

			variant = LipVariant.Secondary;
			AddToClassList("lipbutton");
			this.AddManipulator(new PressedClassManipulator());
			RegisterCallback<ClickEvent>(OnClick);
		}

		private void OnClick(ClickEvent evt)
		{
			if (!enabledInHierarchy) return;
			clicked?.Invoke();
		}
	}
}
