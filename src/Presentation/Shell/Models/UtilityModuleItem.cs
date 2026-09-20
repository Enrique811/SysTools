using System.Text.RegularExpressions;
using SysTools.Presentation.ViewModels;

namespace SysTools.Presentation.Shell.Models;

public enum ModuleSection
{
    Utilities,
    Administration
}

public sealed partial class UtilityModuleItem : ViewModelBase
{
    private bool _isSelected;

    public UtilityModuleItem(
        string id,
        string displayName,
        ModuleSection section,
        bool isEnabled,
        ViewModelBase? content = null,
        bool isSelected = false)
    {
        if (string.IsNullOrWhiteSpace(id) || !KebabCasePattern().IsMatch(id))
        {
            throw new ArgumentException("Module id must be non-empty kebab-case.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name is required.", nameof(displayName));
        }

        if (isEnabled && content is null)
        {
            throw new ArgumentException("Enabled modules require content.", nameof(content));
        }

        if (!isEnabled && content is not null)
        {
            throw new ArgumentException("Disabled placeholders cannot expose content.", nameof(content));
        }

        Id = id;
        DisplayName = displayName;
        Section = section;
        IsEnabled = isEnabled;
        Content = content;
        _isSelected = isSelected && isEnabled;
    }

    public string Id { get; }

    public string DisplayName { get; }

    public ModuleSection Section { get; }

    public bool IsEnabled { get; }

    public ViewModelBase? Content { get; }

    public bool IsSelected
    {
        get => _isSelected;
        private set => SetProperty(ref _isSelected, value);
    }

    internal void SetSelected(bool value) => IsSelected = value && IsEnabled;

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex KebabCasePattern();
}
