using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace BlueDeep.Server.Models;

public sealed class ServerConfig
{
    [Required]
    [Range(1, 65535)]
    public int Port { get; set; }
}