using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Platform_for_Ergonomics_evaluation_Methods.Importers.Xsens;
using System.Numerics;

namespace Platform_for_Ergonomics_evaluation_Methods.Controllers
{
    public class StickieTestController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        class StickieData
        {
            public class Frame
            {
                public float time;
                public List<float> headTransform = new List<float>();
                public List<List<Vector3>> limbJointPositions = new List<List<Vector3>>();
            }
            public List<string> limbNames = new List<string>();
            public List<List<string>> limbJoints = new List<List<string>>();
            public List<Frame> frames = new List<Frame>();
            public StickieData(ManikinBase manikin)
            {
                List<Limb> limbs = manikin.GetLimbs();
                foreach (Limb limb in limbs)
                {
                    limbNames.Add(limb.name);
                    List<string> jointNames = new List<string>();
                    foreach (JointID jointID in limb.joints)
                    {
                        jointNames.Add(jointID.ToString());
                    }
                    limbJoints.Add(jointNames);
                }
                float t = 0;
                while (t <= manikin.GetTimelineDuration())
                {
                    Frame frame = new Frame();
                    frames.Add(frame);
                    frame.time = t;
                    manikin.SetTime(t);
                    foreach (Limb limb in limbs)
                    {
                        List<Vector3> positions = new List<Vector3>();
                        foreach (JointID j in limb.joints)
                        {
                            positions.Add(manikin.GetJointPosition(j));
                        }
                        frame.limbJointPositions.Add(positions);
                    }
                    Vector3 headPos;
                    Quaternion headRot;

                    t += .03f;
                }
            }

        }
        [HttpGet]
        public IActionResult GetStickieData()
        {
            //IPS, Xsens, Pem
            //X+ = Forward
            //Y+ = Left
            //Z+ = Up

            //Three
            //X+ = Right
            //Y+ = Up
            //Z+ = Back

            //Aff
            //X+ = Right
            //Y+ = Forward
            //Z+ = Up


            StickieData test = new StickieData(new XsensManikin());

            return Json(JsonConvert.SerializeObject(test, Formatting.Indented));

            if (ManikinManager.loadedManikin == null)
            {
                return BadRequest();
            }
            StickieData stickieData = new StickieData(ManikinManager.loadedManikin);

            return Json(JsonConvert.SerializeObject(stickieData, Formatting.Indented));
        }


    }

}
