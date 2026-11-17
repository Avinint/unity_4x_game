using System;
using NUnit.Framework;
using UnityEngine;

public class UnitCategoryTests
{
    private static UnitType NewType(UnitCategory category)
    {
        var type = ScriptableObject.CreateInstance<UnitType>();
        typeof(UnitType).GetField("<Category>k__BackingField",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(type, category);
        return type;
    }

    [Test]
    public void IsMilitary_FalseOnlyForCivilian()
    {
        foreach (UnitCategory category in Enum.GetValues(typeof(UnitCategory)))
        {
            var type = NewType(category);
            Assert.AreEqual(category != UnitCategory.Civilian, type.IsMilitary, $"{category}");
            UnityEngine.Object.DestroyImmediate(type);
        }
    }
}
