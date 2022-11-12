using System.Collections;
using Mirror;
using UnityEngine;

public class MeshTrail : NetworkBehaviour
{
    private MyPlayerController _myPlayerController;

    public GameObject prefab;
    public float activeTime = 0.07f;
    
    [Header("Mesh Related")]
    public float meshRefreshRate = 0.1f;
    public float meshDestroyDelay = 0.001f;
    public Transform positionToSpawn;

    [Header("Shader Related")] 
    public Material mat;
    private SkinnedMeshRenderer[] _skinnedMeshRenderers;
    
    public bool isTrailActive;
    
    private void Start()
    {
        if (!_myPlayerController) _myPlayerController = GetComponent<MyPlayerController>();
        if (!positionToSpawn) positionToSpawn = transform;
    }

    public IEnumerator ActiveTrail(float timeActive)
    {
        while (timeActive > 0)
        {
            timeActive -= meshRefreshRate;

            if (_skinnedMeshRenderers == null)
                _skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();

            for (int i = 0; i < _skinnedMeshRenderers.Length; i++)
            {
                SpawnTrail(i);
            }
            
            yield return new WaitForSeconds(meshRefreshRate);
        }

        isTrailActive = false;
    }

    
    private void SpawnTrail(int i) 
    {
        GameObject go = Instantiate(prefab);
        go.transform.SetPositionAndRotation(positionToSpawn.position, positionToSpawn.rotation);

        var meshRenderer = go.GetComponent<MeshRenderer>();
        var meshFilter = go.GetComponent<MeshFilter>();

        var mesh = new Mesh();
        _skinnedMeshRenderers[i].BakeMesh(mesh);

        meshRenderer.material = mat;
        meshFilter.mesh = mesh;

        //NetworkServer.Spawn(go);  //была попытка синхронизировать trail, но mesh не хочет передаваться...
        Destroy(go, meshDestroyDelay);
        Destroy(mesh, meshDestroyDelay);
    }
}
