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
        [SerializeField] private bool m_IsWalking;
        [SerializeField] private float m_WalkSpeed;
        [SerializeField] [Range(0f, 1f)] private float m_RunstepLenghten;
        [SerializeField] private float m_JumpSpeed;
        [SerializeField] private float m_StickToGroundForce;
        [SerializeField] private float m_GravityMultiplier;
        [SerializeField] private bool m_UseFovKick;
        [SerializeField] private FOVKick m_FovKick = new FOVKick();
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

        public Texture Btnup, Btnd, Btnr, Btnl;
        private float horizontal,vertical;
        private bool go,back,left,right,move = true,view = false;

        Quaternion originalRotation;

        // Use this for initialization
        private void Start()
        {
            m_CharacterController = GetComponent<CharacterController>();
            m_Camera = Camera.main;
            m_OriginalCameraPosition = m_Camera.transform.localPosition;
            m_FovKick.Setup(m_Camera);
            m_HeadBob.Setup(m_Camera, m_StepInterval);
            m_StepCycle = 0f;
            m_NextStep = m_StepCycle/2f;
            m_Jumping = false;

            originalRotation = m_Camera.transform.localRotation;
        }


        // Update is called once per frame
        

        private void Update()
        {
            if (Input.GetKey(KeyCode.Escape))
            {
                Application.Quit();
                
            }
            if (Input.GetKey(KeyCode.Menu)&&view)
            {
                view = false;
                move = true;
            }
            else if (Input.GetKey(KeyCode.Menu) && move)
            {
                view = true;
                move = false;
            }

            float minimumX = -360F;
            float maximumX = 360F;

            float minimumY = -25F;
            float maximumY = 25F;

            float rotationX = 0F;
            float rotationY = 0F;

            float oldRotationX = 0F;
            float oldRotationY = 0F;

            float lastX = 0F;
            float lastY = 0F;

            if (Input.touches.Length > 0 && view)
            {
                if (Input.touches[0].phase == TouchPhase.Began)
                {
                    lastX = Input.touches[0].position.x;
                    lastY = Input.touches[0].position.y;

                    rotationX = transform.localEulerAngles.x;
                    rotationY = transform.localEulerAngles.y;

                    oldRotationX = rotationX;
                    oldRotationY = rotationY;
                }

                if (Input.touches[0].phase == TouchPhase.Moved)
                {
                    rotationX = (oldRotationX + (Input.touches[0].position.x - lastX)*180/Screen.width);
                    rotationY = (oldRotationY + (Input.touches[0].position.y - lastY)*90/Screen.height);

                    rotationX = ClampAngle(rotationX, minimumX, maximumX);
                    rotationY = ClampAngle(rotationY, minimumY, maximumY);

                    Quaternion xQuaternion = Quaternion.AngleAxis(rotationX, Vector3.up);
                    Quaternion yQuaternion = Quaternion.AngleAxis(rotationY, -Vector3.right);

                    transform.localRotation = originalRotation * xQuaternion * yQuaternion;
                }

                //this should make rotating from last position, but it
                //makes rotating incorrect (askew)
                //without code below rotating works, but always
                //starts from the same position
                if (Input.touches[0].phase == TouchPhase.Ended)
                {
                    originalRotation = transform.localRotation;
                }
            }


            // the jump state needs to read here to make sure it is not missed
            if (!m_Jump)
            {
                m_Jump = false;
            }

            if (!m_PreviouslyGrounded && m_CharacterController.isGrounded)
            {
                StartCoroutine(m_JumpBob.DoBobCycle());
                m_MoveDir.y = 0f;
                m_Jumping = false;
            }
            if (!m_CharacterController.isGrounded && !m_Jumping && m_PreviouslyGrounded)
            {
                m_MoveDir.y = 0f;
            }

            m_PreviouslyGrounded = m_CharacterController.isGrounded;

        }

        public static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360F)
                angle += 360F;
            if (angle > 360F)
                angle -= 360F;
            return angle;
        }

        private void FixedUpdate()
        {
            float speed = m_WalkSpeed;
            GetInput(out speed);
            // always move along the camera forward as it is the direction that it being aimed at
            Vector3 desiredMove = transform.forward*m_Input.y + transform.right*m_Input.x;

            // get a normal for the surface that is being touched to move along it
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
            else
            {
                m_MoveDir += Physics.gravity*m_GravityMultiplier*Time.fixedDeltaTime;
            }
            m_CollisionFlags = m_CharacterController.Move(m_MoveDir*Time.fixedDeltaTime);

            ProgressStepCycle(speed);
            UpdateCameraPosition(speed);

            //m_MouseLook.UpdateCursorLock();
        }

        private void ProgressStepCycle(float speed)
        {
            if (m_CharacterController.velocity.sqrMagnitude > 0 && (m_Input.x != 0 || m_Input.y != 0))
            {
                m_StepCycle += (m_CharacterController.velocity.magnitude + (speed*(m_IsWalking ? 1f : m_RunstepLenghten)))*
                             Time.fixedDeltaTime;
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
                                      (speed*(m_IsWalking ? 1f : m_RunstepLenghten)));
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

        void OnGUI()
        {
            go = GUI.RepeatButton(new Rect(150, 400, 100, 100), Btnup);
            back = GUI.RepeatButton(new Rect(150, 600, 100, 100), Btnd);
            right = GUI.RepeatButton(new Rect(250, 500, 100, 100), Btnr);
            left = GUI.RepeatButton(new Rect(50, 500, 100, 100), Btnl);
            if (move)
            {
                if (go) vertical = 1;
                else if (back) vertical = -1;
                else vertical = 0;
                if (right) horizontal = 1;
                else if (left) horizontal = -1;
                else horizontal = 0;
            }
        }

        private void GetInput(out float speed)
        {
            // Read input is ongui
            
            speed = m_WalkSpeed;
            m_Input = new Vector2(horizontal, vertical);

            // normalize input if it exceeds 1 in combined length:
            if (m_Input.sqrMagnitude > 1)
            {
                m_Input.Normalize();
            }
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
