using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Controller : MonoBehaviour
{
    // Controller is to control the process of game
    public GameObject Game_Board;
    public GameObject CameraController;
    GameBoard gameboard;
    CameraController cameracontroller;
    ControllerUI controllerUI;

    // set game environment
    int player_num = 4;
    int barrier_num = 5;
    
    Player[] playerlist;

    // turn start -> choose a player -> choose which place to move -> do the move -> turn end
    StateManager statemanager;
    ActionHandler actionHandler;

    // state info para
    int present_player;
    bool CameraMoving = false;  // During the action of camera moving

    // common data structure
    Color[] m_ColorList;

    // Start is called before the first frame update
    void Awake()
    {
        // start from the openning
        // player_num = GameObject.Find("InitData").GetComponent<InitData>().playernum;
        // barrier_num = GameObject.Find("InitData").GetComponent<InitData>().barriernum;

        // public data creation
        m_ColorList = ColorListCreate();

        // do the initailization with the gameboard at the same time
        gameboard = Game_Board.GetComponent<GameBoard>();
        cameracontroller = CameraController.GetComponent<CameraController>();
        controllerUI = GameObject.Find("ControllerUI").GetComponent<ControllerUI>();
        actionHandler = GameObject.Find("ActionHandler").GetComponent<ActionHandler>();

        statemanager = new StateManager(actionHandler);

        (int, int)[] init_pos = new (int, int)[4]{((gameboard.gridx - 1)/2 , 0),
                                                   (0 , (gameboard.gridy - 1)/2), 
                                                   ((gameboard.gridx - 1)/2 , gameboard.gridy - 1), 
                                                   (gameboard.gridx - 1 , (gameboard.gridy - 1)/2)};
        // first para: 0 is row, 1 is col; second para: num;
        (int, int)[] init_tar = new (int, int)[4]{(0, gameboard.gridy - 1), (1, gameboard.gridx - 1), (0, 0), (1, 0)};

        int[] init_camera_angle = new int[4]{ -90, 180, 90, 0 };

        // Initialization of every player;
        playerlist = new Player[player_num];
        int k = 0; // for 
        if (player_num == 4)
        {
            k = 1;
        }
        else if (player_num == 2)
        {
            k = 2;
        }
        for (int i = 0; i < player_num; i++) {
            GameObject playerob = gameboard.create_player(init_pos[k*i], i+1);
            playerlist[i] = playerob.GetComponent<Player>();
            playerlist[i].SetID(i);
            playerlist[i].SetColor(m_ColorList[i]);
            playerlist[i].playername = i.ToString();
            cameracontroller.CreatePlayerCamera(init_camera_angle[k*i]);

            // set target for player
            if (init_tar[k*i].Item1 == 0) // row target
            {
                for (int j = 0; j < gameboard.gridx; j++){
                    playerlist[i].AddTarget((j, init_tar[k*i].Item2));
                }
            }
            else                       // col target
            {
                for (int j = 0; j < gameboard.gridy; j++){
                    playerlist[i].AddTarget((init_tar[k*i].Item2, j));
                }
            }

            playerlist[i].RouteInit();
        }


        playerlist[0].in_round = true;
        present_player = 0;
        actionHandler.SetCurPlayer(playerlist[0]);
    }

    void Start()
    {
        ;
    }

    // Update is called once per frame
    void Update()
    {
        if (CameraMoving)
        {
            CameraMoving = cameracontroller.GetState();
        }
        else
        {
            // In player round action
            statemanager.Update();
            if (statemanager.cur_action != null && statemanager.cur_action.handled)
            {
                NextRound();
                statemanager.cur_action = null;
                CameraMoving = true;
            }
        }
    }

//  Barrier Setting Checker
    public bool BarrierCheck((int, int) from_pos, (int, int) to_pos)
    {
        if (gameboard.HasBarrier(from_pos, to_pos)) return false;
        foreach (Player player in playerlist)
        {
            bool blocked = player.BarrierCheck(from_pos, to_pos);
            if (blocked)
            {
                Debug.Log(player.playername.ToString() + " : Start Searching.......");
                bool suc = player.reRouteSearch(from_pos, to_pos, false);
                if (!suc)
                {

                    Debug.Log(player.playername.ToString() + ": Searching Failed!");
                    return false;
                }
            }
            Debug.Log(player.playername.ToString() + " : Barrier Check successfully!");
        }
        Debug.Log("All Barrier Check successfully!");

        return true;
    }

    public bool DoubleBarrierCheck((int, int) from_pos, (int, int) to_pos)
    {
        if (gameboard.HasBarrier(from_pos, to_pos) || gameboard.HasBarrier(to_pos, from_pos)) return false;
        foreach (Player player in playerlist)
        {
            bool blocked = player.DoubleBarrierCheck(from_pos, to_pos);
            if (blocked)
            {
                Debug.Log(player.playername.ToString() + " : Start Searching.......");
                bool suc = player.reRouteSearch(from_pos, to_pos, true);
                if (!suc)
                {
                    Debug.Log(player.playername.ToString() + ": Searching Failed!");
                    return false;
                }
            }
            Debug.Log(player.playername.ToString() + " : Barrier Check successfully!");
        }
        Debug.Log("All Barrier Check successfully!");

        return true;
    }


//  Round Control Function 
    void NextRound()
    {
        // check if game is over
        if (playerlist[present_player].ReachTarget())
        {
            controllerUI.EndGame();
            //EndGame();
        }
        // push to the next round
        Debug.Log("next round");
        playerlist[present_player].in_round = false;
        present_player++;
        if (present_player == player_num)
        {
            present_player = 0;
        }
        playerlist[present_player].in_round = true;

        cameracontroller.NextRound();

        actionHandler.SetCurPlayer(playerlist[present_player]);
        statemanager.GetModeState = 0;
    }

    void EndGame()
    {
        SceneManager.LoadScene("Openning");
    }

    public Player GetPresentPlayer()
    {
        return playerlist[present_player];
    }

//  Functional Function
    public void OnClickBarrier()
    {
        if (GetPresentPlayer().barrier_num > 0)
        {
            statemanager.GetModeState = statemanager.GetModeState == 0 ? 1 : 0;
            Debug.Log("Change to mode :");
            Debug.Log(statemanager.GetModeState);
            statemanager.ClearSelected();
        }
        else
        {
            Debug.Log("No left barrier");
        }
    }

    public void OnClickCamera()
    {
        statemanager.ClearSelected();
    }

// Tool function
    private Color[] ColorListCreate()
    {
        string[] strcolors = new string[4];
        strcolors[0] = "#00ced15f";
        strcolors[1] = "#7fffaa5f";
        strcolors[2] = "#ffff005f";
        strcolors[3] = "#ffc0cb5f";
        Color[] colors = new Color[4];
        for (int i = 0; i < 4; i++)
        {
            Color color;
            ColorUtility.TryParseHtmlString(strcolors[i], out color);
            colors[i] = color;
        }
        return colors;
    }
}


