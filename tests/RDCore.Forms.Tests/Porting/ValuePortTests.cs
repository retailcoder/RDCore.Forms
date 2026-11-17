using RDCore.Forms.Porting;

namespace RDCore.Forms.Tests.Porting;

[TestClass]
public sealed class ValuePortTests
{
    private static ValuePort TextAlign => FormsPortMap.MSForms.MemberOf("TextBox", "TextAlign")!.Value!;
    private static ValuePort CaptionSide => FormsPortMap.MSForms.MemberOf("CheckBox", "Alignment")!.Value!;
    private static ValuePort CheckBoxValue => FormsPortMap.VB6.MemberOf("CheckBox", "Value")!.Value!;
    private static ValuePort Appearance => FormsPortMap.VB6.MemberOf("TextBox", "Appearance")!.Value!;
    private static ValuePort Stretch => FormsPortMap.VB6.MemberOf("Image", "Stretch")!.Value!;

    private static IEnumerable<ValuePort> EveryValuePort =>
        FormsPortMap.VB6.Members.Concat(FormsPortMap.MSForms.Members).Select(m => m.Value).OfType<ValuePort>();

    private static void AssertConverts(ValuePort port, object legacy, object expected)
    {
        var conversion = port.Convert(legacy);
        Assert.IsTrue(conversion.IsConvertible, $"{legacy}");
        Assert.AreEqual(expected, conversion.Value, $"{legacy}");
        Assert.AreEqual(legacy, conversion.Legacy);
        Assert.IsNull(conversion.Diagnostic);
    }

    private static void AssertNotConvertible(ValuePort port, object? legacy)
    {
        var conversion = port.Convert(legacy);
        Assert.IsFalse(conversion.IsConvertible, $"{legacy}");
        Assert.IsNull(conversion.Value);
        Assert.AreEqual(PortDiagnosticKind.ValueNotConvertible, conversion.Diagnostic?.Kind);
    }

    [TestMethod]
    [DataRow(1, 0)]
    [DataRow(2, 2)]
    [DataRow(3, 1)]
    public void TextAlign_ConvertsToVBsAlignment(int legacy, int expected) => AssertConverts(TextAlign, legacy, expected);

    [TestMethod]
    public void TextAligns_ConstantsAreVBsAlignmentValues()
    {
        Assert.AreEqual("0", TextAlign.Constants["fmTextAlignLeft"]);
        Assert.AreEqual("2", TextAlign.Constants["fmTextAlignCenter"]);
        Assert.AreEqual("1", TextAlign.Constants["fmTextAlignRight"]);
        Assert.IsFalse(TextAlign.GrayedComparesWithIsNull);
    }

    [TestMethod]
    [DataRow(0, 1)]
    [DataRow(1, 0)]
    public void AnMSFormsCaptionsSide_IsInverted(int legacy, int expected) => AssertConverts(CaptionSide, legacy, expected);

    [TestMethod]
    public void AnMSFormsCaptionsSideConstants_AreInverted()
    {
        Assert.AreEqual("1", CaptionSide.Constants["fmAlignmentLeft"]);
        Assert.AreEqual("0", CaptionSide.Constants["fmAlignmentRight"]);
    }

    [TestMethod]
    public void AVBCheckBoxsValue_IsFalseTrueOrNull()
    {
        AssertConverts(CheckBoxValue, 0, false);
        AssertConverts(CheckBoxValue, 1, true);
        AssertConverts(CheckBoxValue, 2, DBNull.Value);
        Assert.IsTrue(CheckBoxValue.GrayedComparesWithIsNull);
        Assert.AreEqual("False", CheckBoxValue.Constants["vbUnchecked"]);
        Assert.AreEqual("True", CheckBoxValue.Constants["vbChecked"]);
        Assert.AreEqual("Null", CheckBoxValue.Constants["vbGrayed"]);
    }

