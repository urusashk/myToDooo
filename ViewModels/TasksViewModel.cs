using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using FocusFlow.Data;
using FocusFlow.Models;
using FocusFlow.Services;
using FocusFlow.Views;

namespace FocusFlow.ViewModels
{
    public class TasksViewModel : ViewModelBase
    {
        private ObservableCollection<TaskItem> _allTasks = new();
        private ObservableCollection<TaskItem> _filteredTasks = new();
        private ObservableCollection<Project> _projects = new();

        private string _searchQuery = string.Empty;
        private Project? _selectedProjectFilter;
        private string _selectedViewTab = "All"; // All, Today, Upcoming, Completed
        private string _selectedSortOption = "Due Date"; // Due Date, Priority, Title, Created Date
        private TaskPriority? _selectedPriorityFilter = null;

        public ObservableCollection<TaskItem> Tasks
        {
            get => _filteredTasks;
            set => SetProperty(ref _filteredTasks, value);
        }

        public ObservableCollection<Project> Projects
        {
            get => _projects;
            set => SetProperty(ref _projects, value);
        }

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (SetProperty(ref _searchQuery, value))
                {
                    ApplyFilters();
                }
            }
        }

        public Project? SelectedProjectFilter
        {
            get => _selectedProjectFilter;
            set
            {
                if (SetProperty(ref _selectedProjectFilter, value))
                {
                    ApplyFilters();
                }
            }
        }

        public string SelectedViewTab
        {
            get => _selectedViewTab;
            set
            {
                if (SetProperty(ref _selectedViewTab, value))
                {
                    ApplyFilters();
                }
            }
        }

        public string SelectedSortOption
        {
            get => _selectedSortOption;
            set
            {
                if (SetProperty(ref _selectedSortOption, value))
                {
                    ApplyFilters();
                }
            }
        }

        public TaskPriority? SelectedPriorityFilter
        {
            get => _selectedPriorityFilter;
            set
            {
                if (SetProperty(ref _selectedPriorityFilter, value))
                {
                    ApplyFilters();
                }
            }
        }

        public ICommand CreateTaskCommand { get; }
        public ICommand EditTaskCommand { get; }
        public ICommand DeleteTaskCommand { get; }
        public ICommand ToggleTaskCompletionCommand { get; }
        public ICommand ToggleSubtaskCommand { get; }
        public ICommand StartTaskFocusCommand { get; }
        public ICommand SelectTabCommand { get; }

        public TasksViewModel()
        {
            CreateTaskCommand = new RelayCommand(CreateTask);
            EditTaskCommand = new RelayCommand(EditTask);
            DeleteTaskCommand = new RelayCommand(DeleteTask);
            ToggleTaskCompletionCommand = new RelayCommand(ToggleTaskCompletion);
            ToggleSubtaskCommand = new RelayCommand(ToggleSubtask);
            StartTaskFocusCommand = new RelayCommand(StartTaskFocus);
            SelectTabCommand = new RelayCommand(SelectTab);

            LoadTasks();
        }

        public override void OnNavigatedTo()
        {
            LoadTasks();
        }

        public void LoadTasks()
        {
            var db = DatabaseService.Instance;

            // Load Projects dropdown
            var dbProjects = db.GetProjects();
            Projects.Clear();

            // Add Inbox / All Projects placeholder
            Projects.Add(new Project { Id = 0, Name = "All Projects", ColorHex = "#6366F1" });
            foreach (var p in dbProjects)
            {
                Projects.Add(p);
            }
            if (_selectedProjectFilter == null && Projects.Count > 0)
            {
                _selectedProjectFilter = Projects.First();
            }

            // Load all tasks from SQLite
            var tasksFromDb = db.GetTasks();
            _allTasks.Clear();
            foreach (var t in tasksFromDb)
            {
                _allTasks.Add(t);
            }

            ApplyFilters();
        }

        private void ApplyFilters()
        {
            var query = _allTasks.AsEnumerable();

            // View Tab Filter
            var today = DateTime.Today;
            if (SelectedViewTab == "Today")
            {
                query = query.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date == today || t.CreatedAt.Date == today && !t.IsCompleted);
            }
            else if (SelectedViewTab == "Upcoming")
            {
                query = query.Where(t => !t.IsCompleted && t.DueDate.HasValue && t.DueDate.Value.Date > today);
            }
            else if (SelectedViewTab == "Completed")
            {
                query = query.Where(t => t.IsCompleted);
            }

            // Search query filter
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                string searchLower = SearchQuery.Trim().ToLower();
                query = query.Where(t => t.Title.ToLower().Contains(searchLower) || t.Notes.ToLower().Contains(searchLower));
            }

            // Project Filter
            if (SelectedProjectFilter != null && SelectedProjectFilter.Id > 0)
            {
                query = query.Where(t => t.ProjectId == SelectedProjectFilter.Id);
            }

            // Priority Filter
            if (SelectedPriorityFilter.HasValue)
            {
                query = query.Where(t => t.Priority == SelectedPriorityFilter.Value);
            }

            // Sorting
            query = SelectedSortOption switch
            {
                "Priority" => query.OrderByDescending(t => t.Priority).ThenBy(t => t.DueDate),
                "Title" => query.OrderBy(t => t.Title),
                "Created Date" => query.OrderByDescending(t => t.CreatedAt),
                _ => query.OrderBy(t => t.DueDate.HasValue ? 0 : 1).ThenBy(t => t.DueDate).ThenByDescending(t => t.Priority)
            };

            Tasks.Clear();
            foreach (var item in query)
            {
                Tasks.Add(item);
            }
        }

        private void SelectTab(object? parameter)
        {
            if (parameter is string tabName)
            {
                SelectedViewTab = tabName;
            }
        }

        private void CreateTask()
        {
            var newTask = new TaskItem
            {
                DueDate = DateTime.Today,
                ProjectId = (SelectedProjectFilter != null && SelectedProjectFilter.Id > 0) ? SelectedProjectFilter.Id : null
            };

            var window = new TaskDialogWindow(newTask, Projects.Where(p => p.Id > 0))
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (window.ShowDialog() == true)
            {
                DatabaseService.Instance.SaveTask(window.TaskItem);
                LoadTasks();
                MainViewModel.Instance.DashboardViewModel.LoadDashboardData();
            }
        }

        private void EditTask(object? parameter)
        {
            if (parameter is TaskItem task)
            {
                var window = new TaskDialogWindow(task, Projects.Where(p => p.Id > 0))
                {
                    Owner = System.Windows.Application.Current.MainWindow
                };

                if (window.ShowDialog() == true)
                {
                    DatabaseService.Instance.SaveTask(window.TaskItem);
                    LoadTasks();
                    MainViewModel.Instance.DashboardViewModel.LoadDashboardData();
                }
            }
        }

        private void DeleteTask(object? parameter)
        {
            if (parameter is TaskItem task)
            {
                if (System.Windows.MessageBox.Show($"Are you sure you want to delete task '{task.Title}'?", "Confirm Delete", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning) == System.Windows.MessageBoxResult.Yes)
                {
                    DatabaseService.Instance.DeleteTask(task.Id);
                    LoadTasks();
                    MainViewModel.Instance.DashboardViewModel.LoadDashboardData();
                }
            }
        }

        private void ToggleTaskCompletion(object? parameter)
        {
            if (parameter is TaskItem task)
            {
                task.IsCompleted = !task.IsCompleted;
                DatabaseService.Instance.ToggleTaskCompletion(task.Id, task.IsCompleted);
                ApplyFilters();
                MainViewModel.Instance.DashboardViewModel.LoadDashboardData();
            }
        }

        private void ToggleSubtask(object? parameter)
        {
            if (parameter is Subtask subtask)
            {
                DatabaseService.Instance.SaveSubtasks(subtask.TaskId, Tasks.FirstOrDefault(t => t.Id == subtask.TaskId)?.Subtasks ?? new());
            }
        }

        private void StartTaskFocus(object? parameter)
        {
            if (parameter is TaskItem task)
            {
                MainViewModel.Instance.PomodoroViewModel.SelectedTask = task;
                NavigationService.Instance.NavigateTo(MainViewModel.Instance.PomodoroViewModel);
            }
        }
    }
}
