using System.Threading.Tasks;
using ClinicManager.Helpers;

namespace ClinicManager.ViewModels;

public class StaffViewModel : ViewModelBase, ILoadable
{
    public Task LoadAsync()
    {
        return Task.CompletedTask;
    }
}
