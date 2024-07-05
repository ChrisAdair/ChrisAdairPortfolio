using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;

[Flags]
public enum CompletionCondition
{
    None = 0,
    Score = 1,
    Time = 2,
    Moves = 4,
    ScoreTarget = 8
}
[Flags]
public enum MechanicCondition
{
    None=0,
    SimAnneal=1,
    Elastic=2,
    Frozen=4
}
public class LevelCompletion : NetworkBehaviour
{
    //Set up database to have level completion data
    [Header("Set in the level setup")]
    [SyncVar]
    public CompletionCondition condition;
    [SyncVar]
    public MechanicCondition mechanics;
    //The score that the player needs to reach to get a platium star
    [SyncVar]
    public float completeScore = 0;
    [Tooltip("Time limit in seconds")]
    [SyncVar]
    public int completeTime;
    [SyncVar]
    public int completeMoves;
    [SyncVar]
    public float completeTargetScore = 0;
    [SyncVar(hook = "OnCountMove")]
    public int movesTaken = 0;
    public List<int> frozen;
    [Header("These floats decide the proportion of the total score you need in order to get a star")]
    public float firstStar=0.5f;
    public float secondStar=0.75f;
    public float thirdStar=0.9f;
    public float tolerance;

    public TextMeshProUGUI conditionReminder;
    public TextMeshProUGUI resourceRemainder;
    public Image[] stars;
    public delegate void GameOver();
    public event GameOver EndGame;
    public GrainNetworkAssigner assigner;
    public GameObject completeScreen;
    [Header("Completion screen properties")]
    public Button resetLevel;
    public Button nextLevel;
    public Button stayLevel;
    public Button finishLevel;
    public GameObject[] completeStars;
    public TextMeshProUGUI completeBanner;
    public TextMeshProUGUI completeSubTitle;
    public bool loaded = false;
    public bool pauseListener = false;
    private bool pauseTimer = false;
    private bool triggeredOptionalGO = false;
    private bool fStar, sStar, tStar, platStar = false;
    private bool timeText, moveText = false;
    private int curMin, curSec = 0;
    private Coroutine timer;
    private Coroutine simAnneal;
    private bool sentGameOver = false;
    private float scoreToCompare
    {
        get
        {
            if (mechanics.HasFlag(MechanicCondition.Elastic))
            {
                if (assigner.ElasticOutput > 50)
                    return assigner.SGTOutput;
                else
                    return 1;
            }
            else return assigner.SGTOutput;
        }
    }
    
    [ClientCallback]
    public void Start()
    {
        if(assigner == null)
        {
            assigner = GameObject.FindGameObjectWithTag("GameController").GetComponent<GrainNetworkAssigner>();
        }
        loaded = false;
        StartCoroutine(Initialization());
        triggeredOptionalGO = false;
    }

    public IEnumerator Initialization()
    {
        //var player = MinimalNetworkManager.singleton.client.connection.playerControllers[0].gameObject.GetComponent<PlayerBehavior>();
        PlayerBehavior player = null;
        
        int frames = 0;
        while(player == null && frames <=500)
        {
            var players = GameObject.FindObjectsOfType<PlayerBehavior>();
            foreach (PlayerBehavior play in players)
            {
                if (play.isLocalPlayer)
                {
                    player = play;
                    break;
                }
            }
            if (player == null)
            {
                Debug.Log("Could not find local player");
            }
            frames++;
            yield return null;

            
        }

        nextLevel.onClick.AddListener(delegate { TriggerBedChange.singleton.LevelTransition(0); });
        finishLevel.onClick.AddListener(delegate { TriggerBedChange.singleton.LevelTransition(0); });
        if (condition.HasFlag(CompletionCondition.Time))
        {
            conditionReminder.text = "Get to the target score within the time limit: " + completeScore;
            //TEST THIS RIGHT HERE!!!!!!!!!! May not need this line cause of RPC from server
            //StartCoroutine(TimeCounter(completeTime));
            EndGame = ForceGameOver;
            timeText = true;
        }
        else if (condition.HasFlag(CompletionCondition.Score))
        {
            conditionReminder.text = "Get to the target score: " + completeScore;
            EndGame = OptionalGameOver;
        }

        if (condition.HasFlag(CompletionCondition.Moves))
        {
            EndGame = ForceGameOver;
            moveText = true;
            if (timeText)
                conditionReminder.text += " and within " + completeMoves + "moves";
            else
                conditionReminder.text = "Get to the target score within " + completeMoves + " moves";
        }   

        if (condition.HasFlag(CompletionCondition.ScoreTarget))
            EndGame = ForceGameOver;


        if (mechanics.HasFlag(MechanicCondition.SimAnneal))
        {
            //Same reason as above, may not need cause of RPC
            //StartCoroutine(SimulatedAnnealing(completeTime, 8.0f));
        }
        if (mechanics.HasFlag(MechanicCondition.Frozen))
            FreezeGrains(frozen);
        if (transform.parent != GameObject.FindGameObjectWithTag("UserInterface").GetComponent<RectTransform>())
        {
            transform.SetParent(GameObject.FindGameObjectWithTag("UserInterface").GetComponent<RectTransform>(), false);
        }
        //Add the methods needed for level reset and main menu navigation to the score buttons
            resetLevel.onClick.AddListener(player.RestartLevel);
            nextLevel.onClick.AddListener(player.ProgressLevel);
            stayLevel.onClick.AddListener(StayGame);
            finishLevel.onClick.AddListener(player.ProgressLevel);
        yield break;
    }

