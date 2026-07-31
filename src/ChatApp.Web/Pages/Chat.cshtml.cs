using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChatApp.Web.Pages;

[Authorize]
public class ChatModel : PageModel
{
    public void OnGet()
    {
    }
}
