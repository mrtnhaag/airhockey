using UnityEngine;
using System.Collections;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Collections.Generic;

public enum ActionType { Discrete, Continuous };

public class AirHockeyAgent : Agent
{
    public ActionType actionType;
    //public TrainingPart trainingPart;
    
    private Boundary agentBoundary;
    private Rigidbody2D agentRB;
    private Rigidbody2D handRB;
    private Rigidbody2D puckRB;
    private PuckScript puck;
    private envScript env;
    private HumanPlayer humanPlayer;
    private Vector2 startingPosition;
    private Vector2 lastDirection;
    private Vector2 position;
    private float currContactReward;
    private float V_max_puck = 25;
    private float maxMovementSpeed = 6f;
    private float V_max_human = 3f;
    private float neghumanGoalReward = 0f;
    private float agentGoalReward = 0f;
    private float avoidBoundaries = 0f;
    private float avoidDirectionChanges = 0f;
    private float stayCenteredReward = 0f;
    private float negoffCenterReward = -0.05f;
    private float encouragePuckMovement = 0f;
    private float encouragePuckContact = 0.8f;
    private bool contacthalf = false;
    private float playForwardReward = 2.5f;
    private float negplaybackReward = -0.50f;
    private float negStepReward = -0.01f;
    private float negMaxStepReward = 0f;
    private float behindPuckReward = 0f;
    private float defenceReward = 0f;
    private float backwallReward = 0f;
    public bool puckStopPenalty;
    public bool backWallReward;
    public bool deflectOnly;
    public GameObject puckGameObject;
    public GameObject humanPlayerGameObject;
    public GameObject envGameObject;
    public GameObject agentBoundaryHolderObject;
    public GameObject humanBoundaryHolderObject;

    public Dictionary<string, float> episodeReward;

    // Start is called before the first frame update
    void Start()
    {
        agentRB = GetComponent<Rigidbody2D>();
        Application.targetFrameRate = 100;

        env = envGameObject.GetComponent<envScript>();
        //var puckGameObject = GameObject.Find("Puck");
        puck = puckGameObject.GetComponent<PuckScript>();
        puckRB = puckGameObject.GetComponent<Rigidbody2D>();


        //var humanPlayerGameObject = GameObject.Find("HumanPlayer");
        //humanPlayer = humanPlayerGameObject.GetComponent<HumanPlayer>();
        handRB = humanPlayerGameObject.GetComponent<Rigidbody2D>();

        var agentBoundaryHolder = agentBoundaryHolderObject.GetComponent<Transform>();
        agentBoundary = new Boundary(agentBoundaryHolder.GetChild(0).position.y,
                      agentBoundaryHolder.GetChild(1).position.y,
                      agentBoundaryHolder.GetChild(2).position.x,
                      agentBoundaryHolder.GetChild(3).position.x);



        startingPosition = agentRB.position;
    }