    public override void OnStartServer()
    {
        if (assigner == null)
        {
            assigner = GameObject.FindGameObjectWithTag("GameController").GetComponent<GrainNetworkAssigner>();
            Logs.Log(Barebones.Logging.LogLevel.Info, "Found the network assigner");
        }
        assigner.checkHighScore += ScoreListener;


        if(condition.HasFlag(CompletionCondition.Time))
        {
            conditionReminder.text = "Get to the target score within the time limit: " + completeScore;
            timer = StartCoroutine(TimeCounter(completeTime));
            EndGame = ForceGameOver;
            timeText = true;
        }

        if (condition.HasFlag(CompletionCondition.Score))
        {
            conditionReminder.text = "Get to the target score: " + completeScore;
            EndGame = OptionalGameOver;
        }
                
        if (condition.HasFlag(CompletionCondition.Moves))
        {
            EndGame = MovesGameOver;
            moveText = true;
            if (timeText)
                conditionReminder.text += " and within " + completeMoves + "moves";
            else
                conditionReminder.text = "Get to the target score within " + completeMoves + " moves";
        }
                

        if (condition.HasFlag(CompletionCondition.ScoreTarget))
                EndGame = ForceGameOver;


        if(mechanics.HasFlag(MechanicCondition.SimAnneal))
            simAnneal = StartCoroutine(SimulatedAnnealing(completeTime, 8.0f));

        if (mechanics.HasFlag(MechanicCondition.Frozen))
            FreezeGrains(frozen);

        Debug.Log(condition + " " + mechanics + " " + conditionReminder.text);
    }

    public IEnumerator TimeCounter(int timeLimit)
    {
        int seconds = 0;
        int i = timeLimit;
        while (i >= 0)
        {
            if (pauseTimer)
            {
                yield return null;
                continue;
            }
            int minutes = Math.DivRem(i, 60, out seconds);
            i--;
            RpcSendTime(minutes, seconds);
            yield return new WaitForSeconds(1.0f);
        }
        if(!sentGameOver)
        {
            RpcEndGame(CompletionCondition.Time.ToString());
            sentGameOver = true;
        }
        if(mechanics.HasFlag(MechanicCondition.SimAnneal))
        {
            StopCoroutine(simAnneal);
        }

    }
    public IEnumerator SimulatedAnnealing(int timeLimit, float annealingRate)
    {
        if (timeLimit == 0)
            timeLimit = 120;
        int i = timeLimit;

        while (i >= 0)
        {
            if (pauseTimer)
                yield return null;
            i-=(int)annealingRate;
            //Need to add annealing schedule here
            if(UnityEngine.Random.Range(0.0f,1.0f) < (float)i/timeLimit)
            {
                RandomizeGrain();
            }
            yield return new WaitForSeconds(annealingRate);
        }
    }
    private void RandomizeGrain()
    {
        int range = assigner.allGrains.Count;
        bool frozen = true;
        int grain = 0;
        int iter = 0;
        while(frozen && iter < range)
        {
            grain = UnityEngine.Random.Range(0, range - 1);
            frozen = assigner.allGrains[grain].hasPlayer;
            iter++;
        }
        StartCoroutine(WarningWiggle(grain));
    }
    private IEnumerator WarningWiggle(int grain)
    {
        //Insert wiggle here?
        float wiggleTime = 1.5f;
        float start = Time.time;
        float curTime = Time.time;
        float rotTime = Time.time;
        float rotAngle = 4f;
        float rotInt = wiggleTime / 30;
        Vector3 axis;
        float trash;
        assigner.allGrains[grain].transform.rotation.ToAngleAxis(out trash, out axis);
        assigner.allGrains[grain].hasPlayer = true;
        while (curTime < start + wiggleTime)
        {
            curTime = Time.time;
            if (curTime - rotTime > rotInt)
            {
                rotAngle *= -1.0f;
                rotTime = curTime;
                rotInt -= rotInt/2.0f;
            }
                
            assigner.allGrains[grain].transform.rotation *= Quaternion.AngleAxis(rotAngle, axis);
            yield return null;
        }
        assigner.allGrains[grain].transform.rotation = UnityEngine.Random.rotation;
        assigner.allGrains[grain].UpdateDiffusivities();
        GameObject[] oris = GameObject.FindGameObjectsWithTag("Orientation");
        UpdateTimeHistory(oris, "anneal", Time.time, Time.time);
        assigner.allGrains[grain].hasPlayer = false;
        assigner.sgtUpdated = false;
    }
    public void FreezeGrains(List<int> numFrozen)
    {
        for(int i=0;i<numFrozen.Count;i++)
        {
            assigner.allGrains[i].hasPlayer = true;
        }
    }
    public void UpdateTimeHistory(GameObject[] changedObjects, string type, float startTime, float endTime)
    {
        MinimalNetworkManager.singleton.writer.WriteLine("Server " + type + " " + startTime + " " + endTime);
        foreach (GameObject obj in changedObjects)
        {
            var rot = obj.transform.rotation;
            MinimalNetworkManager.singleton.writer.WriteLine(obj.name + " " + rot.w + " " + rot.x + " " + rot.y + " " + rot.z);
        }
    }
    [Server]
    public void CountMove()
    {
        movesTaken++;
        if (condition.HasFlag(CompletionCondition.Moves) && movesTaken >= completeMoves)
        {
            if (mechanics.HasFlag(MechanicCondition.SimAnneal))
            {
                StopCoroutine(simAnneal);
            }
            if (condition.HasFlag(CompletionCondition.Time))
            {
                StopCoroutine(timer);
            }
            if(!sentGameOver)
            {
                RpcEndGame(CompletionCondition.Moves.ToString());
                sentGameOver = true;
            }
                
            
        }
    }

