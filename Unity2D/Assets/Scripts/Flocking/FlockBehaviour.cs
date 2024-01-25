using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using UnityEngine;
using UnityEngine.EventSystems;

[BurstCompile]
public class FlockBehaviour : MonoBehaviour
{
    List<Obstacle> mObstacles = new List<Obstacle>();

    [SerializeField]
    GameObject[] Obstacles;

    [SerializeField]
    BoxCollider2D Bounds;

    public float TickDuration = 1.0f;
    public float TickDurationSeparationEnemy = 0.1f;
    public float TickDurationRandom = 1.0f;

    public int BoidIncr = 100;
    public bool useFlocking = false;
    public int BatchSize = 100;

    public List<Flock> flocks = new List<Flock>();
    void Reset()
    {
        flocks = new List<Flock>()
    {
      new Flock()
    };
    }

    void Start()
    {
        // Randomize obstacles placement.
        for (int i = 0; i < Obstacles.Length; ++i)
        {
            float x = Random.Range(Bounds.bounds.min.x, Bounds.bounds.max.x);
            float y = Random.Range(Bounds.bounds.min.y, Bounds.bounds.max.y);
            Obstacles[i].transform.position = new Vector3(x, y, 0.0f);
            Obstacle obs = Obstacles[i].AddComponent<Obstacle>();
            Autonomous autono = Obstacles[i].AddComponent<Autonomous>();
            autono.MaxSpeed = 1.0f;
            obs.mCollider = Obstacles[i].GetComponent<CircleCollider2D>();
            mObstacles.Add(obs);
        }

        foreach (Flock flock in flocks)
        {
            CreateFlock(flock);
        }

        StartCoroutine(Coroutine_Flocking());

        StartCoroutine(Coroutine_Random());
        StartCoroutine(Coroutine_AvoidObstacles());
        StartCoroutine(Coroutine_SeparationWithEnemies());
        StartCoroutine(Coroutine_Random_Motion_Obstacles());
    }

    void CreateFlock(Flock flock)
    {
        for (int i = 0; i < flock.numBoids; ++i)
        {
            float x = Random.Range(Bounds.bounds.min.x, Bounds.bounds.max.x);
            float y = Random.Range(Bounds.bounds.min.y, Bounds.bounds.max.y);

            AddBoid(x, y, flock);
        }
    }

    void Update()
    {
        HandleInputs();
        Rule_CrossBorder();
        Rule_CrossBorder_Obstacles();
    }

