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

public class StaffViewModel : ViewModelBase, ILoadable
{
    private readonly StaffService _staffService;

    private StaffMember? _selectedStaff;
    private bool _isEditing;
    private bool _isLoading;
    private string _searchQuery = string.Empty;

    private int _editId;
    private string _editFullName = string.Empty;
    private StaffRole _editRole = StaffRole.Assistant;
    private string _editPhone = string.Empty;
    private string _editEmail = string.Empty;
    private string _editSpecialization = string.Empty;
    private DateTime _editHireDate = DateTime.Today;
    private StaffStatus _editStatus = StaffStatus.Active;
    private string _editNotes = string.Empty;

    public StaffMember? SelectedStaff
    {
        get => _selectedStaff;
        set { SetProperty(ref _selectedStaff, value); OnPropertyChanged(nameof(HasSelection)); }
    }

    public bool IsEditing { get => _isEditing; set => SetProperty(ref _isEditing, value); }
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public bool HasSelection => SelectedStaff != null;
    public string SearchQuery { get => _searchQuery; set { SetProperty(ref _searchQuery, value); _ = SearchAsync(); } }

    public int EditId { get => _editId; set => SetProperty(ref _editId, value); }
    public string EditFullName { get => _editFullName; set => SetProperty(ref _editFullName, value); }
    public StaffRole EditRole { get => _editRole; set => SetProperty(ref _editRole, value); }
    public string EditPhone { get => _editPhone; set => SetProperty(ref _editPhone, value); }
    public string EditEmail { get => _editEmail; set => SetProperty(ref _editEmail, value); }
    public string EditSpecialization { get => _editSpecialization; set => SetProperty(ref _editSpecialization, value); }
    public DateTime EditHireDate { get => _editHireDate; set => SetProperty(ref _editHireDate, value); }
    public StaffStatus EditStatus { get => _editStatus; set => SetProperty(ref _editStatus, value); }
    public string EditNotes { get => _editNotes; set => SetProperty(ref _editNotes, value); }

    public ObservableCollection<StaffMember> Staff { get; } = new();

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelEditCommand { get; }
    public ICommand RefreshCommand { get; }

    public StaffViewModel(StaffService staffService)
    {
        _staffService = staffService;

        AddCommand = new RelayCommand(StartAdd);
        EditCommand = new RelayCommand(StartEdit, () => HasSelection);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => HasSelection);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelEditCommand = new RelayCommand(() => IsEditing = false);
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var list = await _staffService.GetAllAsync();
            Staff.Clear();
            foreach (var s in list) Staff.Add(s);
        }
        finally { IsLoading = false; }
    }

    private async Task SearchAsync()
    {
        IsLoading = true;
        try
        {
            var list = await _staffService.SearchAsync(SearchQuery);
            Staff.Clear();
            foreach (var s in list) Staff.Add(s);
        }
        finally { IsLoading = false; }
    }

    private void StartAdd()
    {
        EditId = 0;
        EditFullName = string.Empty;
        EditRole = StaffRole.Assistant;
        EditPhone = string.Empty;
        EditEmail = string.Empty;
        EditSpecialization = string.Empty;
        EditHireDate = DateTime.Today;
        EditStatus = StaffStatus.Active;
        EditNotes = string.Empty;
        IsEditing = true;
    }

    private void StartEdit()
    {
        if (SelectedStaff == null) return;
        EditId = SelectedStaff.Id;
        EditFullName = SelectedStaff.FullName;
        EditRole = SelectedStaff.Role;
        EditPhone = SelectedStaff.Phone ?? string.Empty;
        EditEmail = SelectedStaff.Email ?? string.Empty;
        EditSpecialization = SelectedStaff.Specialization ?? string.Empty;
        EditHireDate = SelectedStaff.HireDate;
        EditStatus = SelectedStaff.Status;
        EditNotes = SelectedStaff.Notes ?? string.Empty;
        IsEditing = true;
    }

    private async Task SaveAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(EditFullName))
            {
                MessageBox.Show("Full name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var staff = new StaffMember
            {
                Id = EditId,
                FullName = EditFullName.Trim(),
                Role = EditRole,
                Phone = EditPhone.Trim(),
                Email = EditEmail.Trim(),
                Specialization = EditSpecialization.Trim(),
                HireDate = EditHireDate,
                Status = EditStatus,
                Notes = EditNotes.Trim()
            };

            if (EditId == 0)
            {
                await _staffService.CreateAsync(staff);
                var uid = App.SessionService?.CurrentUser?.Id;
                if (uid.HasValue) AuditService.Log(uid.Value, "CreateStaff", staff.FullName);
            }
            else
            {
                staff.CreatedAt = SelectedStaff!.CreatedAt;
                await _staffService.UpdateAsync(staff);
                var uid = App.SessionService?.CurrentUser?.Id;
                if (uid.HasValue) AuditService.Log(uid.Value, "EditStaff", staff.FullName);
            }

            IsEditing = false;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving staff: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task DeleteAsync()
    {
        if (SelectedStaff == null) return;
        var result = MessageBox.Show($"Delete {SelectedStaff.FullName}?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        var name = SelectedStaff.FullName;
        await _staffService.DeleteAsync(SelectedStaff.Id);
        var uid = App.SessionService?.CurrentUser?.Id;
        if (uid.HasValue) AuditService.Log(uid.Value, "DeleteStaff", name);
        SelectedStaff = null;
        await LoadAsync();
    }
}
