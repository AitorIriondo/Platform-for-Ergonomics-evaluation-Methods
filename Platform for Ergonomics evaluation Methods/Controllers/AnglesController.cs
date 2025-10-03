using Microsoft.AspNetCore.Mvc;
using PEM.Models;
using System.Numerics;
using System;
using System.Collections.Generic;

namespace PEM.Controllers
{
    public partial class AnglesController : Controller
    {
        // UI page (keep as-is)
        [HttpGet]
        public IActionResult Index() => View();

        // Returns available angle keys for the dropdown
        [HttpGet("/api/angles/available")]
        public IActionResult GetAvailableAngles()
        {
            return Ok(new[] { "HeadFlexion", "UpperArmLeft", "UpperArmRight" });
        }

        // Returns a single time-series so the UI can mix manikins and angles arbitrarily
        [HttpGet("/api/angles/series")]
        public IActionResult GetSeries([FromQuery] string manikinId, [FromQuery] string angle)
        {
            ManikinBase manikin = null;

            if (!string.IsNullOrWhiteSpace(manikinId))
            {
                if (!ManikinManager.LoadedManikins.TryGetValue(manikinId, out manikin))
                    return NotFound($"Manikin '{manikinId}' not loaded.");
            }
            else
            {
                manikin = ManikinManager.ActiveManikin;
            }

            if (manikin == null)
                return BadRequest("No manikin loaded.");

            if (string.IsNullOrWhiteSpace(angle))
                return BadRequest("Missing 'angle'.");

            if (!IsSupportedAngle(angle))
                return BadRequest($"Unsupported angle '{angle}'.");

            var times = manikin.postureTimeSteps;
            if (times == null || times.Count == 0)
                return BadRequest("Selected manikin has no timeline.");

            var values = ComputeAngleSeries(manikin, angle); // same length as times
            var label = $"{(manikin.GetDescriptiveName() ?? manikinId)} • {HumanLabel(angle)}";

            return Ok(new
            {
                manikinId = manikinId,
                angle = angle,
                label = label,
                time = times,   // List<float>
                values = values // List<double>
            });
        }

        private static bool IsSupportedAngle(string a)
            => a == "HeadFlexion" || a == "UpperArmLeft" || a == "UpperArmRight";

        private static string HumanLabel(string a) => a switch
        {
            "HeadFlexion" => "Head Flexion",
            "UpperArmLeft" => "Upper Arm (Left)",
            "UpperArmRight" => "Upper Arm (Right)",
            _ => a
        };

        private static List<double> ComputeAngleSeries(ManikinBase manikin, string angle)
        {
            var times = manikin.postureTimeSteps;
            var list = new List<double>(times.Count);
            foreach (var t in times)
            {
                manikin.SetTime(t);

                // Trunk proxy = C7T1 - L5S1
                Vector3 trunk = new Vector3(0, 0, 1);
                if (manikin.TryGetJointPosition(JointID.L5S1, out var l5s1) &&
                    manikin.TryGetJointPosition(JointID.C7T1, out var c7t1))
                {
                    trunk = SafeDir(c7t1 - l5s1);
                }

                switch (angle)
                {
                    case "HeadFlexion":
                        {
                            var vertical = new Vector3(0, 0, 1);
                            list.Add(AngleDeg(trunk, vertical));
                            break;
                        }
                    case "UpperArmLeft":
                        {
                            if (manikin.TryGetJointPosition(JointID.LeftShoulder, out var lSh) &&
                                manikin.TryGetJointPosition(JointID.LeftElbow, out var lEl))
                            {
                                list.Add(AngleDeg(SafeDir(lEl - lSh), trunk));
                            }
                            else list.Add(0);
                            break;
                        }
                    case "UpperArmRight":
                        {
                            if (manikin.TryGetJointPosition(JointID.RightShoulder, out var rSh) &&
                                manikin.TryGetJointPosition(JointID.RightElbow, out var rEl))
                            {
                                list.Add(AngleDeg(SafeDir(rEl - rSh), trunk));
                            }
                            else list.Add(0);
                            break;
                        }
                    default:
                        list.Add(0);
                        break;
                }
            }
            return list;
        }

        private static Vector3 SafeDir(Vector3 v)
        {
            var len = v.Length();
            return (len < 1e-6f) ? new Vector3(0, 0, 1) : v / len;
        }

        private static double AngleDeg(Vector3 a, Vector3 b)
        {
            var dot = Math.Clamp(Vector3.Dot(a, b), -1f, 1f);
            return Math.Acos(dot) * (180.0 / Math.PI);
        }
    }
}
