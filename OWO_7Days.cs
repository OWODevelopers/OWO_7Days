using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using InControl;
using DynamicMusic;
using static AstarManager;
using System.Runtime.Remoting.Lifetime;
using System.Security.Policy;
using OWOSkin;

namespace OWO_7Days
{
    [BepInPlugin("org.bepinex.plugins.OWO_7Days", "7Days owo integration", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
#pragma warning disable CS0109 // Remove unnecessary warning
        internal static new ManualLogSource Log;
#pragma warning restore CS0109
        public static OWOSkin.OWOSkin owoSkin;
        public static bool isPaused = false;
        public static bool startedHeart = false;
        public static float currentHealth = 0;
        public static bool inventoryOpened = false;
        public static long buttonPressTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        public static bool playerHasSpawned = false;

        private void Awake()
        {
            // Make my own logger so it can be accessed from the Tactsuit class
            Log = base.Logger;
            // Plugin startup logic
            Logger.LogMessage("Plugin OWO_7Days is loaded!");
            owoSkin = new OWOSkin.OWOSkin();
            // one startup heartbeat so you know the vest works correctly
            owoSkin.Feel("HeartBeat", 0);
            // patch all functions
            var harmony = new Harmony("owo.patch.7days");
            harmony.PatchAll();

            Test hand = TTT;
            harmony.Patch(typeof(PlayerAction).GetMethod("Update"), null, new HarmonyMethod(hand));
            
        }
        public delegate void Test();
        public void TTT()
        {
            Log.LogInfo("awfwafasfwafsagsgsegse");
        }
        public static void checkHealth()
        {
            if (Plugin.currentHealth < 15 && Plugin.currentHealth < 0)
            {
                if (!Plugin.startedHeart)
                {
                    Plugin.startedHeart = true;
                    Plugin.owoSkin.StartHeartBeat();
                }
            }
            else
            {
                if (Plugin.startedHeart)
                {
                    Plugin.startedHeart = false;
                    Plugin.owoSkin.StopHeartBeat();
                }
            }
        }
    }
    
    [HarmonyPatch(typeof(EntityPlayerLocal), "LateUpdate")]
    public class owo_OnUpdate
    {
        [HarmonyPostfix]
        public static void Postfix(EntityPlayerLocal __instance)
        {
            Plugin.currentHealth = Traverse.Create(__instance).Field("oldHealth").GetValue<float>();
        }
    }

    [HarmonyPatch(typeof(GameManager), "IsPaused")]
    public class owo_OnPause
    {
        [HarmonyPostfix]
        public static void Postfix(GameManager __instance)
        {
            if(Traverse.Create(__instance).Field("gamePaused").GetValue<bool>() && !Plugin.isPaused)
            {
                Plugin.owoSkin.StopAllHapticFeedback();
                Plugin.startedHeart = false;
                Plugin.isPaused = true;
            }
            else
            {
                Plugin.checkHealth();
                Plugin.isPaused = false;
            }
        }
    }

    [HarmonyPatch(typeof(EntityPlayerLocal), "DamageEntity")]
    public class owo_OnDamage
    {
        [HarmonyPostfix]
        public static void Postfix(EntityPlayerLocal __instance, DamageSource _damageSource)
        {
            if (Plugin.owoSkin.suitDisabled)
            {
                return;
            }

            if (Traverse.Create(__instance).Field("isSpectator").GetValue<bool>())
            {
                return;
            }

            if (__instance.IsDead())
            {
                return;
            }

            if (_damageSource.damageSource == EnumDamageSource.External)
            {
                //KeyValuePair<float, float> coord = OWOSkin.OWOSkin.getAngleAndShift(__instance.transform, _damageSource.getDirection(), 180f);
                //Plugin.owoSkin.Feel("Impact", coord.Key, coord.Value);
                Plugin.owoSkin.Feel("Impact", 0);
            }
            else
            {
                switch (_damageSource.damageType)
                {
                    // Bloodloss
                    case EnumDamageTypes.BloodLoss:
                        Plugin.owoSkin.Feel("BloodLoss",0);
                        break;
                    // electric
                    case EnumDamageTypes.Radiation:
                        Plugin.owoSkin.Feel("Electric", 0);
                        break;
                    case EnumDamageTypes.Cold:
                        Plugin.owoSkin.Feel("Cold", 0);
                        break;
                    case EnumDamageTypes.Heat:
                        Plugin.owoSkin.Feel("Heat", 0);
                        break;
                    case EnumDamageTypes.Electrical:
                        Plugin.owoSkin.Feel("Electric", 0);
                        break;
                    // infection
                    case EnumDamageTypes.Toxic:
                        Plugin.owoSkin.Feel("Toxic", 0);
                        break;
                    case EnumDamageTypes.Disease:
                        Plugin.owoSkin.Feel("Toxic", 0);
                        break;
                    case EnumDamageTypes.Infection:
                        Plugin.owoSkin.Feel("Toxic", 0);
                        break;
                    // stomach
                    case EnumDamageTypes.Starvation:
                        Plugin.owoSkin.Feel("Starvation", 0);
                        break;
                    // lungs
                    case EnumDamageTypes.Suffocation:
                        Plugin.owoSkin.Feel("Suffocation", 0);
                        break;
                    case EnumDamageTypes.Dehydration:
                        Plugin.owoSkin.Feel("Dehydration", 0);
                        break;
                    default:
                        Plugin.owoSkin.Feel("Impact", 0);
                        break;
                }
            }
        }
    }

    [HarmonyPatch(typeof(EntityPlayerLocal), "OnFired")]
    public class owo_OnFired
    {
        [HarmonyPostfix]
        public static void Postfix(EntityPlayerLocal __instance)
        {
            if (Plugin.owoSkin.suitDisabled)
            {
                return;
            }

            if (Traverse.Create(__instance).Field("isSpectator").GetValue<bool>())
            {
                return;
            }
            Plugin.owoSkin.Feel("Recoil_R", 0);
        }
    }

    [HarmonyPatch(typeof(EntityPlayerLocal), "OnEntityDeath")]
    public class owo_OnEntityDeath
    {
        [HarmonyPostfix]
        public static void Postfix(EntityPlayerLocal __instance)
        {
            if (Plugin.owoSkin.suitDisabled)
            {
                return;
            }

            if (Traverse.Create(__instance).Field("isSpectator").GetValue<bool>())
            {
                return;
            }
            Plugin.owoSkin.Feel("Death", 4);
            Plugin.owoSkin.StopAllHapticFeedback();
            Plugin.startedHeart = false;
        }
    }

    [HarmonyPatch(typeof(EntityPlayerLocal), "OnUpdateEntity")]
    public class owo_OnUpdateEntity
    {
        [HarmonyPrefix]
        public static void Prefix(EntityPlayerLocal __instance)
        {
            if (Plugin.owoSkin.suitDisabled)
            {
                return;
            }

            if (__instance.IsDead() || Traverse.Create(__instance).Field("isSpectator").GetValue<bool>())
            {
                return;
            }

            Plugin.Log.LogMessage("Health " + __instance.Health + " " + Plugin.currentHealth + " " + __instance.GetMaxHealth());

            if ((float)__instance.Health - Plugin.currentHealth > 5)
            {
                Plugin.owoSkin.Feel("Heal", 0);
            }
        }

        [HarmonyPostfix]
        public static void Postfix(EntityPlayerLocal __instance)
        {
            if (Plugin.owoSkin.suitDisabled)
            {
                return;
            }

            if (__instance.IsDead() || Traverse.Create(__instance).Field("isSpectator").GetValue<bool>())
            {
                return;
            }
            Plugin.checkHealth();
        }
    }

    [HarmonyPatch(typeof(EntityPlayerLocal), "FireEvent")]
    public class owo_OnFireEvent
    {
        [HarmonyPostfix]
        public static void Postfix(EntityPlayerLocal __instance, MinEventTypes _eventType)
        {
            if (Plugin.owoSkin.suitDisabled)
            {
                return;
            }

            if (Traverse.Create(__instance).Field("isSpectator").GetValue<bool>())
            {
                return;
            }

            switch (_eventType)
            {
                case MinEventTypes.onSelfJump:
                    Plugin.owoSkin.Feel("OnJump", 0);
                    break;

                case MinEventTypes.onSelfRespawn:
                    Plugin.owoSkin.StopAllHapticFeedback();
                    Plugin.startedHeart = false;
                    Plugin.playerHasSpawned = true;
                    break;

                case MinEventTypes.onSelfFirstSpawn:
                    Plugin.owoSkin.StopAllHapticFeedback();
                    Plugin.startedHeart = false;
                    Plugin.playerHasSpawned = true;
                    break; 

                case MinEventTypes.onSelfEnteredGame:
                    Plugin.startedHeart = false;
                    Plugin.playerHasSpawned = true;
                    break;

                default: break;
            }
        }
    }

    [HarmonyPatch(typeof(EntityPlayerLocal), "SwimModeTick")]
    public class owo_OnSwimModeTick
    {
        [HarmonyPostfix]
        public static void Postfix(EntityPlayerLocal __instance)
        {
            if (Plugin.owoSkin.suitDisabled)
            {
                return;
            }

            if (Traverse.Create(__instance).Field("isSpectator").GetValue<bool>())
            {
                return;
            }

            int swimMode = Traverse.Create(__instance).Field("swimMode").GetValue<int>();
            if (swimMode > 0)
            {
                Plugin.owoSkin.StartSwimming();
            }
            else
            {
                Plugin.owoSkin.StopSwimming();
            }
        }
    }

    [HarmonyPatch(typeof(PlayerAction))]
    public class owo_OnInventoryInputPressed
    {
        [HarmonyPostfix]
        public static void Postfix(PlayerAction __instance)
        {
            if (Plugin.owoSkin.suitDisabled || !Plugin.playerHasSpawned)
            {
                return;
            }

            long diff = DateTimeOffset.Now.ToUnixTimeMilliseconds() - Plugin.buttonPressTime;

            if (diff > 500 && 
                (__instance.Name == "Inventory" || (__instance.Name == "Menu" && Plugin.inventoryOpened)) &&
                __instance.IsPressed)
            {
                if (!Plugin.inventoryOpened)
                {
                    Plugin.owoSkin.Feel("InventoryOpen", 0);
                    Plugin.inventoryOpened = true;
                    Plugin.buttonPressTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    return;
                }

                if (Plugin.inventoryOpened)
                {
                    Plugin.owoSkin.Feel("InventoryClose", 0);
                    Plugin.inventoryOpened = false;
                    Plugin.buttonPressTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    return;
                }
            }
        }
    }

    [HarmonyPatch(typeof(EntityAlive), "FireEvent")]
    public class owo_OnFireEventEntityAlive
    {
        [HarmonyPostfix]
        public static void Postfix(EntityAlive __instance, MinEventTypes _eventType)
        {
            if (Plugin.owoSkin.suitDisabled)
            {
                return;
            }

            if (__instance is EntityPlayerLocal &&
                !Traverse.Create(__instance).Field("isSpectator").GetValue<bool>())
            {

                switch (_eventType)
                {
                    case MinEventTypes.onOtherHealedSelf:
                        Plugin.owoSkin.Feel("Heal", 0);
                        break;

                    case MinEventTypes.onSelfWaterSubmerge:
                        Plugin.owoSkin.Feel("EnterWater", 0);
                        OWOSkin.OWOSkin.headUnderwater = true;
                        break;

                    case MinEventTypes.onSelfWaterSurface:
                        Plugin.owoSkin.Feel("ExitWater", 0);
                        OWOSkin.OWOSkin.headUnderwater = false;
                        break;

                    case MinEventTypes.onSelfPrimaryActionRayHit:
                        Plugin.owoSkin.Feel("Recoil_R",0);
                        break;

                    case MinEventTypes.onSelfSecondaryActionRayHit:
                        Plugin.owoSkin.Feel("Recoil_R", 0);
                        break;

                    default: break;
                }
            }
        }
    }



    [HarmonyPatch(typeof(EntityPlayerLocal), "FallImpact")]
    public class owo_OnFallImpact
    {
        [HarmonyPrefix]
        public static void Prefix(EntityPlayerLocal __instance, float speed)
        {
            if (Plugin.owoSkin.suitDisabled)
            {
                return;
            }

            if (__instance.IsDead() || Traverse.Create(__instance).Field("isSpectator").GetValue<bool>())
            {
                return;
            }

            if (speed > 0.02f)
            {
                Plugin.owoSkin.Feel("LandAfterJump", 0);
            }
        }
    }

    [HarmonyPatch(typeof(ItemActionEat), "ExecuteAction")]
    public class owo_OnEatAndDrink
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.owoSkin.suitDisabled)
            {
                return;
            }

