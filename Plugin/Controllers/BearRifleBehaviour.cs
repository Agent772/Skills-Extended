﻿using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using SkillsExtended.Helpers;
using SkillsExtended.Models;
using System.Collections.Generic;
using SkillsExtended.Skills;
using UnityEngine;

namespace SkillsExtended.Controllers;

internal class BearRifleBehaviour : MonoBehaviour
{
    private static bool _isSubscribed = false;

    public readonly Dictionary<string, int> WeaponInstanceIds = [];
    public IEnumerable<Item> BearWeapons = null;

    private static SkillManager SkillManager => Utils.GetActiveSkillManager();
    private static ISession Session => Plugin.Session;

    private static GameWorld GameWorld => Singleton<GameWorld>.Instance;

    private static int BearAkLevel => Session.Profile.Skills.BearAksystems.Level;
    private static WeaponSkillData BearSkillData => Plugin.SkillData.BearRifleSkill;
    
    // Store an object containing the weapons original stats.
    private readonly Dictionary<string, OrigWeaponValues> _originalWeaponValues = [];
    
    private void Update()
    {
        SetupSkillManager();

        if (SkillManager == null || BearWeapons == null) { return; }

        UpdateWeapons();
    }

    private static void SetupSkillManager()
    {
        if (_isSubscribed || SkillManager is null) return;
        
        if (GameWorld?.MainPlayer is null || GameWorld?.MainPlayer?.Location == "hideout")
        {
            return;
        }

        SkillManager.OnMasteringExperienceChanged += ApplyBearAkXp;
        _isSubscribed = true;
    }

    private static void ApplyBearAkXp(MasterSkillClass action)
    {
        var weaponInHand = Singleton<GameWorld>.Instance.MainPlayer.HandsController.GetItem();

        if (!BearSkillData.Weapons.Contains(weaponInHand.TemplateId))
        {
            return;
        }
        
        SkillBuffs.BearRifleAction.Complete(BearSkillData.WeaponProfXp);
    }

    private void UpdateWeapons()
    {
        foreach (var item in BearWeapons)
        {
            if (item is not Weapon weapon) return;

            // Store the weapons original values
            if (!_originalWeaponValues.ContainsKey(item.TemplateId))
            {
                var origVals = new OrigWeaponValues
                {
                    ergo = weapon.Template.Ergonomics,
                    weaponUp = weapon.Template.RecoilForceUp,
                    weaponBack = weapon.Template.RecoilForceBack
                };

                Plugin.Log.LogDebug($"original {weapon.LocalizedName()} ergo: {weapon.Template.Ergonomics}, up {weapon.Template.RecoilForceUp}, back {weapon.Template.RecoilForceBack}");

                _originalWeaponValues.Add(item.TemplateId, origVals);
            }

            //Skip instances of the weapon that are already adjusted at this level.
            if (WeaponInstanceIds.ContainsKey(item.Id))
            {
                if (WeaponInstanceIds[item.Id] == BearAkLevel)
                {
                    continue;
                }

                WeaponInstanceIds.Remove(item.Id);
            }

            weapon.Template.Ergonomics = _originalWeaponValues[item.TemplateId].ergo * (1 + SkillBuffs.BearAkSystemsErgoBuff);
            weapon.Template.RecoilForceUp = _originalWeaponValues[item.TemplateId].weaponUp * (1 - SkillBuffs.BearAkSystemsRecoilBuff);
            weapon.Template.RecoilForceBack = _originalWeaponValues[item.TemplateId].weaponBack * (1 - SkillBuffs.BearAkSystemsRecoilBuff);

            Plugin.Log.LogDebug($"New {weapon.LocalizedName()} ergo: {weapon.Template.Ergonomics}, up {weapon.Template.RecoilForceUp}, back {weapon.Template.RecoilForceBack}");

            WeaponInstanceIds.Add(item.Id, BearAkLevel);
        }
    }
}