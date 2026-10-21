using System.Globalization;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Converters;

/// <summary>
/// Gets Category name from Category ID
/// </summary>
public class IdToCategoryConverter : IValueConverter
{
    readonly ICategoryService? categoryService;
    readonly ILoggerService<IdToCategoryConverter>? logger;

    public IdToCategoryConverter()
    {
        categoryService = IPlatformApplication.Current?.Services.GetService<ICategoryService>();
        logger = IPlatformApplication.Current?.Services.GetService<ILoggerService<IdToCategoryConverter>>();
    }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (categoryService != null  &&int.TryParse(value?.ToString(), out int ID))
        {
            // try to get category from dictionary
            if (categoryService.Categories.TryGetValue(ID, out var category))
            {
                if (category.ParentCategoryID.HasValue && categoryService.Categories.TryGetValue(category.ParentCategoryID.Value, out var parent))
                    return $"{parent.CategoryName} - {category.CategoryName}";
                return category.CategoryName;
            }
            else // get category from db
            {
                try
                {
                    var task = Task.Run(async () => 
                    {
                        var cat = await categoryService.GetCategory(ID);
                        if (cat == null)
                            return "";
                        
                        if (!cat.ParentCategoryID.HasValue)
                            return cat.CategoryName;
                        
                        var parentCat = await categoryService.GetCategory(cat.ParentCategoryID.Value);
                        if (parentCat != null)
                            return $"{parentCat.CategoryName} - {cat.CategoryName}";

                        return cat.CategoryName;
                    });
                    task.Wait();
                    return task?.Result ?? "";
                }
                catch (Exception ex)
                {
                    logger?.LogError(nameof(IdToCategoryConverter), ex);
                    return "";
                }
            }
        }

        return "";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}