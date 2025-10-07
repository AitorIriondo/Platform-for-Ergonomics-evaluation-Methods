using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Numerics;


namespace IMMA;

public class IMMAManikin : ManikinBase
{
    public string sceneName = "";
    public string operationSequenceName = "";
    public string ipsFamily = "";
    public string familyID = "";
    public string name = "";
    public class ControlPoint
    {
        public string name = "";
        public string id = "";
        public List<Vector3> targets = new List<Vector3>();
        public List<Vector3> contactForces = new List<Vector3>();
        public List<Vector3> contactTorques = new List<Vector3>();
    }
    public class Measure
    {
        public string name = "";
        public float value = 0;
    }
    public class Joint
    {
        public string name = "";
        public int numAngles = 0;
        public List<Vector3> positions = new List<Vector3>();
        public List<List<float>> angles = new List<List<float>>();

    }
    public List<ControlPoint> controlPoints = new List<ControlPoint>();
    public List<Measure> measures = new List<Measure>();
    public List<Joint> joints = new List<Joint>();
    public bool interpolateTorque = false;
    public bool interpolateContactForce = false;
    Dictionary<JointID, string> jointIdToNameMap = new Dictionary<JointID, string>();
    protected Dictionary<string, Joint> jointsByName = new Dictionary<string, Joint>();

    public override List<Limb> GetLimbs()
    {
        List<Limb> limbList = new List<Limb>();
        Limb spine = new Limb("Spine");
        limbList.Add(spine);
        spine.joints = new List<JointID>() {
            JointID.L5S1,
            JointID.L3L4,
            JointID.T12L1,
            JointID.T6T7,
            JointID.T1T2,
            JointID.C6C7,
            JointID.C4C5,
            JointID.AtlantoAxial,
        };
        foreach (string side in new string[] { "Right", "Left" })
        {
            Limb arm = new Limb(side + "Arm");
            arm.AddJoint(JointID.T1T2);
            foreach (string suffix in new string[] { "SC", "AC", "GH", "Elbow", "Wrist" })
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
    ControlPoint getControlPoint(string name)
    {
        foreach (ControlPoint controlPoint in controlPoints)
        {
            if (controlPoint.name == name)
            {
                return controlPoint;
            }
        }
        return null;
    }
    float getMeasureValue(string name)
    {
        foreach (Measure measure in measures)
        {
            if (measure.name == name)
            {
                return measure.value;
            }
        }
        return float.NaN;
    }
    Vector3 GetControlPointForce(string name)
    {
        var cp = getControlPoint(name);
        if (cp != null){
            if (interpolateContactForce) {
                return frameInterpolator.interpolate(getControlPoint(name).contactForces);
            }
            return getControlPoint(name).contactForces[frameInterpolator.lowIdx];
        }
        return Vector3.Zero;
    }
    public override Vector3 GetLeftHandForce()
    {
        return GetControlPointForce("Left Hand");
    }
    public override Vector3 GetRightHandForce()
    {
        return GetControlPointForce("Right Hand");
    }

    public IMMAManikin(string filename)
    {
        var settings = new JsonSerializerSettings
        {
            Converters = { new Vector3JsonConverter() },
            Formatting = Formatting.Indented,
        };
        JsonConvert.PopulateObject(File.ReadAllText(filename), this, settings);
        if (name.StartsWith("Male"))
        {
            gender = Gender.Male;
        }
        else if (name.StartsWith("Female"))
        {
            gender = Gender.Female;
        }
        foreach (Joint j in joints)
        {
            jointsByName.Add(j.name, j);

            string jIdName = j.name.Replace("_", "");
            foreach (JointID jId in Enum.GetValues(typeof(JointID)))
            {
                if (jId.ToString() == jIdName)
                {
                    jointIdToNameMap[jId] = j.name;
                }
            }
        }

        // Apply alias map for missing joints
        var aliasMap = GetImmaJointMap();
        foreach (var kvp in aliasMap)
        {
            if (jointsByName.ContainsKey(kvp.Key) && !jointIdToNameMap.ContainsKey(kvp.Value))
            {
                jointIdToNameMap[kvp.Value] = kvp.Key;
            }
        }

    }

    [Serializable]
    class Message
    {
        public string src = "";
        public string parser = "";
        public string[] manikinFilenames;
    }
    public static IMMAManikin TryParse(string json)
    {
        try
        {
            Message msg = JsonConvert.DeserializeObject<Message>(json);
            if (msg.parser == "IMMAManikin")
            {
                return new IMMAManikin(msg.manikinFilenames[0]);    
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
    public override bool HasJoint(JointID jointID)
    {
        return jointIdToNameMap.ContainsKey(jointID);
    }
    protected Joint GetJoint(JointID jointID)
    {
        if(HasJoint(jointID))
        {
            return jointsByName[jointIdToNameMap[jointID]];
        }
        return null;
    }

    public override Vector3 GetJointPosition(JointID jointID)
    {
        return frameInterpolator.interpolate(GetJoint(jointID).positions);
    }
    public override string GetDescriptiveName()
    {
        return sceneName + " " + operationSequenceName + " " + ipsFamily + " " + name;
    }

    /// <summary>
    /// Provides explicit aliases from IMMA joint names to JointID enum entries.
    /// Used when auto-mapping fails.
    /// </summary>
    private static Dictionary<string, JointID> GetImmaJointMap()
    {
            return new Dictionary<string, JointID>(StringComparer.OrdinalIgnoreCase)
        {
            // Spine
            { "C6C7", JointID.C7T1 },
            { "T1T2", JointID.T1T2 },   // if your JointID has it
            { "T6T7", JointID.T6T7 },
            { "C4C5", JointID.C4C5 },
            { "AtlantoAxial", JointID.AtlantoAxial },

            // Shoulders
            { "RightGH", JointID.RightShoulder },
            { "LeftGH", JointID.LeftShoulder },

            // Wrists
            { "RightWrist", JointID.RightWrist },
            { "LeftWrist", JointID.LeftWrist },

            // Elbows
            { "RightElbow", JointID.RightElbow },
            { "LeftElbow", JointID.LeftElbow },

            // Hips / Knees / Ankles
            { "RightHip", JointID.RightHip },
            { "LeftHip", JointID.LeftHip },
            { "RightKnee", JointID.RightKnee },
            { "LeftKnee", JointID.LeftKnee },
            { "RightAnkle", JointID.RightAnkle },
            { "LeftAnkle", JointID.LeftAnkle },

            // Fingers (if you need them for wrists)
            { "Right_MiddleCarpal", JointID.RightMiddleCarpal },
            { "Right_MiddleProximal", JointID.RightMiddleProximal },
            { "Left_MiddleCarpal", JointID.LeftMiddleCarpal },
            { "Left_MiddleProximal", JointID.LeftMiddleProximal },
            { "Right_IndexCarpal", JointID.RightIndexCarpal },
            { "Left_IndexCarpal", JointID.LeftIndexCarpal },
            { "Right_PinkyCarpal", JointID.RightPinkyCarpal },
            { "Left_PinkyCarpal", JointID.LeftPinkyCarpal },
            // ...extend with others as needed
        };
    }

}
