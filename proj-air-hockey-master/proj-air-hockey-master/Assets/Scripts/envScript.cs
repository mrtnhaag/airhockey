using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class envScript : MonoBehaviour
{
    public enum  TaskType
{
    Reaching,
    Scoring,
    Defending,
    FullGame
}

public enum RoboResetState
{
    centered,
    random,
    shotOnGoal
}

public enum HumanResetState
{
    centered,
    random,
    shotOnGoal
}
    public Boundary agentBoundary;
    public Boundary humanBoundary;

    private Rigidbody2D roboRB;
    private Rigidbody2D humanRB;
    private Rigidbody2D puckRB;
    private PuckScript puck;
    private Handagent human;
    private AirHockeyAgent robo;
    public ResetPuckState resetPuckState;
    public RoboResetState roboResetState;
    public HumanResetState humanResetState;
    public TaskType taskType = TaskType.FullGame;
    public float currContactReward;
    public float V_max_puck = 25;
    public float maxMovementSpeed = 6f;
    public float V_max_human = 3f;
    public float neghumanGoalReward = 0f;
    public float agentGoalReward = 0f;
    public float avoidBoundaries = 0f;
    public float avoidDirectionChanges = 0f;
    public float stayCenteredReward = 0f;
    public float negoffCenterReward = -0.05f;
    public float encouragePuckMovement = 0f;
    public float encouragePuckContact = 0.8f;
    public bool contacthalf = false;
    public float playForwardReward = 2.5f;
    public float negplaybackReward = -0.50f;
    public float negStepReward = -0.01f;
    public float negMaxStepReward = 0f;
    public float behindPuckReward = 0f;
    public float defenceReward = 0f;
    public float backwallReward = 0f;
    public bool puckStopPenalty;
    public bool backWallReward;
    public bool deflectOnly;
    public GameObject puckGameObject;
    public GameObject humanPlayerGameObject;
    public GameObject roboPlayerGameObject;
    public GameObject agentBoundaryHolderObject;
    public GameObject humanBoundaryHolderObject;
    public Dictionary<string, float> episodeReward;
    // Start is called before the first frame update
    void Start()
    {
        puck = puckGameObject.GetComponent<PuckScript>();
        puckRB = puckGameObject.GetComponent<Rigidbody2D>();

        human = humanPlayerGameObject.GetComponent<Handagent>();
        humanRB = puckGameObject.GetComponent<Rigidbody2D>();

        robo = roboPlayerGameObject.GetComponent<AirHockeyAgent>();
        roboRB = puckGameObject.GetComponent<Rigidbody2D>();

        switch(taskType){
                case TaskType.Reaching:
                resetPuckState = ResetPuckState.randomPositionAgentSide;
                V_max_human = 0f;
                roboResetState = RoboResetState.random;
                humanResetState = HumanResetState.random;
                break;

                case TaskType.Defending:
                resetPuckState = ResetPuckState.shotOnGoal;
                V_max_human = 0f;
                roboResetState = RoboResetState.shotOnGoal;
                humanResetState = HumanResetState.shotOnGoal;
                break;

                case TaskType.FullGame:
                resetPuckState = ResetPuckState.randomPositionAgentSide;
                roboResetState = RoboResetState.random;
                humanResetState = HumanResetState.random;
                //V_max_human = 3f;
                break;

                case TaskType.Scoring:
                resetPuckState = ResetPuckState.randomPositionAgentSide;
                V_max_human = 0f;
                roboResetState = RoboResetState.random;
                humanResetState = HumanResetState.random;
                break;
            }
        human.maxMovementSpeed = V_max_human;
        //puck.MaxSpeed = V_max_puck;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
