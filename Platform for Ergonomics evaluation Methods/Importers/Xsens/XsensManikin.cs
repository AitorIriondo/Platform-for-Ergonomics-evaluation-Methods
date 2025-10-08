using Newtonsoft.Json;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.Linq;

namespace Xsens
{
    public class XsensManikin : ManikinBase
    {

        public Dictionary<string, List<Vector3>> jointAnglesByName = new();
        public List<string> jointAngleOrder = new();  // the order of joints in each frame

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
                ret.Add(float.Parse(str, CultureInfo.InvariantCulture));
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
        protected string descriptiveName = "";
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
            Debug.Print($"XsensManikin filename:{filename}");
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(File.ReadAllText(filename));
            try
            {
                descriptiveName = doc.GetElementsByTagName("comment")[0].InnerText;
            }
            catch (Exception e)
            {
                Debug.Print(e.StackTrace);
            }
            XmlNodeList xmlJoints = doc.GetElementsByTagName("joint");
            List<Joint> orderedJoints = new List<Joint>();
            foreach (XmlElement xmlJoint in xmlJoints)
            {
                Joint joint = new Joint(xmlJoint.GetAttribute("label"));
                orderedJoints.Add(joint);
                jointsByName.Add(joint.name, joint);
                Debug.Print($"Joint name:{joint.name}");
            }
            Debug.Print($"Joint cnt:{orderedJoints.Count}");
            List<string> segmentLabels = new List<string>();
            foreach (XmlElement segment in doc.GetElementsByTagName("segment"))
            {
                string label = segment.GetAttribute("label");
                segmentLabels.Add(label);
                Debug.Print($"Segment label:{label}");
            }
            Debug.Print($"Segment label cnt:{segmentLabels.Count}");
            foreach (XmlNode frameNode in doc.GetElementsByTagName("frame"))
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

