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
    [System.NonSerialized]
    public ResetPuckState resetPuckState;
    [System.NonSerialized]
    public RoboResetState roboResetState;
    [System.NonSerialized]
    public HumanResetState humanResetState;
    [System.NonSerialized]
    public TaskType taskType = TaskType.FullGame;
    [System.NonSerialized]
    public float currContactReward;
    [System.NonSerialized]
    public float V_max_puck = 25;
    [System.NonSerialized]
    public float V_max_robo = 6f;
    [System.NonSerialized]
    public float V_max_human = 3f;
    [System.NonSerialized]
    public float neghumanGoalReward = -1f;
    [System.NonSerialized]
    public float agentGoalReward = 2f;
    [System.NonSerialized]
    public float avoidBoundaries = 0f;
    [System.NonSerialized]
    public float avoidDirectionChanges = 0.003f;
    [System.NonSerialized]
    public float stayCenteredReward = 0f;
    [System.NonSerialized]
    public float negoffCenterReward = 0.2f;
    [System.NonSerialized]
    public float encouragePuckMovement = 0.01f;
    [System.NonSerialized]
    public float encouragePuckContact = 0.5f;
    [System.NonSerialized]
    public bool contacthalf = false;
    [System.NonSerialized]
    public float playForwardReward = 0.2f;
    [System.NonSerialized]
    public float negplaybackReward = -0.1f;
    [System.NonSerialized]
    public float negStepReward = -0.01f;
    [System.NonSerialized]
    public float negMaxStepReward = 0f;
    [System.NonSerialized]
    public float behindPuckReward = 0.05f;
    [System.NonSerialized]
    public float defenceReward = 0f;
    [System.NonSerialized]
    public float backwallReward = 0f;
    [System.NonSerialized]
    public bool puckStopPenalty;
    [System.NonSerialized]
    public bool backWallReward;
    [System.NonSerialized]
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
