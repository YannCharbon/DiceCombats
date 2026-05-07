namespace DiceCombats;

public sealed class CustomFieldChangeInfo
{
    public DCCreatureCustomField Field { get; set; } = default!;
    public object? OldValue { get; set; }
    public object? NewValue { get; set; }
    public object? MaxValue { get; set; }
    public string Action { get; set; } = "Changed";
    public string? ChangeDirection { get; set; }
    public int? OptionIndex { get; set; }
    public string? OptionName { get; set; }
    public int? RowIndex { get; set; }
    public string? RowName { get; set; }
    public int? ColumnIndex { get; set; }
    public string? ColumnName { get; set; }
    public int? StatIndex { get; set; }
    public string? StatName { get; set; }
    public string? TextLabel { get; set; }
}
