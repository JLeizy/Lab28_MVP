using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace AmfibiaShop.Views.Auth;

/// <summary>ВИД регистрации: поля формы и ошибки под ними.</summary>
public partial class RegisterView : UserControl, IRegisterView
{
    private readonly Dictionary<string, TextBlock> errorBlocks;

    public RegisterView()
    {
        InitializeComponent();
        errorBlocks = new Dictionary<string, TextBlock>
        {
            ["FirstName"] = FirstNameError, ["LastName"] = LastNameError, ["Email"] = EmailError, ["Phone"] = PhoneError,
            ["Password"] = PasswordError, ["PasswordRepeat"] = PasswordRepeatError, ["Agreed"] = AgreedError
        };
    }

    public event EventHandler? RegisterRequested;
    public event EventHandler? LoginRequested;

    public string FirstName => FirstNameBox.Text;
    public string LastName => LastNameBox.Text;
    public string Email => EmailBox.Text;
    public string Phone => PhoneBox.Text;
    public string Password => PasswordBox1.Password;
    public string PasswordRepeat => PasswordBox2.Password;
    public bool Agreed => AgreeBox.IsChecked == true;

    private void CreateButton_Click(object sender, RoutedEventArgs e) => RegisterRequested?.Invoke(this, EventArgs.Empty);

    private void LoginLink_Click(object sender, RoutedEventArgs e) => LoginRequested?.Invoke(this, EventArgs.Empty);

    public void ShowFieldErrors(IReadOnlyDictionary<string, string> errors)
    {
        foreach (var (field, block) in errorBlocks)
        {
            block.Text = errors.TryGetValue(field, out string? message) ? message : string.Empty;
        }

        MessageBlock.Text = string.Empty;
    }

    public void ShowError(string message) => MessageBlock.Text = message;
}
