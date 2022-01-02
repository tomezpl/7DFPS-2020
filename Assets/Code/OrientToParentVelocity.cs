using UnityEngine;

public class OrientToParentVelocity : MonoBehaviour
{
    public Transform parentTransform;
    public Rigidbody Rigidbody;

    /// <summary>
    /// The velocity above which velocity-based rotation should be performed.
    /// </summary>
    public float MinVelocityMagnitude = 0.5f;

    /// <summary>
    /// The maximum absolute dot product between the parent and child's forward vectors below which rotation should be performed.
    /// </summary>
    public float MaxForwardDotProduct = 0.99f;

    // Start is called before the first frame update
    void Start()
    {
        parentTransform ??= transform.parent;

        Rigidbody ??= parentTransform.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 velocity = Rigidbody.velocity;
        Vector3 velocityNormalized = velocity.normalized;

        if(velocity.magnitude >= MinVelocityMagnitude && Mathf.Abs(Vector3.Dot(transform.forward, velocityNormalized)) < MaxForwardDotProduct)
        {
            transform.rotation *= Quaternion.FromToRotation(transform.forward, velocityNormalized);
        }
    }
}
