using System.Collections;
using System.Collections.Generic;
using System.Numerics;

public class Limb
{
    public string name;
    public List<JointID> joints = new List<JointID>();
    public Limb(string name)
    {
        this.name = name;
    }
    public bool AddJoint(string name)
    {
        try
        {
            return AddJoint((JointID)Enum.Parse(typeof(JointID), name));
        }
        catch (Exception)
        {

        }
        return false;
    }
    public bool AddJoint(JointID j)
    {
        joints.Add(j);
        return true;
    }

}


public abstract class ManikinBase{
    public float time;
    public float weight;
    public float height;
    public Gender gender = Gender.Unspecified;
    public List<float> postureTimeSteps = new List<float>();

    public float GetTimelineDuration()
    {
        if(postureTimeSteps.Count > 0)
        {
            return postureTimeSteps[postureTimeSteps.Count - 1];
        }
        return 0;
    }
    public virtual void SetTime(float newTime) {
        time = MathF.Min(GetTimelineDuration(), MathF.Max(0, newTime));
    }
    public abstract Vector3 GetJointPosition(JointID jointID);

    public virtual List<Limb> GetLimbs()
    {
        return new List<Limb>();
    }
    public virtual Vector3 GetLeftHandForce()
    {
        return Vector3.Zero;
    }
    public virtual Vector3 GetRightHandForce()
    {
        return Vector3.Zero;
    }
    

}

public enum Gender
{
    Unspecified,
    Female,
    Male
}
public enum JointID
{
    L5S1,
    L3L4,
    T12L1,
    T6T7,
    T1T2,
    C7T1,
    C6C7,
    C4C5,
    AtlantoAxial,
    Eyeside,
    LeftHip,
    LeftKnee,
    LeftAnkleRot,
    LeftAnkle,
    LeftToes,
    RightHip,
    RightKnee,
    RightAnkleRot,
    RightAnkle,
    RightToes,
    RightSC,
    RightAC,
    RightGH,
    RightElbow,
    LeftSC,
    LeftAC,
    LeftGH,
    LeftElbow,
    LeftWrist,
    LeftIndexCarpal,
    LeftIndexProximal,
    LeftIndexIntermediate,
    LeftIndexDistal,
    LeftMiddleCarpal,
    LeftMiddleProximal,
    LeftMiddleIntermediate,
    LeftMiddleDistal,
    LeftRingCarpal,
    LeftRingProximal,
    LeftRingIntermediate,
    LeftRingDistal,
    LeftPinkyCarpal,
    LeftPinkyProximal,
    LeftPinkyIntermediate,
    LeftPinkyDistal,
    LeftThumbProximal,
    LeftThumbIntermediate,
    LeftThumbDistal,
    RightWrist,
    RightIndexCarpal,
    RightIndexProximal,
    RightIndexIntermediate,
    RightIndexDistal,
    RightMiddleCarpal,
    RightMiddleProximal,
    RightMiddleIntermediate,
    RightMiddleDistal,
    RightRingCarpal,
    RightRingProximal,
    RightRingIntermediate,
    RightRingDistal,
    RightPinkyCarpal,
    RightPinkyProximal,
    RightPinkyIntermediate,
    RightPinkyDistal,
    RightThumbProximal,
    RightThumbIntermediate,
    RightThumbDistal,
}