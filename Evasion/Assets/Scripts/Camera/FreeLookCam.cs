using System;
using UnityEngine;

using RuntimeDebugDraw;
using UnityStandardAssets.Cameras;

namespace Game.Cameras
{
    public class FreeLookCam : PivotBasedCameraRig
    {
        // This script is designed to be placed on the root object of a camera rig,
        // comprising 3 gameobjects, each parented to the next:

        // 	Camera Rig
        // 		Pivot
        // 			Camera

        public float MoveSpeed = 1f;                      // How fast the rig will move to keep up with the target's position.
        [Range(0f, 10f)] [SerializeField] private float m_TurnSpeed = 1.5f;   // How fast the rig will rotate from user input.
        [SerializeField] private float m_TurnSmoothing = 0.0f;                // How much smoothing to apply to the turn input, to reduce mouse-turn jerkiness
        [SerializeField] private float m_TiltMax = 75f;                       // The maximum value of the x axis rotation of the pivot.
        [SerializeField] private float m_TiltMin = 45f;                       // The minimum value of the x axis rotation of the pivot.
        [SerializeField] private float m_LockAngle = 60f;                       // The minimum value of the y axis rotation around the pivot.
        [SerializeField] private bool m_LockCursor = false;                   // Whether the cursor should be hidden and locked.
        [SerializeField] private bool m_VerticalAutoReturn = false;           // set wether or not the vertical axis should auto return

        [SerializeField] private bool m_DebugLockAngle = false;
        [SerializeField] private bool m_LockAngleActive = false;

        private float m_LookAngle;                    // The rig's y axis rotation.
        private float m_TiltAngle;                    // The pivot's x axis rotation.
        private const float k_LookDistance = 100f;    // How far in front of the pivot the character's look target is.
		private Vector3 m_PivotEulers;
		private Quaternion m_PivotTargetRot;
		private Quaternion m_TransformTargetRot;

        protected override void Awake()
        {
            base.Awake();
            // Lock or unlock the cursor.
            Cursor.lockState = m_LockCursor ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !m_LockCursor;
			m_PivotEulers = m_Pivot.rotation.eulerAngles;

	        m_PivotTargetRot = m_Pivot.transform.localRotation;
			m_TransformTargetRot = transform.localRotation;
        }


        protected void Update()
        {
            HandleRotationMovement();
            if (m_LockCursor && Input.GetMouseButtonUp(0))
            {
                Cursor.lockState = m_LockCursor ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = !m_LockCursor;
            }
        }


        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void SetTarget(Transform target)
        {
            m_Target = target;
        }

        protected override void FollowTarget(float deltaTime)
        {
            if (m_Target == null) return;
            // Move the rig towards target position.
            transform.position = Vector3.Lerp(transform.position, m_Target.position, deltaTime*MoveSpeed);
        }

        private Quaternion ClampEulerToLock(Quaternion r)
        {
            float c = m_Target.eulerAngles.y;
            float a = c - m_LockAngle / 2f;
            float b = c + m_LockAngle / 2f;

            float diffA = Mathf.Abs(Mathf.DeltaAngle(r.eulerAngles.y, a));
            float diffB = Mathf.Abs(Mathf.DeltaAngle(r.eulerAngles.y, b));
            float diffC = Mathf.Abs(Mathf.DeltaAngle(r.eulerAngles.y, c));

            if (diffC < m_LockAngle / 2f) return r;
            if (diffC > 90) r.eulerAngles = new Vector3(r.eulerAngles.x, c, r.eulerAngles.z);
            else if (diffA < diffB) r.eulerAngles = new Vector3(r.eulerAngles.x, a, r.eulerAngles.z);
            else r.eulerAngles = new Vector3(r.eulerAngles.x, b, r.eulerAngles.z);
            return r;
        }

        private void HandleRotationMovement()
        {
			if(Time.timeScale < float.Epsilon)
			return;

            // Read the user input
            var x = Input.GetAxis("Mouse X");
            var y = Input.GetAxis("Mouse Y");


            // Adjust the look angle by an amount proportional to the turn speed and horizontal input.
            m_LookAngle += x * m_TurnSpeed;

            // Rotate the rig (the root object) around Y axis only:
            m_TransformTargetRot = Quaternion.Euler(0f, m_LookAngle, 0f);

            if (m_DebugLockAngle)
            {
                Draw.DrawRay(m_Target.position, m_Target.forward, Color.red);
                Draw.DrawRay(m_Target.position, -m_Target.forward, Color.white);
                Quaternion min = Quaternion.AngleAxis(m_Target.rotation.eulerAngles.y - m_LockAngle / 2f, Vector3.up);
                Draw.DrawRay(m_Target.position, -(min * Vector3.forward), Color.gray);
                Quaternion max = Quaternion.AngleAxis(m_Target.rotation.eulerAngles.y + m_LockAngle / 2f, Vector3.up);
                Draw.DrawRay(m_Target.position, -(max * Vector3.forward), Color.gray);
                Draw.DrawRay(m_Target.position, -(m_TransformTargetRot * Vector3.forward), Color.blue);
            }
            if (m_LockAngleActive)
                m_TransformTargetRot = ClampEulerToLock(m_TransformTargetRot);

            if (m_VerticalAutoReturn)
            {
                // For tilt input, we need to behave differently depending on whether we're using mouse or touch input:
                // on mobile, vertical input is directly mapped to tilt value, so it springs back automatically when the look input is released
                // we have to test whether above or below zero because we want to auto-return to zero even if min and max are not symmetrical.
                m_TiltAngle = y > 0 ? Mathf.Lerp(0, -m_TiltMin, y) : Mathf.Lerp(0, m_TiltMax, -y);
            }
            else
            {
                // on platforms with a mouse, we adjust the current angle based on Y mouse input and turn speed
                m_TiltAngle -= y*m_TurnSpeed;
                // and make sure the new value is within the tilt range
                m_TiltAngle = Mathf.Clamp(m_TiltAngle, -m_TiltMin, m_TiltMax);
            }

            // Tilt input around X is applied to the pivot (the child of this object)
			m_PivotTargetRot = Quaternion.Euler(m_TiltAngle, m_PivotEulers.y , m_PivotEulers.z);

			if (m_TurnSmoothing > 0)
			{
				m_Pivot.localRotation = Quaternion.Slerp(m_Pivot.localRotation, m_PivotTargetRot, m_TurnSmoothing * Time.deltaTime);
				transform.localRotation = Quaternion.Slerp(transform.localRotation, m_TransformTargetRot, m_TurnSmoothing * Time.deltaTime);
			}
			else
			{
				m_Pivot.localRotation = m_PivotTargetRot;
				transform.localRotation = m_TransformTargetRot;
			}
        }

        public Transform Cam { get { return m_Cam; } }
        public Transform Pivot { get { return m_Pivot; } }
    }
}
