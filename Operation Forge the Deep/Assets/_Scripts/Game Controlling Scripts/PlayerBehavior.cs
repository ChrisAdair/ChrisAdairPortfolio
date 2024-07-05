using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Linq;
using Barebones.MasterServer;
using UnityEngine.SceneManagement;
using TMPro;


public enum eSelected
{
    vertex,
    orientation,
    grain
}
public class PlayerBehavior : NetworkBehaviour {

    // Set adjustable player variables
    [Header("Set in Inspector")]    
    public float cameraSpeed;
    public Material ActivePlayerMat;
    public bool vertexSelectable;
    public Material highlightCube;
    private Material normalCube;
    // Set visible properties for Unity
    [Header("Set Dynamically")]
    public Orientation assignedGrain;
    public List<Vertex> selectedVerts;
    public List<Vertex> allowedVerts;
    public eSelected selection;
    public Vector3 oldMousePosition;
    public NetworkIdentity controller;
    public float displayScore;
    public TextMeshProUGUI userScore;
    public TextMeshProUGUI elasticScoreUI;
    public TextMeshProUGUI deltaScoreUI;
    public TextMeshProUGUI elasticRatioUI;
    public Button quitButton;
    public GrainMode grainMode;
    public Button undoButton;
    public GameObject vanishTextPref;
    public Button optimizeButton;
    public string username;
    public string ChatChannel;
    public LayerMask rayMask;
    public delegate void GameReady();
    public static event GameReady OnGameReady;
    public bool allowInputs = true;
    public delegate void TakeMove();
    public event TakeMove moveTaken;
    [SyncVar]
    public bool levelProgressed;
    public bool serverFinishedPosting = false;

    private bool ReadyAndLoaded = false;
    private Transform trans;
    private Transform grainTrans;
    private bool updateSGT = false;
    private bool playerScoreUpdated = true;
    private float invertHorizCamera = 1.0f;
    private float invertVertCamera = 1.0f;
    private float lastSGTScore;
    [SerializeField]
    private float elasticScore;
    private float lastPlayerDelta = 0f;
    private bool switchingGrain = false;
    private Vector3 newGrainPos;
    private float changeGrainTime;
    //Undo variable block
    private List<List<IUndoable>> undoList;
    //Time history variables
    public string levelName;
    private float compTimeStart;
    private bool preemptiveQuit;
    

    // Use this for initialization
    void Awake () {

        //Instantiate player properties
        trans = GetComponent<Transform>();
        selectedVerts = new List<Vertex>();
        allowedVerts = new List<Vertex>();
        undoList = new List<List<IUndoable>>();
        displayScore = 0f;
        levelProgressed = false;

	}

