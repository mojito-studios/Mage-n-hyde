using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;
using TouchPhase = UnityEngine.TouchPhase;

public class Player : NetworkBehaviour
{
    private Camera _camera;
    private float speed = 4f;
    private bool _moving = false;
    private bool _hiding = false;
    public bool button = false;
    public FixedString128Bytes teamAssign = new FixedString128Bytes();
    private Dictionary<ulong, GameObject> playerHidingObjects = new Dictionary<ulong, GameObject>();
    [SerializeField] private Sprite[] allSprites;
    private Vector3 targetPosition = Vector3.zero;
    [HideInInspector] public PropsBehaviour pBehaviour;
    private Button _spell;
    private Tower teamTower;


    void Start()
    {
        _camera = GetComponentInChildren<Camera>();
        Debug.Log(_camera);
        allSprites[0] = GetComponent<SpriteRenderer>().sprite;
        Button[] buttonList = GetComponentsInChildren<Button>();
        foreach (var button in buttonList)
        {
            if (button.CompareTag("AttackButton")) { _spell = GetComponentInChildren<Button>(); }
        }
        teamAssign = "Team1";
        AssignTower();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        SetPlayer();
    }


    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;
        if (!button)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                if (Input.GetTouch(i).phase == TouchPhase.Began)
                {
                    targetPosition = _camera.ScreenToWorldPoint(Input.GetTouch(i).position);
                    targetPosition.z = transform.position.z;
                    _moving = true;
                }
            }
        }
        if (_moving)
            MovePlayer(); //Ahora se mueve por tener una transform autoritativa de parte del cliente para mejor responsividad a los jugadores
    }

    void SetPlayer()
    {
        PlayerInput playerInput = GameObject.Find("@PlayerInput").GetComponent<PlayerInput>();

        playerInput.actionEvents[0].AddListener(this.OnMovement);
        playerInput.actionEvents[1].AddListener(this.OnHide);
    }

    private void AssignTower()
    {
        if (teamAssign == "Team1") teamTower = GameObject.FindGameObjectWithTag("Team1Tower").GetComponent<Tower>();
        else teamTower = GameObject.FindGameObjectWithTag("Team2Tower").GetComponent<Tower>();
       // Debug.Log("Soy cliente? " + IsClient + " mi torre es " + teamTower.tag);
    }
    void MovePlayer()
    {
       
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, Time.deltaTime * speed); 
        if(targetPosition!= transform.position)
        {
            Vector3 targetDirection = targetPosition - transform.position; 
            transform.up = targetDirection;
        }
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            _moving = false;
        }
    }

    

    //En el host funcionan todas las cosas sin problema y se ven los cambios reflejados en el otro cliente
    public void OnMovement(InputAction.CallbackContext context) //Se activa al hacer click izquierdo
    {
        if (!IsOwner) return;
        Debug.Log("Moving");

        Vector3 position = Mouse.current.position.ReadValue();
        targetPosition = _camera.ScreenToWorldPoint(position);
        targetPosition.z = transform.position.z;
        _moving = true;
    }


    
    public void OnHide(InputAction.CallbackContext context) //Se activa al hacer click derecho cuando est�s encima de un prop
    {
        if (!IsOwner || _hiding) return;
        Debug.Log(GetComponent<SpriteRenderer>().sprite);
        Debug.Log("Distancia " + Vector3.Distance(transform.position, pBehaviour.transform.position));
        if (Vector3.Distance(transform.position, pBehaviour.transform.position) < 3f)
        {
            
            OnHideRpc(pBehaviour.spriteNumber, pBehaviour.NetworkObjectId, pBehaviour.timeHiding);
            pBehaviour = null;
        }
    }

    [Rpc(SendTo.Server)]
    private void OnHideRpc(int spriteN, ulong NID, float time)
    {
        _hiding = true;
        ChangeSpriteRpc(spriteN, NID);
        StartCoroutine(HideCoroutine(time, NID)); //Cambiar el hardcode por el tiempo que vaya a durar el objeto seg�n su SO

    }

    [Rpc(SendTo.Everyone)]
    private void ChangeSpriteRpc(int spriteNumber, ulong NID)
    {
        Debug.Log("Cambio de sprite");
        GetComponent<SpriteRenderer>().sprite = allSprites[spriteNumber]; //Voy a ignorar el tema del color porque en principio solo se cambia ahora por ser placeholders 
        var hideGO = NetworkManager.Singleton.SpawnManager.SpawnedObjects[NID].gameObject;
        hideGO.gameObject.SetActive(!hideGO.gameObject.activeSelf);

    }
    private IEnumerator HideCoroutine(float time, ulong NID)
    {
        yield return new WaitForSeconds(time);
        _hiding = false;
        ChangeSpriteRpc(0, NID);
       
    }
    public void OnAttack(InputAction.CallbackContext context) //Se activa al hacer click izquierdo si no est�s encima de un prop
    {
        if (!IsOwner) return;
        Debug.Log("attacking");
        

    }

    public ulong GetTeamTower()
    {
        return teamTower.NetworkObjectId;
    }


}