    //Client SyncVar hook for updating the UI
    private void OnCountMove(int value)
    {
        if (!moveText)
            return;
        movesTaken = value;
        if(timeText)
            resourceRemainder.text = curMin + ":" + curSec.ToString("D2") + "  " + (completeMoves - movesTaken).ToString();
        else
            resourceRemainder.text = (completeMoves - movesTaken).ToString();

    }
    [ClientRpc]
    private void RpcSendTime(int minutes, int seconds)
    {
        if (!timeText)
            return;
        curMin = minutes;
        curSec = seconds;
        if(moveText)
            resourceRemainder.text = curMin + ":" + curSec.ToString("D2") + "  " + (completeMoves - movesTaken).ToString();
        else
            resourceRemainder.text = minutes + ":" + seconds.ToString("D2");
    }
    [ClientRpc]
    public void RpcEndGame(string cond)
    {
        StartCoroutine(WaitEndGame(cond));
    }

    public void EndGameSpecific(string cond)
    {
        CompletionCondition parsed;
        Enum.TryParse<CompletionCondition>(cond, out parsed);
        switch(parsed)
        {
            case CompletionCondition.Time:
                ForceGameOver();
                break;
            case CompletionCondition.Moves:
                MovesGameOver();
                break;
            default:
                OptionalGameOver();
                break;
        }
    }
    private IEnumerator WaitEndGame(string cond)
    {
        yield return null;
        EndGameSpecific(cond);
    }
    public void ForceGameOver()
    {
        //True game over because of a time limit expiration or because a target was met
        //Stop player input from happening
        foreach(PlayerController player in MinimalNetworkManager.singleton.client.connection.playerControllers)
        {
            player.gameObject.GetComponent<PlayerBehavior>().EndGameInput();
        }
        //Display the endgame screen
        for (int i = 0; i < completeStars.Length; i++)
        {
            completeStars[i].SetActive(stars[i].IsActive());
        }
        //Create case for level success and level failure
        if (completeStars[3].activeSelf)
        {
            completeBanner.text = "Incredible!";
            completeSubTitle.text = "You've even beaten the computer's score!";
            resetLevel.interactable = false;
            TriggerBedChange.singleton.EndLevelStinger(true);
        }
        else if (completeStars[0].activeSelf)
        {
            
            if (completeStars[2].activeSelf)
            {
                completeBanner.text = "Congratulations!";
                completeSubTitle.text = "Next try for a platinum star!";
            }
            else
            {
                completeBanner.text = "Good work!";
                completeSubTitle.text = "Next try for three stars!";
            }
            TriggerBedChange.singleton.EndLevelStinger(true);
        }
        else
        {
            completeBanner.text = "That's too bad!";
            completeSubTitle.text = "You ran out of time. Try again?";
            nextLevel.interactable = false;
            TriggerBedChange.singleton.EndLevelStinger(false);
        }
        //Display final screen for player input
        completeScreen.SetActive(true);
        stayLevel.interactable = false;
        
    }

    
    public void OptionalGameOver()
    {
        //Notification of completion of three stars, allow the user to continue if they want to
        foreach (PlayerController player in MinimalNetworkManager.singleton.client.connection.playerControllers)
        {
            player.gameObject.GetComponent<PlayerBehavior>().EndGameInput();
        }
        //Notify of the possibility of a platinum star
        for (int i = 0; i < 3; i++)
        {
            completeStars[i].SetActive(true);
        }
        resetLevel.interactable = false;
        nextLevel.interactable = true;
        stayLevel.interactable = true;
        completeBanner.text = "Congratulations on three stars!";
        completeSubTitle.text = "Would you like to try for a platinum star?";
        completeScreen.SetActive(true);
        EndGame = ForceGameOver;
        TriggerBedChange.singleton.EndLevelStinger(true);
    }

