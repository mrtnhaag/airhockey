using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;


public class Handagent : Agent
{
    public ActionType actionType;
    public float maxMovementSpeed;
    public bool randomHandPosition;
    
    private Boundary handBoundary;

    private Rigidbody2D handRB;
    private Rigidbody2D puckRB;
    private Rigidbody2D roboRB;
    private PuckScript puck;
    private AirHockeyAgent robo;
    private HumanPlayer humanPlayer;

    private Vector2 startingPosition;
    private Vector2 lastDirection;
    private Vector2 position;
    public TaskType taskType;
    public TrainingPart trainingPart;
    public ResetPuckState resetPuckState;

    public float avoidBoundaries;
    public float avoidDirectionChanges;
    public float encouragePuckMovement;
    public bool puckStopPenalty;
    public bool backWallReward;
    public bool deflectOnly;

    public Dictionary<string, float> episodeReward;
    void Start()
    {
        handRB = GetComponent<Rigidbody2D>();

        var puckGameObject = GameObject.Find("Puck");
        puck = puckGameObject.GetComponent<PuckScript>();
        puckRB = puckGameObject.GetComponent<Rigidbody2D>();

        var roboGameObject = GameObject.Find("Agent");
        robo = roboGameObject.GetComponent<AirHockeyAgent>();
        roboRB = roboGameObject.GetComponent<Rigidbody2D>();

       // var humanPlayerGameObject = GameObject.Find("Agent");
       // humanPlayer = humanPlayerGameObject.GetComponent<HumanPlayer>();

        var handBoundaryHolder = GameObject.Find("PlayerBoundaryHolder").GetComponent<Transform>();
        handBoundary = new Boundary(handBoundaryHolder.GetChild(0).localPosition.y,
                      handBoundaryHolder.GetChild(1).localPosition.y,
                      handBoundaryHolder.GetChild(2).localPosition.x,
                      handBoundaryHolder.GetChild(3).localPosition.x);

        //startingPosition = new Vector2(0, -2.3f);
        startingPosition = handRB.position;
    }

    public override void OnEpisodeBegin()
    {
        while (true)
        {
            puck.Reset(resetPuckState, handBoundary);
            handRB.velocity = Vector2.zero;

            if (randomHandPosition)
            {
                handRB.position = new Vector2(Random.Range(handBoundary.Left, handBoundary.Right) * 0.8f, Random.Range(handBoundary.Up, handBoundary.Down) * 0.8f);
            }
            else
            {
                handRB.position = startingPosition;
            }
            // Player Position Reset
          //  humanPlayer.ResetPosition();

            if (Mathf.Abs(puck.PuckRB.position.y - handRB.position.y) >= 1.0 || Mathf.Abs(puck.PuckRB.position.x - handRB.position.x) >= 1.0)
            {
                break;
            }
        }
        episodeReward = new Dictionary<string, float>();
        episodeReward["StepReward"] = 0f;
        episodeReward["DirectionReward"] = 0f;
        episodeReward["BoundaryReward"] = 0f;
        episodeReward["PuckVelocityReward"] = 0f;
        //Debug.Log("-------------------------------------");


    }

