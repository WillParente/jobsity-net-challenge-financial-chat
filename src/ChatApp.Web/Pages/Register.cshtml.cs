using System.ComponentModel.DataAnnotations;
using ChatApp.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChatApp.Web.Pages;

public class RegisterModel : PageModel
{
    private readonly UserAccountService _accounts;

    public RegisterModel(UserAccountService accounts)
    {
        _accounts = accounts;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        [StringLength(30, MinimumLength = 3)]
        [RegularExpression("^[a-zA-Z0-9_.-]+$",
            ErrorMessage = "Username may only contain letters, digits, '_', '.' and '-'.")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var (succeeded, error) = await _accounts.RegisterAsync(Input.Username, Input.Password);
        if (!succeeded)
        {
            ModelState.AddModelError(string.Empty, error!);
            return Page();
        }

        return RedirectToPage("/Login");
    }
}
