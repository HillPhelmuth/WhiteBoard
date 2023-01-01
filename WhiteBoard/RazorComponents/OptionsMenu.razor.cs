using Blazor.ModalDialog;
using Board.Client.Services;
using Microsoft.AspNetCore.Components;

namespace Board.Client.RazorComponents
{
    public partial class OptionsMenu
    {
        private string _textInput;
        private string _selectOption;
        private int selectedWidth = 3;
        [Inject] private AppState AppState { get; set; } = default!;
        [Inject] private IModalDialogService ModalDialogService { get; set; } = default!;
        private void ChangeColor(ChangeEventArgs e) => AppState.Color = e.Value?.ToString() ?? "black";
        private void ChangeText(ChangeEventArgs e) => AppState.Text = _textInput;
        private void ChangeSelectOption(ChangeEventArgs e) => AppState.DblClkOption = e.Value?.ToString() ?? "Text";
        private void ChangeWidth(ChangeEventArgs e) => AppState.MarkerWidth = selectedWidth;

        private void Close()
        {
            ModalDialogService.Close(true);
        }

    }
}