        public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(puck.transform.localPosition);
        sensor.AddObservation(puckRB.velocity);
    }

        public override void Heuristic(in ActionBuffers actionsOut)
    {
        
        if (actionType == ActionType.Discrete)
        {
            
            var discreteActionsOut = actionsOut.DiscreteActions;
            discreteActionsOut[0] = 0 ;
            if (Input.GetKey(KeyCode.A)){       
                discreteActionsOut[0] = 1 ;
            }
            else if (Input.GetKey(KeyCode.D)){
                discreteActionsOut[0] = 2 ;
            }
            else if (Input.GetKey(KeyCode.W)){
                discreteActionsOut[0] = 3 ;
            }
            else if (Input.GetKey(KeyCode.S)){
                discreteActionsOut[0] = 4 ;
            } 
        }// 0: nix, 1:left, 2:right, 3:up, 4:down
        else
        {
            var continuousActionsOut = actionsOut.ContinuousActions;
            // X - Axis
            if (Input.GetKey(KeyCode.Keypad4) || Input.GetKeyDown(KeyCode.Keypad4))
            {
                continuousActionsOut[0] = -1f;
            }
            else if (Input.GetKey(KeyCode.Keypad6) || Input.GetKeyDown(KeyCode.Keypad6))
            {
                continuousActionsOut[0] = 1f;
            }
            else
            {
                continuousActionsOut[0] = 0f;
            }

            // Y - Axis
            if (Input.GetKey(KeyCode.Keypad8) || Input.GetKeyDown(KeyCode.Keypad8))
            {
                continuousActionsOut[1] = 1f;
            }
            else if (Input.GetKey(KeyCode.Keypad2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                continuousActionsOut[1] = -1f;
            }
            else
            {
                continuousActionsOut[1] = 0f;
            }
        }
    }

     public override void OnActionReceived(ActionBuffers actionsIn) 
    {
        var continouosActions = actionsIn.ContinuousActions;
        var discreteActions = actionsIn.DiscreteActions;

        if(taskType == TaskType.Reaching)
        {
            if (puck.AgentContact)
            {
                SetReward(1f);
                EndEpisode();
                return;
            }
            else if (StepCount == MaxStep)
            {
                SetReward(1f - Vector2.Distance(handRB.position, puck.PuckRB.position));
                EndEpisode();
                return;
            }
            SetReward(-0.01f);
        }
        else if(taskType == TaskType.Defending)
        {
            if (puck.playState == PlayState.agentScored)
            {
                SetReward(1.5f);
                EndEpisode();
                return;
            }
            else if (puck.playState == PlayState.playerScored)
            {
                SetReward(-1f);
                EndEpisode();
                return;
            }
            else if (puck.playState == PlayState.backWallReached && backWallReward == true)
            {
                SetReward(1f);
                EndEpisode();
                return;
            }
            else if (puck.playState == PlayState.puckStopped && puckStopPenalty == true){
                SetReward(-.3f);
                EndEpisode();
                return;
            }
            else if(puck.transform.position.y < 0 && puck.AgentContact == true && deflectOnly == true)
            {
                //SetReward(1f);
                EndEpisode();
                return;
            }
            else if (StepCount == MaxStep)
            {
                SetReward(-1f);
                EndEpisode();
                return;
            }
            SetReward(-0.003f); // Negative Step Reward
        }
        else if(taskType == TaskType.Scoring)
        {
            if (puck.playState == PlayState.agentScored)
            {
                SetReward(1f);
                EndEpisode();
                return;
            }
            else if (puck.playState == PlayState.playerScored)
            {
                SetReward(-.1f);
                EndEpisode();
                return;
            }
            else if (StepCount == MaxStep)
            {
                EndEpisode();
                return;
            }
            SetReward(-0.001f);
        }
        else if(taskType == TaskType.FullGame)                                              //full game
        {
            if (puck.playState == PlayState.agentScored)
            {
                SetReward(1f);
                EndEpisode();
                return;
            }
            else if (puck.playState == PlayState.playerScored)
            {
                SetReward(-1f);
                EndEpisode();
                return;
            }
        }
        int dx = discreteActions[0]; 
        Vector2 direction = new Vector2(0, 0);

        if (actionType == ActionType.Continuous)
        {
            float x = continouosActions[0];
            float y = continouosActions[1];
            if(Mathf.Abs(x) < 0.03f)
            {
                x = 0f;
            }
            if (Mathf.Abs(y) < 0.03f)
            {
                y = 0f;
            }
            direction = new Vector2(x, y);
        }
        // normalize so going diagonally doesn't speed things up

        
        Vector2 discretedirection =new Vector2(0, 0);
        if (actionType == ActionType.Discrete)
        {

            switch(discreteActions[0]){
                case 0:
                discretedirection =new Vector2(0, 0);
                break;
                case 1:
                discretedirection =new Vector2(-1, 0);
                break;
                case 2:
                discretedirection =new Vector2(1, 0);
                break;
                case 3:
                discretedirection =new Vector2(0, 1);
                break;
                case 4:
                discretedirection =new Vector2(0, -1);
                break;
            }
        }

        
        if(direction.magnitude > 1f)
        {
            direction.Normalize();
        }

        
        if(avoidDirectionChanges > 0f) // Punish changing direction too much.
        {
            SetReward(-(lastDirection - direction).magnitude * avoidDirectionChanges);
            episodeReward["DirectionReward"] -= (lastDirection - direction).magnitude * avoidDirectionChanges;
        }
        lastDirection = direction;

        if (avoidBoundaries > 0f) // Punish running into boundaries.
        {
            if (handRB.position.x < handBoundary.Left || handRB.position.x > handBoundary.Right || handRB.position.y > handBoundary.Up || handRB.position.y < handBoundary.Down)
            {
                SetReward(-1f*avoidBoundaries);
                episodeReward["BoundaryReward"] -= 1f * avoidBoundaries;
            }
        }

        if (encouragePuckMovement > 0f) // Reward high puck velocities
        {
            SetReward(puckRB.velocity.magnitude * encouragePuckMovement);
            episodeReward["PuckVelocityReward"] += puckRB.velocity.magnitude * encouragePuckMovement;
        }

        if (actionType == ActionType.Discrete)
        {
            direction = discretedirection;
        }
        // Movement
        position = new Vector2(Mathf.Clamp(handRB.position.x, handBoundary.Left,
                            handBoundary.Right),
                            Mathf.Clamp(handRB.position.y, handBoundary.Down,
                            handBoundary.Up));
        handRB.MovePosition(position + direction * maxMovementSpeed * Time.fixedDeltaTime);
    }

//müllgrenze

/* 
    [SerializeField] private Transform targetTransform;

    public override void Heuristic(in ActionBuffers actionsOut){
        ActionSegment<float> continousActions = actionsOut.ContinuousActions;
        continousActions[0]=Input.GetAxisRaw("Horizontal");
        continousActions[1]=Input.GetAxisRaw("Vertical");
    }
    public override void OnEpisodeBegin(){
        transform.position = new Vector3(0, -3.3f ,0); 
    }
    public override void CollectObservations(VectorSensor sensor){
        sensor.AddObservation(transform.position);
        sensor.AddObservation(targetTransform.position);

    }
    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = actions.DiscreteActions[0];
        float moveY = actions.DiscreteActions[1];

        float moveSpeed = 10f;
        transform.position += new Vector3(moveX,moveY,0) * Time.deltaTime *moveSpeed;
        
    }
/*     private void OnTriggerEnter(Collider other) {
        
        if (other.TryGetComponent<Goal>(out Goal goal))
        SetReward(1f);
        EndEpisode();

    } */

} 
