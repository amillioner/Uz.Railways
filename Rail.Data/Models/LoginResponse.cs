using System;

namespace Rail.Data.Models;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime Expires { get; set; }
    public string[] Roles { get; set; } = Array.Empty<string>();
}