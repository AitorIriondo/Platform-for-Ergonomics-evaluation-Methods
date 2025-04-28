using System.Collections;
using System.Collections.Generic;
using System;
using System.Numerics;
using System.Security.Cryptography;

namespace Aff2 {
    [System.Serializable]
    public class Aff {
        [System.Serializable]
        public class Antropometrics {
            // Coefficient of variation for strength
            public float CV = 0.277f;

            // Center of gravity distance ratios
            public float upperArmCogRatio = 0.5302f;  //fraction of length from shoulder
            public float forearmCogRatio = 0.4195f;   //fraction of length from elbow
            public float handCogRatio = 0.7397f;      //fraction of length from wrist

            // Body mass ratios, fraction of body mass
            public float upperArmMassRatio = 0.0270f;
            public float forearmMassRatio = 0.0149f;
            public float handMassRatio = 0.0059f;

            //Added by Micke. Suggested by La Delfa and Potvin in "The ‘Arm Force Field’ method to predict manual arm strength based on only hand location and force direction"
            public float maleStrengthMultiplier = 1.66f;
        }

        [System.Serializable]
        public class ArmInput {
            public Vector3 knuckle;
            public Vector3 wrist;
            public Vector3 elbow;
            public Vector3 shoulder;
            public Vector3 forceDirection; //unit vector for force applied at the knuckle(not the reaction force)
            public float actualLoad;
            public float freqEffortsPerDay = -1;
            public float effDurPerEffortSec = -1;

        }

        [System.Serializable]
        public class Input {
            public float bodyMass;
            public float stature;
            public float percentCapable;
            public bool female;
            public Vector3 C7T1;
            public Vector3 L5S1;
            public ArmInput left = new ArmInput();
            public ArmInput right = new ArmInput();
        }

        [System.Serializable]
        public class ArmPart {
            public float weightN;
            public Vector3 cog;
            public Vector3 momentNm;
        }

        [System.Serializable]
        public class Arm {

            public ArmPart upperArm = new ArmPart();
            public ArmPart forearm = new ArmPart();
            public ArmPart hand = new ArmPart();
            public Vector3 totalMomentNm;
            public float totalMomentResultant;
            public Vector3 handPosInSAS;
            public Vector3 wristPosInSAS;
            public Vector3 elbowPosInSAS;
            public Vector3 gravityForceEffect;
            public Vector3 handForceInSAS;
            public float masNoGravity;
            public float masWithGravity;
            public float actualLoadNoGravity;
            public float masProbabilityPercent;