    public override void OnStartLocalPlayer()
    {
        //Gather the needed references to the UI and game objects
        cameraSpeed = 5.0f;
        elasticScore = 0f;

        #region UISetup

        userScore = GameObject.Find("SGTOutput").GetComponent<TextMeshProUGUI>();
        quitButton = GameObject.Find("QuitButton").GetComponent<Button>();
        elasticScoreUI = GameObject.Find("ElasticScore")?.GetComponent<TextMeshProUGUI>();
        elasticRatioUI = GameObject.Find("CompetingRatio")?.GetComponent<TextMeshProUGUI>();
        deltaScoreUI = GameObject.Find("DeltaScore").GetComponent<TextMeshProUGUI>();
        quitButton?.onClick.AddListener(QuitGame);
        quitButton?.onClick.AddListener(delegate { TriggerBedChange.singleton.LevelTransition(0); });
        grainMode = GameObject.Find("ChangeGrainMode").GetComponent<GrainMode>();
        if(grainMode !=null)
            grainMode.player = this;
        undoButton = GameObject.Find("UndoButton").GetComponent<Button>();
        undoButton?.onClick.AddListener(UndoLastMove);
        optimizeButton = GameObject.Find("Optimize")?.GetComponent<Button>();
        optimizeButton?.onClick.AddListener(delegate { CmdOptimizeGrain(assignedGrain.gameObj); });
        preemptiveQuit = true;
        Debug.Log("moveTaken assigned to the button invocation");
        moveTaken += MoveTaken;
        

        GameObject.Find("CameraSensitivityOption")?.GetComponent<Slider>().onValueChanged.AddListener(ChangeCameraSensitivity);
        GameObject.Find("InvertHoriz")?.GetComponent<Toggle>().onValueChanged.AddListener(InvertHorizCamera);
        GameObject.Find("InvertVert")?.GetComponent<Toggle>().onValueChanged.AddListener(InvertVertCamera);

        Msf.Client.Chat.JoinChannel(ChatChannel, (success, response) => { });
        #endregion

        //Set player color to unique client color
        GetComponentInChildren<MeshRenderer>().material = ActivePlayerMat;
        try
        {
            controller = GameObject.FindGameObjectWithTag("GameController").GetComponent<NetworkIdentity>();
        }
        catch (System.Exception e)
        {
            Debug.LogError("Player object could not find the game controller!!\n" + e.Message);
        }
        userScore.text = "Score: " +controller.gameObject.GetComponent<GrainNetworkAssigner>().SGTOutput.ToString();
        Camera.main.transform.position = trans.position;
        Camera.main.transform.SetParent(transform);
        

        //If player was assigned a grain from server instantiation use that grain
        if (assignedGrain != null)
        {
            
            CmdChangeGrainPlayer(assignedGrain.gameObject, true);
            Vector3 tempPos = assignedGrain.pos;
            tempPos.z -= 10;
            transform.position = tempPos;
            allowedVerts = assignedGrain.GetVertexList();
            grainTrans = assignedGrain.gameObject.transform;
            CmdSetAuthorityGo(assignedGrain.gameObject);
            normalCube = assignedGrain.gameObject.GetComponent<Renderer>().material;
            assignedGrain.gameObject.GetComponent<Renderer>().material = highlightCube;

            assignedGrain.InitializeUndo();
            List<IUndoable> temp = new List<IUndoable>();
            foreach (Vertex vert in allowedVerts)
            {
                vert.InitializeUndo();
                temp.Add(vert);
            }
        }
        else
        {
            SetAssignedGrain();
        }
        //Join the chat room assigned to the room
        CmdJoinRoomChannel();
        //Set up application closing procedure
        Application.wantsToQuit += WantsToQuit;

    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        try
        {
            controller = GameObject.FindGameObjectWithTag("GameController").GetComponent<NetworkIdentity>();
        }
        catch (System.Exception e)
        {
            Debug.LogError("Player object could not find the game controller!!\n" + e.Message);
        }
    }

    public void SetAssignedGrain()
    {
        CmdGetOpenGrain();
    }


