using UnityEngine;
using System.Collections;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Collections.Generic;

public enum ActionType { Discrete, Continuous };
public enum  TaskType
{
    Reaching,
    Scoring,
    Defending,
    FullGame
}
public enum TrainingPart
{
    Training,
    Playing,
    Heuristic
}

public enum AgentResetState
{
    centered,
    random
}

public class AirHockeyAgent : Agent
{
    public ActionType actionType;
    //public TrainingPart trainingPart;
    public float maxMovementSpeed;
    private Boundary agentBoundary;
    private Boundary humanBoudary;

    private Rigidbody2D agentRB;
    private Rigidbody2D handRB;
    private Rigidbody2D puckRB;
    private PuckScript puck;
    private HumanPlayer humanPlayer;

    private Vector2 startingPosition;
    private Vector2 lastDirection;
    private Vector2 position;
    private ResetPuckState resetPuckState;
    private AgentResetState agentResetState;
    private TaskType taskType = TaskType.Defending;
    private float V_max_puck = 25;
    private float V_max_human = 0.3f;
     private float humanGoalReward = 0f;
    private float agentGoalReward = 1f;
    private float avoidBoundaries = 0.00f;
    private float avoidDirectionChanges = 0.00f;
    private float encouragePuckMovement = 0f;
    private float encouragePuckContact = 0.5f;
    private float negStepReward = -0.001f;
    private float negMaxStepReward = -1f;
    private float behindPuckReward = 0.001f;
    private float defenceReward = 1f;
    private float backwallReward = 1f;
    public bool puckStopPenalty;
    public bool backWallReward;
    public bool deflectOnly;
    public GameObject puckGameObject;
    public GameObject humanPlayerGameObject;
    public GameObject agentBoundaryHolderObject;
    public GameObject humanBoundaryHolderObject;

    public Dictionary<string, float> episodeReward;

    // Start is called before the first frame update
    void Start()
    {
        agentRB = GetComponent<Rigidbody2D>();

        //var puckGameObject = GameObject.Find("Puck");
        puck = puckGameObject.GetComponent<PuckScript>();
        puckRB = puckGameObject.GetComponent<Rigidbody2D>();


        //var humanPlayerGameObject = GameObject.Find("HumanPlayer");
        //humanPlayer = humanPlayerGameObject.GetComponent<HumanPlayer>();
        humanPlayer = humanPlayerGameObject.GetComponent<HumanPlayer>();
        handRB = humanPlayerGameObject.GetComponent<Rigidbody2D>();

        var agentBoundaryHolder = agentBoundaryHolderObject.GetComponent<Transform>();
        agentBoundary = new Boundary(agentBoundaryHolder.GetChild(0).position.y,
                      agentBoundaryHolder.GetChild(1).position.y,
                      agentBoundaryHolder.GetChild(2).position.x,
                      agentBoundaryHolder.GetChild(3).position.x);

        var humanBoundaryHolder = humanBoundaryHolderObject.GetComponent<Transform>();
        humanBoudary = new Boundary(humanBoundaryHolder.GetChild(0).position.y,
                      humanBoundaryHolder.GetChild(1).position.y,
                      humanBoundaryHolder.GetChild(2).position.x,
                      humanBoundaryHolder.GetChild(3).position.x);

        startingPosition = agentRB.position;

        switch(taskType){
                case TaskType.Reaching:
                resetPuckState = ResetPuckState.randomPositionAgentSide;
                V_max_human = 0f;
                agentResetState = AgentResetState.random;
                break;

                case TaskType.Defending:
                resetPuckState = ResetPuckState.shotOnGoal;
                V_max_human = 0f;
                agentResetState = AgentResetState.random;
                break;

                case TaskType.FullGame:
                resetPuckState = ResetPuckState.randomPositionGlobal;
                agentResetState = AgentResetState.random;
                break;

                case TaskType.Scoring:
                resetPuckState = ResetPuckState.randomPositionAgentSide;
                V_max_human = 0f;
                agentResetState = AgentResetState.random;
                break;
            }
        puck.MaxSpeed = V_max_puck;
        humanPlayer.maxMovementSpeed = V_max_human;
    }

