using System.Collections.ObjectModel;
using System.Windows.Input;
using FocusFlow.Data;
using FocusFlow.Models;
using FocusFlow.Services;

namespace FocusFlow.ViewModels
{
    public class ProjectsViewModel : ViewModelBase
    {
        private ObservableCollection<Project> _projects = new();
        private Project? _selectedProject;
        private string _newProjectName = string.Empty;
        private string _newProjectDescription = string.Empty;
        private string _newProjectColor = "#3B82F6";

        public ObservableCollection<Project> Projects
        {
            get => _projects;
            set => SetProperty(ref _projects, value);
        }

        public Project? SelectedProject
        {
            get => _selectedProject;
            set => SetProperty(ref _selectedProject, value);
        }

        public string NewProjectName
        {
            get => _newProjectName;
            set => SetProperty(ref _newProjectName, value);
        }

        public string NewProjectDescription
        {
            get => _newProjectDescription;
            set => SetProperty(ref _newProjectDescription, value);
        }

        public string NewProjectColor
        {
            get => _newProjectColor;
            set => SetProperty(ref _newProjectColor, value);
        }

        public ICommand CreateProjectCommand { get; }
        public ICommand EditProjectCommand { get; }
        public ICommand DeleteProjectCommand { get; }
        public ICommand ViewProjectTasksCommand { get; }

        public ProjectsViewModel()
        {
            CreateProjectCommand = new RelayCommand(CreateProject);
            EditProjectCommand = new RelayCommand(EditProject);
            DeleteProjectCommand = new RelayCommand(DeleteProject);
            ViewProjectTasksCommand = new RelayCommand(ViewProjectTasks);

            LoadProjects();
        }

        public override void OnNavigatedTo()
        {
            LoadProjects();
        }

        public void LoadProjects()
        {
            var dbProjects = DatabaseService.Instance.GetProjects();
            Projects.Clear();
            foreach (var p in dbProjects)
            {
                Projects.Add(p);
            }
        }

        private void CreateProject()
        {
            if (string.IsNullOrWhiteSpace(NewProjectName)) return;

            var project = new Project
            {
                Name = NewProjectName.Trim(),
                Description = NewProjectDescription.Trim(),
                ColorHex = string.IsNullOrWhiteSpace(NewProjectColor) ? "#3B82F6" : NewProjectColor.Trim(),
                Icon = "Folder"
            };

            DatabaseService.Instance.SaveProject(project);
            NewProjectName = string.Empty;
            NewProjectDescription = string.Empty;
            LoadProjects();
            MainViewModel.Instance.TasksViewModel.LoadTasks();
        }

        private void EditProject(object? parameter)
        {
            if (parameter is Project p)
            {
                string newName = Microsoft.VisualBasic.Interaction.InputBox("Enter new project name:", "Rename Project", p.Name);
                if (!string.IsNullOrWhiteSpace(newName))
                {
                    p.Name = newName.Trim();
                    DatabaseService.Instance.SaveProject(p);
                    LoadProjects();
                    MainViewModel.Instance.TasksViewModel.LoadTasks();
                }
            }
        }

        private void DeleteProject(object? parameter)
        {
            if (parameter is Project p)
            {
                if (System.Windows.MessageBox.Show($"Delete project '{p.Name}'? Tasks in this project will move to Inbox.", "Confirm Delete", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question) == System.Windows.MessageBoxResult.Yes)
                {
                    DatabaseService.Instance.DeleteProject(p.Id);
                    LoadProjects();
                    MainViewModel.Instance.TasksViewModel.LoadTasks();
                }
            }
        }

        private void ViewProjectTasks(object? parameter)
        {
            if (parameter is Project p)
            {
                MainViewModel.Instance.TasksViewModel.SelectedProjectFilter = p;
                NavigationService.Instance.NavigateTo(MainViewModel.Instance.TasksViewModel);
            }
        }
    }
}
