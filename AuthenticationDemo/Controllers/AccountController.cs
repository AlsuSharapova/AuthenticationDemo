using AuthenticationDemo.Models;
using AuthenticationDemo.Services;
using AuthenticationDemo.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AuthenticationDemo.Controllers {
    public class AccountController : Controller {

        private readonly SignInManager<Users> signInManager;
        private readonly UserManager<Users> userManager;
        private readonly IEmailSender emailSender;

        public AccountController(SignInManager<Users> signInManager, UserManager<Users> userManager, IEmailSender emailSender) {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.emailSender = emailSender;
        }

        //LOGIN
        public IActionResult Login() {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model) {

            if (ModelState.IsValid) {

                var user = await userManager.FindByEmailAsync(model.Email);

                if(user != null && !user.EmailConfirmed) {
                    ModelState.AddModelError("", "Please confirm your email before logging in.");
                    return View(model);
                }

                var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);

                if (result.Succeeded) {
                    return RedirectToAction("Index", "Home");
                }
                else {
                    ModelState.AddModelError("", "Email or password is incorrect.");
                    return View(model);
                }
            }
            return View(model);
        }

        //REGISTER
        public IActionResult Register() {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model) {
            if (ModelState.IsValid) {
                var existingUser = await userManager.FindByEmailAsync(model.Email);

                if (existingUser != null) {
                    if (existingUser.EmailConfirmed) {
                        ModelState.AddModelError("", "Пользователь с таким email уже зарегистрирован.");
                        return View(model);
                    }
                    else {
                        // Аккаунт есть, но не подтверждён — обновляем данные и отправляем новый код
                        existingUser.FullName = model.Name;

                        var removePasswordResult = await userManager.RemovePasswordAsync(existingUser);
                        if (!removePasswordResult.Succeeded) {
                            ModelState.AddModelError("", "Не удалось обновить данные. Попробуйте позже.");
                            return View(model);
                        }

                        var addPasswordResult = await userManager.AddPasswordAsync(existingUser, model.Password);
                        if (!addPasswordResult.Succeeded) {
                            foreach (var error in addPasswordResult.Errors) {
                                ModelState.AddModelError("", error.Description);
                            }
                            return View(model);
                        }

                        await userManager.UpdateAsync(existingUser);
                        await GenerateAndSendConfirmationCode(existingUser);

                        return RedirectToAction("ConfirmEmail", "Account", new { userId = existingUser.Id, purpose = CodePurpose.Registration });
                    }
                }

                // Пользователя ещё не было — создаём нового
                Users newUser = new Users { FullName = model.Name, Email = model.Email, UserName = model.Email };
                var result = await userManager.CreateAsync(newUser, model.Password);

                if (result.Succeeded) {
                    await GenerateAndSendConfirmationCode(newUser);
                    return RedirectToAction("ConfirmEmail", "Account", new { userId = newUser.Id, purpose = CodePurpose.Registration });
                }
                else {
                    foreach (var error in result.Errors) {
                        ModelState.AddModelError("", error.Description);
                    }
                    return View(model);
                }
            }
            return View(model);
        }        

        //CONFIRM EMAIL
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, CodePurpose purpose) {
            if (string.IsNullOrEmpty(userId)) {
                return RedirectToAction("Register", "Account");
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user == null) {
                return RedirectToAction("Register", "Account");
            }

            var model = new ConfirmEmailViewModel {
                UserId = userId,
                SecondsRemaining = GetResendSecondsRemaining(user),
                Purpose = purpose
            };
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> ConfirmEmail(ConfirmEmailViewModel model) {
            if (!ModelState.IsValid) {
                return View(model);
            }

            var user = await userManager.FindByIdAsync(model.UserId);
            if (user == null) {
                ModelState.AddModelError("", "User not found.");
                model.SecondsRemaining = 0;
                return View(model);
            }

            if (user.EmailConfirmationCode != model.Code) {
                ModelState.AddModelError("", "Invalid confirmation code.");
                model.SecondsRemaining = GetResendSecondsRemaining(user);
                return View(model);
            }

            if (user.EmailConfirmationCodeExpiry == null || user.EmailConfirmationCodeExpiry < DateTime.UtcNow) {
                ModelState.AddModelError("", "Confirmation code has expired.");
                model.SecondsRemaining = GetResendSecondsRemaining(user);
                return View(model);
            }

            user.EmailConfirmationCode = null;
            user.EmailConfirmationCodeExpiry = null;

            if (model.Purpose == CodePurpose.Registration) {
                user.EmailConfirmed = true;
                await userManager.UpdateAsync(user);

                TempData["Message"] = "Email confirmed successfully. You can now log in.";
                return RedirectToAction("Login", "Account");
            }
            else // CodePurpose.PasswordReset
            {
                var resetToken = Guid.NewGuid().ToString("N");
                user.PasswordResetToken = resetToken;
                user.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(10);
                await userManager.UpdateAsync(user);

                return RedirectToAction("ChangePassword", "Account", new { username = user.UserName, token = resetToken });
            }
        }

        //RESEND CODE
        [HttpPost]
        public async Task<IActionResult> ResendCode([FromBody] ResendCodeRequest request) {

            var user = await userManager.FindByIdAsync(request.UserId);

            if(user == null || user.EmailConfirmed) {
                return Json(new { success = false });
            }

            await GenerateAndSendConfirmationCode(user);

            return Json(new { success = true });

        }
        public class ResendCodeRequest {
            public string UserId { get; set; }
            public CodePurpose Purpose { get; set; }
        }
        
        //VERIFY EMAIL
        public IActionResult VerifyEmail() {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> VerifyEmail(VerifyEmailViewModel model) {
            if (ModelState.IsValid) {
                var user = await userManager.FindByEmailAsync(model.Email);
                if (user == null) {
                    ModelState.AddModelError("", "Something is wrong.");
                    return View(model);
                }
                else if (!user.EmailConfirmed) {
                    ModelState.AddModelError("", "Please register first.");
                    return View(model);
                }
                else {
                    await GenerateAndSendConfirmationCode(user);
                    return RedirectToAction("ConfirmEmail", "Account", new { userId = user.Id, purpose = CodePurpose.PasswordReset });
                }
            }
            return View(model);
        }

        //CHANGE PASSWORD
        public async Task<IActionResult> ChangePassword(string username, string token) {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(token)) {
                return RedirectToAction("VerifyEmail", "Account");
            }

            var user = await userManager.FindByNameAsync(username);
            if (user == null || user.PasswordResetToken != token || user.PasswordResetTokenExpiry == null || user.PasswordResetTokenExpiry < DateTime.UtcNow) {
                return RedirectToAction("VerifyEmail", "Account");
            }

            return View(new ChangePasswordViewModel { Email = username, Token = token });
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model) {
            if (ModelState.IsValid) {
                var user = await userManager.FindByEmailAsync(model.Email);

                if (user == null || user.PasswordResetToken != model.Token || user.PasswordResetTokenExpiry == null || user.PasswordResetTokenExpiry < DateTime.UtcNow) {
                    ModelState.AddModelError("", "This link has expired. Please request a new one.");
                    return View(model);
                }

                var result = await userManager.RemovePasswordAsync(user);
                result = await userManager.AddPasswordAsync(user, model.NewPassword);
                if (result.Succeeded) {
                    user.PasswordResetToken = null;
                    user.PasswordResetTokenExpiry = null;
                    await userManager.UpdateAsync(user);

                    return RedirectToAction("Login", "Account");
                }
                else {
                    foreach (var error in result.Errors) {
                        ModelState.AddModelError("", error.Description);
                    }
                    return View(model);
                }
            }
            else {
                ModelState.AddModelError("", "Something went wrong. Try again.");
                return View(model);
            }
        }

        //LOGOUT
        public async Task<IActionResult> Logout() {
            await signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }


        //HELPERS
        private async Task GenerateAndSendConfirmationCode(Users user) {
            var random = new Random();
            var code = random.Next(100000, 999999).ToString();

            user.EmailConfirmationCode = code;
            user.EmailConfirmationCodeExpiry = DateTime.UtcNow.AddMinutes(10);
            await userManager.UpdateAsync(user);

            await emailSender.SendEmailAsync(
                user.Email,
                "Код подтверждения",
                $"Ваш код подтверждения: <b>{code}</b>. Код действителен 10 минут.");
        }

        private int GetResendSecondsRemaining(Users user) {
            if (user.EmailConfirmationCodeExpiry == null)
                return 0;

            // Код отправляется на 10 минут, а "подождать" нужно всего 1 минуту
            var sentAt = user.EmailConfirmationCodeExpiry.Value.AddMinutes(-10);
            var resendAllowedAt = sentAt.AddMinutes(1);

            var secondsLeft = (int)(resendAllowedAt - DateTime.UtcNow).TotalSeconds;
            return secondsLeft > 0 ? secondsLeft : 0;
        }
    }
}
