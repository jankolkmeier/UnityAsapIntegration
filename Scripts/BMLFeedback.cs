using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

namespace ASAP {

    public delegate void BlockProgressCallback(BlockProgress blockProgress);
    public delegate void PredictionFeedbackCallback(PredictionFeedback predictionFeedback);
    public delegate void SyncPointProgressCallback(SyncPointProgress syncPointProgress);
    public delegate void WarningFeedbackCallback(WarningFeedback warningFeedback);

    [RequireComponent(typeof(BMLManager))]
    public class BMLFeedback : MonoBehaviour {
        public event BlockProgressCallback BlockProgressEventHandler;
        public event PredictionFeedbackCallback PredictionFeedbackEventHandler;
        public event SyncPointProgressCallback SyncPointProgressEventHandler;
        public event WarningFeedbackCallback WarningFeedbackEventHandler;

        void Start() {}

        public T ParseFeedbackBlock<T>(string block) {
            T res = default(T);
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using (StringReader reader = new StringReader(block)) {
                res = (T)(serializer.Deserialize(reader));
            }
            return res;
         }

        public void HandleFeedback(string feedback) {
            try { 
                if (feedback.StartsWith("<blockProgress")) {
                    BlockProgress blockProgress = ParseFeedbackBlock<BlockProgress>(feedback);
                    //Debug.Log("[blockProgress] " + blockProgress.status + " "+blockProgress.id);
					if (BlockProgressEventHandler != null)
                    	BlockProgressEventHandler(blockProgress);
                } else if (feedback.StartsWith("<predictionFeedback")) {
                    PredictionFeedback predictionFeedback = ParseFeedbackBlock<PredictionFeedback>(feedback);
					//Debug.Log("[predictionFeedback] " + predictionFeedback.bml.id+" -- "+predictionFeedback.bml.status);
					if (PredictionFeedbackEventHandler != null)
                    	PredictionFeedbackEventHandler(predictionFeedback);
                } else if (feedback.StartsWith("<syncPointProgress")) {
                    SyncPointProgress syncPointProgress = ParseFeedbackBlock<SyncPointProgress>(feedback);
					//Debug.Log("[syncPointProgress] " + syncPointProgress.id);
					if (SyncPointProgressEventHandler != null)
                    	SyncPointProgressEventHandler(syncPointProgress);
                } else if (feedback.StartsWith("<warningFeedback")) {
                    WarningFeedback warningFeedback = ParseFeedbackBlock<WarningFeedback>(feedback);
					//Debug.LogWarning("[warningFeedback] " + warningFeedback.id+"\n"+warningFeedback.Value);
					if (WarningFeedbackEventHandler != null)
                    	WarningFeedbackEventHandler(warningFeedback);
                } else {
                    //Debug.LogWarning(feedback);
                }
            } catch (System.Xml.XmlException xmle) {
                Debug.LogWarning("Exception while parsing feedback: " + xmle + "\n\n" +feedback);
            } finally {
                //ExperimentLogger.LOG_STRIP_NEWLINES(feedback, "bmlFeedback");
            }
        }

    }

    /* For some of the time properties we use "string" instead of decimal, because the parser
     * seems to have difficulties with parsing numers in exponential notation....
     */

    [XmlRoot("blockProgress", Namespace = "http://www.bml-initiative.org/bml/bml-1.0")]
    public class BlockProgress {
        [XmlAttribute("id")]
        public string id { get; set; }

        [XmlAttribute("characterId")]
        public string characterId { get; set; }

        [XmlAttribute("globalTime")]
        public string globalTime { get; set; }

        [XmlAttribute("posixTime", Namespace = "http://www.asap-project.org/bmla")]
        public long posixTime { get; set; }

        [XmlAttribute("status", Namespace = "http://www.asap-project.org/bmla")]
        public string status { get; set; }
    }

    [XmlRoot("syncPointProgress", Namespace = "http://www.bml-initiative.org/bml/bml-1.0")]
    public class SyncPointProgress {
        [XmlAttribute("id")]
        public string id { get; set; }

        [XmlAttribute("characterId")]
        public string characterId { get; set; }

        [XmlAttribute("globalTime")]
        public string globalTime { get; set; }

        [XmlAttribute("posixTime", Namespace = "http://www.asap-project.org/bmla")]
        public long posixTime { get; set; }

        [XmlAttribute("time")]
        public string time { get; set; }
    }

    [XmlRoot("warningFeedback", Namespace = "http://www.bml-initiative.org/bml/bml-1.0")]
    public class WarningFeedback {

        [XmlAttribute("id")]
        public string id { get; set; }

        [XmlAttribute("characterId")]
        public string characterId { get; set; }

        [XmlAttribute("type")]
        public string type { get; set; }

        [XmlText]
        public string Value { get; set; }
    }

    [XmlRoot("predictionFeedback", Namespace = "http://www.bml-initiative.org/bml/bml-1.0")]
    public class PredictionFeedback {
        [XmlElement]
        public BmlBlock bml;
    }

    [XmlRoot("bml")]
    public class BmlBlock {
        [XmlAttribute("id")]
        public string id { get; set; }

        [XmlAttribute("status", Namespace = "http://www.asap-project.org/bmla")]
        public string status { get; set; }

        [XmlAttribute("globalStart")]
        public string globalStart { get; set; }

        [XmlAttribute("globalEnd")]
        public string globalEnd { get; set; }

        [XmlAttribute("posixStartTime", Namespace = "http://www.asap-project.org/bmla")]
        public long posixStartTime { get; set; }

        [XmlAttribute("posixEndTime", Namespace = "http://www.asap-project.org/bmla")]
        public long posixEndTime { get; set; }

        [XmlText]
        public string Value { get; set; }
    }


    /*
    [XmlElement("TestElement")]
    public TestElement TestElement { get; set; }

    [XmlText]
    public int Value { get; set; }

    [XmlAttribute]
    public string attr1 { get; set; }
    */
}
