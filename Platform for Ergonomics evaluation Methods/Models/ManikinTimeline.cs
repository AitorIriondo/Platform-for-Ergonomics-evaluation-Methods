using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Numerics;
using System.Diagnostics;
using System.Runtime.InteropServices;

class ManikinControlPointsTimeline{ 
    public List<string> controlPointNames = new List<string>();
    public List<float> timeSteps = new List<float>();
    public List<List<Vector3>> positions = new List<List<Vector3>>();
    public List<List<Quaternion>> rotations = new List<List<Quaternion>>();
    public List<List<Vector3>> forces = new List<List<Vector3>>();
    public List<List<Vector3>> torques = new List<List<Vector3>>();
	public static ManikinControlPointsTimeline FromFile(string filename) {
		ManikinControlPointsTimeline ret = new ManikinControlPointsTimeline();
		ret.Load(filename);
		return ret;
	}
	private void Load(string filename) { 
        BinFileReader fileReader = new BinFileReader(filename);
        fileReader.float64 = false;
		fileReader.bigEndian = true;	
        string headerJson = fileReader.readString();
		JsonConvert.PopulateObject(headerJson, this);
		for (int i = 0; i < timeSteps.Count; i++)
		{
			for (int j = 0; j < controlPointNames.Count; j++)
			{
				if(i==0)
				{
                    positions.Add(new List<Vector3>());
					rotations.Add(new List<Quaternion>());
					forces.Add(new List<Vector3>());
					torques.Add(new List<Vector3>());
                }
				positions[j].Add(fileReader.readVector3());
				rotations[j].Add(fileReader.readQuaternion());
				forces[j].Add(fileReader.readVector3());
				torques[j].Add(fileReader.readVector3());
            }
		}
		fileReader.Close();
    }
    int GetFrameIdxAtTime(float time)
	{
		for(int i=0; i<timeSteps.Count;i++)
		{
			if (timeSteps[i] >= time)
			{
				return i;
			}
		}
		return timeSteps.Count-1;
	}

    public Vector3 GetForce(int controlPointIdx, float time)
    {
        return forces[controlPointIdx][GetFrameIdxAtTime(time)];
    }
    public Vector3 GetTorque(int controlPointIdx, float time)
    {
        return torques[controlPointIdx][GetFrameIdxAtTime(time)];
    }
}

public class ManikinTimeline : TimelineBase{
	public List<MJoint> joints;
	public bool interpolateTorque = false;
	public bool interpolateContactForceData = false;

	FrameInterpolationInfo frameInterPolationInfo = new FrameInterpolationInfo();
	public List<float> postureTimeSteps { get { return modelInfo.timeSteps; } }
	public ModelInfo modelInfo;
	public FrameInterpolationInfo getFrameInterpolationInfo(float time, bool justLowIdx = false) {
		//if (time == frameInterPolationInfo.time) {
		//	return frameInterPolationInfo;
		//}
		FrameInterpolationInfo ret = new FrameInterpolationInfo();
		ret.lowIdx = postureTimeSteps.IndexOf(time);
		if (ret.lowIdx >= 0) {
			return ret;
		}
		ret.lowIdx = 0;
		if (time <= 0) {
			return ret;
		}
		if (time >= postureTimeSteps[postureTimeSteps.Count - 1]) {
			ret.lowIdx = postureTimeSteps.Count - 1;
			return ret;
		}

		while (postureTimeSteps[ret.lowIdx] < time) {
			ret.lowIdx++;
		}
		ret.lowIdx--;
		if (justLowIdx) {
			return ret;
		}
		float lowTime = postureTimeSteps[ret.lowIdx];
		if (lowTime == time || ret.lowIdx >= postureTimeSteps.Count - 1) {
			return ret;
		}
		ret.highIdx = ret.lowIdx + 1;
		while (ret.highIdx < postureTimeSteps.Count && postureTimeSteps[ret.highIdx] == lowTime) {
			ret.highIdx++;
		}
		float tRange = postureTimeSteps[ret.highIdx] - lowTime;
		if (tRange == 0) {
			return ret;
		}
		ret.factor = (time - lowTime) / tRange;
		return ret;
	}

