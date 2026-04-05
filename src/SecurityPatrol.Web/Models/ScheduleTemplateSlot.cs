namespace SecurityPatrol.Web.Models;
public class ScheduleTemplateSlot
{
    public int Id { get; set; }
    public int TemplateId { get; set; }
    public ScheduleTemplate Template { get; set; } = null!;
    public string Label { get; set; } = string.Empty;   // e.g. "Morning Patrol"
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int? PatrolRouteId { get; set; }
    public PatrolRoute? PatrolRoute { get; set; }
    public int OrderIndex { get; set; }
}