    public override void OnEpisodeBegin()
    {
        currContactReward = encouragePuckContact;
        while (true)
        {
            puck.Reset(env.resetPuckState, agentBoundary);
            agentRB.velocity = Vector2.zero;

            if (env.roboResetState == envScript.RoboResetState.random)
            {
                agentRB.position = new Vector2(Random.Range(agentBoundary.Left, agentBoundary.Right) * 0.8f, Random.Range(agentBoundary.Down, agentBoundary.Up) * 0.8f);
            }
            else if (env.roboResetState == envScript.RoboResetState.centered)
            {
                agentRB.position = new Vector2((agentBoundary.Left + agentBoundary.Right) * 0.5f, (agentBoundary.Down + agentBoundary.Up) * 0.5f);
            }
            else if (env.roboResetState == envScript.RoboResetState.shotOnGoal)
            {
                agentRB.position = new Vector2(Random.Range(agentBoundary.Left, agentBoundary.Right) * 0.8f, Random.Range(agentBoundary.Down, agentBoundary.Up) * 0.8f);
            }
            else
            {
                agentRB.position = new Vector2(agentBoundary.Left + 0.2f, agentBoundary.Down + 0.2f);
            }

            

            if (Mathf.Abs(puck.PuckRB.position.y - agentRB.position.y) >= 1.0 || Mathf.Abs(puck.PuckRB.position.x - agentRB.position.x) >= 1.0)
            {
                break;
            }
        }

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
        if(env.taskType == envScript.TaskType.Reaching)
        {
            if (puck.AgentContact)
            {
                episodeReward["ContactReward"] += currContactReward;
                AddReward(currContactReward);
                if (contacthalf){
                    currContactReward = currContactReward * 0.5f;
                }
                if (puckRB.position.y < agentRB.position.x)
                {
                    AddReward(playForwardReward);
                }
                else
                {
                    AddReward(negplaybackReward);
                }
                EndEpisode();

            }
            else if (StepCount == MaxStep)
            {
                AddReward(negMaxStepReward);
                EndEpisode();
                return;
            }
            AddReward(negStepReward);
        }
        else if(env.taskType == envScript.TaskType.Defending)
        {
            if (puck.playState == PlayState.agentScored)
            {
                AddReward(agentGoalReward); //no extra, its about blocking
                EndEpisode();
                return;
            }
            else if (puck.playState == PlayState.playerScored)
            {
                AddReward(neghumanGoalReward); // no penalty, just blocking
                EndEpisode();
                return;
            }
           
            else if (StepCount == MaxStep)
            {
                AddReward(negMaxStepReward);
                EndEpisode();
                return;
            }
            else if (puckRB.velocity.y < 0f)
            {
                AddReward(defenceReward);
                EndEpisode();
            }
            AddReward(negStepReward); 
        }
        else if(env.taskType == envScript.TaskType.Scoring)
        {
            if (puck.playState == PlayState.agentScored)
            {
                AddReward(agentGoalReward);
                EndEpisode();
                return;
            }
            else if (puck.playState == PlayState.playerScored)
            {
                AddReward(neghumanGoalReward);
                EndEpisode();
                return;
            }
            else if (StepCount == MaxStep)
            {
                EndEpisode();
                return;
            }
            AddReward(negStepReward);
        }
        else if(env.taskType == envScript.TaskType.FullGame)                       //full game
        {
            if (puck.playState == PlayState.agentScored)
            {
                AddReward(agentGoalReward);
                EndEpisode();
                return;
            }
            else if (puck.playState == PlayState.playerScored)
            {
                AddReward(neghumanGoalReward);
                EndEpisode();
            }
            if (puck.AgentContact)
            {
                AddReward(currContactReward);
                if (contacthalf){
                    currContactReward = currContactReward * 0.5f;
                }
                if (puckRB.position.y < agentRB.position.x)
                {
                    AddReward(playForwardReward);
                }
                else
                {
                    AddReward(negplaybackReward);
                }

            }
            if (puckRB.position.x < agentBoundary.Left-0.2 || puckRB.position.x > agentBoundary.Right+0.2 || puckRB.position.y > agentBoundary.Up+1 || puckRB.position.y < agentBoundary.Down - 5f)
            {
                EndEpisode();
            }
            AddReward(negStepReward); // Negative Step Reward

            if (puckRB.position.y < agentRB.position.x){
                AddReward(behindPuckReward);                
            }

            AddReward((2f-(Mathf.Abs(agentRB.position.x-(agentBoundary.Left+1.8f))))*stayCenteredReward*0.5f);
            AddReward(((Mathf.Abs(agentRB.position.x-(agentBoundary.Left+2f))))*negoffCenterReward*0.5f);

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
        if(discretedirection.magnitude > 1f)
        {
            discretedirection.Normalize();
        }

        
        if(avoidDirectionChanges > 0f) // Punish changing direction too much.
        {
            AddReward(-((lastDirection - direction).magnitude) * avoidDirectionChanges);
            episodeReward["DirectionReward"] -= (lastDirection - direction).magnitude * avoidDirectionChanges;
        }
        lastDirection = direction;

        if (avoidBoundaries > 0f) // Punish running into boundaries.
        {
            if (agentRB.position.x < agentBoundary.Left || agentRB.position.x > agentBoundary.Right || agentRB.position.y > agentBoundary.Up || agentRB.position.y < agentBoundary.Down)
            {
                AddReward(-1f*avoidBoundaries);
                episodeReward["BoundaryReward"] -= 1f * avoidBoundaries;
            }
        }

        

        if (encouragePuckMovement > 0f) // Reward high puck velocities
        {
            AddReward(puckRB.velocity.magnitude * encouragePuckMovement);
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