    public override void OnEpisodeBegin()
    {
        while (true)
        {
            puck.Reset(resetPuckState, agentBoundary);
            agentRB.velocity = Vector2.zero;

            if (agentResetState == AgentResetState.random)
            {
                agentRB.position = new Vector2(Random.Range(agentBoundary.Left, agentBoundary.Right) * 0.8f, Random.Range(agentBoundary.Down, agentBoundary.Up) * 0.8f);
                handRB.position = new Vector2(Random.Range(humanBoudary.Left, humanBoudary.Right) * 0.8f, Random.Range(humanBoudary.Down, humanBoudary.Up) * 0.8f);

            }
            else if (agentResetState == AgentResetState.centered)
            {
                agentRB.position = new Vector2((agentBoundary.Left + agentBoundary.Right) * 0.5f, (agentBoundary.Down + agentBoundary.Up) * 0.5f);
            }
            else
            {
                agentRB.position = new Vector2(humanBoudary.Left + 0.2f, humanBoudary.Down + 0.2f);
            }
            // Player Position Reset
          //  humanPlayer.ResetPosition();

            if (Mathf.Abs(puck.PuckRB.position.y - agentRB.position.y) >= 1.0 || Mathf.Abs(puck.PuckRB.position.x - agentRB.position.x) >= 1.0)
            {
                break;
            }
        }
        episodeReward = new Dictionary<string, float>();
        episodeReward["StepReward"] = 0f;
        episodeReward["DirectionReward"] = 0f;
        episodeReward["BoundaryReward"] = 0f;
        episodeReward["PuckVelocityReward"] = 0f;
        episodeReward["ContactReward"] = 0f;
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
            if (Input.GetKey(KeyCode.Q)){       
                discreteActionsOut[0] = 1 ;
            }
            else if (Input.GetKey(KeyCode.W)){
                discreteActionsOut[0] = 2 ;
            }
            else if (Input.GetKey(KeyCode.E)){
                discreteActionsOut[0] = 3 ;
            }
            else if (Input.GetKey(KeyCode.A)){
                discreteActionsOut[0] = 4 ;
            } 
            else if (Input.GetKey(KeyCode.D)){
                discreteActionsOut[0] = 5 ;
            }
            else if (Input.GetKey(KeyCode.Y)){
                discreteActionsOut[0] = 6 ;
            }
            else if (Input.GetKey(KeyCode.X)){
                discreteActionsOut[0] = 7 ;
            }
            else if (Input.GetKey(KeyCode.C)){
                discreteActionsOut[0] = 8 ;
            }
        }// 0: nix, 1:left_ up, 2:up, 3:right_up, 4:left, 5:right, 6:left_down, 7:down, 8:right_down 
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
                SetReward(encouragePuckContact);
                EndEpisode();
                return;
            }
            else if (StepCount == MaxStep)
            {
                SetReward(1f - Vector2.Distance(agentRB.position, puck.PuckRB.position)*0.2f);
                EndEpisode();
                return;
            }
            SetReward(negStepReward);
        }
        else if(taskType == TaskType.Defending)
        {
            if (puck.playState == PlayState.agentScored)
            {
                //SetReward(agentGoalReward);
                SetReward(0);//no extra, its about blocking
                EndEpisode();
                return;
            }
            else if (puck.playState == PlayState.playerScored)
            {
                SetReward(humanGoalReward);
                EndEpisode();
                return;
            }
           
            else if (StepCount == MaxStep)
            {
                SetReward(negMaxStepReward);
                EndEpisode();
                return;
            }
            else if (puckRB.velocity.y < 0f)
            {
                SetReward(defenceReward);
                EndEpisode();
            }
            SetReward(negStepReward); 
        }
        else if(taskType == TaskType.Scoring)
        {
            if (puck.playState == PlayState.agentScored)
            {
                SetReward(agentGoalReward);
                EndEpisode();
                return;
            }
            else if (puck.playState == PlayState.playerScored)
            {
                SetReward(humanGoalReward);
                EndEpisode();
                return;
            }
            else if (StepCount == MaxStep)
            {
                EndEpisode();
                return;
            }
            SetReward(negStepReward);
        }
        else if(taskType == TaskType.FullGame)                       //full game
        {

            if (puck.playState == PlayState.agentScored)
            {
                SetReward(agentGoalReward);
                EndEpisode();
                if (false){
                Debug.Log("score. 1");
                Debug.Log(episodeReward["StepReward"]);
                Debug.Log(episodeReward["DirectionReward"]);
                Debug.Log(episodeReward["BoundaryReward"]);
                Debug.Log(episodeReward["PuckVelocityReward"]);
                Debug.Log(episodeReward["ContactReward"]);
                return;
                }

                return;
            }
            else if (puck.playState == PlayState.playerScored)
            {
                SetReward(humanGoalReward);
                EndEpisode();
                if (false){
                Debug.Log("score. -0.4");
                Debug.Log(episodeReward["StepReward"]);
                Debug.Log(episodeReward["DirectionReward"]);
                Debug.Log(episodeReward["BoundaryReward"]);
                Debug.Log(episodeReward["PuckVelocityReward"]);
                Debug.Log(episodeReward["ContactReward"]);
                return;
                }
            }
            if (puck.AgentContact)
            {
                episodeReward["ContactReward"] += encouragePuckContact;
                SetReward(encouragePuckContact);

            }
            if (puckRB.position.x < agentBoundary.Left-0.2 || puckRB.position.x > agentBoundary.Right+0.2 || puckRB.position.y > agentBoundary.Up+1 || puckRB.position.y < humanBoudary.Down -0.2)
            {
                EndEpisode();
            }
            SetReward(negStepReward); // Negative Step Reward

            if (puckRB.position.y > agentRB.position.x){
                SetReward(behindPuckReward);
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
                discretedirection =new Vector2(-1, 1);
                break;
                case 2:
                discretedirection =new Vector2(0, 1);
                break;
                case 3:
                discretedirection =new Vector2(1, 1);
                break;
                case 4:
                discretedirection =new Vector2(-1, 0);
                break;
                case 5:
                discretedirection =new Vector2(1, 0);
                break;
                case 6:
                discretedirection =new Vector2(-1, -1);
                break;
                case 7:
                discretedirection =new Vector2(0, -1);
                break;
                case 8:
                discretedirection =new Vector2(1, -1);
                break;
                
            }
        }
        if(direction.magnitude > 1f)
        {
            direction.Normalize();
        }

        
        if(avoidDirectionChanges > 0f) // Punish changing direction too much.
        {
            SetReward(-((lastDirection - direction).magnitude) * avoidDirectionChanges);
            episodeReward["DirectionReward"] -= (lastDirection - direction).magnitude * avoidDirectionChanges;
        }
        lastDirection = direction;

        if (avoidBoundaries > 0f) // Punish running into boundaries.
        {
            if (agentRB.position.x < agentBoundary.Left || agentRB.position.x > agentBoundary.Right || agentRB.position.y > agentBoundary.Up || agentRB.position.y < agentBoundary.Down)
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
        if (actionType == ActionType.Discrete){
            direction = discretedirection;
            }
        // Movement
        position = new Vector2(Mathf.Clamp(agentRB.position.x, agentBoundary.Left,
                            agentBoundary.Right),
                            Mathf.Clamp(agentRB.position.y, agentBoundary.Down,
                            agentBoundary.Up));
        
        agentRB.MovePosition(position + direction * maxMovementSpeed * Time.fixedDeltaTime);
    }
}
