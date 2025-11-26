namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="RadzenTree.BeforeChange" /> event that is being raised.
/// The event subscriber may cancel changing the selected item.
/// </summary>
public class TreeCancelEventArgs : TreeEventArgs
{
    internal bool Cancelled { get; set; }

    /// <summary>
    /// Cancel the selection.
    /// </summary>
    public void Cancel()
    {
        Cancelled = true;
    }
}