public class MouseSelect
{
private
    Ray ray;
    RaycastHit hit;

public
    GameObject HitObj;

    public GameObject GetObject()
    {
        if (Input.GetMouseButtonDown(0)) {
            ray = Camera.allCameras[0].ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit))
            {
                HitObj = hit.collider.gameObject;
                if (HitObj.layer == 5)
                {
                    Debug.Log("Clicked UI");
                    return null;
                }
                if (HitObj.layer == 7)
                {
                    // player
                    Debug.Log("Clicked Player");
                    if (!HitObj.GetComponent<Player>().selected)
                    {
                        HitObj.GetComponent<Player>().Selected();
                    }
                    else
                    {
                        HitObj.GetComponent<Player>().Deselected();
                    }
                }
                else if (HitObj.layer == 8)
                {
                    // board
                    Debug.Log("Clicked Board");
                    if (!HitObj.GetComponent<BoardPiece>().selected)
                    {
                        HitObj.GetComponent<BoardPiece>().Selected();
                    }
                    else
                    {
                        HitObj.GetComponent<BoardPiece>().Deselected();
                    }
                }
                return HitObj;
            }  
        }

        return null;      
    }
}

public class StateManager
{
    // State 0 means in player operation
    // State 1 means in implementing action
    private int State = 0;
    // ModeState 0 means in moving player
    // ModeState 1 means in setting barriers
    private int ModeState = 0;

