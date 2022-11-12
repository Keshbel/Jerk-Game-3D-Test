using UnityEngine;

public class LockCursorUnity : MonoBehaviour
{
    private void Start()
    {
        Cursor.lockState = CursorLockMode.Confined;
    }
}
