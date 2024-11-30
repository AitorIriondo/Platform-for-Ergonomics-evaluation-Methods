using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Platform_for_Ergonomics_evaluation_Methods.Services;
using System.Diagnostics;
using System.Numerics;

namespace Platform_for_Ergonomics_evaluation_Methods.Controllers
{
    public class AffTestController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly MessageStorageService _messageStorageService;
        public AffTestController(ILogger<HomeController> logger, MessageStorageService messageStorageService) {
            _logger = logger;
            _messageStorageService = messageStorageService;

        }
        public IActionResult Index()
        {
            ViewData["Message"] = "Nisse";
            ViewData["NumTimes"] = 1;
            return View();
        }

        
        static Vector3 Ips2Aff(Vector3 v)
        {
            return new Vector3(v.Y * -1, v.X, v.Z);
        }
        Aff.Input GetAffInput(ManikinBase manikin)
        {
            Aff.Input input = new Aff.Input();
            input.bodyMass = 70;
            input.stature = 1.63f;
            input.female = true;
            input.C7T1 = Ips2Aff(manikin.GetJointPosition(JointID.C7T1));
            input.L5S1 = Ips2Aff(manikin.GetJointPosition(JointID.L5S1));
            input.percentCapable = 75;

            input.left.knuckle = Ips2Aff(manikin.GetJointPosition(JointID.LeftMiddleProximal));
            input.left.wrist = Ips2Aff(manikin.GetJointPosition(JointID.LeftWrist)); 
            input.left.elbow = Ips2Aff(manikin.GetJointPosition(JointID.LeftElbow)); 
            input.left.shoulder = Ips2Aff(manikin.GetJointPosition(JointID.LeftGH)); 
            Vector3 force = manikin.GetLeftHandForce();
            if (force.Length() == 0)
            {
                force.Z = 0.00001f;
            }
            input.left.actualLoad = force.Length();
            input.left.forceDirection = Ips2Aff(force.Normalized());

            input.right.knuckle = Ips2Aff(manikin.GetJointPosition(JointID.RightMiddleProximal));
            input.right.wrist = Ips2Aff(manikin.GetJointPosition(JointID.RightWrist));
            input.right.elbow = Ips2Aff(manikin.GetJointPosition(JointID.RightElbow));
            input.right.shoulder = Ips2Aff(manikin.GetJointPosition(JointID.RightGH));

            force = manikin.GetRightHandForce();
            if (force.Length() == 0)
            {
                force.Z = 0.00001f;
            }
            input.right.actualLoad = force.Length();
            input.right.forceDirection = Ips2Aff(force.Normalized());
            return input;

        }


        [HttpGet]
        public IActionResult GetGraphValArrs(float percentCapable = 75, float demoLoadPercent = 100)
        {
            //string dir = "C:/Users/lebm/AppData/Local/PEM/IpsErgoExportTest/";
            //string msg = _messageStorageService.GetLatestMessage();
            //Debug.WriteLine(msg);
            //ManikinBase manikin = new IMMA.IMMAManikin(dir + "Male_w=78_s=1756.bin", dir + "Family 1.ctrlpts");
            ManikinBase? manikin = ManikinManager.loadedManikin;
            if (manikin == null)
            {
                return BadRequest();
            }
            List<float> timestamps = new List<float>();
            float t = 0;
            int frame = 0;
            bool includeProbability = true;
            List<float>[] vals = new List<float>[2 * (includeProbability ? 3 : 2)];
            for (int i = 0; i < vals.Length; i++)
            {
                vals[i] = new List<float>();
            }
            while (t < manikin.timelineDuration)
            {
                timestamps.Add(t);
                manikin.SetTime(t);
                Aff aff = new Aff();
                aff.input = GetAffInput(manikin);
                aff.input.percentCapable = percentCapable;
                aff.input.left.actualLoad *= demoLoadPercent / 100;
                aff.input.right.actualLoad *= demoLoadPercent / 100;
                aff.Calculate();
                int arrIdx = 0;
                for (int i = 0; i < 2; i++)
                {
                    vals[arrIdx++].Add((i == 0 ? aff.input.left : aff.input.right).actualLoad);
                    vals[arrIdx++].Add((i == 0 ? aff.leftArm : aff.rightArm).masWithGravity);
                    if (includeProbability)
                    {
                        vals[arrIdx++].Add((i == 0 ? aff.leftArm : aff.rightArm).masProbabilityPercent);
                    }
                }
                t = ++frame * .1f;
            }
            Debug.WriteLine(vals[1].Count);
            List<string> labels = new List<string>()
            {
                "Load (N)",
                $"MAS for {percentCapable}% cap (N)",
            };
            if (includeProbability)
            {
                labels.Add("%Cap for load");
            }
            return Json(new { timestamps, vals, labels });
        }
                
    }

}
