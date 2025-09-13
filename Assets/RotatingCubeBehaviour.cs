using UnityEngine;

public sealed class RotatingCubeBehaviour : MonoBehaviour
{
    private void Start()
    {
        Resources.UnloadUnusedAssets();
    }

    private void Update()
    {
        transform.Rotate(new Vector3(180, 180, 0) * Time.deltaTime);
    }
}
