using System;
using System.Collections.Generic;
using System.Numerics;

namespace PEM.Models
{
    /// <summary>
    /// Per-frame geometric primitives required to compute ergonomic criteria.
    /// Light C# port of C++ ManikinVectors (names aligned where practical).
    /// </summary>
    public class ManikinVectors
    {
        public readonly List<Vector3> rightShoulderPos = new();
        public readonly List<Vector3> leftShoulderPos = new();
        public readonly List<Vector3> rightElbowPos = new();
        public readonly List<Vector3> leftElbowPos = new();
        public readonly List<Vector3> rightWristPos = new();
        public readonly List<Vector3> leftWristPos = new();
        public readonly List<Vector3> rightHipPos = new();
        public readonly List<Vector3> leftHipPos = new();
        public readonly List<Vector3> rightKneePos = new();
        public readonly List<Vector3> leftKneePos = new();
        public readonly List<Vector3> rightAnklePos = new();
        public readonly List<Vector3> leftAnklePos = new();
        public readonly List<Vector3> uppBackPos = new(); // C7T1
        public readonly List<Vector3> lowBackPos = new(); // L5S1

        // Centers
        public readonly List<Vector3> ctrShoulderTrans = new();
        public readonly List<Vector3> ctrHipTrans = new();
        public readonly List<Vector3> ctrKneeTrans = new();
        public readonly List<Vector3> ctrWristTrans = new();

        // Vectors
        public readonly List<Vector3> lowerBackToUpperBack = new();
        public readonly List<Vector3> rightShoulderToLeftShoulder = new();
        public readonly List<Vector3> rightHipToLeftHip = new();
        public readonly List<Vector3> rightUpperArmToRightElbow = new();
        public readonly List<Vector3> leftUpperArmToLeftElbow = new();
        public readonly List<Vector3> rightElbowToRightWrist = new();
        public readonly List<Vector3> leftElbowToLeftWrist = new();
        public readonly List<Vector3> rightKneeToRightHip = new();
        public readonly List<Vector3> leftKneeToLeftHip = new();
        public readonly List<Vector3> rightAnkleToRightKnee = new();
        public readonly List<Vector3> leftAnkleToLeftKnee = new();

        // Optional foot vectors (ankle -> toes), only filled if available
        public readonly List<Vector3> rightAnkleToRightFoot = new();
        public readonly List<Vector3> leftAnkleToLeftFoot = new();

        // Plane normals (projection only needs the normal, not a plane point)
        public readonly List<Vector3> upperSagittalNormal = new();   // normal ≈ shoulder span
        public readonly List<Vector3> lowerSagittalNormal = new();   // normal ≈ hip span
        public readonly List<Vector3> upperFrontNormal = new();      // plane through R/L shoulders & low back
        public readonly List<Vector3> lowerFrontNormal = new();      // plane through R/L hips & upper back
        public readonly List<Vector3> upperTransverseNormal = new(); // normal ≈ spine at C7T1
        public readonly List<Vector3> lowerTransverseNormal = new(); // normal ≈ spine at L5S1
        public readonly List<Vector3> rightArmPlaneNormal = new();   // plane through R shoulder, elbow, wrist
        public readonly List<Vector3> leftArmPlaneNormal = new();    // plane through L shoulder, elbow, wrist

        public int FrameCount => lowerBackToUpperBack.Count;

