using System.Numerics;

public class GeometryUtils
{
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
public class FrameInterpolationInfo
{
    public int lowIdx = 0;
    public int highIdx = -1;
    public float factor = 0;
    public float time = 0;
    public bool isApplicable()
    {
        return highIdx > lowIdx && factor > 0;
    }
}

