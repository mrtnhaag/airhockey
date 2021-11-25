using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ResetPuckState
{
    normalPosition,
    randomPositionAgentSide,
    randomPositionGlobal,
    shotOnGoal
}

public enum PlayState
{
    normal,
    playerScored,
    agentScored,
    backWallReached,
    puckStopped
}

public class PuckScript : MonoBehaviour
{
    public ScoreScript ScoreScriptInstance;
    public float MaxSpeed;
    public bool AgentContact { get { return agentContact; } }
    public Rigidbody2D PuckRB { get { return puckRB; } }

    private Rigidbody2D puckRB;
    public PlayState playState;
    private bool agentContact;
    public GameObject marker;
    public GameObject markerContainerObject;
    public GameObject puckBoundaryGameObject;
    private Transform markerContainer;
    Boundary puckBoundary;

    void Start()
    {
        puckRB = GetComponent<Rigidbody2D>();
        markerContainer = markerContainerObject.transform;
        var puckBoundaryHolder = puckBoundaryGameObject.GetComponent<Transform>();
        //var puckBoundaryHolder = GameObject.Find("AgentPuckBoundaryHolder").GetComponent<Transform>();
        float offset = 0.21f;
        puckBoundary = new Boundary(puckBoundaryHolder.GetChild(0).position.y - offset/2,
                      puckBoundaryHolder.GetChild(1).position.y + offset,
                      puckBoundaryHolder.GetChild(2).position.x + offset,
                      puckBoundaryHolder.GetChild(3).position.x - offset);
    }

    public void Reset(ResetPuckState resetPuckState, Boundary agentBoundary)
    {
        puckRB.velocity = puckRB.position = Vector2.zero;
        puckRB.angularVelocity = 0f;

        if (resetPuckState == ResetPuckState.normalPosition)
        {
            if (playState == PlayState.agentScored)
            {
                puckRB.position = new Vector2((agentBoundary.Left+agentBoundary.Right)*0.5f, (puckBoundary.Down+puckBoundary.Up)*0.5f-4.5f);
            }
            else if(playState == PlayState.playerScored)
            {
                puckRB.position = new Vector2((agentBoundary.Left+agentBoundary.Right)*0.5f, (puckBoundary.Down+puckBoundary.Up)*0.5f+0.1f);
            }
            else
            {
                puckRB.position = new Vector2((agentBoundary.Left+agentBoundary.Right)*0.5f, (puckBoundary.Down+puckBoundary.Up)*0.5f+0.1f);//agent anstoß
            }
        }
        else if(resetPuckState == ResetPuckState.randomPositionAgentSide)
        {
            puckRB.position = new Vector2(Random.Range(agentBoundary.Left, agentBoundary.Right) * 0.9f, Random.Range(agentBoundary.Down, agentBoundary.Up) * 0.9f);
        }
        else if(resetPuckState == ResetPuckState.randomPositionGlobal)
        {
            puckRB.position = new Vector2(Random.Range(puckBoundary.Left, puckBoundary.Right) * 0.9f, Random.Range(puckBoundary.Down, puckBoundary.Up) * 0.9f);
        }

        else if(resetPuckState == ResetPuckState.shotOnGoal)
        {
            foreach(Transform m in markerContainer)
            {
                Destroy(m.gameObject);
            }

            var currentPoint = new Vector2(Random.Range((puckBoundary.Left +puckBoundary.Right)*0.5f-.35f, (puckBoundary.Left +puckBoundary.Right)*0.5f+.35f), puckBoundary.Up);
            //Instantiate(marker, new Vector3(currentPoint.x, currentPoint.y, 0), Quaternion.identity, markerContainer);
            var angle = Random.Range(-70f, 70f);
            var spawnLine = Random.Range(puckBoundary.Down, puckBoundary.Down-3.08f);

            Vector2 nextPoint = Vector2.zero;
            //Vector2 nextPoint = new Vector2((puckBoundary.Left +puckBoundary.Right)*0.5f,(puckBoundary.Up +puckBoundary.Down)*0.5f);
            Vector2 startingVelocity = Vector2.zero;
            while (true)
            {
                if(angle > 0)
                {
                    nextPoint = new Vector2(puckBoundary.Right, currentPoint.y - (puckBoundary.Right - currentPoint.x) / Mathf.Tan(angle * Mathf.Deg2Rad));

                }
                else
                {
                    nextPoint = new Vector2(puckBoundary.Left, currentPoint.y - (puckBoundary.Left - currentPoint.x) / Mathf.Tan(angle * Mathf.Deg2Rad));
                }
                if(nextPoint.y < spawnLine)
                {
                    nextPoint = new Vector2(currentPoint.x - (spawnLine - currentPoint.y) * Mathf.Tan(angle * Mathf.Deg2Rad), spawnLine);
                    //Debug.DrawLine(currentPoint, nextPoint, Color.green, 1f);
                    //Instantiate(marker, new Vector3(nextPoint.x, nextPoint.y, 0), Quaternion.identity, markerContainer);
                    angle = -angle;
                    startingVelocity = new Vector2(Mathf.Sin(angle * Mathf.Deg2Rad), Mathf.Cos(angle * Mathf.Deg2Rad))*Random.Range(4f, 18f);
                    break;
                }
                else { 
                    angle = -angle;
                    //Debug.DrawLine(currentPoint, nextPoint, Color.green, 1f, false);
                    currentPoint = nextPoint;
                }
                //Instantiate(marker, new Vector3(nextPoint.x, nextPoint.y, 0), Quaternion.identity, markerContainer);
            }                  
            puckRB.position = nextPoint;
            puckRB.velocity = startingVelocity;
        }
        agentContact = false;
        playState = PlayState.normal;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        playState = PlayState.normal;

        if (other.tag == "AIGoal")
        {
            ScoreScriptInstance.Increment(ScoreScript.Score.PlayerScore);
            playState = PlayState.playerScored;
        }
        else if (other.tag == "PlayerGoal")
        {
            ScoreScriptInstance.Increment(ScoreScript.Score.AIScore);
            playState = PlayState.agentScored;
        }
        else if (other.tag == "PlayerBackWall")
        {
            //ScoreScriptInstance.Increment(ScoreScript.Score.AIScore);
            playState = PlayState.backWallReached;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Agent")
        {
            agentContact = true;
        }
        //if (collision.gameObject.tag == "Player" && agentContact == true)
        //{
        //    agentContact = false;
        //}
    }

    private void FixedUpdate()
    {
    
        puckRB.velocity = Vector2.ClampMagnitude(puckRB.velocity, MaxSpeed);
        if(puckRB.velocity.magnitude == 0)
        {
            playState = PlayState.puckStopped;
        }
    }
}