using System.Collections;
using UnityEngine;
using System.Linq;

using ProtectCameraFromWallClip = Game.Cameras.ProtectCameraFromWallClip;
using FreeLookCam = Game.Cameras.FreeLookCam;
using Bolt;
using TMPro;

public class PlayerMovement : Bolt.EntityBehaviour<IPlayerState>
{
    public float speed;
    public float turnSpeed;
    public GameObject crosshairRef;

    private bool _allowDoorOpen;
    private DoorManager _facingDoor;

    Animator animator;
    CharacterController controller;
    bool _crouchJustEnded;
    CollisionFlags collisionFlags;
    BoxCollider triggerArea;
    bool isServerPlayer;
    int lastFrameFired = 0;

    public GameObject camPrefab;
    [SerializeField] private TextMeshProUGUI _healthUI;
    [SerializeField] private TextMeshProUGUI _activateGameOverFeedback;

    private float TimeSinceHit = 0;

    public override void Attached()
    {
        _healthUI = GameObject.FindGameObjectWithTag("HealthUI").GetComponent<TextMeshProUGUI>();
        var instance = GameObject.FindGameObjectWithTag("CheatUI");
        _activateGameOverFeedback = instance.GetComponent<TextMeshProUGUI>();
        _healthUI.enabled = true;
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
        state.SetTransforms(state.transform, transform);
        state.SetAnimator(GetComponent<Animator>());
        triggerArea = GetComponent<BoxCollider>();
        state.OnDeath = OnDeath;
        state.OnFlash = OnFlash;
        state.OnShoot = OnShoot;
        state.OnOpenDoor = OnOpenDoor;
        if (camPrefab != null && (entity.hasControl ||
            (BoltNetwork.IsServer && GameObject.FindGameObjectsWithTag("Player").Length <= 1)))
        {
            GameObject cam = Instantiate(camPrefab, transform.position, Quaternion.identity);
            cam.GetComponent<Game.Cameras.FreeLookCam>().SetTarget(transform);
            gameObject.AddComponent<AudioListener>();
            crosshairRef = FindObjectOfType<Canvas>().transform.GetChild(0).gameObject;
        }
        isServerPlayer = (BoltNetwork.IsServer && GameObject.FindGameObjectsWithTag("Player").Length <= 1);
        if (entity.isOwner)
        {
            state.Health = 100;
            state.AllowDeath = true;
        }
    }
    
    public override void SimulateOwner()
    {
        if (TimeSinceHit + 5 < BoltNetwork.Time)
        {
            state.Health += 10f * BoltNetwork.FrameDeltaTime;
            if (state.Health > 100)
                state.Health = 100;
        }
        if (!(entity.isOwner && isServerPlayer))
            return ;
        UpdateHealthUI((int)state.Health);
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        state.Crouch = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);
        Crouch();

        state.Aim = GetComponent<Inventory>().Weapon != null && Input.GetMouseButton(1);
        state.Run = Input.GetKey(KeyCode.LeftShift);
        if (Input.GetKeyDown(KeyCode.G) && lastFrameFired != Time.frameCount)
        {
            lastFrameFired = Time.frameCount;
            state.AllowDeath = !state.AllowDeath;
            FindObjectOfType<GameManager>().ToggleCheat();
        }
        if (Input.GetKeyDown(KeyCode.E))
            state.OpenDoor();
        float s = (!state.Crouch && state.Run) ? 1 : 0.5f;
        Vector3 forward = Camera.main.transform.forward;
        Vector3 right = Camera.main.transform.right;

