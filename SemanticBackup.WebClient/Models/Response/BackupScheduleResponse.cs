﻿using System;

namespace SemanticBackup.WebClient.Models.Response
{
    public class BackupScheduleResponse
    {
        public string Id { get; set; }
        public string BackupDatabaseInfoId { get; set; }
        public string Name { get; set; }
        public string ScheduleType { get; set; }
        public int EveryHours { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime NextRun { get { return StartDate.AddHours(EveryHours); } }
        public DateTime? LastRun { get; set; } = null;
    }
}