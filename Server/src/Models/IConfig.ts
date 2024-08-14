export interface ISkillRewards
{
    ProgressionEnabled: boolean;
    BaseRoubleReward: number;
    RewardPool: IRewardTier[];
}

export interface IRewardTier
{
    Tier: number;
    Weight: number;
    // Item Tpl, and the chance to get it from this tier
    Rewards: Record<string, number>;
}