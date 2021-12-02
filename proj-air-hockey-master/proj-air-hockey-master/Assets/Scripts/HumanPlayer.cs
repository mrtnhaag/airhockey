using UnityEngine;

public enum ResetHumanState
{
    normalPosition,
    random,
    shotOnGoal
}

public class HumanPlayer : MonoBehaviour
{
    public bool automaticMovement;
    public float maxMovementSpeed;

    private Rigidbody2D humanPlayerRB;
    private Rigidbody2D PuckRB;
    public GameObject targetPositionObject;
    private Vector2 startingPosition;
    private Boundary playerBoundary;
    private Boundary puckBoundary;
    private Vector2 targetPosition;

    private bool randomSetFlag = false;
    private float offsetXFromTarget;
    private Collider2D humanPlayerCollider;
    bool wasJustClicked = true;
    bool canMove;
    public GameObject playerBoundaryHolderObject;
    public GameObject puckBoundaryHolderObject;
    

    // mein zeug
    [SerializeField] bool handsteering = true;
    float horizMove = 0;
    float vertiMove = 0;

    private void Start()
    {
        // Find Puck in Scene
       //var puckGameObject = GameObject.Find("Puck");
       // targetPositionObject = puckGameObject;
        var puckGameObject = targetPositionObject; 
        PuckRB = puckGameObject.GetComponent<Rigidbody2D>();
        // Find Boundary in Scene
        var playerBoundaryHolder = playerBoundaryHolderObject.GetComponent<Transform>();
        var puckBoundaryHolder = puckBoundaryHolderObject.GetComponent<Transform>();
        // Get Rigidbody and Collider
        humanPlayerRB = GetComponent<Rigidbody2D>();
        humanPlayerCollider = GetComponent<Collider2D>();
        
        //startingPosition = humanPlayerRB.position;
        //startingPosition = humanPlayerRB.transform.position;
        startingPosition = new Vector2((playerBoundary.Right+playerBoundary.Left)/2,(playerBoundary.Down+playerBoundary.Up)/2);
        
        playerBoundary = new Boundary(playerBoundaryHolder.GetChild(0).position.y,
            playerBoundaryHolder.GetChild(1).position.y,
            playerBoundaryHolder.GetChild(2).position.x,
            playerBoundaryHolder.GetChild(3).position.x);

        puckBoundary = new Boundary(puckBoundaryHolder.GetChild(0).position.y,
            puckBoundaryHolder.GetChild(1).position.y,
            puckBoundaryHolder.GetChild(2).position.x,
            puckBoundaryHolder.GetChild(3).position.x);
    }

    public void ResetPosition()
    {
        humanPlayerRB.position = startingPosition;
        
    }

    private void Update(){
        horizMove = Input.GetAxis("Horizontal");
        vertiMove = Input.GetAxis("Vertical");

    }

    private void FixedUpdate()
    {
         int handMoveScale = 3;
        if (handsteering){
            humanPlayerRB.velocity= new Vector3(horizMove*handMoveScale,vertiMove*handMoveScale,0);
        }
        
        if(automaticMovement)
            {
                
            float movementSpeed;
            if (PuckRB.position.y > playerBoundary.Up) // Puck in Opponents half
            {
                targetPosition = new Vector2(PuckRB.position.x,playerBoundary.Up-1f);
           /*  if (!randomSetFlag)  //????
            
            {
                offsetXFromTarget = Random.Range(-1f, 1f);
                randomSetFlag = true;
            }
                movementSpeed = Random.Range(maxMovementSpeed * 0.5f, maxMovementSpeed);
                targetPosition = new Vector2(offsetXFromTarget, startingPosition.y);*/
            } 
            else // Puck in hand half
            {
                
                movementSpeed = Random.Range(maxMovementSpeed * 0.5f, maxMovementSpeed);
                randomSetFlag = false;
                if (PuckRB.position.y-0.2 < humanPlayerRB.position.y ) // puck unterhalb von hand
                {
                    //targetPosition = new Vector2(0, Mathf.Clamp(PuckRB.position.y-1f, playerBoundary.Down,
                    //        playerBoundary.Up));
                    targetPosition = new Vector2(PuckRB.position.x,PuckRB.position.y-1f);

                }
                else
                {
                    /* targetPosition = new Vector2(Mathf.Clamp(PuckRB.position.x, playerBoundary.Left,
                                                playerBoundary.Right),
                                                Mathf.Clamp(PuckRB.position.y, playerBoundary.Down,
                                                playerBoundary.Up)); */
                    targetPosition = new Vector2(PuckRB.position.x,PuckRB.position.y);
                
                }
                
            }        

            
        
        

        var direction = new Vector2(-humanPlayerRB.position.x+ targetPosition.x,
            -humanPlayerRB.position.y+ targetPosition.y);
        if(direction.magnitude > 0.2f)
        {
            direction.Normalize();
        }
        
        var position = new Vector2(Mathf.Clamp(humanPlayerRB.position.x, playerBoundary.Left,
                playerBoundary.Right),
                Mathf.Clamp(humanPlayerRB.position.y, playerBoundary.Down,
                playerBoundary.Up));

        var position_move = (position + direction * maxMovementSpeed * Time.fixedDeltaTime);                        //new position
        
        var position_clamped = new Vector2(Mathf.Clamp(position_move.x,playerBoundary.Left,playerBoundary.Right),Mathf.Clamp(position_move.y, playerBoundary.Down,  //clamped new pos
                playerBoundary.Up));

        //Debug.Log(position);
                
        //humanPlayerRB.MovePosition(position + direction * maxMovementSpeed * Time.fixedDeltaTime);
        humanPlayerRB.MovePosition(position_clamped);

        //humanPlayerRB.MovePosition(Vector2.MoveTowards(humanPlayerRB.position, targetPosition,
        //    movementSpeed * Time.fixedDeltaTime));
        }
        else    
        {
            if (Input.GetMouseButton(0))
            {
                Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

                if (wasJustClicked)
                {
                    wasJustClicked = false;

                    if (humanPlayerCollider.OverlapPoint(mousePos))
                    {
                        canMove = true;
                    }
                    else
                    {
                        canMove = false;
                    }
                }

                if (canMove)
                {
                    Vector2 clampedMousePos = new Vector2(Mathf.Clamp(mousePos.x, playerBoundary.Left,
                                                                      playerBoundary.Right),
                                                          Mathf.Clamp(mousePos.y, playerBoundary.Down,
                                                                      playerBoundary.Up));
                    humanPlayerRB.MovePosition(clampedMousePos);
                }
            }
            else
            {
                wasJustClicked = true;
            }
        }

        
        
        
    }
}
