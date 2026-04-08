namespace SecurityPatrol.Web.Models;

public class FloorPlanWaypoint
{
    public int Id { get; set; }
    public int FloorPlanId { get; set; }
    public int FromLocationId { get; set; }
    public int ToLocationId { get; set; }
    public int OrderIndex { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public FloorPlan FloorPlan { get; set; } = null!;
}
