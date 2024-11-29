using System.Collections;
using System.Collections.Generic;
using System.Numerics;



namespace IMMA;

public class ContactForceData
{
    public Vector3 mForce = Vector3.Zero;
    public Vector3 mTorque = Vector3.Zero;
    public int mJointIndex = -1;
}


[System.Serializable]
public class MJoint{
	public IMMAManikin timeline = null;
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