    GameObject selected;
    GameObject targeted;

    MouseSelect mouseselect;
    ActionHandler handler;

    public Action cur_action;
    public Player cur_player;

    public StateManager(ActionHandler o_handler)
    {
        mouseselect = new MouseSelect();
        cur_action = null;
        handler = o_handler;
    }

    public void Update()
    {
        GameObject clicked = mouseselect.GetObject();
        if (clicked != null)
        {
            Debug.Log(clicked);
        }
        if (State == 0)
        {
            // try to select
            if (clicked && !selected)
            {
                selected = clicked;
            }
            else if (clicked && !targeted)
            {
                targeted = clicked;
            }

            if (selected && targeted)
            {

                Debug.Log("Pair Selected!");
                if (selected.layer == 7 && targeted.layer == 8)
                {
                    // movement of player
                    selected.GetComponent<Player>().Deselected();
                    targeted.GetComponent<BoardPiece>().Deselected();
                    // reasonable action
                    if (ModeState == 0)
                    {
                        Move new_action = new Move(selected, targeted);
                        bool res = handler.SetAction(new_action);
                        if (res) 
                        { 
                            State = 1;
                            cur_action = new_action;
                        }
                    }
                }
                else if (selected.layer == 8 && targeted.layer == 8)
                {
                    // set a barrier
                    selected.GetComponent<BoardPiece>().Deselected();
                    targeted.GetComponent<BoardPiece>().Deselected();
                    // reasonable action
                    if (ModeState == 1)
                    {
                        Debug.Log("Creating SetBarrier Action!");
                        SetBarrier new_action = new SetBarrier(selected, targeted);
                        bool res = handler.SetAction(new_action);
                        if (res) 
                        { 
                            State = 1;
                            cur_action = new_action;
                        }
                    }
                }
                selected = null;
                targeted = null;
            }
        }

        else if (State == 1)
        {
            if (handler.busy)
            {
                //handler.Update();
            }

            else
            {
                cur_action.handled = true;
                State = 0;
            }
        }  
    }
    public int GetState
    {
        get { return State; }
        set {State = value; }
    }

    public int GetModeState
    {
        get { return ModeState; }
        set {ModeState = value; }
    }

    public void ClearSelected()
    {
        selected = null;
        targeted = null;
        Debug.Log("Clear Selection!");
    }

}

public class Action
{ 
    public GameObject m_FirstObject;
    public GameObject m_SecondObject;

    public bool handled = false;

    public Action()
    {
        ;
    }

    public Action(GameObject player)
    {
        m_FirstObject = player;
        m_SecondObject = null;
    }

    public virtual void init()
    {
        ;
    }

    public virtual bool implement()
    {
        return true;
    }
}

public class Move : Action
{
public
    Vector3 m_Offset;
    
    public Move()
    {
        ;
    }

    public Move(GameObject player, GameObject target)
    {
        m_FirstObject = player;
        m_SecondObject = target;
    }

    public override bool implement() // return true if the implement is finished
    {
        m_FirstObject.transform.position += m_Offset * Time.deltaTime * 1.3f;
        Vector2 dis = new Vector2(m_SecondObject.transform.position.x - m_FirstObject.transform.position.x, m_SecondObject.transform.position.z - m_FirstObject.transform.position.z);
        if (dis.magnitude < 0.1f)
        {
            m_FirstObject.transform.position = new Vector3(m_SecondObject.transform.position.x, m_FirstObject.transform.position.y, m_SecondObject.transform.position.z);
            return true;
        }
        else
        {
            return false;
        }
    }

}

public class SetBarrier : Action
{

    public GameObject BarrierSample;
    
    public SetBarrier()
    {
        ;
    }
    public SetBarrier(GameObject block1, GameObject block2)
    {
        m_FirstObject = block1;
        m_SecondObject = block2;
    }

    public override bool implement() // return true if the implement is finished
    {
        return true;
    }
}
