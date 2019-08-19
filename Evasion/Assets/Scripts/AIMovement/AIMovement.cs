using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIMovement : MonoBehaviour
{
    private float speed = 0;
    private Vector3 velocity;
    private List<Vector3> path = null;
    private bool moving = false;
    private Vector3 target;

    //all the hyper parameters the user can change to change the behaviours
    
    public float stoppingRadius = 0.1f;
    public float slowingRadius = 5f;
    public float maxMag = 7f;
    public float maxAcc = 10f;

    private Animator anim; //Save The animator

    public static AIMovement instance;//Singleton pattern

    private void Awake()
    {
        anim = GetComponent<Animator>();
        instance = this;
    }

    /// <summary>
    /// Performs a steering movement through acceleration on the character and decelerates then stops when nearing destination
    /// </summary>
    void SteeringArrive()
    {
        Vector3 dir = target - transform.position;
//        dir = new Vector3(dir.x, 0, dir.z);

        float targetSpeed = maxMag * dir.magnitude;
        if (dir.magnitude < stoppingRadius)
        {
            path.Remove(target);
            if (path.Count == 0)
                moving = false;
            else
                target = path[0];
            return;
        }
        else if (dir.magnitude < slowingRadius)
        {
            targetSpeed = maxMag * dir.magnitude / slowingRadius;
        }
        Vector3 acceleration = targetSpeed * dir.normalized - velocity;
        if (acceleration.magnitude > maxAcc)
            acceleration = acceleration.normalized * maxAcc;

        velocity = velocity + acceleration * Time.deltaTime;

        if (velocity.magnitude > maxMag)
            velocity = velocity.normalized * maxMag;

        transform.position += velocity * Time.deltaTime;   //changes the position
        speed = velocity.magnitude / maxMag;
        TurnTowardsTarget();
    }

    /// <summary>
    /// Lerps the rotation towards the current velocity (where it is going)
    /// </summary>
    void TurnTowardsTarget()
    {
        if (Vector3.Distance(target, transform.position) == 0)  //in case they are on top of each other
            return;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(velocity), 10);
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.A))
            SetPath(Astar.CreateAndMakePath(transform.position, new Vector3(26.72f, 0f, 12.07f)));
        if (moving)
            SteeringArrive();
        else
            speed = 0;
        anim.SetFloat("MoveSpeed", speed);
    }

    /// <summary>
    /// Sets the path to follow
    /// </summary>
    public void SetPath(List<Vector3> path)
    {
        if (path == null || path.Count == 0)
            return;
        this.path = path;
        target = path[0];
        moving = true;
    }
}