        public ManikinVectors(ManikinBase manikin)
        {
            if (manikin.postureTimeSteps == null || manikin.postureTimeSteps.Count == 0)
                throw new InvalidOperationException("Manikin has no time steps.");

            foreach (var t in manikin.postureTimeSteps)
            {
                manikin.SetTime(t);

                if (!(manikin.TryGetJointPosition(JointID.C7T1, out var c7) &&
                      manikin.TryGetJointPosition(JointID.L5S1, out var l5s1) &&
                      manikin.TryGetJointPosition(JointID.RightShoulder, out var rSh) &&
                      manikin.TryGetJointPosition(JointID.LeftShoulder, out var lSh) &&
                      manikin.TryGetJointPosition(JointID.RightElbow, out var rEl) &&
                      manikin.TryGetJointPosition(JointID.LeftElbow, out var lEl) &&
                      manikin.TryGetJointPosition(JointID.RightWrist, out var rWr) &&
                      manikin.TryGetJointPosition(JointID.LeftWrist, out var lWr) &&
                      manikin.TryGetJointPosition(JointID.RightHip, out var rHip) &&
                      manikin.TryGetJointPosition(JointID.LeftHip, out var lHip) &&
                      manikin.TryGetJointPosition(JointID.RightKnee, out var rKn) &&
                      manikin.TryGetJointPosition(JointID.LeftKnee, out var lKn) &&
                      manikin.TryGetJointPosition(JointID.RightAnkle, out var rAn) &&
                      manikin.TryGetJointPosition(JointID.LeftAnkle, out var lAn)))
                {
                    throw new Exception("Missing required joints for ManikinVectors.");
                }

                // Now these locals exist and can be added to the lists
                rightShoulderPos.Add(rSh); leftShoulderPos.Add(lSh);
                rightElbowPos.Add(rEl); leftElbowPos.Add(lEl);
                rightWristPos.Add(rWr); leftWristPos.Add(lWr);
                rightHipPos.Add(rHip); leftHipPos.Add(lHip);
                rightKneePos.Add(rKn); leftKneePos.Add(lKn);
                rightAnklePos.Add(rAn); leftAnklePos.Add(lAn);
                uppBackPos.Add(c7); lowBackPos.Add(l5s1);

                // centers
                ctrShoulderTrans.Add((rSh + lSh) * 0.5f);
                ctrHipTrans.Add((rHip + lHip) * 0.5f);
                ctrKneeTrans.Add((rKn + lKn) * 0.5f);
                ctrWristTrans.Add((rWr + lWr) * 0.5f);

                // vectors
                lowerBackToUpperBack.Add(c7 - l5s1);
                rightShoulderToLeftShoulder.Add(lSh - rSh);
                rightHipToLeftHip.Add(lHip - rHip);
                rightUpperArmToRightElbow.Add(rEl - rSh);
                leftUpperArmToLeftElbow.Add(lEl - lSh);
                rightElbowToRightWrist.Add(rWr - rEl);
                leftElbowToLeftWrist.Add(lWr - lEl);
                rightKneeToRightHip.Add(rHip - rKn);
                leftKneeToLeftHip.Add(lHip - lKn);
                rightAnkleToRightKnee.Add(rKn - rAn);
                leftAnkleToLeftKnee.Add(lKn - lAn);

                // optional foot (ankle->toes) if your loader provides them
                if (manikin.TryGetJointPosition(JointID.RightToes, out var rToes))
                    rightAnkleToRightFoot.Add(rToes - rAn);
                if (manikin.TryGetJointPosition(JointID.LeftToes, out var lToes))
                    leftAnkleToLeftFoot.Add(lToes - lAn);

                // plane normals
                var spine = SafeNorm(c7 - l5s1);
                var shSpan = SafeNorm(lSh - rSh);
                var hipSpan = SafeNorm(lHip - rHip);

                upperSagittalNormal.Add(shSpan);
                lowerSagittalNormal.Add(hipSpan);

                // front planes: normal of plane through (R,L,lowerBack) and (Rhip,Lhip,upperBack)
                var nUpperFront = SafeNorm(Vector3.Cross(lSh - rSh, l5s1 - rSh));
                var nLowerFront = SafeNorm(Vector3.Cross(lHip - rHip, c7 - rHip));
                upperFrontNormal.Add(nUpperFront);
                lowerFrontNormal.Add(nLowerFront);

                // transverse planes (normal ≈ spine)
                upperTransverseNormal.Add(spine);
                lowerTransverseNormal.Add(spine);

                // arm planes
                var nRightArm = SafeNorm(Vector3.Cross(rEl - rSh, rWr - rSh));
                var nLeftArm = SafeNorm(Vector3.Cross(lEl - lSh, lWr - lSh));
                rightArmPlaneNormal.Add(nRightArm);
                leftArmPlaneNormal.Add(nLeftArm);
            }
        }

        public static Vector3 ProjectOnPlane(Vector3 v, Vector3 planeNormal)
        {
            var n = SafeNorm(planeNormal);
            return v - Vector3.Dot(v, n) * n;
        }

        public static float AngleDeg(Vector3 a, Vector3 b)
        {
            var na = SafeNorm(a);
            var nb = SafeNorm(b);
            var d = Math.Clamp(Vector3.Dot(na, nb), -1f, 1f);
            return (float)(Math.Acos(d) * 180.0 / Math.PI);
        }

        public static Vector3 SafeNorm(Vector3 v)
        {
            var len = v.Length();
            if (len < 1e-8f) return new Vector3(0, 0, 1);
            return v / len;
        }
    }
}
