using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum NPCstate
{
    STOP,
    WALK,
    TALK,
    GOTO,
    INSPECT,
    SHOOT,
    COVER,
    DEAD
};

public class NPCStateMachine : Bolt.EntityBehaviour<IPlayerState>
{
    public Vector3 midPatrol;
    public Vector3 endPatrol;
    public List<Vector3> patrolWaypoints;

    CharacterController characterController;
    public GameObject target;
    public Vector3 targetPosition = Vector3.zero;
    Vector3 velocity = Vector3.zero;
    public List<Vector3> path;
    public NPCstate behaviourState;
    TacticalPoint curCover;
    int patrolIdx = 0;
    float health = 100;
    float transitionTime = 0;
    public float alertLevel = 0;
    float lastShotReceived = 0;
    float lastShotFired = 0;

    float stoppingRadius = 1f;
    float slowingRadius = 1.5f;
    float maxMag = 1.5f;
    float maxAcc = 7.5f;

    [SerializeField] private GameObject _inspectUIFeedback;
    [SerializeField] private GameObject _shootUIFeedback;

    public override void Attached()
    {
        characterController = GetComponent<CharacterController>();
        state.SetTransforms(state.transform, transform);
        state.SetAnimator(GetComponent<Animator>());
        behaviourState = NPCstate.WALK;
        patrolIdx = 0;
        state.Animator.applyRootMotion = entity.isOwner;
        target = null;
        path = new List<Vector3>();
        curCover = null;
        state.OnInspect = InspectUI;
        state.OnShootMode = ShootUI;
        state.OnResetUI = ResetUI;
        state.OnDeath = ResetUI;
        if (midPatrol != Vector3.zero && endPatrol != Vector3.zero)
        {
            patrolWaypoints = Astar.CreateAndMakePath(transform.position, midPatrol);
            patrolWaypoints.AddRange(Astar.CreateAndMakePath(midPatrol, endPatrol));
            patrolWaypoints.AddRange(Astar.CreateAndMakePath(endPatrol, transform.position));
        }
    }

    private bool anyNPCNearby()
    {
        Vector3 lift = transform.position;

        lift.y = 1;
        foreach (NPCStateMachine tmp in GameObject.FindObjectsOfType<NPCStateMachine>())
        {
            GameObject npc = tmp.gameObject;
            if (npc == gameObject)
                continue;
            if (Vector3.Distance(transform.position, npc.transform.position) < 2 &&
                Physics.Raycast(lift, npc.transform.position - transform.position, out RaycastHit hitInfo))
            {
                if (hitInfo.collider.gameObject.tag == "NPC")
                {
                    target = npc;
                    return (true);
                }
            }
        }
        return (false);
    }

    public void isShot(GameObject shooter, float damage)
    {
        if (behaviourState == NPCstate.DEAD)
            return ;
        lastShotReceived = Time.time;
        health -= damage;
        if (health <= 0)
        {
            behaviourState = NPCstate.DEAD;
            _shootUIFeedback?.gameObject?.SetActive(false);
            _inspectUIFeedback?.gameObject?.SetActive(false);
            state.Death();
            characterController.enabled = false;
            if (transform.name == "Boss")
            {
                FindObjectOfType<GameManager>().SetCurrentInstructionIdx(3);
            }
        }
        else
        {
            target = shooter;
            behaviourState = NPCstate.SHOOT;
            path.Clear();
            alertLevel = 1;
        }
    }

