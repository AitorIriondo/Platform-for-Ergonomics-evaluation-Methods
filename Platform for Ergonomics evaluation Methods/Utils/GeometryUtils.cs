using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Numerics;
using MathNet.Numerics.Interpolation;

public class GeometryUtils
{
}


public class Vector3JsonConverter : JsonConverter<Vector3>
{
    public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        // Ensure the token is a StartArray
        if (reader.TokenType != JsonToken.StartArray)
        {
            throw new JsonException("Expected a JSON array for Vector3.");
        }

        // Read the components from the array
        JArray array = JArray.Load(reader);
        if (array.Count != 3)
        {
            throw new JsonException("Expected an array with exactly 3 elements for Vector3.");
        }

        float x = array[0].ToObject<float>();
        float y = array[1].ToObject<float>();
        float z = array[2].ToObject<float>();

        return new Vector3(x, y, z);
    }

    public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
    {
        // Write Vector3 as a JSON array
        writer.WriteStartArray();
        writer.WriteValue(value.X);
        writer.WriteValue(value.Y);
        writer.WriteValue(value.Z);
        writer.WriteEndArray();
    }
}

public static class Vector3Extension
{
    public static Vector3 Normalized(this Vector3 v)
    {
        if (true || v.Length() > 0)
        {
            return Vector3.Normalize(v);
        }
        return Vector3.Zero;
    }
}

public static class Matrix4x4Extension
{
    public static Vector3 GetPosition(this Matrix4x4 matrix)
    {
        return matrix.Translation;
    }
    public static Quaternion GetRotation(this Matrix4x4 matrix)
    {
        throw new NotImplementedException();
        return Quaternion.Identity;
    }
}


public class MTransform
{
    public Vector3 pos { get { return matrix.GetPosition(); } }
    public Quaternion rot { get { return matrix.GetRotation(); } }
    public Matrix4x4 matrix = new Matrix4x4();
    public MTransform()
    {
    }
    public bool Equals(MTransform other)
    {
        return other != null && other.pos == pos && other.rot == rot;
    }
}

public class FrameInterpolator
{
    public int lowIdx = 0;
    public int highIdx = -1;
    public float factor = 0;
    public float time = -1;
    public bool isApplicable()
    {
        return highIdx > lowIdx && factor > 0;
    }
    public Vector3 interpolate(Vector3 v0, Vector3 v1)
    {
        return v0 + (v1 - v0) * factor;
    }

    public Vector3 interpolate(List<Vector3> frames)
    {
        Vector3 v0 = frames[lowIdx];
        if (!isApplicable())
        {
            return v0;
        }
        return interpolate(v0, frames[highIdx]);

    }
    public FrameInterpolator() { }
    public FrameInterpolator(float time, List<float> timeSteps, bool justLowIdx = false)
    {
        lowIdx = timeSteps.IndexOf(time);
        if (lowIdx >= 0)
        {
            return;
        }
        lowIdx = 0;
        if (time <= 0)
        {
            return;
        }
        if (time >= timeSteps[timeSteps.Count - 1])
        {
            lowIdx = timeSteps.Count - 1;
            return;
        }

        while (timeSteps[lowIdx] < time)
        {
            lowIdx++;
        }
        lowIdx--;
        if (justLowIdx)
        {
            return;
        }
        float lowTime = timeSteps[lowIdx];
        if (lowTime == time || lowIdx >= timeSteps.Count - 1)
        {
            return;
        }
        highIdx = lowIdx + 1;
        while (highIdx < timeSteps.Count && timeSteps[highIdx] == lowTime)
        {
            highIdx++;
        }
        float tRange = timeSteps[highIdx] - lowTime;
        if (tRange == 0)
        {
            return;
        }
        factor = (time - lowTime) / tRange;
    }

}

