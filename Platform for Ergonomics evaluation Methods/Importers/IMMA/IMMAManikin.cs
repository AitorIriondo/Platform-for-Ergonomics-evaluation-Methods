using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Numerics;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using Microsoft.VisualBasic;

namespace IMMA;

public class IMMAManikin : ManikinBase
{
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
    protected Dictionary<JointID, Joint> jointDict = new Dictionary<JointID, Joint>();

    public FrameInterpolator getFrameInterpolationInfo(float time, bool justLowIdx = false)
    {
        return new FrameInterpolator(time, postureTimeSteps, justLowIdx);
    }

    public int getFrameIdxAtOrBeforeTime(float time)
    {
        return getFrameInterpolationInfo(time, true).lowIdx;
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
        if (interpolateContactForce)
        {
            return frameInterpolator.interpolate(getControlPoint(name).contactForces);
        }
        return getControlPoint(name).contactForces[frameInterpolator.lowIdx];
    }
    public override Vector3 GetLeftHandForce()
    {
        return GetControlPointForce("Left Hand");
    }
    public override Vector3 GetRightHandForce()
    {
        return GetControlPointForce("Right Hand");
    }

    public IMMAManikin()
    {
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
            if (msg.parser == "IMMAManikinLua")
            {
                var settings = new JsonSerializerSettings
                {
                    Converters = { new Vector3JsonConverter() },
                    Formatting = Formatting.Indented,
                };
                IMMAManikin manikin = JsonConvert.DeserializeObject<IMMAManikin>(File.ReadAllText(msg.manikinFilenames[0]), settings);
                if (manikin.name.StartsWith("Male"))
                {
                    manikin.gender = Gender.Male;
                }
                else if (manikin.name.StartsWith("Female"))
                {
                    manikin.gender = Gender.Female;
                }
                return manikin;
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

    protected Joint getJointByName(string name)
    {
        foreach (var joint in joints)
        {
            if (joint.name.Replace("_", "") == name)
            {
                return joint;
            }
        }
        return null;
    }
    protected Joint GetJoint(JointID jointID)
    {
        if (jointDict.ContainsKey(jointID))
        {
            return jointDict[jointID];
        }
        Joint ret = null;
        if (jointID == JointID.C7T1)
        {
            ret = getJointByName("C6C7");
        }
        else
        {
            ret = getJointByName(jointID.ToString());
        }
        if (ret == null)
        {
            throw new Exception("Missing joint " + jointID.ToString());
        }
        jointDict[jointID] = ret;
        return ret;
    }

    public override Vector3 GetJointPosition(JointID jointID)
    {
        return frameInterpolator.interpolate(GetJoint(jointID).positions);
    }
}
