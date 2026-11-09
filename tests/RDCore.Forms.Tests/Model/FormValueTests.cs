using RDCore.Forms.Model;

namespace RDCore.Forms.Tests.Model;

[TestClass]
public sealed class FormValueTests
{
    [TestMethod]
    public void TheDefaultValue_IsEmpty()
    {
        FormValue value = default;

        Assert.AreEqual(FormValueKind.Empty, value.Kind);
        Assert.AreEqual(FormValue.Empty, value);
        Assert.IsNull(value.ClrValue);
    }

    [TestMethod]
    public void AValue_IsReadAsItsKindOnly_WithNoConversion()
    {
        var value = FormValue.FromInteger(5);

        Assert.AreEqual((short)5, value.AsInteger());
        Assert.ThrowsExactly<InvalidOperationException>(() => value.AsLong());
        Assert.ThrowsExactly<InvalidOperationException>(() => value.AsString());
    }

    [TestMethod]
    public void Nothing_IsAnObjectValueHoldingNull()
    {
        Assert.AreEqual(FormValueKind.Object, FormValue.Nothing.Kind);
        Assert.IsNull(FormValue.Nothing.AsObject());
        Assert.AreNotEqual(FormValue.Null, FormValue.Nothing);
    }

    [TestMethod]
    public void TwoObjectValues_AreEqualOnlyWhenTheyHoldTheSameObject()
    {
        var target = new object();

        Assert.AreEqual(FormValue.FromObject(target), FormValue.FromObject(target));
        Assert.AreNotEqual(FormValue.FromObject(target), FormValue.FromObject(new object()));
    }

    [TestMethod]
    public void TwoValuesOfDifferentKinds_AreNotEqual_EvenWhenTheirClrValuesAre()
    {
        Assert.AreNotEqual(FormValue.FromCurrency(1m), FormValue.FromDecimal(1m));
        Assert.AreEqual(FormValue.FromCurrency(1m), FormValue.FromCurrency(1m));
    }

    [TestMethod]
    public void AnArray_IsIndexedFromItsLowerBound()
    {
        var array = new FormArray(FormValueKind.String, 1, [FormValue.FromString("a"), FormValue.FromString("b")]);

        Assert.AreEqual(2, array.UpperBound);
        Assert.AreEqual("b", array[2].AsString());
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => array[0]);
    }

    [TestMethod]
    public void AnArrayOfADeclaredKind_RefusesAnElementOfAnother()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new FormArray(FormValueKind.String, 0, [FormValue.FromLong(1)]));
    }

    [TestMethod]
    public void AnArrayOfVariant_HoldsElementsOfAnyKind()
    {
        var array = new FormArray(FormValueKind.Empty, 0, [FormValue.FromLong(1), FormValue.FromString("a"), FormValue.Null]);

        Assert.AreEqual(3, array.Elements.Count);
    }
}
