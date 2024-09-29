using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;

/// <summary>
/// A hummingbird Machine Learning Agent.
/// </summary>
public class HummingbirdAgent : Agent
{
    [Tooltip("Force to apply when moving.")]
    public float moveForce = 2f;

    [Tooltip("Speed to pitch up or down.")]
    public float pitchSpeed = 100f;

    [Tooltip("Speed to rotate around the up axis.")]
    public float yawSpeed = 100f;

    [Tooltip("Transform at the tip of the beak.")]
    public Transform beakTip;

    [Tooltip("The agent's camera.")]
    public Camera agentCamera;

    [Tooltip("Wether this is training mode or gameplay mode.")]
    public bool trainingMode;

    new private Rigidbody rigidbody;

    private FlowerArea flowerArea;

    private Flower nearestFlower;

    private float smoothPitchChange = 0f;

    private float smoothYawChange = 0f;

    private const float MaxPitchAngle = 80f;

    private const float BeakTipRadius = 0.008f;

    private bool frozen = false;

    public float NectarObtained { get; private set; }

    public override void Initialize()
    {
        rigidbody = GetComponent<Rigidbody>();
        flowerArea = GetComponentInParent<FlowerArea>();

        // If not training mode, no max ste, play forever.
        if (!trainingMode) MaxStep = 0;
    }

    public override void OnEpisodeBegin()
    {
        if (trainingMode)
        {
            // Only reset flowers in training when there is one agent per area.
            flowerArea.ResetFlowers();
        }

        NectarObtained = 0f;

        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;

        bool inFrontOfFlower = true;
        if (trainingMode)
        {
            inFrontOfFlower = Random.value > .5f;
        }

        MoveToSafeRandomPosition(inFrontOfFlower);

        UpdateNearestFlower();
    }

