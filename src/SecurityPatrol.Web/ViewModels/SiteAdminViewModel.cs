using SecurityPatrol.Web.Models;
using System.ComponentModel.DataAnnotations;

namespace SecurityPatrol.Web.ViewModels;

public class SiteAdminViewModel
{
    public List<Building> BuildingTree { get; set; } = new();
}

public class BuildingsViewModel
{
    public List<Building> Buildings { get; set; } = new();
    public BuildingFormModel Form { get; set; } = new();
}

public class BuildingFormModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Building name is required")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? TimeZoneId { get; set; }

    public bool IsActive { get; set; } = true;
}

public class FloorsViewModel
{
    public List<Floor> Floors { get; set; } = new();
    public List<Building> Buildings { get; set; } = new();
    public FloorFormModel Form { get; set; } = new();
}

public class FloorFormModel
{
    public int Id { get; set; }

    [Required]
    public int BuildingId { get; set; }

    [Required(ErrorMessage = "Floor name is required")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public int FloorNumber { get; set; }

    public bool IsActive { get; set; } = true;
}

public class LocationsViewModel
{
    public List<Location> Locations { get; set; } = new();
    public List<Building> Buildings { get; set; } = new();
    public List<Floor> Floors { get; set; } = new();
    public LocationFormModel Form { get; set; } = new();
}

public class LocationFormModel
{
    public int Id { get; set; }

    [Required]
    public int BuildingId { get; set; }

    public int? FloorId { get; set; }

    [Required(ErrorMessage = "Location name is required")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
