using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using FocusFlow.Models;

namespace FocusFlow.Data
{
    public class DatabaseService
    {
        private static DatabaseService? _instance;
        public static DatabaseService Instance => _instance ??= new DatabaseService();

        private readonly string _dbPath;

        public DatabaseService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folder = Path.Combine(appData, "FocusFlow");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            _dbPath = Path.Combine(folder, "focusflow.db");
            InitializeDatabase();
        }

        private SqliteConnection GetConnection()
        {
            var conn = new SqliteConnection($"Data Source={_dbPath}");
            conn.Open();
            return conn;
        }

        private void InitializeDatabase()
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Projects (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Description TEXT,
                    ColorHex TEXT NOT NULL DEFAULT '#3B82F6',
                    Icon TEXT NOT NULL DEFAULT 'Folder',
                    CreatedAt TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Tasks (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    Notes TEXT,
                    Priority INTEGER NOT NULL DEFAULT 1,
                    DueDate TEXT,
                    IsCompleted INTEGER NOT NULL DEFAULT 0,
                    CompletedAt TEXT,
                    EstimatedPomodoros INTEGER NOT NULL DEFAULT 1,
                    CompletedPomodoros INTEGER NOT NULL DEFAULT 0,
                    ProjectId INTEGER,
                    Recurring INTEGER NOT NULL DEFAULT 0,
                    ReminderTime TEXT,
                    CreatedAt TEXT NOT NULL,
                    FOREIGN KEY(ProjectId) REFERENCES Projects(Id) ON DELETE SET NULL
                );

                CREATE TABLE IF NOT EXISTS Subtasks (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TaskId INTEGER NOT NULL,
                    Title TEXT NOT NULL,
                    IsCompleted INTEGER NOT NULL DEFAULT 0,
                    FOREIGN KEY(TaskId) REFERENCES Tasks(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS PomodoroSessions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TaskId INTEGER,
                    Type INTEGER NOT NULL DEFAULT 0,
                    DurationMinutes INTEGER NOT NULL DEFAULT 25,
                    StartedAt TEXT NOT NULL,
                    CompletedAt TEXT NOT NULL,
                    IsSuccessful INTEGER NOT NULL DEFAULT 1,
                    FOREIGN KEY(TaskId) REFERENCES Tasks(Id) ON DELETE SET NULL
                );

                CREATE TABLE IF NOT EXISTS Reminders (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TaskId INTEGER NOT NULL,
                    ReminderDateTime TEXT NOT NULL,
                    Message TEXT,
                    IsTriggered INTEGER NOT NULL DEFAULT 0,
                    FOREIGN KEY(TaskId) REFERENCES Tasks(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS Settings (
                    Id INTEGER PRIMARY KEY CHECK (Id = 1),
                    FocusDurationMinutes INTEGER NOT NULL DEFAULT 25,
                    ShortBreakMinutes INTEGER NOT NULL DEFAULT 5,
                    LongBreakMinutes INTEGER NOT NULL DEFAULT 15,
                    LongBreakInterval INTEGER NOT NULL DEFAULT 4,
                    SoundEnabled INTEGER NOT NULL DEFAULT 1,
                    NotificationsEnabled INTEGER NOT NULL DEFAULT 1,
                    LaunchAtStartup INTEGER NOT NULL DEFAULT 0,
                    AutoStartBreaks INTEGER NOT NULL DEFAULT 0,
                    AutoStartPomodoros INTEGER NOT NULL DEFAULT 0,
                    Theme TEXT NOT NULL DEFAULT 'Dark'
                );
            ";
            cmd.ExecuteNonQuery();

            // Ensure default settings exist
            cmd.CommandText = "INSERT OR IGNORE INTO Settings (Id, FocusDurationMinutes, ShortBreakMinutes, LongBreakMinutes, LongBreakInterval, SoundEnabled, NotificationsEnabled, LaunchAtStartup, AutoStartBreaks, AutoStartPomodoros, Theme) VALUES (1, 25, 5, 15, 4, 1, 1, 0, 0, 0, 'Dark');";
            cmd.ExecuteNonQuery();

            // Seed default projects if none exist
            cmd.CommandText = "SELECT COUNT(*) FROM Projects;";
            long projectCount = (long)(cmd.ExecuteScalar() ?? 0);
            if (projectCount == 0)
            {
                cmd.CommandText = @"
                    INSERT INTO Projects (Name, Description, ColorHex, Icon, CreatedAt) VALUES 
                    ('Personal', 'Personal tasks and goals', '#3B82F6', 'User', @now),
                    ('Work', 'Professional projects & deliverables', '#8B5CF6', 'Briefcase', @now),
                    ('Study', 'Learning, reading, and courses', '#10B981', 'Book', @now),
                    ('Fitness', 'Health, workouts, and wellness', '#EF4444', 'Heart', @now);
                ";
                cmd.Parameters.AddWithValue("@now", DateTime.Now.ToString("o"));
                cmd.ExecuteNonQuery();

                // Seed starter tasks
                cmd.CommandText = @"
                    INSERT INTO Tasks (Title, Notes, Priority, DueDate, IsCompleted, EstimatedPomodoros, CompletedPomodoros, ProjectId, CreatedAt) VALUES 
                    ('Welcome to FocusFlow!', 'Explore sidebar, set up custom project colors, and launch your first Pomodoro timer session.', 2, @today, 0, 1, 0, 1, @now),
                    ('Set up weekly focus targets', 'Review Settings to configure your preferred Pomodoro durations.', 1, @tomorrow, 0, 2, 0, 2, @now),
                    ('Daily 30-min Reading', 'Read chapters 1 & 2 of deep work strategy.', 0, @today, 1, 1, 1, 3, @now);
                ";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@today", DateTime.Today.ToString("o"));
                cmd.Parameters.AddWithValue("@tomorrow", DateTime.Today.AddDays(1).ToString("o"));
                cmd.Parameters.AddWithValue("@now", DateTime.Now.ToString("o"));
                cmd.ExecuteNonQuery();
            }
        }

        #region Projects CRUD
        public List<Project> GetProjects()
        {
            var list = new List<Project>();
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT p.Id, p.Name, p.Description, p.ColorHex, p.Icon, p.CreatedAt,
                       COUNT(t.Id) AS TaskCount,
                       SUM(CASE WHEN t.IsCompleted = 1 THEN 1 ELSE 0 END) AS CompletedTaskCount
                FROM Projects p
                LEFT JOIN Tasks t ON p.Id = t.ProjectId
                GROUP BY p.Id, p.Name, p.Description, p.ColorHex, p.Icon, p.CreatedAt
                ORDER BY p.Name;
            ";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Project
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    ColorHex = reader.GetString(3),
                    Icon = reader.GetString(4),
                    CreatedAt = DateTime.Parse(reader.GetString(5)),
                    TaskCount = reader.GetInt32(6),
                    CompletedTaskCount = reader.IsDBNull(7) ? 0 : reader.GetInt32(7)
                });
            }
            return list;
        }

        public int SaveProject(Project project)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            if (project.Id == 0)
            {
                cmd.CommandText = @"
                    INSERT INTO Projects (Name, Description, ColorHex, Icon, CreatedAt)
                    VALUES (@Name, @Description, @ColorHex, @Icon, @CreatedAt);
                    SELECT last_insert_rowid();
                ";
            }
            else
            {
                cmd.CommandText = @"
                    UPDATE Projects 
                    SET Name = @Name, Description = @Description, ColorHex = @ColorHex, Icon = @Icon
                    WHERE Id = @Id;
                ";
                cmd.Parameters.AddWithValue("@Id", project.Id);
            }

            cmd.Parameters.AddWithValue("@Name", project.Name);
            cmd.Parameters.AddWithValue("@Description", project.Description ?? "");
            cmd.Parameters.AddWithValue("@ColorHex", project.ColorHex);
            cmd.Parameters.AddWithValue("@Icon", project.Icon);
            cmd.Parameters.AddWithValue("@CreatedAt", project.CreatedAt.ToString("o"));

            if (project.Id == 0)
            {
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
            cmd.ExecuteNonQuery();
            return project.Id;
        }

        public void DeleteProject(int projectId)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Projects WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", projectId);
            cmd.ExecuteNonQuery();
        }
        #endregion

        #region Tasks CRUD
        public List<TaskItem> GetTasks(int? projectId = null, bool? isCompleted = null, DateTime? dueDate = null)
        {
            var list = new List<TaskItem>();
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();

            string sql = @"
                SELECT t.Id, t.Title, t.Notes, t.Priority, t.DueDate, t.IsCompleted, t.CompletedAt,
                       t.EstimatedPomodoros, t.CompletedPomodoros, t.ProjectId, t.Recurring, t.ReminderTime, t.CreatedAt,
                       COALESCE(p.Name, 'Inbox') AS ProjectName,
                       COALESCE(p.ColorHex, '#64748B') AS ProjectColor
                FROM Tasks t
                LEFT JOIN Projects p ON t.ProjectId = p.Id
                WHERE 1=1
            ";

            if (projectId.HasValue)
            {
                sql += " AND t.ProjectId = @ProjectId";
                cmd.Parameters.AddWithValue("@ProjectId", projectId.Value);
            }
            if (isCompleted.HasValue)
            {
                sql += " AND t.IsCompleted = @IsCompleted";
                cmd.Parameters.AddWithValue("@IsCompleted", isCompleted.Value ? 1 : 0);
            }
            if (dueDate.HasValue)
            {
                sql += " AND date(t.DueDate) = date(@DueDate)";
                cmd.Parameters.AddWithValue("@DueDate", dueDate.Value.ToString("yyyy-MM-dd"));
            }

            sql += " ORDER BY t.IsCompleted ASC, t.Priority DESC, t.DueDate ASC, t.Id DESC;";
            cmd.CommandText = sql;

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var task = new TaskItem
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Notes = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Priority = (TaskPriority)reader.GetInt32(3),
                    DueDate = reader.IsDBNull(4) ? null : DateTime.Parse(reader.GetString(4)),
                    IsCompleted = reader.GetInt32(5) == 1,
                    CompletedAt = reader.IsDBNull(6) ? null : DateTime.Parse(reader.GetString(6)),
                    EstimatedPomodoros = reader.GetInt32(7),
                    CompletedPomodoros = reader.GetInt32(8),
                    ProjectId = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                    Recurring = (RecurringPattern)reader.GetInt32(10),
                    ReminderTime = reader.IsDBNull(11) ? null : DateTime.Parse(reader.GetString(11)),
                    CreatedAt = DateTime.Parse(reader.GetString(12)),
                    ProjectName = reader.GetString(13),
                    ProjectColor = reader.GetString(14)
                };
                list.Add(task);
            }

            // Load subtasks for each task
            foreach (var task in list)
            {
                task.Subtasks = GetSubtasks(task.Id);
            }

            return list;
        }

        public int SaveTask(TaskItem task)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();

            if (task.Id == 0)
            {
                cmd.CommandText = @"
                    INSERT INTO Tasks (Title, Notes, Priority, DueDate, IsCompleted, CompletedAt, EstimatedPomodoros, CompletedPomodoros, ProjectId, Recurring, ReminderTime, CreatedAt)
                    VALUES (@Title, @Notes, @Priority, @DueDate, @IsCompleted, @CompletedAt, @EstimatedPomodoros, @CompletedPomodoros, @ProjectId, @Recurring, @ReminderTime, @CreatedAt);
                    SELECT last_insert_rowid();
                ";
            }
            else
            {
                cmd.CommandText = @"
                    UPDATE Tasks
                    SET Title = @Title, Notes = @Notes, Priority = @Priority, DueDate = @DueDate,
                        IsCompleted = @IsCompleted, CompletedAt = @CompletedAt, EstimatedPomodoros = @EstimatedPomodoros,
                        CompletedPomodoros = @CompletedPomodoros, ProjectId = @ProjectId, Recurring = @Recurring, ReminderTime = @ReminderTime
                    WHERE Id = @Id;
                ";
                cmd.Parameters.AddWithValue("@Id", task.Id);
            }

            cmd.Parameters.AddWithValue("@Title", task.Title);
            cmd.Parameters.AddWithValue("@Notes", task.Notes ?? "");
            cmd.Parameters.AddWithValue("@Priority", (int)task.Priority);
            cmd.Parameters.AddWithValue("@DueDate", task.DueDate.HasValue ? task.DueDate.Value.ToString("o") : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@IsCompleted", task.IsCompleted ? 1 : 0);
            cmd.Parameters.AddWithValue("@CompletedAt", task.CompletedAt.HasValue ? task.CompletedAt.Value.ToString("o") : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@EstimatedPomodoros", task.EstimatedPomodoros);
            cmd.Parameters.AddWithValue("@CompletedPomodoros", task.CompletedPomodoros);
            cmd.Parameters.AddWithValue("@ProjectId", task.ProjectId.HasValue ? task.ProjectId.Value : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Recurring", (int)task.Recurring);
            cmd.Parameters.AddWithValue("@ReminderTime", task.ReminderTime.HasValue ? task.ReminderTime.Value.ToString("o") : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedAt", task.CreatedAt.ToString("o"));

            int taskId = task.Id;
            if (task.Id == 0)
            {
                taskId = Convert.ToInt32(cmd.ExecuteScalar());
                task.Id = taskId;
            }
            else
            {
                cmd.ExecuteNonQuery();
            }

            // Save Subtasks
            SaveSubtasks(taskId, task.Subtasks);

            // Sync Task Reminder
            SyncTaskReminder(taskId, task.Title, task.ReminderTime, task.Recurring);

            return taskId;
        }

        public void IncrementCompletedPomodoros(int taskId)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Tasks SET CompletedPomodoros = CompletedPomodoros + 1 WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", taskId);
            cmd.ExecuteNonQuery();
        }

        public void ToggleTaskCompletion(int taskId, bool isCompleted)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Tasks SET IsCompleted = @IsCompleted, CompletedAt = @CompletedAt WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", taskId);
            cmd.Parameters.AddWithValue("@IsCompleted", isCompleted ? 1 : 0);
            cmd.Parameters.AddWithValue("@CompletedAt", isCompleted ? DateTime.Now.ToString("o") : (object)DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        public void DeleteTask(int taskId)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Tasks WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", taskId);
            cmd.ExecuteNonQuery();
        }
        #endregion

        #region Subtasks
        public List<Subtask> GetSubtasks(int taskId)
        {
            var list = new List<Subtask>();
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, TaskId, Title, IsCompleted FROM Subtasks WHERE TaskId = @TaskId ORDER BY Id;";
            cmd.Parameters.AddWithValue("@TaskId", taskId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Subtask
                {
                    Id = reader.GetInt32(0),
                    TaskId = reader.GetInt32(1),
                    Title = reader.GetString(2),
                    IsCompleted = reader.GetInt32(3) == 1
                });
            }
            return list;
        }

        public void SaveSubtasks(int taskId, List<Subtask> subtasks)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();

            // Clear existing subtasks and re-insert
            cmd.CommandText = "DELETE FROM Subtasks WHERE TaskId = @TaskId;";
            cmd.Parameters.AddWithValue("@TaskId", taskId);
            cmd.ExecuteNonQuery();

            foreach (var st in subtasks)
            {
                using var insertCmd = conn.CreateCommand();
                insertCmd.CommandText = "INSERT INTO Subtasks (TaskId, Title, IsCompleted) VALUES (@TaskId, @Title, @IsCompleted);";
                insertCmd.Parameters.AddWithValue("@TaskId", taskId);
                insertCmd.Parameters.AddWithValue("@Title", st.Title);
                insertCmd.Parameters.AddWithValue("@IsCompleted", st.IsCompleted ? 1 : 0);
                insertCmd.ExecuteNonQuery();
            }
        }
        #endregion

        #region Reminders CRUD
        public List<Reminder> GetPendingReminders()
        {
            var list = new List<Reminder>();
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT r.Id, r.TaskId, r.ReminderDateTime, r.Message, r.IsTriggered,
                       COALESCE(t.Title, 'Task') AS TaskTitle,
                       COALESCE(t.Recurring, 0) AS Recurrence
                FROM Reminders r
                LEFT JOIN Tasks t ON r.TaskId = t.Id
                WHERE r.IsTriggered = 0 AND r.ReminderDateTime <= @Now;
            ";
            cmd.Parameters.AddWithValue("@Now", DateTime.Now.ToString("o"));

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Reminder
                {
                    Id = reader.GetInt32(0),
                    TaskId = reader.GetInt32(1),
                    ReminderDateTime = DateTime.Parse(reader.GetString(2)),
                    Message = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    IsTriggered = reader.GetInt32(4) == 1,
                    TaskTitle = reader.GetString(5),
                    Recurrence = (RecurringPattern)reader.GetInt32(6)
                });
            }
            return list;
        }

        public void SaveReminder(Reminder reminder)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            if (reminder.Id == 0)
            {
                cmd.CommandText = @"
                    INSERT INTO Reminders (TaskId, ReminderDateTime, Message, IsTriggered)
                    VALUES (@TaskId, @ReminderDateTime, @Message, @IsTriggered);
                ";
            }
            else
            {
                cmd.CommandText = @"
                    UPDATE Reminders
                    SET TaskId = @TaskId, ReminderDateTime = @ReminderDateTime, Message = @Message, IsTriggered = @IsTriggered
                    WHERE Id = @Id;
                ";
                cmd.Parameters.AddWithValue("@Id", reminder.Id);
            }

            cmd.Parameters.AddWithValue("@TaskId", reminder.TaskId);
            cmd.Parameters.AddWithValue("@ReminderDateTime", reminder.ReminderDateTime.ToString("o"));
            cmd.Parameters.AddWithValue("@Message", reminder.Message ?? "");
            cmd.Parameters.AddWithValue("@IsTriggered", reminder.IsTriggered ? 1 : 0);
            cmd.ExecuteNonQuery();
        }

        public void MarkReminderTriggered(int reminderId)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Reminders SET IsTriggered = 1 WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", reminderId);
            cmd.ExecuteNonQuery();
        }

        public void UpdateReminderDateTime(int reminderId, DateTime newDateTime)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Reminders SET ReminderDateTime = @ReminderDateTime, IsTriggered = 0 WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", reminderId);
            cmd.Parameters.AddWithValue("@ReminderDateTime", newDateTime.ToString("o"));
            cmd.ExecuteNonQuery();
        }

        public void DismissHistoricalReminders()
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Reminders SET IsTriggered = 1 WHERE IsTriggered = 0 AND ReminderDateTime <= @Now;";
            cmd.Parameters.AddWithValue("@Now", DateTime.Now.ToString("o"));
            cmd.ExecuteNonQuery();
        }

        public void SyncTaskReminder(int taskId, string taskTitle, DateTime? reminderTime, RecurringPattern recurrence)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();

            if (!reminderTime.HasValue)
            {
                cmd.CommandText = "DELETE FROM Reminders WHERE TaskId = @TaskId;";
                cmd.Parameters.AddWithValue("@TaskId", taskId);
                cmd.ExecuteNonQuery();
                return;
            }

            cmd.CommandText = "SELECT Id FROM Reminders WHERE TaskId = @TaskId LIMIT 1;";
            cmd.Parameters.AddWithValue("@TaskId", taskId);
            var existingId = cmd.ExecuteScalar();

            if (existingId != null && existingId != DBNull.Value)
            {
                cmd.CommandText = "UPDATE Reminders SET ReminderDateTime = @ReminderDateTime, Message = @Message, IsTriggered = 0 WHERE TaskId = @TaskId;";
            }
            else
            {
                cmd.CommandText = "INSERT INTO Reminders (TaskId, ReminderDateTime, Message, IsTriggered) VALUES (@TaskId, @ReminderDateTime, @Message, 0);";
            }

            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@TaskId", taskId);
            cmd.Parameters.AddWithValue("@ReminderDateTime", reminderTime.Value.ToString("o"));
            cmd.Parameters.AddWithValue("@Message", $"Task Reminder: {taskTitle}");
            cmd.ExecuteNonQuery();
        }
        #endregion

        #region Pomodoro Sessions
        public void RecordPomodoroSession(PomodoroSession session)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO PomodoroSessions (TaskId, Type, DurationMinutes, StartedAt, CompletedAt, IsSuccessful)
                VALUES (@TaskId, @Type, @DurationMinutes, @StartedAt, @CompletedAt, @IsSuccessful);
            ";
            cmd.Parameters.AddWithValue("@TaskId", session.TaskId.HasValue ? session.TaskId.Value : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Type", (int)session.Type);
            cmd.Parameters.AddWithValue("@DurationMinutes", session.DurationMinutes);
            cmd.Parameters.AddWithValue("@StartedAt", session.StartedAt.ToString("o"));
            cmd.Parameters.AddWithValue("@CompletedAt", session.CompletedAt.ToString("o"));
            cmd.Parameters.AddWithValue("@IsSuccessful", session.IsSuccessful ? 1 : 0);
            cmd.ExecuteNonQuery();
        }

        public List<PomodoroSession> GetRecentSessions(int limit = 50)
        {
            var list = new List<PomodoroSession>();
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT s.Id, s.TaskId, s.Type, s.DurationMinutes, s.StartedAt, s.CompletedAt, s.IsSuccessful,
                       COALESCE(t.Title, 'General Focus') AS TaskTitle
                FROM PomodoroSessions s
                LEFT JOIN Tasks t ON s.TaskId = t.Id
                ORDER BY s.StartedAt DESC
                LIMIT @Limit;
            ";
            cmd.Parameters.AddWithValue("@Limit", limit);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new PomodoroSession
                {
                    Id = reader.GetInt32(0),
                    TaskId = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                    Type = (SessionType)reader.GetInt32(2),
                    DurationMinutes = reader.GetInt32(3),
                    StartedAt = DateTime.Parse(reader.GetString(4)),
                    CompletedAt = DateTime.Parse(reader.GetString(5)),
                    IsSuccessful = reader.GetInt32(6) == 1,
                    TaskTitle = reader.GetString(7)
                });
            }
            return list;
        }

        public (int TodayCount, int TodayFocusMinutes, int TotalCount, int TotalFocusMinutes) GetFocusStats()
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            
            string todayStr = DateTime.Today.ToString("yyyy-MM-dd");
            cmd.CommandText = @"
                SELECT 
                    SUM(CASE WHEN date(StartedAt) = date(@Today) AND Type = 0 THEN 1 ELSE 0 END) AS TodayCount,
                    SUM(CASE WHEN date(StartedAt) = date(@Today) AND Type = 0 THEN DurationMinutes ELSE 0 END) AS TodayMinutes,
                    SUM(CASE WHEN Type = 0 THEN 1 ELSE 0 END) AS TotalCount,
                    SUM(CASE WHEN Type = 0 THEN DurationMinutes ELSE 0 END) AS TotalMinutes
                FROM PomodoroSessions;
            ";
            cmd.Parameters.AddWithValue("@Today", todayStr);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                int todayCount = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader.GetValue(0));
                int todayMinutes = reader.IsDBNull(1) ? 0 : Convert.ToInt32(reader.GetValue(1));
                int totalCount = reader.IsDBNull(2) ? 0 : Convert.ToInt32(reader.GetValue(2));
                int totalMinutes = reader.IsDBNull(3) ? 0 : Convert.ToInt32(reader.GetValue(3));
                return (todayCount, todayMinutes, totalCount, totalMinutes);
            }
            return (0, 0, 0, 0);
        }
        #endregion

        #region Advanced Analytics
        public ReportOverview GetReportOverview(DateTime? startDate = null, DateTime? endDate = null)
        {
            var overview = new ReportOverview();
            using var conn = GetConnection();

            // 1. Focus Minutes & Pomodoros in Date Range
            using (var cmd = conn.CreateCommand())
            {
                string sql = @"
                    SELECT 
                        COALESCE(SUM(CASE WHEN Type = 0 THEN DurationMinutes ELSE 0 END), 0) AS RangeFocusMinutes,
                        COALESCE(SUM(CASE WHEN Type = 0 THEN 1 ELSE 0 END), 0) AS RangePomodoros,
                        COALESCE(SUM(CASE WHEN Type = 0 AND date(StartedAt) = date('now') THEN DurationMinutes ELSE 0 END), 0) AS TodayFocusMinutes,
                        COALESCE(SUM(CASE WHEN Type = 0 AND date(StartedAt) >= date('now', '-7 days') THEN DurationMinutes ELSE 0 END), 0) AS WeeklyFocusMinutes
                    FROM PomodoroSessions
                    WHERE 1=1
                ";

                if (startDate.HasValue)
                {
                    sql += " AND date(StartedAt) >= date(@StartDate)";
                    cmd.Parameters.AddWithValue("@StartDate", startDate.Value.ToString("yyyy-MM-dd"));
                }
                if (endDate.HasValue)
                {
                    sql += " AND date(StartedAt) <= date(@EndDate)";
                    cmd.Parameters.AddWithValue("@EndDate", endDate.Value.ToString("yyyy-MM-dd"));
                }

                cmd.CommandText = sql;
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    overview.TotalFocusMinutes = reader.GetInt32(0);
                    overview.TotalPomodoros = reader.GetInt32(1);
                    overview.TodayFocusMinutes = reader.GetInt32(2);
                    overview.WeeklyFocusMinutes = reader.GetInt32(3);
                }
            }

            // 2. Task Completion Stats in Date Range
            using (var cmd = conn.CreateCommand())
            {
                string sql = @"
                    SELECT 
                        COALESCE(SUM(CASE WHEN IsCompleted = 1 THEN 1 ELSE 0 END), 0) AS CompletedCount,
                        COALESCE(SUM(CASE WHEN IsCompleted = 0 THEN 1 ELSE 0 END), 0) AS PendingCount,
                        COALESCE(SUM(CASE WHEN IsCompleted = 0 AND DueDate IS NOT NULL AND date(DueDate) < date('now') THEN 1 ELSE 0 END), 0) AS OverdueCount
                    FROM Tasks
                    WHERE 1=1
                ";

                if (startDate.HasValue)
                {
                    sql += " AND (date(CreatedAt) >= date(@StartDate) OR (CompletedAt IS NOT NULL AND date(CompletedAt) >= date(@StartDate)))";
                    cmd.Parameters.AddWithValue("@StartDate", startDate.Value.ToString("yyyy-MM-dd"));
                }
                if (endDate.HasValue)
                {
                    sql += " AND (date(CreatedAt) <= date(@EndDate) OR (CompletedAt IS NOT NULL AND date(CompletedAt) <= date(@EndDate)))";
                    cmd.Parameters.AddWithValue("@EndDate", endDate.Value.ToString("yyyy-MM-dd"));
                }

                cmd.CommandText = sql;
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    overview.CompletedTasks = reader.GetInt32(0);
                    overview.PendingTasks = reader.GetInt32(1);
                    overview.OverdueTasks = reader.GetInt32(2);
                }
            }

            return overview;
        }

        public List<DailyTrendPoint> GetDailyTrends(DateTime startDate, DateTime endDate)
        {
            var list = new List<DailyTrendPoint>();
            using var conn = GetConnection();

            for (var d = startDate.Date; d <= endDate.Date; d = d.AddDays(1))
            {
                string dayStr = d.ToString("yyyy-MM-dd");
                var point = new DailyTrendPoint
                {
                    Date = d,
                    DayLabel = d.ToString("ddd dd")
                };

                // Focus Mins & Pomodoros for this day
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT 
                            COALESCE(SUM(CASE WHEN Type = 0 THEN DurationMinutes ELSE 0 END), 0),
                            COALESCE(SUM(CASE WHEN Type = 0 THEN 1 ELSE 0 END), 0)
                        FROM PomodoroSessions
                        WHERE date(StartedAt) = date(@Day);
                    ";
                    cmd.Parameters.AddWithValue("@Day", dayStr);
                    using var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        point.FocusMinutes = reader.GetInt32(0);
                        point.PomodorosCompleted = reader.GetInt32(1);
                    }
                }

                // Tasks completed for this day
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT COUNT(*) FROM Tasks
                        WHERE IsCompleted = 1 AND date(CompletedAt) = date(@Day);
                    ";
                    cmd.Parameters.AddWithValue("@Day", dayStr);
                    point.TasksCompleted = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
                }

                list.Add(point);
            }

            // Calculate height ratios for bar rendering (max scaling)
            int maxFocus = list.Max(p => p.FocusMinutes);
            int maxPomo = list.Max(p => p.PomodorosCompleted);
            int maxTasks = list.Max(p => p.TasksCompleted);

            foreach (var p in list)
            {
                p.FocusHeightRatio = maxFocus > 0 ? (double)p.FocusMinutes / maxFocus : 0.0;
                p.PomodoroHeightRatio = maxPomo > 0 ? (double)p.PomodorosCompleted / maxPomo : 0.0;
                p.TasksHeightRatio = maxTasks > 0 ? (double)p.TasksCompleted / maxTasks : 0.0;
            }

            return list;
        }

        public List<ProjectAnalytics> GetProjectAnalytics(DateTime? startDate = null, DateTime? endDate = null)
        {
            var list = new List<ProjectAnalytics>();
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();

            string sql = @"
                SELECT 
                    p.Id, p.Name, p.ColorHex,
                    COUNT(t.Id) AS TotalTasks,
                    SUM(CASE WHEN t.IsCompleted = 1 THEN 1 ELSE 0 END) AS CompletedTasks,
                    COALESCE((
                        SELECT SUM(ps.DurationMinutes)
                        FROM PomodoroSessions ps
                        INNER JOIN Tasks t2 ON ps.TaskId = t2.Id
                        WHERE t2.ProjectId = p.Id AND ps.Type = 0
                    ), 0) AS TotalFocusMinutes,
                    COALESCE((
                        SELECT SUM(ps.IsSuccessful)
                        FROM PomodoroSessions ps
                        INNER JOIN Tasks t2 ON ps.TaskId = t2.Id
                        WHERE t2.ProjectId = p.Id AND ps.Type = 0
                    ), 0) AS PomodorosCompleted
                FROM Projects p
                LEFT JOIN Tasks t ON p.Id = t.ProjectId
                GROUP BY p.Id, p.Name, p.ColorHex
                ORDER BY p.Name;
            ";
            cmd.CommandText = sql;

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new ProjectAnalytics
                {
                    ProjectId = reader.GetInt32(0),
                    ProjectName = reader.GetString(1),
                    ColorHex = reader.GetString(2),
                    TotalTasks = reader.GetInt32(3),
                    CompletedTasks = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                    TotalFocusMinutes = reader.GetInt32(5),
                    PomodorosCompleted = reader.GetInt32(6)
                });
            }

            return list;
        }
        #endregion

        #region User Settings
        public UserSettings GetSettings()
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT FocusDurationMinutes, ShortBreakMinutes, LongBreakMinutes, LongBreakInterval, SoundEnabled, NotificationsEnabled, LaunchAtStartup, AutoStartBreaks, AutoStartPomodoros, Theme FROM Settings WHERE Id = 1;";
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new UserSettings
                {
                    FocusDurationMinutes = reader.GetInt32(0),
                    ShortBreakMinutes = reader.GetInt32(1),
                    LongBreakMinutes = reader.GetInt32(2),
                    LongBreakInterval = reader.GetInt32(3),
                    SoundEnabled = reader.GetInt32(4) == 1,
                    NotificationsEnabled = reader.GetInt32(5) == 1,
                    LaunchAtStartup = reader.GetInt32(6) == 1,
                    AutoStartBreaks = reader.GetInt32(7) == 1,
                    AutoStartPomodoros = reader.GetInt32(8) == 1,
                    Theme = reader.GetString(9)
                };
            }
            return new UserSettings();
        }

        public void SaveSettings(UserSettings settings)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE Settings SET
                    FocusDurationMinutes = @FocusDurationMinutes,
                    ShortBreakMinutes = @ShortBreakMinutes,
                    LongBreakMinutes = @LongBreakMinutes,
                    LongBreakInterval = @LongBreakInterval,
                    SoundEnabled = @SoundEnabled,
                    NotificationsEnabled = @NotificationsEnabled,
                    LaunchAtStartup = @LaunchAtStartup,
                    AutoStartBreaks = @AutoStartBreaks,
                    AutoStartPomodoros = @AutoStartPomodoros,
                    Theme = @Theme
                WHERE Id = 1;
            ";
            cmd.Parameters.AddWithValue("@FocusDurationMinutes", settings.FocusDurationMinutes);
            cmd.Parameters.AddWithValue("@ShortBreakMinutes", settings.ShortBreakMinutes);
            cmd.Parameters.AddWithValue("@LongBreakMinutes", settings.LongBreakMinutes);
            cmd.Parameters.AddWithValue("@LongBreakInterval", settings.LongBreakInterval);
            cmd.Parameters.AddWithValue("@SoundEnabled", settings.SoundEnabled ? 1 : 0);
            cmd.Parameters.AddWithValue("@NotificationsEnabled", settings.NotificationsEnabled ? 1 : 0);
            cmd.Parameters.AddWithValue("@LaunchAtStartup", settings.LaunchAtStartup ? 1 : 0);
            cmd.Parameters.AddWithValue("@AutoStartBreaks", settings.AutoStartBreaks ? 1 : 0);
            cmd.Parameters.AddWithValue("@AutoStartPomodoros", settings.AutoStartPomodoros ? 1 : 0);
            cmd.Parameters.AddWithValue("@Theme", settings.Theme);
            cmd.ExecuteNonQuery();
        }
        #endregion
    }
}
