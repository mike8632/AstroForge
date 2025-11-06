using System;
using UnityEngine;

[RequireComponent(typeof(SaveableEntity))]
public class PlayerSave : MonoBehaviour, ISaveable
{
    [Serializable]
    private struct Data
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    public object CaptureState()
    {
        return new Data
        {
            position = transform.position,
            rotation = transform.rotation
        };
    }

    public void RestoreState(object state)
    {
        if (state is Data d)
        {
            var rb = GetComponent<Rigidbody>();
            var rb2d = GetComponent<Rigidbody2D>();

            transform.SetPositionAndRotation(d.position, d.rotation);

            if (rb)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            if (rb2d)
            {
                rb2d.linearVelocity = Vector2.zero;
                rb2d.angularVelocity = 0f;
            }
        }
    }
}