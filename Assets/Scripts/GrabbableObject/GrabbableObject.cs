using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabbableObject : MonoBehaviour
{
    [SerializeField] float lerpSpeed = 15f; // Control speed of smoothed movement when grabbed
    private Rigidbody rb;
    private Transform grabPoint;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if(rb == null)
        {
            Debug.LogError("Rigidbody not found on grabbable object!");
        }
    }

    public void Grab(Transform grabPoint)
    {
        this.grabPoint = grabPoint;
        rb.useGravity = false;
    }

    public void Drop()
    {
        this.grabPoint = null;
        rb.useGravity = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.collider.gameObject.CompareTag("Player"))
        {
            return;
        }
        if(collision.collider.gameObject.transform.root.CompareTag("Enemy"))
        {
            //TODO: define behavior if player throws object at enemy
            //      note: no longer considered "thrown by player" once it reaches 0 velocity after being let go by player
            return;
        }
        HearingManager.Instance.OnSoundEmitted(gameObject, transform.position, EHeardSoundCategory.EObjectCollision, 5f);
    }

    private void FixedUpdate()
    {
        if(grabPoint != null)
        {
            Vector3 newPosition = Vector3.Lerp(transform.position, grabPoint.position, Time.deltaTime * lerpSpeed);
            rb.MovePosition(newPosition);
        }
    }

}
