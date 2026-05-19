namespace Stylebazar.DAL.Entities.Enums;

/// <summary>
/// Premium subscription tier for sellers. Free is the default for every
/// account; Pro / VIP are reached via admin upgrade after manual payment
/// (no online gateway yet).
///
/// Integer values are stored in DB so reordering or renaming requires a
/// data migration.
/// </summary>
public enum SubscriptionPlan
{
    /// <summary>Default. 5 product cap, no analytics, no verified badge.</summary>
    Free = 0,

    /// <summary>50 product cap, full analytics dashboard, ₼15/month.</summary>
    Pro = 1,

    /// <summary>500 product cap, advanced analytics, verified badge, catalog priority, ₼49/month.</summary>
    VIP = 2
}