	public int getFrameIdxAtOrBeforeTime(float time) {
		return getFrameInterpolationInfo(time, true).lowIdx;
	}

	MJoint initJoint(string name, ModelInfo modelInfo) {
		MJoint joint = new MJoint();
		joint.name = name;
		joint.timeline = this;
		joint.idx = modelInfo.jointNames.IndexOf(name);
		joint.firstAngleIdx = modelInfo.jointStateStart[joint.idx];
		joint.angleCnt = modelInfo.jointDoF[joint.idx];
		List<ContactForceData> cfs = modelInfo.GetPostureData(0).mContactForces;
		joint.contactForceIdx = -1;
		for (int i = 0; i < cfs.Count; i++) {
			if (cfs[i].mJointIndex == joint.idx) {
				joint.contactForceIdx = i;
			}
		}
		joints.Add(joint);
		return joint;
	}

	public float getFinalTime() {
		return postureTimeSteps[postureTimeSteps.Count - 1];
	}
	public List<float> getTimeSteps() {
		return postureTimeSteps;
	}

	public PostureData getPostureData() {
		return getPostureData(time);
	}

	public PostureData getPostureData(float time) {
		return modelInfo.GetPostureData(getFrameIdxAtOrBeforeTime(time));
	}
	public MJoint GetJointByName(string name) {
		foreach (MJoint j in joints) {
			if (j.name == name) {
				return j;
			}
		}
		return null;
	}

    public Vector3 GetLeftHandForce()
    {
        return ctrlPointsTimeline.GetForce(1, time);
    }
    public Vector3 GetRightHandForce()
    {
        return ctrlPointsTimeline.GetForce(0, time);
    }

    public string modelInfoFilename = "";
	ManikinControlPointsTimeline ctrlPointsTimeline;
    public ManikinTimeline(string modelInfoFilename, string ctrlPointsFilename)
    {
        ctrlPointsTimeline = ManikinControlPointsTimeline.FromFile(ctrlPointsFilename);	
		this.modelInfoFilename = modelInfoFilename;
        modelInfo = ModelInfo.FromFile(modelInfoFilename);
		initJoints(modelInfo);
		duration = getFinalTime();
	}
	public override void OnTimeChanged() {
		frameInterPolationInfo = getFrameInterpolationInfo(time);
    }
	#region A lot of joint lines

	public MJoint translation;
	public MJoint rotation;
	public MJoint jointL5S1;
	public MJoint jointL3L4;
	public MJoint jointT12L1;
	public MJoint jointT6T7;
	public MJoint jointT1T2;
	public MJoint jointC6C7;
	public MJoint jointC4C5;
	public MJoint jointAtlantoAxial;
	public MJoint jointEyeside;
	public MJoint jointLeftHip;
	public MJoint jointLeftKnee;
	public MJoint jointLeftAnkleRot;
	public MJoint jointLeftAnkle;
	public MJoint jointLeftToes;
	public MJoint jointRightHip;
	public MJoint jointRightKnee;
	public MJoint jointRightAnkleRot;
	public MJoint jointRightAnkle;
	public MJoint jointRightToes;
	public MJoint jointRightSC;
	public MJoint jointRightAC;
	public MJoint jointRightGH;
	public MJoint jointRightShoulderRotation;
	public MJoint jointRightElbow;
	public MJoint jointRightWristRotation;
	public MJoint jointLeftSC;
	public MJoint jointLeftAC;
	public MJoint jointLeftGH;
	public MJoint jointLeftShoulderRotation;
	public MJoint jointLeftElbow;
	public MJoint jointLeftWristRotation;
	public MJoint jointLeftWrist;
	public MJoint jointLeft_IndexCarpal;
	public MJoint jointLeft_IndexProximal;
	public MJoint jointLeft_IndexIntermediate;
	public MJoint jointLeft_IndexDistal;
	public MJoint jointLeft_MiddleCarpal;
	public MJoint jointLeft_MiddleProximal;
	public MJoint jointLeft_MiddleIntermediate;
	public MJoint jointLeft_MiddleDistal;
	public MJoint jointLeft_RingCarpal;
	public MJoint jointLeft_RingProximal;
	public MJoint jointLeft_RingIntermediate;
	public MJoint jointLeft_RingDistal;
	public MJoint jointLeft_PinkyCarpal;
	public MJoint jointLeft_PinkyProximal;
	public MJoint jointLeft_PinkyIntermediate;
	public MJoint jointLeft_PinkyDistal;
	public MJoint jointLeft_ThumbProximal;
	public MJoint jointLeft_ThumbIntermediate;
	public MJoint jointLeft_ThumbDistal;
	public MJoint jointRightWrist;
	public MJoint jointRight_IndexCarpal;
	public MJoint jointRight_IndexProximal;
	public MJoint jointRight_IndexIntermediate;
	public MJoint jointRight_IndexDistal;
	public MJoint jointRight_MiddleCarpal;
	public MJoint jointRight_MiddleProximal;
	public MJoint jointRight_MiddleIntermediate;
	public MJoint jointRight_MiddleDistal;
	public MJoint jointRight_RingCarpal;
	public MJoint jointRight_RingProximal;
	public MJoint jointRight_RingIntermediate;
	public MJoint jointRight_RingDistal;
	public MJoint jointRight_PinkyCarpal;
	public MJoint jointRight_PinkyProximal;
	public MJoint jointRight_PinkyIntermediate;
	public MJoint jointRight_PinkyDistal;
	public MJoint jointRight_ThumbProximal;
	public MJoint jointRight_ThumbIntermediate;
	public MJoint jointRight_ThumbDistal;

