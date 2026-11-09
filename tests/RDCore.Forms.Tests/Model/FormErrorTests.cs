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
}
