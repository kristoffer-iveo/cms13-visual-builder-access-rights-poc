using System.ComponentModel.DataAnnotations;

namespace Cms13Poc.Models;

public class LoginViewModel
{
    [Required]
    public string Username { get; set; }

    [Required]
    public string Password { get; set; }
}
