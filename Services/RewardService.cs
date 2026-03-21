using ProjectM;
using Unity.Entities;
using Stunlock.Core;
using System;
using ProjectM.Network;

namespace BestKillfeed.Services
{
    public class RewardService
    {
        private static RewardService _instance;
        public static RewardService Instance => _instance ??= new RewardService();

        public void GiveKillReward(Entity killerCharacterEntity, int currentStreak)
        {
            try
            {
                if (!KillfeedSettings.EnableKillRewards.Value) return;

                var itemPrefab = new PrefabGUID(KillfeedSettings.RewardItemId.Value);
                int baseAmount = KillfeedSettings.RewardItemAmount.Value;

                float multiplier = 1.0f;
                string streakName = "";

                if (currentStreak >= KillfeedSettings.ThresholdGodlike.Value)
                {
                    multiplier = KillfeedSettings.MultiplierGodlike.Value;
                    streakName = "GODLIKE";
                }
                else if (currentStreak >= KillfeedSettings.ThresholdRelentless.Value)
                {
                    multiplier = KillfeedSettings.MultiplierRelentless.Value;
                    streakName = "RELENTLESS";
                }
                else if (currentStreak >= KillfeedSettings.ThresholdSpree.Value)
                {
                    multiplier = KillfeedSettings.MultiplierSpree.Value;
                    streakName = "KILLING SPREE";
                }

                int amount = (int)Math.Floor(baseAmount * multiplier);
                if (amount <= 0) return;

                Core.ServerGameManager.TryAddInventoryItem(killerCharacterEntity, itemPrefab, amount);

                if (Core.EntityManager.HasComponent<PlayerCharacter>(killerCharacterEntity))
                {
                    var userEntity = Core.EntityManager.GetComponentData<PlayerCharacter>(killerCharacterEntity).UserEntity;
                    if (Core.EntityManager.HasComponent<User>(userEntity))
                    {
                        var userData = Core.EntityManager.GetComponentData<User>(userEntity);
                        string bonusInfo = multiplier > 1.0f ? $" <color=#00ff00>({streakName} x{multiplier})</color>" : "";
                        string message = $"<color=#ffff00>Reward:</color> You received <color=#ffffff>{amount}x</color> items{bonusInfo} for your kill!";
                        Unity.Collections.FixedString512Bytes fMessage = message;
                        ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, userData, ref fMessage);
                    }
                }
            }
            catch (Exception e)
            {
                Core.LogException(e);
            }
        }

        public void GiveBountyReward(Entity characterEntity, string victimName, int streak, bool isSelfClaim = false)
        {
            try
            {
                var itemPrefab = new PrefabGUID(KillfeedSettings.RewardItemId.Value);
                int baseAmount = KillfeedSettings.BountyRewardItemAmount.Value;
                float streakMultiplier = KillfeedSettings.BountyRewardStreakMultiplier.Value;

                // Scale en fonction du streak du porteur de bounty
                int amount = (int)Math.Floor(baseAmount * (1f + streak * streakMultiplier));
                if (amount <= 0) return;

                Core.ServerGameManager.TryAddInventoryItem(characterEntity, itemPrefab, amount);

                if (Core.EntityManager.HasComponent<PlayerCharacter>(characterEntity))
                {
                    var userEntity = Core.EntityManager.GetComponentData<PlayerCharacter>(characterEntity).UserEntity;
                    if (Core.EntityManager.HasComponent<User>(userEntity))
                    {
                        var userData = Core.EntityManager.GetComponentData<User>(userEntity);

                        string scalingInfo = streakMultiplier > 0f
                            ? $" <color=#00ff00>(Streak {streak} — x{(1f + streak * streakMultiplier):F1})</color>"
                            : "";

                        string message = isSelfClaim
                            ? $"<color=#ffff00>Bounty Claimed:</color> You received <color=#ffffff>{amount}x</color> items for surviving your bounty!{scalingInfo}"
                            : $"<color=#ffff00>Bounty Claimed:</color> You received <color=#ffffff>{amount}x</color> items for killing {victimName}!{scalingInfo}";

                        Unity.Collections.FixedString512Bytes fMessage = message;
                        ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, userData, ref fMessage);
                    }
                }
            }
            catch (Exception e)
            {
                Core.LogException(e);
            }
        }
    }
}