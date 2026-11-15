using System.Globalization;
using System.Reflection;
using RDCore.Forms.Model;

namespace RDCore.Forms.Tests.Model;

[TestClass]
public sealed class FormErrorTests
{
    private static readonly PropertyInfo[] StandardErrors = typeof(FormError)
        .GetProperties(BindingFlags.Public | BindingFlags.Static)
        .Where(p => p.PropertyType == typeof(FormError))
        .ToArray();

    private static T InCulture<T>(string culture, Func<T> read)
    {
        var before = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
        try
        {
            return read();
        }
        finally
        {
            CultureInfo.CurrentUICulture = before;
        }
    }

    [TestMethod]
    public void AStandardError_HasVBsDescription_InTheCurrentUICulture()
    {
        Assert.AreEqual("Type mismatch", InCulture("en-CA", () => FormError.TypeMismatch.Description));
        Assert.AreEqual("Incompatibilité de type", InCulture("fr-CA", () => FormError.TypeMismatch.Description));
        Assert.AreEqual(13, FormError.TypeMismatch.Number);
    }

    [TestMethod]
    public void EveryStandardError_HasAnEnglishAndAFrenchDescription()
    {
        foreach (var property in StandardErrors)
        {
            var english = InCulture("en-CA", () => (FormError)property.GetValue(null)!);
            var french = InCulture("fr-CA", () => (FormError)property.GetValue(null)!);

            Assert.AreNotEqual($"Error {english.Number}", english.Description, property.Name);
            Assert.AreNotEqual(english.Description, french.Description, property.Name);
        }
    }

    [TestMethod]
    public void TheExceptionsMessage_IsTheErrorsDescription()
    {
        var exception = InCulture("fr-CA", () => new FormErrorException(FormError.InvalidPropertyValue));

        Assert.AreEqual("Valeur de propriété incorrecte", exception.Message);
        Assert.AreEqual(380, exception.Error.Number);
    }

    [TestMethod]
    public void Error387_NamesTheProperty_InEitherLanguage()
    {
        Assert.AreEqual("'Checked' property can't be set on this control",
            InCulture("en-CA", () => FormError.PropertyCantBeSetOnThisControl("Checked").Description));
        Assert.AreEqual("Impossible de définir la propriété « Checked » pour ce contrôle",
            InCulture("fr-CA", () => FormError.PropertyCantBeSetOnThisControl("Checked").Description));
        Assert.AreEqual(387, FormError.PropertyCantBeSetOnThisControl("Visible").Number);
    }

    [TestMethod]
    public void Error340_NamesTheElementsIndex_InEitherLanguage()
    {
        Assert.AreEqual("Control array element '12' doesn't exist",
            InCulture("en-CA", () => FormError.ControlArrayElementDoesntExist(12).Description));
        Assert.AreEqual("L'élément « 12 » du groupe de contrôles n'existe pas",
            InCulture("fr-CA", () => FormError.ControlArrayElementDoesntExist(12).Description));
        Assert.AreEqual(340, FormError.ControlArrayElementDoesntExist(12).Number);
    }

    [TestMethod]
    public void EveryErrorThatNamesSomething_NamesItInEitherLanguage()
    {
        var named = typeof(FormError).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.ReturnType == typeof(FormError) && m.GetParameters() is [{ ParameterType: var type }] && type == typeof(string));
        foreach (var method in named)
        {
            var english = InCulture("en-CA", () => (FormError)method.Invoke(null, ["Thing1"])!);
            var french = InCulture("fr-CA", () => (FormError)method.Invoke(null, ["Thing1"])!);

            StringAssert.Contains(english.Description, "Thing1", method.Name);
            StringAssert.Contains(french.Description, "Thing1", method.Name);
            Assert.AreNotEqual(english.Description, french.Description, method.Name);
        }
    }

    [TestMethod]
    public void TheControlsCollectionsErrors_ReadAsVB6s()
    {
        var read = InCulture("en-CA", () => new[]
        {
            FormError.InvalidClassString("VB.Bogus"), FormError.NotAValidControlType("Menu"), FormError.NotAValidControlContainer("CommandButton"),
            FormError.ControlNameTaken("cmdNew"), FormError.NotALegalControlName(""), FormError.CanOnlyRemoveAddedControls,
            FormError.ControlNotFound("nothere"), FormError.ControlRemoved, FormError.ContainerHoldsADesignTimeControl("Command1"),
        });

        CollectionAssert.AreEqual(new[] { 711, 720, 721, 727, 728, 729, 730, 735, 740 }, read.Select(e => e.Number).ToArray());
        StringAssert.StartsWith(read[0].Description, "Invalid class string.");
        StringAssert.EndsWith(read[0].Description, "Looking for object with ProgID: VB.Bogus");
        CollectionAssert.AreEqual(new[]
        {
            "'Menu' is not a valid control type",
            "'CommandButton' is not a valid control container",
            "There is already a control with the name 'cmdNew'",
            "'' is not a legal control name",
            "Controls.Remove can only remove controls added with Controls.Add",
            "Control 'nothere' not found",
            "The control is no longer usable because it has been removed from the controls collection",
            "Controls.Remove can only remove container controls that contain controls added with Controls.Add.  'Command1' was not added with Controls.Add.",
        }, read.Skip(1).Select(e => e.Description).ToArray());
    }
}
