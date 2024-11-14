using System.Collections;
using System.Collections.Generic;
using System.Numerics;
[System.Serializable]
public class PostureData {
	public PostureData() { 
	}

	public PostureData(int iNrJoint, int iDoF){
		mJointVec = new List<float>(new float[iDoF]);
		mJointTorques = new List<Vector3>(new Vector3[iNrJoint]);
		mJointTrans = new List<Matrix4x4>(new Matrix4x4[iNrJoint]);
	}
	//The joint angles in radians for all joints.
	public List<float> mJointVec = new List<float>();
	//Contains all the joint positions and orientations saved as transformations
	public List<Matrix4x4> mJointTrans = new List<Matrix4x4>();
	// Contact forces
	public List<ContactForceData> mContactForces=new List<ContactForceData>();
	// Joint torques
	public List<Vector3> mJointTorques=new List<Vector3>();
	// Center of mass
	public Vector3 mCoM = Vector3.Zero;

	// Will be title of grip
	public string mLeftGrip = "";
	public string mRightGrip = "";
	public float time = 0; //in seconds
	public static PostureData Merge(PostureData pd0, PostureData pd1) {
		PostureData ret = new PostureData();
		ret.mCoM = (pd0.mCoM + pd1.mCoM) * .5f;
		PostureData pdDefault = pd1;
		for (int i = 0; i < pd0.mContactForces.Count; i++) {
			ContactForceData cfd0 = pd0.mContactForces[i];
			ContactForceData cfd1 = pd1.mContactForces[i];
			ContactForceData cfd = new ContactForceData();
			cfd.mForce = (cfd0.mForce + cfd1.mForce) * .5f;
			cfd.mTorque = (cfd0.mTorque + cfd1.mTorque) * .5f;
			cfd.mJointIndex = cfd0.mJointIndex;
			ret.mContactForces.Add(cfd);
		}
		for (int i = 0; i < pd0.mJointTorques.Count; i++) {
			ret.mJointTorques.Add((pd0.mJointTorques[i] + pd1.mJointTorques[i]) * .5f);
		}
		bool hasUnmergedTransform = false;
		for (int i = 0; i < pd0.mJointTrans.Count; i++) {
			hasUnmergedTransform |= !pd0.mJointTrans[i].Equals(pd1.mJointTrans[i]);
			ret.mJointTrans.Add(pdDefault.mJointTrans[i]);
		}
		ret.mJointVec = new List<float>(new float[pd0.mJointVec.Count]);
		for (int i = 0; i < pd0.mJointVec.Count; i++) {
			ret.mJointVec[i] = (pd0.mJointVec[i] + pd1.mJointVec[i]) * .5f;
		}
		ret.mLeftGrip = pdDefault.mLeftGrip;
		ret.mRightGrip = pdDefault.mRightGrip;
		ret.time = pdDefault.time;
		//if (hasUnmergedTransform) {
		//	Debug.Log("Unmerged transform at " + pdDefault.time);
		//}
		//if (pd0.mLeftGrip != pd1.mLeftGrip || pd0.mRightGrip != pd1.mRightGrip) {
		//	Debug.Log("Unmerged grip at " + pdDefault.time);
		//}
		return ret;
	}
}

