using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ASAP {

	[RequireComponent(typeof(ASAPManager))]
    [RequireComponent(typeof(BMLFeedback))]
    [RequireComponent(typeof(BMLRequests))]
    public class BMLManager : MonoBehaviour {
        IMiddleware middleware;
		BMLFeedback feedback;
		ASAPManager asapManager;

        void Start() {
			asapManager = GetComponent<ASAPManager> ();
			middleware = new STOMPMiddleware("tcp://" + asapManager.middlewareLocation + ":61613", "topic://bmlFeedback", "topic://bmlRequests", "admin", "password", false);
            feedback = GetComponent<BMLFeedback>();
        }

        void Update() {
            ReadMessages();
        }

        public void SendBML(string bml) {
            Send(JsonUtility.ToJson(new BMLMiddlewareMessage {
                bml = new MiddlewareContent { content = System.Uri.EscapeDataString(bml) }
            }));
        }

        public void SendBML_noEscape(string escapedBML) {
            Send(JsonUtility.ToJson(new BMLMiddlewareMessage {
                bml = new MiddlewareContent { content = escapedBML }
            }));
        }

        public void Send(string data) {
            middleware.SendMessage(data);
        }

        void ReadMessages() {
            string rawMsg = middleware.ReadMessage();
            if (rawMsg.Length == 0) return;
            FeedbackMiddlewareMessage msg = JsonUtility.FromJson<FeedbackMiddlewareMessage>(rawMsg);
            feedback.HandleFeedback(System.Uri.UnescapeDataString(msg.feedback.content).Replace('+',' '));
        }

        void OnApplicationQuit() {
            middleware.Close();
        }
    }

    [System.Serializable]
    public class FeedbackMiddlewareMessage {
        public MiddlewareContent feedback;
    }

    [System.Serializable]
    public class BMLMiddlewareMessage {
        public MiddlewareContent bml;
    }

    [System.Serializable]
    public class MiddlewareContent {
        public string content;
    }
}