            public float effortDutyCycle = -1;
            public float effortRelativeToMas = -1;
            public float maxAcceptableEffort = -1;
            public float mafNoGravity = -1;
            public float percentMvc = -1;
            public float percentMaf = -1;
            //public float percentCapableWithMaf = -1;
            //public float subacromialImpingeScaleFactor = -1;
            //public float mafWithSf = -1;
            public readonly bool isLeft;
            [NonSerialized]
            Aff aff;
            ArmInput input;
            public Arm(Aff aff, bool isLeft) {
                this.aff = aff;
                this.isLeft = isLeft;
                input = isLeft ? aff.input.left : aff.input.right;
                upperArm.weightN = aff.input.bodyMass * aff.antropometrics.upperArmMassRatio * 9.81f;
                forearm.weightN = aff.input.bodyMass * aff.antropometrics.forearmMassRatio * 9.81f;
                hand.weightN = aff.input.bodyMass * aff.antropometrics.handMassRatio * 9.81f;
            }
            Vector3 GetJointPosInSAS(Vector3 jointPos) {
                Vector3 ret = aff.ToSAS(jointPos - input.shoulder);
                if (isLeft) {
                    ret.Z *= -1;
                    //ret[2] *= -1;
                }
                return ret;
            }
            public void Calculate() {
                handPosInSAS = GetJointPosInSAS(input.knuckle);
                elbowPosInSAS = GetJointPosInSAS(input.elbow);
                wristPosInSAS = GetJointPosInSAS(input.wrist);
                /* hand force unit vector in SAS */
                handForceInSAS = aff.ToSAS(input.forceDirection.Normalized());
                CalculateGravityForceEffect();


                float mas = (float)ANN.GetMAS(handPosInSAS, handForceInSAS, isLeft);
                float min, max;
                GetMinMax(handPosInSAS, handForceInSAS, isLeft, out min, out max);
                mas = (float)Math.Min(Math.Max(mas, min), max);
                if (!aff.input.female) {
                    mas *= aff.antropometrics.maleStrengthMultiplier;
                }
                /* _____________________________________________________________________________________________
                Estimate of Zero-Gravity maximum arm strength(MAS) for selected population*/
                float sd = mas * aff.antropometrics.CV;                         // estimate of standard deviation based on mean and global CV value

                float prob = 1 - aff.input.percentCapable / 100;
                masNoGravity = (float)MatlabFuncs.Norminv(prob, mas, sd);       // Zero G MAS values based on percent capable selected

                /*component of gravity acting along the force vector*/
                float gravityAssist = (gravityForceEffect.X * handForceInSAS.X)
                    + (gravityForceEffect.Y * handForceInSAS.Y)
                    + (gravityForceEffect.Z * handForceInSAS.Z * (isLeft ? -1 : 1)); //must reverse lateral side for Left
                                                                                     //float gravityAssist = (gravityForceEffect[0] * handForceInSAS[0])
                                                                                     //    + (gravityForceEffect[1] * handForceInSAS[1])
                                                                                     //    + (gravityForceEffect[2] * handForceInSAS[2] * (isLeft ? -1 : 1)); //must reverse lateral side for Left

                /*Final MAS value (with Gravity)*/
                masWithGravity = masNoGravity + gravityAssist;

                /*Percentage capable of the actual loads*/
                actualLoadNoGravity = input.actualLoad - gravityAssist;
                masProbabilityPercent = (1 - MatlabFuncs.Normcdf(actualLoadNoGravity, mas, sd)) * 100;

                /*MAF stuff */
                if (input.freqEffortsPerDay > 0 && input.effDurPerEffortSec > 0) {
                    effortDutyCycle = input.freqEffortsPerDay * input.effDurPerEffortSec / 25200f;
                    maxAcceptableEffort = 1;
                    if (input.freqEffortsPerDay * input.effDurPerEffortSec >= 1) {
                        maxAcceptableEffort = 1 - MathF.Pow(effortDutyCycle - 1 / 25200, .24f);
                    }

                    //% Percent Effort(Peak D / C Ratio)
                    //    LPercentMVC = (LActualLoad - Lga) / L0gMAS
                    percentMvc = (input.actualLoad - gravityAssist) / masNoGravity;

                    //% Max Acceptble Force
                    //    L0gMAF = L0gMAS * MAE
                    mafNoGravity = masNoGravity * maxAcceptableEffort;

                    //% Percent of MAF(Fatigue D/ C Ratio)    
                    //    LPercentMAF = (LActualLoad - Lga) / L0gMAF
                    percentMaf = (input.actualLoad - gravityAssist) / mafNoGravity;

                }

            }
            void CalculateGravityForceEffect() {
                Vector3 gravityInSAS = aff.ToSAS(gravityDirection);
                upperArm.cog = elbowPosInSAS * aff.antropometrics.upperArmCogRatio;
                forearm.cog = elbowPosInSAS + (wristPosInSAS - elbowPosInSAS) * aff.antropometrics.forearmCogRatio;
                hand.cog = wristPosInSAS + (handPosInSAS - wristPosInSAS) * aff.antropometrics.handCogRatio;

                upperArm.momentNm = Vector3.Cross(upperArm.cog, gravityInSAS) * upperArm.weightN;   // shoulder moment caused by Upper Arm
                forearm.momentNm = Vector3.Cross(forearm.cog, gravityInSAS) * forearm.weightN;      // shoulder moment caused by Forearm
                hand.momentNm = Vector3.Cross(hand.cog, gravityInSAS) * hand.weightN;               // shouler moment caused by Hand
                totalMomentNm = upperArm.momentNm + forearm.momentNm + hand.momentNm;               // Total shoulder moment caused by segments
                totalMomentResultant = totalMomentNm.Length();

                Vector3 assist = Vector3.Cross(totalMomentNm.Normalized(), handPosInSAS.Normalized());  // direction of gravity contributon to MAS
                float reach = handPosInSAS.Length();                                               // reach distance
                float gfeResultant = totalMomentResultant / reach;                                       // Gravity Force Effect resultant
                gravityForceEffect = (assist * gfeResultant) / assist.Length();                         // Gravity Force Effect vector
            }
        }

