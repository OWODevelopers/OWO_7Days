using System;
using System.Collections.Generic;
using OWO_7Days;
using OWOGame;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using InControl;
using UnityEngine;


namespace OWOSkin
{

    public class OWOSkin
    {
        public bool suitDisabled = true;
        public bool systemInitialized = false;
        private static bool heartBeatIsActive = false;
        private static bool swimmingIsActive = false;
        private static bool stringBowIsActive = false;
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

            var gameAuth = GameAuth.Create(AllBakedSensations()).WithId("26202665");

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
                Feel("Heartbeat",0);
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
            }
            else LOG("Feedback not registered: " + key);
        }

        public async Task HeartBeatFuncAsync()
        {
            while (heartBeatIsActive)
            {
                Feel("Heartbeat", 0);
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

        public async Task BowFuncAsync()
        {
            while (stringBowIsActive)
            {
                Feel("Hold Bow", 2);
                await Task.Delay(1000);
            }
        }

        public void StartBow()
        {
            if (stringBowIsActive) return;

            stringBowIsActive = true;
            BowFuncAsync();
        }

        public void StopBow()
        {
            stringBowIsActive = false;
        }

        public async Task FeelStringBow()
        {
            Plugin.owoSkin.Feel("String Bow", 2);
            StartBow();
        }

        public void StopAllHapticFeedback()
        {
            StopHeartBeat();
            StopSwimming();
            StopBow();

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

        internal void FallSensation(float speed)
        {
            OWO.Send(FeedbackMap["JumpLanding"].WithMuscles(Muscle.Abdominal_R.WithIntensity((int)Mathf.Clamp(speed * 100 + 50, 50, 100))).WithPriority(3));
        }
    }
}