            Plugin.owoSkin.Feel("Eating", 0);
        }
    }
    
    [HarmonyPatch(typeof(ItemActionEat), "ExecuteInstantAction")]
    public class owo_OnDrinkAndDrink
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.owoSkin.suitDisabled)
            {
                return;
            }

            Plugin.owoSkin.Feel("Eating", 0);
        }
    }

    [HarmonyPatch(typeof(GameManager), "OnApplicationQuit")]
    public class owo_OnAppQuit
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.owoSkin.suitDisabled)
            {
                return;
            }
            Plugin.owoSkin.StopAllHapticFeedback();
            Plugin.startedHeart = false;
            Plugin.playerHasSpawned = false;
        }
    }

    [HarmonyPatch(typeof(EntityPlayerLocal), "LateUpdate")]
    public class owo_OnLateUpdate
    {
        [HarmonyPostfix]
        public static void Postfix(EntityPlayerLocal __instance)
        {
            if (Plugin.owoSkin.suitDisabled)
            {
                return;
            }
            if (Traverse.Create(__instance).Field("swimMode").GetValue<int>() < 0)
            {
                Plugin.owoSkin.StopSwimming();
            }
        }
    }

    [HarmonyPatch(typeof(EntityPlayerLocal), "OnEntityUnload")]
    public class owo_OnDestroy
    {
        [HarmonyPostfix]
        public static void Postfix(EntityPlayerLocal __instance)
        {
            if (Plugin.owoSkin.suitDisabled)
            {
                return;
            }

            Plugin.owoSkin.StopAllHapticFeedback();
            Plugin.startedHeart = false;
            Plugin.playerHasSpawned = false;
        }
    }
}

