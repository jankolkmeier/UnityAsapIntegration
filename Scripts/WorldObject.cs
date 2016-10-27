using UnityEngine;
using System.Collections;

namespace ASAP {

    public class WorldObject : MonoBehaviour {

        private VJoint vjoint;

        // Use this for initialization
        void Start() {
            vjoint = new VJoint(transform.name, transform.position, transform.rotation);
            FindObjectOfType<ASAPManager>().OnWorldObjectInitialized(vjoint);
        }

        // Update is called once per frame
        void Update() {
            //if (vjoint.position.Equals (transform.position)) {
            // Todo: only transmit if changed? use a flag for "moved" ?
            //}
            vjoint.position = transform.position;
            vjoint.rotation = transform.rotation;
        }
    }

}