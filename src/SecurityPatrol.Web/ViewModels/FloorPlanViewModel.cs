using SecurityPatrol.Web.Models;

namespace SecurityPatrol.Web.ViewModels;

public class FloorPlanListViewModel
{
    public List<FloorPlanBuildingGroup> Buildings { get; set; } = new();
}

public class FloorPlanBuildingGroup
{
    public Building Building { get; set; } = null!;
    public List<FloorPlanFloorGroup> Floors { get; set; } = new();
}

public class FloorPlanFloorGroup
{
    public Floor Floor { get; set; } = null!;
    public List<FloorPlan> Plans { get; set; } = new();
}

public class FloorPlanEditorViewModel
{
    public FloorPlan FloorPlan { get; set; } = null!;
    public List<Location> MappedLocations { get; set; } = new();
    public List<Location> UnmappedLocations { get; set; } = new();
    public List<Floor> AllFloors { get; set; } = new();
    public List<Building> AllBuildings { get; set; } = new();
}

public class FloorPlanViewerViewModel
{
    public FloorPlan FloorPlan { get; set; } = null!;
    public List<Location> Locations { get; set; } = new();
}

// AJAX request/response models
public class AddMapPinRequest
{
    public int FloorPlanId { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ExistingLocationId { get; set; }
}

public class MovePinRequest
{
    public int LocationId { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
}

public class UpdatePinRequest
{
    public int LocationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class RemovePinRequest
{
    public int LocationId { get; set; }
}

public class SavePinSizeRequest
{
    public int FloorPlanId { get; set; }
    public int PinRadius { get; set; }
}
