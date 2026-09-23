using Alchemy.Inspector;
using Opoint8182.Player;
using UnityEngine;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class TouchControlsUI : MonoBehaviour
    {
        [Title("References")]
        [FoldoutGroup("References")] [SerializeField] private PlaneController m_planeController;

        [Title("Tunables")]
        [FoldoutGroup("Tunables")] [SerializeField] private float m_joystickMaxRadius = 50f;

        private UIDocument m_uiDocument;
        private VisualElement m_joystickBase;
        private VisualElement m_joystickHandle;
        private VisualElement m_boostButton;

        private void Awake()
        {
            m_uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            var root = m_uiDocument.rootVisualElement;
            m_joystickBase = root.Q<VisualElement>("touch-joystick-background");
            m_joystickHandle = root.Q<VisualElement>("touch-joystick-handle");
            m_boostButton = root.Q<VisualElement>("touch-boost-button");

            m_joystickBase.RegisterCallback<PointerDownEvent>(OnJoystickPointerDown);
            m_joystickBase.RegisterCallback<PointerMoveEvent>(OnJoystickPointerMove);
            m_joystickBase.RegisterCallback<PointerUpEvent>(OnJoystickPointerUp);
            m_joystickBase.RegisterCallback<PointerCancelEvent>(OnJoystickPointerCancel);

            m_boostButton.RegisterCallback<PointerDownEvent>(OnBoostPointerDown);
            m_boostButton.RegisterCallback<PointerUpEvent>(OnBoostPointerUp);
            m_boostButton.RegisterCallback<PointerCancelEvent>(OnBoostPointerCancel);
        }

        private void OnDisable()
        {
            m_joystickBase.UnregisterCallback<PointerDownEvent>(OnJoystickPointerDown);
            m_joystickBase.UnregisterCallback<PointerMoveEvent>(OnJoystickPointerMove);
            m_joystickBase.UnregisterCallback<PointerUpEvent>(OnJoystickPointerUp);
            m_joystickBase.UnregisterCallback<PointerCancelEvent>(OnJoystickPointerCancel);

            m_boostButton.UnregisterCallback<PointerDownEvent>(OnBoostPointerDown);
            m_boostButton.UnregisterCallback<PointerUpEvent>(OnBoostPointerUp);
            m_boostButton.UnregisterCallback<PointerCancelEvent>(OnBoostPointerCancel);
        }

        private void OnJoystickPointerDown(PointerDownEvent evt)
        {
            m_joystickBase.CapturePointer(evt.pointerId);
            UpdateJoystick(evt.position);
        }

        private void OnJoystickPointerMove(PointerMoveEvent evt)
        {
            if (!m_joystickBase.HasPointerCapture(evt.pointerId)) return;
            UpdateJoystick(evt.position);
        }

        private void OnJoystickPointerUp(PointerUpEvent evt)
        {
            if (!m_joystickBase.HasPointerCapture(evt.pointerId)) return;
            m_joystickBase.ReleasePointer(evt.pointerId);
            ResetJoystick();
        }

        private void OnJoystickPointerCancel(PointerCancelEvent evt)
        {
            if (!m_joystickBase.HasPointerCapture(evt.pointerId)) return;
            m_joystickBase.ReleasePointer(evt.pointerId);
            ResetJoystick();
        }

        private void UpdateJoystick(Vector2 pointerPosition)
        {
            var center = m_joystickBase.worldBound.center;
            var delta = Vector2.ClampMagnitude(pointerPosition - (Vector2)center, m_joystickMaxRadius);

            m_joystickHandle.style.translate = new StyleTranslate(new Translate(delta.x, delta.y, 0f));

            // UI-space Y grows downward; gameplay steer Y is "up positive" (matches PlaneController.Velocity.y).
            if (m_planeController != null) m_planeController.TouchSteer = new Vector2(delta.x, -delta.y) / m_joystickMaxRadius;
        }

        private void ResetJoystick()
        {
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
