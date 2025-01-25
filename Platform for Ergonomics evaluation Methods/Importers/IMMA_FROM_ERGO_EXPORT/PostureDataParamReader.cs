using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace IMMA_BY_ERGO_EXPORT;
public class PostureDataParamReader {
	public string name;
	public long size;
	public long startPos;
	public List<int> frameRelPositions = new List<int>();
	public System.Action<BinFileReader, PostureData> Populate;
	PostureDataParamReader(string name, System.Action<BinFileReader, PostureData> Populate) {
		this.name = name;
		this.Populate = Populate;
	}
	public void Read(BinFileReader reader, PostureData pd, int frameIdx) {
		reader.position = startPos + frameRelPositions[frameIdx];
		Populate(reader, pd);
	}

	public static PostureDataParamReader[] CreateReaders() {
		return new PostureDataParamReader[] {
			new PostureDataParamReader("contactForces", delegate (BinFileReader reader, PostureData postureData) {
				int cnt = reader.readInt();
				for (int i = 0; i < cnt; i++) {
					ContactForceData contactForce = new ContactForceData();
					contactForce.mJointIndex = reader.readInt();
					contactForce.mForce = reader.readVector3();
					contactForce.mTorque = reader.readVector3();
					if (contactForce.mForce != Vector3.Zero) {
						//Debug.Log(JsonUtility.ToJson(contactForce));
					}
					postureData.mContactForces.Add(contactForce);
				}
			}),
			new PostureDataParamReader("jointTrans", delegate (BinFileReader reader, PostureData postureData) {
				int cnt = reader.readInt();
				for (int i = 0; i < cnt; i++) {
					postureData.mJointTrans[i] = reader.readTransform();
				}
			}),
			new PostureDataParamReader("jointTorques", delegate (BinFileReader reader, PostureData postureData) {
				int cnt = reader.readInt();
				for (int i = 0; i < cnt; i++) {
					postureData.mJointTorques[i] = reader.readVector3();
				}
			}),
			new PostureDataParamReader("jointVec", delegate (BinFileReader reader, PostureData postureData) {
				int cnt = reader.readInt();
				for (int i = 0; i < cnt; i++) {
					postureData.mJointVec[i] = reader.readFloat();
				}
			}),
			new PostureDataParamReader("com", delegate (BinFileReader reader, PostureData postureData) {
				postureData.mCoM = reader.readVector3();
			}),
			new PostureDataParamReader("time", delegate (BinFileReader reader, PostureData postureData) {
				postureData.time = reader.readFloat();
			}),
			new PostureDataParamReader("leftGrip", delegate (BinFileReader reader, PostureData postureData) {
				postureData.mLeftGrip = reader.readString();
			}),
			new PostureDataParamReader("rightGrip", delegate (BinFileReader reader, PostureData postureData) {
				postureData.mRightGrip = reader.readString();
			})
		};
	}
}
