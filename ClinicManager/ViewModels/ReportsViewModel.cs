using System.Threading.Tasks;
using ClinicManager.Helpers;

namespace ClinicManager.ViewModels;

public class ReportsViewModel : ViewModelBase, ILoadable
{
    public Task LoadAsync()
    {
        return Task.CompletedTask;
    }
}