    // Update is called once per frame
    void Update () {

        if (!isLocalPlayer) return;
        if (!ReadyAndLoaded) return;
        if (!allowInputs) return;
        //ADD a new check that sees if there is a grain that is currently optimizing: If there is, don't allow input other than camera movement
        //Scoring block
        displayScore = controller.gameObject.GetComponent<GrainNetworkAssigner>().SGTOutput;
        userScore.text = "Total Score: " +displayScore.ToString();
        elasticScoreUI.text = "Elastic Score: " + (controller.gameObject.GetComponent<GrainNetworkAssigner>().ElasticOutput/1e9).ToString("F3") + " GPa";
        elasticRatioUI.text = "Ratio: " + (controller.gameObject.GetComponent<GrainNetworkAssigner>().ElasticOutput / 1e9 / displayScore).ToString("F3");
        if (!playerScoreUpdated)
        {
            if(displayScore != lastSGTScore && !(float.IsNaN(displayScore) || float.IsNaN(lastSGTScore)))
            {
                lastPlayerDelta = displayScore - lastSGTScore;
                deltaScoreUI.color = lastPlayerDelta > 0 ? Color.green : Color.red;
                deltaScoreUI.text = lastPlayerDelta.ToString();
                playerScoreUpdated = true;
                //Create a vanishing score popup at the mouse location
                GameObject vanishScore = Instantiate(vanishTextPref);
                vanishScore.GetComponent<VanishingText>().displayText = lastPlayerDelta.ToString();
                vanishScore.GetComponent<VanishingText>().original = lastPlayerDelta > 0 ? Color.green : Color.red;
            }
        }
        lastSGTScore = displayScore;


        //Check if moving to new grain
        if (switchingGrain)
        {
            var center = assignedGrain.pos;
            var travel = trans.position - center;
            var destination = new Vector3(0, 0, -10);
            float completeTime = (Time.time - changeGrainTime) / 1.0f;

            trans.position = Vector3.Slerp(travel, destination, Time.deltaTime);
            trans.position += center;
            Quaternion curRot = trans.rotation;
            Quaternion destRot = Quaternion.LookRotation(assignedGrain.pos-trans.position);
            trans.rotation = Quaternion.Slerp(curRot, destRot, completeTime);
            
            if (completeTime >= 1.0f)
                switchingGrain = false;
            else
                return;
        }

        //Camera controls for player
        Vector3 vel = Vector3.zero;
        vel.x = Input.GetAxis("Horizontal") * cameraSpeed * invertHorizCamera;
        vel.y = Input.GetAxis("Vertical") * cameraSpeed * invertVertCamera;
        vel.z = Input.GetAxis("Mouse ScrollWheel") * cameraSpeed;


        trans.RotateAround(assignedGrain.pos, Vector3.up, vel.x);
        trans.RotateAround(assignedGrain.pos, trans.right, vel.y);
        trans.Translate(Vector3.forward * vel.z);
        bool isMoving = false;
        //Called on the frame that the mouse button is pushed down
        if (Input.GetMouseButtonDown(0))
        {
            isMoving = true;
            //Use a raycast to find what the user is clicking on
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 200, rayMask))
            {
                Debug.Log(hit.collider.gameObject);
                if(selection == eSelected.grain && hit.collider.gameObject.GetComponent<Orientation>() != null)
                {
                    ChangeGrain(hit.collider.gameObject);
                }
                else
                {
                    //Depending on selection, change mode or add to selection group

                    //Check if the clicked object is a vertex
                    Vertex vert = hit.collider.gameObject.GetComponent<Vertex>();
                    if (vert != null && vertexSelectable)
                    {
                        selection = eSelected.vertex;
                        //Selects vertex with conditions
                        //Will not allow selection if vertex is green (taken)
                        if ((selectedVerts.Count == 0 && allowedVerts.Contains(vert)) ||(allowedVerts.Contains(vert) && !selectedVerts.Contains(vert) && !vert.selected && Input.GetKey(KeyCode.LeftControl)))
                        {
                            CmdSetAuthorityGo(vert.gameObject);
                            CmdChangeVertexState(vert.gameObject, true);
                            selectedVerts.Add(vert);

                        }
                        //If vertex was already selected and controlled, deselect the vertex
                        else if (selectedVerts.Contains(vert) && Input.GetKey(KeyCode.LeftControl))
                        {

                            selectedVerts.Remove(vert);
                            CmdChangeVertexState(vert.gameObject, false);
                            CmdRemoveAuthorityGo(vert.gameObject);
                        }

                    }
                    else
                    {
                        if (hit.collider.gameObject.GetComponent<Orientation>() == assignedGrain)
                        {
                            //Clicking on the orientation center clears the selected vertices
                            selection = eSelected.orientation;
                            foreach (Vertex clear in selectedVerts)
                            {
                                CmdChangeVertexState(clear.gameObject, false);
                            }
                            selectedVerts.Clear();

                            
                        }

                    }
                }
                
                
            }
            //Set mouse position for possible movement
            oldMousePosition = Input.mousePosition;

