﻿using DV;
using Harmony12;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DVOwnership
{
    internal class RollingStockManager : SingletonBehaviour<RollingStockManager>
    {
        private List<Equipment> equipment = new List<Equipment>();

        public static new string AllowAutoCreate() { return "DVOwnership_RollingStockManager"; }

        public void Add(Equipment equipment)
        {
            this.equipment.Add(equipment);
        }

        public void Remove(Equipment equipment)
        {
            this.equipment.Remove(equipment);
        }

        public Equipment GetByTrainCar(TrainCar trainCar)
        {
            var equipment = from eq in this.equipment where eq == trainCar select eq;
            var count = equipment.Count();
            if (count > 1) { DVOwnership.LogError($"Unexpected number of equipment found! Expected 1 but found {count} for train car ID {trainCar.ID}."); }
            return equipment.FirstOrDefault();
        }

        public void LoadSaveData(JArray data)
        {
            foreach(var token in data)
            {
                if (token.Type != JTokenType.Object) { continue; }

                equipment.Add(Equipment.FromSaveData((JObject)token));
            }
        }

        public JArray GetSaveData() { return new JArray(from eq in equipment select eq.GetSaveData()); }

        // TODO: This feels hacky. Is there a better way?
        [HarmonyPatch(typeof(CommsRadioController), "PlayAudioFromCar")]
        class CommsRadioController_PlayAudioFromCar_Patch
        {
            static void Postfix(CommsRadioController __instance, AudioClip clip, TrainCar audioOriginCar)
            {
                var removeCarSound = __instance.deleteControl.removeCarSound;
                if (removeCarSound == null || clip != removeCarSound) { return; }

                var manager = Instance;
                var equipment = manager.GetByTrainCar(audioOriginCar);
                if (equipment == null) { return; }

                manager.Remove(equipment);
            }
        }
    }
}