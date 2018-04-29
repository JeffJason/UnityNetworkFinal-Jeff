using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class GameManager : NetworkManager
{
    public enum GameState
    {
        Lobby,
        Game,
        WinState
    }

    private Dictionary<uint, PlayerController> players = new Dictionary<uint, PlayerController>();

    public GameState State { get; private set; }

    public int totalPlayers = 3;
    public float timerLeft = 30f;

    public bool IsHost
    {
        get
        {
            if(players.Count == 0)
            {
                return false;
            }
            else
            {
                PlayerController playercontrol = players.Values.ToList()[0];
                return playercontrol != null && playercontrol.isLocalPlayer;
            }
        }
    }

    [SerializeField] private GameObject timer;
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject winMenu;

    [SerializeField] private GameObject Flag;
    [SerializeField] private GameObject speedPickUp;

    public GameObject FlagObject;
    public PlayerController FlagHolder;

    public GameObject SpeedPickUpObject;

    public void Reset()
    {
        foreach (var player in players)
        {
            if (player.Value == null)
            {
                continue;
            }
            Destroy(player.Value.gameObject);
        }

        players.Clear();

        if(FlagObject != null)
        {
            Destroy(FlagObject);
        }
        
        timerLeft = 30f;
        State = GameState.Lobby;
    }
    
    private void OnDisable()
    {
        StopHost();
    }

    public override void OnStartHost()
    {
        base.OnStartHost();
    }

    public override void OnStartClient(NetworkClient client)
    {
        base.OnStartClient(client);
    }

    public override void OnStopHost()
    {
        base.OnStopHost();
        this.SendMessage("Reset", SendMessageOptions.DontRequireReceiver);
        this.mainMenu.SetActive(true);
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        this.SendMessage("Reset", SendMessageOptions.DontRequireReceiver);
        this.mainMenu.SetActive(true);
    }

    public override void OnClientDisconnect(NetworkConnection conn)
    {
        base.OnClientDisconnect(conn);
        this.SendMessage("Reset", SendMessageOptions.DontRequireReceiver);
        this.mainMenu.SetActive(true);
    }
    
    private void Update()
    {
        if(State == GameState.Lobby)
        {
            timer.SetActive(false);

            if (players.Count >= totalPlayers)
            {
                Debug.Log("Total players have reached (" + totalPlayers.ToString() + "). ");

                State = GameState.Game;
            }
        }
        else if (State == GameState.Game)
        {
            timer.SetActive(true);

            SpawnFlag();
            SpawnSpeedPickUp();
            
            if (FlagObject != null && FlagHolder != null)
            {
                FlagObject.transform.position = FlagHolder.transform.position + Vector3.up;

                timerLeft -= Time.deltaTime;
                timer.GetComponentInChildren<Text>().text = "TIME: " + Mathf.RoundToInt(timerLeft).ToString();
            }

            if(timerLeft < Mathf.Epsilon)
            {
                timerLeft = 0f;
                Debug.Log("GAME OVER. WINNER! Player " + FlagHolder.netId);

				State = GameState.WinState;
            }
        }
        else if (State == GameState.WinState)
        {
            foreach (var player in players)
            {
                player.Value.enabled = false;
            }
            GameWinner(players.Values.ToList().IndexOf(FlagHolder) + 1);
        }
    }
    
    private void StartGame()
    {
        StartHost();        
    }

    private void JoinGame()
    {
        StartCoroutine(JoinGameRoutine());
    }

    private void RestartGame()
    {
        if(IsHost)
        {
            StartGame();
        }
        else
        {
            JoinGame();
        }
    }

    private IEnumerator JoinGameRoutine()
    {
        var client = StartClient();

        yield return new Waiting(3f, () => client.isConnected);

        if (!client.isConnected)
        {
            yield return new WaitForSeconds(2f);
            LeaveGame();
        }
    }

    private void LeaveGame()
    {
        if(IsHost)
        {
            StopHost();
        }
        else
        {
            StopClient();
        }
        gameObject.SendMessage("Reset", SendMessageOptions.DontRequireReceiver);
        mainMenu.SetActive(true);
    }

    private void PauseGame()
    {
        pauseMenu.SetActive(true);
    }

    private void PlayAgain()
    {
        gameObject.SendMessage("Reset", SendMessageOptions.DontRequireReceiver);

        LeaveGame();
        RestartGame();
    }

    private void GameWinner(int whoWon)
    {
       winMenu.SetActive(true);
       winMenu.GetComponentInChildren<Text>().text = "WINNER! Player " + whoWon.ToString();
    }
		
    private void QuitGame()
    {
        Application.Quit();
    }

    private void OnPlayerJoined(PlayerController player)
    {        
        if(player.netId.IsEmpty() || players.ContainsKey(player.netId.Value))
        {
            return;
        }

        Debug.Log("Player " + player.netId.Value + " has join the game.");

        players.Add(player.netId.Value, player);
    }

    private void SpawnFlag()
    {
        if(Flag != null && FlagObject == null)
        {
            ClientScene.RegisterPrefab(Flag);
            FlagObject = Instantiate<GameObject>(Flag, Vector3.zero, Quaternion.identity);
            NetworkServer.Spawn(FlagObject);    
        }
    }

    private void SpawnSpeedPickUp()
    {
        if (speedPickUp != null && SpeedPickUpObject == null)
        {
            if(IsHost)
            {
                Vector3 spot = new Vector3(Random.Range(-10f, 10f), 0f, Random.Range(-10f, 10f));
                ClientScene.RegisterPrefab(speedPickUp);
                SpeedPickUpObject = Instantiate<GameObject>(speedPickUp, spot, Quaternion.identity);
                NetworkServer.Spawn(SpeedPickUpObject);
            }
        }
    }

    private void PickupFlag(PlayerController who)
    {
        if(FlagHolder == null)
        {
            FlagHolder = who;
        }
    }

    private void DropFlag()
    {
        FlagHolder = null;
        Destroy(FlagObject);
    }
}
