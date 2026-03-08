using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ClinicManager.Helpers;
using ClinicManager.Models;
using ClinicManager.Services;

namespace ClinicManager.ViewModels;

public class UsersManagementViewModel : ViewModelBase, ILoadable
{
    private readonly UserService _userService;
    private readonly AuthService _authService;
    private User? _selectedUser;
    private bool _isEditing;
    private string _editUsername = string.Empty;
    private string _editPassword = string.Empty;
    private UserRole _editRole;
    private UserStatus _editStatus;
    private string _statusMessage = string.Empty;

    public ObservableCollection<User> Users { get; } = new();
    public User? SelectedUser
    {
        get => _selectedUser;
        set
        {
            SetProperty(ref _selectedUser, value);
            OnPropertyChanged(nameof(CanEditUsername));
            if (value != null)
                LoadUserForEdit(value);
        }
    }

    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }

    public string EditUsername
    {
        get => _editUsername;
        set => SetProperty(ref _editUsername, value);
    }

    public string EditPassword
    {
        get => _editPassword;
        set => SetProperty(ref _editPassword, value);
    }

    public UserRole EditRole
    {
        get => _editRole;
        set => SetProperty(ref _editRole, value);
    }

    public UserStatus EditStatus
    {
        get => _editStatus;
        set => SetProperty(ref _editStatus, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool CanEditUsername => SelectedUser == null;

    public Array Roles { get; } = Enum.GetValues(typeof(UserRole));
    public Array Statuses { get; } = Enum.GetValues(typeof(UserStatus));

    public ICommand AddUserCommand { get; }
    public ICommand SaveUserCommand { get; }
    public ICommand ResetPasswordCommand { get; }
    public ICommand CancelEditCommand { get; }
    public ICommand RefreshCommand { get; }

    public UsersManagementViewModel(UserService userService, AuthService authService)
    {
        _userService = userService;
        _authService = authService;

        AddUserCommand = new RelayCommand(StartAdd);
        SaveUserCommand = new AsyncRelayCommand(SaveUserAsync);
        ResetPasswordCommand = new AsyncRelayCommand(ResetPasswordAsync, () => SelectedUser != null && IsEditing);
        CancelEditCommand = new RelayCommand(CancelEdit);
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
    }

    public async Task LoadAsync()
    {
        Users.Clear();
        var list = await _userService.GetAllAsync();
        foreach (var u in list)
            Users.Add(u);
    }

    private void StartAdd(object? _)
    {
        SelectedUser = null;
        EditUsername = string.Empty;
        EditPassword = string.Empty;
        EditRole = UserRole.Reception;
        EditStatus = UserStatus.Active;
        IsEditing = true;
        StatusMessage = string.Empty;
        OnPropertyChanged(nameof(CanEditUsername));
    }

    private void LoadUserForEdit(User user)
    {
        EditUsername = user.Username;
        EditPassword = string.Empty;
        EditRole = user.Role;
        EditStatus = user.Status;
        IsEditing = true;
        StatusMessage = string.Empty;
        OnPropertyChanged(nameof(CanEditUsername));
    }

    private void CancelEdit(object? _)
    {
        IsEditing = false;
        SelectedUser = null;
        EditUsername = string.Empty;
        EditPassword = string.Empty;
    }

    private async Task SaveUserAsync()
    {
        if (string.IsNullOrWhiteSpace(EditUsername))
        {
            StatusMessage = "Username is required.";
            return;
        }

        if (SelectedUser == null)
        {
            if (string.IsNullOrEmpty(EditPassword))
            {
                StatusMessage = "Password is required for new users.";
                return;
            }
            var (success, msg) = await _authService.CreateUserAsync(EditUsername, EditPassword, EditRole);
            StatusMessage = msg;
            if (success)
            {
                var currentUser = App.SessionService?.CurrentUser;
                if (currentUser != null)
                    AuditService.Log(currentUser.Id, AuditService.Actions.CreateUser, $"Created user: {EditUsername}");
                IsEditing = false;
                await LoadAsync();
            }
        }
        else
        {
            var (success, msg) = await _authService.ChangeRoleAsync(SelectedUser.Id, EditRole);
            if (!success)
            {
                StatusMessage = msg;
                return;
            }
            var (success2, msg2) = await _authService.SetUserStatusAsync(SelectedUser.Id, EditStatus);
            StatusMessage = success2 ? msg2 : msg;

            if (!string.IsNullOrEmpty(EditPassword))
            {
                var (success3, msg3) = await _authService.ResetPasswordAsync(SelectedUser.Id, EditPassword);
                StatusMessage = success3 ? msg3 : msg;
            }

            var currentUser = App.SessionService?.CurrentUser;
            if (currentUser != null)
                AuditService.Log(currentUser.Id, AuditService.Actions.ChangeRole, $"Updated user: {EditUsername}");

            IsEditing = false;
            await LoadAsync();
        }
    }

    private async Task ResetPasswordAsync()
    {
        if (SelectedUser == null || string.IsNullOrEmpty(EditPassword)) return;

        var validation = AuthService.ValidatePassword(EditPassword);
        if (!validation.Success)
        {
            StatusMessage = validation.Message;
            return;
        }

        var (success, msg) = await _authService.ResetPasswordAsync(SelectedUser.Id, EditPassword);
        StatusMessage = msg;
        if (success)
        {
            var currentUser = App.SessionService?.CurrentUser;
            if (currentUser != null)
                AuditService.Log(currentUser.Id, AuditService.Actions.ResetPassword, $"Reset password for: {SelectedUser.Username}");
            EditPassword = string.Empty;
        }
    }
}
