using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] public GameObject bulletPrefab;
    [SerializeField] public Transform bulletSpawn;
    private GameManager gameManager;

    public NetworkPlayer NetworkPlayer;
    public bool IsStunned { get; private set; }
    public bool HasFlag { get { return gameManager.FlagHolder == this; } }

    private float speedAdd = 1f;

    public override void OnStartLocalPlayer()
    {
        GetComponentInChildren<MeshRenderer>().material.color = Color.blue;
    }

    private void Awake()
    {
        gameManager = FindObjectOfType<GameManager>();
    }

    private void Start()
    {
        gameManager.SendMessage("OnPlayerJoined", this, SendMessageOptions.DontRequireReceiver);
    }

    private void Update()
    {
        if (!isLocalPlayer || NetworkPlayer == null)
        {
            return;
        }

        if(Input.GetButtonDown("Pause"))
        {
            gameManager.SendMessage("PauseGame");
        }

        if(!IsStunned)
        {
            var x = Input.GetAxis("Horizontal") * Time.deltaTime * 150.0f;
            var z = Input.GetAxis("Vertical") * Time.deltaTime * 3.0f * speedAdd;

            transform.Rotate(0, x, 0);
            transform.Translate(0, 0, z);

			if (Input.GetKeyDown(KeyCode.Space))
			{
				CmdFire();
			}
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("Bullet"))
        {
            gameManager.SendMessage("DropFlag", null, SendMessageOptions.DontRequireReceiver);
        }
        else if (other.gameObject.CompareTag("Flag"))
        {
            gameManager.SendMessage("PickupFlag", this, SendMessageOptions.DontRequireReceiver);
        }

        if(isLocalPlayer)
        {
            if (other.gameObject.CompareTag("SpeedUp") && HasFlag)
            {
                Destroy(other.gameObject);
                StartCoroutine(SpeedRun());
            }
        }
    }

    private IEnumerator SpeedRun()
    {
        speedAdd = 2f;
        yield return new WaitForSeconds(10f);
        speedAdd = 1f;
    }
		
	[Command]
	void CmdFire()
	{
		// Create the Bullet from the Bullet Prefab
		var bullet = (GameObject)Instantiate(
			bulletPrefab,
			bulletSpawn.position,
			bulletSpawn.rotation);

		// Add velocity to the bullet
		bullet.GetComponent<Rigidbody>().velocity = bullet.transform.forward * 30;

		// Spawn the bullet on the Clients
		NetworkServer.Spawn(bullet);

		// Destroy the bullet after 2 seconds
		Destroy(bullet, 2.0f);
	}
}