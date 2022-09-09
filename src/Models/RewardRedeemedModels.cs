namespace Twitcher.PubSub.Models;

/// <param name="Timestamp">Time the pubsub message was sent</param>
/// <param name="Redemption">Data about the redemption, includes unique id and user that redeemed it</param>
public record RewardRedeemedModel(DateTime Timestamp, RewardRedeemedRedemptionModel Redemption);

/// <param name="Id">Unique redemption id</param>
/// <param name="User">Data about the user who redeemed</param>
/// <param name="ChannelId">ID of the channel in which the reward was redeemed</param>
/// <param name="RedeemedAt">Timestamp in which a reward was redeemed</param>
/// <param name="Reward">Data about the reward that was redeemed</param>
/// <param name="UserInput">A string that the user entered if the reward requires input</param>
/// <param name="Status">reward redemption status, will be FULFULLED if a user skips the reward queue, UNFULFILLED otherwise</param>
public record RewardRedeemedRedemptionModel(Guid Id,
    RewardRedeemedUserModel User,
    string ChannelId, DateTime RedeemedAt,
    RewardRedeemedRewardModel Reward,
    string? UserInput, string Status);

/// <param name="Id">The ID of the user that redeemed the reward</param>
/// <param name="Login">The login of the user who redeemed the reward</param>
/// <param name="DisplayName">The display name of the user that redeemed the reward</param>
public record RewardRedeemedUserModel(string Id, string Login, string DisplayName);

/// <param name="Id">ID of the reward</param>
/// <param name="ChannelId">ID of the channel in which the reward was redeemed</param>
/// <param name="Title">The title of the reward</param>
/// <param name="Prompt">The prompt for the viewer when they are redeeming the reward</param>
/// <param name="Cost">The cost of the reward</param>
/// <param name="IsUserInputRequired">Does the user need to enter information when redeeming the reward</param>
/// <param name="IsSubOnly">Not described in the twitch API docs</param>
/// <param name="Image">Set of custom images of 1x, 2x and 4x sizes for the reward, can be null if no images have been uploaded</param>
/// <param name="DefaultImage">Set of default images of 1x, 2x and 4x sizes for the reward</param>
/// <param name="BackgroundColor">Custom background color for the reward. Format: Hex with # prefix</param>
/// <param name="IsEnabled">Is the reward currently enabled, if false the reward won’t show up to viewers</param>
/// <param name="IsPaused">Is the reward currently paused, if true viewers can’t redeem</param>
/// <param name="IsInStock">Is the reward currently in stock, if false viewers can’t redeem</param>
/// <param name="MaxPerStream">Whether a maximum per stream is enabled and what the maximum is</param>
/// <param name="MaxPerUserPerStream">Whether a maximum per user per stream is enabled and what the maximum is</param>
/// <param name="GlobalCooldown">Whether a cooldown is enabled and what the cooldown is</param>
/// <param name="ShouldRedemptionsSkipRequestQueue">Should redemptions be set to FULFULLED status immediately when redeemed and skip the request queue instead of the normal UNFULFILLED status</param>
/// <param name="UpdatedForIndicatorAt">Not described in the twitch API docs</param>
/// <param name="RedemptionsRedeemedCurrentStream">Not described in the twitch API docs</param>
/// <param name="CooldownExpiresAt">Not described in the twitch API docs</param>
public record RewardRedeemedRewardModel(Guid Id, string ChannelId, string Title, string Prompt, int Cost, bool IsUserInputRequired, bool IsSubOnly,
    RewardRedeemedImagesModel Image,
    RewardRedeemedImagesModel DefaultImage,
    string BackgroundColor, bool IsEnabled, bool IsPaused, bool IsInStock,
    RewardRedeemedMaxPerStreamModel MaxPerStream,
    RewardRedeemedMaxPerUserPerStreamModel MaxPerUserPerStream,
    RewardRedeemedGlobalCooldownModel GlobalCooldown,
    bool ShouldRedemptionsSkipRequestQueue,
    DateTime? UpdatedForIndicatorAt,
    int? RedemptionsRedeemedCurrentStream,
    DateTime? CooldownExpiresAt);

/// <param name="Url1x">1x image url</param>
/// <param name="Url2x">2x image url</param>
/// <param name="Url4x">4x image url</param>
public record RewardRedeemedImagesModel(string Url1x, string Url2x, string Url4x);

/// <param name="IsEnabled">Whether a maximum per stream is enabled</param>
/// <param name="MaxPerStream">The maximum number per stream if enabled</param>
public record RewardRedeemedMaxPerStreamModel(bool IsEnabled, int MaxPerStream);

/// <param name="IsEnabled">Whether a maximum per user per stream is enabled</param>
/// <param name="MaxPerUserPerStream">The maximum number per user per stream if enabled</param>
public record RewardRedeemedMaxPerUserPerStreamModel(bool IsEnabled, int MaxPerUserPerStream);

/// <param name="IsEnabled">Whether a cooldown is enabled</param>
/// <param name="GlobalCooldownSeconds">The cooldown in seconds if enabled</param>
public record RewardRedeemedGlobalCooldownModel(bool IsEnabled, int GlobalCooldownSeconds);