    private GameObject checkForPlayers()
    {
        foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (Vector3.Angle(player.transform.position - transform.position, transform.forward) < 60)
            {
                Vector3 lift = transform.position;
                lift.y += 1.5f;
                if (Physics.Raycast(lift, player.transform.position - transform.position, out RaycastHit hitInfo) &&
                    hitInfo.collider.gameObject.tag == "Player")
                {
                    return (hitInfo.collider.gameObject);
                }
            }
        }
        return (null);
    }

    private bool playerOnSight()
    {
        GameObject gm = checkForPlayers();
        if (gm != null)
        {
            targetPosition = gm.transform.position;
            targetPosition.y = 0;
            target = gm;
            behaviourState = NPCstate.GOTO;
            state.Inspect();
            return (true);
        }
        return (false);
    }

    private void AlertEnemies(int distance = 15)
    {
        foreach (NPCStateMachine tmp in GameObject.FindObjectsOfType<NPCStateMachine>())
        {
            if (tmp == this)
                continue;
            if ((tmp.behaviourState == NPCstate.WALK || tmp.behaviourState == NPCstate.TALK || tmp.behaviourState == NPCstate.STOP) &&
                Astar.GetLengthOfPath(target.transform.position, tmp.transform.position) < distance)
            {
                if (target?.transform?.position != null)
                {
                    tmp.HeardSomething(target.transform.position);
                }
            }
        }
    }

    private bool computeAlert()
    {
        GameObject gm = checkForPlayers();
        if (gm != null)
        {
            float dist = Vector3.Distance(transform.position, gm.transform.position);
            alertLevel += 60 * BoltNetwork.FrameDeltaTime / (dist * dist);
            if (alertLevel >= 1)
            {
                alertLevel = 1;
                target = gm;
                behaviourState = NPCstate.SHOOT;
                state.ShootMode();
                path.Clear();
                return (true);
            }
        }
        return (false);
    }

    public override void SimulateOwner()
    {
        if (transform.name == "Boss")
            return;
        //To walk: MoveZ == 0.5f, to run: MoveZ == 1, 
        //to go left: MoveX < 0 (-0.5f or -1), to go right: MoveZ > 0 (0.5f or 1)
        state.MoveX = 0;
        state.MoveZ = 0;
        state.Crouch = false;
        state.Aim = false;
        switch (behaviourState)
        {
            case NPCstate.WALK:
                //Arrive to next patrol waypoint
                //If close, increment index
                //If on "stop" waypoint, trigger stop state
                if (playerOnSight() || patrolWaypoints.Count == 0)
                    break;
                if (SteeringArrive(patrolWaypoints[patrolIdx], false))
                {
                    if (patrolWaypoints[patrolIdx] == midPatrol || patrolWaypoints[patrolIdx] == endPatrol)
                    {
                        behaviourState = NPCstate.STOP;
                        transitionTime = Time.time + 10;
                    }
                    patrolIdx = (patrolIdx + 1) % patrolWaypoints.Count;
                }
                if (anyNPCNearby() && transitionTime <= Time.time)
                {
                    behaviourState = NPCstate.TALK;
                    transitionTime = Time.time + 10;
                }
                break;
            case NPCstate.STOP:
                //Check timer to go back walking
                if (transitionTime <= Time.time)
                    behaviourState = NPCstate.WALK;
                playerOnSight();
                break;
            case NPCstate.TALK:
                //Face target, check timer
                if (target != null)
                    transform.LookAt(target.transform);
                if (transitionTime <= Time.time)
                {
                    behaviourState = NPCstate.WALK;
                    target = null;
                    transitionTime += 5;
                }
                playerOnSight();
                break;
            case NPCstate.GOTO:
                if (Vector3.Distance(targetPosition, transform.position) > 2)
                    playerOnSight();
                if (path == null || path.Count == 0)
                {
                    path = Astar.CreateAndMakePath(transform.position, targetPosition);
                    if (path != null)
                        path.Add(targetPosition);
                }
                if (path != null && SteeringArrive(path[0], false))
                    path.RemoveAt(0);
                if (Vector3.Distance(targetPosition, transform.position) < 1f || path.Count == 0)
                {
                    behaviourState = NPCstate.INSPECT;
                    path.Clear();
                }
                computeAlert();
                break;
            case NPCstate.INSPECT:
                //Wander close to target
                if (target != null)
                    transform.LookAt(target.transform);
                else
                {
                    if (Vector3.Distance(targetPosition, transform.position) < 1f)
                        targetPosition = randomAccessiblePoint();
                    SteeringArrive(targetPosition, false);
                }
                if (computeAlert())
                    break;
                transitionTime += alertLevel * BoltNetwork.FrameDeltaTime / 2;
                if (alertLevel > 0)
                    alertLevel -= BoltNetwork.FrameDeltaTime / 50;
                if (transitionTime <= Time.time)
                {
                    behaviourState = NPCstate.WALK;
                    state.ResetUI();
                    target = null;
                    targetPosition = Vector3.zero;
                }
                break;
            case NPCstate.SHOOT:
                AlertEnemies();
                _shootUIFeedback?.SetActive(true);
                if (target != null)
                {
                    transform.LookAt(target.transform);
                    if (!AimAtPlayer())
                    {
                        behaviourState = NPCstate.GOTO;
                        state.Inspect();
                        targetPosition = target.transform.position;
                    }
                }
                if (health < 30 && (lastShotReceived + (health / 10) > Time.time))
                    behaviourState = NPCstate.COVER;
                break;
            case NPCstate.COVER:
                //If cover spot valid (covered from target, away from other NPCs), crouch
                //Else, go to nearest cover spot
                if (curCover == null || curCover.coverValue < 0.1f)
                {
                    if (curCover != null)
                        curCover.used = false;
                    curCover = TacticalGraph.GetClosestAndBestCoverPoint(transform.position);
                    if (curCover != null)
                        curCover.used = true;
                }
                if (curCover != null && Vector3.Distance(transform.position, curCover.pos) > stoppingRadius)
                {
                    if (path == null || path.Count == 0)
                        path = Astar.CreateAndMakePath(transform.position, curCover.pos, true);
                    if (path != null && SteeringArrive(path[0], true))
                    {
                        path.RemoveAt(0);
                        state.Crouch = true;
                        health += BoltNetwork.FrameDeltaTime;
                    }
                }
                else
                {
                    state.Crouch = true;
                    health += BoltNetwork.FrameDeltaTime;
                }
                if (health > 80 || 
                    (lastShotReceived + 1 < Time.time &&
                    Vector3.Distance(transform.position, target.transform.position) < 2))
                {
                    behaviourState = NPCstate.SHOOT;
                    state.Crouch = false;
                    if (curCover != null)
                        curCover.used = false;
                }
                break;
        }
    }

    Vector3 randomAccessiblePoint()
    {
        switch (Random.Range(0, 4))
        {
            case 0:
                if (!Physics.Raycast(transform.position, transform.forward, 2))
                    return (transform.position + (transform.forward * 2));
                break;
            case 1:
                if (!Physics.Raycast(transform.position, transform.right, 2))
                    return (transform.position + (transform.right * 2));
                break;
            case 2:
                if (!Physics.Raycast(transform.position, transform.forward * -1, 2))
                    return (transform.position + (transform.forward * -2));
                break;
            case 3:
                if (!Physics.Raycast(transform.position, transform.right * -1, 2))
                    return (transform.position + (transform.right * -2));
                break;
        }
        return Vector3.zero;
    }

    /// <summary>
    /// Performs a steering movement through acceleration on the character and decelerates then stops when nearing destination
    /// </summary>
    bool SteeringArrive(Vector3 target, bool isRunning)
    {
        Vector3 dir = target - transform.position;
        dir = new Vector3(dir.x, 0, dir.z);

        float targetSpeed = maxMag * dir.magnitude;
        if (dir.magnitude < stoppingRadius)
        {
            return true;
        }
        else if (dir.magnitude < slowingRadius)
        {
            targetSpeed = maxMag * dir.magnitude / slowingRadius;
            transform.LookAt(target);
        }
        Vector3 acceleration = targetSpeed * dir.normalized - velocity;
        if (acceleration.magnitude > (isRunning ? maxAcc * 1.3f : maxAcc))
            acceleration = acceleration.normalized * (isRunning ? maxAcc * 1.3f : maxAcc);
        velocity = velocity + acceleration * BoltNetwork.FrameDeltaTime;
        if (velocity.magnitude > (isRunning ? maxMag * 1.3f : maxMag))
            velocity = velocity.normalized * (isRunning ? maxMag * 1.3f : maxMag);
        TurnTowardsTarget(target);
        characterController.Move(velocity * BoltNetwork.FrameDeltaTime);
        state.MoveZ = isRunning ? 1 : 0.5f;
        return (false);
    }

    /// <summary>
    /// Lerps the rotation towards the current velocity (where it is going)
    /// </summary>
    void TurnTowardsTarget(Vector3 target)
    {
        if (Vector3.Distance(target, transform.position) == 0)  //in case they are on top of each other
            return;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(velocity), 180);
    }

    private void OnTriggerEnter(Collider other)
    {
        DoorManager door = other.GetComponent<DoorManager>();
        if (door != null && entity.isAttached && !door._isOpened)
        {
            other.GetComponent<DoorManager>().TriggerDoor();
        }
    }

    bool AimAtPlayer()
    {
        Vector3 lift = transform.position;
        LayerMask mask = 1 << 0;

        lift.y = 1;
        if (!Physics.Raycast(lift, target.transform.position - lift, Vector3.Distance(lift, target.transform.position), mask))
        {
            state.Aim = true;
            IWeapon w = GetComponent<NPCWeapon>().Weapon;
            if (lastShotFired <= BoltNetwork.Time - w.CooldownTime())
            {
                w.Shoot();
                state.Shoot();
                lastShotFired = BoltNetwork.Time;
                target.GetComponent<PlayerMovement>().TakeDamage(10);
            }
            return (true);
        }
        return (false);
    }

    private void InspectUI()
    {
        _shootUIFeedback?.SetActive(false);
        _inspectUIFeedback?.SetActive(true);
    }

    private void ShootUI()
    {
        _inspectUIFeedback?.SetActive(false);
        _shootUIFeedback?.SetActive(true);
    }

    private void ResetUI()
    {
        _inspectUIFeedback?.SetActive(false);
        _shootUIFeedback?.SetActive(false);
    }

    public void HeardSomething(Vector3 position)
    {
        targetPosition = position;
        behaviourState = NPCstate.GOTO;
        alertLevel += 0.1f;
        InspectUI();
    }

    public static void MakeNPCsInRangeInspect(Vector3 position, float distance)
    {
        foreach (NPCStateMachine npc in FindObjectsOfType<NPCStateMachine>())
        {
            if ((npc.behaviourState == NPCstate.WALK || npc.behaviourState == NPCstate.TALK || npc.behaviourState == NPCstate.STOP
                || npc.behaviourState == NPCstate.GOTO || npc.behaviourState == NPCstate.INSPECT)
                    && Astar.GetLengthOfPath(position, npc.transform.position) < distance)
            {
                npc.HeardSomething(position);
            }
        }
    }
}
