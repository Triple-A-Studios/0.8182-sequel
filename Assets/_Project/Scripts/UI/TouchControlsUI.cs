using Alchemy.Inspector;
using Opoint8182.Player;
using UnityEngine;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
    [RequireComponent(typeof(PanelRenderer))]
    public class TouchControlsUI : MonoBehaviour
    {
        [Title("References")]
        [FoldoutGroup("References")] [SerializeField] private PlaneController m_planeController;

        [Title("Tunables")]
        [FoldoutGroup("Tunables")] [SerializeField] private float m_joystickMaxRadius = 50f;
        // Matches .touch-joystick-background's width/height in TouchControls.uss - used to
        // center the floating background on the touch point without depending on a live
        // layout query (its display was just flipped from none to flex, so resolvedStyle
        // isn't guaranteed to have a fresh layout pass yet within the same event callback).
        [FoldoutGroup("Tunables")] [SerializeField] private float m_joystickBackgroundSize = 140f;

        private PanelRenderer m_panelRenderer;
        private VisualElement m_joystickZone;
        private VisualElement m_joystickBase;
        private VisualElement m_joystickHandle;
        private VisualElement m_boostButton;
        private Vector2 m_joystickOrigin;

        private void Awake()
        {
            m_panelRenderer = GetComponent<PanelRenderer>();
        }

        private void OnEnable()
        {
            m_panelRenderer.RegisterUIReloadCallback(OnUIReload);
        }

        private void OnDisable()
        {
            m_panelRenderer.UnregisterUIReloadCallback(OnUIReload);

            UnregisterElementCallbacks();
        }

        private void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
        {
            UnregisterElementCallbacks();

            m_joystickZone = root.Q<VisualElement>("touch-joystick-zone");
            m_joystickBase = root.Q<VisualElement>("touch-joystick-background");
            m_joystickHandle = root.Q<VisualElement>("touch-joystick-handle");
            m_boostButton = root.Q<VisualElement>("touch-boost-button");

            m_joystickZone.RegisterCallback<PointerDownEvent>(OnJoystickPointerDown);
            m_joystickZone.RegisterCallback<PointerMoveEvent>(OnJoystickPointerMove);
            m_joystickZone.RegisterCallback<PointerUpEvent>(OnJoystickPointerUp);
            m_joystickZone.RegisterCallback<PointerCancelEvent>(OnJoystickPointerCancel);

            m_boostButton.RegisterCallback<PointerDownEvent>(OnBoostPointerDown);
            m_boostButton.RegisterCallback<PointerUpEvent>(OnBoostPointerUp);
            m_boostButton.RegisterCallback<PointerCancelEvent>(OnBoostPointerCancel);
        }

        // Guards against double-registration - OnUIReload can fire more than once per
        // enable (e.g. a live asset reload), unlike the old UIDocument setup where a
        // single OnEnable query only ever ran once per enable/disable cycle.
        private void UnregisterElementCallbacks()
        {
            if (m_joystickZone != null)
            {
                m_joystickZone.UnregisterCallback<PointerDownEvent>(OnJoystickPointerDown);
                m_joystickZone.UnregisterCallback<PointerMoveEvent>(OnJoystickPointerMove);
                m_joystickZone.UnregisterCallback<PointerUpEvent>(OnJoystickPointerUp);
                m_joystickZone.UnregisterCallback<PointerCancelEvent>(OnJoystickPointerCancel);
            }

            if (m_boostButton != null)
            {
                m_boostButton.UnregisterCallback<PointerDownEvent>(OnBoostPointerDown);
                m_boostButton.UnregisterCallback<PointerUpEvent>(OnBoostPointerUp);
                m_boostButton.UnregisterCallback<PointerCancelEvent>(OnBoostPointerCancel);
            }
        }

        private void OnJoystickPointerDown(PointerDownEvent evt)
        {
            m_joystickZone.CapturePointer(evt.pointerId);

            m_joystickOrigin = evt.position;
            var halfSize = m_joystickBackgroundSize * 0.5f;
            m_joystickBase.style.left = m_joystickOrigin.x - halfSize;
            m_joystickBase.style.top = m_joystickOrigin.y - halfSize;
            m_joystickBase.style.display = DisplayStyle.Flex;

            UpdateJoystick(evt.position);
        }

        private void OnJoystickPointerMove(PointerMoveEvent evt)
        {
            if (!m_joystickZone.HasPointerCapture(evt.pointerId)) return;
            UpdateJoystick(evt.position);
        }

        private void OnJoystickPointerUp(PointerUpEvent evt)
        {
            if (!m_joystickZone.HasPointerCapture(evt.pointerId)) return;
            m_joystickZone.ReleasePointer(evt.pointerId);
            ResetJoystick();
        }

        private void OnJoystickPointerCancel(PointerCancelEvent evt)
        {
            if (!m_joystickZone.HasPointerCapture(evt.pointerId)) return;
            m_joystickZone.ReleasePointer(evt.pointerId);
            ResetJoystick();
        }

        private void UpdateJoystick(Vector2 pointerPosition)
        {
            var delta = Vector2.ClampMagnitude(pointerPosition - m_joystickOrigin, m_joystickMaxRadius);

            m_joystickHandle.style.translate = new StyleTranslate(new Translate(delta.x, delta.y, 0f));

            // UI-space Y grows downward; gameplay steer Y is "up positive" (matches PlaneController.Velocity.y).
            if (m_planeController != null) m_planeController.TouchSteer = new Vector2(delta.x, -delta.y) / m_joystickMaxRadius;
        }

        private void ResetJoystick()
        {
            m_joystickBase.style.display = DisplayStyle.None;
            m_joystickHandle.style.translate = new StyleTranslate(new Translate(0f, 0f, 0f));
            if (m_planeController != null) m_planeController.TouchSteer = Vector2.zero;
        }

        private void OnBoostPointerDown(PointerDownEvent evt)
        {
            m_boostButton.CapturePointer(evt.pointerId);
            if (m_planeController != null) m_planeController.TouchBoost = true;
        }

        private void OnBoostPointerUp(PointerUpEvent evt)
        {
            if (!m_boostButton.HasPointerCapture(evt.pointerId)) return;
            m_boostButton.ReleasePointer(evt.pointerId);
            if (m_planeController != null) m_planeController.TouchBoost = false;
        }

        private void OnBoostPointerCancel(PointerCancelEvent evt)
        {
            if (!m_boostButton.HasPointerCapture(evt.pointerId)) return;
            m_boostButton.ReleasePointer(evt.pointerId);
            if (m_planeController != null) m_planeController.TouchBoost = false;
        }
    }
}
