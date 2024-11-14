using System.Collections;
using System.Collections.Generic;
using System.Numerics;

public class ContactForceData {
	public Vector3 mForce = Vector3.Zero;
	public Vector3 mTorque = Vector3.Zero;
	public int mJointIndex = -1;
}

public class MTransform {
	public Vector3 pos { get { return matrix.GetPosition(); } }
	public Quaternion rot { get { return matrix.GetRotation(); } }
	public Matrix4x4 matrix = new Matrix4x4();
	public MTransform() {
    }
    public bool Equals(MTransform other) {
		return other != null && other.pos == pos && other.rot == rot;
    }
}
public class FrameInterpolationInfo {
	public int lowIdx = 0;
	public int highIdx = -1;
	public float factor = 0;
	public float time=0;
	public bool isApplicable() {
		return highIdx > lowIdx && factor > 0;
	}
}
[System.Serializable]
public class MJoint{
	public ManikinTimeline timeline = null;
	public string name;
	public MJoint parentJoint;
	public int idx = -1;
	public int angleCnt = 0;
	public int firstAngleIdx = 0;
	public int contactForceIdx = -1;
	public int getIdx() { return idx; }
	public List<int> childIndices = new List<int>();
	public void SetParent(MJoint parent) {
		parentJoint = parent;
        if (parent != null) {
			parent.childIndices.Add(idx);
        }
    }
	PostureData GetPostureData(int frameIdx) {
		return timeline.modelInfo.GetPostureData(frameIdx);
	}
	Vector3 interpolate(Vector3 v0, Vector3 v1, float factor) {
		return v0 + (v1 - v0) * factor;
	}
	Vector3 posAtFrame(int frameIdx) {
		return getTransformMatrix(frameIdx).GetPosition();
	}

	Quaternion rotAtFrame(int frameIdx) {
		return getTransformMatrix(frameIdx).GetRotation();
	}
	Matrix4x4 getTransformMatrix(int frameIdx) {
		return GetPostureData(frameIdx).mJointTrans[idx];
	}

	public Vector3 pos(float time) {
		FrameInterpolationInfo interpolation = timeline.getFrameInterpolationInfo(time);
		Vector3 v0 = posAtFrame(interpolation.lowIdx);
		if (!interpolation.isApplicable()) {
			return v0;
		}
		return interpolate(v0, posAtFrame(interpolation.highIdx), interpolation.factor);
	}
	public Quaternion rot(float time) {
		FrameInterpolationInfo interpolation = timeline.getFrameInterpolationInfo(time);
		Quaternion q0 = rotAtFrame(interpolation.lowIdx);
		if (!interpolation.isApplicable()) {
			return q0;
		}
		return Quaternion.Slerp(q0, rotAtFrame(interpolation.highIdx), interpolation.factor);
	}
	public float[] angles(float time) {
		FrameInterpolationInfo interpolation = timeline.getFrameInterpolationInfo(time);
		float[] angs0 = anglesAtFrame(interpolation.lowIdx);
		if (!interpolation.isApplicable()) {
			return angs0;
		}
		float[] angs1 = anglesAtFrame(interpolation.highIdx);
		for (int i = 0; i < angleCnt; i++) {
			angs0[i] += (angs1[i] - angs0[i]) * interpolation.factor;
		}
		return angs0;
	}

	public float angle(float time, int angleIdx) {
		FrameInterpolationInfo interpolation = timeline.getFrameInterpolationInfo(time);
		float ang0 = angleAtFrame(interpolation.lowIdx, angleIdx);
		if (!interpolation.isApplicable()) {
			return ang0;
		}
		return ang0 + (angleAtFrame(interpolation.highIdx, angleIdx) - ang0) * interpolation.factor;
	}

	public Vector3 torque(float time) {
		FrameInterpolationInfo interpolation = timeline.getFrameInterpolationInfo(time);
		Vector3 v0 = GetPostureData(interpolation.lowIdx).mJointTorques[idx];
		if (!timeline.interpolateTorque || !interpolation.isApplicable()) {
			return v0;
		}
		return interpolate(v0, GetPostureData(interpolation.highIdx).mJointTorques[idx], interpolation.factor);
	}


