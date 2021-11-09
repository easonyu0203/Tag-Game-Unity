using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.Messaging;
using System;

public class NetworkRopeGun : NetworkBehaviour
{
    [Tooltip("Amount of force to player when rope grap somthing")]
    [SerializeField] float _ropeFirstBurstVelocity;
    [SerializeField] float _ropeAccelerate;
    [SerializeField] float _additiveGravityScale = 2f;
    

    [SerializeField] GameObject _ropePrefab;
    [SerializeField] Transform _shootPoint;
    [Header("Listening Channel")]
    [SerializeField] GameObjectEventChannelSO _serverHitWallEvent;
    [SerializeField] GameObjectEventChannelSO _serverRopeHitOtherPlayerEvent;

    private GameObject _myRope = null;
    private Rigidbody _rigidbody;

    public void ShootRope(Vector3 ropeDirection){
        RequestShootRopeServerRpc(ropeDirection);
    }

    public void CancelRope(){
        RequestCancelRopeServerRpc();
    }

    private void OnEnable() {
        _serverHitWallEvent.OnEventRaised += OnServerHitWall;
    }

    private void OnDisable() {
        _serverHitWallEvent.OnEventRaised -= OnServerHitWall;
    }

    private void Awake() {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void OnServerHitWall(GameObject rope)
    {
        // tell this client that his character's rope hit wall
        if(rope != _myRope) return;
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[]{OwnerClientId}
            }
        };
        HandleLocalRopeHitWallClientRpc(clientRpcParams);
    }

    [ClientRpc]
    private void HandleLocalRopeHitWallClientRpc(ClientRpcParams clientRpcParams){
        Debug.Log("Apply force to local player");
        Vector3 endpoint = _myRope.GetComponent<RopeBehaviour>().EndPoint;
        StartCoroutine(ApplyForceToEndPoint(endpoint));
    }

    private IEnumerator ApplyForceToEndPoint(Vector3 endPoint)
    {
        Vector3 direction = (endPoint - transform.position).normalized;
        _rigidbody.AddForce(direction * _ropeFirstBurstVelocity, ForceMode.VelocityChange);
        yield return new WaitForFixedUpdate();
        while(_myRope != null){
            direction = (endPoint - transform.position).normalized;
            direction = new Vector3(direction.x, 0, direction.z).normalized;
            _rigidbody.AddForce(direction * _ropeAccelerate, ForceMode.Acceleration);
            _rigidbody.AddForce(Vector3.down * 9.8f * _additiveGravityScale, ForceMode.Acceleration);
            
            yield return new WaitForFixedUpdate();  
        }
    }

    /// Shoot Rope Net code 

    [ServerRpc]
    private void RequestShootRopeServerRpc(Vector3 ropeDirection){
        Debug.Log("Server get shoot request");
        HandlerShootRopeClientRpc(ropeDirection);
        if(IsHost == false){
            if(_myRope != null){
               Destroy(_myRope);
            }
            _myRope = Instantiate(_ropePrefab);
            _myRope.GetComponent<RopeBehaviour>().Init(_shootPoint, ropeDirection, gameObject);          
        }
    }

    [ClientRpc]
    private void HandlerShootRopeClientRpc(Vector3 ropeDirection){
        Debug.Log("Client handle shoot rope");
        if(_myRope != null){
            Destroy(_myRope);
        }
        _myRope = Instantiate(_ropePrefab);
        _myRope.GetComponent<RopeBehaviour>().Init(_shootPoint, ropeDirection, gameObject);
    }

    /// Cancel Rope Net code
    [ServerRpc]
    private void RequestCancelRopeServerRpc(){
        Debug.Log("Server get cancel request");
        HandleCancelRopeClientRpc();
        if(IsHost == false){
            if(_myRope != null){
                Destroy(_myRope);
                _myRope = null;
            }
        }
    }

    [ClientRpc]
    private void HandleCancelRopeClientRpc(){
        Debug.Log("Client handle cancel rope");
        if(_myRope != null){
            Destroy(_myRope);
            _myRope = null;
        }
    }


}
