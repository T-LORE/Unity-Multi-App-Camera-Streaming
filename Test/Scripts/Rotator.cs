using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotator : MonoBehaviour
{
    public float rotationSpeed = 10f;


    void Update()
    {
        // diagonally rotate the game object around the Y-axis and X-axis


        gameObject.transform.Rotate(
                       new Vector3(-rotationSpeed * Time.deltaTime, -rotationSpeed * Time.deltaTime, 0f),
                                  Space.World
                                         );
    }
}