        //%Directions:
        //% [-1, 0, 0]; %Left
        //% [1, 0, 0]; %Right
        //% [0, -1, 0]; %Back
        //% [0, 1, 0]; %Fwd
        //% [0, 0, -1]; %Down
        //% [0, 0, 1]; %Up

        static readonly Vector3 gravityDirection = new Vector3(0, 0, -1);// % gravity in Global Axis System
        public Antropometrics antropometrics = new Antropometrics();
        public Input input = new Input();
        public Vector3[]? SAS;
        public Arm? leftArm;
        public Arm? rightArm;

        public static Vector3[] GetSAS(Input input) {
            /* establish Shoulder Axis System(SAS) */
            Vector3[] ret = new Vector3[3];
            ret[2] = (input.right.shoulder - input.left.shoulder).Normalized();
            Vector3 trunk = (input.C7T1 - input.L5S1).Normalized();
            ret[0] = Vector3.Cross(trunk, ret[2]).Normalized();
            ret[1] = Vector3.Cross(ret[2], ret[0]);
            return ret;
        }
        public static Vector3 ToSAS(Vector3 v, Vector3[] sas) {
            return MatlabFuncs.mtimes(v, MatlabFuncs.transpose(sas));
        }
        public void Calculate() {
            leftArm = new Arm(this, true);
            rightArm = new Arm(this, false);
            SAS = GetSAS(input);
            leftArm.Calculate();
            rightArm.Calculate();
        }
        public Vector3 ToSAS(Vector3 v) {
            return ToSAS(v, SAS);
        }


        static void GetMinMax(Vector3 HSAS, Vector3 FSAS, bool left, out float min, out float max) {
            /* Bounding Strength to Min and Max values observed in our studies
                values are bounded based on the height of the hand wrt shoulder, and the direction of the force vector
                (eg. if the force was anterior(+1) and inferior(-1) with no
                medial / lateral component, the codes would be 1, -1, 0)
            */

            /* arrays of minimums and maximums for each code */
            double[] MinMAS = new double[]{51.3, 49.9, 49.9, 51.3, 51.3, 49.9, 51.3, 49.9, 49.9, 51.3, 64.3, 49.9, 72.3, 9999.0, 49.9, 51.3, 72.3, 49.9, 52.9, 49.9, 49.9, 52.9, 52.9, 49.9, 52.9, 49.9, 49.9, 47.8, 47.1, 47.1, 47.8, 47.8, 47.1, 47.8, 47.1,
            47.1, 47.8, 64.3, 47.1, 68.9, 9999.0, 53.1, 47.8, 72.3, 47.1, 52.9, 51.4, 51.4, 52.9, 52.9, 51.4, 52.9, 51.4, 51.4, 44.2, 44.2, 44.2, 44.2, 44.2, 44.2, 44.2, 44.2, 44.2, 44.2, 64.3, 44.2, 65.5, 9999.0, 56.3, 44.2, 72.3,
            44.2, 52.9, 52.9, 52.9, 52.9, 52.9, 52.9, 52.9, 52.9, 52.9 };

            double[] MaxMAS = new double[]{223.2, 223.2, 223.2, 223.2, 223.2, 223.2, 223.2, 223.2, 223.2, 223.2, 120.0, 223.2, 133.4, -9999.0, 99.3, 223.2, 125.7, 223.2, 184.1, 184.1, 184.1, 184.1, 184.1, 184.1, 184.1, 184.1, 184.1, 220.5, 220.5, 220.5, 220.5,
            199.6, 220.5, 210.4, 210.4, 199.6, 220.5, 149.8, 220.5, 165.5, -9999.0, 134.7, 210.4, 128.0, 199.6, 190.9, 190.9, 181.9, 190.9, 175.5, 181.9, 190.9, 190.9, 177.1, 217.7, 217.7, 217.7, 217.7, 176.0, 217.7, 197.7, 197.7,
            176.0, 217.7, 179.7, 217.7, 197.7, -9999.0, 170.2, 197.7, 130.2, 176.0, 197.7, 197.7, 179.7, 197.7, 166.9, 179.7, 197.7, 197.7, 170.2 };

            float Htband = .01f;
            int heightCode = Math.Abs(HSAS.Y) <= Htband ? 0 : GetCode(HSAS.Y);     // Height Code(-1 for < -0.01, 0 for between - 0.01 & 0.01, 1 for > 0.01)
            int antPostCode = GetCode(FSAS.X);
            int supInfCode = GetCode(FSAS.Y);
            int medLatCode = GetCode(FSAS.Z * (left ? -1 : 1));                     // must switch polarity for left side
                                                                                    //int heightCode = Math.Abs(HSAS[1]) <= Htband ? 0 : GetCode(HSAS[1]);     // Height Code(-1 for < -0.01, 0 for between - 0.01 & 0.01, 1 for > 0.01)
                                                                                    //int antPostCode = GetCode(FSAS[0]);
                                                                                    //int supInfCode = GetCode(FSAS[1]);
                                                                                    //int medLatCode = GetCode(FSAS[2] * (left ? -1 : 1));                     // must switch polarity for left side
            int idx = medLatCode + 1;
            idx += (supInfCode + 1) * 3;
            idx += (antPostCode + 1) * 9;
            idx += (heightCode + 1) * 27;
            min = (float)MinMAS[idx];
            max = (float)MaxMAS[idx];
        }
        //-1 for negative, 0 for 0, 1 for positive
        static int GetCode(float val) {
            if (val == 0) {
                return 0;
            }
            return val < 0 ? -1 : 1;
        }

