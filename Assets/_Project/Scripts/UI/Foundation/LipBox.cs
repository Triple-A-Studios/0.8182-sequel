using UnityEngine.UIElements;

namespace Opoint8182.UI
{
	public enum LipVariant
	{
		Panel,
		Primary,
		Secondary,
		Danger,
		Positive,
	}

	public enum LipSize
	{
		Small,
		Medium,
		Large,
	}

	/// <summary>
	/// The mockup's chunky bevelled chip: a face with a hard, blur-free lip peeking out below it.
	/// USS has no box-shadow, so the lip is a real backing element offset downward (mockup instructions,
	/// section 4). Children added in UXML go inside the face. Everything visual is in <c>Theme.uss</c>;
	/// the variant and lip size only pick modifier classes.
	/// <para>The element's own height includes the lip: a 114px-tall LipBox with a medium (10px) lip has a
	/// 104px face.</para>
	/// </summary>
	[UxmlElement]
	public partial class LipBox : VisualElement
	{
		public const string UssClassName = "lipbox";
		public const string LipUssClassName = "lipbox__lip";
		public const string FaceUssClassName = "lipbox__face";

		private readonly VisualElement m_lip;
		private readonly VisualElement m_face;
		private LipVariant m_variant = LipVariant.Panel;
		private LipSize m_lipSize = LipSize.Medium;

		public override VisualElement contentContainer => m_face;

		/// <summary>The face element; use for state that targets only the face (never the lip).</summary>
		public VisualElement Face => m_face;

		[UxmlAttribute]
		public LipVariant variant
		{
			get => m_variant;
			set
			{
				RemoveFromClassList(VariantClass(m_variant));
				m_variant = value;
				AddToClassList(VariantClass(m_variant));
			}
		}

		[UxmlAttribute]
		public LipSize lipSize
		{
			get => m_lipSize;
			set
			{
				RemoveFromClassList(SizeClass(m_lipSize));
				m_lipSize = value;
				AddToClassList(SizeClass(m_lipSize));
			}
		}

		public LipBox()
		{
			AddToClassList(UssClassName);

			m_lip = new VisualElement { pickingMode = PickingMode.Ignore };
			m_lip.AddToClassList(LipUssClassName);
			hierarchy.Add(m_lip);

			m_face = new VisualElement();
			m_face.AddToClassList(FaceUssClassName);
			hierarchy.Add(m_face);

			AddToClassList(VariantClass(m_variant));
			AddToClassList(SizeClass(m_lipSize));
		}

		private static string VariantClass(LipVariant variant) => $"{UssClassName}--{variant.ToString().ToLowerInvariant()}";

		private static string SizeClass(LipSize size) => size switch
		{
			LipSize.Small => $"{UssClassName}--lip-sm",
			LipSize.Large => $"{UssClassName}--lip-lg",
			_ => $"{UssClassName}--lip-md",
		};
	}
}
