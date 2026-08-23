using System;

namespace FocusFlow.Models
{
    public class Project
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ColorHex { get; set; } = "#3B82F6"; // Default blue
        public string Icon { get; set; } = "Folder";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int TaskCount { get; set; }
        public int CompletedTaskCount { get; set; }

        public int CompletionPercentage => TaskCount > 0 ? (int)Math.Round((double)CompletedTaskCount / TaskCount * 100) : 0;
    }
}