        class ANN {
            /* ANN coefficients(18 inputs, 13 nodes) */

            static readonly double[] in1offset = new double[] { -0.4145454545454540, 0.0000000000000000, -0.2000000000000000, 0.0211450020362057, 0.0000000000000000, 0.0000000000000000, -1.0000000000000000, -1.0000000000000000, -1.0000000000000000, -0.4720000000000000, -0.5072230027811940, -0.5021474062240250, 0.0000000000000000, 0.0000000000000000, 0.0000000000000000, -0.4720000000000000, -0.5135528785197460, -0.5326927207332780 };
            static readonly double[] in1gain = new double[] { 2.2437989556135800, 3.8331616396348900, 2.8838355012166200, 3.9808719139870100, 3.7210886709687600, 3.5496613795916400, 1.0000000000000000, 1.0000000000000000, 1.0000000000000000, 2.1041188125756200, 2.0185852492594400, 1.9796783114071000, 3.7360574357315700, 6.9790625815425600, 13.0370893261040000, 2.1186440677966100, 1.9379162887927500, 1.8726304261237400 };
            static readonly double in1min = -1;

            /* Layer 1 */
            static readonly double[] Layer1b = new double[] { -0.249390763050715, -0.100250151663260, 0.095639766968571, 0.101680098004717, 0.245807318675936, 1.196620804832640, -0.640172565850211, 0.504404303005015, -0.934511666215270, -0.088019829568919, -0.159442240916475, 0.250629528273471, 0.365963496484053 };

