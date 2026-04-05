namespace SecurityPatrol.Web.Models;

public class PatrolRouteLocation
{
    public int Id { get; set; }

    public int PatrolRouteId { get; set; }

    public int LocationId { get; set; }

    public int OrderIndex { get; set; }

    public PatrolRoute PatrolRoute { get; set; } = null!;
    public Location Location { get; set; } = null!;
}
