namespace RDCore.Forms.Avalonia.Rendering;

/// <summary>
/// A program whose forms are shown in <see cref="RDFormsFormWindow"/>s: its windows share one active form, which the form events that follow the activation
/// (<c>Activate</c>, <c>Deactivate</c>, and the <c>GotFocus</c> and <c>LostFocus</c> that come with them) are raised for.
/// </summary>
/// <remarks>
/// <para>
/// As in VB6, a program's active form is its own: a window of another program that takes the activation raises nothing on this program's forms, and this
/// program's active form stays active for it.
/// </para>
/// <para>
/// A process that shows the forms of more than one program gives each program its own <see cref="RDFormsProgram"/>, and sets it on each window it makes
/// (<see cref="RDFormsFormWindow.Program"/>). Every window given none is a window of <see cref="Default"/>: in a process that shows one program's forms,
/// that is the program.
/// </para>
/// <para>
/// An <see cref="RDFormsProgram"/> is used on the thread of the windows (Avalonia's UI thread).
/// </para>
/// </remarks>
public sealed class RDFormsProgram
{
    /// <summary>
    /// The program of every window that is given none.
    /// </summary>
    public static RDFormsProgram Default { get; } = new();

    // The window an Activate was last raised for: the program's active form.
    internal RDFormsFormWindow? Active { get; set; }
}
