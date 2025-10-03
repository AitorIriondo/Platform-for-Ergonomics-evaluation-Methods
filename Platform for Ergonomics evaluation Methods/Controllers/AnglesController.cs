using Microsoft.AspNetCore.Mvc;
using PEM.Models;
using System;
using System.Collections.Generic;

namespace PEM.Controllers
{
    public class AnglesController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("/api/angles/timeseries")]
        public IActionResult GetTimeSeries()
        {
            var manikin = ManikinManager.ActiveManikin;
            if (manikin == null)
                return BadRequest("No manikin loaded.");

            try
            {
                var ts = ComputeTimeSeries(manikin);
                return Ok(ts);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        private object ComputeTimeSeries(ManikinBase manikin)
        {
            var result = new Dictionary<string, object>();

            var times = manikin.postureTimeSteps;   // this is already a List<float>
            var headFlex = new List<double>();
            var uaLeft = new List<double>();
            var uaRight = new List<double>();

            foreach (var t in times)
            {
                manikin.SetTime(t);

                System.Numerics.Vector3 trunk = new System.Numerics.Vector3(0, 0, 1);

                if (manikin.TryGetJointPosition(JointID.L5S1, out var l5s1) &&
                    manikin.TryGetJointPosition(JointID.C7T1, out var c7t1))
                {
                    trunk = SafeDir(c7t1 - l5s1);
                    var vertical = new System.Numerics.Vector3(0, 0, 1);
                    headFlex.Add(AngleDeg(trunk, vertical));
                }
                else
                {
                    headFlex.Add(0);
                }

                if (manikin.TryGetJointPosition(JointID.LeftShoulder, out var lSh) &&
                    manikin.TryGetJointPosition(JointID.LeftElbow, out var lEl))
                {
                    uaLeft.Add(AngleDeg(SafeDir(lSh - lEl), trunk));
                }
                else
                {
                    uaLeft.Add(0);
                }

                if (manikin.TryGetJointPosition(JointID.RightShoulder, out var rSh) &&
                    manikin.TryGetJointPosition(JointID.RightElbow, out var rEl))
                {
                    uaRight.Add(AngleDeg(SafeDir(rSh - rEl), trunk));
                }
                else
                {
                    uaRight.Add(0);
                }
            }

            // Assign lists directly; JSON serializer will turn them into arrays
            result["time"] = times;
            result["headFlexion"] = headFlex;
            result["upperArmLeft"] = uaLeft;
            result["upperArmRight"] = uaRight;

            return result;
        }


        private static System.Numerics.Vector3 SafeDir(System.Numerics.Vector3 v)
        {
            var len = v.Length();
            if (len < 1e-6) return new System.Numerics.Vector3(0, 0, 1);
            return v / len;
        }

        private static double AngleDeg(System.Numerics.Vector3 a, System.Numerics.Vector3 b)
        {
            var dot = Math.Clamp(System.Numerics.Vector3.Dot(a, b), -1f, 1f);
            return Math.Acos(dot) * (180.0 / Math.PI);
        }
    }
}
