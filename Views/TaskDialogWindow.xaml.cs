using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FocusFlow.Models;

namespace FocusFlow.Views
{
    public partial class TaskDialogWindow : System.Windows.Window
    {
        public TaskItem TaskItem { get; private set; }
        private ObservableCollection<Subtask> _subtasks = new();

        public TaskDialogWindow(TaskItem task, IEnumerable<Project> projects)
        {
            InitializeComponent();
            TaskItem = task;

            TitleInput.Text = task.Title;
            NotesInput.Text = task.Notes;
            DueDatePicker.SelectedDate = task.DueDate;
            ReminderDatePicker.SelectedDate = task.ReminderTime;
            PomodoroEstimateInput.Text = task.EstimatedPomodoros.ToString();

            ProjectCombo.ItemsSource = projects;
            if (task.ProjectId.HasValue)
            {
                ProjectCombo.SelectedItem = projects.FirstOrDefault(p => p.Id == task.ProjectId.Value);
            }

            PriorityCombo.SelectedIndex = (int)task.Priority;
            RecurrenceCombo.SelectedIndex = (int)task.Recurring;

            foreach (var st in task.Subtasks)
            {
                _subtasks.Add(st);
            }
            SubtasksListControl.ItemsSource = _subtasks;
        }

        private void AddSubtask_Click(object sender, RoutedEventArgs e)
        {
            string title = NewSubtaskInput.Text.Trim();
            if (!string.IsNullOrEmpty(title))
            {
                _subtasks.Add(new Subtask { Title = title, IsCompleted = false });
                NewSubtaskInput.Text = string.Empty;
            }
        }

        private void DeleteSubtask_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.DataContext is Subtask st)
            {
                _subtasks.Remove(st);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleInput.Text))
            {
                System.Windows.MessageBox.Show("Please enter a task title.", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            TaskItem.Title = TitleInput.Text.Trim();
            TaskItem.Notes = NotesInput.Text.Trim();
            TaskItem.DueDate = DueDatePicker.SelectedDate;
            TaskItem.ReminderTime = ReminderDatePicker.SelectedDate;

            if (int.TryParse(PomodoroEstimateInput.Text, out int est))
            {
                TaskItem.EstimatedPomodoros = Math.Max(1, est);
            }

            if (ProjectCombo.SelectedItem is Project proj)
            {
                TaskItem.ProjectId = proj.Id;
                TaskItem.ProjectName = proj.Name;
                TaskItem.ProjectColor = proj.ColorHex;
            }
            else
            {
                TaskItem.ProjectId = null;
                TaskItem.ProjectName = "Inbox";
                TaskItem.ProjectColor = "#64748B";
            }

            if (PriorityCombo.SelectedItem is ComboBoxItem item && int.TryParse(item.Tag?.ToString(), out int pVal))
            {
                TaskItem.Priority = (TaskPriority)pVal;
            }

            if (RecurrenceCombo.SelectedItem is ComboBoxItem recItem && int.TryParse(recItem.Tag?.ToString(), out int rVal))
            {
                TaskItem.Recurring = (RecurringPattern)rVal;
            }

            TaskItem.Subtasks = _subtasks.ToList();

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
