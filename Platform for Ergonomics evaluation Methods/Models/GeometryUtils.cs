using System.Numerics;

public class GeometryUtils
{
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


