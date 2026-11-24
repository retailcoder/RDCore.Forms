using System.Text;
using RDCore.Forms.Xaml;
using RDCore.SDK.Workspace.Forms;

namespace RDCore.Forms.Tests.Xaml;

[TestClass]
public sealed class FormXamlDocumentReaderTests
{
    private const string Ns = FormXaml.Namespace;

    private const string Code = "Attribute VB_Name = \"Form1\"\r\nOption Explicit\r\n";

    private static readonly string Document = $"""
        <Form xmlns="{Ns}" FormatVersion="1" Name="Form1" Caption="Orders" Width="480">
          <Frame Name="Frame1" TabIndex="0">
            <TextBox Name="Text1" TabIndex="1" />
          </Frame>
          <OptionButton Name="Option1" Index="0" TabIndex="2" />
          <OptionButton Name="Option1" Index="1" TabIndex="3" />
          <UnmodelledComponent TypeName="MSComctlLib.Slider" Name="Slider1">Begin MSComctlLib.Slider Slider1</UnmodelledComponent>
          <Menu Name="mnuFile" Caption="File"><Menu Name="mnuOpen" Caption="Open" /></Menu>
        </Form>
        """;

    private static string Tree(IEnumerable<FormControlDescriptor> controls) =>
        string.Join(" ", controls.Select(c => $"{c.Class}:{c.Name}{(c.Index is { } i ? $"({i})" : "")}{(c.TabIndex is { } t ? $"#{t}" : "")}"
            + (c.Children.Count > 0 ? $"[{Tree(c.Children)}]" : "")));

    [TestMethod]
    public void AnRDFormsDocument_DeclaresItsFormAndEachOfItsControls_InTheDocumentsOrder()
    {
        var form = FormXamlDocumentReader.DescriptorOf(Document);

        Assert.AreEqual("Form1", form.Name);
        Assert.AreEqual(FormDesignerKind.RDForms, form.Kind);
        Assert.AreEqual("RDForms.Form", form.Class.ToString());
        Assert.AreEqual("Orders", form.Caption);
        Assert.AreEqual("480", form.Properties["width"]);
        Assert.AreEqual(Document, form.DesignerText);
        Assert.AreEqual(
            "RDForms.Frame:Frame1#0[RDForms.TextBox:Text1#1] RDForms.OptionButton:Option1(0)#2 RDForms.OptionButton:Option1(1)#3 "
            + "RDForms.Menu:mnuFile[RDForms.Menu:mnuOpen]",
            Tree(form.Controls));
        CollectionAssert.AreEqual(new[] { "Frame1", "Text1", "Option1", "mnuFile", "mnuOpen" }, form.DeclaredControls().Select(c => c.Name).ToArray());
    }

    [TestMethod]
    public void ADocumentsBytes_AreReadAsItsText_WhateverMarkItOpensOn()
    {
        var plain = FormXamlDocumentReader.Instance.Read(Encoding.UTF8.GetBytes(Document));
        var marked = FormXamlDocumentReader.Instance.Read(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(Document)).ToArray());
        var utf16 = FormXamlDocumentReader.Instance.Read(Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(Document)).ToArray());

        Assert.AreEqual(FormDesigner.RDFormsDocumentExtension, FormXamlDocumentReader.Instance.DocumentExtension);
        foreach (var form in new[] { plain, marked, utf16 })
            Assert.AreEqual(Tree(FormXamlDocumentReader.DescriptorOf(Document).Controls), Tree(form.Controls));
    }

    [TestMethod]
    [DataRow("<Form", DisplayName = "not well-formed")]
    [DataRow("<Window xmlns=\"https://rubberduckvba.ca/schemas/rdforms\" Name=\"Form1\" />", DisplayName = "no form")]
    public void ADocumentThatHoldsNoForm_IsAnInvalidDocument(string document)
    {
        var problem = Assert.ThrowsExactly<FormDesignerFormatException>(() => FormXamlDocumentReader.DescriptorOf(document));
        Assert.AreEqual(FormDesignerFormatProblem.InvalidDocument, problem.Problem);
    }

    [TestMethod]
    public void Registered_ItIsWhatAnRDFormsModuleDeclares()
    {
        FormDesigner.Register(FormXamlDocumentReader.Instance);

        var form = FormDesigner.Read("Forms/Form1.rdform", Code, Encoding.UTF8.GetBytes(Document), out var problem);

        Assert.IsNull(problem);
        Assert.AreEqual(Tree(FormXamlDocumentReader.DescriptorOf(Document).Controls), Tree(form!.Controls));
        Assert.IsNull(FormDesigner.Read("Forms/Form1.rdform", Code, Encoding.UTF8.GetBytes("<Form"), out var invalid));
        Assert.AreEqual(FormDesignerFormatProblem.InvalidDocument, invalid?.Problem);
    }
}