    /// <summary>
    /// Called when an action is recieved from either the player input or the nerual network.
    /// 
    /// vectorAction[i] represents:
    /// Index0: move vector x (+1 = right, -1 = left)
    /// Index1: move vector y (+1 = up, -1 = down)
    /// Index2: move vector z (+1 = forward, -1 = backward)
    /// Index3: pitch angle (+1 = pitch up, -1 pitch down)
    /// Index4: yaw angle (+1 = turn right, -1 = turn left)
    /// </summary>
    /// <param name="vectorAction">The actions to take</param>
    public override void OnActionReceived(float[] vectorAction)
    {
        if (frozen) return;

        Vector3 move = new Vector3(vectorAction[0], vectorAction[1], vectorAction[2]);

        rigidbody.AddForce(move * moveForce);

        Vector3 rotationVector = transform.rotation.eulerAngles;

        float pitchChange = vectorAction[3];
        float yawChange = vectorAction[4];

        smoothPitchChange = Mathf.MoveTowards(smoothPitchChange, pitchChange, 2f * Time.fixedDeltaTime);
        smoothYawChange = Mathf.MoveTowards(smoothYawChange, yawChange, 2f * Time.fixedDeltaTime);

        float pitch = rotationVector.x + smoothPitchChange * Time.fixedDeltaTime * pitchSpeed;
        if (pitch > 180f) pitch -= 360f;
        pitch = Mathf.Clamp(pitch, -MaxPitchAngle, MaxPitchAngle);

        float yaw = rotationVector.y + smoothYawChange * Time.fixedDeltaTime * yawSpeed;

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    /// <summary>
    /// Collect vector observations from the environmnent.
    /// </summary>
    /// <param name="sensor">The vector sensor.</param>
    public override void CollectObservations(VectorSensor sensor)
    {
        // If nearestFlower is null, observe an empty array and return early.
        if (nearestFlower == null)
        {
            sensor.AddObservation(new float[10]);
            return;
        }

        // Observe the agent's local rotation. (4 observations)
        sensor.AddObservation(transform.localRotation.normalized);

        // Get a vector from the beak tip to the nearest flower.
        Vector3 toFlower = nearestFlower.FlowerCenterPosition - beakTip.position;

        // Observe a normalized vector pointing to the nearest flower. (3 observations)
        sensor.AddObservation(toFlower.normalized);

        // Observe a dot product that indicates whether the beak tip is in front of the flower. (1 observation)
        // (+1 means that the beak tip is directly in front of the flower, -1 means directly behind)
        sensor.AddObservation(Vector3.Dot(toFlower.normalized, -nearestFlower.FlowerUpVector));

        // Observe a dot product that indicates whether the beak is pointing towards the flower. (1 observation)
        // (+1 means that the beak is pointing directly at the flower, -1 means directly away)
        sensor.AddObservation(Vector3.Dot(beakTip.forward, -nearestFlower.FlowerUpVector));

        // Observe the relative distance from the beak tip to the flower. (1 observation)
        sensor.AddObservation(toFlower.magnitude / FlowerArea.AreaDiameter);

        // 10 total observations
    }

    /// <summary>
    /// When Behavior Type is set to "Huristic Only" on the agent's Behavior Paramenters,
    /// this function will be called. Its return values will be fed into
    /// <see cref="OnActionReceived(float[])"/> instead of using the neural network.
    /// </summary>
    /// <param name="actionsOut">An output action array.</param>
    public override void Heuristic(float[] actionsOut)
    {
        Vector3 forward = Vector3.zero;
        Vector3 left = Vector3.zero;
        Vector3 up = Vector3.zero;
        float pitch = 0f;
        float yaw = 0f;

        if (Input.GetKey(KeyCode.W)) forward = transform.forward;
        else if (Input.GetKey(KeyCode.S)) forward = -transform.forward;

        if (Input.GetKey(KeyCode.A)) left = -transform.right;
        else if (Input.GetKey(KeyCode.D)) left = transform.right;

        if (Input.GetKey(KeyCode.E)) up = transform.up;
        else if (Input.GetKey(KeyCode.Q)) up = -transform.up;

        if (Input.GetKey(KeyCode.UpArrow)) pitch = 1f;
        else if (Input.GetKey(KeyCode.DownArrow)) pitch = -1f;

        if (Input.GetKey(KeyCode.LeftArrow)) yaw = -1f;
        else if (Input.GetKey(KeyCode.RightArrow)) yaw = 1f;

        Vector3 combined = (forward + left + up).normalized;

        actionsOut[0] = combined.x;
        actionsOut[1] = combined.y;
        actionsOut[2] = combined.z;
        actionsOut[3] = pitch;
        actionsOut[4] = yaw;
    }

    /// <summary>
    /// Prevent the agent from moving and taking actions.
    /// </summary>
    public void FreezeAgent()
    {
        Debug.Assert(trainingMode == false, "Freeze/Unfreeze not supported in training mode.");
        frozen = true;
        rigidbody.Sleep();
    }

    /// <summary>
    /// Allows the agent to move and take actions again.
    /// </summary>
    public void UnfreezeAgent()
    {
        Debug.Assert(trainingMode == false, "Freeze/Unfreeze not supported in training mode.");
        frozen = false;
        rigidbody.WakeUp();
    }

    private void MoveToSafeRandomPosition(bool inFrontOfFlower)
    {
        bool safePositionFound = false;
        int attemptsRemaining = 100;
        Vector3 potentialPosition = Vector3.zero;
        Quaternion potentialRotation = new Quaternion();

        while (!safePositionFound && attemptsRemaining > 0)
        {
            attemptsRemaining--;

            if (inFrontOfFlower)
            {
                Flower randomFlower = flowerArea.Flowers[Random.Range(0, flowerArea.Flowers.Count)];

                float distanceFromFlower = Random.Range(.1f, .2f);
                potentialPosition = randomFlower.transform.position + randomFlower.FlowerUpVector * distanceFromFlower;

                Vector3 toFlower = randomFlower.FlowerCenterPosition - potentialPosition;
                potentialRotation = Quaternion.LookRotation(toFlower, Vector3.up);
            }
            else
            {
                float height = Random.Range(1.2f, 2.5f);
                float radius = Random.Range(2f, 7f);
                Quaternion direction = Quaternion.Euler(0f, Random.Range(-180f, 180f), 0f);

                potentialPosition = flowerArea.transform.position + Vector3.up * height + direction * Vector3.forward * radius;

                float pitch = Random.Range(-60f, 60f);
                float yaw = Random.Range(-180f, 180f);
                potentialRotation = Quaternion.Euler(pitch, yaw, 0f);
            }

            Collider[] colliders = Physics.OverlapSphere(potentialPosition, 0.05f);

            safePositionFound = colliders.Length == 0;
        }

        Debug.Assert(safePositionFound, "Could not find a safe position to spawn.");

        transform.position = potentialPosition;
        transform.rotation = potentialRotation;
    }

    private void UpdateNearestFlower()
    {
        foreach (Flower flower in flowerArea.Flowers)
        {
            if (nearestFlower == null && flower.HasNectar)
            {
                nearestFlower = flower;
            }
            else if (flower.HasNectar)
            {
                float distanceToFlower = Vector3.Distance(flower.transform.position, beakTip.position);
                float distanceToCurrentNearestFlower = Vector3.Distance(nearestFlower.transform.position, beakTip.position);

                if (!nearestFlower.HasNectar || distanceToFlower < distanceToCurrentNearestFlower)
                {
                    nearestFlower = flower;
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TriggerEnterOrStay(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TriggerEnterOrStay(other);
    }

    private void TriggerEnterOrStay(Collider collider)
    {
        // Check if agent is colliding with nectar.
        if (collider.CompareTag("nectar"))
        {
            Vector3 closestPointToBeakTip = collider.ClosestPoint(beakTip.position);

            if (Vector3.Distance(beakTip.position, closestPointToBeakTip) < BeakTipRadius)
            {
                Flower flower = flowerArea.GetFlowerFromNectar(collider);

                float nectarRecieved = flower.Feed(.01f);

                NectarObtained += nectarRecieved;

                if (trainingMode)
                {
                    float bonus = .02f * Mathf.Clamp01(Vector3.Dot(transform.forward, -nearestFlower.FlowerUpVector));
                    AddReward(.01f + bonus);
                }

                if (!flower.HasNectar)
                {
                    UpdateNearestFlower();
                }
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (trainingMode && collision.collider.CompareTag("boundary"))
        {
            // Collided with the area boundary, give a negative reward.
            AddReward(-.5f);
        }
    }

    private void Update()
    {
        // Draw a line from the beak tip to the nearest flower.
        if (nearestFlower != null)
            Debug.DrawLine(beakTip.position, nearestFlower.FlowerCenterPosition, Color.green);
    }

    private void FixedUpdate()
    {
        // Avoids scenario where nearest flower nectar is stolen by opponent and not updated.
        if (nearestFlower != null && !nearestFlower.HasNectar)
            UpdateNearestFlower();
    }
}