            static readonly double[][] Layer1c = new double[][]{
            new double[] {0.0396468197036280, 0.5135914913409210, -0.1615158131460990, 0.5335782521154120, 0.3926468149028430, 0.7784365394317120, -0.0902124024580994, -0.0258725059064285, -0.3186976135026210, 0.1408568009578810, -0.1015030485416650, 0.2236865262109910, 0.0184515483855661, 0.2848686155088860, 0.0966284464548426, 0.2746562193587130, 0.0259812747144168, 0.0879899598845416 },
            new double[] {-0.5746118508217600, -0.5027904960725710, -0.0346106776749378, 0.3400709517981290, 0.2055955545287030, 0.3016142159084500, 0.3007020715811980, -0.0492985615840271, -0.0303543877659774, 0.8771017058082360, -0.7025433938175290, -0.7009810281763620, -0.4055899728486460, 0.3227272624391930, 0.4020874234982020, 0.3610266765209100, -0.1992278230825480, -0.2430789659264560},
            new double[] {-0.0557183046775558, 0.0490560616244855, 0.0732281213380645, -0.3808356272955540, 0.2394342636225010, -0.4721690370427150, 0.4100268921804240, 0.2704500232626520, -0.2550484876609980, 0.1449649272503230, -0.4809743876368540, -0.2546406454871900, 0.3465404330579830, 0.0708754262465684, 0.4788433598444420, 0.2215891388376550, -0.4773030365151250, -0.2050673971388950},
            new double[] {0.3610611665751980, 0.2245754371636250, 0.2554306518605670, -0.4629606992678790, -0.3229704656758240, -0.5781072481730050, 0.2132757087698990, 0.1465432624230660, 0.0376969558157140, 0.4972601070092010, -0.2098604304412510, 0.6795406140198610, 0.1627211073321460, 0.0219398458443392, -0.0658912388596506, 0.4337293482561090, 0.1039589318217870, 0.7510560601092920},
            new double[] {0.2705797866315510, -0.6402561185960470, 0.0848149984877266, 0.3125525727147880, -0.4783292551509150, 0.4418097040331320, -0.0565511811718371, -0.5084718686313590, 0.1351866169817260, 0.5187860156051750, 0.0646553732286881, -0.2940020003199560, -0.6337273006332680, -0.2026063794675500, 0.5239938084219410, -0.1340013610736720, -0.4554200176011580, 0.4528727227080690},
            new double[] {-0.3349960804891440, -0.4357978550461050, 0.4465358482261200, -0.0375157672679093, -0.7067229365556190, 0.2756001172307310, 0.2248372901459630, 0.3299628845887790, 0.1822251533236540, 0.3533438912919030, 0.5186303608981890, -0.9299265033661340, 0.3321282113954300, 0.4971312002270840, -0.2924830392757020, -0.1625864627104710, 0.2983953130234120, -0.0127645885137380},
            new double[] {-0.2164840626998560, 0.4989879882631440, 0.0217402997176494, 0.1149004951516220, 0.5062698137551010, 0.2317911665459110, 0.6100899160989360, 0.2567140735923170, 0.0144884567379115, 0.5710515402598770, -0.2946443936700260, 0.0265906078074100, 0.5048313837962140, -0.3792877826710210, -0.0454203902615683, 0.1801269116220240, 0.1372308233697510, 0.0979411270189525},
            new double[] {0.3913441374502880, -0.0073456205366371, 0.1755164415474560, 0.0523500610091085, 0.1243779626016780, -0.3666722086134470, 0.9308840320885010, 0.6956057633373600, -0.1477020919957110, -0.1508098949617220, 0.2167013349329770, 0.1728450799063930, -0.8902712375254020, -0.0679969677511183, 0.4471591656305260, -0.0896015899235026, 0.3044721158016960, 0.4884305865702950},
            new double[] {-0.4946760291059190, 0.1939477889212450, -0.1364066060231560, 0.9642073332147800, -0.3823007930166790, -0.8581877087935020, 0.5678240113666970, 0.0183348911745601, 0.0734474674712959, -0.3038824379926810, -0.2105586924908500, -0.6436023360344460, -1.7207455397837500, -0.1053675074478800, 0.4871803180932090, -0.4073649576265590, 0.0790913947610594, 0.0685909517456038},
            new double[] {-0.3072776347547340, 0.1253616866532380, 0.3129490393046960, -0.6842875264248760, 0.7812594038954920, 0.3974967146848980, 0.1525550347153040, 0.1361716306263450, 0.0096179114168465, 0.0172555667366513, -0.4714076889981510, 0.2259879519205360, -0.2578286175424030, 0.3522799993269420, -0.1745347203626280, -0.0405847095874450, -0.5426321171031580, -0.1430194565848930},
            new double[] {-0.0791367309675499, -0.4438012585473360, -0.4787994868168900, 0.5757443432838650, 0.0303014125868337, -0.6913000111042270, 0.2852278064428020, -0.0843136504085405, -0.2629878149433620, -0.3190129683251240, 0.0875081476021028, -0.5423870522968970, 0.2141963456722130, 0.4311146825841830, 0.0024140834105431, 0.0722952121344132, -0.3238967625044890, -0.4829474364743040},
            new double[] {0.1418259625408540, 0.0760784150445575, -0.5283565483560720, -0.0011468411281056, -0.2474033651048630, -0.1732445962983420, -0.3189666135670600, -0.4959499926905650, 0.1190199212636580, 0.5403429274270760, 0.0638667163600597, -0.2305280571308560, -0.0812576791473448, 0.2206287611054260, -0.0940649002307790, -0.3564957901562640, -0.6196230305451320, 0.1155897334030620},
            new double[] {0.0132468525879893, -0.8765998536722000, -0.0504484159893567, -0.0875696222338695, 0.1449172648350600, 0.8244006592929960, -0.0540479238588384, 0.1144408906733490, -0.0399914698933143, 0.4346560301984630, -0.0813723658678813, 0.5068478617188480, 1.0401589725829900, -0.4421187830849380, -0.2862908924579780, 0.2308997829770930, -0.0823541762175492, -0.5335282381151980}
        };

