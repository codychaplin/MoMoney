using System.Globalization;
using MoMoney.Core.Exceptions;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.Converters;

/// <summary>
/// Gets Category name from Category ID
/// </summary>
public class IdToCategoryConverter : IValueConverter
{
    readonly ICategoryService categoryService;
    readonly ILoggerService<IdToCategoryConverter> logger;

    public IdToCategoryConverter()
    {
        categoryService = IPlatformApplication.Current.Services.GetService<ICategoryService>();
        logger = IPlatformApplication.Current.Services.GetService<ILoggerService<IdToCategoryConverter>>();
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (int.TryParse(value.ToString(), out int ID))
        {
            // try to get category from dictionary
            if (categoryService.Categories.TryGetValue(ID, out var category))
            {
                return category.CategoryName;
            }
            else // get category from db
            {
                try
                {
                    var task = Task.Run(async () => await categoryService.GetCategory(ID));
                    task.Wait();
                    var cat = task.Result;
                    return cat.CategoryName;
                }
                catch (CategoryNotFoundException ex)
                {
                    logger.LogError(nameof(IdToCategoryConverter), ex);
                    return "";
                }
                catch (Exception ex)
                {
                    logger.LogError(nameof(IdToCategoryConverter), ex);
                    return "";
                }
            }
        }

        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}