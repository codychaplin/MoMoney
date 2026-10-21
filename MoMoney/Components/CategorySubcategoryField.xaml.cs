using System.Collections.ObjectModel;
using System.Windows.Input;
using MoMoney.Core.Models;

namespace MoMoney.Components;

public enum FieldOrientation { Vertical, Horizontal }

public partial class CategorySubcategoryField : ContentView
{
    public static readonly BindableProperty CategoriesProperty =
        BindableProperty.Create(nameof(Categories), typeof(ObservableCollection<Category>), typeof(CategorySubcategoryField), null);

    public static readonly BindableProperty CategoryProperty =
        BindableProperty.Create(nameof(Category), typeof(Category), typeof(CategorySubcategoryField), null, BindingMode.TwoWay);

    public static readonly BindableProperty CategoryChangedCommandProperty =
        BindableProperty.Create(nameof(CategoryChangedCommand), typeof(ICommand), typeof(CategorySubcategoryField), null);

    public static readonly BindableProperty SubcategoriesProperty =
        BindableProperty.Create(nameof(Subcategories), typeof(ObservableCollection<Category>), typeof(CategorySubcategoryField), null);

    public static readonly BindableProperty SubcategoryProperty =
        BindableProperty.Create(nameof(Subcategory), typeof(Category), typeof(CategorySubcategoryField), null, BindingMode.TwoWay);

    public static readonly BindableProperty IsCategoryEnabledProperty =
        BindableProperty.Create(nameof(IsCategoryEnabled), typeof(bool), typeof(CategorySubcategoryField), true);

    public static readonly BindableProperty IsSubcategoryEnabledProperty =
        BindableProperty.Create(nameof(IsSubcategoryEnabled), typeof(bool), typeof(CategorySubcategoryField), true);

    public static readonly BindableProperty SubcategoryChangedCommandProperty =
        BindableProperty.Create(nameof(SubcategoryChangedCommand), typeof(ICommand), typeof(CategorySubcategoryField), null);

    public static readonly BindableProperty OrientationProperty =
        BindableProperty.Create(nameof(Orientation), typeof(FieldOrientation), typeof(CategorySubcategoryField), FieldOrientation.Vertical,
            propertyChanged: (b, _, n) => ((CategorySubcategoryField)b).ApplyOrientation((FieldOrientation)n));

    public ObservableCollection<Category>? Categories
    {
        get => (ObservableCollection<Category>?)GetValue(CategoriesProperty);
        set => SetValue(CategoriesProperty, value);
    }

    public Category? Category
    {
        get => (Category?)GetValue(CategoryProperty);
        set => SetValue(CategoryProperty, value);
    }

    public ICommand? CategoryChangedCommand
    {
        get => (ICommand?)GetValue(CategoryChangedCommandProperty);
        set => SetValue(CategoryChangedCommandProperty, value);
    }

    public ObservableCollection<Category>? Subcategories
    {
        get => (ObservableCollection<Category>?)GetValue(SubcategoriesProperty);
        set => SetValue(SubcategoriesProperty, value);
    }

    public Category? Subcategory
    {
        get => (Category?)GetValue(SubcategoryProperty);
        set => SetValue(SubcategoryProperty, value);
    }

    public bool IsCategoryEnabled
    {
        get => (bool)GetValue(IsCategoryEnabledProperty);
        set => SetValue(IsCategoryEnabledProperty, value);
    }

    public bool IsSubcategoryEnabled
    {
        get => (bool)GetValue(IsSubcategoryEnabledProperty);
        set => SetValue(IsSubcategoryEnabledProperty, value);
    }

    public ICommand? SubcategoryChangedCommand
    {
        get => (ICommand?)GetValue(SubcategoryChangedCommandProperty);
        set => SetValue(SubcategoryChangedCommandProperty, value);
    }

    public FieldOrientation Orientation
    {
        get => (FieldOrientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public CategorySubcategoryField()
    {
        InitializeComponent();
        ApplyOrientation(FieldOrientation.Vertical);
    }

    void ApplyOrientation(FieldOrientation orientation)
    {
        if (pckCategory is null || pckSubcategory is null) return;

        if (orientation == FieldOrientation.Horizontal)
        {
            gridRoot.ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star)];
            gridRoot.RowDefinitions = [new RowDefinition(GridLength.Auto)];
            Grid.SetColumn(pckCategory, 0); Grid.SetRow(pckCategory, 0); Grid.SetColumnSpan(pckCategory, 1);
            Grid.SetColumn(pckSubcategory, 1); Grid.SetRow(pckSubcategory, 0); Grid.SetColumnSpan(pckSubcategory, 1);
        }
        else
        {
            gridRoot.ColumnDefinitions = [new ColumnDefinition(GridLength.Star)];
            gridRoot.RowDefinitions = [new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto)];
            Grid.SetColumn(pckCategory, 0); Grid.SetRow(pckCategory, 0); Grid.SetColumnSpan(pckCategory, 1);
            Grid.SetColumn(pckSubcategory, 0); Grid.SetRow(pckSubcategory, 1); Grid.SetColumnSpan(pckSubcategory, 1);
        }
    }
}
