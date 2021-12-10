﻿using System.Collections;
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
    
    private Boundary humanBoundary;

    private Rigidbody2D handRB;
    private Rigidbody2D puckRB;
    private Rigidbody2D roboRB;
    private PuckScript puck;
    private envScript env;
    private AirHockeyAgent robo;
    private HumanPlayer humanPlayer;
    private Vector2 startingPosition;
    private Vector2 lastDirection;
    private Vector2 position;
    public ResetPuckState resetPuckState;
    public float avoidBoundaries;
    public float avoidDirectionChanges;
    public float encouragePuckMovement;
    public bool puckStopPenalty;
    public bool backWallReward;
    public bool deflectOnly;
    public GameObject humanBoundaryHolderObject;
    public GameObject envGameObject;



    void Start()
    {
        handRB = GetComponent<Rigidbody2D>();
        Debug.Log("start");
        env = envGameObject.GetComponent<envScript>();

        var puckGameObject = GameObject.Find("Puck");
        puck = puckGameObject.GetComponent<PuckScript>();
        puckRB = puckGameObject.GetComponent<Rigidbody2D>();

        var roboGameObject = GameObject.Find("Agent");
        robo = roboGameObject.GetComponent<AirHockeyAgent>();
        roboRB = roboGameObject.GetComponent<Rigidbody2D>();

       // var humanPlayerGameObject = GameObject.Find("Agent");
       // humanPlayer = humanPlayerGameObject.GetComponent<HumanPlayer>();

    //    var handBoundaryHolder = GameObject.Find("PlayerBoundaryHolder").GetComponent<Transform>();
     //   handBoundary = new Boundary(handBoundaryHolder.GetChild(0).localPosition.y,
       //               handBoundaryHolder.GetChild(1).localPosition.y,
         //             handBoundaryHolder.GetChild(2).localPosition.x,
           //           handBoundaryHolder.GetChild(3).localPosition.x);
        

        var humanBoundaryHolder = humanBoundaryHolderObject.GetComponent<Transform>();
        humanBoundary = new Boundary(humanBoundaryHolder.GetChild(0).position.y,
                      humanBoundaryHolder.GetChild(1).position.y,
                      humanBoundaryHolder.GetChild(2).position.x,
                      humanBoundaryHolder.GetChild(3).position.x);

        //startingPosition = new Vector2(0, -2.3f);
        startingPosition = handRB.position;
    }

    public override void OnEpisodeBegin()
    {
        Debug.Log("episode");

        while (true)
        {
            handRB.velocity = Vector2.zero;

            if (env.humanResetState == envScript.HumanResetState.random)
            //if(true)
            {
                handRB.position = new Vector2(Random.Range(humanBoundary.Left, humanBoundary.Right) * 0.8f, Random.Range(humanBoundary.Down, humanBoundary.Up) * 0.8f);
            }
            else if (env.humanResetState == envScript.HumanResetState.centered)
            {
                handRB.position = new Vector2((humanBoundary.Left + humanBoundary.Right) * 0.5f, (humanBoundary.Down + humanBoundary.Up) * 0.5f);
            }
            else if (env.humanResetState == envScript.HumanResetState.shotOnGoal)
            {
                handRB.position = new Vector2(humanBoundary.Left + 0.8f,humanBoundary.Down +0.8f);
            }
            else
            {
                handRB.position = new Vector2(humanBoundary.Left + 0.2f, humanBoundary.Down + 0.2f);
            }

            if (Mathf.Abs(puck.PuckRB.position.y - handRB.position.y) >= 1.0 || Mathf.Abs(puck.PuckRB.position.x - handRB.position.x) >= 1.0)
            {
                break;
            }
        }



    }

        public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(-transform.localPosition);
        sensor.AddObservation(-puck.transform.localPosition);
        sensor.AddObservation(-puckRB.velocity);
    }

        public override void Heuristic(in ActionBuffers actionsOut)
    {
        

        if (actionType == ActionType.Discrete)
        {
            Debug.Log("heuristik");
            var discreteActionsOut = actionsOut.DiscreteActions;
            discreteActionsOut[0] = 0 ;
            if (Input.GetKey(KeyCode.Q)){       
                discreteActionsOut[0] = 1 ;
                //Debug.Log("q");
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
            //Debug.Log(discreteActionsOut[0]);
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
        Debug.Log("action");

        int dx = discreteActions[0]; 
        //Debug.Log(dx);

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
            switch(dx){
                case 0:
                discretedirection =new Vector2(0, 0);
                break;
                case 1:
                discretedirection =new Vector2(1, -1);
                break;
                case 2:
                discretedirection =new Vector2(0, -1);
                break;
                case 3:
                discretedirection =new Vector2(-1, -1);
                break;
                case 4:
                discretedirection =new Vector2(1, 0);
                break;
                case 5:
                discretedirection =new Vector2(-1, 0);
                break;
                case 6:
                discretedirection =new Vector2(1, 1);
                break;
                case 7:
                discretedirection =new Vector2(0, 1);
                break;
                case 8:
                discretedirection =new Vector2(-1, 1);
                break;
                
            }
        }
        


        
        if(discretedirection.magnitude > 1f)
        {
            discretedirection.Normalize();
        }

        if (actionType == ActionType.Discrete){
            direction = discretedirection;
        }
 
        Debug.Log(discretedirection);
        // Movement
        position = new Vector2(Mathf.Clamp(handRB.position.x, humanBoundary.Left,
                            humanBoundary.Right),
                            Mathf.Clamp(handRB.position.y, humanBoundary.Down,
                            humanBoundary.Up));
        handRB.MovePosition(position + direction * maxMovementSpeed * Time.fixedDeltaTime);
    }


} 