    [TestMethod]
    public void VBsAppearance_IsFlatOrSunken()
    {
        AssertConverts(Appearance, 0, 0);
        AssertConverts(Appearance, 1, 2);
        // VB6's VB and VBRUN libraries name no constant of Appearance's values, so a program writes 0 and 1.
        Assert.IsEmpty(Appearance.Constants);
    }

    [TestMethod]
    public void Stretch_IsClipOrStretch_AndABooleanIsVBAsNumberForIt()
    {
        AssertConverts(Stretch, false, 0);
        AssertConverts(Stretch, true, 1);
        AssertConverts(Stretch, -1, 1);
        AssertConverts(Stretch, (short)0, 0);
        Assert.IsEmpty(Stretch.Constants);
        AssertNotConvertible(Stretch, 1);
    }

    [TestMethod]
    public void ANumber_ComparesByValue_WhateverItsType()
    {
        foreach (var one in new object[] { (byte)1, (sbyte)1, (short)1, (ushort)1, 1, 1u, 1L, 1ul, 1.0f, 1.0, 1m })
        {
            AssertConverts(CheckBoxValue, one, true);
        }
    }

    [TestMethod]
    public void ABooleanForANumberedValue_IsNotConvertible()
    {
        AssertNotConvertible(CheckBoxValue, true);
        AssertNotConvertible(TextAlign, false);
    }

    [TestMethod]
    public void AnUndocumentedValue_IsNotConvertible()
    {
        foreach (var legacy in new object?[]
                 {
                     0, 4, -1, 1.5, 2.000001, double.NaN, double.PositiveInfinity, 1e300, ulong.MaxValue, decimal.MaxValue, 1.5m,
                     "1", "fmTextAlignLeft", "", 'a', DBNull.Value, null, new object(), DateTime.MinValue, new[] { 1 },
                 })
        {
            AssertNotConvertible(TextAlign, legacy);
        }
    }

    [TestMethod]
    public void EveryValuePort_ConvertsEachDocumentedValue_AndNothingElse()
    {
        var ports = EveryValuePort.ToList();
        Assert.HasCount(10, ports);
        foreach (var port in ports)
        {
            foreach (var (legacy, expected) in port.Values)
            {
                AssertConverts(port, legacy, expected);
            }

            foreach (var other in new object?[] { 42, "x", null, 0.5, new object() })
            {
                AssertNotConvertible(port, other);
            }
        }
    }

    [TestMethod]
    public void ANotConvertibleValue_IsNamedInItsDiagnostic_AsVBAWritesIt()
    {
        Assert.AreEqual("4", TextAlign.Convert(4).Diagnostic?.Arguments.Single());
        Assert.AreEqual("1.5", TextAlign.Convert(1.5).Diagnostic?.Arguments.Single());
        Assert.AreEqual("\"a\"\"b\"", TextAlign.Convert("a\"b").Diagnostic?.Arguments.Single());
        Assert.AreEqual("True", TextAlign.Convert(true).Diagnostic?.Arguments.Single());
        Assert.AreEqual("Null", TextAlign.Convert(DBNull.Value).Diagnostic?.Arguments.Single());
        Assert.AreEqual("Nothing", TextAlign.Convert(null).Diagnostic?.Arguments.Single());
    }

    [TestMethod]
    public void AValuePort_MadeByACaller_ConvertsItsOwnValues()
    {
        var port = new ValuePort(new Dictionary<object, object> { [(short)5] = "five", [true] = 1 }, new Dictionary<string, string> { ["Five"] = "5" });
        AssertConverts(port, 5L, "five");
        AssertConverts(port, -1, 1);
        Assert.AreEqual("5", port.Constants["FIVE"]);
        AssertNotConvertible(port, 6);
    }

    [TestMethod]
    public void AValuePort_WithTwoKeysOfTheSameValue_IsRefused() =>
        Assert.ThrowsExactly<ArgumentException>(() => new ValuePort(new Dictionary<object, object> { [1] = 1, [1L] = 2 }, new Dictionary<string, string>()));
}
