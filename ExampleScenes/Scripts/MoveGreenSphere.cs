using UnityEngine;
using System.Collections;

public class MoveGreenSphere : MonoBehaviour {

    public float speed = 1.0f;

    // Use this for initialization
    void Start() {}

    // Update is called once per frame
    void Update() {
        transform.position = new Vector3(0.75f + Mathf.Sin(Time.time * speed) * 1.5f, 1.5f + Mathf.Cos(Time.time * speed) * 1f, 0.5f);
    }
}