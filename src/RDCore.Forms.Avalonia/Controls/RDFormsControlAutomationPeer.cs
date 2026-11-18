using Avalonia.Automation.Peers;
using Avalonia.Controls;

namespace RDCore.Forms.Avalonia.Controls;

// A control UI Automation sees as the Win32 control VB6 draws for it: a Label as Text, a Frame as a Group, a PictureBox and an Image as
// an Image. Its name is its caption, where it has one, as the control sets it (AutomationProperties.Name).
internal sealed class RDFormsControlAutomationPeer(Control owner, AutomationControlType type) : ControlAutomationPeer(owner)
{
    protected override AutomationControlType GetAutomationControlTypeCore() => type;
}
