using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PEM.Services;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;

namespace PEM.Controllers
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


        static Vector3 Pem2Aff(Vector3 v)
        {
            return new Vector3(-v.Y, v.X, v.Z);
        }
        static void AddIfUnique<T>(List<T> list, T item)
        {
            if(list != null && !list.Contains(item))
            {
                list.Add(item);
            }
        }
        Aff.Input GetAffInput(ManikinBase manikin, List<string> messages = null)
        {
            Aff.Input input = new Aff.Input();
            bool TryGetAffJointPosition(JointID jointId, out Vector3 pos)
            {
                bool ret = manikin.TryGetJointPosition(jointId, out pos);
                pos = Pem2Aff(pos);
                return ret;
            }
            void PopulateArm(Aff.ArmInput arm)
            {
                if (!TryGetAffJointPosition(arm == input.left ? JointID.LeftShoulder : JointID.RightShoulder, out arm.shoulder))
                {
                    JointID shoulder = arm == input.left ? JointID.LeftGH : JointID.RightGH;
                    TryGetAffJointPosition(shoulder, out arm.shoulder);
                    AddIfUnique(messages, "Shoulder is substituted by GH");
                }
                TryGetAffJointPosition(arm == input.left ? JointID.LeftElbow : JointID.RightElbow, out arm.elbow);
                TryGetAffJointPosition(arm == input.left ? JointID.LeftWrist : JointID.RightWrist, out arm.wrist);
                if (!TryGetAffJointPosition(arm == input.left ? JointID.LeftMiddleProximal : JointID.RightMiddleProximal, out arm.knuckle))
                {
                    Vector3 forearm = arm.elbow - arm.wrist;
                    float knuckleDist = forearm.Length() * .36f;
                    arm.knuckle = arm.wrist + knuckleDist * forearm.Normalized();
                    AddIfUnique(messages, "Knuckle is assumed to be positioned along the line of the forearm, at a distance from the wrist equal to 36% of the forearm's length");
                }
                Vector3 force = arm == input.left ? manikin.GetLeftHandForce() : manikin.GetRightHandForce();
                if (force.Length() == 0)
                {
                    force.Z = 0.00001f;
                }
                arm.actualLoad = force.Length();
                arm.forceDirection = Pem2Aff(force.Normalized());
            }
            input.bodyMass = 70;
            input.stature = 1.63f;
            input.female = true;
            input.percentCapable = 75;
            if(!TryGetAffJointPosition(JointID.C7T1, out input.C7T1))
            {
                TryGetAffJointPosition(JointID.C6C7, out input.C7T1);
                AddIfUnique(messages, "C7T1 is substituted by C6C7");
            }
            input.L5S1 = Pem2Aff(manikin.GetJointPosition(JointID.L5S1));
            PopulateArm(input.left);
            PopulateArm(input.right);
            return input;

        }


        [HttpGet]
        public IActionResult GetGraphValArrs(float percentCapable = 75, float demoLoadPercent = 100, string altGender = "")
        {
            System.GC.Collect();
            Debug.WriteLine("GC.Collect GetGraphValArrs");
            float[] freqEffortsPerDay = { 420, 100 };
            float[] effDurPerEffort = { 1, 1 };
            float[] mae = new float[2];
            for(int i = 0; i < 2; i++)
            {
                float dutyCycle = (freqEffortsPerDay[i] * effDurPerEffort[i]) / 25200;
                mae[i] = 1;
                if (freqEffortsPerDay[i] * effDurPerEffort[i] >= 1)
                {
                    mae[i] = 1 - MathF.Pow(dutyCycle - 1 / 25200, .24f);
                }

            }
            try
            {
                ManikinBase? manikin = ManikinManager.loadedManikin;
                if (manikin == null)
                {
                    return BadRequest();
                }
                List<string> messages = new List<string>();
                List<float> timestamps = new List<float>();
                float t = 0;
                int frame = 0;
                bool includeProbability = true;
                List<float>[] vals = new List<float>[2 * (includeProbability ? 3 : 2)];
                List<string> affJsons = new List<string>();
                for (int i = 0; i < vals.Length; i++)
                {
                    vals[i] = new List<float>();
                }
                while (t < manikin.GetTimelineDuration())
                {
                    timestamps.Add(t);
                    manikin.SetTime(t);
                    Aff aff = new Aff();
                    aff.input = GetAffInput(manikin, messages);
                    if (altGender != "")
                    {
                        aff.input.female = altGender.ToUpper() != "MALE";
                    }
                    aff.input.percentCapable = percentCapable;
                    aff.input.left.actualLoad *= demoLoadPercent / 100;
                    aff.input.right.actualLoad *= demoLoadPercent / 100;
                    aff.Calculate();
                    /*
                    D32 Effort = 'Lft - ANN 13 nodes'!T84
                        T84=IF('Input Data'!D4="Female";'Lft - ANN 13 nodes'!T82;'Lft - ANN 13 nodes'!W82)
                            T82=IF(T81<S82;0;T81)
                                T81=('Input Data'!D25 - 'Lft - ANN 13 nodes'!Q61)/'Lft - ANN 13 nodes'!T64  
                                    
                                S82=0
                            W82=IF(W81<S82;0;W81)
                    */
                    int arrIdx = 0;
                    float[] maf = new float[2];
                    for (int i = 0; i < 2; i++)
                    {
                        vals[arrIdx++].Add((i == 0 ? aff.input.left : aff.input.right).actualLoad);
                        float mas = (i == 0 ? aff.leftArm : aff.rightArm).masWithGravity;
                        vals[arrIdx++].Add(mas);
                        if (includeProbability)
                        {
                            vals[arrIdx++].Add((i == 0 ? aff.leftArm : aff.rightArm).masProbabilityPercent);
                        }
                        maf[i] = mas * mae[i]; 
                    }
                    CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
                    string json = JsonConvert.SerializeObject(aff, Formatting.Indented);
                    string extra = "\"mae\":[" + String.Join(",", mae) + "], \"maf\":[" + String.Join(",", maf) + "]";
                    json = json.Insert(json.Length - 2, "," + extra);
                    affJsons.Add(json);
                    t = ++frame * .1f;
                }
                
                //Debug.WriteLine(vals[1].Count);
                List<string> labels = new List<string>(){
                    "Load (N)",
                    $"MAS for {percentCapable}% cap (N)",
                };
                if (includeProbability)
                {
                    labels.Add("%Cap for load");
                }
                return Json(new { timestamps, vals, labels, messages, affJsons });

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);  
            }
            return BadRequest();
        }

    }

}
