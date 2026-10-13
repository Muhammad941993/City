using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CityGuide.Web.Pages.Admin;

public class LoginModel(SignInManager<IdentityUser> signIn) : PageModel
{
    public string? Email { get; private set; }
    public string? Error { get; private set; }
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }

    public IActionResult OnGet() => User.Identity?.IsAuthenticated == true ? Redirect("/admin") : Page();

    public async Task<IActionResult> OnPostAsync(string email, string password)
    {
        Email = email;
        var result = await signIn.PasswordSignInAsync(email, password, isPersistent: true, lockoutOnFailure: true);
        if (result.Succeeded)
            return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : "/admin");

        Error = result.IsLockedOut ? "الحساب اتقفل مؤقتاً بسبب محاولات كتير. جرب بعد شوية." : "الإيميل أو كلمة السر غلط.";
        return Page();
    }

    public async Task<IActionResult> OnPostLogoutAsync()
    {
        await signIn.SignOutAsync();
        return Redirect("/");
    }
}
