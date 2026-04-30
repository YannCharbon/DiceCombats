namespace DiceCombats;

public sealed class CustomFieldChangeInfo
{
    public DCCreatureCustomField Field { get; set; } = default!;
    public object? OldValue { get; set; }
    public object? NewValue { get; set; }
}
