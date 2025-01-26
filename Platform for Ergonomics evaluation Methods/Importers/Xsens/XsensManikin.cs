using Newtonsoft.Json;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.Linq;

namespace Xsens
{
    public class XsensManikin : ManikinBase
    {

        static string[] parseStrings(XmlElement element, string tagName)
        {
            try
            {
                return element.GetElementsByTagName(tagName)[0].InnerText.Split(" ");
            }
            catch (Exception)
            {

            }
            return new string[0];
        }

        static List<float> parseFloats(XmlElement element, string tagName)
        {
            List<float> ret = new List<float>();
            foreach (string str in parseStrings(element, tagName))
            {
                ret.Add(float.Parse(str.Replace(".", ",")));
            }
            return ret;
        }
        static List<Vector3> parseVector3s(XmlElement element,string tagName)
        {
            List<Vector3> ret = new List<Vector3>();
            List<float> floats = parseFloats(element, tagName);
            for (int i = 0; i < floats.Count; i += 3)
            {
                ret.Add(new Vector3(floats[i], floats[i + 1], floats[i + 2]));
            }
            return ret;
        }

        Dictionary<JointID, string> jointIdToNameMap = new Dictionary<JointID, string>();
        protected Dictionary<string, Joint> jointsByName = new Dictionary<string, Joint>();

        public class Joint
        {
            public string name = "";
            public List<Vector3> positions = new List<Vector3>();
            public Joint(string name) { 
                this.name = name;
            }
        }
        public XsensManikin(string filename)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(File.ReadAllText(filename));
            XmlNodeList xmlJoints = doc.GetElementsByTagName("joint");
            List<Joint> orderedJoints = new List<Joint>();
            foreach (XmlElement xmlJoint in xmlJoints)
            {
                Joint joint = new Joint(xmlJoint.GetAttribute("label"));
                orderedJoints.Add(joint);
                jointsByName.Add(joint.name, joint);

            }
            List<string> segmentLabels = new List<string>();
            foreach (XmlElement segment in doc.GetElementsByTagName("segment"))
            {
                string label = segment.GetAttribute("label");
                segmentLabels.Add(label);
            }
            foreach(XmlNode frameNode in doc.GetElementsByTagName("frame"))
            {
                XmlElement frame = (XmlElement)frameNode;
                if(frame.GetAttribute("type") == "normal")
                {
                    List<Vector3> positions = parseVector3s(frame, "position");
                    postureTimeSteps.Add(float.Parse(frame.GetAttribute("time")) / 1000);
                    for (int i = 0; i < orderedJoints.Count; i++)
                    {
                        string segName = ((XmlElement)xmlJoints[i]).GetElementsByTagName("connector2")[0].InnerText.Split("/")[0];
                        int segIdx = segmentLabels.IndexOf(segName);
                        orderedJoints[i].positions.Add(positions[segIdx]);
                    }
                }
            }

            jointIdToNameMap.Add(JointID.L5S1, "jL5S1");
            jointIdToNameMap.Add(JointID.L3L4, "jL4L3");
            jointIdToNameMap.Add(JointID.T12L1, "jL1T12");
            jointIdToNameMap.Add(JointID.C7T1, "jT1C7");
            jointIdToNameMap.Add(JointID.RightShoulder, "jRightShoulder");
            jointIdToNameMap.Add(JointID.RightElbow, "jRightElbow");
            jointIdToNameMap.Add(JointID.RightWrist, "jRightWrist");
            jointIdToNameMap.Add(JointID.LeftShoulder, "jLeftShoulder");
            jointIdToNameMap.Add(JointID.LeftElbow, "jLeftElbow");
            jointIdToNameMap.Add(JointID.LeftWrist, "jLeftWrist");
            jointIdToNameMap.Add(JointID.RightHip, "jRightHip");
            jointIdToNameMap.Add(JointID.RightKnee, "jRightKnee");
            jointIdToNameMap.Add(JointID.RightAnkle, "jRightAnkle");
            jointIdToNameMap.Add(JointID.LeftHip, "jLeftHip");
            jointIdToNameMap.Add(JointID.LeftKnee, "jLeftKnee");
            jointIdToNameMap.Add(JointID.LeftAnkle, "jLeftAnkle");
            
            Debug.WriteLine(GetJointPosition(JointID.LeftElbow));
        }
        [System.Serializable]
        class Message
        {
            public string parser = "";
            public string file = "";
        }
        public static XsensManikin TryParse(string json)
        {
            try
            {
                Message msg = JsonConvert.DeserializeObject<Message>(json);
                if (msg.parser == "XsensManikin")
                {
                    return new XsensManikin(msg.file);
                }
            }
            catch (JsonReaderException ex)
            {


            }
            catch (Exception ex)
            {
                //Debug.WriteLine(ex.ToString());
            }
            return null;
        }
        Joint? GetJoint(JointID jointID)
        {
            return HasJoint(jointID) ? jointsByName[jointIdToNameMap[jointID]] : null;
        }
        public override bool HasJoint(JointID jointID)
        {
            return jointIdToNameMap.ContainsKey(jointID);
        }
        public override Vector3 GetJointPosition(JointID jointID)
        {
            try
            {
                return frameInterpolator.interpolate(GetJoint(jointID).positions);
            }
            catch (Exception)
            {

                throw new Exception("Missing joint: "+ jointID);
            }
            
        }


        public override List<Limb> GetLimbs()
        {
            List<Limb> limbList = new List<Limb>();
            Limb spine = new Limb("Spine");
            limbList.Add(spine);
            spine.joints = new List<JointID>() {
                JointID.L5S1,
                JointID.L3L4,
                JointID.T12L1,
                JointID.C7T1
            };
            foreach (string side in new string[] { "Right", "Left" })
            {
                Limb arm = new Limb(side + "Arm");
                foreach (string suffix in new string[] { "Shoulder", "Elbow", "Wrist" })
                {
                    arm.AddJoint(side + suffix);
                }
                limbList.Add(arm);
                Limb leg = new Limb(side + "Leg");
                leg.AddJoint(JointID.L5S1);
                foreach (string suffix in new string[] { "Hip", "Knee", "Ankle" })
                {
                    leg.AddJoint(side + suffix);
                }
                limbList.Add(leg);
            }
            return limbList;
        }

    }
}
