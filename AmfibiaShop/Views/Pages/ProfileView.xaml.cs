using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AmfibiaShop.Presenters.Display;

namespace AmfibiaShop.Views.Pages;

/// <summary>ВИД профиля покупателя.</summary>
public partial class ProfileView : UserControl, IProfileView
{
    private readonly Dictionary<string, TextBlock> profileErrors;
    private readonly Dictionary<string, TextBlock> passwordErrors;

    public ProfileView()
    {
        InitializeComponent();
        profileErrors = new Dictionary<string, TextBlock>
        {
            ["FirstName"] = FirstNameError, ["LastName"] = LastNameError, ["Phone"] = PhoneError,
            ["City"] = CityError, ["Postal"] = PostalError
        };
        passwordErrors = new Dictionary<string, TextBlock>
        {
            ["CurrentPassword"] = CurrentPasswordError, ["NewPassword"] = NewPasswordError, ["RepeatPassword"] = RepeatPasswordError
        };
    }

    public event EventHandler? SaveRequested;
    public event EventHandler? ChangePasswordRequested;
    public event EventHandler? LogoutRequested;

    public string FirstName => FirstNameBox.Text;
    public string LastName => LastNameBox.Text;
    public string Phone => PhoneBox.Text;
    public string City => CityBox.Text;
    public string Street => StreetBox.Text;
    public string House => HouseBox.Text;
    public string Apartment => ApartmentBox.Text;
    public string Postal => PostalBox.Text;
    public string CurrentPassword => CurrentPasswordBox.Password;
    public string NewPassword => NewPasswordBox.Password;
    public string RepeatPassword => RepeatPasswordBox.Password;

    private void SaveButton_Click(object sender, RoutedEventArgs e) => SaveRequested?.Invoke(this, EventArgs.Empty);
    private void ChangePasswordButton_Click(object sender, RoutedEventArgs e) => ChangePasswordRequested?.Invoke(this, EventArgs.Empty);
    private void LogoutButton_Click(object sender, RoutedEventArgs e) => LogoutRequested?.Invoke(this, EventArgs.Empty);

    public void ShowProfile(ProfileViewData profile)
    {
        InitialsText.Text = profile.Initials;
        NameText.Text = $"{profile.FirstName} {profile.LastName}";
        EmailText.Text = profile.Email;
        RoleText.Text = profile.Role;
        SinceText.Text = profile.Since;
        StatsItems.ItemsSource = profile.Stats;
        FirstNameBox.Text = profile.FirstName;
        LastNameBox.Text = profile.LastName;
        PhoneBox.Text = profile.Phone;
        EmailBox.Text = profile.Email;
        CityBox.Text = profile.City;
        StreetBox.Text = profile.Street;
        HouseBox.Text = profile.House;
        ApartmentBox.Text = profile.Apartment;
        PostalBox.Text = profile.Postal;
    }

    public void ShowProfileErrors(IReadOnlyDictionary<string, string> errors) => Fill(profileErrors, errors);

    public void ShowPasswordErrors(IReadOnlyDictionary<string, string> errors) => Fill(passwordErrors, errors);

    private static void Fill(Dictionary<string, TextBlock> blocks, IReadOnlyDictionary<string, string> errors)
    {
        foreach (var (field, block) in blocks)
        {
            block.Text = errors.TryGetValue(field, out string? message) ? message : string.Empty;
        }
    }

    public void ShowMessage(string message, bool isError)
    {
        MessageText.Text = message;
        MessageText.Foreground = (Brush)FindResource(isError ? "Error" : "Success");
        MessageText.Visibility = Visibility.Visible;
    }

    public void ClearPasswords()
    {
        CurrentPasswordBox.Clear();
        NewPasswordBox.Clear();
        RepeatPasswordBox.Clear();
    }
}
