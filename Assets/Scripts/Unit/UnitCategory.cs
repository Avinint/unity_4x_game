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
    // Nom affiché en jeu (français). À remplacer par le package Localization si le jeu est traduit.
    public static string DisplayName(this UnitCategory category) => category switch
    {
        UnitCategory.Civilian => "civil",
        UnitCategory.Melee => "mêlée",
        UnitCategory.Ranged => "tir",
        UnitCategory.Cavalry => "cavalerie",
        UnitCategory.Siege => "siège",
        UnitCategory.Recon => "éclaireur",
        _ => category.ToString(),
    };
}
