using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gravity : MonoBehaviour
{
    [Header("Gravity stuff")]
    [Range(-50f,0)] public float gravity;

    private CharacterController _charCont;

    private void Awake() => _charCont = gameObject.GetComponent<CharacterController>();

    public Vector3 ApplyGravity(float mass, Vector3 movement)
    {
        _charCont.yAcceleration += mass * gravity * Time.deltaTime;
        movement.y = _charCont.yAcceleration * (movement.y < 0 ? _charCont.downMultiplier : 1.0f) * Time.deltaTime;
        return movement;
    }
}
