using System.Globalization;
using MoMoney.Services;
using MoMoney.Exceptions;

namespace MoMoney.Converters;

/// <summary>
/// Gets Category name from Category ID
/// </summary>
class IdToCategoryConverter : IValueConverter
{
    readonly ICategoryService categoryService;
    readonly ILoggerService<IdToCategoryConverter> logger;

    public IdToCategoryConverter()
    {
        categoryService = MauiApplication.Current.Services.GetService<ICategoryService>();
        logger = MauiApplication.Current.Services.GetService<ILoggerService<IdToCategoryConverter>>();
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
                    logger.LogError(ex.Message, ex.GetType().Name);
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