            /* Layer 2  */
            static readonly double Layer2b = 0.319619509557245f;

            static readonly double[] Layer2c = new double[] { -0.726008728459171, -0.896119899552364, 1.303898287490790, -0.963599520641712, 0.892304171343111, -1.109496084791920, 1.320456762815650, -0.881510511623027, 1.197329700532090, -1.197641852318800, -0.836429746366561, -0.853341101811818, 1.697291156353840 };

            /* Output Layer */
            static readonly double OUTmin = -1;

            static readonly double OUTgain = 0.0111758422535829f;

            static readonly double OUToffset = 44.2451801385399f;

            public static double GetMAS(Vector3 HSAS, Vector3 FSAS, bool left) {
                /* inputs to the ANN(SI(y), AP(z), ML(x))*/
                /* Hand Inputs */
                double[] ANNin = new double[18];
                ANNin[0] = HSAS.Y;                                              // Hand Location wrt Shoudler(r)
                ANNin[1] = HSAS.X;
                ANNin[2] = HSAS.Z;
                ANNin[6] = FSAS.Y;                                              // direction cosine(DC) of Force unit vector(F)
                ANNin[7] = FSAS.X;
                ANNin[8] = FSAS.Z * (left ? -1 : 1);                                 // reverse for Left arm
                                                                                     //ANNin[0] = HSAS[1];                                              // Hand Location wrt Shoudler(r)
                                                                                     //ANNin[1] = HSAS[0];
                                                                                     //ANNin[2] = HSAS[2];
                                                                                     //ANNin[6] = FSAS[1];                                              // direction cosine(DC) of Force unit vector(F)
                                                                                     //ANNin[7] = FSAS[0];
                                                                                     //ANNin[8] = FSAS[2] * (left ? -1 : 1);                                 // reverse for Left arm

                ANNin[3] = Math.Sqrt(Math.Pow(ANNin[1], 2) + Math.Pow(ANNin[2], 2));                       // 2D Projection or r on plane

                ANNin[4] = Math.Sqrt(Math.Pow(ANNin[0], 2) + Math.Pow(ANNin[2], 2));
                ANNin[5] = Math.Sqrt(Math.Pow(ANNin[0], 2) + Math.Pow(ANNin[1], 2));
                ANNin[9] = (ANNin[1] * ANNin[8]) - (ANNin[2] * ANNin[7]);      // DC of 3D moment arm(DC or r x F)
                ANNin[10] = (ANNin[2] * ANNin[6]) - (ANNin[0] * ANNin[8]);
                ANNin[11] = (ANNin[0] * ANNin[7]) - (ANNin[1] * ANNin[6]);
                ANNin[12] = Math.Sqrt(                                            // resultant of 3D moment arm(3DMA)
                    Math.Pow(ANNin[9], 2) +
                    Math.Pow(ANNin[10], 2) +
                    Math.Pow(ANNin[11], 2)
                );
                ANNin[13] = Math.Pow(ANNin[12], 2);// 3DMA ^ 2
                ANNin[14] = Math.Pow(ANNin[12], 3);// 3DMA ^ 3
                ANNin[15] = ANNin[3] * ANNin[6];                                 // DC of F x 2D projection of r
                ANNin[16] = ANNin[4] * ANNin[7];
                ANNin[17] = ANNin[5] * ANNin[8];

                double[] p = new double[18];
                /* MaxMin Function to Modify the Original Input */
                for (int i = 0; i < 18; i++) {
                    p[i] = in1gain[i] * (ANNin[i] - in1offset[i]) - 1;
                }

                double[] sum = new double[13];
                double[] a = new double[13];

                /* Layer 1 operations */
                for (int n = 0; n < 13; n++) {          // 13 nodes
                    sum[n] = Layer1b[n];                // Layer 1 bias value
                    for (int i = 0; i < 18; i++) {      // 18 inputs
                        sum[n] += p[i] * Layer1c[n][i]; // summing(p * Layer 1 coeficients) for each node
                    }
                    a[n] = Math.Tanh(sum[n]);           // TanH of sum from Layer 1
                }

                /* Layer 2 operations */
                double sum2 = Layer2b;                  // Layer 2 bias value
                for (int n = 0; n < 13; n++) {
                    sum2 += a[n] * Layer2c[n];          // summing values for Layer 2
                }

                return (sum2 + 1) / OUTgain + OUToffset;// ANN raw maximum arm strength estimates

            }
        }

    }
}