            XmlNodeList jointAngleNodes = doc.GetElementsByTagName("jointAngle");
            if (jointAngleNodes.Count > 0)
            {
                jointAngleOrder = new List<string>
                    {
                        "jL5S1","jL4L3","jL1T12","jT9T8","jT1C7","jC1Head",
                        "jRightT4Shoulder","jRightShoulder","jRightElbow","jRightWrist",
                        "jLeftT4Shoulder","jLeftShoulder","jLeftElbow","jLeftWrist",
                        "jRightHip","jRightKnee","jRightAnkle","jRightBallFoot",
                        "jLeftHip","jLeftKnee","jLeftAnkle","jLeftBallFoot"
                    };

                int nJoints = jointAngleOrder.Count;

                // Initialize storage
                foreach (var jName in jointAngleOrder)
                    jointAnglesByName[jName] = new List<Vector3>();

                foreach (XmlElement frame in jointAngleNodes)
                {
                    List<float> vals = new();
                    foreach (string s in frame.InnerText.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                        vals.Add(float.Parse(s, CultureInfo.InvariantCulture));

                    for (int j = 0; j < nJoints; j++)
                    {
                        var ang = new Vector3(vals[j * 3], vals[j * 3 + 1], vals[j * 3 + 2]);
                        jointAnglesByName[jointAngleOrder[j]].Add(ang);
                    }
                }

                Debug.Print($"Parsed {jointAnglesByName.Count} joint angle series ({jointAnglesByName.First().Value.Count} frames each)");
            }

            // Robust mappings that accept both legacy "j*" and newer plain labels,
            // plus common flipped-pair aliases.
            MapJointSmart(JointID.L5S1, "L5S1");
            MapJointSmart(JointID.L3L4, "L3L4", "L4L3");
            MapJointSmart(JointID.T12L1, "T12L1", "L1T12");
            MapJointSmart(JointID.C7T1, "C7T1", "T1C7");
            MapJointSmart(JointID.AtlantoAxial, "C1Head");   // jC1Head → upper cervical

            MapJointSmart(JointID.RightShoulder, "RightShoulder");
            MapJointSmart(JointID.RightElbow, "RightElbow");
            MapJointSmart(JointID.RightWrist, "RightWrist");
            MapJointSmart(JointID.LeftShoulder, "LeftShoulder");
            MapJointSmart(JointID.LeftElbow, "LeftElbow");
            MapJointSmart(JointID.LeftWrist, "LeftWrist");

            MapJointSmart(JointID.RightHip, "RightHip");
            MapJointSmart(JointID.RightKnee, "RightKnee");
            MapJointSmart(JointID.RightAnkle, "RightAnkle");
            MapJointSmart(JointID.LeftHip, "LeftHip");
            MapJointSmart(JointID.LeftKnee, "LeftKnee");
            MapJointSmart(JointID.LeftAnkle, "LeftAnkle");


            Debug.WriteLine(GetJointPosition(JointID.LeftElbow));
        }
        [System.Serializable]
        public class Message
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
                Debug.WriteLine(ex.ToString());


            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                Debug.WriteLine(ex.StackTrace);
            }
            return null;
        }
        Joint? GetJoint(JointID jointID)
        {
            return HasJoint(jointID) ? jointsByName[jointIdToNameMap[jointID]] : null;
        }
        public override bool HasJoint(JointID jointID)
        {
            return jointIdToNameMap.TryGetValue(jointID, out var label)
                && !string.IsNullOrWhiteSpace(label)
                && jointsByName.ContainsKey(label);
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

        public override bool TryGetImportedJointAngle(string name, out List<Vector3> series)
        {
            if (jointAnglesByName.TryGetValue(name, out series))
                return true;
            return false;
        }

        public bool TryGetJointAngle(string jointName, int frameIdx, out Vector3 euler)
        {
            euler = Vector3.Zero;
            if (!jointAnglesByName.TryGetValue(jointName, out var list)) return false;
            if (frameIdx < 0 || frameIdx >= list.Count) return false;
            euler = list[frameIdx];
            return true;
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
        public override string GetDescriptiveName()
        {
            return descriptiveName;
        }

        // Returns the first label that exists in jointsByName.
        // Tries each candidate as-is and with a leading 'j' prefix.
        // Try candidates as-is and with a 'j' prefix; require full-length positions.
        private string? ResolveExistingLabelWithData(IEnumerable<string> candidates)
        {
            foreach (var c in candidates)
            {
                if (string.IsNullOrWhiteSpace(c)) continue;

                // exact
                if (jointsByName.TryGetValue(c, out var j) && j.positions.Count == postureTimeSteps.Count)
                    return c;

                // try with 'j' prefix if missing
                if (c[0] != 'j')
                {
                    var jp = "j" + c;
                    if (jointsByName.TryGetValue(jp, out var jj) && jj.positions.Count == postureTimeSteps.Count)
                        return jp;
                }
            }
            return null;
        }

        private void MapJointSmart(JointID id, params string[] candidates)
        {
            // First pass: direct candidates (exact + 'j' variants)
            var found = ResolveExistingLabelWithData(candidates);
            if (found != null) { jointIdToNameMap[id] = found; return; }

            // Second pass: common aliases (C7T1<->T1C7, add C6C7 as last resort; L3L4<->L4L3; T12L1<->L1T12)
            var aliasList = new List<string>();
            foreach (var c in candidates)
            {
                if (string.IsNullOrWhiteSpace(c)) continue;
                if (c.Contains("C7T1")) { aliasList.Add("T1C7"); aliasList.Add("C6C7"); }
                if (c.Contains("T1C7")) { aliasList.Add("C7T1"); aliasList.Add("C6C7"); }
                if (c.Contains("L3L4")) aliasList.Add("L4L3");
                if (c.Contains("L4L3")) aliasList.Add("L3L4");
                if (c.Contains("T12L1")) aliasList.Add("L1T12");
                if (c.Contains("L1T12")) aliasList.Add("T12L1");
            }

            found = ResolveExistingLabelWithData(aliasList);
            if (found != null) { jointIdToNameMap[id] = found; return; }

            // Leave unmapped; HasJoint() will correctly return false.
        }

        private void DebugLogMissingMap(JointID id, string purpose = "")
        {
            try
            {
                System.Diagnostics.Debug.Print(
                    $"[XSENS] Missing map for {id}{(string.IsNullOrEmpty(purpose) ? "" : $" ({purpose})")}. " +
                    $"Available labels: {string.Join(", ", jointsByName.Keys)}");
            }
            catch { /* best-effort logging only */ }
        }


    }
}
