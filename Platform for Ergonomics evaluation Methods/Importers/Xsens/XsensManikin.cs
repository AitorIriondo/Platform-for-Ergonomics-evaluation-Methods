using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Xml;

namespace Platform_for_Ergonomics_evaluation_Methods.Importers.Xsens
{
    public class XsensManikin : ManikinBase
    {
        public class Frame
        {
            float time = 0;
            int index = -1;
            public List<Vector3> position = new List<Vector3>();
            public List<Quaternion> orientation = new List<Quaternion>();
            public void Parse(XmlElement element)
            {
                string[] getStrings(string tagName)
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

                List<float> getFloats(string tagName)
                {
                    List<float> ret = new List<float>();
                    foreach(string str in getStrings(tagName))
                    {
                        ret.Add(float.Parse(str.Replace(".",",")));
                    }
                    return ret;
                }
                List<Vector3> getVector3s(string tagName)
                {
                    List<Vector3> ret = new List<Vector3>();
                    List<float> floats=getFloats(tagName);
                    for(int i = 0; i < floats.Count; i += 3)
                    {
                        ret.Add(new Vector3(floats[i], floats[i+1], floats[i+2]));
                    }
                    return ret;
                }
                position = getVector3s("position");
                List<float> floats = getFloats("orientation");
                for (int i = 0; i < floats.Count; i += 4)
                {
                    orientation.Add(new Quaternion(floats[i], floats[i + 1], floats[i + 2], floats[i + 3]));
                }
            }
        }
        XmlDocument doc = new XmlDocument();
        List<string> jointLabels = new List<string>();
        List<string> segmentLabels = new List<string>();
        List<Frame> frames = new List<Frame>();
        Dictionary<JointID, string> jointLabelMap = new Dictionary<JointID, string>();
        public XsensManikin()
        {
            Debug.WriteLine("Hej");
            
            doc.LoadXml(File.ReadAllText("C:\\Downloads\\Emma-001.mvnx"));
            XmlNodeList joints = doc.GetElementsByTagName("joint");
            foreach (XmlElement joint in joints)
            {
                string label = joint.GetAttribute("label");
                jointLabels.Add(label);

            }
            jointLabelMap.Add(JointID.L5S1, "jL5S1");
            jointLabelMap.Add(JointID.L3L4, "jL4L3");
            jointLabelMap.Add(JointID.T12L1, "jL1T12");
            jointLabelMap.Add(JointID.C7T1, "jT1C7");
            jointLabelMap.Add(JointID.RightGH, "jRightShoulder");
            jointLabelMap.Add(JointID.RightElbow, "jRightElbow");
            jointLabelMap.Add(JointID.RightWrist, "jRightWrist");
            jointLabelMap.Add(JointID.LeftGH, "jLeftShoulder");
            jointLabelMap.Add(JointID.LeftElbow, "jLeftElbow");
            jointLabelMap.Add(JointID.LeftWrist, "jLeftWrist");
            jointLabelMap.Add(JointID.RightHip, "jRightHip");
            jointLabelMap.Add(JointID.RightKnee, "jRightKnee");
            jointLabelMap.Add(JointID.RightAnkle, "jRightAnkle");
            jointLabelMap.Add(JointID.LeftHip, "jLeftHip");
            jointLabelMap.Add(JointID.LeftKnee, "jLeftKnee");
            jointLabelMap.Add(JointID.LeftAnkle, "jLeftAnkle");
            Debug.WriteLine("Joints.Count:" + joints.Count);

            foreach (XmlElement segment in doc.GetElementsByTagName("segment"))
            {
                string label = segment.GetAttribute("label");
                segmentLabels.Add(label);
            }
            Debug.WriteLine("Joints.Count:" + joints.Count);
            foreach(XmlNode frameNode in doc.GetElementsByTagName("frame"))
            {
                Frame frame = new Frame();
                frame.Parse((XmlElement)frameNode);
                frames.Add(frame);
            }
            Debug.WriteLine(frames[0].position.Count / 3);
            Debug.WriteLine(GetJointPosition(JointID.LeftElbow));
        }

        XmlElement GetJointElement(string label)
        {
            return (XmlElement)doc.GetElementsByTagName("joint")[jointLabels.IndexOf(label)];
        }
        public override Vector3 GetJointPosition(JointID jointID)
        {
            string jointLabel = jointLabelMap[jointID];
            string segName = GetJointElement(jointLabel).GetElementsByTagName("connector1")[0].InnerText.Split("/")[0];
            int segIdx = segmentLabels.IndexOf(segName);
            Frame frame = frames[3];
            return frame.position[segIdx];
            throw new Exception("Missing joint " + jointID.ToString());
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
                foreach (string suffix in new string[] { "GH", "Elbow", "Wrist" })
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
