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
    private float currContactReward;
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
        currContactReward = env.encouragePuckContact;
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
                AddReward(currContactReward);
                if (env.contacthalf){
                    currContactReward = currContactReward * 0.5f;
                }
                if (puckRB.position.y < agentRB.position.x)
                {
                    AddReward(env.playForwardReward);
                }
                else
                {
                    AddReward(env.negplaybackReward);
                }
                EndEpisode();

            }
            else if (StepCount == MaxStep)
            {
                AddReward(env.negMaxStepReward);
                EndEpisode();
                return;
            }
            AddReward(env.negStepReward);
        }
        else if(env.taskType == envScript.TaskType.Defending)
        {
            if (puck.playState == PlayState.agentScored)
            {
                AddReward(env.agentGoalReward); //no extra, its about blocking
                EndEpisode();
                return;
            }
            else if (puck.playState == PlayState.playerScored)
            {
                AddReward(env.neghumanGoalReward); // no penalty, just blocking
                EndEpisode();
                return;
            }
           
            else if (StepCount == MaxStep)
            {
                AddReward(env.negMaxStepReward);
                EndEpisode();
                return;
            }
            else if (puckRB.velocity.y < 0f)
            {
                AddReward(env.defenceReward);
                EndEpisode();
            }
            AddReward(env.negStepReward); 
        }
        else if(env.taskType == envScript.TaskType.Scoring)
        {
            if (puck.playState == PlayState.agentScored)
            {
                AddReward(env.agentGoalReward);
                EndEpisode();
                return;
            }
            else if (puck.playState == PlayState.playerScored)
            {
                AddReward(env.neghumanGoalReward);
                EndEpisode();
                return;
            }
            else if (agentRB.position.x < agentBoundary.Left || agentRB.position.x > agentBoundary.Right || agentRB.position.y > agentBoundary.Up || agentRB.position.y < agentBoundary.Down)
            {
                EndEpisode();
                return;

            }
            else if (StepCount == MaxStep)
            {
                EndEpisode();
                return;
            }
            AddReward(env.negStepReward);
        }
        else if(env.taskType == envScript.TaskType.FullGame)                       //full game
        {
            if (puck.playState == PlayState.agentScored)
            {
                AddReward(env.agentGoalReward);
                EndEpisode();
                return;
            }
            else if (puck.playState == PlayState.playerScored)
            {
                AddReward(env.neghumanGoalReward);
                EndEpisode();
            }
            if (puck.AgentContact)
            {
                AddReward(currContactReward);
                if (env.contacthalf){
                    currContactReward = currContactReward * 0.5f;
                }
                if (puckRB.position.y < agentRB.position.x)
                {
                    AddReward(env.playForwardReward);
                }
                else
                {
                    AddReward(env.negplaybackReward);
                }

            }
            if (puckRB.position.x < agentBoundary.Left-0.2 || puckRB.position.x > agentBoundary.Right+0.2 || puckRB.position.y > agentBoundary.Up+1 || puckRB.position.y < agentBoundary.Down - 5f)
            {
                EndEpisode();
            }
            AddReward(env.negStepReward); // Negative Step Reward

            if (puckRB.position.y < agentRB.position.x){
                AddReward(env.behindPuckReward);                
            }

            AddReward((2f-(Mathf.Abs(agentRB.position.x-(agentBoundary.Left+2f))))*env.stayCenteredReward*0.5f);
            AddReward(((Mathf.Abs(agentRB.position.x-(agentBoundary.Left+2f))))*env.negoffCenterReward*0.5f);

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

        
        if(env.avoidDirectionChanges > 0f) // Punish changing direction too much.
        {
            AddReward(((lastDirection - direction).magnitude) * env.avoidDirectionChanges);

        }
        lastDirection = direction;

        if (env.avoidBoundaries > 0f) // Punish running into boundaries.
        {
            if (agentRB.position.x < agentBoundary.Left || agentRB.position.x > agentBoundary.Right || agentRB.position.y > agentBoundary.Up || agentRB.position.y < agentBoundary.Down)
            {
                AddReward(env.avoidBoundaries);

            }
        }

        

        if (env.encouragePuckMovement > 0f) // Reward high puck velocities
        {
            AddReward(puckRB.velocity.magnitude * env.encouragePuckMovement);

        }
        if (actionType == ActionType.Discrete){
            direction = discretedirection;
            }
        // Movement
        position = new Vector2(Mathf.Clamp(agentRB.position.x, agentBoundary.Left,
                            agentBoundary.Right),
                            Mathf.Clamp(agentRB.position.y, agentBoundary.Down,
                            agentBoundary.Up));
        agentRB.MovePosition(position + direction * env.V_max_robo * Time.fixedDeltaTime);
    }
}