	public float[] anglesAtFrame(int frameIdx) {
		float[] ret=new float[angleCnt];
		for (int i = 0; i < angleCnt; i++) {
			ret[i] = angleAtFrame(frameIdx, i);
		}
		return ret;

	}
	public float angleAtFrame(int frameIdx, int angleIdx) {
		return GetPostureData(frameIdx).mJointVec[firstAngleIdx + angleIdx];
	}
	public Vector3 pos() {
		return pos(timeline.time);

	}
	public Quaternion rot() {
		return rot(timeline.time);
	}
	public float[] angles() {
		return angles(timeline.time);
	}

	public float angle(int angleIdx) {
		return angle(timeline.time, angleIdx);
	}
	Vector3 torque() {
		return torque(timeline.time);
	}

	public bool hasContactForceData() {
		return contactForceIdx >= 0;
	}

	public ContactForceData contactForceData() {
		return contactForceData(timeline.time);
	}

	public ContactForceData contactForceData(float time) {
		ContactForceData cfd = new ContactForceData();
		if (!hasContactForceData()) {
			return cfd;
		}
		cfd.mJointIndex = idx;
		FrameInterpolationInfo interpolation = timeline.getFrameInterpolationInfo(time);
		ContactForceData cfd0 = GetPostureData(interpolation.lowIdx).mContactForces[contactForceIdx];
		if (!timeline.interpolateContactForceData || !interpolation.isApplicable()) {
			cfd.mForce = cfd0.mForce;
			cfd.mTorque = cfd0.mTorque;
			return cfd;
		}
		ContactForceData cfd1 = GetPostureData(interpolation.highIdx).mContactForces[contactForceIdx];
		cfd.mForce = interpolate(cfd0.mForce, cfd1.mForce, interpolation.factor);
		cfd.mTorque = interpolate(cfd0.mTorque, cfd1.mTorque, interpolation.factor);
		return cfd;
	}

}
public enum JointEnum {
	Translation = 0,
	Rotation = 1,
	L5S1 = 2,
	L3L4 = 3,
	T12L1 = 4,
	T6T7 = 5,
	T1T2 = 6,
	C6C7 = 7,
	C4C5 = 8,
	AtlantoAxial = 9,
	Eyeside = 10,
	LeftHip = 11,
	LeftKnee = 12,
	LeftAnkleRot = 13,
	LeftAnkle = 14,
	LeftToes = 15,
	RightHip = 16,
	RightKnee = 17,
	RightAnkleRot = 18,
	RightAnkle = 19,
	RightToes = 20,
	RightSC = 21,
	RightAC = 22,
	RightGH = 23,
	RightShoulderRotation = 24,
	RightElbow = 25,
	RightWristRotation = 26,
	LeftSC = 27,
	LeftAC = 28,
	LeftGH = 29,
	LeftShoulderRotation = 30,
	LeftElbow = 31,
	LeftWristRotation = 32,
	LeftWrist = 33,
	Left_IndexCarpal = 34,
	Left_IndexProximal = 35,
	Left_IndexIntermediate = 36,
	Left_IndexDistal = 37,
	Left_MiddleCarpal = 38,
	Left_MiddleProximal = 39,
	Left_MiddleIntermediate = 40,
	Left_MiddleDistal = 41,
	Left_RingCarpal = 42,
	Left_RingProximal = 43,
	Left_RingIntermediate = 44,
	Left_RingDistal = 45,
	Left_PinkyCarpal = 46,
	Left_PinkyProximal = 47,
	Left_PinkyIntermediate = 48,
	Left_PinkyDistal = 49,
	Left_ThumbProximal = 50,
	Left_ThumbIntermediate = 51,
	Left_ThumbDistal = 52,
	RightWrist = 53,
	Right_IndexCarpal = 54,
	Right_IndexProximal = 55,
	Right_IndexIntermediate = 56,
	Right_IndexDistal = 57,
	Right_MiddleCarpal = 58,
	Right_MiddleProximal = 59,
	Right_MiddleIntermediate = 60,
	Right_MiddleDistal = 61,
	Right_RingCarpal = 62,
	Right_RingProximal = 63,
	Right_RingIntermediate = 64,
	Right_RingDistal = 65,
	Right_PinkyCarpal = 66,
	Right_PinkyProximal = 67,
	Right_PinkyIntermediate = 68,
	Right_PinkyDistal = 69,
	Right_ThumbProximal = 70,
	Right_ThumbIntermediate = 71,
	Right_ThumbDistal = 72
}