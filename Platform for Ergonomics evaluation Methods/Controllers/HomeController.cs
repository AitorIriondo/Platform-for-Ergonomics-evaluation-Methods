using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Platform_for_Ergonomics_evaluation_Methods.Importers.Xsens;
using Platform_for_Ergonomics_evaluation_Methods.Models;
using Platform_for_Ergonomics_evaluation_Methods.Services;
using System.Diagnostics;
using System.Numerics;
using System.Text.Json.Nodes;

namespace Platform_for_Ergonomics_evaluation_Methods.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly MessageStorageService _messageStorageService;
        private string prevJsonMessage = "";

        public HomeController(ILogger<HomeController> logger, MessageStorageService messageStorageService)
        {
            _logger = logger;
            _messageStorageService = messageStorageService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // API to get the latest JSON message
        [HttpGet]
        public IActionResult GetLatestJsonMessage()
        {
            string jsonMessage = _messageStorageService.GetLatestMessage();
            return Json(new { jsonMessage });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public IActionResult GetLoadedManikinInfo()
        {
            string type = ManikinManager.loadedManikin != null ? ManikinManager.loadedManikin.GetType().ToString() : "None, Run exportTest.lua [TODO: Change this text]";
            return Json(new { type });
        }

        [HttpGet]
        public IActionResult LoadLastManikin()
        {
            string res = ManikinManager.LoadLast() ? "OK" : "FAIL";
            return Json(new { res });
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
                    foreach(JointID jointID in limb.joints)
                    {
                        jointNames.Add(jointID.ToString());
                    }
                    limbJoints.Add(jointNames);
                }
                float t = 0;
                while (t <= manikin.timelineDuration)
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

            //StickieData test = new StickieData(new XsensManikin());

            //return Json(JsonConvert.SerializeObject(test, Formatting.Indented));

            if (ManikinManager.loadedManikin == null)
            {
                return BadRequest();
            }
            StickieData stickieData = new StickieData(ManikinManager.loadedManikin);

            return Json(JsonConvert.SerializeObject(stickieData, Formatting.Indented));
        }


    }
}