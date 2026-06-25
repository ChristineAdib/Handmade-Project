using HandoraDomain.Models.AppUser;
using HandoraMVC.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HandoraMVC.Controllers
{

    public class AccountController(
        SignInManager<User> signInManager,
        UserManager<User> userManager) : Controller
    {
        private readonly SignInManager<User> _signInManager = signInManager;
        private readonly UserManager<User> _userManager = userManager;

        // ── GET /Account/Login ───────────────────────────────────
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            // لو مسجّل دخول خلاص، روح للـ Analytics مباشرة
            if (_signInManager.IsSignedIn(User))
                return RedirectToAction("Analytics", "Admin");

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // ── POST /Account/Login ──────────────────────────────────
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // تأكد إن اليوزر موجود
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user is null || user.IsDeleted)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            // تأكد إن هو Admin
            var isAdmin = await _userManager.IsInRoleAsync(user, AppRoles.Admin);
            if (!isAdmin)
            {
                ModelState.AddModelError(string.Empty, "Access denied. Admin accounts only.");
                return View(model);
            }

            // سجّل الدخول بـ Cookie
            var result = await _signInManager.PasswordSignInAsync(
                user,
                model.Password,
                isPersistent: model.RememberMe,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                var returnUrl = model.ReturnUrl;
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Analytics", "Admin");
            }

            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        // ── POST /Account/Logout ─────────────────────────────────
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        // ── GET /Account/AccessDenied ────────────────────────────
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied() => View();
    }

}
