using System;
using System.Collections.Generic;
using System.Numerics;

namespace PEM.Models
{
    /// <summary>
    /// Centralized computation of per-frame ergonomic angles, ported from the C++ Criterias modules.
    /// Uses ManikinVectors to derive the geometric primitives.
    /// </summary>
    public class ManikinCriterias
    {
        public readonly List<float> Time = new();
        private readonly ManikinVectors V;

        // Back & Neck
        public readonly List<double> backAng = new();         // trunk inclination (sagittal)
        public readonly List<double> backTwistAng = new();    // trunk axial rotation (transverse)
        public readonly List<double> backBendAng = new();     // trunk lateral bending (frontal)
        public readonly List<double> neckAng = new();         // approx. (no head joints in current loaders)
        public readonly List<double> neckBendAng = new();
        public readonly List<double> neckTwistAng = new();
        public readonly List<double> t1c7c1HeadAng = new();

        // Upper Arm
        public readonly List<double> rightUpperArmBackAng = new();
        public readonly List<double> leftUpperArmBackAng = new();
        public readonly List<double> rightUpperArmBackSagitalAng = new();
        public readonly List<double> leftUpperArmBackSagitalAng = new();
        public readonly List<double> rightUpperArmBackAbductedAng = new();
        public readonly List<double> leftUpperArmBackAbductedAng = new();
        public readonly List<double> rightUpperArmRaisedDist = new();
        public readonly List<double> leftUpperArmRaisedDist = new();
        public readonly List<double> rightHandRaisedOverShoulderDist = new();
        public readonly List<double> leftHandRaisedOverShoulderDist = new();
        public readonly List<double> rightUpperArmToVerticalAng = new();
        public readonly List<double> leftUpperArmToVerticalAng = new();

        // Lower Arm
        public readonly List<double> rightElbowAng = new();
        public readonly List<double> leftElbowAng = new();
        public readonly List<double> rightWristAng = new();        // placeholders until hand axes available
        public readonly List<double> leftWristAng = new();
        public readonly List<double> rightWristBendAng = new();
        public readonly List<double> leftWristBendAng = new();
        public readonly List<double> rightWristTwistAng = new();
        public readonly List<double> leftWristTwistAng = new();

        // Lower Body
        public readonly List<double> rKneeAngle = new();
        public readonly List<double> lKneeAngle = new();
        public readonly List<double> rAnkleAngle = new();
        public readonly List<double> lAnkleAngle = new();

        private readonly List<Vector3>? xsensC1HeadAngles;

        public ManikinCriterias(ManikinBase manikin)
        {
            if (manikin.postureTimeSteps == null || manikin.postureTimeSteps.Count == 0)
                throw new InvalidOperationException("Manikin has no time steps.");
            Time.AddRange(manikin.postureTimeSteps);

            V = new ManikinVectors(manikin);

            //Temp fix
            if (manikin.TryGetImportedJointAngle("jC1Head", out var series))
                xsensC1HeadAngles = series;

            // --- Combined T1C7 + C1Head flexion (from imported Xsens joint angles) ---
            if (manikin.TryGetImportedJointAngle("jT1C7", out var t1c7Series) &&
                manikin.TryGetImportedJointAngle("jC1Head", out var c1HeadSeries))
            {
                int n = Math.Min(t1c7Series.Count, c1HeadSeries.Count);
                for (int i = 0; i < n; i++)
                {
                    // Xsens convention: Z = Flex/Ext, X = Lateral Bend, Y = Axial Rotation
                    double flexExt = t1c7Series[i].Z + c1HeadSeries[i].Z;
                    t1c7c1HeadAng.Add(flexExt);
                }
            }

            ComputeBackAndNeck();
            ComputeUpperArms();
            ComputeLowerArms();
            ComputeLowerBody();
        }

