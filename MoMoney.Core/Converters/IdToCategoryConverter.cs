﻿using System.Globalization;
using MoMoney.Core.Services;
using MoMoney.Core.Exceptions;

namespace MoMoney.Core.Converters;

/// <summary>
/// Gets Category name from Category ID
/// </summary>
public class IdToCategoryConverter : IValueConverter
{
# if ANDROID
    readonly ICategoryService categoryService;
    readonly ILoggerService<IdToCategoryConverter> logger;
#endif

    public IdToCategoryConverter()
    {
#if ANDROID
        categoryService = IPlatformApplication.Current.Services.GetService<ICategoryService>();
        logger = IPlatformApplication.Current.Services.GetService<ILoggerService<IdToCategoryConverter>>();
#endif
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
#if ANDROID
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
#endif

        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}