	void initJoints(ModelInfo modelInfo) {
		joints = new List<MJoint>();
		translation = initJoint("Translation", modelInfo);
		rotation = initJoint("Rotation", modelInfo);
		jointL5S1 = initJoint("L5S1", modelInfo);
		jointL3L4 = initJoint("L3L4", modelInfo);
		jointT12L1 = initJoint("T12L1", modelInfo);
		jointT6T7 = initJoint("T6T7", modelInfo);
		jointT1T2 = initJoint("T1T2", modelInfo);
		jointC6C7 = initJoint("C6C7", modelInfo);
		jointC4C5 = initJoint("C4C5", modelInfo);
		jointAtlantoAxial = initJoint("AtlantoAxial", modelInfo);
		jointEyeside = initJoint("Eyeside", modelInfo);
		jointLeftHip = initJoint("LeftHip", modelInfo);
		jointLeftKnee = initJoint("LeftKnee", modelInfo);
		jointLeftAnkleRot = initJoint("LeftAnkleRot", modelInfo);
		jointLeftAnkle = initJoint("LeftAnkle", modelInfo);
		jointLeftToes = initJoint("LeftToes", modelInfo);
		jointRightHip = initJoint("RightHip", modelInfo);
		jointRightKnee = initJoint("RightKnee", modelInfo);
		jointRightAnkleRot = initJoint("RightAnkleRot", modelInfo);
		jointRightAnkle = initJoint("RightAnkle", modelInfo);
		jointRightToes = initJoint("RightToes", modelInfo);
		jointRightSC = initJoint("RightSC", modelInfo);
		jointRightAC = initJoint("RightAC", modelInfo);
		jointRightGH = initJoint("RightGH", modelInfo);
		jointRightShoulderRotation = initJoint("RightShoulderRotation", modelInfo);
		jointRightElbow = initJoint("RightElbow", modelInfo);
		jointRightWristRotation = initJoint("RightWristRotation", modelInfo);
		jointLeftSC = initJoint("LeftSC", modelInfo);
		jointLeftAC = initJoint("LeftAC", modelInfo);
		jointLeftGH = initJoint("LeftGH", modelInfo);
		jointLeftShoulderRotation = initJoint("LeftShoulderRotation", modelInfo);
		jointLeftElbow = initJoint("LeftElbow", modelInfo);
		jointLeftWristRotation = initJoint("LeftWristRotation", modelInfo);
		jointLeftWrist = initJoint("LeftWrist", modelInfo);
		jointLeft_IndexCarpal = initJoint("Left_IndexCarpal", modelInfo);
		jointLeft_IndexProximal = initJoint("Left_IndexProximal", modelInfo);
		jointLeft_IndexIntermediate = initJoint("Left_IndexIntermediate", modelInfo);
		jointLeft_IndexDistal = initJoint("Left_IndexDistal", modelInfo);
		jointLeft_MiddleCarpal = initJoint("Left_MiddleCarpal", modelInfo);
		jointLeft_MiddleProximal = initJoint("Left_MiddleProximal", modelInfo);
		jointLeft_MiddleIntermediate = initJoint("Left_MiddleIntermediate", modelInfo);
		jointLeft_MiddleDistal = initJoint("Left_MiddleDistal", modelInfo);
		jointLeft_RingCarpal = initJoint("Left_RingCarpal", modelInfo);
		jointLeft_RingProximal = initJoint("Left_RingProximal", modelInfo);
		jointLeft_RingIntermediate = initJoint("Left_RingIntermediate", modelInfo);
		jointLeft_RingDistal = initJoint("Left_RingDistal", modelInfo);
		jointLeft_PinkyCarpal = initJoint("Left_PinkyCarpal", modelInfo);
		jointLeft_PinkyProximal = initJoint("Left_PinkyProximal", modelInfo);
		jointLeft_PinkyIntermediate = initJoint("Left_PinkyIntermediate", modelInfo);
		jointLeft_PinkyDistal = initJoint("Left_PinkyDistal", modelInfo);
		jointLeft_ThumbProximal = initJoint("Left_ThumbProximal", modelInfo);
		jointLeft_ThumbIntermediate = initJoint("Left_ThumbIntermediate", modelInfo);
		jointLeft_ThumbDistal = initJoint("Left_ThumbDistal", modelInfo);
		jointRightWrist = initJoint("RightWrist", modelInfo);
		jointRight_IndexCarpal = initJoint("Right_IndexCarpal", modelInfo);
		jointRight_IndexProximal = initJoint("Right_IndexProximal", modelInfo);
		jointRight_IndexIntermediate = initJoint("Right_IndexIntermediate", modelInfo);
		jointRight_IndexDistal = initJoint("Right_IndexDistal", modelInfo);
		jointRight_MiddleCarpal = initJoint("Right_MiddleCarpal", modelInfo);
		jointRight_MiddleProximal = initJoint("Right_MiddleProximal", modelInfo);
		jointRight_MiddleIntermediate = initJoint("Right_MiddleIntermediate", modelInfo);
		jointRight_MiddleDistal = initJoint("Right_MiddleDistal", modelInfo);
		jointRight_RingCarpal = initJoint("Right_RingCarpal", modelInfo);
		jointRight_RingProximal = initJoint("Right_RingProximal", modelInfo);
		jointRight_RingIntermediate = initJoint("Right_RingIntermediate", modelInfo);
		jointRight_RingDistal = initJoint("Right_RingDistal", modelInfo);
		jointRight_PinkyCarpal = initJoint("Right_PinkyCarpal", modelInfo);
		jointRight_PinkyProximal = initJoint("Right_PinkyProximal", modelInfo);
		jointRight_PinkyIntermediate = initJoint("Right_PinkyIntermediate", modelInfo);
		jointRight_PinkyDistal = initJoint("Right_PinkyDistal", modelInfo);
		jointRight_ThumbProximal = initJoint("Right_ThumbProximal", modelInfo);
		jointRight_ThumbIntermediate = initJoint("Right_ThumbIntermediate", modelInfo);
		jointRight_ThumbDistal = initJoint("Right_ThumbDistal", modelInfo);

		jointL5S1.SetParent(null);
		jointL3L4.SetParent(jointL5S1);
		jointT12L1.SetParent(jointL3L4);
		jointT6T7.SetParent(jointT12L1);
		jointT1T2.SetParent(jointT6T7);
		jointC6C7.SetParent(jointT1T2);
		jointC4C5.SetParent(jointC6C7);
		jointAtlantoAxial.SetParent(jointC4C5);
		jointEyeside.SetParent(jointAtlantoAxial);
		jointLeftHip.SetParent(jointL5S1);
		jointLeftKnee.SetParent(jointLeftHip);
		jointLeftAnkleRot.SetParent(jointLeftKnee);
		jointLeftAnkle.SetParent(jointLeftAnkleRot);
		jointLeftToes.SetParent(jointLeftAnkle);
		jointRightHip.SetParent(jointL5S1);
		jointRightKnee.SetParent(jointRightHip);
		jointRightAnkleRot.SetParent(jointRightKnee);
		jointRightAnkle.SetParent(jointRightAnkleRot);
		jointRightToes.SetParent(jointRightAnkle);
		jointRightSC.SetParent(jointT1T2);
		jointRightAC.SetParent(jointRightSC);
		jointRightGH.SetParent(jointRightAC);
		jointRightShoulderRotation.SetParent(jointRightGH);
		jointRightElbow.SetParent(jointRightShoulderRotation);
		jointRightWristRotation.SetParent(jointRightElbow);
		jointLeftSC.SetParent(jointT1T2);
		jointLeftAC.SetParent(jointLeftSC);
		jointLeftGH.SetParent(jointLeftAC);
		jointLeftShoulderRotation.SetParent(jointLeftGH);
		jointLeftElbow.SetParent(jointLeftShoulderRotation);
		jointLeftWristRotation.SetParent(jointLeftElbow);
		jointLeftWrist.SetParent(jointLeftWristRotation);
		jointLeft_IndexCarpal.SetParent(jointLeftWrist);
		jointLeft_IndexProximal.SetParent(jointLeft_IndexCarpal);
		jointLeft_IndexIntermediate.SetParent(jointLeft_IndexProximal);
		jointLeft_IndexDistal.SetParent(jointLeft_IndexIntermediate);
		jointLeft_MiddleCarpal.SetParent(jointLeftWrist);
		jointLeft_MiddleProximal.SetParent(jointLeft_MiddleCarpal);
		jointLeft_MiddleIntermediate.SetParent(jointLeft_MiddleProximal);
		jointLeft_MiddleDistal.SetParent(jointLeft_MiddleIntermediate);
		jointLeft_RingCarpal.SetParent(jointLeftWrist);
		jointLeft_RingProximal.SetParent(jointLeft_RingCarpal);
		jointLeft_RingIntermediate.SetParent(jointLeft_RingProximal);
		jointLeft_RingDistal.SetParent(jointLeft_RingIntermediate);
		jointLeft_PinkyCarpal.SetParent(jointLeftWrist);
		jointLeft_PinkyProximal.SetParent(jointLeft_PinkyCarpal);
		jointLeft_PinkyIntermediate.SetParent(jointLeft_PinkyProximal);
		jointLeft_PinkyDistal.SetParent(jointLeft_PinkyIntermediate);
		jointLeft_ThumbProximal.SetParent(jointLeftWrist);
		jointLeft_ThumbIntermediate.SetParent(jointLeft_ThumbProximal);
		jointLeft_ThumbDistal.SetParent(jointLeft_ThumbIntermediate);
		jointRightWrist.SetParent(jointRightWristRotation);
		jointRight_IndexCarpal.SetParent(jointRightWrist);
		jointRight_IndexProximal.SetParent(jointRight_IndexCarpal);
		jointRight_IndexIntermediate.SetParent(jointRight_IndexProximal);
		jointRight_IndexDistal.SetParent(jointRight_IndexIntermediate);
		jointRight_MiddleCarpal.SetParent(jointRightWrist);
		jointRight_MiddleProximal.SetParent(jointRight_MiddleCarpal);
		jointRight_MiddleIntermediate.SetParent(jointRight_MiddleProximal);
		jointRight_MiddleDistal.SetParent(jointRight_MiddleIntermediate);
		jointRight_RingCarpal.SetParent(jointRightWrist);
		jointRight_RingProximal.SetParent(jointRight_RingCarpal);
		jointRight_RingIntermediate.SetParent(jointRight_RingProximal);
		jointRight_RingDistal.SetParent(jointRight_RingIntermediate);
		jointRight_PinkyCarpal.SetParent(jointRightWrist);
		jointRight_PinkyProximal.SetParent(jointRight_PinkyCarpal);
		jointRight_PinkyIntermediate.SetParent(jointRight_PinkyProximal);
		jointRight_PinkyDistal.SetParent(jointRight_PinkyIntermediate);
		jointRight_ThumbProximal.SetParent(jointRightWrist);
		jointRight_ThumbIntermediate.SetParent(jointRight_ThumbProximal);
		jointRight_ThumbDistal.SetParent(jointRight_ThumbIntermediate);
	}
    #endregion
}
