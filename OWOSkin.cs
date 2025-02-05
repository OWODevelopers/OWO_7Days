using System;
using System.Collections.Generic;
using OWO_7Days;
using OWOGame;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Threading;


namespace OWOSkin
{

    public class OWOSkin
    {
        public bool suitDisabled = true;
        public bool systemInitialized = false;
        private static bool heartBeatIsActive = false;
        private static bool swimmingIsActive = false;
        public static bool headUnderwater = false;
        public Dictionary<String, Sensation> FeedbackMap = new Dictionary<String, Sensation>();


        public OWOSkin()
        {
            RegisterAllSensationsFiles();
            InitializeOWO();
        }

        public void LOG(string logStr)
        {
            Plugin.Log.LogInfo(logStr);
        }

        private void RegisterAllSensationsFiles()
        {
            string configPath = Directory.GetCurrentDirectory() + "\\BepinEx\\Plugins\\OWO";
            DirectoryInfo d = new DirectoryInfo(configPath);
            FileInfo[] Files = d.GetFiles("*.owo", SearchOption.AllDirectories);
            for (int i = 0; i < Files.Length; i++)
            {
                string filename = Files[i].Name;
                string fullName = Files[i].FullName;
                string prefix = Path.GetFileNameWithoutExtension(filename);
                if (filename == "." || filename == "..")
                    continue;
                string tactFileStr = File.ReadAllText(fullName);
                try
                {
                    Sensation test = Sensation.Parse(tactFileStr);
                    FeedbackMap.Add(prefix, test);
                }
                catch (Exception e) { LOG(e.Message); }

            }

            systemInitialized = true;
        }

        private async void InitializeOWO()
        {
            LOG("Initializing OWO skin");

            var gameAuth = GameAuth.Create(AllBakedSensations()).WithId("0"); ;

            OWO.Configure(gameAuth);
            string[] myIPs = getIPsFromFile("OWO_Manual_IP.txt");
            if (myIPs.Length == 0) await OWO.AutoConnect();
            else
            {
                await OWO.Connect(myIPs);
            }

            if (OWO.ConnectionState == ConnectionState.Connected)
            {
                suitDisabled = false;
                LOG("OWO suit connected.");
            }
            if (suitDisabled) LOG("OWO is not enabled?!?!");
        }

        public BakedSensation[] AllBakedSensations()
        {
            var result = new List<BakedSensation>();

            foreach (var sensation in FeedbackMap.Values)
            {
                if (sensation is BakedSensation baked)
                {
                    LOG("Registered baked sensation: " + baked.name);
                    result.Add(baked);
                }
                else
                {
                    LOG("Sensation not baked? " + sensation);
                    continue;
                }
            }
            return result.ToArray();
        }

        public string[] getIPsFromFile(string filename)
        {
            List<string> ips = new List<string>();
            string filePath = Directory.GetCurrentDirectory() + "\\BepinEx\\Plugins\\OWO" + filename;
            if (File.Exists(filePath))
            {
                LOG("Manual IP file found: " + filePath);
                var lines = File.ReadLines(filePath);
                foreach (var line in lines)
                {
                    IPAddress address;
                    if (IPAddress.TryParse(line, out address)) ips.Add(line);
                    else LOG("IP not valid? ---" + line + "---");
                }
            }
            return ips.ToArray();
        }

        ~OWOSkin()
        {
            LOG("Destructor called");
            DisconnectOWO();
        }

        public void DisconnectOWO()
        {
            LOG("Disconnecting OWO skin.");
            OWO.Disconnect();
        }

        public void Feel(String key, int Priority, float intensity = 1.0f, float duration = 1.0f)
        {
            if (FeedbackMap.ContainsKey(key))
            {
                OWO.Send(FeedbackMap[key].WithPriority(Priority));
                LOG("SENSATION: " + key);
            }
            else LOG("Feedback not registered: " + key);
        }

        public async Task HeartBeatFuncAsync()
        {
            while (heartBeatIsActive)
            {
                Feel("HeartBeat", 1);
                await Task.Delay(1000);
            }
        }

        public async Task SwimmingFuncAsync()
        {
            while (swimmingIsActive)
            {
                Feel("Swimming", 0);
                await Task.Delay(1000);
            }
        }

        public void StartHeartBeat()
        {
            if (heartBeatIsActive) return;

            heartBeatIsActive = true;
            HeartBeatFuncAsync();
        }

