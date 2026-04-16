using UnityEngine;

public class MarbleReset : MonoBehaviour
{
    private Vector3 startPosition;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        startPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ResetPosition()
    {
        transform.position = startPosition;
        var rb = this.GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = startPosition;
    }
}
