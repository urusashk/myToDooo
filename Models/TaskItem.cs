using System;
using System.Collections.Generic;

namespace FocusFlow.Models
{
    public enum TaskPriority
    {
        Low = 0,
        Medium = 1,
        High = 2
    }

    public enum RecurringPattern
    {
        None = 0,
        Daily = 1,
        Weekly = 2,
        Monthly = 3
    }

    public class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        public DateTime? DueDate { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int EstimatedPomodoros { get; set; } = 1;
        public int CompletedPomodoros { get; set; } = 0;
        public int? ProjectId { get; set; }
        public string ProjectName { get; set; } = "Inbox";
        public string ProjectColor { get; set; } = "#64748B";
        public RecurringPattern Recurring { get; set; } = RecurringPattern.None;
        public DateTime? ReminderTime { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public List<Subtask> Subtasks { get; set; } = new();

        public string PriorityDisplay => Priority.ToString();
        
        public string PriorityColor => Priority switch
        {
            TaskPriority.High => "#EF4444",   // Red
            TaskPriority.Medium => "#F59E0B", // Amber
            TaskPriority.Low => "#10B981",    // Emerald
            _ => "#94A3B8"
        };

        public string DueDateDisplay => DueDate.HasValue
            ? DueDate.Value.Date == DateTime.Today
                ? "Today"
                : DueDate.Value.Date == DateTime.Today.AddDays(1)
                    ? "Tomorrow"
                    : DueDate.Value.ToString("MMM dd, yyyy")
            : "No Due Date";

        public bool IsOverdue => DueDate.HasValue && DueDate.Value.Date < DateTime.Today && !IsCompleted;
    }
}
