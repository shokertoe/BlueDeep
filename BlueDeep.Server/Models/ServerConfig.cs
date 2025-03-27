using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace BlueDeep.Server.Models;

/// <summary>
/// Server configuration
/// </summary>
public sealed class ServerConfig
{
    /// <summary>
    /// Port
    /// </summary>
    [Required]
    [Range(1, 65535)]
    public int Port { get; set; }
    
    [DefaultValue(false)]
    public bool UseWebServer { get; set; }
}