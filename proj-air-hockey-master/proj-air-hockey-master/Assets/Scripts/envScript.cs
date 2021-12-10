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
    public TaskType taskType = TaskType.Reaching;
    public float currContactReward;
    public float V_max_puck;
    public float V_max_robo;
    public float V_max_human;
    public float neghumanGoalReward ;
    public float agentGoalReward;
    public float avoidBoundaries;
    public float avoidDirectionChanges;
    public float stayCenteredReward;
    public float negoffCenterReward;
    public float encouragePuckMovement;
    public float encouragePuckContact;
    public bool contacthalf;
    public float playForwardReward;
    public float negplaybackReward;
    public float negStepReward;
    public float negMaxStepReward;
    public float behindPuckReward;
    public float defenceReward;
    public float backwallReward;
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
                resetPuckState = ResetPuckState.randomPositionRoboSide;
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
                resetPuckState = ResetPuckState.randomPositionGlobal;
                roboResetState = RoboResetState.random;
                humanResetState = HumanResetState.random;
                break;

                case TaskType.Scoring:
                resetPuckState = ResetPuckState.randomPositionRoboSide;
                V_max_human = 0f;
                roboResetState = RoboResetState.random;
                humanResetState = HumanResetState.random;
                break;
            }

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