        public void StopHeartBeat()
        {
            heartBeatIsActive = false;
        }
        public void StartSwimming()
        {
            if (swimmingIsActive) return;

            swimmingIsActive = true;
            SwimmingFuncAsync();
        }

        public void StopSwimming()
        {
            swimmingIsActive = false;
        }


        public void StopAllHapticFeedback()
        {
            StopHeartBeat();
            StopSwimming();

            OWO.Stop();
        }

        public void PlayBackHit(String key, float myRotation)
        {
            Sensation hitSensation = Sensation.Parse("100,3,76,0,200,0,Impact");

            if (myRotation >= 0 && myRotation <= 180)
            {
                if (myRotation >= 0 && myRotation <= 90) hitSensation = hitSensation.WithMuscles(Muscle.Dorsal_L, Muscle.Lumbar_L);
                else hitSensation = hitSensation.WithMuscles(Muscle.Dorsal_R, Muscle.Lumbar_R);
            }
            else
            {
                if (myRotation >= 270 && myRotation <= 359) hitSensation = hitSensation.WithMuscles(Muscle.Pectoral_L, Muscle.Abdominal_L);
                else hitSensation.WithMuscles(Muscle.Pectoral_R, Muscle.Abdominal_R);
            }

            if (suitDisabled) { return; }
            OWO.Send(hitSensation.WithPriority(3));
        }

        //public void PlayBackHit(String key, float xzAngle, float yShift)
        //{
        //    // two parameters can be given to the pattern to move it on the vest:
        //    // 1. An angle in degrees [0, 360] to turn the pattern to the left
        //    // 2. A shift [-0.5, 0.5] in y-direction (up and down) to move it up or down
        //    if (suitDisabled) { return; }
        //    ScaleOption scaleOption = new ScaleOption(1f, 1f);
        //    RotationOption rotationOption = new RotationOption(xzAngle, yShift);
        //    hapticPlayer.SubmitRegisteredVestRotation(key, key, rotationOption, scaleOption);
        //}

        //public static KeyValuePair<float, float> getAngleAndShift(Transform player, Vector3 hit, float fixRotation = 0f)
        //{
        //    // bhaptics pattern starts in the front, then rotates to the left. 0° is front, 90° is left, 270° is right.
        //    // y is "up", z is "forward" in local coordinates
        //    Vector3 patternOrigin = new Vector3(0f, 0f, 1f);
        //    Vector3 hitPosition = hit - player.position;
        //    Quaternion myPlayerRotation = player.rotation;
        //    //rotation fix if needed
        //    myPlayerRotation *= Quaternion.Euler(0, fixRotation, 0);
        //    Vector3 playerDir = myPlayerRotation.eulerAngles;
        //    // get rid of the up/down component to analyze xz-rotation
        //    Vector3 flattenedHit = new Vector3(hitPosition.x, 0f, hitPosition.z);

        //    // get angle. .Net < 4.0 does not have a "SignedAngle" function...
        //    float hitAngle = Vector3.Angle(flattenedHit, patternOrigin);
        //    // check if cross product points up or down, to make signed angle myself
        //    Vector3 crossProduct = Vector3.Cross(flattenedHit, patternOrigin);
        //    if (crossProduct.y < 0f) { hitAngle *= -1f; }
        //    // relative to player direction
        //    float myRotation = hitAngle - playerDir.y;
        //    // switch directions (bhaptics angles are in mathematically negative direction)
        //    myRotation *= -1f;
        //    //fix rotation
        //    myRotation += -1f * fixRotation;
        //    // convert signed angle into [0, 360] rotation
        //    if (myRotation < 0f) { myRotation = 360f + myRotation; }


        //    // up/down shift is in y-direction
        //    // in Battle Sister, the torso Transform has y=0 at the neck,
        //    // and the torso ends at roughly -0.5 (that's in meters)
        //    // so cap the shift to [-0.5, 0]...
        //    float hitShift = hitPosition.y;
        //    float upperBound = -4.5f;
        //    float lowerBound = -5.5f;
        //    if (hitShift > upperBound) { hitShift = 0.5f; }
        //    else if (hitShift < lowerBound) { hitShift = -0.5f; }
        //    // ...and then spread/shift it to [-0.5, 0.5], which is how bhaptics expects it
        //    else { hitShift = (hitShift - lowerBound) / (upperBound - lowerBound) - 0.5f; }

        //    // No tuple returns available in .NET < 4.0, so this is the easiest quickfix
        //    return new KeyValuePair<float, float>(myRotation, hitShift);
        //}
    }
}