            if(selection == eSelected.orientation)
            {
                //Start the computation time for orientation movement
                compTimeStart = Time.time;
            }

        }
        //Called on each frame the mouse button is held down
        if (Input.GetMouseButton(0))
        {
            isMoving = true;
            //If the button is held down, start movement of selected group
            switch (selection)
            {
                case eSelected.vertex:
                    Vector3 avgPos = Vector3.zero;
                    Vector3 delta = Vector3.zero;
                    foreach(Vertex vert in selectedVerts)
                    {
                        avgPos += vert.pos;
                    }
                    avgPos /= selectedVerts.Count;
                    avgPos = Camera.main.WorldToScreenPoint(avgPos);
                    Vector3 newMousePosition = Input.mousePosition;
                    newMousePosition.z = avgPos.z;
                    oldMousePosition.z = avgPos.z;
                    newMousePosition = Camera.main.ScreenToWorldPoint(newMousePosition);
                    oldMousePosition = Camera.main.ScreenToWorldPoint(oldMousePosition);
                    delta = newMousePosition - oldMousePosition;

                    oldMousePosition = Camera.main.WorldToScreenPoint(newMousePosition);
                    //Move all the selected vertices a distance delta
                    foreach (Vertex vert in selectedVerts)
                    {
                        if(vert.hasAuthority)
                        {
                            vert.pos = CheckVertexMoveValid(vert, delta);
                            vert.CmdUpdateMeshes();
                        }

                    }
                    updateSGT = true;
                    GetComponent<ColorOrientations>().updated = false;
                    break;
                case eSelected.orientation:
                    Vector3 currMousePos = Input.mousePosition;
                    float deltaX = currMousePos.x - oldMousePosition.x;
                    float deltaY = currMousePos.y - oldMousePosition.y;

                    float rotMag = deltaX * 0.5f;
                    grainTrans.Rotate(Camera.main.transform.up, -rotMag, Space.World);
                    rotMag = deltaY * 0.5f;
                    grainTrans.RotateAround(grainTrans.position, Camera.main.transform.right, rotMag);
                    
                    
                    oldMousePosition = currMousePos;
                    
                    if (deltaX != 0 || deltaY != 0)
                    {
                        assignedGrain.CmdUpdateDiffusivities();
                        GetComponent<ColorOrientations>().updated = false;
                        updateSGT = true;
                    }
                    
                    break;
            }

        }
        //Called on the frame that the mouse button is released
        if (Input.GetMouseButtonUp(0))
        {
            isMoving = true;

            if(updateSGT)
            {
                //Undo list implementation
                switch (selection)
                {
                    case eSelected.vertex:
                        List<IUndoable> temp = new List<IUndoable>();
                        foreach(Vertex vert in selectedVerts)
                        {
                            if(vert.GetLastValue() != vert.pos)
                            {
                                vert.AddState(vert.pos);
                                temp.Add(vert);
                            }
                        }
                        if(temp.Count !=0)
                            undoList.Add(temp);
                        CmdUpdateSGT(controller,true);
                        break;
                    case eSelected.orientation:
                        if(assignedGrain.GetLastValue() != assignedGrain.orientation)
                        {
                            assignedGrain.AddState(assignedGrain.orientation);
                            undoList.Add(new List<IUndoable>() { assignedGrain });
                            //Update Time history
                            GameObject[] oris = GameObject.FindGameObjectsWithTag("Orientation");
                            CmdUpdateTimeHistory(oris, "ori", compTimeStart, Time.time);
                            if(moveTaken != null)
                                moveTaken.Invoke();
                        }
                        assignedGrain.CmdUpdateDiffusivities();
                        CmdUpdateSGT(controller, false);
                        break;
                }

                //Update the scores for the last move
                
                playerScoreUpdated = false;
            }
                
        }

        if(!isMoving && Input.GetKeyDown(KeyCode.Space))
        {
            grainMode.ToggleGrainMode();
        }
    }

    public Vector3 CheckVertexMoveValid(Vertex vert, Vector3 delta)
    {
        Vector3 output = Vector3.zero;
        //Check if the vertex is pinned in any directions
        if (!vert.xMove) delta.x = 0;
        if (!vert.yMove) delta.y = 0;
        if (!vert.zMove) delta.z = 0;
        output = vert.pos + delta;

        //Clamp the vertex movement to the bounds of the playing fields
        float tempX = Mathf.Clamp(output.x, 0, 10);
        float tempY = Mathf.Clamp(output.y, -5, 5);
        float tempZ = Mathf.Clamp(output.z, 0, 10);

        output = new Vector3(tempX, tempY, tempZ);

        //TODO: Check if the movement would destroy a grain by volumetric checking or pure coplanar grain vertices

        return output;
    }

    public void UndoLastMove()
    {
        if(undoList.Count > 0)
        {
            IUndoable[] undoNow = undoList[undoList.Count - 1].ToArray();
            foreach (IUndoable undo in undoNow)
            {
                undo.Undo();
                
            }
            //UPDATE time history
            GameObject[] undoObj = (from obj in undoNow select obj.gameObj).ToArray<GameObject>();
            //TODO find the computation time of an undo function
            CmdUpdateTimeHistory(undoObj, "Undo", 0, 0);
            //Remove entry from undo
            undoList.RemoveAt(undoList.Count - 1);
        }
        

    }

    public void QuitGame()
    {
        if (isLocalPlayer)
        {
            //Removes all game authorities and detatches scene objects from the player object
            CmdChangeGrainPlayer(assignedGrain.gameObj, false);
            CmdRemoveAuthorityGo(assignedGrain.gameObject);
            foreach (Vertex clear in selectedVerts)
            {
                CmdChangeVertexState(clear.gameObject, false);
                CmdRemoveAuthorityGo(clear.gameObject);
            }
            selectedVerts.Clear();
            gameObject.transform.DetachChildren();
            //Application.Quit();
            StartCoroutine(QuitAfterAuthority(false));
        }
        
    }
    public IEnumerator QuitAfterAuthority(bool trueQuit)
    {

        yield return new WaitUntil(() => !assignedGrain.hasAuthority);
        if (MinimalNetworkManager.singleton.singleplayer && SceneManager.GetActiveScene().name != "Raid_Puzzle")
        {
            //Add a check for the file stream and profile updating here
            //yield return new WaitUntil(() => { return levelProgressed || preemptiveQuit; });
            yield return new WaitUntil(delegate { return levelProgressed||preemptiveQuit; });
            MinimalNetworkManager.singleton.StopHost();
            
        }
        else
        {
            CmdWaitForServerPost();
            yield return new WaitUntil(delegate { return serverFinishedPosting; });
            if(MinimalNetworkManager.singleton.singleplayer)
                MinimalNetworkManager.singleton.StopHost();
            else
                MinimalNetworkManager.singleton.client.Disconnect();
        }
            
            

        if (trueQuit)
        {
            Application.wantsToQuit -= WantsToQuit;
            Application.Quit();
        }
        yield return null;
    }
    [TargetRpc]
    public void TargetLevelProgressedTrue(NetworkConnection conn)
    {
        Debug.LogError("Level progression recieved by client");
        levelProgressed = true;
        QuitGame();
    }
    private bool WantsToQuit()
    {
        if (isLocalPlayer)
        {
            //Removes all game authorities and detatches scene objects from the player object
            CmdChangeGrainPlayer(assignedGrain.gameObj, false);
            CmdRemoveAuthorityGo(assignedGrain.gameObject);
            foreach (Vertex clear in selectedVerts)
            {
                CmdChangeVertexState(clear.gameObject, false);
                CmdRemoveAuthorityGo(clear.gameObject);
            }
            selectedVerts.Clear();
            gameObject.transform.DetachChildren();
            StartCoroutine(QuitAfterAuthority(true));
            return false;
        }
        return true;
    }
    public void ChangeCameraSensitivity(float input)
    {
        cameraSpeed = input;
    }

    public void DisplayFreeGrains()
    {
        GameObject[] allGrains = GameObject.FindGameObjectsWithTag("Orientation");

        foreach (GameObject grain in allGrains)
        {
            if (!grain.GetComponent<Orientation>().hasPlayer)
            {
                grain.GetComponent<Renderer>().material.color = Color.green;
            }
            else
            {
                grain.GetComponent<Renderer>().material.color = Color.red;
            }
        }
    }

    public void HideFreeGrains()
    {
        GameObject[] allGrains = GameObject.FindGameObjectsWithTag("Orientation");

        foreach (GameObject grain in allGrains)
        {
            grain.GetComponent<Renderer>().material.color = Color.white;
            grain.GetComponent<Renderer>().material = normalCube;
        }
        assignedGrain.gameObject.GetComponent<Renderer>().material = highlightCube;
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("GrainBoundary"))
        {

            go.GetComponent<Renderer>().enabled = false;
        }
        foreach (GameObject go in allGrains)
        {
            //go.GetComponent<Renderer>().enabled = false;
        }
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("Vertex"))
        {
            //go.GetComponent<Renderer>().enabled = false;
        }

        foreach (Vertex vert in allowedVerts)
        {
            //vert.gameObject.GetComponent<Renderer>().enabled = true;
            //Doesn't work in client only
            //CmdShowAttachedFaces(vert.gameObject);
        }
        assignedGrain.gameObject.GetComponent<Renderer>().enabled = true;
        //Doesn't work in client only
        //CmdShowNeighborGrains(assignedGrain.gameObject);
    }


    public void ChangeGrain(GameObject toGrain)
    {
        //Make all the changes
        if(!toGrain.GetComponent<Orientation>().hasPlayer)
        {
            CmdChangeGrainPlayer(assignedGrain.gameObject, false);
            CmdRemoveAuthorityGo(assignedGrain.gameObject);
            assignedGrain = toGrain.GetComponent<Orientation>();

            foreach (Vertex vert in selectedVerts)
            {
                CmdChangeVertexState(vert.gameObject, false);
                CmdRemoveAuthorityGo(vert.gameObject);
            }
            selectedVerts.Clear();
            allowedVerts = assignedGrain.GetVertexList();
            grainTrans = toGrain.transform;
            CmdSetAuthorityGo(assignedGrain.gameObject);
            CmdChangeGrainPlayer(assignedGrain.gameObject, true);

            //Exit grain switching mode
            grainMode.ToggleGrainMode();

            //Reset the grain structure colors
            if(GetComponent<ColorOrientations>().display == false)
            {
                GetComponent<ColorOrientations>().ToggleOrientColoring();
            }


            //Re-initialize the undo history
            assignedGrain.InitializeUndo();
            undoList.Clear();
            List<IUndoable> temp = new List<IUndoable>();
            foreach (Vertex vert in allowedVerts)
            {
                vert.InitializeUndo();
                temp.Add(vert);
            }
        }

        //Set motion variables for switching grains
        switchingGrain = true;
        changeGrainTime = Time.time;
    }

    public void InvertHorizCamera(bool input)
    {
        invertHorizCamera *= -1;
    }
    public void InvertVertCamera(bool input)
    {
        invertVertCamera *= -1;
    }
    public void EndGameInput()
    {
        allowInputs = false;
        undoButton.interactable = false;
        if(optimizeButton != null)
            optimizeButton.interactable = false;
    }
    public void ResumeGameInput()
    {
        allowInputs = true;
        undoButton.interactable = true;
        if(optimizeButton !=null)
            optimizeButton.interactable = true;
    }
    [Command]
    public void CmdWaitForServerPost()
    {
        MinimalNetworkManager.singleton.PostToLeaderboard(username,() =>
        {
            TargetServerPosted(connectionToClient);
        });
    }
    [TargetRpc]
    public void TargetServerPosted(NetworkConnection conn)
    {
        serverFinishedPosting = true;
    }
    [Command]
    public void CmdShowAttachedFaces(GameObject vert)
    {
        List<GameObject> facesToShow = new List<GameObject>();
        foreach (GrainMesher go in vert.GetComponent<Vertex>().attachedMeshes)
        {
            facesToShow.Add(go.gameObject);
        }
        TargetShowGameObjects(connectionToClient,facesToShow.ToArray());
    }
    [Command]
    public void CmdShowNeighborGrains(GameObject currGrain)
    {
        List<GameObject> neighbors = new List<GameObject>();
        foreach(GameObject neighbor in currGrain.GetComponent<Orientation>().neighbors)
        {
            neighbors.Add(neighbor);
        }
        TargetShowGameObjects(connectionToClient, neighbors.ToArray());
    }
    [TargetRpc]
    public void TargetShowGameObjects(NetworkConnection conn, GameObject[] objects)
    {
        foreach(GameObject go in objects)
        {
            go.gameObject.GetComponent<Renderer>().enabled = true;
        }
    }

    [Command]
    public void CmdChangeGrainPlayer(GameObject grain, bool state)
    {
        grain.GetComponent<Orientation>().hasPlayer = state;
    }

    [Command]
    public void CmdChangeVertexState(GameObject vert, bool selected)
    {
        vert.GetComponent<Vertex>().selected = selected;
    }
    [Command]
    public void CmdSetAuthorityGo(GameObject go)
    {
        //Could signal here if the clicked object cannot belong to them 
        //using the bool return from AssignClientAuthority
        //using a TargetRPC
        go.GetComponent<NetworkIdentity>().AssignClientAuthority(connectionToClient);
    }
    [Command]
    public void CmdRemoveAuthorityGo(GameObject go)
    {
        go.GetComponent<NetworkIdentity>().RemoveClientAuthority(connectionToClient);
    }
    [Command]
    public void CmdUpdateMesher(NetworkIdentity mesher)
    {
        if(mesher !=null)
        mesher.gameObject.GetComponent<GrainMesher>().updated = false;
    }
    [Command]
    public void CmdUpdateSGT(NetworkIdentity control, bool remesh)
    {
        control.gameObject.GetComponent<GrainNetworkAssigner>().sgtRemesh = remesh;
        control.gameObject.GetComponent<GrainNetworkAssigner>().sgtUpdated = false;
    }
    [Command]
    public void CmdHighlightFace(bool on, GameObject orientation)
    {
        Orientation ori = orientation.GetComponent<Orientation>();
        if (on)
        {
            foreach (GameObject face in ori.faces)
                face.GetComponent<GrainMesher>().TargetHighlightFace(connectionToClient);
        }
        else
        {
            foreach (GameObject face in ori.faces)
                face.GetComponent<GrainMesher>().TargetUnHighlightFace(connectionToClient);
        }
    }
    [Command]
    public void CmdSaveGame(GameObject saveButton)
    {
        saveButton.GetComponent<SaveFunctions>().SaveGame();
    }

    [Command]
    public void CmdOptimizeGrain(GameObject currentGrain)
    {
        GradientDescent opt = GameObject.Find("Optimize").GetComponent<GradientDescent>();
        currentGrain.GetComponent<NetworkIdentity>().RemoveClientAuthority(connectionToClient);
        opt.grainToOptimize = currentGrain.GetComponent<Orientation>();
        var timeStart = Time.time;
        int optSteps = 0;
        opt.StartOptimization(finished=>
        {
            if (finished)
            {
                //Update everything for the final step
                currentGrain.GetComponent<NetworkIdentity>().AssignClientAuthority(connectionToClient);
                controller.gameObject.GetComponent<GrainNetworkAssigner>().sgtUpdated = false;
                RpcUpdatePlayerScore();
                currentGrain.GetComponent<Orientation>().UpdateDiffusivities();
                Quaternion rotation = currentGrain.transform.rotation;
                RpcFixOrientations(currentGrain, rotation);
                TargetUpdateUndo(connectionToClient, currentGrain);
                TargetTriggerMoveCount(connectionToClient);
                //Update time history for optimization
                optSteps+=6;
                MinimalNetworkManager.singleton.writer.WriteLine(username + " Opt" + currentGrain.name + " " + optSteps + " " + timeStart + " " + Time.time);
                GameObject[] oris = GameObject.FindGameObjectsWithTag("Orientation");
                foreach (GameObject obj in oris)
                {
                    var rot = obj.transform.rotation;
                    MinimalNetworkManager.singleton.writer.WriteLine(obj.name + " " +rot.w + " " + rot.x + " " + rot.y + " " + rot.z);
                }
            }
            else
            {
                optSteps+=6;
            }   
        });
    }
    [TargetRpc]
    public void TargetUpdateUndo(NetworkConnection conn, GameObject ori)
    {
        ori.GetComponent<Orientation>().AddState(assignedGrain.orientation);
        undoList.Add(new List<IUndoable>() { ori.GetComponent<Orientation>() });
    }
    [ClientRpc]
    public void RpcUpdatePlayerScore()
    {
        playerScoreUpdated = false;
    }
    [ClientRpc]
    public void RpcFixOrientations(GameObject ori, Quaternion quat)
    {
        ori.transform.rotation = quat;
    }
    [TargetRpc]
    public void TargetTriggerMoveCount(NetworkConnection conn)
    {
        if(moveTaken !=null)
            moveTaken.Invoke();
    }
    /// <summary>
    /// Currently only updates time history for orientation objects
    /// </summary>
    /// <param name="changedObjects"></param>
    /// <param name="type"></param>
    /// <param name="startTime"></param>
    /// <param name="endTime"></param>
    [Command]
    public void CmdUpdateTimeHistory(GameObject[] changedObjects, string type, float startTime, float endTime)
    {
       MinimalNetworkManager.singleton.writer.WriteLine(username + " " + type + " "+ startTime + " " + endTime);
        foreach(GameObject obj in changedObjects)
        {
            var rot = obj.transform.rotation;
            MinimalNetworkManager.singleton.writer.WriteLine(obj.name + " " +rot.w + " " + rot.x + " " + rot.y + " " + rot.z);
        }
    }
    [Command]
    public void CmdGetOpenGrain()
    {
        Orientation[] assigner = MinimalNetworkManager.FindObjectsOfType<Orientation>();
        if (assigner != null)
        {
            bool foundCell = false;
            foreach (Orientation orient in assigner)
            {
                // Check if there are any available grains in the network
                // If there are any, assign the player to the empty grain
                if (!orient.hasPlayer && !foundCell)
                {
                    foundCell = true;
                    orient.hasPlayer = true;
                    TargetGetOpenGrain(connectionToClient, orient.gameObject);
                    ReadyAndLoaded = true;
                }
                orient.hasPlayer = orient.hasPlayer;
            }
            //If the grain network is full, throw an error
            if (assignedGrain == null)
            {
                Debug.LogWarning("Game Instance is full");
            }
        }
    }

    [TargetRpc]
    public void TargetGetOpenGrain(NetworkConnection conn, GameObject grain)
    {
        assignedGrain = grain.GetComponent<Orientation>();
        CmdChangeGrainPlayer(assignedGrain.gameObject, true);
        Vector3 tempPos = assignedGrain.pos;
        tempPos.z -= 10;
        transform.position = tempPos;
        allowedVerts = assignedGrain.GetVertexList();
        grainTrans = assignedGrain.gameObject.transform;
        CmdSetAuthorityGo(assignedGrain.gameObject);
        normalCube = assignedGrain.gameObject.GetComponent<Renderer>().material;
        assignedGrain.gameObject.GetComponent<Renderer>().material = highlightCube;

        assignedGrain.InitializeUndo();
        List<IUndoable> temp = new List<IUndoable>();
        foreach (Vertex vert in allowedVerts)
        {
            vert.InitializeUndo();
            temp.Add(vert);
        }
        ReadyAndLoaded = true;
        //Call event that notifies all other things that the game is ready to play
        if(OnGameReady != null)
            OnGameReady.Invoke();
        //If the grain network is full, throw an error
        if (assignedGrain == null)
        {
            Debug.LogWarning("Game Instance is full");
        }
    }

    [Command]
    public void CmdJoinRoomChannel()
    {
        TargetJoinChatChannel(connectionToClient, ChatChannel);
    }
    [TargetRpc]
    public void TargetJoinChatChannel(NetworkConnection conn, string channelName)
    {
        Msf.Client.Chat.JoinChannel(channelName, (success, error) => {
            if (!success)
            {
                Debug.LogError("Error joining Channel: " + error);
            }
        });

        Msf.Client.Chat.SetDefaultChannel(channelName, (success, error) => {
            if (!success)
            {
                Debug.LogError("Error setting default Channel: " + error);
            }
        });
    }
    public void RestartLevel()
    {
        CmdRestartLevel();
    }
    [Command]
    public void CmdRestartLevel()
    {
        MinimalNetworkManager.singleton.RestartLevel();
    }
    public void ProgressLevel()
    {
        preemptiveQuit = false;
        Debug.LogError("Command sent to host");
        CmdProgressLevel();
        
        //May need to add an ensure mechanic to change scenes
    }
    [Command]
    public void CmdProgressLevel()
    {
        Debug.LogError("Command recieved by host!");
        MinimalNetworkManager.singleton.ProgressLevelProfile();
        TargetLevelProgressedTrue(connectionToClient);
        
    }

    [Client]
    public void MoveTaken()
    {
        CmdMoveTaken();
    }
    [Command]
    public void CmdMoveTaken()
    {
        GameObject.FindGameObjectWithTag("GameCompletion").GetComponent<LevelCompletion>().CountMove();
    }

    private void OnDestroy()
    {
        Application.wantsToQuit -= WantsToQuit;   
    }
}
