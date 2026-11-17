public enum UnitCategory
{
    Civilian,   // colon, ouvrier, marchand : ne combat pas
    Melee,      // infanterie de mêlée (guerrier, piquier, épéiste)
    Ranged,     // tir (archer, arbalétrier)
    Cavalry,    // cavalerie
    Siege,      // engins de siège (bélier, catapulte, trébuchet)
    Recon,      // éclaireurs
}

public static class UnitCategoryExtensions
{
    // Nom affiché, traduit (table Units, clé "category.melee"…).
    public static string DisplayName(this UnitCategory category) =>
        L10n.Get("Units", "category." + category.ToString().ToLower());
}