    public void StayGame()
    {
        foreach (PlayerController player in MinimalNetworkManager.singleton.client.connection.playerControllers)
        {
            player.gameObject.GetComponent<PlayerBehavior>().ResumeGameInput();
        }
        pauseTimer = false;
        completeScreen.SetActive(false);
        finishLevel.gameObject.SetActive(true);
    }
    public void MovesGameOver()
    {
       
        //True game over that states they are out of moves
        foreach (PlayerController player in MinimalNetworkManager.singleton.client.connection.playerControllers)
        {
            player.gameObject.GetComponent<PlayerBehavior>().EndGameInput();
        }
        resetLevel.interactable = true;
        stayLevel.interactable = false;
        nextLevel.interactable = true;
        //Display the endgame screen
        for (int i = 0; i < completeStars.Length; i++)
        {
            completeStars[i].SetActive(stars[i].IsActive());
        }
        //Create case for level success and level failure
        if (completeStars[3].activeSelf)
        {
            completeBanner.text = "Incredible!";
            completeSubTitle.text = "You've even beaten the computer's score!";
            resetLevel.interactable = false;
            TriggerBedChange.singleton.EndLevelStinger(true);
        }
        else if (completeStars[0].activeSelf)
        {

            if (completeStars[2].activeSelf)
            {
                completeBanner.text = "Congratulations!";
                completeSubTitle.text = "Next try for a platinum star!";
            }
            else
            {
                completeBanner.text = "Good work!";
                completeSubTitle.text = "Next try for three stars!";
            }
            TriggerBedChange.singleton.EndLevelStinger(true);
        }
        else
        {
            completeBanner.text = "That's too bad!";
            completeSubTitle.text = "You ran out of moves. Try again?";
            nextLevel.interactable = false;
            TriggerBedChange.singleton.EndLevelStinger(false);
        }

        
        completeScreen.SetActive(true);
    }

