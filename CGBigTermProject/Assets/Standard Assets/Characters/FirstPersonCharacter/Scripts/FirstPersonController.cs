using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using Random = UnityEngine.Random;

namespace UnityStandardAssets.Characters.FirstPerson
{
    [RequireComponent(typeof (CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        [SerializeField] private float m_WalkSpeed;
        [SerializeField] private float m_JumpSpeed;
        [SerializeField] private float m_StickToGroundForce;
        [SerializeField] private float m_GravityMultiplier;
        [SerializeField] private bool m_UseHeadBob;
        [SerializeField] private CurveControlledBob m_HeadBob = new CurveControlledBob();
        [SerializeField] private LerpControlledBob m_JumpBob = new LerpControlledBob();
        [SerializeField] private float m_StepInterval;

        private Camera m_Camera;
        private bool m_Jump;
        private float m_YRotation;
        private Vector2 m_Input;
        private Vector3 m_MoveDir = Vector3.zero;
        private CharacterController m_CharacterController;
        private CollisionFlags m_CollisionFlags;
        private bool m_PreviouslyGrounded;
        private Vector3 m_OriginalCameraPosition;
        private float m_StepCycle;
        private float m_NextStep;
        private bool m_Jumping;

        public Texture Btn;
        private float horizontal,vertical;
        private bool go,back,left,right,jump;

        float minimumX = -360F;
        float maximumX = 360F;
        float minimumY = -25F;
        float maximumY = 25F;
        float rotationX = 0F;
        float rotationY = 0F;
        float lastX = 0F;
        float lastY = 0F;
        Quaternion originalRotation;
        
        private void Start()
        {
            m_CharacterController = GetComponent<CharacterController>();
            m_Camera = Camera.main;
            m_OriginalCameraPosition = m_Camera.transform.localPosition;
            m_HeadBob.Setup(m_Camera, m_StepInterval);
            m_StepCycle = 0f;
            m_NextStep = m_StepCycle/2f;
            m_Jumping = false;

            originalRotation = m_Camera.transform.localRotation;
        }
        
        private void Update()
        {
            if (Input.GetKey(KeyCode.Escape))
            {
                Application.Quit();
            }

            if (Input.touches.Length > 0)
            {
                if (Input.touches[0].phase == TouchPhase.Began)
                {
                    lastX = Input.touches[0].position.x;
                    lastY = Input.touches[0].position.y;
                }

                if (Input.touches[0].phase == TouchPhase.Moved)
                {
                    rotationX = (Input.touches[0].position.x - lastX)/Screen.width * 180;
                    rotationY = (Input.touches[0].position.y - lastY)/Screen.height * 90;

                    rotationX = ClampAngle(rotationX, minimumX, maximumX);
                    rotationY = ClampAngle(rotationY, minimumY, maximumY);

                    Quaternion xQuat = Quaternion.AngleAxis(rotationX, -Vector3.up);
                    Quaternion yQuat = Quaternion.AngleAxis(rotationY, Vector3.right);

                    transform.localRotation = originalRotation * xQuat * yQuat;
                }
                
                if (Input.touches[0].phase == TouchPhase.Ended)
                {
                    originalRotation = transform.localRotation;
                }
            }
            
            m_Jump = jump;
            
            if (!m_PreviouslyGrounded && m_CharacterController.isGrounded)
            {
                StartCoroutine(m_JumpBob.DoBobCycle());
                m_MoveDir.y = 0f;
                m_Jumping = false;
            }
            if (!m_CharacterController.isGrounded && !m_Jumping && m_PreviouslyGrounded) m_MoveDir.y = 0f;

            m_PreviouslyGrounded = m_CharacterController.isGrounded;
        }

        public static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360F) angle += 360F;
            if (angle > 360F) angle -= 360F;
            return angle;
        }

        private void FixedUpdate()
        {
            float speed = m_WalkSpeed;
            m_Input = new Vector2(horizontal, vertical);
            if (m_Input.sqrMagnitude > 1) m_Input.Normalize();
            
            Vector3 desiredMove = transform.forward*m_Input.y + transform.right*m_Input.x;
            
            RaycastHit hitInfo;
            Physics.SphereCast(transform.position, m_CharacterController.radius, Vector3.down, out hitInfo,
                               m_CharacterController.height/2f, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

            m_MoveDir.x = desiredMove.x*speed;
            m_MoveDir.z = desiredMove.z*speed;

            if (m_CharacterController.isGrounded)
            {
                m_MoveDir.y = -m_StickToGroundForce;

                if (m_Jump)
                {
                    m_MoveDir.y = m_JumpSpeed;
                    m_Jump = false;
                    m_Jumping = true;
                }
            }
            else m_MoveDir += Physics.gravity*m_GravityMultiplier*Time.fixedDeltaTime;
            
            m_CollisionFlags = m_CharacterController.Move(m_MoveDir*Time.fixedDeltaTime);

            ProgressStepCycle(speed);
            UpdateCameraPosition(speed);
        }

        private void OnGUI()
        {
            go = GUI.RepeatButton(new Rect(150, 400, 100, 100), Btn);
            back = GUI.RepeatButton(new Rect(150, 600, 100, 100), Btn);
            right = GUI.RepeatButton(new Rect(250, 500, 100, 100), Btn);
            left = GUI.RepeatButton(new Rect(50, 500, 100, 100), Btn);
            jump = GUI.RepeatButton(new Rect(1000, 500, 100, 100), Btn);

            if (go) vertical = 1;
            else if (back) vertical = -1;
            else vertical = 0;

            if (right) horizontal = 1;
            else if (left) horizontal = -1;
            else horizontal = 0;

        }

        private void ProgressStepCycle(float speed)
        {
            if (m_CharacterController.velocity.sqrMagnitude > 0 && (m_Input.x != 0 || m_Input.y != 0))
            {
                m_StepCycle += (m_CharacterController.velocity.magnitude + speed)*Time.fixedDeltaTime;
            }

            if (!(m_StepCycle > m_NextStep))
            {
                return;
            }

            m_NextStep = m_StepCycle + m_StepInterval;
        }
        
        private void UpdateCameraPosition(float speed)
        {
            Vector3 newCameraPosition;

            if (!m_UseHeadBob)
            {
                return;
            }

            if (m_CharacterController.velocity.magnitude > 0 && m_CharacterController.isGrounded)
            {
                m_Camera.transform.localPosition =
                    m_HeadBob.DoHeadBob(m_CharacterController.velocity.magnitude +
                                      speed);
                newCameraPosition = m_Camera.transform.localPosition;
                newCameraPosition.y = m_Camera.transform.localPosition.y - m_JumpBob.Offset();
            }
            else
            {
                newCameraPosition = m_Camera.transform.localPosition;
                newCameraPosition.y = m_OriginalCameraPosition.y - m_JumpBob.Offset();
            }

            m_Camera.transform.localPosition = newCameraPosition;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Rigidbody body = hit.collider.attachedRigidbody;
            //dont move the rigidbody if the character is on top of it
            if (m_CollisionFlags == CollisionFlags.Below)
            {
                return;
            }

            if (body == null || body.isKinematic)
            {
                return;
            }
            body.AddForceAtPosition(m_CharacterController.velocity*0.1f, hit.point, ForceMode.Impulse);
        }
    }
}
