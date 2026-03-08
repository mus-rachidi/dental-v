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

public class InventoryViewModel : ViewModelBase, ILoadable
{
    private readonly InventoryService _inventoryService;

    private InventoryItem? _selectedItem;
    private bool _isEditing;
    private bool _isLoading;
    private string _searchQuery = string.Empty;

    private int _editId;
    private string _editName = string.Empty;
    private string _editCategory = string.Empty;
    private int _editQuantity;
    private string _editUnit = "pcs";
    private int _editMinStockLevel;
    private decimal _editUnitPrice;
    private string _editSupplier = string.Empty;
    private string _editNotes = string.Empty;
    private int _restockQuantity;

    public InventoryItem? SelectedItem
    {
        get => _selectedItem;
        set { SetProperty(ref _selectedItem, value); OnPropertyChanged(nameof(HasSelection)); }
    }

    public bool IsEditing { get => _isEditing; set => SetProperty(ref _isEditing, value); }
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public bool HasSelection => SelectedItem != null;
    public string SearchQuery { get => _searchQuery; set { SetProperty(ref _searchQuery, value); _ = SearchAsync(); } }

    public int EditId { get => _editId; set => SetProperty(ref _editId, value); }
    public string EditName { get => _editName; set => SetProperty(ref _editName, value); }
    public string EditCategory { get => _editCategory; set => SetProperty(ref _editCategory, value); }
    public int EditQuantity { get => _editQuantity; set => SetProperty(ref _editQuantity, value); }
    public string EditUnit { get => _editUnit; set => SetProperty(ref _editUnit, value); }
    public int EditMinStockLevel { get => _editMinStockLevel; set => SetProperty(ref _editMinStockLevel, value); }
    public decimal EditUnitPrice { get => _editUnitPrice; set => SetProperty(ref _editUnitPrice, value); }
    public string EditSupplier { get => _editSupplier; set => SetProperty(ref _editSupplier, value); }
    public string EditNotes { get => _editNotes; set => SetProperty(ref _editNotes, value); }
    public int RestockQuantity { get => _restockQuantity; set => SetProperty(ref _restockQuantity, value); }

    public bool HasLowStock => LowStockItems.Count > 0;

    public ObservableCollection<InventoryItem> Items { get; } = new();
    public ObservableCollection<InventoryItem> LowStockItems { get; } = new();
    public ObservableCollection<string> Categories { get; } = new();

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelEditCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand RestockCommand { get; }

    public InventoryViewModel(InventoryService inventoryService)
    {
        _inventoryService = inventoryService;

        AddCommand = new RelayCommand(StartAdd);
        EditCommand = new RelayCommand(StartEdit, () => HasSelection);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => HasSelection);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelEditCommand = new RelayCommand(() => IsEditing = false);
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
        RestockCommand = new AsyncRelayCommand(RestockAsync, () => HasSelection && RestockQuantity > 0);
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var list = await _inventoryService.GetAllAsync();
            Items.Clear();
            foreach (var i in list) Items.Add(i);

            var lowStock = await _inventoryService.GetLowStockAsync();
            LowStockItems.Clear();
            foreach (var i in lowStock) LowStockItems.Add(i);
            OnPropertyChanged(nameof(HasLowStock));

            var cats = await _inventoryService.GetCategoriesAsync();
            Categories.Clear();
            foreach (var c in cats) Categories.Add(c);
        }
        finally { IsLoading = false; }
    }

    private async Task SearchAsync()
    {
        IsLoading = true;
        try
        {
            var list = await _inventoryService.SearchAsync(SearchQuery);
            Items.Clear();
            foreach (var i in list) Items.Add(i);
        }
        finally { IsLoading = false; }
    }

    private void StartAdd()
    {
        EditId = 0;
        EditName = string.Empty;
        EditCategory = string.Empty;
        EditQuantity = 0;
        EditUnit = "pcs";
        EditMinStockLevel = 0;
        EditUnitPrice = 0;
        EditSupplier = string.Empty;
        EditNotes = string.Empty;
        RestockQuantity = 0;
        IsEditing = true;
    }

    private void StartEdit()
    {
        if (SelectedItem == null) return;
        EditId = SelectedItem.Id;
        EditName = SelectedItem.Name;
        EditCategory = SelectedItem.Category ?? string.Empty;
        EditQuantity = SelectedItem.Quantity;
        EditUnit = SelectedItem.Unit ?? "pcs";
        EditMinStockLevel = SelectedItem.MinStockLevel;
        EditUnitPrice = SelectedItem.UnitPrice;
        EditSupplier = SelectedItem.Supplier ?? string.Empty;
        EditNotes = SelectedItem.Notes ?? string.Empty;
        RestockQuantity = 0;
        IsEditing = true;
    }

    private async Task SaveAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(EditName))
            {
                MessageBox.Show("Item name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var item = new InventoryItem
            {
                Id = EditId,
                Name = EditName.Trim(),
                Category = EditCategory.Trim(),
                Quantity = EditQuantity,
                Unit = string.IsNullOrWhiteSpace(EditUnit) ? "pcs" : EditUnit.Trim(),
                MinStockLevel = EditMinStockLevel,
                UnitPrice = EditUnitPrice,
                Supplier = EditSupplier.Trim(),
                Notes = EditNotes.Trim()
            };

            if (EditId == 0)
            {
                await _inventoryService.CreateAsync(item);
                var uid = App.SessionService?.CurrentUser?.Id;
                if (uid.HasValue) AuditService.Log(uid.Value, "CreateInventory", item.Name);
            }
            else
            {
                item.CreatedAt = SelectedItem!.CreatedAt;
                item.LastRestockedDate = SelectedItem.LastRestockedDate;
                await _inventoryService.UpdateAsync(item);
                var uid = App.SessionService?.CurrentUser?.Id;
                if (uid.HasValue) AuditService.Log(uid.Value, "EditInventory", item.Name);
            }

            IsEditing = false;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving item: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task DeleteAsync()
    {
        if (SelectedItem == null) return;
        var result = MessageBox.Show($"Delete {SelectedItem.Name}?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        var name = SelectedItem.Name;
        await _inventoryService.DeleteAsync(SelectedItem.Id);
        var uid = App.SessionService?.CurrentUser?.Id;
        if (uid.HasValue) AuditService.Log(uid.Value, "DeleteInventory", name);
        SelectedItem = null;
        await LoadAsync();
    }

    private async Task RestockAsync()
    {
        if (SelectedItem == null || RestockQuantity <= 0) return;
        await _inventoryService.RestockAsync(SelectedItem.Id, RestockQuantity);
        RestockQuantity = 0;
        await LoadAsync();
        MessageBox.Show("Stock updated.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