        private void ComputeBackAndNeck()
        {
            var up = new Vector3(0, 0, 1);

            for (int i = 0; i < V.FrameCount; i++)
            {
                var spine = V.lowerBackToUpperBack[i];
                var shSpan = V.rightShoulderToLeftShoulder[i];
                var hipSpan = V.rightHipToLeftHip[i];

                // Back angle: angle between trunk and vertical (projected in sagittal plane)
                var sagN = V.upperSagittalNormal[i];
                var projSpineSag = ManikinVectors.ProjectOnPlane(spine, sagN);
                var projUpSag = ManikinVectors.ProjectOnPlane(up, sagN);
                backAng.Add(ManikinVectors.AngleDeg(projSpineSag, projUpSag));

                // Back twist: shoulders vs hips on transverse plane
                var transN = V.lowerTransverseNormal[i];
                var projShoulder = ManikinVectors.ProjectOnPlane(shSpan, transN);
                var projHip = ManikinVectors.ProjectOnPlane(hipSpan, transN);
                backTwistAng.Add(ManikinVectors.AngleDeg(projShoulder, projHip));

                // Back bend (lateral): spine vs vertical in front plane
                var frontN = V.upperFrontNormal[i];
                var projSpineFront = ManikinVectors.ProjectOnPlane(spine, frontN);
                var projUpFront = ManikinVectors.ProjectOnPlane(up, frontN);
                backBendAng.Add(ManikinVectors.AngleDeg(projSpineFront, projUpFront));

                // --- Neck angles (relative to trunk/spine) ---
                var neckVec = V.upperBackToNeck[i];

                // Flex/Ext: neck vs spine in sagittal plane
                var projNeckSag = ManikinVectors.ProjectOnPlane(neckVec, sagN);
                neckAng.Add(ManikinVectors.AngleDeg(projNeckSag, projSpineSag));

                // Lateral bend: neck vs spine in frontal plane
                var projNeckFront = ManikinVectors.ProjectOnPlane(neckVec, frontN);
                neckBendAng.Add(ManikinVectors.AngleDeg(projNeckFront, projSpineFront));
            }
        }

        private void ComputeUpperArms()
        {
            for (int i = 0; i < V.FrameCount; i++)
            {
                var spine = V.lowerBackToUpperBack[i];
                var sagN = V.upperSagittalNormal[i];
                var frontN = V.upperFrontNormal[i];

                // C++ uses opposite(shoulder->elbow)
                var rArm = -(V.rightUpperArmToRightElbow[i]);
                var lArm = -(V.leftUpperArmToLeftElbow[i]);

                // 3D against spine
                rightUpperArmBackAng.Add(ManikinVectors.AngleDeg(rArm, spine));
                leftUpperArmBackAng.Add(ManikinVectors.AngleDeg(lArm, spine));

                // Sagittal projection
                var rArmSag = ManikinVectors.ProjectOnPlane(rArm, sagN);
                var lArmSag = ManikinVectors.ProjectOnPlane(lArm, sagN);
                var spineSag = ManikinVectors.ProjectOnPlane(spine, sagN);
                rightUpperArmBackSagitalAng.Add(ManikinVectors.AngleDeg(rArmSag, spineSag));
                leftUpperArmBackSagitalAng.Add(ManikinVectors.AngleDeg(lArmSag, spineSag));

                // Frontal projection (abduction)
                var rArmFront = ManikinVectors.ProjectOnPlane(rArm, frontN);
                var lArmFront = ManikinVectors.ProjectOnPlane(lArm, frontN);
                var spineFront = ManikinVectors.ProjectOnPlane(spine, frontN);
                rightUpperArmBackAbductedAng.Add(ManikinVectors.AngleDeg(rArmFront, spineFront));
                leftUpperArmBackAbductedAng.Add(ManikinVectors.AngleDeg(lArmFront, spineFront));

                // Height deltas
                var rSh = V.rightShoulderPos[i]; var lSh = V.leftShoulderPos[i];
                var rEl = V.rightElbowPos[i]; var lEl = V.leftElbowPos[i];
                var rWr = V.rightWristPos[i]; var lWr = V.leftWristPos[i];

                //Against vertical (LAL)
                var up = new Vector3(0, 0, 1); // vertical reference
                rightUpperArmToVerticalAng.Add(ManikinVectors.AngleDeg(rArm, up));
                leftUpperArmToVerticalAng.Add(ManikinVectors.AngleDeg(lArm, up));

                rightUpperArmRaisedDist.Add(rEl.Z - rSh.Z);
                leftUpperArmRaisedDist.Add(lEl.Z - lSh.Z);
                rightHandRaisedOverShoulderDist.Add(rWr.Z - rSh.Z);
                leftHandRaisedOverShoulderDist.Add(lWr.Z - lSh.Z);
            }
        }

