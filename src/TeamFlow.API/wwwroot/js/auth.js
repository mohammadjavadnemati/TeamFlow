// اگر قبلا لاگین کرده، مستقیم بره داشبورد
if (Storage.getAccessToken()) {
  window.location.href = "dashboard.html";
}

function showForm(which) {
  document.getElementById("login-form").style.display = which === "login" ? "block" : "none";
  document.getElementById("signup-form").style.display = which === "signup" ? "block" : "none";
  hideBanner("login-banner");
  hideBanner("signup-banner");
}

function setLoading(buttonId, isLoading, label) {
  const btn = document.getElementById(buttonId);
  btn.disabled = isLoading;
  btn.querySelector(".btn-label").textContent = isLoading ? "لطفاً صبر کنید..." : label;
}

// ─── Login ───────────────────────────────────────────────────────────────────
document.getElementById("login-form").addEventListener("submit", async (e) => {
  e.preventDefault();
  hideBanner("login-banner");

  const email = document.getElementById("li-email").value.trim();
  const password = document.getElementById("li-pass").value;

  if (!email || !password) {
    showBanner("login-banner", "ایمیل و رمز عبور را وارد کنید.");
    return;
  }

  setLoading("login-submit", true, "ورود به فضای کاری");
  try {
    const auth = await Api.login(email, password);
    Storage.setSession(auth);
    window.location.href = "dashboard.html";
  } catch (err) {
    showBanner("login-banner", err.message || "ورود ناموفق بود.");
  } finally {
    setLoading("login-submit", false, "ورود به فضای کاری");
  }
});

// ─── Signup ──────────────────────────────────────────────────────────────────
document.getElementById("signup-form").addEventListener("submit", async (e) => {
  e.preventDefault();
  hideBanner("signup-banner");

  const firstName = document.getElementById("su-first").value.trim();
  const lastName = document.getElementById("su-last").value.trim();
  const email = document.getElementById("su-email").value.trim();
  const password = document.getElementById("su-pass").value;

  if (!firstName || !lastName || !email || password.length < 6) {
    showBanner("signup-banner", "همه فیلدها را کامل کنید (رمز عبور حداقل ۶ کاراکتر).");
    return;
  }

  setLoading("signup-submit", true, "شروع کن، رایگانه");
  try {
    const auth = await Api.register(firstName, lastName, email, password);
    Storage.setSession(auth);
    window.location.href = "dashboard.html";
  } catch (err) {
    showBanner("signup-banner", err.message || "ثبت‌نام ناموفق بود.");
  } finally {
    setLoading("signup-submit", false, "شروع کن، رایگانه");
  }
});
