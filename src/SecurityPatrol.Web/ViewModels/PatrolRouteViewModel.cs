using SecurityPatrol.Web.Models;
using System.ComponentModel.DataAnnotations;

namespace SecurityPatrol.Web.ViewModels;

public class PatrolRouteViewModel
{
    public List<PatrolRoute> Routes { get; set; } = new();
    public PatrolRouteFormModel Form { get; set; } = new();
}

public class PatrolRouteFormModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Route name is required")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public List<RouteLocationItem> Locations { get; set; } = new();
}

public class RouteLocationItem
{
    public int LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string? FloorName { get; set; }
    public string BuildingName { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public int? PatrolRouteLocationId { get; set; }
}

public class EditRouteViewModel
{
    public PatrolRouteFormModel Form { get; set; } = new();
    public List<Location> AllLocations { get; set; } = new();
    public List<Building> Buildings { get; set; } = new();
}
