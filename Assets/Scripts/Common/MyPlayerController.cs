using System.Collections;
using System.Linq;
using Cinemachine;
using Mirror;
using TMPro;
using UnityEngine;

public class MyPlayerController : NetworkBehaviour
{
    [SyncVar(hook = nameof(UpdatePlayerName))] public string playerName;

    //components
    private Rigidbody _rigidbody;
    private Animator _animator;
    private CinemachineFreeLook _cinemachineFreeLook;
        
    [Header("Components")]
    public Transform cam;
    public SkinnedMeshRenderer meshRenderer;
    public MeshTrail meshTrail;
    public TMP_Text playerNameText;

    [Header("Options")] 
    [SyncVar(hook = nameof(CheckPlayerWin))] public int hitCount = 0;
    [SyncVar(hook = nameof(UpdateColor))] public Color playerColor;
    
    [Header("Invincibility Mode")]
    public float invincibilityModeDuration = 3f;
    [SyncVar] public bool isInvincibilityMode;
    
    [Header("Movements")]
    public float speed = 6f;
    public float turnSmoothTime = 0.01f;
    
    [SyncVar] public bool isDashing;
    [Range(0.1f, 20f)] public float dashDistance = 5f;
    
    private float _turnSmoothVelocity;
    private static readonly int SpeedAnim = Animator.StringToHash("speed");
    private float _horizontal;
    private float _horizontalRaw;
    private float _vertical;
    private float _verticalRaw;
    private Vector3 _direction;
    private Vector3 _moveDir;

    public override void OnStartClient()
    {
        base.OnStartClient();
        
        //set player name
        var players = FindObjectsOfType<NetworkRoomPlayer>().ToList();
        if (isClient && isOwned)
            CmdSetPlayerName(players.Find(p=>p.isOwned).playerName);
        
        //set camera target
        if (isOwned)
        {
            _cinemachineFreeLook.Follow = transform;
            _cinemachineFreeLook.LookAt = transform;
        }
    }

    private void Awake()
    {
        //initializations
        if (!cam) cam = Camera.main.transform;
        if (!_rigidbody) _rigidbody = GetComponent<Rigidbody>();
        if (!_animator) _animator = GetComponent<Animator>();
        if (!meshRenderer) meshRenderer = transform.GetComponentInChildren<SkinnedMeshRenderer>();
        if (!_cinemachineFreeLook) _cinemachineFreeLook = FindObjectOfType<CinemachineFreeLook>();
    }

    void Update()
    {
        if (!isOwned) return;
        
        if (Input.GetMouseButtonDown(0)) StartCoroutine(Dash());
    }

    private void FixedUpdate()
    {
        if (!isOwned) return;

        UpdateAnimator();
        
        #region Movement/Rotation
        
        _horizontalRaw = Input.GetAxisRaw("Horizontal");
        _verticalRaw = Input.GetAxisRaw("Vertical");
        var directionRawNormalized = new Vector3(_horizontalRaw, 0, _verticalRaw).normalized;

        if (directionRawNormalized.magnitude >= 0.05f)
        {
            if (!isDashing)
            {
                float targetAngle = Mathf.Atan2(directionRawNormalized.x, directionRawNormalized.z) * Mathf.Rad2Deg +
                                    cam.eulerAngles.y;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity,
                    turnSmoothTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);


                _moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

                Move();
            }
        }
        else Move(0);
        
        #endregion
    }


    private void OnCollisionEnter(Collision hit)
    {
        if (!hit.gameObject.CompareTag("Player") || isInvincibilityMode || !isOwned) return;
        if (hit.gameObject.GetComponent<MyPlayerController>().isInvincibilityMode) return;

        if (isDashing)
        {
            if (isServer) AddHitCount();
            else CmdAddHitCount();
        }
        else if (hit.gameObject.GetComponent<MyPlayerController>().isDashing)
        {
            StartCoroutine(GotDamageRoutine());
        }
    }

    private void UpdateAnimator()
    {
        _horizontal = Input.GetAxis("Horizontal");
        _vertical = Input.GetAxis("Vertical");
        _direction = new Vector3(_horizontal, 0, _vertical);

        if (_direction.magnitude > Mathf.Abs(0.05f))
        {
            _animator.SetFloat(SpeedAnim, Vector3.ClampMagnitude(_direction, 1).magnitude);
        }
    }
    
    private void Move(float multiSpeed = 1)
    {
        _rigidbody.velocity = _moveDir * (speed * multiSpeed);
    }

    private IEnumerator Dash()
    {
        if (isDashing || _direction.magnitude < Mathf.Abs(0.1f)) yield break;
        
        if (isServer) SetDashBool(true);
        else CmdSetDashBool(true);
        isDashing = true;

        //init
        if (!isInvincibilityMode)
            CmdSetColor(Color.green);
        var timeStart = Time.time;
        var startPoint = transform.position;
        
        if (isDashing && !meshTrail.isTrailActive) //trail mesh on
        {
            meshTrail.isTrailActive = true;
            StartCoroutine(meshTrail.ActiveTrail(meshTrail.activeTime));
        }

        while (isDashing) // dash move
        {
            Move(dashDistance/3f);
            
            if (Vector3.Distance(transform.position, startPoint) >= dashDistance || Time.time - timeStart > 0.1f) break;
            
            yield return null;
        }

        yield return new WaitForSeconds(0.15f); //dash delay

        if (!isInvincibilityMode)
            CmdSetColor(Color.white);
        
        if (isServer) SetDashBool(false);
        else CmdSetDashBool(false);
        isDashing = false;
    }

    
    [Server]
    private void SetDashBool(bool isOn)
    {
        isDashing = isOn;
    }
    [Command]
    private void CmdSetDashBool(bool isOn)
    {
        SetDashBool(isOn);
    }
    
    
    [Server]
    private void AddHitCount()
    {
        hitCount++;
    }
    [Command]
    private void CmdAddHitCount()
    {
        AddHitCount();
    }

    
    [Server]
    private void SetColor(Color newColor)
    {
        playerColor = newColor;
    }
    [Command]
    private void CmdSetColor(Color newColor)
    {
        SetColor(newColor);
    }

    
    [Command]
    private void CmdSetPlayerName(string playerN)
    {
        playerName = playerN;

        if (playerName == "" || playerName == "Player")
            playerName = "Player" + NetworkServer.connections.Count;
    }

    
    [Server]
    private void SetInvincibilityMode(bool isOn)
    {
        isInvincibilityMode = isOn;
        SetColor(isOn ? Color.red : Color.white);
    }
    [Command]
    private void CmdSetInvincibilityMode(bool isOn)
    {
        SetInvincibilityMode(isOn);
    }
    
    
    private IEnumerator GotDamageRoutine()
    {
        print("GotDamage");
        if (isServer)
        {
            SetInvincibilityMode(true);
        }
        else
        {
            CmdSetInvincibilityMode(true);
        }
        
        yield return new WaitForSeconds(invincibilityModeDuration);
        
        if (isServer)
        {
            SetInvincibilityMode(false);
        }
        else
        {
            CmdSetInvincibilityMode(false);
        }
    }

    #region Hooks

    private void UpdatePlayerName(string oldString, string newString)
    {
        playerNameText.text = newString;
    }
    
    private void UpdateColor(Color oldColor, Color newColor)
    {
        meshRenderer.material.color = newColor;
        playerNameText.color = newColor;
    }

    private void CheckPlayerWin(int oldInt, int newInt)
    {
        if (newInt == 3)
        {
            StartCoroutine(GameManager.Instance.PlayerWin(this));
        }
    }
    
    #endregion
}