        private void ComputeLowerArms()
        {
            for (int i = 0; i < V.FrameCount; i++)
            {
                var rUpper = -(V.rightUpperArmToRightElbow[i]); // shoulder->elbow opposite
                var rLower = (V.rightElbowToRightWrist[i]);    // elbow->wrist
                var lUpper = -(V.leftUpperArmToLeftElbow[i]);
                var lLower = (V.leftElbowToLeftWrist[i]);

                rightElbowAng.Add(ManikinVectors.AngleDeg(rUpper, rLower));
                leftElbowAng.Add(ManikinVectors.AngleDeg(lUpper, lLower));



                
                var rHandProj = ManikinVectors.ProjectOnPlane(rLower, V.rightArmPlaneNormal[i]);
                var lHandProj = ManikinVectors.ProjectOnPlane(lLower, V.leftArmPlaneNormal[i]);
                // Wrist angles need hand axes; keep placeholders until available
                rightWristAng.Add(ManikinVectors.AngleDeg(rHandProj, rLower));
                leftWristAng.Add(ManikinVectors.AngleDeg(rHandProj, rLower));
                // Bend (approx): hand proj in arm plane vs forearm
                rightWristBendAng.Add(ManikinVectors.AngleDeg(rHandProj, rLower));
                leftWristBendAng.Add(ManikinVectors.AngleDeg(lHandProj, lLower));

                rightWristTwistAng.Add(double.NaN);
                leftWristTwistAng.Add(double.NaN);
            }
        }

        private void ComputeLowerBody()
        {
            for (int i = 0; i < V.FrameCount; i++)
            {
                var rKneeToHip = V.rightKneeToRightHip[i];
                var rKneeToAnkle = -(V.rightAnkleToRightKnee[i]);
                var lKneeToHip = V.leftKneeToLeftHip[i];
                var lKneeToAnkle = -(V.leftAnkleToLeftKnee[i]);

                rKneeAngle.Add(ManikinVectors.AngleDeg(rKneeToHip, rKneeToAnkle));
                lKneeAngle.Add(ManikinVectors.AngleDeg(lKneeToHip, lKneeToAnkle));

                if (i < V.rightAnkleToRightFoot.Count)
                    rAnkleAngle.Add(ManikinVectors.AngleDeg(V.rightAnkleToRightKnee[i], V.rightAnkleToRightFoot[i]));
                else rAnkleAngle.Add(double.NaN);

                if (i < V.leftAnkleToLeftFoot.Count)
                    lAnkleAngle.Add(ManikinVectors.AngleDeg(V.leftAnkleToLeftKnee[i], V.leftAnkleToLeftFoot[i]));
                else lAnkleAngle.Add(double.NaN);
            }
        }

