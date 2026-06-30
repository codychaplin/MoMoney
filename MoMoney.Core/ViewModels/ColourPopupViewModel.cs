using CommunityToolkit.Maui;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace MoMoney.Core.ViewModels;

public record ColorShade(Color Color, string Hex);
public record ColorGroup(string Name, List<ColorShade> Shades);

public partial class ColourPopupViewModel : ObservableObject
{
    readonly IPopupService popupService;

    [ObservableProperty]
    public partial ObservableCollection<ColorGroup> ColourGroups { get; set; } = [];

    readonly Color[] baseColours =
    [
        Color.FromArgb("#ff3535"),
        Color.FromArgb("#ffa237"),
        Color.FromArgb("#ffdd35"),
        Color.FromArgb("#e4ff37"),
        Color.FromArgb("#b6ff42"),
        Color.FromArgb("#3cffae"),
        Color.FromArgb("#3cf2ff"),
        Color.FromArgb("#4394f0"),
        Color.FromArgb("#7437f5"),
        Color.FromArgb("#cd55f9"),
        Color.FromArgb("#906546"),
        Color.FromArgb("#808080"),
    ];

    public ColourPopupViewModel(IPopupService _popupService)
    {
        popupService = _popupService;
        InitializeColours();
    }

    [RelayCommand]
    async Task SelectColour(string colour)
    {
        await popupService.ClosePopupAsync(Shell.Current, colour);
    }

    void InitializeColours()
    {
        var groups = new List<ColorGroup>();
        foreach (var baseColor in baseColours)
        {
            var shades = GenerateShades(baseColor).Select(s => new ColorShade(s, s.ToHex())).ToList();
            groups.Add(new ColorGroup(baseColor.ToHex(), shades));
        }
        ColourGroups = new ObservableCollection<ColorGroup>(groups);
    }

    static Color Lighten(Color c, float amount) => 
        Color.FromRgba(
            c.Red + (1 - c.Red) * amount,
            c.Green + (1 - c.Green) * amount,
            c.Blue + (1 - c.Blue) * amount,
            c.Alpha
        );

    static Color Darken(Color c, float amount) => 
        Color.FromRgba(
            c.Red * (1 - amount),
            c.Green * (1 - amount),
            c.Blue * (1 - amount),
            c.Alpha
        );

    Color[] GenerateShades(Color baseColor)
    {
        return
        [
            Darken(baseColor, 0.66f),
            Darken(baseColor, 0.5f),
            Darken(baseColor, 0.33f),
            baseColor,
            Lighten(baseColor, 0.33f),
            Lighten(baseColor, 0.5f),
            Lighten(baseColor, 0.66f)
        ];
    }
}