    void HandleInputs()
    {
        if (EventSystem.current.IsPointerOverGameObject() ||
           enabled == false)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            AddBoids(BoidIncr);
        }
    }

    void AddBoids(int count)
    {
        for (int i = 0; i < count; ++i)
        {
            float x = Random.Range(Bounds.bounds.min.x, Bounds.bounds.max.x);
            float y = Random.Range(Bounds.bounds.min.y, Bounds.bounds.max.y);

            AddBoid(x, y, flocks[0]);
        }
        flocks[0].numBoids += count;
    }

    void AddBoid(float x, float y, Flock flock)
    {
        GameObject obj = Instantiate(flock.PrefabBoid);
        obj.name = "Boid_" + flock.name + "_" + flock.mAutonomous.Count;
        obj.transform.position = new Vector3(x, y, 0.0f);
        Autonomous boid = obj.GetComponent<Autonomous>();
        flock.mAutonomous.Add(boid);
        boid.MaxSpeed = flock.maxSpeed;
        boid.RotationSpeed = flock.maxRotationSpeed;
    }

    static float Distance(Autonomous a1, Autonomous a2)
    {
        return (a1.transform.position - a2.transform.position).magnitude;
    }
     
    //Optimization
    //Some looping in this function could be replaced with RayCasting 
    //Looping all the time can be expensive and using RayCast instead can be faster

    [BurstCompile]
    void Execute(Flock flock, int i)
    { 
        //Initializes respective values
        Vector3 flockDir = Vector3.zero;
        Vector3 separationDir = Vector3.zero; 
        Vector3 cohesionDir = Vector3.zero;

        float speed = 0.0f; 
        float separationSpeed = 0.0f; 

        int count = 0; 
        int separationCount = 0; 
        Vector3 steerPos = Vector3.zero; 

        Autonomous curr = flock.mAutonomous[i]; //Get the current boid
        Vector3 currPosition = curr.transform.position; //Gets its position

        //Loops through all boids in flock
        for (int j = 0; j < flock.numBoids; ++j)
        {
            if (i == j) continue; 

            Autonomous other = flock.mAutonomous[j]; //Get nearby boids
            Vector3 otherPosition = other.transform.position; //Get nearby boid positions

            float distSqr = (currPosition - otherPosition).sqrMagnitude; //Calculate squared distance between the current boid and other boids

            if (distSqr < flock.visibility * flock.visibility) //Check if other boids are within range of visibility
            { 
                //Check visibility with RayCasting which is more efficient in this case, rather than checking visibility by looping through all boids
                //Raycasting efficiently checks if a boid is visible from the current boid's position
                RaycastHit2D hit = Physics2D.Raycast(currPosition, otherPosition - currPosition, flock.visibility, 1 << 6);
                if (hit.collider != null && hit.collider.gameObject != curr.gameObject)
                {
                    continue;
                }

                //If nearby boid is visible: add settings for calculations
                speed += other.Speed;
                flockDir += other.TargetDirection;
                steerPos += otherPosition;
                count++;

                if (distSqr < flock.separationDistance * flock.separationDistance)
                {
                    //If nearby boids are within the separation dist: add to separation behaviors
                    Vector3 targetDirection = (currPosition - otherPosition).normalized;
                    separationDir += targetDirection;
                    separationSpeed += Mathf.Sqrt(distSqr) * flock.weightSeparation;
                    separationCount++;
                }
            }
        } 
         
        //Calculates respective values for behaviour computing
        if (count > 0)
        {
            speed /= count;
            flockDir /= count;
            flockDir.Normalize();
            steerPos /= count;
        }

        if (separationCount > 0)
        {
            separationSpeed /= separationCount;
            separationDir /= separationSpeed;
            separationDir.Normalize();
        }

        //Applies calculated values from earlier to provide combined behavior to current boid
        curr.TargetDirection =
            flockDir * speed * (flock.useAlignmentRule ? flock.weightAlignment : 0.0f) +
            separationDir * separationSpeed * (flock.useSeparationRule ? flock.weightSeparation : 0.0f) +
            (steerPos - currPosition) * (flock.useCohesionRule ? flock.weightCohesion : 0.0f);
    }

    [BurstCompile]
    IEnumerator Coroutine_Flocking()
    {
        while (true)
        {
            if (useFlocking)
            {
                foreach (Flock flock in flocks)
                {
                    List<Autonomous> autonomousList = flock.mAutonomous;
                    for (int i = 0; i < autonomousList.Count; ++i)
                    {
                        Execute(flock, i);
                        if (i % BatchSize == 0)
                        {
                            yield return null;
                        }
                    }
                    yield return null;
                }
            }
            yield return new WaitForSeconds(TickDuration);
        }
    }

    [BurstCompile]
    void SeparationWithEnemies_Internal(
    List<Autonomous> boids,
    float sepDist,
    float sepWeight)
    {
        foreach (Autonomous boid in boids)
        {
            //Instead of looping, I decided to use RayCasting 
            //Raycasting is more efficient than looping in this case 

            //I am using CircleCast which casts a circle from the boid's position with a defined separation distance 
            //It can identify nearby enemies without going through every object in the scene 

            RaycastHit2D[] hits = Physics2D.CircleCastAll(boid.transform.position, sepDist, Vector2.zero, 0f, 1 << 3);

            foreach (var hit in hits)
            {
                if (hit.collider != null)
                {
                    Autonomous enemy = hit.collider.GetComponent<Autonomous>();
                    if (enemy != null && enemy != boid)
                    {
                        float dist = (boid.transform.position - enemy.transform.position).magnitude;
                        if (dist < sepDist)
                        {
                            Vector3 targetDirection = (boid.transform.position - enemy.transform.position).normalized;

                            boid.TargetDirection += targetDirection;
                            boid.TargetDirection.Normalize();

                            boid.TargetSpeed += dist * sepWeight;
                            boid.TargetSpeed /= 2.0f;
                        }
                    }
                }
            }
        }
    }

    [BurstCompile]
    IEnumerator Coroutine_SeparationWithEnemies()
    {
        while (true)
        {
            foreach (Flock flock in flocks)
            {
                if (!flock.useFleeOnSightEnemyRule || flock.isPredator) continue;

                foreach (Flock enemies in flocks)
                {
                    if (!enemies.isPredator) continue;

                    SeparationWithEnemies_Internal(
                      flock.mAutonomous,
                      flock.enemySeparationDistance,
                      flock.weightFleeOnSightEnemy);
                }
                //yield return null;
            }
            yield return null;
        }
    }

    [BurstCompile]
    IEnumerator Coroutine_AvoidObstacles()
    {
        while (true)
        {
            foreach (Flock flock in flocks)
            {
                if (flock.useAvoidObstaclesRule)
                {
                    List<Autonomous> autonomousList = flock.mAutonomous;
                    for (int i = 0; i < autonomousList.Count; ++i)
                    {
                        for (int j = 0; j < mObstacles.Count; ++j)
                        {
                            float dist = (
                              mObstacles[j].transform.position -
                              autonomousList[i].transform.position).magnitude;
                            if (dist < mObstacles[j].AvoidanceRadius)
                            {
                                Vector3 targetDirection = (
                                  autonomousList[i].transform.position -
                                  mObstacles[j].transform.position).normalized;

                                autonomousList[i].TargetDirection += targetDirection * flock.weightAvoidObstacles;
                                autonomousList[i].TargetDirection.Normalize();
                            }
                        }
                    }
                }
                //yield return null;
            }
            yield return null;
        }
    }

    [BurstCompile]
    IEnumerator Coroutine_Random_Motion_Obstacles()
    {
        while (true)
        {
            for (int i = 0; i < Obstacles.Length; ++i)
            {
                Autonomous autono = Obstacles[i].GetComponent<Autonomous>();
                float rand = Random.Range(0.0f, 1.0f);
                autono.TargetDirection.Normalize();
                float angle = Mathf.Atan2(autono.TargetDirection.y, autono.TargetDirection.x);

                if (rand > 0.5f)
                {
                    angle += Mathf.Deg2Rad * 45.0f;
                }
                else
                {
                    angle -= Mathf.Deg2Rad * 45.0f;
                }
                Vector3 dir = Vector3.zero;
                dir.x = Mathf.Cos(angle);
                dir.y = Mathf.Sin(angle);

                autono.TargetDirection += dir * 0.1f;
                autono.TargetDirection.Normalize();
                //Debug.Log(autonomousList[i].TargetDirection);

                float speed = Random.Range(1.0f, autono.MaxSpeed);
                autono.TargetSpeed += speed;
                autono.TargetSpeed /= 2.0f;
            }
            yield return new WaitForSeconds(2.0f);
        }
    }

    [BurstCompile]
    IEnumerator Coroutine_Random()
    {
        while (true)
        {
            foreach (Flock flock in flocks)
            {
                if (flock.useRandomRule)
                {
                    List<Autonomous> autonomousList = flock.mAutonomous;
                    for (int i = 0; i < autonomousList.Count; ++i)
                    {
                        float rand = Random.Range(0.0f, 1.0f);
                        autonomousList[i].TargetDirection.Normalize();
                        float angle = Mathf.Atan2(autonomousList[i].TargetDirection.y, autonomousList[i].TargetDirection.x);

                        if (rand > 0.5f)
                        {
                            angle += Mathf.Deg2Rad * 45.0f;
                        }
                        else
                        {
                            angle -= Mathf.Deg2Rad * 45.0f;
                        }
                        Vector3 dir = Vector3.zero;
                        dir.x = Mathf.Cos(angle);
                        dir.y = Mathf.Sin(angle);

                        autonomousList[i].TargetDirection += dir * flock.weightRandom;
                        autonomousList[i].TargetDirection.Normalize();
                        //Debug.Log(autonomousList[i].TargetDirection);

                        float speed = Random.Range(1.0f, autonomousList[i].MaxSpeed);
                        autonomousList[i].TargetSpeed += speed * flock.weightSeparation;
                        autonomousList[i].TargetSpeed /= 2.0f;
                    }
                }
                //yield return null;
            }
            yield return new WaitForSeconds(TickDurationRandom);
        }
    }

    [BurstCompile]
    void Rule_CrossBorder_Obstacles()
    {
        for (int i = 0; i < Obstacles.Length; ++i)
        {
            Autonomous autono = Obstacles[i].GetComponent<Autonomous>();
            Vector3 pos = autono.transform.position;
            if (autono.transform.position.x > Bounds.bounds.max.x)
            {
                pos.x = Bounds.bounds.min.x;
            }
            if (autono.transform.position.x < Bounds.bounds.min.x)
            {
                pos.x = Bounds.bounds.max.x;
            }
            if (autono.transform.position.y > Bounds.bounds.max.y)
            {
                pos.y = Bounds.bounds.min.y;
            }
            if (autono.transform.position.y < Bounds.bounds.min.y)
            {
                pos.y = Bounds.bounds.max.y;
            }
            autono.transform.position = pos;
        }

        //for (int i = 0; i < Obstacles.Length; ++i)
        //{
        //  Autonomous autono = Obstacles[i].GetComponent<Autonomous>();
        //  Vector3 pos = autono.transform.position;
        //  if (autono.transform.position.x + 5.0f > Bounds.bounds.max.x)
        //  {
        //    autono.TargetDirection.x = -1.0f;
        //  }
        //  if (autono.transform.position.x - 5.0f < Bounds.bounds.min.x)
        //  {
        //    autono.TargetDirection.x = 1.0f;
        //  }
        //  if (autono.transform.position.y + 5.0f > Bounds.bounds.max.y)
        //  {
        //    autono.TargetDirection.y = -1.0f;
        //  }
        //  if (autono.transform.position.y - 5.0f < Bounds.bounds.min.y)
        //  {
        //    autono.TargetDirection.y = 1.0f;
        //  }
        //  autono.TargetDirection.Normalize();
        //}
    }

    [BurstCompile]
    void Rule_CrossBorder()
    {
        foreach (Flock flock in flocks)
        {
            List<Autonomous> autonomousList = flock.mAutonomous;
            if (flock.bounceWall)
            {
                for (int i = 0; i < autonomousList.Count; ++i)
                {
                    //Created currentPosition to cache autonomousList[i].transform.position 
                    //Doing this will reduce the number of extern calls in the function 
                    Vector3 currentPosition = autonomousList[i].transform.position;
                    Vector3 targetDirection = autonomousList[i].TargetDirection;

                    if (currentPosition.x + 5.0f > Bounds.bounds.max.x)
                    {
                        targetDirection.x = -1.0f;
                    }
                    if (currentPosition.x - 5.0f < Bounds.bounds.min.x)
                    {
                        targetDirection.x = 1.0f;
                    }
                    if (currentPosition.y + 5.0f > Bounds.bounds.max.y)
                    {
                        targetDirection.y = -1.0f;
                    }
                    if (currentPosition.y - 5.0f < Bounds.bounds.min.y)
                    {
                        targetDirection.y = 1.0f;
                    }
                    targetDirection.Normalize();
                    autonomousList[i].TargetDirection = targetDirection;
                }
            }
            else
            {
                for (int i = 0; i < autonomousList.Count; ++i)
                {
                    //Did the same here
                    Vector3 currentPosition = autonomousList[i].transform.position;
                    Vector3 newPosition = currentPosition;

                    if (currentPosition.x > Bounds.bounds.max.x)
                    {
                        newPosition.x = Bounds.bounds.min.x;
                    }
                    if (currentPosition.x < Bounds.bounds.min.x)
                    {
                        newPosition.x = Bounds.bounds.max.x;
                    }
                    if (currentPosition.y > Bounds.bounds.max.y)
                    {
                        newPosition.y = Bounds.bounds.min.y;
                    }
                    if (currentPosition.y < Bounds.bounds.min.y)
                    {
                        newPosition.y = Bounds.bounds.max.y;
                    }
                    autonomousList[i].transform.position = newPosition;
                }
            }
        }
    }
}