    public void PauseScoreListen()
    {
        pauseListener = !pauseListener;
    }
    [Server]
    public void ScoreListener()
    {
        if(pauseListener)
        {
            return;
        }
        RpcScoreListener(scoreToCompare);
        if (condition.HasFlag(CompletionCondition.ScoreTarget) && (completeTargetScore-tolerance < scoreToCompare && scoreToCompare < completeTargetScore+tolerance))
        {
            if(!sentGameOver)
            {
                RpcEndGame(CompletionCondition.Time.ToString());
                sentGameOver = true;
            }
            if (mechanics.HasFlag(MechanicCondition.SimAnneal))
            {
                StopCoroutine(simAnneal);
            }
            if (condition.HasFlag(CompletionCondition.Time))
            {
                StopCoroutine(timer);
            }
        }
        else if (scoreToCompare >= completeScore)
        {
            if (!sentGameOver)
            {
                RpcEndGame(CompletionCondition.Time.ToString());
                sentGameOver = true;
            }
            if (mechanics.HasFlag(MechanicCondition.SimAnneal))
            {
                StopCoroutine(simAnneal);
            }
            if (condition.HasFlag(CompletionCondition.Time))
            {
                StopCoroutine(timer);
            }
        }
        else if (scoreToCompare >= completeScore * thirdStar && !triggeredOptionalGO)
        {
            RpcEndGame(CompletionCondition.None.ToString());
            pauseTimer = true;
            triggeredOptionalGO = true;
        }
        
    }
    [ClientRpc]
    public void RpcScoreListener(float serverScore)
    {
        if (assigner == null)
            return;
        
        //Nested if to see what the currently displayed star should be
        if(serverScore >= completeScore * firstStar && !platStar)
        {
            if (!fStar)
            {
                stars[0].gameObject.SetActive(true);
                try
                {
                    TriggerBedChange.singleton.GotStar();
                }
                catch (Exception e) { Debug.Log(e.ToString()); }
                fStar = true;
            }
            
            if (serverScore >= completeScore * secondStar)
            {
                if (!sStar)
                {
                    stars[1].gameObject.SetActive(true);
                    try
                    {
                        TriggerBedChange.singleton.GotStar();
                    }
                    catch(Exception e) { Debug.Log(e.ToString()); }
                    
                    sStar = true;
                }
                

                if (serverScore >= completeScore * thirdStar)
                {
                    if (!tStar)
                    {
                        stars[2].gameObject.SetActive(true);
                        try
                        {
                            TriggerBedChange.singleton.GotStar();
                        }
                        catch (Exception e) { Debug.Log(e.ToString()); }
                        tStar = true;
                    }
                    
                    if (serverScore >= completeScore)
                    {
                        //Give platinum star here
                        for (int i = 0; i < 3; i++)
                        {
                            stars[i].gameObject.SetActive(false);
                        }
                        stars[3].gameObject.SetActive(true);
                        platStar = true;
                        try
                        {
                            TriggerBedChange.singleton.GotStar();
                        }
                        catch (Exception e) { Debug.Log(e.ToString()); }
                    }
                }
            }
        }

    }

    [Server]
    public void RestartLevel()
    {
        //Doesn't work
        //MinimalNetworkManager.singleton.ServerChangeScene(SceneManager.GetActiveScene().name);
        //Reset the stars for the level
        
        foreach(Image star in stars)
        {
            star.gameObject.SetActive(false);
        }
        //Reset the level condition
        if (condition.HasFlag(CompletionCondition.Time))
        {
            conditionReminder.text = "Get to the target score within the time limit: " + completeScore;
            timer = StartCoroutine(TimeCounter(completeTime));
            EndGame = ForceGameOver;
        }
        else if (condition.HasFlag(CompletionCondition.Score))
        {
            conditionReminder.text = "Get to the target score: " + completeScore;
            EndGame = OptionalGameOver;
        }

        if (condition.HasFlag(CompletionCondition.Moves))
        {
            EndGame = ForceGameOver;
            movesTaken = 0;
        }
            

        if (condition.HasFlag(CompletionCondition.ScoreTarget))
            EndGame = ForceGameOver;


        if (mechanics.HasFlag(MechanicCondition.SimAnneal))
        {
            simAnneal = StartCoroutine(SimulatedAnnealing(completeTime, 8.0f));
        }
            

        if (mechanics.HasFlag(MechanicCondition.Frozen))
            FreezeGrains(frozen);
        //Give control back to the player
        pauseTimer = false;
        sentGameOver = false;
        RpcResumePlayerControl();
    }

    [ClientRpc]
    public void RpcResumePlayerControl()
    {
        completeScreen.SetActive(false);
        if (condition.HasFlag(CompletionCondition.Time))
        {
            conditionReminder.text = "Get to the target score within the time limit: " + completeScore;
            //TEST THIS RIGHT HERE!!!!!!!!!! May not need this line cause of RPC from server
            //StartCoroutine(TimeCounter(completeTime));
            EndGame = ForceGameOver;
            timeText = true;
        }

        else if (condition.HasFlag(CompletionCondition.Score))
        {
            conditionReminder.text = "Get to the target score: " + completeScore;
            EndGame = OptionalGameOver;
        }

        if (condition.HasFlag(CompletionCondition.Moves))
        {
            EndGame = MovesGameOver;
            moveText = true;
            if (timeText)
                conditionReminder.text += " and within " + completeMoves + "moves";
            else
                conditionReminder.text = "Get to the target score within " + completeMoves + " moves";
        }

        if (condition.HasFlag(CompletionCondition.ScoreTarget))
            EndGame = ForceGameOver;


        if (mechanics.HasFlag(MechanicCondition.SimAnneal))
        {
            //Same reason as above, may not need cause of RPC
            //StartCoroutine(SimulatedAnnealing(completeTime, 8.0f));
        }
        if (mechanics.HasFlag(MechanicCondition.Frozen))
            FreezeGrains(frozen);
        foreach (PlayerController player in MinimalNetworkManager.singleton.client.connection.playerControllers)
        {
            player.gameObject.GetComponent<PlayerBehavior>().ResumeGameInput();
        }
    }
}
