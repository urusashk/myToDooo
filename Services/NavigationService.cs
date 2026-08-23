using System;
using FocusFlow.ViewModels;

namespace FocusFlow.Services
{
    public class NavigationService
    {
        private static NavigationService? _instance;
        public static NavigationService Instance => _instance ??= new NavigationService();

        public event Action<ViewModelBase>? CurrentViewModelChanged;

        private ViewModelBase? _currentViewModel;
        public ViewModelBase? CurrentViewModel
        {
            get => _currentViewModel;
            set
            {
                _currentViewModel = value;
                CurrentViewModelChanged?.Invoke(_currentViewModel!);
            }
        }

        public void NavigateTo<TViewModel>(TViewModel viewModel) where TViewModel : ViewModelBase
        {
            CurrentViewModel = viewModel;
        }
    }
}