        forward.y = 0f;
        right.y = 0f;
        Vector3 moveDirection = forward * v + right * h;
        controller.Move(((moveDirection * s * speed) + Physics.gravity) * BoltNetwork.FrameDeltaTime * 3);
        state.MoveX = Vector3.Angle(transform.forward, moveDirection) > 45 ?
            Vector3.SignedAngle(transform.forward, moveDirection, Vector3.up) / 90 : 0;
        state.MoveZ = moveDirection != Vector3.zero ? s : 0;
        if (moveDirection.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                        Quaternion.LookRotation(moveDirection), turnSpeed);
        HandleAim(Input.GetMouseButtonDown(0), Camera.main.transform.position, Camera.main.transform.forward);
    }

    void Crouch()
    {
        if (state.Crouch)
        {
            controller.height = 0.7f;
            controller.center = new Vector3(controller.center.x, .35f, controller.center.z);
            _crouchJustEnded = true;
        }
        else if (_crouchJustEnded)
        {
            controller.height = 2f;
            controller.center = new Vector3(controller.center.x, 1.0f, controller.center.z);
            _crouchJustEnded = false;
        }
    }

    public override void SimulateController()
    {
        IPlayerCommandInput input = PlayerCommand.Create();
        input.horizontal = Input.GetAxis("Horizontal");
        input.vertical = Input.GetAxis("Vertical");
        input.crouch = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);
        input.aiming = Input.GetMouseButton(1);
        if (Input.GetMouseButtonDown(0) && lastFrameFired != Time.frameCount)
        {
            input.fire = Input.GetMouseButtonDown(0);
            lastFrameFired = Time.frameCount;
        }
        else
            input.fire = false;
        input.aimDirection = Camera.main.transform.forward;
        input.aimPosition = Camera.main.transform.position;
        input.rightDirection = Camera.main.transform.right;
        input.running = Input.GetKey(KeyCode.LeftShift);
        if (Input.GetKeyDown(KeyCode.G) && lastFrameFired != Time.frameCount)
        {
            input.toggleCheat = true;
            lastFrameFired = Time.frameCount;
            FindObjectOfType<GameManager>().ToggleCheat();
        }
        else
            input.toggleCheat = false;
        input.rotation = transform.localRotation;
        input.interact = Input.GetKeyDown(KeyCode.E);
        UpdateHealthUI((int)state.Health);
        entity.QueueInput(input);
        Vector3 forward = input.aimDirection;
        Vector3 right = input.rightDirection;
        float s = (!input.crouch && input.running) ? 1 : 0.5f;
        forward.y = 0f;
        right.y = 0f;
        Vector3 moveDirection = forward * input.vertical + right * input.horizontal;
        controller.Move(((moveDirection * s * speed) + Physics.gravity) * BoltNetwork.FrameDeltaTime * 3);
        if (moveDirection.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                        Quaternion.LookRotation(moveDirection), turnSpeed);
    }

    public override void ExecuteCommand(Command command, bool resetState)
    {
        if (!(command is PlayerCommand))
            return;
        ExecutePlayerCommand((PlayerCommand)command, resetState);
    }

    private void ExecutePlayerCommand(PlayerCommand command, bool resetState)
    {
        if (resetState)
        {
            transform.localPosition = command.Result.position;
            transform.localRotation = command.Result.rotation;
            HandleAim(command.Input.fire, command.Input.aimPosition, command.Input.rightDirection);
        }
        else if (entity.isOwner && command.IsFirstExecution)
        {
            state.Crouch = command.Input.crouch;
            state.Run = command.Input.running;
            state.Aim = command.Input.aiming;
            if (command.Input.toggleCheat)                
                state.AllowDeath = !state.AllowDeath;
            if (command.Input.interact)
                state.OpenDoor();
            if (command.Input.aiming)
                transform.localRotation = command.Input.rotation;
            float s = (!state.Crouch && state.Run) ? 1 : 0.5f;
            Vector3 forward = command.Input.aimDirection;
            Vector3 right = command.Input.rightDirection;

            Crouch();
            forward.y = 0f;
            right.y = 0f;
            Vector3 moveDirection = forward * command.Input.vertical + right * command.Input.horizontal;
            controller.Move(((moveDirection * s * speed) + Physics.gravity) * BoltNetwork.FrameDeltaTime * 3);
            state.MoveX = Vector3.Angle(transform.forward, moveDirection) > 45 && moveDirection != Vector3.zero ?
                Vector3.SignedAngle(transform.forward, moveDirection, Vector3.up) / 90 : 0;
            state.MoveZ = moveDirection != Vector3.zero ? s : 0;
            if (moveDirection.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                            Quaternion.LookRotation(moveDirection), turnSpeed);
            HandleAim(command.Input.fire, command.Input.aimPosition, command.Input.aimDirection);
            command.Result.position = transform.localPosition;
            command.Result.rotation = transform.localRotation;
        }
        else if (entity.hasControl)
        {
            HandleAim(command.Input.fire, command.Input.aimPosition, command.Input.aimDirection);
        }
    }

    private void HandleAim(bool fire, Vector3 position, Vector3 direction)
    {
        FreeLookCam cameraRig = null;
        ProtectCameraFromWallClip wallClip = null;
        Transform cam = null;
        if (entity.hasControl || (entity.isOwner && isServerPlayer))
        {
            cameraRig = FindObjectOfType<FreeLookCam>();
            wallClip = cameraRig.gameObject.GetComponent<ProtectCameraFromWallClip>();
            cam = cameraRig.GetComponentInChildren<Camera>().transform;
        }
        if (state.Aim)
        {
            if (entity.hasControl || (entity.isOwner && isServerPlayer))
            {
                // zoom in camera
                Vector3 lookPos = cameraRig.Cam.forward;
                lookPos.y = 0;
                transform.rotation = Quaternion.LookRotation(lookPos);
                cameraRig.Cam.localPosition = new Vector3 { z = -1f };
                cameraRig.Pivot.localPosition = new Vector3(0.5f, cameraRig.Pivot.localPosition.y, cameraRig.Pivot.localPosition.z);
                cameraRig.MoveSpeed = 20f;
                wallClip.active = false; // avoid wall clip script interfering w/ zoomed cam positions
            }
            if (fire && lastFrameFired != Time.frameCount && entity.isOwner)
            {
                lastFrameFired = Time.frameCount;
                GetComponent<Inventory>().Weapon.Shoot();
                state.Shoot();
                RaycastHit[] hits;
                if ((hits = Physics.RaycastAll(position, direction, 50).OrderBy(h => h.distance).ToArray()) != null)
                {
                    foreach (RaycastHit hit in hits)
                    {
                        if (hit.collider.CompareTag("NPC"))
                            hit.collider.GetComponent<robotCollider>().isShot(gameObject);
                        else if (hit.transform.gameObject.layer == LayerMask.NameToLayer("NPC"))
                            continue;
                        break;
                    }
                }
            }
            // activate crosshair
            if (entity.hasControl || (entity.isOwner && isServerPlayer))
                crosshairRef.SetActive(true);
        }
        else if (entity.hasControl || (entity.isOwner && isServerPlayer))
        {
            // reset camera
            wallClip.active = true;
            cameraRig.MoveSpeed = 1f;
            cameraRig.Pivot.localPosition = new Vector3(0.35f, cameraRig.Pivot.localPosition.y, cameraRig.Pivot.localPosition.z);

            // deactivate crosshair
            crosshairRef.SetActive(false);
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody body = hit.collider.attachedRigidbody;
        //dont move the rigidbody if the character is on top of it
        if (collisionFlags == CollisionFlags.Below)
        {
            return;
        }

        if (body == null || body.isKinematic)
        {
            return;
        }
        body.AddForceAtPosition(controller.velocity * 0.1f, hit.point, ForceMode.Impulse);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Door")
        {
            _allowDoorOpen = true;
            _facingDoor = other.GetComponent<DoorManager>();
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Door")
        {
            _allowDoorOpen = false;
        }
    }

    public void TakeDamage(int damage)
    {

        state.Flash();
        state.Health -= (float)damage;
        TimeSinceHit = BoltNetwork.Time;
        if (state.Health <= 0 && state.AllowDeath)
        {
            state.Death();
        }
        UpdateHealthUI((int)state.Health);
    }

    private void OnDeath()
    {
        if ((entity.hasControl || (entity.isOwner && isServerPlayer)))
            FindObjectOfType<GameManager>().GameOver();
    }

    private void OnFlash()
    {
        if (entity.hasControl || (entity.isOwner && isServerPlayer))
            FindObjectOfType<FlashDamage>().Flash();
    }

    private void OnOpenDoor()
    {
        if (_allowDoorOpen)
            _facingDoor.TriggerDoor();
    }

    private void OnShoot()
    {
        if (entity.isOwner)
        {
            float soundDistance = GetComponent<Inventory>().WeaponPrefab.GetComponent<AudioSource>().maxDistance;
            NPCStateMachine.MakeNPCsInRangeInspect(transform.position, soundDistance);
        }
    }

    public void UpdateHealthUI(int health)
    {
        _healthUI.text = Mathf.Clamp(health, 0, 100).ToString();
    }
}
