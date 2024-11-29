using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace IMMA;

public class ModelInfo {
	public string filename;
	public List<string> jointNames = new List<string>();
	public List<int> jointStateStart = new List<int>();
	public List<int> jointDoF = new List<int>();
	public string manikinName = "";
	public bool isMale;
	public List<float> timeSteps = new List<float>();
	public PostureData zeroPosture = new PostureData();
	PostureDataParamReader[] paramReaders = PostureDataParamReader.CreateReaders();
	public int dof = 0;
	int jointCnt;
	Dictionary<float, List<int>> timeMappedRawFrameIndices = new Dictionary<float, List<int>>();
	Dictionary<int, PostureData> postureDataCache = new Dictionary<int, PostureData>();
	List<int> cachedIndices = new List<int>();
	public bool hasZeroPose() {
		return zeroPosture.mJointVec.Count != 0;
	}
	bool useCache = false;
	public PostureData GetPostureData(int frameIdx) {
        if (useCache && postureDataCache.ContainsKey(frameIdx)) {
            return postureDataCache.GetValueOrDefault(frameIdx, null);
        }
        List<int> rawIndices=timeMappedRawFrameIndices.GetValueOrDefault(timeSteps[frameIdx]);
		PostureData pd = ReadRawPostureDataFrame(rawIndices[0]);
		return pd;
        if (rawIndices.Count > 1) {
			PostureData pd2 = ReadRawPostureDataFrame(rawIndices[1]);
			pd = PostureData.Merge(pd, pd2);
        }
        if (useCache) {
			postureDataCache.Add(frameIdx, pd);
			cachedIndices.Add(frameIdx);
			if (cachedIndices.Count > 10) {
				postureDataCache.Remove(cachedIndices[0]);
				cachedIndices.RemoveAt(0);
			}
		}
		return pd;
    }
	public static ModelInfo FromFile(string filename) {
		ModelInfo modelInfo = new ModelInfo();
		modelInfo.ParseFile(filename);
		return modelInfo;
	}
	BinFileReader fileReader;
	void ParseFile(string filename) {
		fileReader = new BinFileReader(filename);
		fileReader.float64 = true;
		string headerJson = fileReader.readString();
		JsonConvert.PopulateObject(headerJson, this);
		//JsonUtility.FromJsonOverwrite(headerJson, this);
        List<float> uniqueTimesteps = new List<float>();
        for (int i = 0; i < timeSteps.Count; i++) {
            float t = timeSteps[i];
			int fIdx = i + 1;
            List<int> indices;
            if (timeMappedRawFrameIndices.TryGetValue(t, out indices)) {
                indices.Add(fIdx);
            }
            else {
                timeMappedRawFrameIndices.Add(t, new List<int>() { fIdx });
                uniqueTimesteps.Add(t);
            }
        }
        timeSteps = uniqueTimesteps;
		jointCnt = jointNames.Count;
		JObject headerJo = JObject.Parse(headerJson);
		foreach (JToken pdtJo in headerJo["postureDataTimelines"].Children()) {
			PostureDataParamReader pr = GetParamReader(pdtJo["name"].ToString());
			int frameRelPos = 0;
			foreach (int frameSize in JObject.Parse(fileReader.readString())["frameSizes"].ToObject<List<int>>()) {
				pr.frameRelPositions.Add(frameRelPos);
				frameRelPos += frameSize;
			}
			pr.startPos = fileReader.getPos();
			fileReader.position += frameRelPos;
		}
		zeroPosture = ReadRawPostureDataFrame(0);
	}
	PostureDataParamReader GetParamReader(string name) {
		foreach (PostureDataParamReader r in paramReaders) {
			if (r.name == name) {
				return r;
			}
		}
		return null;
	}
	PostureData ReadRawPostureDataFrame(int rawFrameIdx) {
		PostureData pd = new PostureData(jointCnt, dof);
		foreach (PostureDataParamReader pr in paramReaders) {
			pr.Read(fileReader, pd, rawFrameIdx);
		}
		return pd;
	}
	//public string ReadGrip(int frameIdx, Hand hand) {
	//	PostureData pd = new PostureData(jointCnt, dof);
	//	PostureDataParamReader reader = GetParamReader(hand == Hand.Left ? "leftGrip" : "rightGrip");
	//	foreach (int rawIdx in timeMappedRawFrameIndices.GetValueOrDefault(timeSteps[frameIdx])) {
	//		reader.Read(fileReader, pd, rawIdx);
	//		string gripStr = hand == Hand.Left ? pd.mLeftGrip : pd.mRightGrip;
 //           if (gripStr.Length > 0) {
	//			return gripStr;
 //           }
	//	}
	//	return null;
	//}


	public void CloseFileReader() {
		if (fileReader != null) {
			fileReader.Close();
			fileReader = null;
		}

	}
	~ModelInfo() {
		CloseFileReader();
	}

};