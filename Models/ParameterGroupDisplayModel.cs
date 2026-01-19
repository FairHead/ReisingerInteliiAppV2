using System.Collections.ObjectModel;

namespace ReisingerIntelliApp_V4.Models;

/// <summary>
/// Display model for a group of parameters with a category header.
/// Used for grouped display in the UI with CollectionView IsGrouped="True".
/// Inherits from ObservableCollection so the CollectionView can enumerate the items.
/// </summary>
public class ParameterGroupDisplayModel : ObservableCollection<DeviceParameterDisplayModel>
{
    /// <summary>
    /// The category of this group.
    /// </summary>
    public ParameterCategory Category { get; init; }
    
    /// <summary>
    /// Display name for the category.
    /// </summary>
    public string CategoryName => Category switch
    {
        ParameterCategory.Zeiten => "?? Zeiten",
        ParameterCategory.Weiten => "?? Weiten",
        ParameterCategory.Tempo => "?? Tempo",
        ParameterCategory.IO => "?? I/O",
        ParameterCategory.Basis => "?? Basis",
        _ => "Sonstige"
    };
    
    /// <summary>
    /// Short name without emoji for compact display.
    /// </summary>
    public string ShortName => Category switch
    {
        ParameterCategory.Zeiten => "Zeiten",
        ParameterCategory.Weiten => "Weiten",
        ParameterCategory.Tempo => "Tempo",
        ParameterCategory.IO => "I/O",
        ParameterCategory.Basis => "Basis",
        _ => "Sonstige"
    };
    
    /// <summary>
    /// Number of parameters in this group.
    /// </summary>
    public int ParameterCount => Count;
    
    /// <summary>
    /// Whether this group is expanded in the UI.
    /// </summary>
    public bool IsExpanded { get; set; } = true;
    
    /// <summary>
    /// Creates a new parameter group with the specified category.
    /// </summary>
    public ParameterGroupDisplayModel(ParameterCategory category) : base()
    {
        Category = category;
    }
    
    /// <summary>
    /// Creates a new parameter group with the specified category and initial items.
    /// </summary>
    public ParameterGroupDisplayModel(ParameterCategory category, IEnumerable<DeviceParameterDisplayModel> items) 
        : base(items)
    {
        Category = category;
    }
}
