using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine.InputSystem;


public class Handagent : Agent
{
    public ActionType actionType;
    private Controller controlls;
    private Boundary humanBoundary;
    private float horiMove;
    private float vertiMove;
    private Vector2 move;
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
    public GameObject humanBoundaryHolderObject;
    public GameObject envGameObject;
    public bool controllerinput;




    //void OnEnable()
    //{
    //    Debug.Log("enable");
    //    controlls = new Controller();
    //    controlls.Controlleractions.move.performed += ctx => move = ctx.ReadValue<Vector2>();
    //    controlls.Controlleractions.move.canceled += ctx => move = Vector2.zero;
    //    controlls.Controlleractions.Enable();
    //}
    //void OnDisable()
    //{
    //    controlls.Controlleractions.Disable();
    //}
        
    void Start()
    {
        Debug.Log("start");
        handRB = GetComponent<Rigidbody2D>();
        env = envGameObject.GetComponent<envScript>();

        var puckGameObject = GameObject.Find("Puck");
        puck = puckGameObject.GetComponent<PuckScript>();
        puckRB = puckGameObject.GetComponent<Rigidbody2D>();

        var roboGameObject = GameObject.Find("Agent");
        robo = roboGameObject.GetComponent<AirHockeyAgent>();
        roboRB = roboGameObject.GetComponent<Rigidbody2D>();
        
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
        private void FixedUpdate(){
        horiMove = Input.GetAxis("Horizontal");
        vertiMove = Input.GetAxis("Vertical");
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
            var discreteActionsOut = actionsOut.DiscreteActions;

            if (controllerinput){
                //horiMove = Input.GetAxis("Horizontal");
                //vertiMove = Input.GetAxis("Vertical");
                Debug.Log(new Vector2(horiMove,vertiMove));

                Debug.Log("contr");
                if(Mathf.Abs(horiMove)<0.2f) //weder links noch rechts
                {
                    if(Mathf.Abs(vertiMove)<0.2f)// weder hoch noch runter
                    {
                        discreteActionsOut[0] = 0 ;
                    }
                    else if(vertiMove<-0.2f){//runter
                        discreteActionsOut[0] = 2 ;

                    }
                    else if(vertiMove>0.2f){//hoch
                        discreteActionsOut[0] = 7 ;

                    }
                }
                else if(horiMove<=-0.2f) 
                {//links
                Debug.Log("links");
                    if(Mathf.Abs(vertiMove)<0.2f)// weder hoch noch runter
                    {
                        discreteActionsOut[0] = 5 ;
                    }
                    else if(vertiMove<-0.2f){//runter
                        discreteActionsOut[0] = 3 ;

                    }
                    else if(vertiMove>0.2f){//hoch
                        discreteActionsOut[0] = 8 ;

                    }
                }
                else if(horiMove>=0.2f)
                {//rechts
                Debug.Log("rechts");
                    if(Mathf.Abs(vertiMove)<0.2f)// weder hoch noch runterqas
                    {
                        discreteActionsOut[0] = 4 ;
                    }
                    else if(vertiMove<-0.2f){//runter
                        discreteActionsOut[0] = 1 ;

                    }
                    else if(vertiMove>0.2f){//hoch
                        discreteActionsOut[0] = 6 ;

                    }
                }

            }
            
            else{
                Debug.Log("keybaord");
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
        }
        Debug.Log(discreteActionsOut);
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
        Debug.Log(discreteActions[0]);
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
        // Movement
        position = new Vector2(Mathf.Clamp(handRB.position.x, humanBoundary.Left,
                            humanBoundary.Right),
                            Mathf.Clamp(handRB.position.y, humanBoundary.Down,
                            humanBoundary.Up));
        handRB.MovePosition(position + direction * env.V_max_human * Time.fixedDeltaTime);
    }


} 
