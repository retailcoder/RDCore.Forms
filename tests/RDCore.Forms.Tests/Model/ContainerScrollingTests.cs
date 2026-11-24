using RDCore.Forms.Libraries;
using RDCore.Forms.Model;
using RDCore.Forms.Porting;
using P = RDCore.Forms.Model.Properties;

namespace RDCore.Forms.Tests.Model;

// A form's and a Frame's scroll bars, the area they scroll, and their Scroll event.
[TestClass]
public sealed class ContainerScrollingTests
{
    private static readonly ComponentClass[] Scrolling = [ComponentClasses.Form, ComponentClasses.Frame];

    [TestMethod]
    public void AFormAndAFrame_HaveTheScrollingProperties_WithTheirDefaults()
    {
        foreach (var type in Scrolling)
        {
            var component = new ComponentInstance(type, "Thing1");
            Assert.AreEqual(ScrollBars.None, component.Get(P.ContainerScrollBars), type.TypeName);
            Assert.AreEqual(ScrollBars.Both, component.Get(P.KeepScrollBarsVisible), type.TypeName);
            Assert.AreEqual(0d, component.Get(P.ScrollHeight), type.TypeName);
            Assert.AreEqual(0d, component.Get(P.ScrollWidth), type.TypeName);
            Assert.AreEqual(0d, component.Get(P.ScrollLeft), type.TypeName);
            Assert.AreEqual(0d, component.Get(P.ScrollTop), type.TypeName);
        }
    }

    [TestMethod]
    public void TheScrollingProperties_AreNamedAsInMSForms_AndAProgramSetsThem()
    {
        PropertyDefinition[] properties = [P.ContainerScrollBars, P.KeepScrollBarsVisible, P.ScrollHeight, P.ScrollWidth, P.ScrollLeft, P.ScrollTop];
        Assert.AreEqual("ScrollBars KeepScrollBarsVisible ScrollHeight ScrollWidth ScrollLeft ScrollTop",
            string.Join(" ", properties.Select(p => p.Name)));
        foreach (var type in Scrolling)
        {
            foreach (var property in properties)
            {
                Assert.IsTrue(type.Has(property), $"{type.TypeName}.{property.Name}");
                Assert.IsFalse(type.IsReadOnlyAtRunTime(property), $"{type.TypeName}.{property.Name}");
            }
        }

        var frame = new ComponentInstance(ComponentClasses.Frame, "Frame1")
            .Set(P.ContainerScrollBars, ScrollBars.Vertical).Set(P.ScrollHeight, 400d).Set(P.ScrollTop, 37.5);
        Assert.AreEqual(ScrollBars.Vertical, frame.Get(P.ContainerScrollBars));
        Assert.AreEqual(37.5, frame.Get(P.ScrollTop));
    }

    [TestMethod]
    public void ATextBoxsScrollBars_StayItsOwn_ReadOnlyAtRunTimeOnAVisualBasic6Form()
    {
        Assert.IsTrue(ComponentClasses.TextBox.Has(P.ScrollBars));
        Assert.IsFalse(ComponentClasses.TextBox.Has(P.ContainerScrollBars));
        Assert.IsTrue(ComponentClasses.TextBox.IsReadOnlyAtRunTimeOnAVisualBasic6Form(P.ScrollBars));
        Assert.IsFalse(ComponentClasses.TextBox.IsReadOnlyAtRunTime(P.ScrollBars));
        Assert.IsFalse(ComponentClasses.Form.Has(P.ScrollBars));
    }

    [TestMethod]
    public void AFormAndAFrame_RaiseScroll_WithTheParametersOfThePortedHandler()
    {
        foreach (var type in Scrolling)
            Assert.IsTrue(type.Events.Contains(Events.ContainerScroll), type.TypeName);
        Assert.IsFalse(ComponentClasses.Frame.Events.Contains(Events.Scroll));

        var ported = FormsPortMap.MSForms.EventOf("UserForm", "Scroll");
        Assert.IsNotNull(ported);
        Assert.AreEqual("Scroll", Events.ContainerScroll.Name);
        Assert.AreEqual(
            string.Join(", ", ported.Parameters.Select(p => $"{p.Name} As {p.RDFormsType}")),
            string.Join(", ", Events.ContainerScroll.Parameters.Select(p => $"{p.Name} As {p.TypeName}")));
    }

    [TestMethod]
    public void AScrollAction_HasTheValueOfItsConstantInRDForms_AsMSFormsNamesIt()
    {
        Assert.IsTrue(RDFormsLibraries.Instance.TryGet(RDFormsLibraries.RDForms, out var rdforms));
        var constants = rdforms.Enums.Single(declared => declared.Name == "fmScrollAction").Members.ToDictionary(member => member.Name, member => member.Value);
        foreach (var action in Enum.GetValues<ScrollAction>())
        {
            var name = action == ScrollAction.AbsoluteChange ? "_fmScrollActionAbsoluteChange" : "fmScrollAction" + action;
            Assert.AreEqual((long)(int)action, constants[name], name);
        }
    }
}
