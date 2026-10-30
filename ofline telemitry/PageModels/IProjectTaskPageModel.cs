using CommunityToolkit.Mvvm.Input;
using ofline_telemitry.Models;

namespace ofline_telemitry.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}