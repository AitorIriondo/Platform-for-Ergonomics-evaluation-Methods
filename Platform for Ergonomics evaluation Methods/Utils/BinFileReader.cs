using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
public class BinFileReader {
	Stream file;
	public BinaryReader reader;
	string filename;
	public bool float64 = false;
	public bool bigEndian = false;
	public BinFileReader(string filename) {
		this.filename = filename;
		file = File.OpenRead(filename);
		reader = new BinaryReader(file);
	}
	public void Close() {
		file.Close();
	}
	public long available { get { return file.Length - file.Position; } }
	public long getPos() {
		return file.Position;
	}
	public long position { get { return file.Position; } set { file.Position = value; } }
	//virtual ~BinFileReader() {
	//	close();
	//}
	byte[] ReadBytesAndReverse(int cnt) {
		byte[] bytes = reader.ReadBytes(cnt);
		Array.Reverse(bytes);
		return bytes;
	}
	public float readFloat() {
		if (bigEndian) {
			return float64 ? (float)BitConverter.ToDouble(ReadBytesAndReverse(8)) : BitConverter.ToSingle(ReadBytesAndReverse(4));
		}
		return float64 ? (float)reader.ReadDouble() : reader.ReadSingle();
	}
	public void readFloats(List<float> vals, int cnt) {
		cnt = Math.Min(vals.Count, cnt);
		for(int i = 0; i < cnt; i++) {
			vals[i] = readFloat();
        }
	}
	public int readInt() {
		if (bigEndian) {
			return BitConverter.ToInt32(ReadBytesAndReverse(4));
		}
		return reader.ReadInt32();
	}
	public Vector3 readVector3() {
		return new Vector3(readFloat(), readFloat(), readFloat());
	}
	public Quaternion readQuaternion() {
		return new Quaternion(readFloat(), readFloat(), readFloat(), readFloat());
	}
	public Matrix4x4 readTransform() {
		Matrix4x4 ret = new Matrix4x4(
            readFloat(), readFloat(), readFloat(), readFloat(),
			readFloat(), readFloat(), readFloat(), readFloat(),
			readFloat(), readFloat(), readFloat(), readFloat(),
            readFloat(), readFloat(), readFloat(), readFloat()
		);
		//for (int i = 0; i < 16; i++) {
		//	ret[i] = readFloat();
		//}
		return ret;
	}
	public string readString() {
		int strLen = readInt();
		if (strLen == 0) {
			return "";
		}
		char[] chars = reader.ReadChars(strLen);
		return new string(chars);
	}


}
