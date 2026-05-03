public class TitleService
{
    private string _title = string.Empty;
    public string Title
    {
        get => _title;
        set
        {
            _title = value;
            NotifyStateChanged();
        }
    }

    public event Action? OnChange;

    private void NotifyStateChanged() => OnChange?.Invoke();
}