        /// <summary>
        /// Expose all numeric series by key (names match the C++ members where possible).
        /// </summary>
        public Dictionary<string, IList<double>> ToSeriesDictionary()
        {
            return new Dictionary<string, IList<double>>(StringComparer.OrdinalIgnoreCase)
            {
                // Back & neck
                [nameof(backAng)] = backAng,
                [nameof(backTwistAng)] = backTwistAng,
                [nameof(backBendAng)] = backBendAng,
                [nameof(neckAng)] = neckAng,
                [nameof(neckBendAng)] = neckBendAng,
                [nameof(neckTwistAng)] = neckTwistAng,
                [nameof(t1c7c1HeadAng)] = t1c7c1HeadAng,

                // Upper arms
                [nameof(rightUpperArmBackAng)] = rightUpperArmBackAng,
                [nameof(leftUpperArmBackAng)] = leftUpperArmBackAng,
                [nameof(rightUpperArmBackSagitalAng)] = rightUpperArmBackSagitalAng,
                [nameof(leftUpperArmBackSagitalAng)] = leftUpperArmBackSagitalAng,
                [nameof(rightUpperArmBackAbductedAng)] = rightUpperArmBackAbductedAng,
                [nameof(leftUpperArmBackAbductedAng)] = leftUpperArmBackAbductedAng,
                [nameof(rightUpperArmRaisedDist)] = rightUpperArmRaisedDist,
                [nameof(leftUpperArmRaisedDist)] = leftUpperArmRaisedDist,
                [nameof(rightHandRaisedOverShoulderDist)] = rightHandRaisedOverShoulderDist,
                [nameof(leftHandRaisedOverShoulderDist)] = leftHandRaisedOverShoulderDist,
                [nameof(rightUpperArmToVerticalAng)] = rightUpperArmToVerticalAng,
                [nameof(leftUpperArmToVerticalAng)] = leftUpperArmToVerticalAng,

                // Lower arms
                [nameof(rightElbowAng)] = rightElbowAng,
                [nameof(leftElbowAng)] = leftElbowAng,
                [nameof(rightWristAng)] = rightWristAng,
                [nameof(leftWristAng)] = leftWristAng,
                [nameof(rightWristBendAng)] = rightWristBendAng,
                [nameof(leftWristBendAng)] = leftWristBendAng,
                [nameof(rightWristTwistAng)] = rightWristTwistAng,
                [nameof(leftWristTwistAng)] = leftWristTwistAng,

                // Lower body
                [nameof(rKneeAngle)] = rKneeAngle,
                [nameof(lKneeAngle)] = lKneeAngle,
                [nameof(rAnkleAngle)] = rAnkleAngle,
                [nameof(lAnkleAngle)] = lAnkleAngle,
            };
        }

        public static readonly Dictionary<string, string> HumanLabels =
            new(StringComparer.OrdinalIgnoreCase)
            {
                // Back & Neck
                ["backAng"] = "Back Flexion/Extension (Sagittal)",
                ["backTwistAng"] = "Back Axial Rotation (Twist)",
                ["backBendAng"] = "Back Lateral Bending (Frontal)",
                ["neckAng"] = "Neck Flexion/Extension",
                ["neckBendAng"] = "Neck Lateral Bending",
                ["neckTwistAng"] = "Neck Axial Rotation",
                ["t1c7c1HeadAng"] = "Neck Flexion/Extension (T1C7 + C1Head)",

                // Upper Arms
                ["rightUpperArmBackAng"] = "Right Upper Arm vs Spine",
                ["leftUpperArmBackAng"] = "Left Upper Arm vs Spine",
                ["rightUpperArmBackSagitalAng"] = "Right Upper Arm (Sagittal)",
                ["leftUpperArmBackSagitalAng"] = "Left Upper Arm (Sagittal)",
                ["rightUpperArmBackAbductedAng"] = "Right Upper Arm (Abduction)",
                ["leftUpperArmBackAbductedAng"] = "Left Upper Arm (Abduction)",
                ["rightUpperArmRaisedDist"] = "Right Elbow Height vs Shoulder",
                ["leftUpperArmRaisedDist"] = "Left Elbow Height vs Shoulder",
                ["rightHandRaisedOverShoulderDist"] = "Right Hand Height vs Shoulder",
                ["leftHandRaisedOverShoulderDist"] = "Left Hand Height vs Shoulder",
                ["rightUpperArmToVerticalAng"] = "Right Upper Arm to Vertical",
                ["leftUpperArmToVerticalAng"] = "Left Upper Arm to Vertical",

                // Lower Arms
                ["rightElbowAng"] = "Right Elbow Flexion",
                ["leftElbowAng"] = "Left Elbow Flexion",
                ["rightWristBendAng"] = "Right Wrist Bend (approx)",
                ["leftWristBendAng"] = "Left Wrist Bend (approx)",
                ["rightWristAng"] = "Right Wrist Angle (placeholder)",
                ["leftWristAng"] = "Left Wrist Angle (placeholder)",
                ["rightWristTwistAng"] = "Right Wrist Twist (placeholder)",
                ["leftWristTwistAng"] = "Left Wrist Twist (placeholder)",

                // Lower Body
                ["rKneeAngle"] = "Right Knee Flexion",
                ["lKneeAngle"] = "Left Knee Flexion",
                ["rAnkleAngle"] = "Right Ankle Angle",
                ["lAnkleAngle"] = "Left Ankle Angle",
            };

    }
}
