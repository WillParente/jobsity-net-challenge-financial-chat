using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ChatApp.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChatApp.Web.Pages;

public class LoginModel : PageModel
{
    private readonly UserAccountService _accounts;

    public LoginModel(UserAccountService accounts)
    {
        _accounts = accounts;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    public IActionResult OnGet()
    {
        return User.Identity?.IsAuthenticated == true ? RedirectToPage("/Index") : Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _accounts.ValidateCredentialsAsync(Input.Username, Input.Password);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return Page();
        }

        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Name, user.Username) },
            CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return RedirectToPage("/Index");
    }
}
