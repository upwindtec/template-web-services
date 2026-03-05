//
// Template Web Services Application for Upwindtec Cloud.
// Can be freely adapted and distributed without resitrictions.
// For more information, visit https://www.upwindtec.pt
//
namespace expo_sample_web_services;

public partial class item : UpwindtecCloudStorageUtils.IBaseEntity
{
    public string Id { get; set; } = null!;

    public bool? done { get; set; }

    public string? value { get; set; }
}

