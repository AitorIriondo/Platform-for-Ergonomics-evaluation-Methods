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


